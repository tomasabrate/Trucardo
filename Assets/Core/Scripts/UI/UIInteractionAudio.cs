using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class UIInteractionAudio : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, ISubmitHandler
{
    private const float SliderSoundInterval = 0.1f;
    private const float UiSoundVolume = 0.3f;
    // Ganancia adicional para las grabaciones de victoria y derrota, que vienen bajas.
    private const float EndGameSoundGain = 2.5f;
    private const string OpenButtonResource = "Sonidos/BotonUI/Open_button_wood";
    private const string CloseButtonResource = "Sonidos/BotonUI/CloseButtonWood";
    private const string SliderResource = "Sonidos/BotonUI/SliderButton";
    private const string DropdownResource = "Sonidos/BotonUI/litupsubway-ui-hover-sfx-513360";
    private const string ButtonHoverResource = "Sonidos/BotonUI/SelecctionMenu";
    private const string VictoryResource = "Sonidos/Efectos/Victoria";
    private const string DefeatResource = "Sonidos/Efectos/Derrota";

    private static AudioClip openButtonClip;
    private static AudioClip closeButtonClip;
    private static AudioClip sliderClip;
    private static AudioClip dropdownClip;
    private static AudioClip buttonHoverClip;
    private static AudioClip victoryClip;
    private static AudioClip defeatClip;
    private static AudioSource audioSource;
    private static AudioSource endGameAudioSource;
    private static bool clipsLoaded;
    private static bool endGameAudioActive;
    private static bool previousListenerPause;

    private Button button;
    private Slider slider;
    private TMP_Dropdown dropdown;
    private float lastSliderSoundValue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        SetEndGameAudioActive(false);
        clipsLoaded = false;
        openButtonClip = null;
        closeButtonClip = null;
        sliderClip = null;
        dropdownClip = null;
        buttonHoverClip = null;
        victoryClip = null;
        defeatClip = null;
        if (audioSource != null) audioSource.Stop();
        if (endGameAudioSource != null) endGameAudioSource.Stop();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadClips();
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

    public static bool PlayVictory()
    {
        return Play(UISound.Victory);
    }

    public static bool PlayDefeat()
    {
        return Play(UISound.Defeat);
    }

    public static void SetEndGameAudioActive(bool active)
    {
        if (endGameAudioActive == active)
            return;

        endGameAudioActive = active;
        if (active)
        {
            previousListenerPause = AudioListener.pause;
            if (audioSource != null) audioSource.Stop();
            AudioListener.pause = true;
        }
        else
        {
            AudioListener.pause = previousListenerPause;
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

    private static bool Play(UISound sound)
    {
        bool isEndGameSound = sound == UISound.Victory || sound == UISound.Defeat;
        if (endGameAudioActive && !isEndGameSound)
            return false;

        LoadClips();
        AudioClip clip = sound switch
        {
            UISound.ButtonOpen => openButtonClip,
            UISound.ButtonClose => closeButtonClip,
            UISound.Slider => sliderClip,
            UISound.Dropdown => dropdownClip,
            UISound.ButtonHover => buttonHoverClip,
            UISound.Victory => victoryClip,
            UISound.Defeat => defeatClip,
            _ => null
        };
        if (clip == null)
        {
            return false;
        }

        if (clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();
        if (clip.loadState != AudioDataLoadState.Loaded)
            return false;

        if (isEndGameSound)
        {
            EnsureEndGameAudioSource();
            endGameAudioSource.Stop();
            endGameAudioSource.PlayOneShot(clip, EndGameSoundGain);
            return true;
        }

        EnsureAudioSource();
        audioSource.PlayOneShot(clip);
        return true;
    }

    private static void EnsureEndGameAudioSource()
    {
        if (endGameAudioSource != null)
            return;

        GameObject audioObject = new GameObject("End Game Audio");
        Object.DontDestroyOnLoad(audioObject);
        endGameAudioSource = audioObject.AddComponent<AudioSource>();
        endGameAudioSource.playOnAwake = false;
        endGameAudioSource.loop = false;
        endGameAudioSource.spatialBlend = 0f;
        endGameAudioSource.ignoreListenerPause = true;
        endGameAudioSource.priority = 0;
        endGameAudioSource.volume = 1f;
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
        ButtonHover,
        Victory,
        Defeat
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
        victoryClip = LoadClip(VictoryResource);
        defeatClip = LoadClip(DefeatResource);
    }

    private static AudioClip LoadClip(string resourcePath)
    {
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null)
        {
            Debug.LogError("No se encontró el sonido de UI en Resources/" + resourcePath + ".");
        }
        else if (clip.loadState == AudioDataLoadState.Unloaded && !clip.LoadAudioData())
        {
            Debug.LogError("No se pudieron cargar los datos de audio de Resources/" + resourcePath + ".");
        }

        return clip;
    }
}
