using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuOptionsController : MonoBehaviour
{
    private Slider masterVolumeSlider;
    private Slider musicVolumeSlider;
    private Slider brightnessSlider;
    private TMP_Dropdown qualityDropdown;
    private TMP_Dropdown resolutionDropdown;
    private Button backButton;
    private MainMenu mainMenu;

    private void Awake()
    {
        masterVolumeSlider = FindControl<Slider>("SliderVolumenGeneral");
        musicVolumeSlider = FindControl<Slider>("SliderVolumenMusica");
        brightnessSlider = FindControl<Slider>("SliderBrillo");
        qualityDropdown = FindControl<TMP_Dropdown>("DropdownCalidadImagen");
        resolutionDropdown = FindControl<TMP_Dropdown>("DropdownResolucion");
        backButton = FindBackButton();
        mainMenu = Object.FindAnyObjectByType<MainMenu>(FindObjectsInactive.Include);

        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(GameSettingsManager.SetMasterVolume);
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(GameSettingsManager.SetMusicVolume);
        if (brightnessSlider != null) brightnessSlider.onValueChanged.AddListener(GameSettingsManager.SetBrightness);
        if (qualityDropdown != null) qualityDropdown.onValueChanged.AddListener(GameSettingsManager.SetQuality);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (backButton != null) backButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void OnEnable()
    {
        InitializeControls();
    }

    private void OnDestroy()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(GameSettingsManager.SetMasterVolume);
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveListener(GameSettingsManager.SetMusicVolume);
        if (brightnessSlider != null) brightnessSlider.onValueChanged.RemoveListener(GameSettingsManager.SetBrightness);
        if (qualityDropdown != null) qualityDropdown.onValueChanged.RemoveListener(GameSettingsManager.SetQuality);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        if (backButton != null) backButton.onClick.RemoveListener(ReturnToMainMenu);
    }

    private void InitializeControls()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.SetValueWithoutNotify(Mathf.Clamp01(PlayerPrefs.GetFloat("Settings_MasterVolume", 1f)));
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.SetValueWithoutNotify(Mathf.Clamp01(PlayerPrefs.GetFloat("Settings_MusicVolume", 1f)));
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = 0f;
            brightnessSlider.maxValue = 1f;
            brightnessSlider.SetValueWithoutNotify(Mathf.Clamp01(PlayerPrefs.GetFloat("Settings_Brightness", 0.5f)));
        }

        InitializeQualityDropdown();
        InitializeResolutionDropdown();
    }

    private void InitializeQualityDropdown()
    {
        if (qualityDropdown == null)
        {
            return;
        }

        int count = GameSettingsManager.QualityOptionCount;
        List<string> labels = new List<string>(count);
        for (int index = 0; index < count; index++)
        {
            labels.Add(GameSettingsManager.GetQualityLabel(index));
        }

        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(labels);
        qualityDropdown.interactable = count > 0;
        if (count > 0)
        {
            qualityDropdown.SetValueWithoutNotify(Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, count - 1));
            qualityDropdown.RefreshShownValue();
        }
    }

    private void InitializeResolutionDropdown()
    {
        if (resolutionDropdown == null)
        {
            return;
        }

        Resolution[] resolutions = GameSettingsManager.GetAvailableResolutions();
        List<string> labels = new List<string>(resolutions.Length);
        int selectedIndex = 0;
        for (int index = 0; index < resolutions.Length; index++)
        {
            Resolution resolution = resolutions[index];
            labels.Add(resolution.width + " x " + resolution.height);
            if (resolution.width == Screen.width && resolution.height == Screen.height)
            {
                selectedIndex = index;
            }
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(labels);
        resolutionDropdown.interactable = resolutions.Length > 0;
        if (resolutions.Length > 0)
        {
            resolutionDropdown.SetValueWithoutNotify(selectedIndex);
            resolutionDropdown.RefreshShownValue();
        }
    }

    private void OnResolutionChanged(int index)
    {
        Resolution[] resolutions = GameSettingsManager.GetAvailableResolutions();
        if (index >= 0 && index < resolutions.Length)
        {
            GameSettingsManager.SetResolution(resolutions[index].width, resolutions[index].height);
        }
    }

    private void ReturnToMainMenu()
    {
        GameSettingsManager.SaveSettings();
        if (mainMenu != null)
        {
            mainMenu.OpenMainMenuPanel();
        }
    }

    private T FindControl<T>(string controlName) where T : Component
    {
        foreach (T component in GetComponentsInChildren<T>(true))
        {
            if (component.gameObject.name == controlName)
            {
                return component;
            }
        }

        return null;
    }

    private Button FindBackButton()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button.gameObject.name == "VolverAtrasMenuPrincipal")
            {
                return button;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null && (label.text.Trim() == "Atras" || label.text.Trim() == "Atrás"))
            {
                return button;
            }
        }

        Transform existingButton = transform.Find("Button (3)");
        return existingButton != null ? existingButton.GetComponent<Button>() : null;
    }
}