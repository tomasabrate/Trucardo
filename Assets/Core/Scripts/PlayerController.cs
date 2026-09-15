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
        public NetworkVariable<float> headPitch = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private Transform headTransform;
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

        public override void OnNetworkSpawn()
        {
            characterController = GetComponent<CharacterController>();
            headTransform = transform.Find("Cabeza");
            
            if (cameraTransform != null)
                cameraTransform.localEulerAngles = Vector3.zero;


            if (IsOwner)
            {
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
                
                if (cameraTransform != null) cameraTransform.gameObject.SetActive(true);

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                if (cameraTransform != null) cameraTransform.gameObject.SetActive(false);
            }

            BuildGauchoVisuals();
        }

        private void BuildGauchoVisuals()
        {
            // Host es 0, Cliente es 1
            bool isHost = OwnerClientId == 0;
            Color teamColor = isHost ? Color.blue : Color.red;
            Color skinColor = new Color(0.9f, 0.75f, 0.6f);
            Color hatColor = new Color(0.4f, 0.2f, 0.05f); // Marron
            Color mustacheColor = Color.black;

            Transform cabeza = transform.Find("Cabeza");
            Transform cuerpo = transform.Find("Cuerpo");
            
            Material baseMat = null;
            if (cuerpo != null && cuerpo.GetComponent<MeshRenderer>() != null)
            {
                baseMat = cuerpo.GetComponent<MeshRenderer>().material;
            }
            if (baseMat == null) return;

            Material skinMat = new Material(baseMat) { color = skinColor };
            Material hatMat = new Material(baseMat) { color = hatColor };
            Material mustacheMat = new Material(baseMat) { color = mustacheColor };
            Material eyeMat = new Material(baseMat) { color = Color.black };
            Material ponchoMat = new Material(baseMat) { color = teamColor }; // El poncho lleva el color del equipo

            if (cuerpo != null)
            {
                // Achicar el cuerpo para que sea mas un cuadrado y no tan ancho/largo
                cuerpo.localScale = new Vector3(0.35f, 0.6f, 0.25f); 
                cuerpo.localPosition = new Vector3(0, 0.7f, 0); // Ajustar altura para que se conecte con la cabeza (y=1.25)
                
                MeshRenderer mr = cuerpo.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = ponchoMat;
            }

            if (cabeza != null)
            {
                MeshRenderer mr = cabeza.GetComponent<MeshRenderer>();
                if (mr != null) mr.material = skinMat;

                // Sombrero Base
                GameObject hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hat.name = "Sombrero";
                hat.transform.SetParent(cabeza);
                hat.transform.localPosition = new Vector3(0, 0.45f, 0); // En la parte superior de la esfera
                hat.transform.localScale = new Vector3(1.3f, 0.05f, 1.3f);
                hat.GetComponent<MeshRenderer>().material = hatMat;
                Destroy(hat.GetComponent<Collider>());

                // Sombrero Copa
                GameObject hatTop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hatTop.name = "CopaSombrero";
                hatTop.transform.SetParent(cabeza);
                hatTop.transform.localPosition = new Vector3(0, 0.6f, 0); // Arriba de la base
                hatTop.transform.localScale = new Vector3(0.7f, 0.15f, 0.7f);
                hatTop.GetComponent<MeshRenderer>().material = hatMat;
                Destroy(hatTop.GetComponent<Collider>());

                // Cinta Sombrero (Color del equipo)
                GameObject hatBand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hatBand.name = "CintaSombrero";
                hatBand.transform.SetParent(cabeza);
                hatBand.transform.localPosition = new Vector3(0, 0.48f, 0);
                hatBand.transform.localScale = new Vector3(0.72f, 0.04f, 0.72f);
                hatBand.GetComponent<MeshRenderer>().material = ponchoMat;
                Destroy(hatBand.GetComponent<Collider>());

                // Bigote
                GameObject mustache = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mustache.name = "Bigote";
                mustache.transform.SetParent(cabeza);
                mustache.transform.localPosition = new Vector3(0, -0.15f, 0.45f);
                mustache.transform.localScale = new Vector3(0.6f, 0.1f, 0.15f);
                mustache.GetComponent<MeshRenderer>().material = mustacheMat;
                Destroy(mustache.GetComponent<Collider>());

                // Ojo Izquierdo
                GameObject eyeL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eyeL.name = "OjoIzq";
                eyeL.transform.SetParent(cabeza);
                eyeL.transform.localPosition = new Vector3(-0.2f, 0.1f, 0.42f);
                eyeL.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
                eyeL.GetComponent<MeshRenderer>().material = eyeMat;
                Destroy(eyeL.GetComponent<Collider>());
                
                // Ojo Derecho
                GameObject eyeR = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eyeR.name = "OjoDer";
                eyeR.transform.SetParent(cabeza);
                eyeR.transform.localPosition = new Vector3(0.2f, 0.1f, 0.42f);
                eyeR.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
                eyeR.GetComponent<MeshRenderer>().material = eyeMat;
                Destroy(eyeR.GetComponent<Collider>());

                // Ocultar mallas para el jugador local para que no obstruyan la vista de la camara
                if (IsOwner)
                {
                    hat.GetComponent<MeshRenderer>().enabled = false;
                    hatTop.GetComponent<MeshRenderer>().enabled = false;
                    hatBand.GetComponent<MeshRenderer>().enabled = false;
                    mustache.GetComponent<MeshRenderer>().enabled = false;
                    eyeL.GetComponent<MeshRenderer>().enabled = false;
                    eyeR.GetComponent<MeshRenderer>().enabled = false;
                }
            }
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
                    
                    // Configuración absoluta (Player 1 está en -Z, Player 2 en +Z)
                    handVisualizer.absoluteZ = (NetworkManager.Singleton.IsServer) ? -0.55f : 0.55f;
                    handVisualizer.sideSpacing = 0.2f; 
                    handVisualizer.absoluteY = 1.03f; // Valores ajustados por el usuario
                    handVisualizer.tiltAngle = -35f; 
                }
                
                handVisualizer.cardsInHand.Clear();

                if (opponentHandVisualizer == null)
                {
                    opponentHandVisualizer = gameObject.AddComponent<HandVisualizer>();
                    
                    // El oponente está en el lado contrario
                    opponentHandVisualizer.absoluteZ = (NetworkManager.Singleton.IsServer) ? 0.55f : -0.55f;
                    opponentHandVisualizer.sideSpacing = 0.2f;
                    opponentHandVisualizer.absoluteY = 1.0f; // Valores ajustados por el usuario
                    opponentHandVisualizer.tiltAngle = -35f;
                }
                
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
                HandleLook();
                if (!isSeated) HandleMovement();
                
                HandleInputs();
            }

            // Aplicar rotacion de la cabeza (Pitch) para todos (owner y remotos)
            // Se sincroniza a traves de la NetworkVariable headPitch
            if (headTransform != null)
            {
                headTransform.localEulerAngles = new Vector3(headPitch.Value, 0f, 0f);
            }
        }

        private void HandleLook()
        {
            if (Mouse.current == null) return;
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            // Rotar cuerpo de izquierda a derecha (esto se sincroniza por el NetworkTransform del root)
            transform.Rotate(Vector3.up * mouseDelta.x * lookSensitivity);

            // Calcular rotacion de arriba a abajo y guardarla en la NetworkVariable (que el Owner puede escribir)
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
            characterController.Move(move * walkSpeed * Time.deltaTime);

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                TrySitDown();
            }
        }

        private void HandleInputs()
        {
            if (!isSeated) return;

            // Click para jugar carta
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
                        // Si hace click y pega en un collider hijo (ej. el quad de la carta)
                        visual = hit.collider.GetComponentInParent<CardVisual>();
                        if (visual != null && handVisualizer != null && handVisualizer.cardsInHand.Contains(visual))
                        {
                            int index = handVisualizer.cardsInHand.IndexOf(visual);
                            PlayCard(index);
                        }
                        else 
                        {
                            // Si aun asi no lo encuentra, intentar buscar por la jerarquia
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
            }
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

            // Validar turno en el cliente para evitar que la carta desaparezca si clickeamos cuando no es nuestro turno
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
            // Buscar sillas por nombre para evitar problemas de Tags no definidos
            GameObject chair1 = GameObject.Find("Silla1");
            GameObject chair2 = GameObject.Find("Silla2");
            
            GameObject closestChair = null;
            float minDistance = 10f; // Distancia máxima para sentarse (aumentada para que no tengas que caminar hasta la silla)

            if (chair1 != null)
            {
                float dist1 = Vector3.Distance(transform.position, chair1.transform.position);
                if (dist1 < minDistance) { minDistance = dist1; closestChair = chair1; }
            }
            if (chair2 != null)
            {
                float dist2 = Vector3.Distance(transform.position, chair2.transform.position);
                if (dist2 < minDistance) { minDistance = dist2; closestChair = chair2; }
            }

            if (closestChair != null)
            {
                // Para teletransportar un CharacterController, primero hay que apagarlo
                if (characterController != null) characterController.enabled = false;

                // Anclar jugador a la silla (Altura media para no tapar cartas pero no quedar arriba de la mesa)
                Vector3 toCenter = (new Vector3(0, closestChair.transform.position.y, 0) - closestChair.transform.position).normalized;
                transform.position = closestChair.transform.position + Vector3.up * 0.35f; 
                // Mirar al centro de la mesa
                transform.LookAt(new Vector3(0, transform.position.y, 0));
                
                isSeated = true;
                
                if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
                if (serverInstance != null) serverInstance.PlayerSitDownServerRpc();
                
                Debug.Log("Te has sentado en la mesa.");
            }
            else
            {
                Debug.LogWarning("Estás muy lejos de una silla.");
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
