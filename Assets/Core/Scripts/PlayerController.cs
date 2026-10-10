using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Trucardo.Core.Rules;

namespace Trucardo.Core
{
    /// <summary>
    /// Controlador del jugador (Cliente). 
    /// Recibe el input (teclado/mouse) y lo env�a como comandos al servidor.
    /// Solo interact�a si IsOwner es verdadero (es decir, es la computadora local del jugador).
    /// </summary>
    public class PlayerController : NetworkBehaviour
    {
        [Header("Referencias Visuales")]
        public Transform cameraTransform;
        public GameObject[] localMeshesToHide;

        [Header("Movimiento")]
        public float walkSpeed = 5f;
        public float lookSensitivity = 0.5f;

        private CharacterController characterController;
        private float verticalLookRotation = 0f;
        private float horizontalHeadRotation = 0f;
        public NetworkVariable<float> headPitch = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<float> headYaw = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<float> netSpeed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<bool> netIsSeated = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private Transform headTransform;
        private Animator animator;
                private bool isSeated = false;
        private Transform currentTable = null;
                private TableDefinition currentTableDefinition = null;
        private SeatInteractable currentSeatInteractable = null;

        [Header("Referencias a Prefabs")]
        public GameObject cardPrefab;
        public PowerCatalog powerCatalog;

        private int[] myCards = new int[3] { -1, -1, -1 };
        private GameObject[] localCardInstances = new GameObject[3];
        private HandVisualizer handVisualizer;
        private HandVisualizer opponentHandVisualizer;
        private PowerHandVisualizer powerHandVisualizer;
        private PowerSystem powerSystem;
        private readonly List<GameObject> localPowerInstances = new List<GameObject>();
        
        // Estado local para la UI
        private GameState localGameState;
        private ulong localTurnId;
        private int localP1Score;
        private int localP2Score;
        private TipoCanto localCantoPendiente;
        private EstadoEnvido localEstadoEnvido;
        private int localEnvidoAcceptedPoints;
        private int localPendingEnvidoPoints;
        private ulong localLastCallerId;
        private GameServer serverInstance;

        private Camera firstPersonCamera;
        private Camera thirdPersonCamera;
        public bool isFirstPersonView = true;

        public bool ModalInputBlocked { get; set; }
        private int suppressedInputFrame = -1;
        public bool GameplayInputBlocked => ModalInputBlocked || suppressedInputFrame == Time.frameCount;
        public Camera InteractionCamera => thirdPersonCamera != null && thirdPersonCamera.enabled
            ? thirdPersonCamera : firstPersonCamera;

        public void SuppressGameplayInputThisFrame() => suppressedInputFrame = Time.frameCount;

        // Called only by the Interact router when no physical object consumed the press.
        public void HandleContextualInteract()
        {
            if (!IsOwner || !IsSpawned || GameplayInputBlocked || Time.timeScale <= 0f) return;
            if (isSeated) CallEnvido(EnvidoCall.Envido);
            else TrySitDown();
        }

        public override void OnNetworkSpawn()
        {
            TableDefinition.EnsureSceneConfiguration();
            characterController = GetComponent<CharacterController>();

            AudioListener audioListener = GetComponent<AudioListener>();
            if (audioListener != null)
            {
                audioListener.enabled = IsOwner;
            }
            
            // Fix culling issues (mesh disappearing when looking away from its original bounds)
            // Esto arregla que el sombrero/cabeza del oponente desaparezca al mirar hacia abajo
            SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in smrs)
            {
                smr.updateWhenOffscreen = true;
                // Tambien ampliamos un poco los limites por si acaso
                smr.localBounds = new UnityEngine.Bounds(smr.localBounds.center, smr.localBounds.size * 2f);
            }
            
            MeshRenderer[] mrs = GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in mrs)
            {
                // Ampliamos los limites de los MeshRenderer estaticos tambien
                mr.localBounds = new UnityEngine.Bounds(mr.localBounds.center, mr.localBounds.size * 2f);
            }
            
            Transform visuals = transform.Find("Visuals");
            if (visuals != null)
            {
                animator = visuals.GetComponentInChildren<Animator>();
            }
            else
            {
                animator = GetComponentInChildren<Animator>();
            }

            headTransform = transform.Find("Cabeza");
            
            if (cameraTransform != null)
                cameraTransform.localEulerAngles = Vector3.zero;

