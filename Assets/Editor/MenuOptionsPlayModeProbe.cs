using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class MenuOptionsPlayModeProbe
{
    private const string PhaseKey = "Trucardo.MenuOptionsProbe.Phase5";
    private const string SnapshotKey = "Trucardo.MenuOptionsProbe.Snapshot";
    private const string ExpectedResolutionKey = "Trucardo.MenuOptionsProbe.Resolution";
    private const string ResultPath = "Logs/MenuOptionsPlayModeProbe.log";
    private static readonly string[] SettingKeys =
    {
        "Settings_MasterVolume", "Settings_MusicVolume", "Settings_Brightness",
        "Settings_Quality", "Settings_ResolutionWidth", "Settings_ResolutionHeight"
    };
    private static readonly string[] QualityLabels =
    {
        "Muy bajo", "Bajo", "Medio", "Alto", "Muy alto", "Ultra"
    };
    private static bool running;

    [Serializable]
    private sealed class Snapshot
    {
        public bool[] hasKeys;
        public float[] floatValues;
        public int[] intValues;
        public int qualityLevel;
        public int screenWidth;
        public int screenHeight;
        public int fullScreenMode;
    }

    static MenuOptionsPlayModeProbe()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        if (EditorApplication.isPlaying)
        {
            EditorApplication.delayCall += RunCurrentPhase;
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.delayCall += RunCurrentPhase;
        }
        else if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetInt(PhaseKey, 0) == 1)
        {
            SessionState.SetInt(PhaseKey, 2);
            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
        }
    }

    private static void RunCurrentPhase()
    {
        if (!EditorApplication.isPlaying || running)
        {
            return;
        }

        int phase = SessionState.GetInt(PhaseKey, 0);
        if (phase == 0)
        {
            running = true;
            try
            {
                RunFirstPass();
            }
            catch (Exception exception)
            {
                Record("FAIL: Play Mode pass 1 threw " + exception);
                FinishFirstPass();
            }
            running = false;
        }
        else if (phase == 2)
        {
            running = true;
            try
            {
                RunRestartPass();
            }
            catch (Exception exception)
            {
                Record("FAIL: Play Mode restart pass threw " + exception);
            }
            finally
            {
                RestoreOriginalState();
                SessionState.SetInt(PhaseKey, 3);
                running = false;
                EditorApplication.isPlaying = false;
                Debug.Log("MenuOptions Play Mode probe finished. See Logs/MenuOptionsPlayModeProbe.log.");
            }
        }
    }

    private static void RunFirstPass()
    {
        SaveOriginalState();
        Record("Play Mode pass 1 started.");

        MainMenu mainMenu = UnityEngine.Object.FindAnyObjectByType<MainMenu>(FindObjectsInactive.Include);
        Check(mainMenu != null, "MainMenu exists in the running scene.");
        if (mainMenu == null)
        {
            return;
        }

        mainMenu.OpenOptionsMenu();
        Slider master = FindControl<Slider>("SliderVolumenGeneral");
        Slider music = FindControl<Slider>("SliderVolumenMusica");
        Slider brightness = FindControl<Slider>("SliderBrillo");
        TMP_Dropdown quality = FindControl<TMP_Dropdown>("DropdownCalidadImagen");
        TMP_Dropdown resolution = FindControl<TMP_Dropdown>("DropdownResolucion");
        Button back = FindControllerBackButton();

        Check(master != null && music != null && brightness != null && quality != null && resolution != null && back != null,
            "All six existing controls resolve in MenuOptions.");
        if (master == null || music == null || brightness == null || quality == null || resolution == null || back == null)
        {
            return;
        }

        master.value = 0.33f;
        Check(Near(AudioListener.volume, 0.33f) && Near(PlayerPrefs.GetFloat("Settings_MasterVolume"), 0.33f),
            "Master slider applies and stores 0.33.");

        brightness.value = 0.8f;
        Volume[] brightnessVolumes = FindBrightnessVolumes();
        bool exposureApplied = brightnessVolumes.Length == 1 &&
            brightnessVolumes[0].profile.TryGet(out ColorAdjustments adjustments) && Near(adjustments.postExposure.value, 1.2f);
        Check(exposureApplied && Near(PlayerPrefs.GetFloat("Settings_Brightness"), 0.8f),
            "Brightness slider applies +1.2 EV at 0.8 and stores the value.");

        Check(quality.options.Select(option => option.text).SequenceEqual(QualityLabels),
            "Quality dropdown contains the six exact configured labels.");
        PropertyInfo pipelineProperty = typeof(QualitySettings).GetProperty("renderPipeline", BindingFlags.Public | BindingFlags.Static);
        bool qualityApplied = quality.options.Count == 6 && pipelineProperty != null;
        for (int index = 0; index < Mathf.Min(quality.options.Count, QualityLabels.Length); index++)
        {
            quality.value = index;
            RenderPipelineAsset selectedPipeline = pipelineProperty == null ? null : pipelineProperty.GetValue(null) as RenderPipelineAsset;
            qualityApplied &= QualitySettings.GetQualityLevel() == index &&
                (pipelineProperty == null || selectedPipeline == GraphicsSettings.currentRenderPipeline);
        }
        Check(qualityApplied, "All six quality choices activate their matching Quality Level and URP Asset.");
        quality.value = 5;

        Resolution[] availableResolutions = GameSettingsManager.GetAvailableResolutions();
        string[] resolutionLabels = resolution.options.Select(option => option.text).ToArray();
        bool resolutionOptionsCorrect = availableResolutions.Length == resolutionLabels.Length &&
            resolutionLabels.Distinct().Count() == resolutionLabels.Length &&
            resolutionLabels.All(label => System.Text.RegularExpressions.Regex.IsMatch(label, @"^\d+ x \d+$"));
        if (resolutionLabels.Contains("1920 x 1080"))
        {
            resolutionOptionsCorrect &= resolutionLabels[0] == "1920 x 1080";
        }
        Check(resolutionOptionsCorrect, "Resolution list has unique width x height labels and prefers 1920 x 1080.");

        int resolutionIndex = Array.FindIndex(availableResolutions,
            item => item.width != Screen.width || item.height != Screen.height);
        if (resolutionIndex >= 0)
        {
            Resolution selectedResolution = availableResolutions[resolutionIndex];
            resolution.value = resolutionIndex;
            Check(PlayerPrefs.GetInt("Settings_ResolutionWidth") == selectedResolution.width &&
                PlayerPrefs.GetInt("Settings_ResolutionHeight") == selectedResolution.height,
                "Resolution selection applies and stores its width and height.");
            SessionState.SetInt(ExpectedResolutionKey, selectedResolution.width);
            SessionState.SetInt(ExpectedResolutionKey + ".height", selectedResolution.height);
        }
        else
        {
            Check(false, "At least two distinct supported resolutions are available to test.");
        }

        back.onClick.Invoke();
        Check(!mainMenu.optionsMenu.activeSelf && mainMenu.mainMenu.activeSelf,
            "Existing back control returns to the existing main menu.");
        mainMenu.OpenOptionsMenu();
        Check(Near(master.value, 0.33f) && Near(music.value, PlayerPrefs.GetFloat("Settings_MusicVolume", 1f)) &&
            Near(brightness.value, 0.8f) && quality.value == 5,
            "Closing and reopening options restores saved slider and quality values.");
        Check(FindBrightnessVolumes().Length == 1, "Repeated menu opens do not create duplicate global brightness volumes.");

        GameObject musicObject = new GameObject("MusicaBar");
        AudioSource musicSource = musicObject.AddComponent<AudioSource>();
        musicSource.volume = 0.398f;
        musicSource.loop = true;
        GameObject effectObject = new GameObject("UnrelatedLoopingEffect");
        AudioSource effectSource = effectObject.AddComponent<AudioSource>();
        effectSource.volume = 0.73f;
        effectSource.loop = true;
        music.value = 0.27f;
        Check(Near(musicSource.volume, 0.398f * 0.27f) && Near(effectSource.volume, 0.73f) &&
            Near(PlayerPrefs.GetFloat("Settings_MusicVolume"), 0.27f),
            "Music slider changes only the explicitly named music source and stores the value.");
        UnityEngine.Object.Destroy(musicObject);
        UnityEngine.Object.Destroy(effectObject);
        mainMenu.OpenMainMenuPanel();
        FinishFirstPass();
    }

    private static void FinishFirstPass()
    {
        PlayerPrefs.Save();
        SessionState.SetInt(PhaseKey, 1);
        running = false;
        EditorApplication.isPlaying = false;
    }

    private static void RunRestartPass()
    {
        Record("Play Mode restart pass started.");
        MainMenu mainMenu = UnityEngine.Object.FindAnyObjectByType<MainMenu>(FindObjectsInactive.Include);
        if (mainMenu == null)
        {
            Check(false, "MainMenu exists after restarting Play Mode.");
            return;
        }

        mainMenu.OpenOptionsMenu();
        Slider master = FindControl<Slider>("SliderVolumenGeneral");
        Slider music = FindControl<Slider>("SliderVolumenMusica");
        Slider brightness = FindControl<Slider>("SliderBrillo");
        TMP_Dropdown quality = FindControl<TMP_Dropdown>("DropdownCalidadImagen");
        Resolution[] resolutions = GameSettingsManager.GetAvailableResolutions();
        int width = SessionState.GetInt(ExpectedResolutionKey, 0);
        int height = SessionState.GetInt(ExpectedResolutionKey + ".height", 0);
        Check(Near(master.value, 0.33f) && Near(AudioListener.volume, 0.33f) &&
            Near(music.value, 0.27f) && Near(brightness.value, 0.8f) &&
            quality.value == 5 && QualitySettings.GetQualityLevel() == 5,
            "PlayerPrefs and applied settings survive a Play Mode restart.");
        Check(PlayerPrefs.GetInt("Settings_ResolutionWidth") == width && PlayerPrefs.GetInt("Settings_ResolutionHeight") == height &&
            resolutions.Any(item => item.width == width && item.height == height),
            "Selected resolution survives a Play Mode restart.");
        Check(FindBrightnessVolumes().Length == 1, "Brightness still uses exactly one persistent global Volume after restart.");
        mainMenu.OpenMainMenuPanel();
    }

    private static void SaveOriginalState()
    {
        Snapshot snapshot = new Snapshot
        {
            hasKeys = SettingKeys.Select(PlayerPrefs.HasKey).ToArray(),
            floatValues = new[]
            {
                PlayerPrefs.GetFloat("Settings_MasterVolume", 1f),
                PlayerPrefs.GetFloat("Settings_MusicVolume", 1f),
                PlayerPrefs.GetFloat("Settings_Brightness", 0.5f)
            },
            intValues = new[]
            {
                PlayerPrefs.GetInt("Settings_Quality", QualitySettings.GetQualityLevel()),
                PlayerPrefs.GetInt("Settings_ResolutionWidth", Screen.width),
                PlayerPrefs.GetInt("Settings_ResolutionHeight", Screen.height)
            },
            qualityLevel = QualitySettings.GetQualityLevel(),
            screenWidth = Screen.width,
            screenHeight = Screen.height,
            fullScreenMode = (int)Screen.fullScreenMode
        };
        SessionState.SetString(SnapshotKey, JsonUtility.ToJson(snapshot));
    }

    private static void RestoreOriginalState()
    {
        Snapshot snapshot = JsonUtility.FromJson<Snapshot>(SessionState.GetString(SnapshotKey, string.Empty));
        if (snapshot == null)
        {
            return;
        }

        GameSettingsManager.SetMasterVolume(snapshot.floatValues[0]);
        GameSettingsManager.SetMusicVolume(snapshot.floatValues[1]);
        GameSettingsManager.SetBrightness(snapshot.floatValues[2]);
        GameSettingsManager.SetQuality(Mathf.Clamp(snapshot.qualityLevel, 0, GameSettingsManager.QualityOptionCount - 1));
        Screen.SetResolution(snapshot.screenWidth, snapshot.screenHeight, (FullScreenMode)snapshot.fullScreenMode);

        for (int index = 0; index < SettingKeys.Length; index++)
        {
            if (!snapshot.hasKeys[index])
            {
                PlayerPrefs.DeleteKey(SettingKeys[index]);
            }
            else if (index < 3)
            {
                PlayerPrefs.SetFloat(SettingKeys[index], snapshot.floatValues[index]);
            }
            else
            {
                PlayerPrefs.SetInt(SettingKeys[index], snapshot.intValues[index - 3]);
            }
        }

        PlayerPrefs.Save();
    }

    private static T FindControl<T>(string objectName) where T : Component
    {
        return Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(component =>
            component.gameObject.scene.IsValid() && component.gameObject.name == objectName);
    }

    private static Button FindControllerBackButton()
    {
        MenuOptionsController controller = UnityEngine.Object.FindAnyObjectByType<MenuOptionsController>(FindObjectsInactive.Include);
        FieldInfo field = typeof(MenuOptionsController).GetField("backButton", BindingFlags.Instance | BindingFlags.NonPublic);
        return controller != null && field != null ? field.GetValue(controller) as Button : null;
    }

    private static Volume[] FindBrightnessVolumes()
    {
        return Resources.FindObjectsOfTypeAll<Volume>()
            .Where(volume => volume.gameObject.name == "SettingsBrightnessVolume")
            .ToArray();
    }

    private static bool Near(float left, float right)
    {
        return Mathf.Abs(left - right) < 0.015f;
    }

    private static void Check(bool passed, string message)
    {
        Record((passed ? "PASS: " : "FAIL: ") + message);
    }

    private static void Record(string message)
    {
        Debug.Log("[MenuOptionsPlayModeProbe] " + message);
        File.AppendAllText(ResultPath, message + Environment.NewLine);
    }
}