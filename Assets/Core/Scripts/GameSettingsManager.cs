using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSettingsManager : MonoBehaviour
{
    private const string MasterVolumeKey = "Settings_MasterVolume";
    private const string MusicVolumeKey = "Settings_MusicVolume";
    private const string BrightnessKey = "Settings_Brightness";
    private const string QualityKey = "Settings_Quality";
    private const string ResolutionWidthKey = "Settings_ResolutionWidth";
    private const string ResolutionHeightKey = "Settings_ResolutionHeight";
    private const string BrightnessObjectName = "SettingsBrightnessOverlay";
    private const string LegacyBrightnessVolumeName = "SettingsBrightnessVolume";
    private const string MusicSourceObjectName = "MusicaBar";
    private const string MusicClipKeyword = "instrumental";

    private static readonly string[] QualityLabels =
    {
        "Muy bajo", "Bajo", "Medio", "Alto", "Muy alto", "Ultra"
    };

    private static readonly Dictionary<AudioSource, float> MusicSourceVolumes = new Dictionary<AudioSource, float>();
    private static bool initialized;
    private static float musicVolume = 1f;
    private static Image brightnessOverlay;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        initialized = false;
        musicVolume = 1f;
        brightnessOverlay = null;
        MusicSourceVolumes.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeBeforeSceneLoad()
    {
        EnsureInitialized();
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    public static int QualityOptionCount
    {
        get { return Mathf.Min(QualityLabels.Length, QualitySettings.names.Length); }
    }

    public static string GetQualityLabel(int index)
    {
        return QualityLabels[index];
    }

    public static Resolution[] GetAvailableResolutions()
    {
        Dictionary<long, Resolution> uniqueResolutions = new Dictionary<long, Resolution>();
        foreach (Resolution resolution in Screen.resolutions)
        {
            long key = ((long)resolution.width << 32) | (uint)resolution.height;
            if (!uniqueResolutions.TryGetValue(key, out Resolution current) ||
                GetRefreshRate(resolution) > GetRefreshRate(current))
            {
                uniqueResolutions[key] = resolution;
            }
        }

        if (uniqueResolutions.Count == 0)
        {
            Resolution current = Screen.currentResolution;
            uniqueResolutions[((long)current.width << 32) | (uint)current.height] = current;
        }

        return uniqueResolutions.Values
            .OrderBy(resolution => resolution.width == 1920 && resolution.height == 1080 ? 0 : 1)
            .ThenByDescending(resolution => resolution.width)
            .ThenByDescending(resolution => resolution.height)
            .ToArray();
    }

    public static void SetMasterVolume(float value)
    {
        EnsureInitialized();
        PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(value));
        AudioListener.volume = Mathf.Clamp01(value);
    }

    public static void SetMusicVolume(float value)
    {
        EnsureInitialized();
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        ApplyMusicVolumeToSources();
    }

    public static void SetBrightness(float value)
    {
        EnsureInitialized();
        PlayerPrefs.SetFloat(BrightnessKey, Mathf.Clamp01(value));
        ApplyBrightness(value);
    }

    public static void SetQuality(int index)
    {
        EnsureInitialized();
        if (index < 0 || index >= QualityOptionCount)
        {
            return;
        }

        PlayerPrefs.SetInt(QualityKey, index);
        QualitySettings.SetQualityLevel(index, true);
    }

    public static void SetResolution(int width, int height)
    {
        EnsureInitialized();
        if (width <= 0 || height <= 0)
        {
            return;
        }

        PlayerPrefs.SetInt(ResolutionWidthKey, width);
        PlayerPrefs.SetInt(ResolutionHeightKey, height);

        RefreshRate refreshRate = Screen.currentResolution.refreshRateRatio;
        foreach (Resolution resolution in GetAvailableResolutions())
        {
            if (resolution.width == width && resolution.height == height)
            {
                refreshRate = resolution.refreshRateRatio;
                break;
            }
        }

        Screen.SetResolution(width, height, Screen.fullScreenMode, refreshRate);
    }

    public static void SaveSettings()
    {
        PlayerPrefs.Save();
    }

    private static void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
        ApplyBrightness(PlayerPrefs.GetFloat(BrightnessKey, 0.5f));

        int availableQualityLevels = QualityOptionCount;
        if (availableQualityLevels > 0)
        {
            int quality = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
            quality = Mathf.Clamp(quality, 0, availableQualityLevels - 1);
            QualitySettings.SetQualityLevel(quality, true);

            if (QualitySettings.names.Length < QualityLabels.Length)
            {
                string missing = string.Join(", ", QualityLabels.Skip(QualitySettings.names.Length));
                Debug.LogWarning("Faltan niveles de calidad por configurar en Unity: " + missing + ".");
            }
        }

        if (PlayerPrefs.HasKey(ResolutionWidthKey) && PlayerPrefs.HasKey(ResolutionHeightKey))
        {
            SetResolution(PlayerPrefs.GetInt(ResolutionWidthKey), PlayerPrefs.GetInt(ResolutionHeightKey));
        }
        else
        {
            Resolution[] resolutions = GetAvailableResolutions();
            Resolution preferred = resolutions.FirstOrDefault(resolution => resolution.width == 1920 && resolution.height == 1080);
            if (preferred.width == 1920 && preferred.height == 1080)
            {
                Screen.SetResolution(preferred.width, preferred.height, Screen.fullScreenMode, preferred.refreshRateRatio);
            }
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyMusicVolumeToSources();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyMusicVolumeToSources();
        ApplyBrightness(PlayerPrefs.GetFloat(BrightnessKey, 0.5f));
    }

    private static void ApplyMusicVolumeToSources()
    {
        foreach (AudioSource source in Object.FindObjectsByType<AudioSource>())
        {
            if (!IsMusicSource(source))
            {
                continue;
            }

            if (!MusicSourceVolumes.TryGetValue(source, out float baseVolume))
            {
                baseVolume = source.volume;
                MusicSourceVolumes[source] = baseVolume;
            }

            source.volume = baseVolume * musicVolume;
        }
    }

    private static void ApplyBrightness(float value)
    {
        value = Mathf.Clamp01(value);
        EnsureBrightnessOverlay();

        if (value < 0.5f)
        {
            brightnessOverlay.color = new Color(0f, 0f, 0f, (0.5f - value) * 1.8f);
        }
        else
        {
            brightnessOverlay.color = new Color(1f, 1f, 1f, (value - 0.5f) * 1.1f);
        }
    }

    private static bool IsMusicSource(AudioSource source)
    {
        if (source.gameObject.name == MusicSourceObjectName)
        {
            return true;
        }

        if (source.clip == null)
        {
            return false;
        }

        string clipName = source.clip.name;
        return clipName.IndexOf(MusicClipKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            clipName.IndexOf("music", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            clipName.IndexOf("musica", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            clipName.IndexOf("soundtrack", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void EnsureBrightnessOverlay()
    {
        if (brightnessOverlay != null)
        {
            return;
        }

        GameObject legacyVolume = GameObject.Find(LegacyBrightnessVolumeName);
        if (legacyVolume != null)
        {
            Object.Destroy(legacyVolume);
        }

        GameObject overlayObject = GameObject.Find(BrightnessObjectName);
        Canvas overlayCanvas;
        if (overlayObject == null)
        {
            overlayObject = new GameObject(BrightnessObjectName, typeof(RectTransform), typeof(Canvas));
            overlayObject.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(overlayObject);
        }

        overlayCanvas = overlayObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = short.MaxValue;

        Transform overlayTransform = overlayObject.transform.Find("Brightness");
        if (overlayTransform == null)
        {
            GameObject imageObject = new GameObject("Brightness", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(overlayObject.transform, false);
            overlayTransform = imageObject.transform;
        }

        RectTransform rectTransform = (RectTransform)overlayTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        brightnessOverlay = overlayTransform.GetComponent<Image>();
        brightnessOverlay.raycastTarget = false;
        brightnessOverlay.color = Color.clear;
    }

    private static float GetRefreshRate(Resolution resolution)
    {
        RefreshRate refreshRate = resolution.refreshRateRatio;
        return refreshRate.denominator == 0 ? 0f : (float)refreshRate.numerator / refreshRate.denominator;
    }
}