using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    public GameObject optionsMenu;
    public GameObject mainMenu;

    private Button btnJugar;
    private Button btnMultijugador;
    private Button btnOpciones;
    private Button btnSalir;
    private Button btnAtras; // the one in options menu

    private TMP_Text txtJugar;
    private TMP_Text txtMultijugador;
    private TMP_Text txtOpciones;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (optionsMenu != null) optionsMenu.SetActive(false);
        if (mainMenu != null) mainMenu.SetActive(true);

        if (EventSystem.current == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        if (mainMenu != null)
        {
            foreach (Button button in mainMenu.GetComponentsInChildren<Button>(true))
            {
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label == null) continue;

                string text = label.text.Trim();
                switch (text)
                {
                    case "Jugar":
                    case "Individual":
                        if (btnJugar == null || (button.gameObject.activeInHierarchy && !btnJugar.gameObject.activeInHierarchy)) 
                        {
                            if (btnJugar != null) btnJugar.onClick.RemoveAllListeners();
                            btnJugar = button;
                            txtJugar = label;
                            button.onClick.AddListener(PlaySingleplayer);
                        }
                        break;
                    case "Multijugador":
                        if (btnMultijugador == null || (button.gameObject.activeInHierarchy && !btnMultijugador.gameObject.activeInHierarchy))
                        {
                            if (btnMultijugador != null) btnMultijugador.onClick.RemoveAllListeners();
                            btnMultijugador = button;
                            txtMultijugador = label;
                            button.onClick.AddListener(OpenMultiplayerMenu);
                        }
                        break;
                    case "Opciones":
                        if (btnOpciones == null || (button.gameObject.activeInHierarchy && !btnOpciones.gameObject.activeInHierarchy))
                        {
                            if (btnOpciones != null) btnOpciones.onClick.RemoveAllListeners();
                            btnOpciones = button;
                            txtOpciones = label;
                            button.onClick.AddListener(OpenOptionsMenu);
                        }
                        break;
                    case "Salir":
                        if (btnSalir == null || (button.gameObject.activeInHierarchy && !btnSalir.gameObject.activeInHierarchy))
                        {
                            if (btnSalir != null) btnSalir.onClick.RemoveAllListeners();
                            btnSalir = button;
                            button.onClick.AddListener(QuitGame);
                        }
                        break;
                }
            }
        }

        if (optionsMenu != null)
        {
            foreach (Button button in optionsMenu.GetComponentsInChildren<Button>(true))
            {
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label == null) continue;

                string text = label.text.Trim();
                if (text == "Atras")
                {
                    if (btnAtras == null)
                    {
                        btnAtras = button;
                        button.onClick.AddListener(OpenMainMenuPanel);
                    }
                }
            }
        }
    }

    public void OpenOptionsMenu()
    {
        optionsMenu.SetActive(true);
        mainMenu.SetActive(false);
    }

    public void OpenMainMenuPanel()
    {
        optionsMenu.SetActive(false);
        mainMenu.SetActive(true);
        BackToMainMenu();
    }

    public void OpenMultiplayerMenu()
    {
        // Cambiar la funcionalidad y texto de los botones para que sean Host/Invitado
        if (btnJugar != null)
        {
            txtJugar.text = "Host";
            btnJugar.onClick.RemoveAllListeners();
            btnJugar.onClick.AddListener(PlayHost);
        }

        if (btnMultijugador != null)
        {
            txtMultijugador.text = "Invitado";
            btnMultijugador.onClick.RemoveAllListeners();
            btnMultijugador.onClick.AddListener(PlayClient);
        }

        if (btnOpciones != null)
        {
            txtOpciones.text = "Atras";
            btnOpciones.onClick.RemoveAllListeners();
            btnOpciones.onClick.AddListener(BackToMainMenu);
        }

        if (btnSalir != null)
        {
            btnSalir.gameObject.SetActive(false);
        }
    }

    public void BackToMainMenu()
    {
        // Restaurar botones principales
        if (btnJugar != null)
        {
            txtJugar.text = "Individual"; // O "Jugar" dependiendo de cómo estaba en la escena
            btnJugar.onClick.RemoveAllListeners();
            btnJugar.onClick.AddListener(PlaySingleplayer);
        }

        if (btnMultijugador != null)
        {
            txtMultijugador.text = "Multijugador";
            btnMultijugador.onClick.RemoveAllListeners();
            btnMultijugador.onClick.AddListener(OpenMultiplayerMenu);
        }

        if (btnOpciones != null)
        {
            txtOpciones.text = "Opciones";
            btnOpciones.onClick.RemoveAllListeners();
            btnOpciones.onClick.AddListener(OpenOptionsMenu);
        }

        if (btnSalir != null)
        {
            btnSalir.gameObject.SetActive(true);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void PlaySingleplayer()
    {
        Trucardo.Core.NetworkUI.selectedMode = Trucardo.Core.GameMode.Singleplayer;
        SceneManager.LoadScene("SampleScene");
    }

    public void PlayHost()
    {
        Trucardo.Core.NetworkUI.selectedMode = Trucardo.Core.GameMode.Host;
        SceneManager.LoadScene("SampleScene");
    }

    public void PlayClient()
    {
        Trucardo.Core.NetworkUI.selectedMode = Trucardo.Core.GameMode.Client;
        SceneManager.LoadScene("SampleScene");
    }
}
