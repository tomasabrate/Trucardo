using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Trucardo.Core.Rules;
using Trucardo.Core.UI;

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
        private PowerHandVisualizer opponentPowerVisualizer;
        private PowerSystem powerSystem;
        private readonly List<GameObject> localPowerInstances = new List<GameObject>();
        private readonly List<GameObject> opponentPowerInstances = new List<GameObject>();
        
        // Estado local para la UI
        private GameState localGameState;
        private ulong localTurnId;
        private int localCurrentRoundIndex;
        private int localP1Score;
        private int localP2Score;
        private TipoCanto localCantoPendiente;
        private EstadoEnvido localEstadoEnvido;
        private int localEnvidoAcceptedPoints;
        private int localPendingEnvidoPoints;
        private ulong localLastCallerId;
        private bool isBastonPresidencialActiveOnMe = false;
        private bool isPavaCalienteActive = false;
        private bool isLuzMalaActiveOnMe = false;
        private bool isFaconTargeting = false;
        private PowerVisual activeFaconVisual = null;
        private Coroutine activeRevealAndBreakRoutine = null;
        private bool isGuitarraTargeting = false;
        private int guitarraStep = 0;
        private int selectedGuitarraMyCardSlot = -1;
        private PowerVisual activeGuitarraVisual = null;
        private bool isGuampasTargeting = false;
        private int guampasDrawnCardIndex = -1;
        private GameServer serverInstance;
        private HUDController hudController;
        private bool interfaceMode;
        private bool gameOverUIActive;

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
            if (isSeated)
            {
                if (currentTableDefinition == null) StandUp();
                else CallEnvido(EnvidoCall.Envido);
            }
            else TrySitDown();
        }

        private static AudioClip cachedPowerStrumSound;

        private static AudioClip GetPowerStrumSound()
        {
            if (cachedPowerStrumSound == null)
            {
                cachedPowerStrumSound = Resources.Load<AudioClip>("Sonidos/Efectos/Rasgueo Para Poder");
            }
            return cachedPowerStrumSound;
        }

        private void Awake()
        {
            if (Application.targetFrameRate <= 0 || Application.targetFrameRate > 120)
            {
                Application.targetFrameRate = 60;
            }
        }

        public override void OnNetworkSpawn()
        {
            TableDefinition.EnsureSceneConfiguration();
            if (cachedPowerStrumSound == null)
            {
                cachedPowerStrumSound = Resources.Load<AudioClip>("Sonidos/Efectos/Rasgueo Para Poder");
            }
            if (powerCatalog == null)
            {
                powerCatalog = Resources.Load<PowerCatalog>("PowerCatalog");
            }
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

                hudController = HUDController.Instance;
                if (hudController != null)
                    hudController.AttachPlayer(this);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
                return;

            SetInterfaceMode(false);
            if (hudController != null)
                hudController.DetachPlayer(this);
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

            if (localCardInstances != null)
            {
                for (int i = 0; i < localCardInstances.Length; i++)
                {
                    if (localCardInstances[i] != null) Destroy(localCardInstances[i]);
                }
            }
            localCardInstances = new GameObject[3];

            HideRevealedOpponentCard();
            if (handVisualizer != null)
            {
                handVisualizer.ClearHand();
            }
            if (opponentHandVisualizer != null)
            {
                opponentHandVisualizer.ClearHand();
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

        public void SetupPowerVisuals()
        {
            // Limpiar referencias nulas de poderes usados en rondas anteriores
            localPowerInstances.RemoveAll(p => p == null);
            opponentPowerInstances.RemoveAll(p => p == null);
            if (powerHandVisualizer != null)
            {
                powerHandVisualizer.powersOnTable.RemoveAll(p => p == null);
            }
            if (opponentPowerVisualizer != null)
            {
                opponentPowerVisualizer.powersOnTable.RemoveAll(p => p == null);
            }

            // Limpiar la línea de tiempo de la UI para la nueva ronda
            CancelInteractivePowerTargeting();
            if (hudController != null)
            {
                hudController.ClearRoundPowers();
            }

            if (powerCatalog == null)
            {
                powerCatalog = Resources.Load<PowerCatalog>("PowerCatalog");
            }

            if (powerCatalog == null || powerCatalog.AvailablePowers == null || powerCatalog.AvailablePowers.Count == 0)
            {
                return;
            }

            if (powerHandVisualizer == null)
            {
                powerHandVisualizer = gameObject.AddComponent<PowerHandVisualizer>();
            }
            powerHandVisualizer.tableCenter = currentTable;
            powerHandVisualizer.isOpponent = false;

            if (opponentPowerVisualizer == null)
            {
                opponentPowerVisualizer = gameObject.AddComponent<PowerHandVisualizer>();
            }
            opponentPowerVisualizer.tableCenter = currentTable;
            opponentPowerVisualizer.isOpponent = true;

            if (powerSystem == null)
            {
                powerSystem = gameObject.AddComponent<PowerSystem>();
            }

            // Seleccionar 1 poder para el jugador local si tiene menos de 4 acumulados y no está boleado
            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            TableSession currentSession = null;
            if (serverInstance != null && NetworkManager.Singleton != null)
            {
                currentSession = serverInstance.GetSessionByPlayer(NetworkManager.Singleton.LocalClientId);
            }

            bool localBoleado = false;
            bool opponentBoleado = false;
            if (currentSession != null)
            {
                localBoleado = amIPlayer1 ? currentSession.P1Boleado : currentSession.P2Boleado;
                if (localBoleado)
                {
                    if (amIPlayer1) currentSession.P1Boleado = false;
                    else currentSession.P2Boleado = false;
                }

                opponentBoleado = amIPlayer1 ? currentSession.P2Boleado : currentSession.P1Boleado;
                if (opponentBoleado)
                {
                    if (amIPlayer1) currentSession.P2Boleado = false;
                    else currentSession.P1Boleado = false;
                }
            }

            if (localBoleado)
            {
                Debug.Log("<color=#795548><b>[Poder Boleadoras]</b></color> ¡Estás boleado! No recibes poder esta ronda.");
            }
            else if (localPowerInstances.Count < 4)
            {
                PowerData selectedPower = null;
                if (PowerAdminController.Instance != null && PowerAdminController.Instance.HasAdminPowerOverride)
                {
                    selectedPower = PowerAdminController.Instance.ConsumeAdminPowerOverride();
                }
                if (selectedPower == null)
                {
                    selectedPower = powerCatalog.GetRandomPower();
                }

                if (selectedPower != null && selectedPower.IsValid())
                {
                    SpawnPowerOnTable(selectedPower);
                }
            }

            // Si el oponente está boleado o recibe poderes, el servidor autoritativo sincroniza via SyncOpponentPowers.
            if (opponentBoleado)
            {
                Debug.Log("<color=#795548><b>[Poder Boleadoras]</b></color> El oponente está boleado. No recibe poder esta ronda.");
            }

            // Refrescar acomodo de cuadrícula en mesa
            if (powerHandVisualizer != null) powerHandVisualizer.RefreshLayout();
            if (opponentPowerVisualizer != null) opponentPowerVisualizer.RefreshLayout();
        }

        public void SpawnPowerOnTable(PowerData power)
        {
            if (power == null) return;

            if (powerHandVisualizer == null)
            {
                powerHandVisualizer = gameObject.AddComponent<PowerHandVisualizer>();
                powerHandVisualizer.isOpponent = false;
            }
            powerHandVisualizer.tableCenter = currentTable;

            if (powerSystem == null)
            {
                powerSystem = gameObject.AddComponent<PowerSystem>();
            }

            GameObject powerObject = power.WorldPrefab != null
                ? Instantiate(power.WorldPrefab, transform.position, transform.rotation)
                : GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            PowerVisual visual = powerObject.GetComponent<PowerVisual>();
            if (visual == null) visual = powerObject.AddComponent<PowerVisual>();

            visual.isOpponentPower = false;
            visual.SetPowerData(power);

            if (power.WorldPrefab == null)
            {
                powerObject.transform.localScale = new Vector3(0.08f, 0.045f, 0.08f);
            }

            powerHandVisualizer.AddPower(visual);
            localPowerInstances.Add(powerObject);
        }

        public void SpawnOpponentPowerOnTable(PowerData power)
        {
            if (power == null) return;

            if (opponentPowerVisualizer == null)
            {
                opponentPowerVisualizer = gameObject.AddComponent<PowerHandVisualizer>();
                opponentPowerVisualizer.isOpponent = true;
            }
            opponentPowerVisualizer.tableCenter = currentTable;

            GameObject powerObject = power.WorldPrefab != null
                ? Instantiate(power.WorldPrefab, transform.position, transform.rotation)
                : GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            PowerVisual visual = powerObject.GetComponent<PowerVisual>();
            if (visual == null) visual = powerObject.AddComponent<PowerVisual>();

            visual.isOpponentPower = true;
            visual.SetPowerData(power);

            if (power.WorldPrefab == null)
            {
                powerObject.transform.localScale = new Vector3(0.08f, 0.045f, 0.08f);
            }

            opponentPowerVisualizer.AddPower(visual);
            opponentPowerInstances.Add(powerObject);
        }

        public void SyncOpponentPowers(string[] botPowerIds)
        {
            if (botPowerIds == null) return;
            if (powerCatalog == null)
            {
                powerCatalog = Resources.Load<PowerCatalog>("PowerCatalog");
            }
            if (powerCatalog == null || powerCatalog.AvailablePowers == null) return;

            if (opponentPowerVisualizer == null)
            {
                opponentPowerVisualizer = gameObject.AddComponent<PowerHandVisualizer>();
                opponentPowerVisualizer.isOpponent = true;
            }
            opponentPowerVisualizer.tableCenter = currentTable;

            opponentPowerInstances.RemoveAll(p => p == null);

            var currentIds = new System.Collections.Generic.List<string>();
            foreach (var inst in opponentPowerInstances)
            {
                PowerVisual pv = inst.GetComponent<PowerVisual>();
                currentIds.Add(pv != null && pv.Data != null ? pv.Data.PowerId : "");
            }

            bool matches = currentIds.Count == botPowerIds.Length;
            if (matches)
            {
                for (int i = 0; i < botPowerIds.Length; i++)
                {
                    if (currentIds[i] != botPowerIds[i])
                    {
                        matches = false;
                        break;
                    }
                }
            }

            if (matches) return;

            if (opponentPowerVisualizer != null) opponentPowerVisualizer.ClearPowers();
            foreach (var p in opponentPowerInstances) if (p != null) Destroy(p);
            opponentPowerInstances.Clear();

            foreach (string pid in botPowerIds)
            {
                PowerData pData = powerCatalog.AvailablePowers.Find(p => p != null && p.PowerId == pid);
                if (pData != null && pData.IsValid())
                {
                    SpawnOpponentPowerOnTable(pData);
                }
            }

            if (opponentPowerVisualizer != null) opponentPowerVisualizer.RefreshLayout();
        }

        private Coroutine activeCardRevealCoroutine = null;
        private CardVisual currentlyRevealedCard = null;

        public void RevealOpponentCard(int cardIndex, CardData data)
        {
            if (opponentHandVisualizer == null || opponentHandVisualizer.cardsInHand == null || data == null) return;

            HideRevealedOpponentCard();

            foreach (var card in opponentHandVisualizer.cardsInHand)
            {
                if (card == null) continue;
                if (card.iconImage == null || !card.iconImage.enabled || card.iconImage.sprite == null)
                {
                    currentlyRevealedCard = card;
                    activeCardRevealCoroutine = StartCoroutine(RevealAndHideOpponentCardCoroutine(card, data));
                    break;
                }
            }
        }

        private IEnumerator RevealAndHideOpponentCardCoroutine(CardVisual card, CardData data)
        {
            if (card == null || data == null) yield break;

            Vector3 toTable = currentTable != null ? (currentTable.position - transform.position) : transform.forward;
            toTable.y = 0f;
            if (toTable.sqrMagnitude < 0.001f) toTable = transform.forward;
            toTable.Normalize();
            float yRot = Mathf.Atan2(toTable.x, toTable.z) * Mathf.Rad2Deg;

            Quaternion revealedRot = Quaternion.Euler(-35f, yRot, 0f);
            Quaternion hiddenRot = Quaternion.Euler(35f, yRot, 0f);

            if (opponentHandVisualizer != null)
            {
                opponentHandVisualizer.revealedCard = card;
                opponentHandVisualizer.revealedRotation = revealedRot;
            }

            card.SetCardData(data);
            if (card.iconImage != null) card.iconImage.enabled = true;
            if (card.textNumber != null && data.Number > 0) card.textNumber.text = data.Number.ToString();
            if (card.textNumberBottom != null && data.Number > 0) card.textNumberBottom.text = data.Number.ToString();

            card.AnimateTo(card.transform.position, revealedRot, 0.35f, false, 0.05f);

            int startRoundIndex = localCurrentRoundIndex;
            float timer = 5.0f;
            while (timer > 0f)
            {
                if (card == null) yield break;
                if (localCurrentRoundIndex != startRoundIndex || 
                    localGameState == GameState.RondaTerminada || 
                    localGameState == GameState.Repartiendo || 
                    localGameState == GameState.PartidaTerminada)
                {
                    break;
                }
                timer -= Time.deltaTime;
                yield return null;
            }

            if (card != null)
            {
                if (opponentHandVisualizer != null && opponentHandVisualizer.revealedCard == card)
                {
                    opponentHandVisualizer.revealedCard = null;
                }

                card.AnimateTo(card.transform.position, hiddenRot, 0.35f, false, 0.05f);
                yield return new WaitForSeconds(0.35f);
                if (card != null)
                {
                    if (card.iconImage != null) card.iconImage.enabled = false;
                    if (card.textNumber != null) card.textNumber.text = "";
                    if (card.textNumberBottom != null) card.textNumberBottom.text = "";
                }
            }

            if (currentlyRevealedCard == card) currentlyRevealedCard = null;
            activeCardRevealCoroutine = null;
        }

        public void HideRevealedOpponentCard()
        {
            if (activeCardRevealCoroutine != null)
            {
                StopCoroutine(activeCardRevealCoroutine);
                activeCardRevealCoroutine = null;
            }

            if (opponentHandVisualizer != null)
            {
                opponentHandVisualizer.revealedCard = null;
            }

            if (currentlyRevealedCard != null)
            {
                Vector3 toTable = currentTable != null ? (currentTable.position - transform.position) : transform.forward;
                toTable.y = 0f;
                if (toTable.sqrMagnitude < 0.001f) toTable = transform.forward;
                toTable.Normalize();
                float yRot = Mathf.Atan2(toTable.x, toTable.z) * Mathf.Rad2Deg;
                Quaternion hiddenRot = Quaternion.Euler(35f, yRot, 0f);

                currentlyRevealedCard.transform.rotation = hiddenRot;
                if (currentlyRevealedCard.iconImage != null) currentlyRevealedCard.iconImage.enabled = false;
                if (currentlyRevealedCard.textNumber != null) currentlyRevealedCard.textNumber.text = "";
                if (currentlyRevealedCard.textNumberBottom != null) currentlyRevealedCard.textNumberBottom.text = "";
                currentlyRevealedCard = null;
            }
        }

        public int[] GetCurrentCards()
        {
            return myCards != null ? (int[])myCards.Clone() : new int[0];
        }

        public void AdminSetCards(int[] cards)
        {
            if (cards == null || cards.Length < 3) return;
            ReceiveCards(cards);

            if (serverInstance != null)
            {
                var session = serverInstance.GetSessionByPlayer(NetworkManager.Singleton.LocalClientId);
                if (session != null && session.GameModel != null)
                {
                    List<int> hand = amIPlayer1 ? session.GameModel.P1Hand : session.GameModel.P2Hand;
                    hand.Clear();
                    hand.AddRange(cards);
                }
            }
        }

        public void AdminSetSlotCard(int slot, int cardIndex)
        {
            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance == null || serverInstance.masterDeck == null) return;
            if (cardIndex < 0 || cardIndex >= serverInstance.masterDeck.AvailableCards.Count) return;
            if (slot < 0 || slot >= 3) return;

            if (myCards == null || myCards.Length < 3)
            {
                myCards = new int[3] { -1, -1, -1 };
            }
            if (localCardInstances == null || localCardInstances.Length < 3)
            {
                localCardInstances = new GameObject[3];
            }

            myCards[slot] = cardIndex;

            // Sincronizar en el GameModel del servidor si existe
            var session = serverInstance.GetSessionByPlayer(NetworkManager.Singleton.LocalClientId);
            if (session != null && session.GameModel != null)
            {
                List<int> hand = amIPlayer1 ? session.GameModel.P1Hand : session.GameModel.P2Hand;
                while (hand.Count <= slot)
                {
                    hand.Add(-1);
                }
                hand[slot] = cardIndex;
            }

            // Actualizar representación visual 3D
            if (localCardInstances[slot] != null)
            {
                CardVisual visual = localCardInstances[slot].GetComponent<CardVisual>();
                if (visual != null)
                {
                    visual.SetCardData(serverInstance.masterDeck.AvailableCards[cardIndex]);
                    if (handVisualizer != null) handVisualizer.RefreshLayout();
                }
            }
            else if (cardPrefab != null)
            {
                GameObject c = Instantiate(cardPrefab, transform.position, transform.rotation);
                Destroy(c.GetComponent<Unity.Netcode.NetworkObject>());
                Destroy(c.GetComponent<Unity.Netcode.Components.NetworkTransform>());

                CardVisual visual = c.GetComponent<CardVisual>();
                if (visual != null)
                {
                    visual.SetCardData(serverInstance.masterDeck.AvailableCards[cardIndex]);
                    if (handVisualizer != null) handVisualizer.AddCardToHand(visual);
                }
                localCardInstances[slot] = c;
            }
        }

        public List<int> AdminGetOpponentCards()
        {
            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance == null) return new List<int>();

            var session = serverInstance.GetSessionByPlayer(NetworkManager.Singleton.LocalClientId);
            if (session != null && session.GameModel != null)
            {
                List<int> oppHand = amIPlayer1 ? session.GameModel.P2Hand : session.GameModel.P1Hand;
                return oppHand != null ? new List<int>(oppHand) : new List<int>();
            }
            return new List<int>();
        }

        public void AdminSetOpponentCards(int[] cards)
        {
            if (cards == null || cards.Length == 0) return;
            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance == null) return;

            var session = serverInstance.GetSessionByPlayer(NetworkManager.Singleton.LocalClientId);
            if (session != null && session.GameModel != null)
            {
                List<int> oppHand = amIPlayer1 ? session.GameModel.P2Hand : session.GameModel.P1Hand;
                oppHand.Clear();
                oppHand.AddRange(cards);
            }
        }

        public (int p1Score, int p2Score) AdminGetScores()
        {
            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance != null)
            {
                var session = serverInstance.GetSessionByPlayer(NetworkManager.Singleton.LocalClientId);
                if (session != null && session.GameModel != null)
                {
                    return (session.GameModel.ScoreTeam1, session.GameModel.ScoreTeam2);
                }
            }
            return (localP1Score, localP2Score);
        }

        public void AdminSetScores(int p1, int p2)
        {
            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance != null)
            {
                var session = serverInstance.GetSessionByPlayer(NetworkManager.Singleton.LocalClientId);
                if (session != null && session.GameModel != null)
                {
                    session.GameModel.ResetGame();
                    if (p1 > 0) session.GameModel.AddPoints(1, p1);
                    if (p2 > 0) session.GameModel.AddPoints(2, p2);
                    serverInstance.SyncClientState(session);
                }
            }
        }

        public void AddCardToPlayerHand(int cardIndex)
        {
            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance == null || serverInstance.masterDeck == null) return;
            if (cardIndex < 0 || cardIndex >= serverInstance.masterDeck.AvailableCards.Count) return;

            int targetSlot = -1;
            for (int i = 0; i < myCards.Length; i++)
            {
                if (myCards[i] == -1)
                {
                    targetSlot = i;
                    break;
                }
            }

            if (targetSlot == -1)
            {
                int oldLen = myCards.Length;
                System.Array.Resize(ref myCards, oldLen + 1);
                System.Array.Resize(ref localCardInstances, oldLen + 1);
                targetSlot = oldLen;
            }

            myCards[targetSlot] = cardIndex;

            if (cardPrefab != null)
            {
                GameObject c = Instantiate(cardPrefab, transform.position, transform.rotation);
                Destroy(c.GetComponent<Unity.Netcode.NetworkObject>());
                Destroy(c.GetComponent<Unity.Netcode.Components.NetworkTransform>());

                CardVisual visual = c.GetComponent<CardVisual>();
                if (visual != null)
                {
                    visual.SetCardData(serverInstance.masterDeck.AvailableCards[cardIndex]);
                    if (handVisualizer != null) handVisualizer.AddCardToHand(visual);
                }
                localCardInstances[targetSlot] = c;
            }
        }

        public void ReplaceCardInPlayerHand(int oldCardIndex, int newCardIndex)
        {
            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance == null || serverInstance.masterDeck == null) return;

            int slot = -1;
            for (int i = 0; i < myCards.Length; i++)
            {
                if (myCards[i] == oldCardIndex)
                {
                    slot = i;
                    break;
                }
            }
            if (slot == -1) return;

            myCards[slot] = newCardIndex;

            if (localCardInstances[slot] != null)
            {
                if (handVisualizer != null)
                {
                    CardVisual oldVisual = localCardInstances[slot].GetComponent<CardVisual>();
                    if (oldVisual != null) handVisualizer.RemoveCardFromHand(oldVisual);
                }
                Destroy(localCardInstances[slot]);
            }

            if (cardPrefab != null)
            {
                GameObject c = Instantiate(cardPrefab, transform.position, transform.rotation);
                Destroy(c.GetComponent<Unity.Netcode.NetworkObject>());
                Destroy(c.GetComponent<Unity.Netcode.Components.NetworkTransform>());

                CardVisual visual = c.GetComponent<CardVisual>();
                if (visual != null)
                {
                    visual.SetCardData(serverInstance.masterDeck.AvailableCards[newCardIndex]);
                    if (handVisualizer != null) handVisualizer.AddCardToHand(visual);
                }
                localCardInstances[slot] = c;
            }
        }

        public void ReplaceAllHandCards(int[] newCardIndices)
        {
            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance == null || serverInstance.masterDeck == null) return;

            if (localCardInstances != null)
            {
                for (int i = 0; i < localCardInstances.Length; i++)
                {
                    if (localCardInstances[i] != null)
                    {
                        if (handVisualizer != null)
                        {
                            CardVisual visual = localCardInstances[i].GetComponent<CardVisual>();
                            if (visual != null) handVisualizer.RemoveCardFromHand(visual);
                        }
                        Destroy(localCardInstances[i]);
                    }
                }
            }

            myCards = new int[newCardIndices.Length];
            localCardInstances = new GameObject[newCardIndices.Length];

            for (int i = 0; i < newCardIndices.Length; i++)
            {
                myCards[i] = newCardIndices[i];
                GameObject c = Instantiate(cardPrefab, transform.position, transform.rotation);
                Destroy(c.GetComponent<Unity.Netcode.NetworkObject>());
                Destroy(c.GetComponent<Unity.Netcode.Components.NetworkTransform>());

                CardVisual visual = c.GetComponent<CardVisual>();
                if (visual != null)
                {
                    visual.SetCardData(serverInstance.masterDeck.AvailableCards[newCardIndices[i]]);
                    if (isLuzMalaActiveOnMe)
                    {
                        visual.SetBlanked(true);
                    }
                    if (handVisualizer != null) handVisualizer.AddCardToHand(visual);
                }
                localCardInstances[i] = c;
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
                if (isSeated && !gameOverUIActive && Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
                    SetInterfaceMode(!interfaceMode);

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

                if (!interfaceMode)
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
            if (isSeated && interfaceMode) return;
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
            if (!isSeated || localGameState == GameState.PartidaTerminada) return;

            if (!interfaceMode && Mouse.current != null)
            {
                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    CancelInteractivePowerTargeting();
                }

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Ray ray = new Ray(headTransform.position, headTransform.forward);
                    if (Physics.Raycast(ray, out RaycastHit hit, 5f))
                    {
                        // 0. Si estamos en modo selección interactiva para Guampas
                        if (isGuampasTargeting)
                        {
                            CardVisual myCard = hit.collider.GetComponent<CardVisual>();
                            if (myCard == null) myCard = hit.collider.GetComponentInParent<CardVisual>();
                            if (myCard != null && handVisualizer != null && handVisualizer.cardsInHand.Contains(myCard))
                            {
                                int slot = handVisualizer.cardsInHand.IndexOf(myCard);
                                if (slot >= 0 && slot < myCards.Length)
                                {
                                    int cardToReplace = myCards[slot];
                                    isGuampasTargeting = false;
                                    guampasDrawnCardIndex = -1;
                                    if (handVisualizer != null)
                                    {
                                        handVisualizer.elevatedCard = null;
                                        handVisualizer.RefreshLayout();
                                    }
                                    if (hudController != null) hudController.HidePowerPrompt();
                                    AudioClip sound = GetPowerStrumSound();
                                    if (sound != null) AudioSource.PlayClipAtPoint(sound, transform.position, 1.0f);
                                    if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
                                    if (serverInstance != null) serverInstance.SubmitGuampasChoiceServerRpc(cardToReplace);
                                    return;
                                }
                            }
                            return;
                        }

                        // 1. Si estamos en modo selección de objetivo para Facón
                        if (isFaconTargeting)
                        {
                            CardVisual oppCard = hit.collider.GetComponent<CardVisual>();
                            if (oppCard == null) oppCard = hit.collider.GetComponentInParent<CardVisual>();
                            if (oppCard != null && opponentHandVisualizer != null && opponentHandVisualizer.cardsInHand.Contains(oppCard))
                            {
                                int oppSlot = opponentHandVisualizer.cardsInHand.IndexOf(oppCard);
                                if (oppSlot >= 0)
                                {
                                    isFaconTargeting = false;
                                    if (hudController != null) hudController.HidePowerPrompt();
                                    if (activeFaconVisual != null)
                                    {
                                        if (powerHandVisualizer != null) powerHandVisualizer.RemovePower(activeFaconVisual);
                                        localPowerInstances.Remove(activeFaconVisual.gameObject);
                                        Destroy(activeFaconVisual.gameObject);
                                        activeFaconVisual = null;
                                    }
                                    AudioClip sound = GetPowerStrumSound();
                                    if (sound != null) AudioSource.PlayClipAtPoint(sound, transform.position, 1.0f);
                                    if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
                                    if (serverInstance != null) serverInstance.PlayFaconWithTargetServerRpc(oppSlot);
                                    return;
                                }
                            }
                            return;
                        }

                        // 2. Si estamos en modo selección interactiva para Guitarra
                        if (isGuitarraTargeting)
                        {
                            if (guitarraStep == 1)
                            {
                                CardVisual myCard = hit.collider.GetComponent<CardVisual>();
                                if (myCard == null) myCard = hit.collider.GetComponentInParent<CardVisual>();
                                if (myCard != null && handVisualizer != null && handVisualizer.cardsInHand.Contains(myCard))
                                {
                                    int slot = handVisualizer.cardsInHand.IndexOf(myCard);
                                    if (slot >= 0)
                                    {
                                        selectedGuitarraMyCardSlot = slot;
                                        handVisualizer.elevatedCard = myCard;
                                        handVisualizer.RefreshLayout();
                                        guitarraStep = 2;
                                        if (hudController != null) hudController.ShowPowerPrompt("🎸 ¡PODER GUITARRA!", "2. Seleccioná una del rival para intercambiar\n<size=18>(Clic derecho para cancelar)</size>");
                                        return;
                                    }
                                }
                                return;
                            }
                            else if (guitarraStep == 2)
                            {
                                CardVisual cardCandidate = hit.collider.GetComponent<CardVisual>();
                                if (cardCandidate == null) cardCandidate = hit.collider.GetComponentInParent<CardVisual>();

                                if (cardCandidate != null && handVisualizer != null && handVisualizer.cardsInHand.Contains(cardCandidate))
                                {
                                    int slot = handVisualizer.cardsInHand.IndexOf(cardCandidate);
                                    if (slot >= 0)
                                    {
                                        selectedGuitarraMyCardSlot = slot;
                                        handVisualizer.elevatedCard = cardCandidate;
                                        handVisualizer.RefreshLayout();
                                        return;
                                    }
                                }

                                if (cardCandidate != null && opponentHandVisualizer != null && opponentHandVisualizer.cardsInHand.Contains(cardCandidate))
                                {
                                    int oppSlot = opponentHandVisualizer.cardsInHand.IndexOf(cardCandidate);
                                    if (oppSlot >= 0 && selectedGuitarraMyCardSlot >= 0 && selectedGuitarraMyCardSlot < myCards.Length)
                                    {
                                        int myCardIndex = myCards[selectedGuitarraMyCardSlot];
                                        isGuitarraTargeting = false;
                                        guitarraStep = 0;
                                        if (handVisualizer != null)
                                        {
                                            handVisualizer.elevatedCard = null;
                                            handVisualizer.RefreshLayout();
                                        }
                                        if (hudController != null) hudController.HidePowerPrompt();
                                        if (activeGuitarraVisual != null)
                                        {
                                            if (powerHandVisualizer != null) powerHandVisualizer.RemovePower(activeGuitarraVisual);
                                            localPowerInstances.Remove(activeGuitarraVisual.gameObject);
                                            Destroy(activeGuitarraVisual.gameObject);
                                            activeGuitarraVisual = null;
                                        }
                                        AudioClip sound = GetPowerStrumSound();
                                        if (sound != null) AudioSource.PlayClipAtPoint(sound, transform.position, 1.0f);
                                        if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
                                        if (serverInstance != null) serverInstance.PlayGuitarraWithTargetServerRpc(myCardIndex, oppSlot);
                                        return;
                                    }
                                }
                                return;
                            }
                        }

                        // 3. Clic normal en poder propio sobre la mesa
                        PowerVisual powerVisual = hit.collider.GetComponent<PowerVisual>();
                        if (powerVisual == null) powerVisual = hit.collider.GetComponentInParent<PowerVisual>();

                        if (powerVisual != null && powerHandVisualizer != null
                            && powerHandVisualizer.powersOnTable.Contains(powerVisual))
                        {
                            PlayPower(powerVisual);
                            return;
                        }

                        // 4. Clic normal en carta propia para jugar a la mesa
                        CardVisual visual = hit.collider.GetComponent<CardVisual>();
                        if (visual == null) visual = hit.collider.GetComponentInParent<CardVisual>();
                        
                        if (visual != null && handVisualizer != null && handVisualizer.cardsInHand.Contains(visual))
                        {
                            int originalIndex = -1;
                            for (int i = 0; i < localCardInstances.Length; i++)
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
                            for (int i = 0; i < localCardInstances.Length; i++)
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
                if (Keyboard.current.digit4Key.wasPressedThisFrame && myCards.Length > 3) PlayCard(3);

                if (Keyboard.current.rKey.wasPressedThisFrame) CallEnvido(EnvidoCall.RealEnvido);
                if (Keyboard.current.fKey.wasPressedThisFrame) CallEnvido(EnvidoCall.FaltaEnvido);
                if (Keyboard.current.qKey.wasPressedThisFrame) CallAction("Truco");
                if (Keyboard.current.aKey.wasPressedThisFrame) CallAction("Accept");
                if (Keyboard.current.nKey.wasPressedThisFrame)
                {
                    if (isBastonPresidencialActiveOnMe)
                    {
                        if (hudController != null) hudController.ShowToast("BASTÓN PRESIDENCIAL", "¡No puedes rechazar bajo la orden del Bastón Presidencial!");
                    }
                    else
                    {
                        CallAction("Decline");
                    }
                }
                if (Keyboard.current.pKey.wasPressedThisFrame) StandUp();
            }
        }

                private void StandUp()
        {
            if (!isSeated) return;
            CancelInteractivePowerTargeting();
            DismissGuampasChoice();
            DismissGuitarraChoice();
            HideRevealedOpponentCard();
            if (activeRevealAndBreakRoutine != null)
            {
                StopCoroutine(activeRevealAndBreakRoutine);
                activeRevealAndBreakRoutine = null;
            }

            if (powerHandVisualizer != null) powerHandVisualizer.ClearPowers();
            if (opponentPowerVisualizer != null) opponentPowerVisualizer.ClearPowers();
            foreach (var p in localPowerInstances) if (p != null) Destroy(p);
            foreach (var p in opponentPowerInstances) if (p != null) Destroy(p);
            localPowerInstances.Clear();
            opponentPowerInstances.Clear();

            // Limpieza estricta de cartas propias y del rival al levantarse de la mesa
            if (handVisualizer != null) handVisualizer.ClearHand();
            if (opponentHandVisualizer != null) opponentHandVisualizer.ClearHand();
            if (localCardInstances != null)
            {
                for (int i = 0; i < localCardInstances.Length; i++)
                {
                    if (localCardInstances[i] != null) Destroy(localCardInstances[i]);
                }
            }
            localCardInstances = new GameObject[3];
            myCards = new int[0];

            gameOverUIActive = false;
            isSeated = false;
                    localGameState = GameState.EsperandoJugadores;
                    localTurnId = ulong.MaxValue;
                    localCurrentRoundIndex = 0;
                    localP1Score = 0;
                    localP2Score = 0;
                    localCantoPendiente = TipoCanto.Ninguno;
                    localEstadoTruco = NivelTruco.Ninguno;
                    localEstadoEnvido = EstadoEnvido.Ninguno;
                    if (hudController != null)
                        hudController.SetPlayerSeated(false);
                    SetInterfaceMode(false);
            
            if (currentSeatInteractable != null)
            {
                if (currentSeatInteractable.CurrentOccupant == gameObject)
                {
                    currentSeatInteractable.CurrentOccupant = null;
                }
                currentSeatInteractable = null;
            }

            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance != null && currentTableDefinition != null) serverInstance.PlayerStandUpServerRpc();
            currentTableDefinition = null;
            currentTable = null;

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

        public void CallTrucoFromHud()
        {
            if (!IsOwner || !isSeated || !interfaceMode) return;
            CallAction("Truco");
        }

        public void CallEnvidoFromHud(EnvidoCall call)
        {
            if (!IsOwner || !isSeated || !interfaceMode) return;
            CallEnvido(call);
        }

        public void RespondToCallFromHud(bool accept)
        {
            if (!IsOwner || !isSeated || !interfaceMode) return;
            if (!accept && isBastonPresidencialActiveOnMe)
            {
                if (hudController != null) hudController.ShowToast("BASTÓN PRESIDENCIAL", "¡No puedes rechazar bajo la orden del Bastón Presidencial!");
                return;
            }
            CallAction(accept ? "Accept" : "Decline");
        }

        public void RestartMatchFromHud()
        {
            if (!IsOwner || !isSeated || !gameOverUIActive)
                return;

            if (serverInstance == null)
                serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance == null)
            {
                Debug.LogError("No se encontró GameServer para reiniciar la partida.", this);
                return;
            }

            serverInstance.RestartMatchServerRpc();
        }

        public void StandUpFromEndGameHud()
        {
            if (!IsOwner || !isSeated || !gameOverUIActive)
                return;

            StandUp();
        }

        private bool CanCallEnvidoLocally(EnvidoCall call)
        {
            int playedCardCount = 0;
            for (int i = 0; i < myCards.Length; i++)
                if (myCards[i] == -1) playedCardCount++;
            return EnvidoRules.CanCallFromContext(
                localGameState,
                localCantoPendiente,
                localLastCallerId,
                NetworkManager.Singleton.LocalClientId,
                localCurrentRoundIndex,
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

        public void OnPowerClicked(PowerVisual visual)
        {
            PlayPower(visual);
        }

        private void PlayPower(PowerVisual visual)
        {
            if (visual == null || visual.Data == null) return;

            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance == null) return;

            var session = serverInstance.GetSessionByPlayer(NetworkManager.Singleton.LocalClientId);

            if (visual.Data.Effect != null)
            {
                PowerContext context = new PowerContext(
                    NetworkManager.Singleton.LocalClientId,
                    amIPlayer1 ? 1 : 2,
                    gameObject,
                    null,
                    session != null ? session.GameModel : null);

                if (!visual.Data.Effect.CanExecute(context, out string failReason))
                {
                    if (hudController != null)
                    {
                        hudController.ShowToast(visual.Data.DisplayName.ToUpper(), failReason);
                    }
                    Debug.LogWarning($"[Power] No se puede ejecutar {visual.Data.DisplayName}: {failReason}");
                    return;
                }
            }

            // Flujo táctil 3D para Facón
            if (visual.Data.PowerId == "facon")
            {
                CancelInteractivePowerTargeting();
                isFaconTargeting = true;
                activeFaconVisual = visual;
                if (hudController != null)
                {
                    hudController.ShowPowerPrompt("🔪 ¡PODER FACÓN!", "Seleccioná una carta del rival para destrozarla\n<size=18>(Clic derecho para cancelar)</size>");
                }
                return;
            }

            // Flujo táctil 3D para Guitarra
            if (visual.Data.PowerId == "guitarra")
            {
                CancelInteractivePowerTargeting();
                isGuitarraTargeting = true;
                guitarraStep = 1;
                selectedGuitarraMyCardSlot = -1;
                activeGuitarraVisual = visual;
                if (hudController != null)
                {
                    hudController.ShowPowerPrompt("🎸 ¡PODER GUITARRA!", "1. Seleccioná una de tus cartas para cambiar\n<size=18>(Clic derecho para cancelar)</size>");
                }
                return;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                serverInstance.PlayPowerServerRpc(visual.Data.PowerId);
            }

            AudioClip sound = GetPowerStrumSound();
            if (sound != null)
            {
                AudioSource.PlayClipAtPoint(sound, transform.position, 1.0f);
            }

            if (powerHandVisualizer != null)
            {
                powerHandVisualizer.RemovePower(visual);
            }
            localPowerInstances.Remove(visual.gameObject);
            Destroy(visual.gameObject);
        }

        public void OnPowerActivatedNotification(ulong actorId, string powerId)
        {
            if (powerCatalog == null)
            {
                powerCatalog = Resources.Load<PowerCatalog>("PowerCatalog");
            }

            PowerData powerData = (powerCatalog != null && powerCatalog.AvailablePowers != null)
                ? powerCatalog.AvailablePowers.Find(p => p != null && p.PowerId == powerId)
                : null;

            if (powerData == null) return;

            bool isLocal = (NetworkManager.Singleton != null && actorId == NetworkManager.Singleton.LocalClientId);

            // Registrar cronológicamente en la línea de tiempo de la ronda
            if (hudController != null)
            {
                hudController.AddPowerToRoundTimeline(powerData, isLocal);
            }

            if (isLocal)
            {
                Debug.Log($"<color=#4CAF50><b>[Poder Propio]</b></color> ¡Activaste <b>{powerData.DisplayName}</b>!");
            }
            else
            {
                // Remover el objeto 3D del poder rival específico de la mesa
                if (opponentPowerVisualizer != null && opponentPowerInstances.Count > 0)
                {
                    GameObject toRemove = opponentPowerInstances.Find(go => {
                        if (go == null) return false;
                        PowerVisual pv = go.GetComponent<PowerVisual>();
                        return pv != null && pv.Data != null && pv.Data.PowerId == powerId;
                    });

                    if (toRemove == null)
                    {
                        toRemove = opponentPowerInstances[0];
                    }

                    if (toRemove != null)
                    {
                        opponentPowerInstances.Remove(toRemove);
                        PowerVisual pv = toRemove.GetComponent<PowerVisual>();
                        if (pv != null) opponentPowerVisualizer.RemovePower(pv);
                        Destroy(toRemove);
                    }

                    opponentPowerVisualizer.RefreshLayout();
                }

                AudioClip sound = GetPowerStrumSound();
                if (sound != null)
                {
                    AudioSource.PlayClipAtPoint(sound, transform.position, 1.0f);
                }

                Debug.Log($"<color=#FF5722><b>[Poder Rival]</b></color> ¡El oponente activó <b>{powerData.DisplayName}</b>!");
            }
        }

        public void AdminTestActivatePower(PowerData power, bool isLocal)
        {
            if (power == null) return;
            if (hudController == null) hudController = HUDController.Instance;
            if (hudController == null) return;

            if (isLocal)
            {
                hudController.SetLocalPlayerActivePower(power);
            }
            else
            {
                hudController.SetOpponentActivePower(power);
            }
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

                // En modo individual o si hay bots en la escena (y NO en multijugador), Banco B siempre está reservado para el bot
                bool isMultiplayer = Trucardo.Core.NetworkUI.selectedMode == GameMode.Host 
                    || Trucardo.Core.NetworkUI.selectedMode == GameMode.Client;

                if (!isMultiplayer && (Trucardo.Core.NetworkUI.selectedMode == GameMode.Singleplayer 
                    || FindObjectsByType<BotMarker>(FindObjectsSortMode.None).Length > 0))
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

                if (characterController != null) characterController.enabled = false;

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
                SetInterfaceMode(false);

                if (currentTableDefinition != null)
                {
                    // Asiento de mesa de juego (Truco)
                    currentTable = currentTableDefinition.transform;

                    Vector3 toTable = currentTable.position - seatTransform.position;
                    toTable.y = 0;
                    transform.rotation = toTable.sqrMagnitude > 0.001f 
                        ? Quaternion.LookRotation(toTable) 
                        : Quaternion.Euler(0, seatTransform.eulerAngles.y, 0);

                    if (hudController != null)
                        hudController.SetPlayerSeated(true);

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

                    if (powerHandVisualizer != null)
                    {
                        powerHandVisualizer.tableCenter = currentTable;
                        powerHandVisualizer.RefreshLayout();
                    }

                    if (opponentPowerVisualizer != null)
                    {
                        opponentPowerVisualizer.tableCenter = currentTable;
                        opponentPowerVisualizer.RefreshLayout();
                    }

                    if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
                    if (serverInstance != null) serverInstance.PlayerSitDownServerRpc(currentTableDefinition.TableId);

                    Debug.Log($"Te has sentado en la mesa {currentTableDefinition.TableId}.");
                }
                else
                {
                    // Asiento especial / decorativo (barra, afuera, etc.)
                    // Permite sentarse normalmente pero NO inicia partida ni conecta a ninguna mesa.
                    currentTable = null;
                    transform.rotation = Quaternion.Euler(0, seatTransform.eulerAngles.y, 0);

                    if (hudController != null)
                        hudController.SetPlayerSeated(false);

                    // Asegurar que ningún visualizador apunte a mesas anteriores
                    if (handVisualizer != null) handVisualizer.ClearHand();
                    if (opponentHandVisualizer != null) opponentHandVisualizer.ClearHand();
                    if (powerHandVisualizer != null)
                    {
                        powerHandVisualizer.ClearPowers();
                        powerHandVisualizer.tableCenter = null;
                    }
                    if (opponentPowerVisualizer != null)
                    {
                        opponentPowerVisualizer.ClearPowers();
                        opponentPowerVisualizer.tableCenter = null;
                    }

                    Debug.Log($"Te has sentado en el banco {seatTransform.name} (asiento de descanso, no inicia partida).");
                }
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
                if (cardToRemove == currentlyRevealedCard)
                {
                    HideRevealedOpponentCard();
                }
                opponentHandVisualizer.RemoveCardFromHand(cardToRemove);
                Destroy(cardToRemove.gameObject);
            }
        }

        private bool amIPlayer1 = true;

        public void UpdateUIState(
            GameState state,
            ulong turnId,
            int currentRoundIndex,
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
            if (currentRoundIndex != localCurrentRoundIndex ||
                state == GameState.RondaTerminada ||
                state == GameState.Repartiendo ||
                state == GameState.PartidaTerminada)
            {
                HideRevealedOpponentCard();
            }

            localGameState = state;
            localTurnId = turnId;
            localCurrentRoundIndex = currentRoundIndex;
            localP1Score = p1Score;
            localP2Score = p2Score;
            localCantoPendiente = canto;
            localLastCallerId = lastCaller;
            localEstadoTruco = estadoTruco;
            localEstadoEnvido = estadoEnvido;
            localEnvidoAcceptedPoints = envidoAcceptedPoints;
            localPendingEnvidoPoints = pendingEnvidoPoints;
            amIPlayer1 = (NetworkManager.Singleton.LocalClientId == p1Id);
            RefreshHUD();
        }

        public void SyncPowerStates(bool pavaCaliente, bool forcedAccept, ulong forcedCallerId, bool p1Luz, bool p2Luz)
        {
            isPavaCalienteActive = pavaCaliente;
            isBastonPresidencialActiveOnMe = forcedAccept && (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId != forcedCallerId);
            bool newLuzMala = amIPlayer1 ? p1Luz : p2Luz;

            if (hudController != null)
            {
                hudController.SetPavaCaliente(pavaCaliente);
                hudController.SetBastonPresidencial(isBastonPresidencialActiveOnMe);
            }

            if (newLuzMala != isLuzMalaActiveOnMe)
            {
                isLuzMalaActiveOnMe = newLuzMala;
                ApplyLuzMalaVisuals(isLuzMalaActiveOnMe);
            }
        }

        private void ApplyLuzMalaVisuals(bool active)
        {
            for (int i = 0; i < localCardInstances.Length; i++)
            {
                if (localCardInstances[i] != null)
                {
                    CardVisual visual = localCardInstances[i].GetComponent<CardVisual>();
                    if (visual != null)
                    {
                        visual.SetBlanked(active);
                    }
                }
            }
        }

        private void SetInterfaceMode(bool enabled)
        {
            interfaceMode = isSeated && enabled;
            Cursor.lockState = interfaceMode ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = interfaceMode;
            if (hudController != null)
                hudController.SetInterfaceMode(interfaceMode);
            RefreshHUD();
        }

        private void RefreshHUD()
        {
            if (!IsOwner || hudController == null)
                return;

            bool matchFinished = localGameState == GameState.PartidaTerminada;
            if (matchFinished && isSeated && !gameOverUIActive)
            {
                gameOverUIActive = true;
                interfaceMode = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                hudController.SetInterfaceMode(true);
            }
            else if (!matchFinished)
            {
                gameOverUIActive = false;
            }

            ulong localClientId = NetworkManager.Singleton != null
                ? NetworkManager.Singleton.LocalClientId
                : ulong.MaxValue;
            bool localTurn = localGameState == GameState.TurnoJugador && localTurnId == localClientId;
            bool responseRequired = localGameState == GameState.EsperandoRespuestaCanto
                && localLastCallerId != localClientId;
            string turnMessage = "";

            if (localGameState == GameState.TurnoJugador)
                turnMessage = localTurn ? "¡ES TU TURNO!" : "¡TURNO DEL RIVAL!";
            else if (localGameState == GameState.EsperandoRespuestaCanto)
                turnMessage = responseRequired ? "¡ES TU TURNO!" : "¡TURNO DEL RIVAL!";
            else if (localGameState == GameState.RondaTerminada || localGameState == GameState.Repartiendo)
                turnMessage = "REPARTIENDO...";

            bool hasNetwork = NetworkManager.Singleton != null;
            bool canCallEnvido = hasNetwork && CanCallEnvidoLocally(EnvidoCall.Envido);
            bool canCallRealEnvido = hasNetwork && CanCallEnvidoLocally(EnvidoCall.RealEnvido);
            bool canCallFaltaEnvido = hasNetwork && CanCallEnvidoLocally(EnvidoCall.FaltaEnvido);
            int myScore = amIPlayer1 ? localP1Score : localP2Score;
            int opponentScore = amIPlayer1 ? localP2Score : localP1Score;

            hudController.Refresh(
                turnMessage,
                myScore,
                opponentScore,
                localTurn,
                responseRequired,
                canCallEnvido,
                canCallRealEnvido,
                canCallFaltaEnvido,
                matchFinished,
                myScore >= 30);
        }

        private void OnGUI()
        {
            if (!IsOwner || !isSeated || currentTableDefinition == null) return;

            // Indicador de nivel de Truco
            GUI.skin.label.fontSize = 20;
            GUI.skin.label.alignment = TextAnchor.UpperCenter;
            int trucoPts = (localEstadoTruco == NivelTruco.Ninguno) ? 1 : (int)localEstadoTruco;
            GUI.contentColor = new Color(0.8f, 0.8f, 1f);
            GUI.Label(new Rect(0, 10, Screen.width, 30), $"Mano en Juego: {trucoPts} punto(s) ({(localEstadoTruco == NivelTruco.Ninguno ? "Simple" : localEstadoTruco.ToString())})");

            if (localGameState == GameState.EsperandoRespuestaCanto)
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
            
            GUI.contentColor = Color.white;
            GUI.skin.label.fontSize = 18;
            GUI.skin.label.alignment = TextAnchor.LowerLeft;
            GUI.Label(new Rect(20, Screen.height - 145, 500, 145), "Controles:\n[1, 2, 3] Jugar Carta\n[E] Envido - [R] Real Envido - [F] Falta Envido\n[Q] Truco - [A] Quiero - [N] No Quiero\n[Tab] Alternar cámara / interfaz");

            // Crosshair
            GUI.skin.label.fontSize = 20; // Mas chico a pedido del jugador
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(Screen.width / 2f - 25f, Screen.height / 2f - 25f, 50f, 50f), "•");
        }

        public void PromptGuampasChoice(int drawnCardIndex, int[] handIndices)
        {
            isGuampasTargeting = true;
            guampasDrawnCardIndex = drawnCardIndex;

            string cardName = $"Carta #{drawnCardIndex}";
            int rank = 0;
            int envido = 0;
            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance != null && serverInstance.masterDeck != null && drawnCardIndex >= 0 && drawnCardIndex < serverInstance.masterDeck.AvailableCards.Count)
            {
                CardData c = serverInstance.masterDeck.AvailableCards[drawnCardIndex];
                cardName = $"{c.Number} de {c.Suit}";
                rank = c.TrucoRank;
                envido = c.EnvidoValue;
            }

            if (hudController != null)
            {
                hudController.ShowPowerPrompt(
                    "🐃 ¡PODER GUAMPAS!",
                    $"Robaste: <b>{cardName}</b> (Fuerza Truco: {rank} | Envido: {envido})\n• <b>Clic en una carta tuya</b> para reemplazarla  • <b>Clic derecho</b> para conservar tu mano"
                );
            }
        }

        public void DismissGuampasChoice()
        {
            isGuampasTargeting = false;
            guampasDrawnCardIndex = -1;
            if (handVisualizer != null)
            {
                handVisualizer.elevatedCard = null;
                handVisualizer.RefreshLayout();
            }
            if (hudController != null)
            {
                hudController.HidePowerPrompt();
            }
        }

        public void PromptGuitarraChoice(int[] myHandCards, int[] opponentSlots)
        {
            // La Guitarra se opera de forma táctil e interactiva en 3D directamente sobre las cartas.
        }

        public void DismissGuitarraChoice()
        {
            CancelInteractivePowerTargeting();
        }

        public void CancelInteractivePowerTargeting()
        {
            if (isGuampasTargeting)
            {
                isGuampasTargeting = false;
                guampasDrawnCardIndex = -1;
                if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
                if (serverInstance != null)
                {
                    serverInstance.SubmitGuampasChoiceServerRpc(-1);
                }
            }

            isFaconTargeting = false;
            activeFaconVisual = null;
            isGuitarraTargeting = false;
            guitarraStep = 0;
            selectedGuitarraMyCardSlot = -1;
            activeGuitarraVisual = null;
            if (handVisualizer != null)
            {
                handVisualizer.elevatedCard = null;
                handVisualizer.RefreshLayout();
            }
            if (hudController != null)
            {
                hudController.HidePowerPrompt();
            }
        }

        public void RevealAndBreakOpponentCard(int slotIndex, int destroyedCardIndex, int newCardIndex)
        {
            if (activeRevealAndBreakRoutine != null)
            {
                StopCoroutine(activeRevealAndBreakRoutine);
            }
            activeRevealAndBreakRoutine = StartCoroutine(RevealAndBreakOpponentCardRoutine(slotIndex, destroyedCardIndex, newCardIndex));
        }

        private IEnumerator RevealAndBreakOpponentCardRoutine(int slotIndex, int destroyedCardIndex, int newCardIndex)
        {
            if (!isSeated) yield break;
            if (opponentHandVisualizer == null || opponentHandVisualizer.cardsInHand == null) yield break;
            if (slotIndex < 0 || slotIndex >= opponentHandVisualizer.cardsInHand.Count) yield break;

            CardVisual card = opponentHandVisualizer.cardsInHand[slotIndex];
            if (card == null) yield break;

            if (serverInstance == null) serverInstance = FindAnyObjectByType<GameServer>();
            if (serverInstance != null && serverInstance.masterDeck != null && destroyedCardIndex >= 0 && destroyedCardIndex < serverInstance.masterDeck.AvailableCards.Count)
            {
                CardData destroyedData = serverInstance.masterDeck.AvailableCards[destroyedCardIndex];

                Vector3 toTable = currentTable != null ? (currentTable.position - transform.position) : transform.forward;
                toTable.y = 0f;
                if (toTable.sqrMagnitude < 0.001f) toTable = transform.forward;
                toTable.Normalize();
                float yRot = Mathf.Atan2(toTable.x, toTable.z) * Mathf.Rad2Deg;

                Quaternion revealedRot = Quaternion.Euler(-35f, yRot, 0f);
                card.SetCardData(destroyedData);
                if (card.iconImage != null) card.iconImage.enabled = true;
                if (card.textNumber != null && destroyedData.Number > 0) card.textNumber.text = destroyedData.Number.ToString();
                if (card.textNumberBottom != null && destroyedData.Number > 0) card.textNumberBottom.text = destroyedData.Number.ToString();

                card.AnimateTo(card.transform.position, revealedRot, 0.3f, false, 0.05f);
                yield return new WaitForSeconds(1.2f);
            }

            if (!isSeated) yield break;

            if (card != null && opponentHandVisualizer != null)
            {
                opponentHandVisualizer.RemoveCardFromHand(card);
                Destroy(card.gameObject);
            }

            // Si recibió una nueva carta, reponer dorso blanco en la mano del rival
            if (newCardIndex != -1 && cardPrefab != null && opponentHandVisualizer != null)
            {
                GameObject oc = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
                Destroy(oc.GetComponent<Unity.Netcode.NetworkObject>());
                Destroy(oc.GetComponent<Unity.Netcode.Components.NetworkTransform>());

                CardVisual oppVisual = oc.GetComponent<CardVisual>();
                if (oppVisual != null)
                {
                    if (oppVisual.textNumber != null) oppVisual.textNumber.text = "";
                    if (oppVisual.textNumberBottom != null) oppVisual.textNumberBottom.text = "";
                    if (oppVisual.iconImage != null) oppVisual.iconImage.enabled = false;
                    opponentHandVisualizer.AddCardToHand(oppVisual);
                }
            }

            if (opponentHandVisualizer != null) opponentHandVisualizer.RefreshLayout();
            activeRevealAndBreakRoutine = null;
        }
    }
}