            if (IsOwner)
            {
                // Crear cámara de 3ra persona para debug
                GameObject tpCamObj = new GameObject("ThirdPersonCamera");
                tpCamObj.transform.SetParent(transform);
                tpCamObj.transform.localPosition = new Vector3(2f, 2.5f, -2.5f); // Un poco a la derecha, arriba y atras
                tpCamObj.transform.localRotation = Quaternion.Euler(20f, -35f, 0f); // Mirando hacia el jugador
                thirdPersonCamera = tpCamObj.AddComponent<Camera>();
                thirdPersonCamera.enabled = false;
                // Mover al punto de aparición (Host a Spawn1, Cliente a Spawn2)
                GameObject spawn = IsServer ? GameObject.Find("SpawnPoint1") : GameObject.Find("SpawnPoint2");
                if (spawn != null)
                {
                    if (characterController != null) characterController.enabled = false;
                    transform.position = spawn.transform.position;
                    transform.LookAt(new Vector3(0, transform.position.y, 0)); // Mirar a la mesa
                    if (characterController != null) characterController.enabled = true;
                }

                foreach (var mesh in localMeshesToHide)
                {
                    if (mesh != null)
                    {
                        var rend = mesh.GetComponent<Renderer>();
                        if (rend != null) rend.enabled = false;
                    }
                }
                
                if (cameraTransform != null)
                {
                    cameraTransform.gameObject.SetActive(true);
                    firstPersonCamera = cameraTransform.GetComponent<Camera>();
                }

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                if (cameraTransform != null) cameraTransform.gameObject.SetActive(false);
            }

