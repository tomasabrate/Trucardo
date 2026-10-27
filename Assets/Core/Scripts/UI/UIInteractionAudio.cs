using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class UIInteractionAudio : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, ISubmitHandler
{
    private const float SliderSoundInterval = 0.1f;
    private const float UiSoundVolume = 0.3f;
    private const string OpenButtonResource = "Sonidos/BotonUI/Open_button_wood";
    private const string CloseButtonResource = "Sonidos/BotonUI/CloseButtonWood";
    private const string SliderResource = "Sonidos/BotonUI/SliderButton";
    private const string DropdownResource = "Sonidos/BotonUI/litupsubway-ui-hover-sfx-513360";
    private const string ButtonHoverResource = "Sonidos/BotonUI/SelecctionMenu";

    private static AudioClip openButtonClip;
    private static AudioClip closeButtonClip;
    private static AudioClip sliderClip;
    private static AudioClip dropdownClip;
    private static AudioClip buttonHoverClip;
    private static AudioSource audioSource;
    private static bool clipsLoaded;

    private Button button;
    private Slider slider;
    private TMP_Dropdown dropdown;
    private float lastSliderSoundValue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttachToLoadedSceneControls();
    }

    private void Awake()
    {
        button = GetComponent<Button>();
        slider = GetComponent<Slider>();
        dropdown = GetComponent<TMP_Dropdown>();
    }

    private void OnEnable()
    {
        if (slider != null)
        {
            lastSliderSoundValue = slider.value;
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        if (dropdown != null)
        {
            dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }
    }

    private void OnDisable()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }

        if (dropdown != null)
        {
            dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button != null && eventData.button == PointerEventData.InputButton.Left && button.IsInteractable())
        {
            PlayButton();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && button.IsInteractable())
        {
            Play(UISound.ButtonHover);
        }
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (button != null && button.IsInteractable())
        {
            PlayButton();
        }
    }

    private void OnSliderValueChanged(float value)
    {
        float range = Mathf.Abs(slider.maxValue - slider.minValue);
        float significantChange = Mathf.Max(range * SliderSoundInterval, 0.0001f);
        if (Mathf.Abs(value - lastSliderSoundValue) + 0.0001f < significantChange)
        {
            return;
        }

        lastSliderSoundValue = value;
        Play(UISound.Slider);
    }

    private void OnDropdownValueChanged(int value)
    {
        Play(UISound.Dropdown);
    }

    private void PlayButton()
    {
        Play(IsCloseAction() ? UISound.ButtonClose : UISound.ButtonOpen);
    }

    private bool IsCloseAction()
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        string action = button.gameObject.name.ToLowerInvariant();
        if (label != null)
        {
            action += " " + label.text.Trim().ToLowerInvariant();
        }

        return action.Contains("back") ||
               action.Contains("exit") ||
               action.Contains("quit") ||
               action.Contains("close") ||
               action.Contains("cancel") ||
               action.Contains("volver") ||
               action.Contains("atrás") ||
               action.Contains("atras") ||
               action.Contains("salir") ||
               action.Contains("cerrar") ||
               action.Contains("menuprincipal") ||
               action.Contains("menu principal") ||
               action.Contains("menú principal") ||
               action.Contains("mainmenu") ||
               action.Contains("main menu");
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToLoadedSceneControls();
    }

    private static void AttachToLoadedSceneControls()
    {
        AttachToObjects(Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        AttachToObjects(Object.FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        AttachToObjects(Object.FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include, FindObjectsSortMode.None));
    }

    private static void AttachToObjects<T>(T[] controls) where T : Component
    {
        foreach (T control in controls)
        {
            if (control.GetComponent<UIInteractionAudio>() == null)
            {
                control.gameObject.AddComponent<UIInteractionAudio>();
            }
        }
    }

    private static void Play(UISound sound)
    {
        LoadClips();
        AudioClip clip = sound == UISound.ButtonOpen ? openButtonClip :
            sound == UISound.ButtonClose ? closeButtonClip :
            sound == UISound.Slider ? sliderClip :
            sound == UISound.Dropdown ? dropdownClip : buttonHoverClip;
        if (clip == null)
        {
            return;
        }

        EnsureAudioSource();
        audioSource.PlayOneShot(clip);
    }

    private static void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            GameObject audioObject = new GameObject("UI Interaction Audio");
            Object.DontDestroyOnLoad(audioObject);
            audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = UiSoundVolume;
        }
    }

    private enum UISound
    {
        ButtonOpen,
        ButtonClose,
        Slider,
        Dropdown,
        ButtonHover
    }

    private static void LoadClips()
    {
        if (clipsLoaded)
        {
            return;
        }

        clipsLoaded = true;
        openButtonClip = LoadClip(OpenButtonResource);
        closeButtonClip = LoadClip(CloseButtonResource);
        sliderClip = LoadClip(SliderResource);
        dropdownClip = LoadClip(DropdownResource);
        buttonHoverClip = LoadClip(ButtonHoverResource);
    }

    private static AudioClip LoadClip(string resourcePath)
    {
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null)
        {
            Debug.LogError("No se encontró el sonido de UI en Resources/" + resourcePath + ".");
        }

        return clip;
    }
}
