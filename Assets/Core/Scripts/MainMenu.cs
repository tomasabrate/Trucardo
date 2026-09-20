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

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null) continue;

            switch (label.text.Trim())
            {
                case "Jugar":
                    button.onClick.AddListener(PlayGame);
                    break;
                case "Opciones":
                    button.onClick.AddListener(OpenOptionsMenu);
                    break;
                case "Atras":
                    button.onClick.AddListener(OpenMainMenuPanel);
                    break;
                case "Salir":
                    button.onClick.AddListener(QuitGame);
                    break;
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
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