            // Configuramos los renderers visuales para el jugador local
            if (IsOwner)
            {
                SetupLocalPlayerVisuals();
                
                isFirstPersonView = true;
                if (firstPersonCamera != null)
                {
                    firstPersonCamera.gameObject.SetActive(true);
                    firstPersonCamera.enabled = true;
                }
                if (thirdPersonCamera != null)
                {
                    thirdPersonCamera.enabled = false;
                }
            }
        }

        private void SetupLocalPlayerVisuals()
        {
            // Ocultar la cabeza del jugador local, pero mantener la sombra (Cast Shadows: Shadows Only)
            // para que no obstruya el frustum de la cámara en primera persona.
            // Intento buscar la cabeza o sombrero recursivamente con nombres flexibles
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            bool headHidden = false;
            foreach (var r in renderers)
            {
                string n = r.gameObject.name.ToLower();
                if (n.Contains("head") || n.Contains("cabeza") || n.Contains("cebeza") || n.Contains("sombrero") || n.Contains("hat") || n.Contains("gorro"))
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                    headHidden = true;
                }
            }

            // Fallback si resulta que exportaron todo junto en una sola malla
            if (!headHidden && renderers.Length == 1)
            {
                // Si hay 1 sola malla, no podemos ocultar la cabeza sin ocultar el cuerpo.
                // Lo dejamos as�, el problema de ver el sombrero se mitigar� ajustando la cámara.
            }

            // Ocultar placeholders antiguos en caso de que existan en el array manual
            foreach (var meshObj in localMeshesToHide)
            {
                if (meshObj != null) 
                {
                    Renderer mr = meshObj.GetComponent<Renderer>();
                    if (mr != null) mr.enabled = false;
                }
            }

            if (firstPersonCamera == null)
            {
                Transform fpCamTrans = transform.Find("Cabeza/FirstPersonCamera");
                if (fpCamTrans != null) firstPersonCamera = fpCamTrans.GetComponent<Camera>();
            }
            if (firstPersonCamera == null) 
            {
                Camera[] cameras = GetComponentsInChildren<Camera>();
                foreach (var cam in cameras)
                {
                    if (cam != thirdPersonCamera)
                    {
                        firstPersonCamera = cam;
                        break;
                    }
                }
            }
        }

        public void ReceiveCards(int[] cards)
        {
            if (cards == null || cards.Length < 3)
            {
                Debug.LogError("[Local Player] No se recibieron tres cartas válidas.");
                return;
            }

            myCards = cards;
            Debug.Log($"[Local Player] Cartas recibidas ({cards.Length}). Instanciando 3D...");

                        for (int i = 0; i < 3; i++)
            {
                if (localCardInstances[i] != null) Destroy(localCardInstances[i]);
            }
            if (opponentHandVisualizer != null)
            {
                foreach (var card in opponentHandVisualizer.cardsInHand)
                {
                    if (card != null) Destroy(card.gameObject);
                }
                opponentHandVisualizer.cardsInHand.Clear();
            }

            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance == null)
            {
                Debug.LogError("[Local Player] No se encontró GameServer para obtener el mazo.");
                return;
            }
            
            DeckData deck = serverInstance.masterDeck;
            
            if (cardPrefab == null)
            {
                Debug.LogError("[Local Player] cardPrefab no está asignado.");
                return;
            }

            if (deck == null || deck.AvailableCards == null)
            {
                Debug.LogError("[Local Player] GameServer no tiene un mazo válido asignado.");
                return;
            }

            if (deck.AvailableCards.Count == 0)
            {
                Debug.LogError("[Local Player] El mazo no contiene cartas.");
                return;
            }

            if (cardPrefab != null && deck != null)
            {
                if (handVisualizer == null)
                {
                    handVisualizer = gameObject.AddComponent<HandVisualizer>();
                }
                
                                
                                handVisualizer.tableCenter = currentTable;
                handVisualizer.isOpponent = false;
                handVisualizer.sideSpacing = 0.2f; 
                handVisualizer.heightOffset = 0.82f;
                handVisualizer.forwardDistance = 1.0f;
                handVisualizer.lateralOffset = 0f;
                handVisualizer.tableEdgeDistance = 0.61f;
                handVisualizer.tiltAngle = -35f;
                
                handVisualizer.cardsInHand.Clear();

                if (opponentHandVisualizer == null)
                {
                    opponentHandVisualizer = gameObject.AddComponent<HandVisualizer>();
                }
                
                opponentHandVisualizer.tableCenter = currentTable;
                opponentHandVisualizer.isOpponent = true;
                opponentHandVisualizer.sideSpacing = 0.2f;
                opponentHandVisualizer.heightOffset = 0.82f;
                opponentHandVisualizer.forwardDistance = 1.8f;
                opponentHandVisualizer.lateralOffset = 0f;
                opponentHandVisualizer.tableEdgeDistance = 0.61f;
                opponentHandVisualizer.tiltAngle = -35f;
                
                opponentHandVisualizer.cardsInHand.Clear();

                for (int i = 0; i < 3; i++)
                {
                    // Cartas Locales
                    GameObject c = Instantiate(cardPrefab, transform.position, transform.rotation);
                    Destroy(c.GetComponent<Unity.Netcode.NetworkObject>());
                    Destroy(c.GetComponent<Unity.Netcode.Components.NetworkTransform>());

                    CardVisual visual = c.GetComponent<CardVisual>();
                    if (visual != null)
                    {
                        int cardIndex = cards[i];
                        if (cardIndex >= 0 && cardIndex < deck.AvailableCards.Count)
                        {
                            visual.SetCardData(deck.AvailableCards[cardIndex]);
                        }
                        handVisualizer.AddCardToHand(visual);
                    }

                    localCardInstances[i] = c;

                    // Cartas Enemigas (Vacias, solo el reverso)
                    GameObject oc = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
                    Destroy(oc.GetComponent<Unity.Netcode.NetworkObject>());
                    Destroy(oc.GetComponent<Unity.Netcode.Components.NetworkTransform>());

                    // Quitamos los textos para que se vea solo el dorso blanco 
                    CardVisual oppVisual = oc.GetComponent<CardVisual>();
                    if (oppVisual != null)
                    {
                        if (oppVisual.textNumber != null) oppVisual.textNumber.text = "";
                        if (oppVisual.textNumberBottom != null) oppVisual.textNumberBottom.text = "";
                        if (oppVisual.iconImage != null) oppVisual.iconImage.enabled = false;
                        opponentHandVisualizer.AddCardToHand(oppVisual);
                    }
                }

                SetupPowerVisuals();
            }
        }

        private void SetupPowerVisuals()
        {
            foreach (GameObject powerObject in localPowerInstances)
            {
                if (powerObject != null) Destroy(powerObject);
            }
            localPowerInstances.Clear();

            if (powerCatalog == null || powerCatalog.AvailablePowers == null
                || powerCatalog.AvailablePowers.Count == 0)
            {
                return;
            }

            if (powerHandVisualizer == null)
            {
                powerHandVisualizer = gameObject.AddComponent<PowerHandVisualizer>();
            }

            powerHandVisualizer.tableCenter = currentTable;
            powerHandVisualizer.powersOnTable.Clear();

            if (powerSystem == null)
            {
                powerSystem = gameObject.AddComponent<PowerSystem>();
            }

            foreach (PowerData power in powerCatalog.AvailablePowers)
            {
                if (power == null || !power.IsValid()) continue;

                GameObject powerObject = power.WorldPrefab != null
                    ? Instantiate(power.WorldPrefab, transform.position, transform.rotation)
                    : GameObject.CreatePrimitive(PrimitiveType.Cube);
                PowerVisual visual = powerObject.GetComponent<PowerVisual>();
                if (visual == null) visual = powerObject.AddComponent<PowerVisual>();

                if (power.WorldPrefab == null)
                {
                    powerObject.transform.localScale = new Vector3(0.45f, 0.08f, 0.7f);
                }

                visual.SetPowerData(power);
                powerHandVisualizer.AddPower(visual);
                localPowerInstances.Add(powerObject);
            }
        }

        private void Update()
        {
            if (IsOwner && (Time.timeScale <= 0f || GameplayInputBlocked))
            {
                netSpeed.Value = 0f;
            }
            else if (IsOwner)
            {
                if (UnityEngine.InputSystem.Keyboard.current.digit0Key.wasPressedThisFrame)
                {
                    if (firstPersonCamera == null && cameraTransform != null) firstPersonCamera = cameraTransform.GetComponent<Camera>();
                    if (firstPersonCamera == null) 
            {
                Camera[] cameras = GetComponentsInChildren<Camera>();
                foreach (var cam in cameras)
                {
                    if (cam != thirdPersonCamera)
                    {
                        firstPersonCamera = cam;
                        break;
                    }
                }
            } // Fallback agresivo
                    
                    if (firstPersonCamera != null && thirdPersonCamera != null)
                    {
                        bool isFP = firstPersonCamera.enabled;
                        firstPersonCamera.enabled = !isFP;
                        thirdPersonCamera.enabled = isFP;
                        Debug.Log("Cambiando cámara a: " + (isFP ? "Tercera Persona" : "Primera Persona"));
                    }
                    else
                    {
                        Debug.LogError("Error al cambiar cámara: fpc=" + (firstPersonCamera!=null) + " tpc=" + (thirdPersonCamera!=null));
                    }
                }

                netIsSeated.Value = isSeated;
                if (!isSeated)
                {
                    // Usamos la entrada de teclado directo para asegurar que la animación responda al instante
                    Vector2 input = Vector2.zero;
                    if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed) input.y += 1;
                    if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed) input.y -= 1;
                    if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed) input.x -= 1;
                    if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed) input.x += 1;
                    
                    netSpeed.Value = input.magnitude > 0.1f ? 1.0f : 0f;
                }
                else
                {
                    netSpeed.Value = 0f;
                }

                HandleLook();
                if (!isSeated) 
                {
                    HandleMovement();
                }
                else
                {
                    HandleInputs();
                }
            }

            // Aplicar variables de red al Animator local (para todos)
            if (animator == null) animator = GetComponentInChildren<Animator>(); 
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool("IsSeated", netIsSeated.Value);
                animator.SetFloat("Speed", netSpeed.Value);
            }

            // Deslizar suavemente el modelo visual hacia arriba para acomodarlo en la silla
            Transform visuals = transform.Find("Visuals");
            if (visuals != null)
            {
                // Mantenemos su X y Z original por si el modelo no est� centrado perfectamente
                float currentX = visuals.localPosition.x;
                float currentZ = visuals.localPosition.z;
                
                if (netIsSeated.Value)
                {
                    // Subimos un poco en Y para que no se hunda en la silla
                    visuals.localPosition = Vector3.MoveTowards(visuals.localPosition, new Vector3(currentX, 0.35f, currentZ), Time.deltaTime * 0.6f);
                }
                else
                {
                    visuals.localPosition = Vector3.MoveTowards(visuals.localPosition, new Vector3(currentX, 0f, currentZ), Time.deltaTime * 2f);
                }
            }

            // Aplicar rotacion de la cabeza (Pitch y Yaw) para todos
            if (headTransform != null)
            {
                // Adelantamos la cámara un poco (0.25 en Z) para que no vea su propio cuello/cuerpo al mirar hacia abajo
                float targetCamHeight = netIsSeated.Value ? 1.35f : 1.55f;
                headTransform.localPosition = new Vector3(0, targetCamHeight, 0.25f);
                
                headTransform.localEulerAngles = new Vector3(headPitch.Value, headYaw.Value, 0f);
            }
        }

        private Transform cachedHeadBone;

        private void LateUpdate()
        {
            if (animator != null)
            {
                if (cachedHeadBone == null)
                {
                    Transform[] allBones = animator.GetComponentsInChildren<Transform>();
                    foreach (var bone in allBones)
                    {
                        if (bone.name.ToLower().Contains("head") && !bone.name.ToLower().Contains("top"))
                        {
                            cachedHeadBone = bone;
                            break;
                        }
                    }
                }
                
                if (cachedHeadBone != null)
                {
                    // 1. Ocultar la cabeza solo para el jugador local en Primera Persona
                    if (IsOwner)
                    {
                        if (isFirstPersonView)
                            cachedHeadBone.localScale = UnityEngine.Vector3.zero;
                        else
                            cachedHeadBone.localScale = UnityEngine.Vector3.one;
                    }

                    // 2. Rotar el hueso de la cabeza visualmente para TODOS los clientes
                    // Usamos RotateAround con los ejes globales del jugador para evitar problemas con los ejes locales del hueso
                    cachedHeadBone.RotateAround(cachedHeadBone.position, transform.right, headPitch.Value);
                    cachedHeadBone.RotateAround(cachedHeadBone.position, transform.up, headYaw.Value);
                }
            }
        }

        private void HandleLook()
        {
            if (Mouse.current == null) return;
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            if (!isSeated)
            {
                // Primero rotamos la cabeza
                horizontalHeadRotation += mouseDelta.x * lookSensitivity;
                
                // L�mite de grados que la cabeza puede girar antes de arrastrar el cuerpo
                float maxHeadYaw = 55f; 
                
                if (horizontalHeadRotation > maxHeadYaw)
                {
                    float excess = horizontalHeadRotation - maxHeadYaw;
                    transform.Rotate(Vector3.up * excess);
                    horizontalHeadRotation = maxHeadYaw;
                }
                else if (horizontalHeadRotation < -maxHeadYaw)
                {
                    float excess = horizontalHeadRotation - (-maxHeadYaw);
                    transform.Rotate(Vector3.up * excess);
                    horizontalHeadRotation = -maxHeadYaw;
                }

                // Si el personaje est� caminando, alineamos gradualmente el cuerpo hacia donde mira la cabeza
                if (netSpeed.Value > 0.1f && Mathf.Abs(horizontalHeadRotation) > 0.1f)
                {
                    float alignSpeed = 6f; // Velocidad con la que el cuerpo se alinea a la mirada
                    float correction = horizontalHeadRotation * Time.deltaTime * alignSpeed;
                    
                    // Rotamos el cuerpo en la dirección de la correcci�n
                    transform.Rotate(Vector3.up * correction);
                    
                    // Restamos la correcci�n a la cabeza para que la cámara siga mirando al mismo punto fijo en el mundo
                    horizontalHeadRotation -= correction;
                }
                
                headYaw.Value = horizontalHeadRotation;
            }
            else
            {
                // Si est� sentado, el cuerpo no gira, solo la cabeza (con un l�mite de 90 grados a cada lado)
                horizontalHeadRotation += mouseDelta.x * lookSensitivity;
                horizontalHeadRotation = Mathf.Clamp(horizontalHeadRotation, -90f, 90f);
                headYaw.Value = horizontalHeadRotation;
            }

            verticalLookRotation -= mouseDelta.y * lookSensitivity;
            verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);
            
            headPitch.Value = verticalLookRotation;
        }

        private void HandleMovement()
        {
            Vector2 moveInput = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
            if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;
            if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;
            if (Keyboard.current.dKey.isPressed) moveInput.x += 1;

            Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
            move *= walkSpeed;
            
            if (!characterController.isGrounded)
            {
                move.y += Physics.gravity.y;
            }
            else
            {
                move.y = -2f; 
            }

            characterController.Move(move * Time.deltaTime);

        }

        private void HandleInputs()
        {
            if (!isSeated) return;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = new Ray(headTransform.position, headTransform.forward);
                                if (Physics.Raycast(ray, out RaycastHit hit, 5f))
                {
                                    PowerVisual powerVisual = hit.collider.GetComponent<PowerVisual>();
                                    if (powerVisual == null) powerVisual = hit.collider.GetComponentInParent<PowerVisual>();

                                    if (powerVisual != null && powerHandVisualizer != null
                                        && powerHandVisualizer.powersOnTable.Contains(powerVisual))
                                    {
                                        PlayPower(powerVisual);
                                        return;
                                    }

                                    CardVisual visual = hit.collider.GetComponent<CardVisual>();
                    if (visual == null) visual = hit.collider.GetComponentInParent<CardVisual>();
                    
                    if (visual != null && handVisualizer != null && handVisualizer.cardsInHand.Contains(visual))
                    {
                        int originalIndex = -1;
                        for (int i = 0; i < 3; i++)
                        {
                            if (localCardInstances[i] != null && localCardInstances[i].GetComponent<CardVisual>() == visual)
                            {
                                originalIndex = i;
                                break;
                            }
                        }
                        
                        if (originalIndex != -1)
                        {
                            PlayCard(originalIndex);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            if (localCardInstances[i] != null && hit.collider.transform.IsChildOf(localCardInstances[i].transform))
                            {
                                PlayCard(i);
                                break;
                            }
                        }
                    }
                }
            }

            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame) PlayCard(0);
                if (Keyboard.current.digit2Key.wasPressedThisFrame) PlayCard(1);
                if (Keyboard.current.digit3Key.wasPressedThisFrame) PlayCard(2);

                if (Keyboard.current.rKey.wasPressedThisFrame) CallEnvido(EnvidoCall.RealEnvido);
                if (Keyboard.current.fKey.wasPressedThisFrame) CallEnvido(EnvidoCall.FaltaEnvido);
                if (Keyboard.current.qKey.wasPressedThisFrame) CallAction("Truco");
                if (Keyboard.current.aKey.wasPressedThisFrame) CallAction("Accept");
                if (Keyboard.current.nKey.wasPressedThisFrame) CallAction("Decline");
                if (Keyboard.current.pKey.wasPressedThisFrame) StandUp();
            }
        }

                private void StandUp()
        {
            if (!isSeated) return;
            isSeated = false;
            
            if (currentSeatInteractable != null)
            {
                if (currentSeatInteractable.CurrentOccupant == gameObject)
                {
                    currentSeatInteractable.CurrentOccupant = null;
                }
                currentSeatInteractable = null;
            }

            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance != null) serverInstance.PlayerStandUpServerRpc();

            // Moverse apenas hacia atr�s (0.5m) para salir de la colision de la silla
            if (characterController != null) characterController.enabled = false;
            transform.position = transform.position - transform.forward * 0.5f;
            if (characterController != null) characterController.enabled = true;
        }

        private void CallAction(string actionType)
        {
            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance == null) return;

            switch (actionType)
            {
                case "Truco": serverInstance.CallTrucoServerRpc(); break;
                case "Accept": serverInstance.RespondServerRpc(true); break;
                case "Decline": serverInstance.RespondServerRpc(false); break;
            }
        }

        private void CallEnvido(EnvidoCall call)
        {
            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance == null || !CanCallEnvidoLocally(call)) return;
            serverInstance.CallEnvidoServerRpc(call);
        }

        private bool CanCallEnvidoLocally(EnvidoCall call)
        {
            int playedCardCount = 0;
            for (int i = 0; i < myCards.Length; i++)
                if (myCards[i] == -1) playedCardCount++;
            int inferredRoundIndex = playedCardCount == 0 ? 0 : 1;
            return EnvidoRules.CanCallFromContext(
                localGameState,
                localCantoPendiente,
                localLastCallerId,
                NetworkManager.Singleton.LocalClientId,
                inferredRoundIndex,
                playedCardCount,
                localEstadoTruco,
                localEstadoEnvido,
                call);
        }

        private void PlayCard(int handSlot)
        {
            if (myCards[handSlot] == -1) return;

            if (localGameState != GameState.TurnoJugador || localTurnId != NetworkManager.Singleton.LocalClientId) 
            {
                Debug.LogWarning("No es tu turno para jugar una carta.");
                return;
            }

            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance != null)
            {
                serverInstance.PlayCardServerRpc(myCards[handSlot]);
                myCards[handSlot] = -1; // Marcar como jugada

                if (localCardInstances[handSlot] != null)
                {
                    if (handVisualizer != null) 
                    {
                        CardVisual visual = localCardInstances[handSlot].GetComponent<CardVisual>();
                        if (visual != null) handVisualizer.RemoveCardFromHand(visual);
                    }
                    Destroy(localCardInstances[handSlot]);
                }
            }
        }

        private void PlayPower(PowerVisual visual)
        {
            if (visual == null || visual.Data == null || powerSystem == null) return;

            bool executed = powerSystem.TryExecute(
                visual.Data,
                NetworkManager.Singleton.LocalClientId,
                amIPlayer1 ? 1 : 2,
                gameObject);
            if (!executed) return;

            powerHandVisualizer.RemovePower(visual);
            localPowerInstances.Remove(visual.gameObject);
            Destroy(visual.gameObject);
        }

                private void TrySitDown()
        {
                    TableDefinition.EnsureSceneConfiguration();

                    GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            GameObject closestObject = null;
            SeatInteractable closestInteractable = null;
            float closestDistance = float.PositiveInfinity;

            PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

            foreach (GameObject obj in allObjects)
            {
                bool usesLegacyBehavior = TableDefinition.IsSeatName(obj.name);
                SeatInteractable interactable = obj.GetComponent<SeatInteractable>();
                if (!usesLegacyBehavior && interactable == null) continue;

                if (interactable != null && interactable.IsOccupied)
                    continue;

                bool isOccupied = false;
                Vector3 seatPos = obj.transform.position;
                foreach (PlayerController player in allPlayers)
                {
                    if (player != this)
                    {
                        Vector3 playerPos = player.transform.position;
                        float horizDist = Mathf.Sqrt((playerPos.x - seatPos.x) * (playerPos.x - seatPos.x) + (playerPos.z - seatPos.z) * (playerPos.z - seatPos.z));
                        if (horizDist < 0.6f && Mathf.Abs(playerPos.y - seatPos.y) < 1.2f)
                        {
                            isOccupied = true;
                            break;
                        }
                    }
                }

                if (!isOccupied)
                {
                    BotMarker[] allBots = FindObjectsByType<BotMarker>(FindObjectsSortMode.None);
                    foreach (BotMarker bot in allBots)
                    {
                        if (interactable != null && bot.assignedSeat == interactable)
                        {
                            isOccupied = true;
                            break;
                        }
                        Vector3 botPos = bot.transform.position;
                        float horizDist = Mathf.Sqrt((botPos.x - seatPos.x) * (botPos.x - seatPos.x) + (botPos.z - seatPos.z) * (botPos.z - seatPos.z));
                        if (horizDist < 0.6f && Mathf.Abs(botPos.y - seatPos.y) < 1.2f)
                        {
                            isOccupied = true;
                            break;
                        }
                    }
                }

                if (isOccupied) continue;

                // En modo individual o si hay bots en la escena, Banco B siempre está reservado para el bot
                if (Trucardo.Core.NetworkUI.selectedMode == GameMode.Singleplayer 
                    || FindObjectsByType<BotMarker>(FindObjectsSortMode.None).Length > 0)
                {
                    TableDefinition table = TableDefinition.FindForSeat(obj.transform);
                    if (table != null && table.Seats != null && table.Seats.Count >= 2 && table.Seats[1] != null 
                        && (table.Seats[1].gameObject == obj || obj.transform.IsChildOf(table.Seats[1].transform)))
                    {
                        continue;
                    }
                }

                float distance = Vector3.Distance(transform.position, obj.transform.position);
                float interactionRange = usesLegacyBehavior ? 1.5f : 1.0f;
                if (distance < interactionRange && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObject = obj;
                    closestInteractable = interactable;
                }
            }

            if (closestObject != null)
            {
                Transform seatTransform = closestObject.transform;
                currentTableDefinition = TableDefinition.FindForSeat(seatTransform);
                if (currentTableDefinition == null)
                {
                    Debug.LogError($"El banco {seatTransform.name} no está vinculado a ninguna mesa. Verificá que esté dentro de Mesas_y_Sillas.");
                    return;
                }
                currentTable = currentTableDefinition.transform;

                if (characterController != null) characterController.enabled = false;

                Vector3 toTable = currentTable.position - seatTransform.position;
                toTable.y = 0;
                transform.rotation = toTable.sqrMagnitude > 0.001f ? Quaternion.LookRotation(toTable) : Quaternion.Euler(0, seatTransform.eulerAngles.y, 0);
                float seatHeight = closestInteractable != null
                    ? closestInteractable.GetTopHeight() - 0.45f
                    : 0f;
                transform.position = new Vector3(seatTransform.position.x, seatHeight, seatTransform.position.z);

                currentSeatInteractable = closestInteractable;
                if (currentSeatInteractable != null)
                {
                    currentSeatInteractable.CurrentOccupant = gameObject;
                }

                isSeated = true;

                if (handVisualizer != null)
                {
                    handVisualizer.tableCenter = currentTable;
                    handVisualizer.RefreshLayout();
                }

                if (opponentHandVisualizer != null)
                {
                    opponentHandVisualizer.tableCenter = currentTable;
                    opponentHandVisualizer.RefreshLayout();
                }

                if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
                if (serverInstance != null) serverInstance.PlayerSitDownServerRpc(currentTableDefinition.TableId);

                Debug.Log("Te has sentado en una silla.");
            }
            else
            {
                Debug.LogWarning("Estás muy lejos de una silla. Acércate más a una para sentarte.");
            }
        }

        private NivelTruco localEstadoTruco = NivelTruco.Ninguno;

        public void RemoveOpponentCard()
        {
            if (opponentHandVisualizer != null && opponentHandVisualizer.cardsInHand.Count > 0)
            {
                CardVisual cardToRemove = opponentHandVisualizer.cardsInHand[0];
                opponentHandVisualizer.RemoveCardFromHand(cardToRemove);
                Destroy(cardToRemove.gameObject);
            }
        }

        private bool amIPlayer1 = true;

        public void UpdateUIState(
            GameState state,
            ulong turnId,
            int p1Score,
            int p2Score,
            TipoCanto canto,
            ulong lastCaller,
            NivelTruco estadoTruco,
            EstadoEnvido estadoEnvido,
            int envidoAcceptedPoints,
            int pendingEnvidoPoints,
            ulong p1Id)
        {
            localGameState = state;
            localTurnId = turnId;
            localP1Score = p1Score;
            localP2Score = p2Score;
            localCantoPendiente = canto;
            localLastCallerId = lastCaller;
            localEstadoTruco = estadoTruco;
            localEstadoEnvido = estadoEnvido;
            localEnvidoAcceptedPoints = envidoAcceptedPoints;
            localPendingEnvidoPoints = pendingEnvidoPoints;
            amIPlayer1 = (NetworkManager.Singleton.LocalClientId == p1Id);
        }

        private void DrawTrucoBox(Rect rect, int points)
        {
            if (points <= 0) return;
            
            Texture2D tex = Texture2D.whiteTexture;
            int thickness = 2;
            
            // 1 punto: L�nea vertical izquierda
            if (points >= 1) GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), tex);
            // 2 puntos: L�nea horizontal superior
            if (points >= 2) GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), tex);
            // 3 puntos: L�nea vertical derecha
            if (points >= 3) GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), tex);
            // 4 puntos: L�nea horizontal inferior
            if (points >= 4) GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), tex);
            // 5 puntos: Diagonal
            if (points >= 5) 
            {
                Vector2 pivot = new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2);
                GUIUtility.RotateAroundPivot(-45f, pivot);
                // Dibujar una l�nea horizontal pero rotada parecer� una diagonal
                GUI.DrawTexture(new Rect(rect.x - 2, rect.y + rect.height / 2 - 1, rect.width + 4, thickness), tex);
                GUIUtility.RotateAroundPivot(45f, pivot); // Restaurar matriz
            }
        }
        
        private void DrawScoreRow(float x, float y, string label, int totalPoints)
        {
            GUI.Label(new Rect(x, y, 100, 30), label);
            
            int limit = Mathf.Min(totalPoints, 15);
            float currentX = x + 80;
            
            while (limit > 0)
            {
                int p = Mathf.Min(limit, 5);
                DrawTrucoBox(new Rect(currentX, y + 5, 30, 30), p);
                currentX += 40;
                limit -= p;
            }
        }

        private void OnGUI()
        {
            if (!IsOwner || !isSeated) return;

            // Puntuaci�n tipo Fosforos a la derecha
            GUI.skin.label.fontSize = 20;
            GUI.skin.label.alignment = TextAnchor.UpperLeft; // Left align text for the rows
            GUI.contentColor = Color.white;
            
            float scoreX = Screen.width - 250;
            
            int myScore = amIPlayer1 ? localP1Score : localP2Score;
            int theirScore = amIPlayer1 ? localP2Score : localP1Score;
            
            GUI.Label(new Rect(scoreX, 20, 230, 30), "--- YO ---");
            DrawScoreRow(scoreX, 50, "Malas:", myScore);
            DrawScoreRow(scoreX, 90, "Buenas:", Mathf.Max(0, myScore - 15));
            GUI.Label(new Rect(scoreX, 130, 230, 30), $"Total: {myScore}");
            
            GUI.Label(new Rect(scoreX, 180, 230, 30), "--- EL ---");
            DrawScoreRow(scoreX, 210, "Malas:", theirScore);
            DrawScoreRow(scoreX, 250, "Buenas:", Mathf.Max(0, theirScore - 15));
            GUI.Label(new Rect(scoreX, 290, 230, 30), $"Total: {theirScore}");

            // Indicador de nivel de Truco
            GUI.skin.label.alignment = TextAnchor.UpperCenter;
            int trucoPts = (localEstadoTruco == NivelTruco.Ninguno) ? 1 : (int)localEstadoTruco;
            GUI.contentColor = new Color(0.8f, 0.8f, 1f);
            GUI.Label(new Rect(0, 10, Screen.width, 30), $"Mano en Juego: {trucoPts} punto(s) ({(localEstadoTruco == NivelTruco.Ninguno ? "Simple" : localEstadoTruco.ToString())})");

            // Estado del Turno
            GUI.skin.label.fontSize = 32;
            
            if (localGameState == GameState.TurnoJugador)
            {
                if (localTurnId == NetworkManager.Singleton.LocalClientId)
                {
                    GUI.contentColor = Color.green;
                    GUI.Label(new Rect(0, 50, Screen.width, 50), "¡ES TU TURNO!");
                }
                else
                {
                    GUI.contentColor = Color.red;
                    GUI.Label(new Rect(0, 50, Screen.width, 50), "Esperando al rival...");
                }
            }
            else if (localGameState == GameState.EsperandoRespuestaCanto)
            {
                GUI.contentColor = Color.yellow;
                GUI.skin.label.fontSize = 24; // Mas chico
                if (localLastCallerId != NetworkManager.Singleton.LocalClientId)
                {
                    // Un poco m�s arriba (Screen.height / 4)
                    string pendingName = localCantoPendiente == TipoCanto.Envido
                        ? EnvidoRules.GetDisplayName(localEstadoEnvido)
                        : localCantoPendiente.ToString().ToUpperInvariant();
                    string options = "[A] Quiero - [N] No Quiero";
                    if (CanCallEnvidoLocally(EnvidoCall.Envido)) options += " - [E] Envido";
                    if (CanCallEnvidoLocally(EnvidoCall.RealEnvido)) options += " - [R] Real Envido";
                    if (CanCallEnvidoLocally(EnvidoCall.FaltaEnvido)) options += " - [F] Falta Envido";
                    if (localCantoPendiente == TipoCanto.Envido)
                        options += $"\nApuesta: {localPendingEnvidoPoints} | No Quiero: {EnvidoRules.GetRejectedPoints(localEnvidoAcceptedPoints)}";
                    GUI.Label(new Rect(0, Screen.height / 4, Screen.width, 120), $"¡El rival cantó {pendingName}!\n{options}");
                }
                else
                {
                    string pendingName = localCantoPendiente == TipoCanto.Envido
                        ? EnvidoRules.GetDisplayName(localEstadoEnvido)
                        : localCantoPendiente.ToString().ToUpperInvariant();
                    GUI.Label(new Rect(0, 80, Screen.width, 50), $"Esperando que el rival responda tu {pendingName}...");
                }
            }
            else if (localGameState == GameState.RondaTerminada)
            {
                GUI.contentColor = Color.cyan;
                GUI.Label(new Rect(0, 50, Screen.width, 50), "¡Mano terminada! Repartiendo...");
            }
            
            GUI.contentColor = Color.white;
            GUI.skin.label.fontSize = 18;
            GUI.skin.label.alignment = TextAnchor.LowerLeft;
            GUI.Label(new Rect(20, Screen.height - 145, 500, 125), "Controles:\n[1, 2, 3] Jugar Carta\n[E] Envido - [R] Real Envido - [F] Falta Envido\n[Q] Truco - [A] Quiero - [N] No Quiero");

            // Crosshair
            GUI.skin.label.fontSize = 20; // Mas chico a pedido del jugador
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(Screen.width / 2f - 25f, Screen.height / 2f - 25f, 50f, 50f), "•");
        }
    }
}
