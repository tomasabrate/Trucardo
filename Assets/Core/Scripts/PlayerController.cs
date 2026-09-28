using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Trucardo.Core
{
    /// <summary>
    /// Controlador del jugador (Cliente). 
    /// Recibe el input (teclado/mouse) y lo envía como comandos al servidor.
    /// Solo interactúa si IsOwner es verdadero (es decir, es la computadora local del jugador).
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

        [Header("Referencias a Prefabs")]
        public GameObject cardPrefab;

        private int[] myCards = new int[3] { -1, -1, -1 };
        private GameObject[] localCardInstances = new GameObject[3];
        private HandVisualizer handVisualizer;
        private HandVisualizer opponentHandVisualizer;
        
        // Estado local para la UI
        private GameState localGameState;
        private ulong localTurnId;
        private int localP1Score;
        private int localP2Score;
        private TipoCanto localCantoPendiente;
        private ulong localLastCallerId;
        private GameServer serverInstance;

        private Camera firstPersonCamera;
        private Camera thirdPersonCamera;

        public override void OnNetworkSpawn()
        {
            characterController = GetComponent<CharacterController>();
            
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
                    if (mesh != null) mesh.GetComponent<MeshRenderer>().enabled = false;
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
            }
        }

        private void SetupLocalPlayerVisuals()
        {
            // Ocultar la cabeza del jugador local, pero mantener la sombra (Cast Shadows: Shadows Only)
            // para que no obstruya el frustum de la camara en primera persona.
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
                // Lo dejamos así, el problema de ver el sombrero se mitigará ajustando la cámara.
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
            if (firstPersonCamera == null) firstPersonCamera = GetComponentInChildren<Camera>();
        }

        public void ReceiveCards(int[] cards)
        {
            myCards = cards;
            Debug.Log($"[Local Player] Cartas recibidas. Instanciando 3D...");

            for (int i = 0; i < 3; i++)
            {
                if (localCardInstances[i] != null) Destroy(localCardInstances[i]);
            }

            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance == null) return;
            
            DeckData deck = serverInstance.gameModel?.DeckReference;
            
            if (cardPrefab != null && deck != null)
            {
                if (handVisualizer == null)
                {
                    handVisualizer = gameObject.AddComponent<HandVisualizer>();
                }
                
                float mySideZ = transform.position.z > 0 ? 0.55f : -0.55f;
                handVisualizer.absoluteZ = mySideZ;
                handVisualizer.sideSpacing = 0.2f; 
                handVisualizer.absoluteY = 0.83f; 
                handVisualizer.tiltAngle = -35f; 
                
                handVisualizer.cardsInHand.Clear();

                if (opponentHandVisualizer == null)
                {
                    opponentHandVisualizer = gameObject.AddComponent<HandVisualizer>();
                }
                
                opponentHandVisualizer.absoluteZ = -mySideZ;
                opponentHandVisualizer.sideSpacing = 0.2f;
                opponentHandVisualizer.absoluteY = 0.80f; 
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
                        visual.SetCardData(deck.AvailableCards[cards[i]]);
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
            }
        }

        private void Update()
        {
            if (IsOwner)
            {
                if (UnityEngine.InputSystem.Keyboard.current.digit0Key.wasPressedThisFrame)
                {
                    if (firstPersonCamera == null && cameraTransform != null) firstPersonCamera = cameraTransform.GetComponent<Camera>();
                    if (firstPersonCamera == null) firstPersonCamera = GetComponentInChildren<Camera>(); // Fallback agresivo
                    
                    if (firstPersonCamera != null && thirdPersonCamera != null)
                    {
                        bool isFP = firstPersonCamera.enabled;
                        firstPersonCamera.enabled = !isFP;
                        thirdPersonCamera.enabled = isFP;
                        Debug.Log("Cambiando camara a: " + (isFP ? "Tercera Persona" : "Primera Persona"));
                    }
                    else
                    {
                        Debug.LogError("Error al cambiar camara: fpc=" + (firstPersonCamera!=null) + " tpc=" + (thirdPersonCamera!=null));
                    }
                }

                netIsSeated.Value = isSeated;
                if (!isSeated)
                {
                    // Usamos la entrada de teclado directo para asegurar que la animacion responda al instante
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
                // Mantenemos su X y Z original por si el modelo no está centrado perfectamente
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
                    if (IsOwner && firstPersonCamera != null)
                    {
                        if (firstPersonCamera.gameObject.activeInHierarchy && firstPersonCamera.enabled)
                            cachedHeadBone.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                        else
                            cachedHeadBone.localScale = Vector3.one;
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
                
                // Límite de grados que la cabeza puede girar antes de arrastrar el cuerpo
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

                // Si el personaje está caminando, alineamos gradualmente el cuerpo hacia donde mira la cabeza
                if (netSpeed.Value > 0.1f && Mathf.Abs(horizontalHeadRotation) > 0.1f)
                {
                    float alignSpeed = 6f; // Velocidad con la que el cuerpo se alinea a la mirada
                    float correction = horizontalHeadRotation * Time.deltaTime * alignSpeed;
                    
                    // Rotamos el cuerpo en la dirección de la corrección
                    transform.Rotate(Vector3.up * correction);
                    
                    // Restamos la corrección a la cabeza para que la cámara siga mirando al mismo punto fijo en el mundo
                    horizontalHeadRotation -= correction;
                }
                
                headYaw.Value = horizontalHeadRotation;
            }
            else
            {
                // Si está sentado, el cuerpo no gira, solo la cabeza (con un límite de 90 grados a cada lado)
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

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                TrySitDown();
            }
        }

        private void HandleInputs()
        {
            if (!isSeated) return;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = new Ray(headTransform.position, headTransform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, 5f))
                {
                    CardVisual visual = hit.collider.GetComponent<CardVisual>();
                    if (visual != null && handVisualizer != null && handVisualizer.cardsInHand.Contains(visual))
                    {
                        int index = handVisualizer.cardsInHand.IndexOf(visual);
                        PlayCard(index);
                    }
                    else if (visual == null) 
                    {
                        visual = hit.collider.GetComponentInParent<CardVisual>();
                        if (visual != null && handVisualizer != null && handVisualizer.cardsInHand.Contains(visual))
                        {
                            int index = handVisualizer.cardsInHand.IndexOf(visual);
                            PlayCard(index);
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
            }

            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame) PlayCard(0);
                if (Keyboard.current.digit2Key.wasPressedThisFrame) PlayCard(1);
                if (Keyboard.current.digit3Key.wasPressedThisFrame) PlayCard(2);

                if (Keyboard.current.eKey.wasPressedThisFrame) CallAction("Envido");
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
            
            // Moverse apenas hacia atrás (0.5m) para salir de la colision de la silla
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
                case "Envido": serverInstance.CallEnvidoServerRpc(); break;
                case "Truco": serverInstance.CallTrucoServerRpc(); break;
                case "Accept": serverInstance.RespondServerRpc(true); break;
                case "Decline": serverInstance.RespondServerRpc(false); break;
            }
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

        private void TrySitDown()
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            GameObject closestChair = null;
            float minDistance = 2.5f; // Distancia máxima para poder sentarse

            foreach (var obj in allObjects)
            {
                // Buscar objetos que se llamen Silla (ej: "PrefabSilla", "Silla(Clone)", "Silla1")
                if (obj.name.ToLower().Contains("silla"))
                {
                    float dist = Vector3.Distance(transform.position, obj.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestChair = obj;
                    }
                }
            }

            if (closestChair != null)
            {
                if (characterController != null) characterController.enabled = false;

                // Rotar para mirar hacia la mesa (asumiendo que la silla mira hacia la mesa)
                // Si la silla está rotada apuntando a la mesa, copiamos su rotación Y
                transform.rotation = Quaternion.Euler(0, closestChair.transform.eulerAngles.y, 0);

                // Sentar al jugador (y lo movemos hacia adelante para que quede más cerca de la mesa)
                transform.position = new Vector3(closestChair.transform.position.x, 0f, closestChair.transform.position.z) ; 
                
                isSeated = true;
                
                if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
                if (serverInstance != null) serverInstance.PlayerSitDownServerRpc();
                
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

        public void UpdateUIState(GameState state, ulong turnId, int p1Score, int p2Score, TipoCanto canto, ulong lastCaller, NivelTruco estadoTruco, ulong p1Id)
        {
            localGameState = state;
            localTurnId = turnId;
            localP1Score = p1Score;
            localP2Score = p2Score;
            localCantoPendiente = canto;
            localLastCallerId = lastCaller;
            localEstadoTruco = estadoTruco;
            amIPlayer1 = (NetworkManager.Singleton.LocalClientId == p1Id);
        }

        private void DrawTrucoBox(Rect rect, int points)
        {
            if (points <= 0) return;
            
            Texture2D tex = Texture2D.whiteTexture;
            int thickness = 2;
            
            // 1 punto: Línea vertical izquierda
            if (points >= 1) GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), tex);
            // 2 puntos: Línea horizontal superior
            if (points >= 2) GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), tex);
            // 3 puntos: Línea vertical derecha
            if (points >= 3) GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), tex);
            // 4 puntos: Línea horizontal inferior
            if (points >= 4) GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), tex);
            // 5 puntos: Diagonal
            if (points >= 5) 
            {
                Vector2 pivot = new Vector2(rect.x + rect.width / 2, rect.y + rect.height / 2);
                GUIUtility.RotateAroundPivot(-45f, pivot);
                // Dibujar una línea horizontal pero rotada parecerá una diagonal
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
            if (!IsOwner) return;

            // Puntuación tipo Fosforos a la derecha
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
                GUI.skin.label.fontSize = 24; // Más chico
                if (localLastCallerId != NetworkManager.Singleton.LocalClientId)
                {
                    // Un poco más arriba (Screen.height / 4)
                    GUI.Label(new Rect(0, Screen.height / 4, Screen.width, 100), $"¡El rival cantó {localCantoPendiente}!\n[A] Aceptar - [N] Declinar");
                }
                else
                {
                    GUI.Label(new Rect(0, 80, Screen.width, 50), $"Esperando que el rival responda tu {localCantoPendiente}...");
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
            GUI.Label(new Rect(20, Screen.height - 120, 400, 100), "Controles:\n[1, 2, 3] Jugar Carta\n[E] Cantar Envido\n[Q] Cantar Truco");

            // Crosshair
            GUI.skin.label.fontSize = 20; // Más chico a pedido del jugador
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(Screen.width / 2f - 25f, Screen.height / 2f - 25f, 50f, 50f), "•");
        }
    }
}

