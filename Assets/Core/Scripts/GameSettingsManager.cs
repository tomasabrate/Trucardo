using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GameSettingsManager : MonoBehaviour
{
    private const string MasterVolumeKey = "Settings_MasterVolume";
    private const string MusicVolumeKey = "Settings_MusicVolume";
    private const string BrightnessKey = "Settings_Brightness";
    private const string QualityKey = "Settings_Quality";
    private const string ResolutionWidthKey = "Settings_ResolutionWidth";
    private const string ResolutionHeightKey = "Settings_ResolutionHeight";
    private const string BrightnessObjectName = "SettingsBrightnessVolume";
    private const string MusicSourceObjectName = "MusicaBar";

    private static readonly string[] QualityLabels =
    {
        "Muy bajo", "Bajo", "Medio", "Alto", "Muy alto", "Ultra"
    };

    private static readonly Dictionary<AudioSource, float> MusicSourceVolumes = new Dictionary<AudioSource, float>();
    private static bool initialized;
    private static float musicVolume = 1f;
    private static Volume brightnessVolume;
    private static ColorAdjustments brightnessAdjustments;
    private static VolumeProfile brightnessProfile;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        initialized = false;
        musicVolume = 1f;
        brightnessVolume = null;
        brightnessAdjustments = null;
        brightnessProfile = null;
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
            if (source.gameObject.name != MusicSourceObjectName)
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
        EnsureBrightnessVolume();
        brightnessAdjustments.postExposure.value = (value - 0.5f) * 4f;
    }

    private static void EnsureBrightnessVolume()
    {
        if (brightnessVolume != null && brightnessAdjustments != null)
        {
            return;
        }

        GameObject volumeObject = GameObject.Find(BrightnessObjectName);
        if (volumeObject == null)
        {
            volumeObject = new GameObject(BrightnessObjectName);
            volumeObject.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(volumeObject);
        }

        brightnessVolume = volumeObject.GetComponent<Volume>();
        if (brightnessVolume == null)
        {
            brightnessVolume = volumeObject.AddComponent<Volume>();
        }

        brightnessVolume.isGlobal = true;
        brightnessVolume.priority = 1000f;
        brightnessVolume.weight = 1f;

        brightnessProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        brightnessProfile.hideFlags = HideFlags.HideAndDontSave;
        brightnessAdjustments = brightnessProfile.Add<ColorAdjustments>();
        brightnessAdjustments.postExposure.overrideState = true;
        brightnessVolume.sharedProfile = brightnessProfile;
    }

    private static float GetRefreshRate(Resolution resolution)
    {
        RefreshRate refreshRate = resolution.refreshRateRatio;
        return refreshRate.denominator == 0 ? 0f : (float)refreshRate.numerator / refreshRate.denominator;
    }
}