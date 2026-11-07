using UnityEngine;
using UnityEngine.InputSystem;
using Trucardo.Core.Interaction;

namespace Trucardo.Core
{
    /// <summary>
    /// Botón situado detrás de la barra para probar la caminata del Bartender hacia la Mesa 9
    /// y la activación del Jefe Final.
    /// Implementa IInteractable para integrarse con el sistema oficial de interacción con la mira ([E] Presionar).
    /// También permite activarlo con la tecla F9 desde cualquier lugar.
    /// </summary>
    public class BartenderTestButton : MonoBehaviour, IInteractable
    {
        [Header("Configuración de Interacción")]
        [SerializeField] private string promptText = "Presionar";
        [SerializeField] private float proximityDistance = 3.5f;

        private bool isPlayerNearby = false;
        private Transform cachedViewerTransform = null;

        // --- Implementación de IInteractable (Muestra "[E] Presionar" en el HUD al mirarlo) ---
        public string PromptText => promptText;

        public bool CanInteract(GameObject interactor) => isActiveAndEnabled;

        public void Interact(GameObject interactor)
        {
            TriggerBartenderBoss();
        }

        private void Awake()
        {
            EnsureColliders();
        }

        private void Start()
        {
            EnsureColliders();
        }

        private void EnsureColliders()
        {
            // Asegurar que cada parte visual (Placa_Base, Pulsador_Rojo, etc.) tenga su propio Collider
            // para que el raycast de la cámara del jugador siempre lo detecte sin importar dónde se mueva.
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r.GetComponent<Collider>() == null)
                {
                    var box = r.gameObject.AddComponent<BoxCollider>();
                    box.isTrigger = false;
                }
            }

            // Si el propio GameObject raíz no tiene Collider, le agregamos uno generoso
            if (GetComponent<Collider>() == null)
            {
                var rootBox = gameObject.AddComponent<BoxCollider>();
                rootBox.size = new Vector3(0.6f, 0.6f, 0.6f);
                rootBox.isTrigger = false;
            }
        }

        private Vector3 GetEffectiveButtonPosition()
        {
            // Si el usuario movió los hijos en el editor, calculamos el centro real de las partes visuales
            if (transform.childCount > 0)
            {
                Vector3 sum = Vector3.zero;
                int count = 0;
                foreach (Transform child in transform)
                {
                    sum += child.position;
                    count++;
                }
                if (count > 0) return sum / count;
            }
            return transform.position;
        }

        private Transform GetPlayerViewer()
        {
            if (cachedViewerTransform != null && cachedViewerTransform.gameObject.activeInHierarchy)
            {
                return cachedViewerTransform;
            }

            // 1. Obtener desde el jugador local en Netcode
            var network = Unity.Netcode.NetworkManager.Singleton;
            if (network != null && network.IsClient && network.LocalClient != null && network.LocalClient.PlayerObject != null)
            {
                var pc = network.LocalClient.PlayerObject.GetComponent<PlayerController>();
                if (pc != null)
                {
                    Camera cam = pc.InteractionCamera;
                    if (cam != null)
                    {
                        cachedViewerTransform = cam.transform;
                        return cachedViewerTransform;
                    }
                    cachedViewerTransform = pc.transform;
                    return cachedViewerTransform;
                }
            }

            // 2. Buscar cualquier PlayerController dueño en la escena
            var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.IsOwner)
                {
                    Camera cam = p.InteractionCamera;
                    if (cam != null)
                    {
                        cachedViewerTransform = cam.transform;
                        return cachedViewerTransform;
                    }
                    cachedViewerTransform = p.transform;
                    return cachedViewerTransform;
                }
            }

            if (players.Length > 0)
            {
                Camera cam = players[0].InteractionCamera;
                if (cam != null)
                {
                    cachedViewerTransform = cam.transform;
                    return cachedViewerTransform;
                }
                cachedViewerTransform = players[0].transform;
                return cachedViewerTransform;
            }

            if (Camera.main != null) return Camera.main.transform;
            return null;
        }

        private void Update()
        {
            // Atajo de teclado directo (F9) para pruebas instantáneas desde cualquier parte
            if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
            {
                TriggerBartenderBoss();
                return;
            }

            Transform viewer = GetPlayerViewer();
            if (viewer == null) return;

            Vector3 buttonPos = GetEffectiveButtonPosition();
            float dist = Vector3.Distance(viewer.position, buttonPos);
            isPlayerNearby = dist <= proximityDistance;

            // Si el jugador está cerca y presiona [E], se activa
            if (isPlayerNearby)
            {
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    TriggerBartenderBoss();
                }
            }
        }

        public void TriggerBartenderBoss()
        {
            Debug.Log("<color=#FF9800><b>[BotonTest] ¡Pulsador activado! Iniciando caminata del Bartender a la Mesa 9...</b></color>");

            var ctrl = BartenderController.Instance ?? FindAnyObjectByType<BartenderController>();
            if (ctrl != null)
            {
                // Si ya estaba en la Mesa 9 o en camino por una prueba anterior,
                // lo reubicamos primero detrás de la barra para reiniciar la prueba limpiamente.
                if (ctrl.CurrentState == BartenderController.State.SeatedAtTable ||
                    ctrl.CurrentState == BartenderController.State.WalkingToTable)
                {
                    ctrl.ResetToBar();
                }

                ctrl.GoToTable9();
            }
            else
            {
                Debug.LogWarning("[BotonTest] No se encontró BartenderController en la escena.");
            }
        }
    }
}
