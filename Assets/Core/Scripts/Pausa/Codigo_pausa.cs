using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class Codigo_pausa : MonoBehaviour
{
    public GameObject ObjetoMenuPausa;
    public bool Pausa = false;

    private GameObject grupoMenuOpciones;
    private GameObject grupoConfirmarMenuPrincipal;
    private GameObject grupoConfirmarSalirJuego;
    private CursorLockMode estadoCursorAnterior;
    private bool cursorVisibleAnterior;

    private void Awake()
    {
        if (ObjetoMenuPausa == null)
        {
            Transform menuPausa = transform.Find("GrupoMenuPausa");
            if (menuPausa != null) ObjetoMenuPausa = menuPausa.gameObject;
        }

        grupoMenuOpciones = BuscarGrupo("GrupoMenuOpciones");
        grupoConfirmarMenuPrincipal = BuscarGrupo("GrupoConfirmarMenuPrincipal");
        grupoConfirmarSalirJuego = BuscarGrupo("GrupoConfirmarSalirJuego");

        PrepararBotones(ObjetoMenuPausa);
        PrepararBotones(grupoMenuOpciones);
        PrepararBotones(grupoConfirmarMenuPrincipal);
        PrepararBotones(grupoConfirmarSalirJuego);
        DesactivarRaycastDeTexto(ObjetoMenuPausa);
        DesactivarRaycastDeTexto(grupoMenuOpciones);
        DesactivarRaycastDeTexto(grupoConfirmarMenuPrincipal);
        DesactivarRaycastDeTexto(grupoConfirmarSalirJuego);

        EnlazarBoton(ObjetoMenuPausa, "BottonReanudar", Reanudar);
        EnlazarBoton(ObjetoMenuPausa, "BottonOpciones", AbrirOpciones);
        EnlazarBoton(ObjetoMenuPausa, "BottonMenuPrincipal", ConfirmarMenuPrincipal);
        EnlazarBoton(ObjetoMenuPausa, "BottonSalirDelJuego", ConfirmarSalirJuego);
        EnlazarBoton(grupoMenuOpciones, "BottonVolverMenuPausa", VolverAlMenuPausa);
        EnlazarBoton(grupoConfirmarMenuPrincipal, "BottonVolverMenuPausa", VolverAlMenuPausa);
        EnlazarBoton(grupoConfirmarMenuPrincipal, "BottonConfirmarMenu", IrAlMenuPrincipal);
        EnlazarBoton(grupoConfirmarSalirJuego, "BottonVolverMenuPausa", VolverAlMenuPausa);
        EnlazarBoton(grupoConfirmarSalirJuego, "BottonConfirmarSalir", SalirDelJuego);

        Pausa = false;
        Time.timeScale = 1f;
        MostrarSolo(null);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {   
            if (Pausa) Reanudar();
            else AbrirPausa();
        }
    }

    private GameObject BuscarGrupo(string nombre)
    {
        Transform grupo = transform.Find(nombre);
        return grupo != null ? grupo.gameObject : null;
    }

    private void EnlazarBoton(GameObject grupo, string nombre, UnityAction accion)
    {
        if (grupo == null) return;

        Transform objetoBoton = grupo.transform.Find(nombre);
        Button boton = objetoBoton != null ? objetoBoton.GetComponent<Button>() : null;
        if (boton != null) boton.onClick.AddListener(accion);
    }

    private void PrepararBotones(GameObject grupo)
    {
        if (grupo == null) return;

        foreach (Button boton in grupo.GetComponentsInChildren<Button>(true))
        {
            Graphic grafico = boton.GetComponent<Graphic>();
            if (grafico != null) boton.targetGraphic = grafico;

            ColorBlock colores = boton.colors;
            colores.highlightedColor = colores.selectedColor;
            boton.colors = colores;
        }
    }

    private void DesactivarRaycastDeTexto(GameObject grupo)
    {
        if (grupo == null) return;

        foreach (TMP_Text texto in grupo.GetComponentsInChildren<TMP_Text>(true))
        {
            texto.raycastTarget = false;
        }
    }

    private void MostrarSolo(GameObject grupoVisible)
    {
        if (ObjetoMenuPausa != null) ObjetoMenuPausa.SetActive(ObjetoMenuPausa == grupoVisible);
        if (grupoMenuOpciones != null) grupoMenuOpciones.SetActive(grupoMenuOpciones == grupoVisible);
        if (grupoConfirmarMenuPrincipal != null) grupoConfirmarMenuPrincipal.SetActive(grupoConfirmarMenuPrincipal == grupoVisible);
        if (grupoConfirmarSalirJuego != null) grupoConfirmarSalirJuego.SetActive(grupoConfirmarSalirJuego == grupoVisible);
    }

    public void AbrirPausa()
    {
        if (Pausa) return;

        estadoCursorAnterior = Cursor.lockState;
        cursorVisibleAnterior = Cursor.visible;
        Pausa = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        MostrarSolo(ObjetoMenuPausa);
    }

    public void Reanudar()
    {
        Pausa = false;
        Time.timeScale = 1f;
        MostrarSolo(null);
        Cursor.lockState = estadoCursorAnterior;
        Cursor.visible = cursorVisibleAnterior;
    }

    public void AbrirOpciones()
    {
        MostrarSolo(grupoMenuOpciones);
    }

    public void ConfirmarMenuPrincipal()
    {
        MostrarSolo(grupoConfirmarMenuPrincipal);
    }

    public void ConfirmarSalirJuego()
    {
        MostrarSolo(grupoConfirmarSalirJuego);
    }

    public void VolverAlMenuPausa()
    {
        MostrarSolo(ObjetoMenuPausa);
    }

    public void IrAlMenuPrincipal()
    {
        Time.timeScale = 1f;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        Trucardo.Core.NetworkUI.selectedMode = Trucardo.Core.GameMode.None;
        SceneManager.LoadScene("MainMenu");
    }

    public void SalirDelJuego()
    {
        Time.timeScale = 1f;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
