using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using Trucardo.Core.Bots;
using Trucardo.Core.Rules;
using Trucardo.Core.BotDialogue;

namespace Trucardo.Core
{
    public enum ParticipantType
    {
        None,
        Human,
        Bot
    }

    public class TableSession
    {
        public string TableId;
        public TableDefinition Table;
        public Vector3 TablePosition;
        public GameModel GameModel;
        public ulong Player1Id = ulong.MaxValue;
        public ulong Player2Id = ulong.MaxValue;
        public bool Player1Seated = false;
        public bool Player2Seated = false;
        public ParticipantType Player1Type = ParticipantType.None;
        public ParticipantType Player2Type = ParticipantType.None;
        public BotController Player2BotController;
        public BotDialogueController Player2Dialogue;
        public BotPersonality DialoguePersonality;
        public Coroutine PendingBotActionCoroutine;
        public Coroutine PendingStartRoundCoroutine;
        public List<GameObject> TableCards = new List<GameObject>();
        public bool IsStartingRound = false;

        // Estados de Poderes en la Mesa
        public bool ForcedAcceptNextCanto = false;
        public ulong ForcedAcceptCallerId = 0;
        public bool PavaCalienteActive = false;
        public ulong PavaCalienteCasterId = 0;
        public Coroutine PavaTurnTimeoutCoroutine = null;
        public bool P1Boleado = false;
        public bool P2Boleado = false;
        public bool P1LuzMalaActive = false;
        public bool P2LuzMalaActive = false;
        public string P1ActivePowerId = null;
        public string P2ActivePowerId = null;
        public bool BotPowerUsedThisRound = false;
        public ulong GuampasPendingPlayerId = 0;
        public int GuampasDrawnCardIndex = -1;
        public HashSet<int> P1RevealedOpponentCards = new HashSet<int>();
        public HashSet<int> P2RevealedOpponentCards = new HashSet<int>();
        public ulong GuitarraPendingPlayerId = 0;
        public System.Collections.Generic.List<string> BotAvailablePowerIds = new System.Collections.Generic.List<string>();
    }

    public partial class GameServer : NetworkBehaviour
    {
        private const ulong FirstLogicalBotId = ulong.MaxValue - 1;

        [Header("Datos Generales")]
        public DeckData masterDeck;
        [SerializeField] private bool autoFillPlayer2WithBot = true;
        [SerializeField, Min(0f)] private float botActionDelaySeconds = 0.75f;
        [SerializeField] private BotDifficulty defaultBotDifficulty = BotDifficulty.Normal;
        [SerializeField] private BotPersonality defaultBotPersonality = BotPersonality.Balanced;
        [SerializeField] private bool useFixedBotSeed = false;
        [SerializeField] private int botSeed = 12345;
        [SerializeField] private bool logBotDecisions = false;
        [Header("Cartas")]
        public GameObject cardPrefab;
        [Header("Bots")]
        public GameObject botPrefab;
        public GameObject bartenderPrefab;
        public float botYOffset = 0.35f; // Ajuste vertical
        public float botForwardOffset = 0.15f; // Ajuste hacia adelante
        public Transform bartenderPosition; // Asignar un objeto vacío en la barra

        private List<TableSession> sessions = new List<TableSession>();
        private ulong nextLogicalBotId = FirstLogicalBotId;

        public override void OnNetworkSpawn()
        {
            TableDefinition.EnsureSceneConfiguration();
            if (!IsServer)
            {
                enabled = false;
                return;
            }
            Debug.Log("Servidor de Trucardo Iniciado. Soportando múltiples mesas.");
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;

            bool isSingleplayer = ShouldAutoFillBot();

            if (isSingleplayer && botPrefab != null)
            {
                SpawnVisualBots();
            }
        }

        private void SpawnVisualBots()
        {
            if (botPrefab == null)
            {
#if UNITY_EDITOR
                botPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Core/Prefabs/BotPrefab.prefab");
#endif
            }
            var tables = TableDefinition.FindAll();
            foreach (var table in tables)
            {
                // Ignorar la mesa 9
                if (table.TableId == "Mesa_9" || table.TableId.Contains("9"))
                    continue;

                if (table.Seats != null && table.Seats.Count >= 2)
                {
                    var seat = table.Seats[1].transform; // Banquito B
                    
                    Vector3 toTable = table.transform.position - seat.position;
                    toTable.y = 0;
                    Quaternion botRot = toTable.sqrMagnitude > 0.001f 
                        ? Quaternion.LookRotation(toTable) 
                        : seat.rotation;
                    
                    Vector3 forwardDir = toTable.sqrMagnitude > 0.001f 
                        ? toTable.normalized 
                        : seat.forward;

                    Vector3 spawnPos = seat.position + (Vector3.up * botYOffset) + (forwardDir * botForwardOffset);
                    GameObject botInst = Instantiate(botPrefab, spawnPos, botRot);
                    botInst.name = $"Bot {table.TableId.Replace("_", " ")}";
                    
                    CleanBotComponents(botInst);

                    SeatInteractable seatComp = seat.GetComponent<SeatInteractable>();
                    if (seatComp != null)
                    {
                        seatComp.CurrentOccupant = botInst;
                    }
                    var botMarker = botInst.AddComponent<BotMarker>();
                    botMarker.assignedSeat = seatComp;

                    Animator anim = botInst.GetComponentInChildren<Animator>();
                    if (anim != null)
                    {
                        anim.SetBool("IsSeated", true);
                        anim.Play("Seated", 0, 0f);
                    }
                }
            }

            // Spawn Bartender
            Transform barTransform = bartenderPosition != null ? bartenderPosition : null;
            if (barTransform == null)
            {
                GameObject posObj = GameObject.Find("PosicionBartender");
                if (posObj != null) barTransform = posObj.transform;
            }

            Vector3 bartenderPos = Vector3.zero;
            Quaternion bartenderRot = Quaternion.identity;

            if (barTransform != null)
            {
                bartenderPos = barTransform.position;
                bartenderRot = barTransform.rotation;
            }
            else
            {
                // Fallback: Detrás del mostrador principal mirando al salón
                GameObject mostrador = GameObject.Find("Mostrador_Principal");
                GameObject mesaLarga = GameObject.Find("MesaLarga");

                if (mostrador != null)
                {
                    if (mesaLarga != null)
                    {
                        bartenderPos = (mostrador.transform.position + mesaLarga.transform.position) / 2f;
                    }
                    else
                    {
                        bartenderPos = mostrador.transform.position + new Vector3(0f, 0f, 1.4f);
                    }
                    
                    // Asegurar que esté tocando el piso (raycast desde 1.5m de altura, NO 5m para evitar el techo a 4.1m)
                    if (Physics.Raycast(bartenderPos + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 5f))
                    {
                        bartenderPos.y = hit.point.y;
                    }
                    else
                    {
                        bartenderPos.y = 0f; // Asumir piso en 0
                    }
                    
                    // Que mire hacia el mostrador / salón
                    Vector3 lookDir = mostrador.transform.position - bartenderPos;
                    lookDir.y = 0;
                    if (lookDir != Vector3.zero) bartenderRot = Quaternion.LookRotation(lookDir);
                    else bartenderRot = Quaternion.Euler(0f, 180f, 0f);
                }
                else
                {
                    bartenderPos = new Vector3(6.05f, 0f, 8.2f);
                    bartenderRot = Quaternion.Euler(0f, 180f, 0f);
                }
            }

            GameObject prefabToSpawn = bartenderPrefab;
            if (prefabToSpawn == null)
            {
#if UNITY_EDITOR
                prefabToSpawn = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Core/Prefabs/BartenderPrefab.prefab");
#endif
            }
            if (prefabToSpawn == null)
            {
                prefabToSpawn = botPrefab;
            }

            GameObject bartender = Instantiate(prefabToSpawn, bartenderPos, bartenderRot);
            bartender.name = "Bartender";
            CleanBotComponents(bartender);

            Animator animBar = bartender.GetComponentInChildren<Animator>();
            if (animBar != null && animBar.runtimeAnimatorController != null && animBar.avatar != null && animBar.avatar.isValid)
            {
                animBar.SetBool("IsSeated", false);
                animBar.Play("Idle", 0, 0f); // Animación base respirando/parado
            }
        }

        private void CleanBotComponents(GameObject botInst)
        {
            var playerCtrl = botInst.GetComponent<PlayerController>();
            if (playerCtrl != null) Destroy(playerCtrl);
            var netTransform = botInst.GetComponent<ClientNetworkTransform>();
            if (netTransform != null) Destroy(netTransform);
            var netObj = botInst.GetComponent<Unity.Netcode.NetworkObject>();
            if (netObj != null) Destroy(netObj);
            var charCtrl = botInst.GetComponent<CharacterController>();
            if (charCtrl != null) Destroy(charCtrl);
            var cam = botInst.GetComponentInChildren<Camera>();
            if (cam != null) Destroy(cam.gameObject);
            var audioListener = botInst.GetComponentInChildren<AudioListener>();
            if (audioListener != null) Destroy(audioListener);
        }

        public override void OnNetworkDespawn()
        {
            ReleaseAllDialogue();
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
            }
        }

        private void OnClientDisconnect(ulong clientId)
        {
            var session = GetSessionByPlayer(clientId);
            if (session != null)
            {
                CancelGameSession(session, clientId);
            }
        }

        public bool IsPlayer1(ulong clientId)
        {
            var session = GetSessionByPlayer(clientId);
            return session != null && session.Player1Id == clientId;
        }

        public TableSession GetSessionByPlayer(ulong clientId)
        {
            return sessions.FirstOrDefault(s => s.Player1Id == clientId || s.Player2Id == clientId);
        }

        private TableSession GetOrCreateSession(TableDefinition table)
        {
            var session = sessions.FirstOrDefault(s => s.TableId == table.TableId);
            if (session == null)
            {
                session = new TableSession 
                { 
                    TableId = table.TableId,
                    Table = table,
                    TablePosition = table.transform.position,
                    GameModel = new GameModel(masterDeck)
                };
                sessions.Add(session);
            }
            return session;
        }

        private void CancelGameSession(TableSession session, ulong departingPlayerId)
        {
            if (session == null) return;

            CancelPendingBotAction(session);
            CancelPendingStartRound(session);
            CancelPavaCalienteTimer(session);
            CancelGuampasChoice(session);
            CancelGuitarraChoice(session);
            if (session.P1RevealedOpponentCards != null) session.P1RevealedOpponentCards.Clear();
            if (session.P2RevealedOpponentCards != null) session.P2RevealedOpponentCards.Clear();
            if (session.BotAvailablePowerIds != null) session.BotAvailablePowerIds.Clear();

            if (session.Player1Id == departingPlayerId)
            {
                session.Player1Id = ulong.MaxValue;
                session.Player1Seated = false;
                session.Player1Type = ParticipantType.None;
            }

            if (session.Player2Id == departingPlayerId && session.Player2Type != ParticipantType.Bot)
            {
                session.Player2Id = ulong.MaxValue;
                session.Player2Seated = false;
                session.Player2Type = ParticipantType.None;
            }

            ReleasePlayer2Bot(session);
            ClearTableCards(session);

            session.GameModel = new GameModel(masterDeck)
            {
                Player1Id = session.Player1Id,
                Player2Id = session.Player2Id,
                CurrentTurnId = ulong.MaxValue
            };
            session.IsStartingRound = false;

            SyncClientState(session);
        }

                [ServerRpc(RequireOwnership = false)]
        public void PlayerStandUpServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            var session = GetSessionByPlayer(sender);
            if (session != null)
            {
                CancelGameSession(session, sender);
                Debug.Log($"Jugador {sender} se levanto de la mesa.");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RestartMatchServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            TableSession session = GetSessionByPlayer(sender);
            if (!CanStartGameSession(session)
                || session.GameModel.CurrentState != GameState.PartidaTerminada)
            {
                Debug.LogWarning($"El jugador {sender} intentó reiniciar una partida que no terminó o no está disponible.");
                return;
            }

            CancelPendingBotAction(session);
            CancelPendingStartRound(session);
            ClearTableCards(session);
            session.GameModel = new GameModel(masterDeck)
            {
                Player1Id = session.Player1Id,
                Player2Id = session.Player2Id
            };
            session.IsStartingRound = false;

            Debug.Log($"Reiniciando la partida en {session.TableId} por solicitud del jugador {sender}.");
            StartRound(session);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayerSitDownServerRpc(string tableId, ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            TableDefinition table = TableDefinition.FindById(tableId);
            if (table == null)
            {
                Debug.LogWarning($"El jugador {sender} intentó sentarse en una mesa inexistente: {tableId}.");
                return;
            }

            TableSession requestedSession = GetOrCreateSession(table);
            if (requestedSession.Player1Id != ulong.MaxValue
                && requestedSession.Player2Id != ulong.MaxValue
                && requestedSession.Player1Id != sender
                && requestedSession.Player2Id != sender)
            {
                Debug.LogWarning($"La mesa {table.TableId} ya tiene dos jugadores.");
                return;
            }
            
            var oldSession = GetSessionByPlayer(sender);
            if (oldSession != null)
            {
                CancelGameSession(oldSession, sender);
            }

            TableSession session = requestedSession;

            if (session.Player1Id == ulong.MaxValue)
            {
                session.Player1Id = sender;
                session.GameModel.Player1Id = sender;
                session.Player1Seated = true;
                session.Player1Type = ParticipantType.Human;
            }
            else if (session.Player2Id == ulong.MaxValue)
            {
                session.Player2Id = sender;
                session.GameModel.Player2Id = sender;
                session.Player2Seated = true;
                session.Player2Type = ParticipantType.Human;
            }

            TryAutoFillPlayer2WithBot(session);
            CheckStartGame(session);
        }

        public bool ShouldAutoFillBot()
        {
            // En multijugador (Host o Cliente), NUNCA se usan bots ni se autocompleta la mesa con bots.
            // La partida únicamente debe comenzar cuando dos jugadores humanos reales se sienten en la misma mesa.
            if (Trucardo.Core.NetworkUI.selectedMode == GameMode.Host 
                || Trucardo.Core.NetworkUI.selectedMode == GameMode.Client)
            {
                return false;
            }

            // En Modo Individual (Singleplayer), SIEMPRE está activado para jugar con bots.
            if (Trucardo.Core.NetworkUI.selectedMode == GameMode.Singleplayer)
            {
                return true;
            }

            // En el editor directo sin pasar por el menú (GameMode.None), depende de autoFillPlayer2WithBot (por defecto true)
            return autoFillPlayer2WithBot;
        }

        private void TryAutoFillPlayer2WithBot(TableSession session)
        {
            if (!ShouldAutoFillBot()) return;
            if (session.Player1Type != ParticipantType.Human || !session.Player1Seated) return;
            if (session.Player2Id != ulong.MaxValue || session.Player2Seated || session.Player2Type != ParticipantType.None) return;

            RegisterBotAsPlayer2(session);
        }

        private bool RegisterBotAsPlayer2(TableSession session)
        {
            if (session == null) return false;
            if (session.Player2Id != ulong.MaxValue || session.Player2Seated || session.Player2Type != ParticipantType.None) return false;
            if (!TryGenerateLogicalBotId(out ulong botId))
            {
                Debug.LogError($"No se pudo generar un ID lógico para el bot de la mesa {session.TableId}.");
                return false;
            }

            session.Player2Id = botId;
            session.Player2Seated = true;
            session.Player2Type = ParticipantType.Bot;
            session.GameModel.Player2Id = botId;
            BotProfile profile = BotProfile.Create(defaultBotDifficulty, defaultBotPersonality);
            int seed = useFixedBotSeed ? botSeed : unchecked((int)(botId ^ (botId >> 32)));
            session.Player2BotController = new BotController(new BotBrain(profile, seed));
            RegisterDialogue(session);

            Debug.Log($"Bot lógico {botId} registrado como Jugador 2 en la mesa {session.TableId}.");
            return true;
        }

        private bool TryGenerateLogicalBotId(out ulong botId)
        {
            while (nextLogicalBotId != ulong.MaxValue)
            {
                ulong candidate = nextLogicalBotId;
                nextLogicalBotId = candidate == 0 ? ulong.MaxValue : candidate - 1;

                bool isConnectedClient = NetworkManager.Singleton != null
                    && NetworkManager.Singleton.ConnectedClients.ContainsKey(candidate);
                bool isAssignedParticipant = sessions.Any(session =>
                    session.Player1Id == candidate || session.Player2Id == candidate);

                if (!isConnectedClient && !isAssignedParticipant)
                {
                    botId = candidate;
                    return true;
                }
            }

            botId = ulong.MaxValue;
            return false;
        }

        private void ReleasePlayer2Bot(TableSession session)
        {
            if (session == null || session.Player2Type != ParticipantType.Bot) return;

            CancelPendingBotAction(session);
            session.Player2BotController = null;
            ReleaseDialogue(session);
            session.Player2Id = ulong.MaxValue;
            session.Player2Seated = false;
            session.Player2Type = ParticipantType.None;
        }

        public void TryScheduleBotAction(TableSession session)
        {
            if (session != null && session.GameModel.CurrentState == GameState.PartidaTerminada)
            {
                CancelPendingBotAction(session);
                CancelPendingStartRound(session);
                return;
            }
            if (session == null || session.PendingBotActionCoroutine != null) return;
            if (!CanPlayer2BotTakeAction(session, session.Player2Id)) return;

            ulong botId = session.Player2Id;
            session.PendingBotActionCoroutine = StartCoroutine(ProcessBotTurnDelayed(session, botId));
        }

        private IEnumerator ProcessBotTurnDelayed(TableSession session, ulong botId)
        {
            float waitSeconds = session.PavaCalienteActive ? Mathf.Min(botActionDelaySeconds, 0.25f) : botActionDelaySeconds;
            yield return new WaitForSeconds(waitSeconds);
            session.PendingBotActionCoroutine = null;

            if (!CanPlayer2BotTakeAction(session, botId)) yield break;

            BotObservation observation = BuildBotObservation(session, botId);
            BotDecision decision = session.Player2BotController.Decide(observation, logBotDecisions);
            if (logBotDecisions && !string.IsNullOrEmpty(session.Player2BotController.LastEvaluationSummary))
                Debug.Log(session.Player2BotController.LastEvaluationSummary);

            if (decision == null) yield break;

            // Activación opcional de poder por parte del Bot (únicamente poderes que posee en la mesa)
            if (!session.BotPowerUsedThisRound && !session.P2Boleado && session.GameModel.CurrentState == GameState.TurnoJugador)
            {
                if (UnityEngine.Random.value < 0.35f && session.BotAvailablePowerIds != null && session.BotAvailablePowerIds.Count > 0)
                {
                    PowerCatalog catalog = Resources.Load<PowerCatalog>("PowerCatalog");
                    if (catalog != null && catalog.AvailablePowers != null && catalog.AvailablePowers.Count > 0)
                    {
                        var playablePowers = new System.Collections.Generic.List<string>();
                        foreach (var pid in session.BotAvailablePowerIds)
                        {
                            PowerData pData = catalog.AvailablePowers.Find(p => p != null && p.PowerId == pid);
                            if (pData != null && pData.Effect != null)
                            {
                                PowerContext context = new PowerContext(botId, 2, null, null, session.GameModel);
                                if (pData.Effect.CanExecute(context, out _))
                                {
                                    playablePowers.Add(pid);
                                }
                            }
                        }

                        if (playablePowers.Count > 0)
                        {
                            string chosenPowerId = playablePowers[UnityEngine.Random.Range(0, playablePowers.Count)];
                            session.BotAvailablePowerIds.Remove(chosenPowerId);
                            session.BotPowerUsedThisRound = true;
                            ProcessPlayPower(botId, chosenPowerId);
                            SyncClientState(session);
                        }
                    }
                }
            }

            // Luz Mala: Si el bot está bajo efecto de Luz Mala y va a jugar carta, juega una al azar
            if (session.P2LuzMalaActive && decision.Type == BotDecisionType.PlayCard && session.GameModel.P2Hand.Count > 0)
            {
                int randomCard = session.GameModel.P2Hand[UnityEngine.Random.Range(0, session.GameModel.P2Hand.Count)];
                decision = BotDecision.PlayCard(randomCard);
            }

            // Bastón Presidencial: Si debe responder a un canto del rival que usó el bastón, fuerza aceptación
            if (session.ForcedAcceptNextCanto && decision.Type == BotDecisionType.Respond && botId != session.ForcedAcceptCallerId)
            {
                decision = BotDecision.Respond(true);
            }

            if (decision.Type == BotDecisionType.Respond && CanPlayer2BotRespond(session, botId))
            {
                ProcessResponse(botId, decision.Accepts);
            }
            else if (decision.Type == BotDecisionType.CallEnvido
                && CanPlayer2BotCallEnvido(session, botId, decision.EnvidoCall))
            {
                ProcessCallEnvido(botId, decision.EnvidoCall);
            }
            else if (decision.Type == BotDecisionType.CallTruco && CanPlayer2BotCallTruco(session, botId))
            {
                ProcessCallTruco(botId);
            }
            else if (decision.Type == BotDecisionType.PlayCard && CanPlayer2BotPlayCard(session, botId)
                && session.GameModel.P2Hand.Contains(decision.CardIndex))
            {
                ProcessPlayCard(botId, decision.CardIndex);
            }
        }

        private BotObservation BuildBotObservation(TableSession session, ulong botId)
        {
            GameModel model = session.GameModel;
            return new BotObservation(
                botId,
                model.CurrentState,
                model.CurrentTurnId,
                model.CantoPendiente,
                model.LastCallerId,
                CanPlayer2BotCallEnvido(session, botId, EnvidoCall.Envido),
                CanPlayer2BotCallEnvido(session, botId, EnvidoCall.RealEnvido),
                CanPlayer2BotCallEnvido(session, botId, EnvidoCall.FaltaEnvido),
                CanPlayer2BotCallTruco(session, botId),
                model.ScoreTeam1,
                model.ScoreTeam2,
                model.CurrentRoundIndex,
                model.RoundWinners,
                model.EnvidoCantado,
                model.EstadoTruco,
                BuildBotCardObservations(model, model.P2Hand),
                BuildBotCardObservations(model, model.P1PlayedCards),
                BuildBotCardObservations(model, model.P2PlayedCards),
                model.ManoPlayerId == botId,
                model.LastTrucoCallerId,
                System.Math.Max(0, 3 - model.P1PlayedCards.Count),
                BuildUnseenBotCards(model),
                model.EstadoEnvido,
                model.EnvidoAcceptedPoints,
                EnvidoRules.GetAcceptedPoints(model.EstadoEnvido, model.ScoreTeam1, model.ScoreTeam2));
        }

        private List<BotCardObservation> BuildUnseenBotCards(GameModel model)
        {
            // Normal deck composition minus only our cards and publicly played cards.
            var known = new HashSet<int>(model.P2Hand);
            known.UnionWith(model.P1PlayedCards);
            known.UnionWith(model.P2PlayedCards);
            var unseen = new List<BotCardObservation>();
            for (int i = 0; i < model.DeckReference.AvailableCards.Count; i++)
                if (!known.Contains(i) && TryBuildBotCardObservation(model, i, out BotCardObservation card))
                    unseen.Add(card);
            return unseen;
        }

        private List<BotCardObservation> BuildBotCardObservations(GameModel model, IEnumerable<int> cardIndices)
        {
            var observations = new List<BotCardObservation>();

            foreach (int cardIndex in cardIndices)
            {
                if (TryBuildBotCardObservation(model, cardIndex, out BotCardObservation observation))
                {
                    observations.Add(observation);
                }
            }

            return observations;
        }

        private bool TryBuildBotCardObservation(
            GameModel model,
            int cardIndex,
            out BotCardObservation observation)
        {
            observation = null;
            DeckData deck = model != null ? model.DeckReference : null;
            if (deck == null || deck.AvailableCards == null) return false;
            if (cardIndex < 0 || cardIndex >= deck.AvailableCards.Count) return false;

            CardData card = deck.AvailableCards[cardIndex];
            if (card == null) return false;

            observation = new BotCardObservation(
                cardIndex,
                card.Suit,
                card.Number,
                card.TrucoRank,
                card.EnvidoValue);
            return true;
        }

        private bool IsPlayer2BotActive(TableSession session, ulong expectedBotId)
        {
            return session != null
                && session.Player2Type == ParticipantType.Bot
                && session.Player2Seated
                && session.Player2Id == expectedBotId
                && session.Player2BotController != null;
        }

        private bool CanPlayer2BotPlayCard(TableSession session, ulong expectedBotId)
        {
            return IsPlayer2BotActive(session, expectedBotId)
                && session.GameModel.CurrentState == GameState.TurnoJugador
                && session.GameModel.CurrentTurnId == expectedBotId;
        }

        private bool CanPlayer2BotRespond(TableSession session, ulong expectedBotId)
        {
            return IsPlayer2BotActive(session, expectedBotId)
                && session.GameModel.CurrentState == GameState.EsperandoRespuestaCanto
                && session.GameModel.CantoPendiente != TipoCanto.Ninguno
                && session.GameModel.LastCallerId != expectedBotId;
        }

        private bool CanPlayer2BotCallEnvido(
            TableSession session, ulong expectedBotId, EnvidoCall call)
        {
            return IsPlayer2BotActive(session, expectedBotId)
                && CanPlayerCallEnvido(session, expectedBotId, call);
        }

        private bool CanPlayerCallEnvido(TableSession session, ulong actorId, EnvidoCall call)
        {
            if (session == null || session.GameModel == null) return false;
            if (actorId != session.Player1Id && actorId != session.Player2Id) return false;
            int playedCardCount = actorId == session.Player1Id
                ? session.GameModel.P1PlayedCards.Count
                : session.GameModel.P2PlayedCards.Count;
            return EnvidoRules.CanCallFromContext(
                session.GameModel.CurrentState,
                session.GameModel.CantoPendiente,
                session.GameModel.LastCallerId,
                actorId,
                session.GameModel.CurrentRoundIndex,
                playedCardCount,
                session.GameModel.EstadoTruco,
                session.GameModel.EstadoEnvido,
                call);
        }

        private bool CanPlayer2BotCallTruco(TableSession session, ulong expectedBotId)
        {
            return IsPlayer2BotActive(session, expectedBotId)
                && session.GameModel.CurrentState == GameState.TurnoJugador
                && session.GameModel.CurrentTurnId == expectedBotId
                && session.GameModel.CantoPendiente == TipoCanto.Ninguno
                && session.GameModel.EstadoTruco != NivelTruco.Vale4
                && (session.GameModel.EstadoTruco == NivelTruco.Ninguno
                    || session.GameModel.LastTrucoCallerId != expectedBotId);
        }

        private bool CanPlayer2BotTakeAction(TableSession session, ulong expectedBotId)
        {
            return CanPlayer2BotRespond(session, expectedBotId)
                || CanPlayer2BotCallEnvido(session, expectedBotId, EnvidoCall.Envido)
                || CanPlayer2BotCallEnvido(session, expectedBotId, EnvidoCall.RealEnvido)
                || CanPlayer2BotCallEnvido(session, expectedBotId, EnvidoCall.FaltaEnvido)
                || CanPlayer2BotCallTruco(session, expectedBotId)
                || CanPlayer2BotPlayCard(session, expectedBotId);
        }

        private void CancelPendingBotAction(TableSession session)
        {
            if (session == null || session.PendingBotActionCoroutine == null) return;

            StopCoroutine(session.PendingBotActionCoroutine);
            session.PendingBotActionCoroutine = null;
        }

        private void CancelPendingStartRound(TableSession session)
        {
            if (session == null) return;

            if (session.PendingStartRoundCoroutine != null)
            {
                StopCoroutine(session.PendingStartRoundCoroutine);
                session.PendingStartRoundCoroutine = null;
            }

            session.IsStartingRound = false;
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayCardServerRpc(int cardIndex, ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            ProcessPlayCard(sender, cardIndex);
        }

        internal void ProcessPlayCard(ulong actorId, int cardIndex)
        {
            var session = GetSessionByPlayer(actorId);
            if (session == null) return;
            if (actorId != session.Player1Id && actorId != session.Player2Id) return;
            
            if (session.GameModel.CurrentState != GameState.TurnoJugador) return;
            if (session.GameModel.CurrentTurnId != actorId) return;

            List<int> hand;
            if (actorId == session.Player1Id)
            {
                hand = session.GameModel.P1Hand;
            }
            else if (actorId == session.Player2Id)
            {
                hand = session.GameModel.P2Hand;
            }
            else
            {
                return;
            }

            if (!hand.Contains(cardIndex)) return;

            var dialogueBefore = CaptureDialogue(session);
            session.GameModel.actionQueue.EnqueueCommand(new Trucardo.Core.Commands.PlayCardCommand(session.GameModel, actorId, cardIndex));
            session.GameModel.actionQueue.ProcessNext();
            AfterDialogueAction(session, dialogueBefore, actorId, DialogueDecision.PlayedCard);

            SpawnCardOnTable(session, actorId, cardIndex);
            
            NotifyOpponentPlayedCardClientRpc(actorId);

            if (session.GameModel.CurrentState == GameState.RondaTerminada)
            {
                ScheduleStartRound(session, 3f);
            }
            
            SyncClientState(session);
            TryScheduleBotAction(session);
            CheckStartPavaCalienteTimer(session);
        }

        private void SpawnCardOnTable(TableSession session, ulong playerId, int cardIndex)
        {
            if (cardPrefab == null) return;

            GameObject c = Instantiate(cardPrefab);
            NetworkObject netObj = c.GetComponent<NetworkObject>();

            Transform mesa = session.Table != null ? session.Table.transform : null;

            PlayerController pc = null;
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.ClientId == playerId)
                {
                    pc = client.PlayerObject != null ? client.PlayerObject.GetComponent<PlayerController>() : null;
                    break;
                }
            }

            // Determinar si el jugador que jugó la carta está en el Banco A (Seats[0]) o Banco B (Seats[1])
            bool isAtSeatA = true;
            if (pc != null)
            {
                if (session.Table != null && session.Table.Seats != null && session.Table.Seats.Count >= 2 
                    && session.Table.Seats[0] != null && session.Table.Seats[1] != null)
                {
                    float distA = Vector3.Distance(pc.transform.position, session.Table.Seats[0].transform.position);
                    float distB = Vector3.Distance(pc.transform.position, session.Table.Seats[1].transform.position);
                    isAtSeatA = distA <= distB;
                }
                else if (mesa != null)
                {
                    Vector3 localPlayerPos = mesa.InverseTransformPoint(pc.transform.position);
                    isAtSeatA = localPlayerPos.x <= 0f;
                }
            }
            else
            {
                // Si pc == null, es un bot (en modo individual siempre juega como Player 2 desde Banco B / Seats[1]).
                isAtSeatA = (playerId == session.Player1Id);
            }

            // Origen del tiro de la carta (startPos y startRot)
            Vector3 startPos = Vector3.zero;
            Quaternion startRot = Quaternion.identity;

            if (pc != null)
            {
                startPos = pc.transform.position + pc.transform.forward * 0.8f;
                startPos.y = 0.80f;

                Vector3 forwardOnTable = pc.transform.forward;
                forwardOnTable.y = 0;
                if (forwardOnTable != Vector3.zero)
                {
                    startRot = Quaternion.LookRotation(forwardOnTable, Vector3.up);
                    startRot *= Quaternion.Euler(-35f, 0f, 0f);
                }
            }
            else
            {
                // El bot tira la carta desde su banco hacia el centro de la mesa
                Transform botSeat = (session.Table != null && session.Table.Seats != null && session.Table.Seats.Count >= 2)
                    ? (isAtSeatA ? session.Table.Seats[0].transform : session.Table.Seats[1].transform)
                    : null;

                if (botSeat != null && mesa != null)
                {
                    Vector3 toCenter = mesa.position - botSeat.position;
                    toCenter.y = 0;
                    Vector3 forward = toCenter != Vector3.zero ? toCenter.normalized : botSeat.forward;

                    startPos = botSeat.position + forward * 0.35f + Vector3.up * 0.75f;
                    startRot = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(-35f, 0f, 0f);
                }
                else if (mesa != null)
                {
                    startPos = mesa.position + Vector3.up * 0.8f;
                    startRot = mesa.rotation * Quaternion.Euler(-35f, 0f, 0f);
                }
            }

            c.transform.position = startPos;
            c.transform.rotation = startRot;
            if (netObj != null) netObj.Spawn();

            int playerPlayedCount = 0;
            if (playerId == session.Player1Id) playerPlayedCount = 2 - session.GameModel.P1Hand.Count;
            else if (playerId == session.Player2Id) playerPlayedCount = 2 - session.GameModel.P2Hand.Count;
            playerPlayedCount = Mathf.Clamp(playerPlayedCount, 0, 2);

            float zSpacing = 0.22f;
            // Visto desde la perspectiva del jugador (en Banco A mirando a la mesa):
            // -Z local es la Izquierda del jugador.
            // +Z local es la Derecha del jugador.
            // Mano 1 (playerPlayedCount = 0): zOffset = -0.22 (Izquierda)
            // Mano 2 (playerPlayedCount = 1): zOffset = 0.00 (Centro)
            // Mano 3 (playerPlayedCount = 2): zOffset = +0.22 (Derecha)
            // Tanto las cartas del jugador como las del rival se alinean en las mismas columnas,
            // garantizando que el jugador siempre ve todas las manos jugarse de izquierda a derecha.
            float zOffset = (playerPlayedCount - 1) * zSpacing;

            // Banco A (lado del jugador) usa xOffset = +0.2f y rotación -90° para estar de su lado y mirarlo
            // Banco B (lado del rival) usa xOffset = -0.2f y rotación +90° para estar de su lado y mirarlo
            float xOffset = isAtSeatA ? 0.2f : -0.2f;
            float yRot = isAtSeatA ? -90f : 90f;

            Vector3 targetPos = new Vector3(xOffset, 0f, zOffset);
            Quaternion targetRot = Quaternion.Euler(0, yRot, 0);

            if (mesa != null)
            {
                targetPos = mesa.TransformPoint(targetPos);
                targetPos.y = GetTableSurfaceHeight(mesa, targetPos.y);
                targetRot = mesa.rotation * targetRot;
            }

            CardVisual visual = c.GetComponent<CardVisual>();
            if (visual != null)
            {
                var nt = c.GetComponent<Unity.Netcode.Components.NetworkTransform>();
                if (nt != null) Destroy(nt);
                visual.netCardIndex.Value = cardIndex;
            }

            session.TableCards.Add(c);
            SpawnCardOnTableClientRpc(cardIndex, targetPos, targetRot, c.GetComponent<Unity.Netcode.NetworkObject>());
        }

        private float GetTableSurfaceHeight(Transform table, float fallback)
        {
            float highestSurface = float.NegativeInfinity;
            foreach (Renderer renderer in table.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || renderer.bounds.size.y > 1.5f)
                    continue;

                highestSurface = Mathf.Max(highestSurface, renderer.bounds.max.y);
            }

            return float.IsNegativeInfinity(highestSurface) ? fallback + 0.95f : highestSurface + 0.03f;
        }

        [ClientRpc]
        private void SpawnCardOnTableClientRpc(int cardIndex, Vector3 targetPos, Quaternion targetRot, Unity.Netcode.NetworkObjectReference cardRef)
        {
            if (cardRef.TryGet(out Unity.Netcode.NetworkObject netObj))
            {
                GameObject c = netObj.gameObject;
                CardVisual visual = c.GetComponent<CardVisual>();
                if (visual != null) 
                {
                    var nt = c.GetComponent<Unity.Netcode.Components.NetworkTransform>();
                    if (nt != null) Destroy(nt);
                    
                    visual.netCardIndex.Value = cardIndex;
                    visual.PlayToTable(targetPos, targetRot, 0.6f);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void CallEnvidoServerRpc(EnvidoCall call, ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            ProcessCallEnvido(sender, call);
        }

        internal void ProcessCallEnvido(ulong actorId, EnvidoCall call)
        {
            var session = GetSessionByPlayer(actorId);
            if (session == null) return;
            if (actorId != session.Player1Id && actorId != session.Player2Id) return;
            if (!CanPlayerCallEnvido(session, actorId, call)) return;

            var dialogueBefore = CaptureDialogue(session);
            session.GameModel.actionQueue.EnqueueCommand(
                new Trucardo.Core.Commands.CallEnvidoCommand(session.GameModel, actorId, call));
            session.GameModel.actionQueue.ProcessNext();
            AfterDialogueAction(session, dialogueBefore, actorId, DialogueDecision.Call, ToDialogueCall(call));
            SyncClientState(session);
            TryScheduleBotAction(session);
            CheckStartPavaCalienteTimer(session);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CallTrucoServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            ProcessCallTruco(sender);
        }

        internal void ProcessCallTruco(ulong actorId)
        {
            var session = GetSessionByPlayer(actorId);
            if (session == null) return;
            if (actorId != session.Player1Id && actorId != session.Player2Id) return;

            var dialogueBefore = CaptureDialogue(session);
            session.GameModel.actionQueue.EnqueueCommand(new Trucardo.Core.Commands.CallTrucoCommand(session.GameModel, actorId));
            session.GameModel.actionQueue.ProcessNext();
            AfterDialogueAction(session, dialogueBefore, actorId, DialogueDecision.Call, PendingTrucoName(dialogueBefore.Truco));
            SyncClientState(session);
            TryScheduleBotAction(session);
            CheckStartPavaCalienteTimer(session);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RespondServerRpc(bool accepts, ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            ProcessResponse(sender, accepts);
        }

        internal void ProcessResponse(ulong actorId, bool accepts)
        {
            var session = GetSessionByPlayer(actorId);
            if (session == null) return;
            if (actorId != session.Player1Id && actorId != session.Player2Id) return;

            if (session.ForcedAcceptNextCanto && actorId != session.ForcedAcceptCallerId)
            {
                accepts = true;
                session.ForcedAcceptNextCanto = false;
                session.ForcedAcceptCallerId = 0;
                Debug.Log("<color=#FFD700><b>[Bastón Presidencial]</b></color> ¡El canto ha sido aceptado por orden presidencial!");
            }

            var dialogueBefore = CaptureDialogue(session);
            session.GameModel.actionQueue.EnqueueCommand(new Trucardo.Core.Commands.RespondCommand(session.GameModel, actorId, accepts));
            session.GameModel.actionQueue.ProcessNext();
            AfterDialogueAction(session, dialogueBefore, actorId, accepts ? DialogueDecision.Accept : DialogueDecision.Reject);

            if (session.GameModel.CurrentState == GameState.RondaTerminada)
            {
                CancelPavaCalienteTimer(session);
                ScheduleStartRound(session, 3f);
            }
            SyncClientState(session);
            TryScheduleBotAction(session);
            CheckStartPavaCalienteTimer(session);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayPowerServerRpc(string powerId, ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            ProcessPlayPower(sender, powerId);
        }

        public void ProcessPlayPower(ulong actorId, string powerId)
        {
            if (string.IsNullOrEmpty(powerId)) return;
            var session = GetSessionByPlayer(actorId);
            if (session == null) return;
            if (actorId != session.Player1Id && actorId != session.Player2Id) return;

            int team = (actorId == session.Player1Id) ? 1 : 2;

            PowerCatalog catalog = Resources.Load<PowerCatalog>("PowerCatalog");
            PowerData powerData = (catalog != null && catalog.AvailablePowers != null)
                ? catalog.AvailablePowers.Find(p => p != null && p.PowerId == powerId)
                : null;

            if (powerData != null && powerData.Effect != null)
            {
                PowerContext context = new PowerContext(actorId, team, null, null, session.GameModel);
                if (!powerData.Effect.CanExecute(context, out string reason))
                {
                    Debug.LogWarning($"[Power] No se puede ejecutar {powerData.DisplayName}: {reason}");
                    SendPowerToast(session, actorId, powerData.DisplayName.ToUpper(), reason);
                    return;
                }

                bool executed = powerData.Effect.Execute(context);
                if (!executed)
                {
                    return;
                }
            }

            if (team == 1) session.P1ActivePowerId = powerId;
            else session.P2ActivePowerId = powerId;

            NotifyPowerActivatedClientRpc(actorId, powerId);
            SyncClientState(session);
            TryScheduleBotAction(session);
            CheckStartPavaCalienteTimer(session);
        }

        [ClientRpc]
        private void NotifyPowerActivatedClientRpc(ulong actorId, string powerId)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                var playerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                if (playerObj != null)
                {
                    PlayerController pc = playerObj.GetComponent<PlayerController>();
                    if (pc != null)
                    {
                        pc.OnPowerActivatedNotification(actorId, powerId);
                    }
                }
            }
        }

        private void CheckStartGame(TableSession session)
        {
            if (CanStartGameSession(session) && session.GameModel.CurrentState == GameState.EsperandoJugadores)
            {
                Debug.Log($"Jugador listo en {session.TableId}! Repartiendo.");
                StartRound(session);
            }
        }

        private bool CanStartGameSession(TableSession session)
        {
            return session != null
                && sessions.Contains(session)
                && session.Player1Seated
                && session.Player2Seated
                && session.Player1Type != ParticipantType.None
                && session.Player2Type != ParticipantType.None
                && session.Player1Id != ulong.MaxValue
                && session.Player2Id != ulong.MaxValue;
        }

        private void ScheduleStartRound(TableSession session, float delay)
        {
            if (session == null || session.PendingStartRoundCoroutine != null || session.IsStartingRound) return;
            if (session.GameModel.CurrentState != GameState.RondaTerminada) return;

            session.IsStartingRound = true;
            GameModel expectedGameModel = session.GameModel;
            session.PendingStartRoundCoroutine = StartCoroutine(StartRoundDelayed(session, delay, expectedGameModel));
        }

        private IEnumerator StartRoundDelayed(TableSession session, float delay, GameModel expectedGameModel)
        {
            yield return new WaitForSeconds(delay);

            if (session.GameModel != expectedGameModel) yield break;

            session.PendingStartRoundCoroutine = null;
            session.IsStartingRound = false;

            if (!CanStartGameSession(session)) yield break;
            if (expectedGameModel.CurrentState != GameState.RondaTerminada) yield break;

            StartRound(session);
        }

        public int GetUnusedCard(TableSession session)
        {
            if (session == null || session.GameModel == null || masterDeck == null || masterDeck.AvailableCards == null) return -1;

            HashSet<int> used = new HashSet<int>();
            if (session.GameModel.P1Hand != null) foreach (int c in session.GameModel.P1Hand) used.Add(c);
            if (session.GameModel.P2Hand != null) foreach (int c in session.GameModel.P2Hand) used.Add(c);
            if (session.GameModel.P1PlayedCards != null) foreach (int c in session.GameModel.P1PlayedCards) used.Add(c);
            if (session.GameModel.P2PlayedCards != null) foreach (int c in session.GameModel.P2PlayedCards) used.Add(c);
            if (session.GuampasDrawnCardIndex != -1) used.Add(session.GuampasDrawnCardIndex);

            List<int> available = new List<int>();
            for (int i = 0; i < masterDeck.AvailableCards.Count; i++)
            {
                if (!used.Contains(i)) available.Add(i);
            }
            if (available.Count == 0) return -1;
            return available[UnityEngine.Random.Range(0, available.Count)];
        }

        private void StartRound(TableSession session)
        {
            if (!CanStartGameSession(session) || session.GameModel.CurrentState == GameState.PartidaTerminada) return;
            session.GameModel.ResetHand();
            ClearTableCards(session);

            session.PavaCalienteActive = false;
            session.PavaCalienteCasterId = 0;
            CancelPavaCalienteTimer(session);
            CancelGuampasChoice(session);
            CancelGuitarraChoice(session);
            if (session.P1RevealedOpponentCards != null) session.P1RevealedOpponentCards.Clear();
            if (session.P2RevealedOpponentCards != null) session.P2RevealedOpponentCards.Clear();
            session.P1LuzMalaActive = false;
            session.P2LuzMalaActive = false;
            session.ForcedAcceptNextCanto = false;
            session.ForcedAcceptCallerId = 0;
            session.P1ActivePowerId = null;
            session.P2ActivePowerId = null;
            session.BotPowerUsedThisRound = false;

            if (session.Player2Type == ParticipantType.Bot)
            {
                bool botBoleado = session.P2Boleado;
                if (botBoleado)
                {
                    session.P2Boleado = false;
                }
                else if (session.BotAvailablePowerIds.Count < 4)
                {
                    PowerCatalog catalog = Resources.Load<PowerCatalog>("PowerCatalog");
                    if (catalog != null && catalog.AvailablePowers != null && catalog.AvailablePowers.Count > 0)
                    {
                        PowerData newPower = catalog.GetRandomPower();
                        if (newPower != null && newPower.IsValid())
                        {
                            session.BotAvailablePowerIds.Add(newPower.PowerId);
                        }
                    }
                }
            }

            List<int> deckIndices = new List<int>();
            for (int i = 0; i < 40; i++) deckIndices.Add(i);
            
            for (int i = 0; i < deckIndices.Count; i++)
            {
                int temp = deckIndices[i];
                int randomIndex = UnityEngine.Random.Range(i, deckIndices.Count);
                deckIndices[i] = deckIndices[randomIndex];
                deckIndices[randomIndex] = temp;
            }

            int[] p1Cards = new int[] { deckIndices[0], deckIndices[1], deckIndices[2] };
            int[] p2Cards = new int[] { deckIndices[3], deckIndices[4], deckIndices[5] };

            session.GameModel.DealCardsToPlayer(1, new List<int>(p1Cards));
            session.GameModel.DealCardsToPlayer(2, new List<int>(p2Cards));

            if (session.Player1Seated && session.Player1Type == ParticipantType.Human)
            {
                ClientRpcParams clientRpcParamsP1 = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { session.Player1Id } }
                };
                ReceiveCardsClientRpc(session.Player1Id, p1Cards, clientRpcParamsP1);
            }

            if (session.Player2Seated && session.Player2Type == ParticipantType.Human)
            {
                ClientRpcParams clientRpcParamsP2 = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { session.Player2Id } }
                };
                ReceiveCardsClientRpc(session.Player2Id, p2Cards, clientRpcParamsP2);
            }

            session.GameModel.CurrentState = GameState.TurnoJugador;
            session.GameModel.CurrentTurnId = session.GameModel.ManoPlayerId;
            DialogueHandStarted(session);
            
            SyncClientState(session);
            TryScheduleBotAction(session);
            CheckStartPavaCalienteTimer(session);
        }

        private void ClearTableCards(TableSession session)
        {
            if (session == null) return;

            foreach (var card in session.TableCards)
            {
                if (card == null) continue;

                NetworkObject netObj = card.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned) netObj.Despawn();
                Destroy(card);
            }

            session.TableCards.Clear();
        }

                [ClientRpc]
        private void ReceiveCardsClientRpc(ulong targetClientId, int[] cardIndices, ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            if (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
            {
                PlayerController pc = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
                if (pc != null) pc.ReceiveCards(cardIndices);
            }
        }

        [ClientRpc]
        private void NotifyOpponentPlayedCardClientRpc(ulong playedById)
        {
            if (NetworkManager.Singleton.LocalClientId != playedById)
            {
                if (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
                {
                    PlayerController pc = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
                    if (pc != null) pc.RemoveOpponentCard();
                }
            }
        }

        [ClientRpc]
        private void UpdateClientStateClientRpc(
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
            ulong p1Id,
            ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
            {
                PlayerController pc = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
                if (pc != null) pc.UpdateUIState(
                    state,
                    turnId,
                    currentRoundIndex,
                    p1Score,
                    p2Score,
                    canto,
                    lastCaller,
                    estadoTruco,
                    estadoEnvido,
                    envidoAcceptedPoints,
                    pendingEnvidoPoints,
                    p1Id);
            }
        }

        [ClientRpc]
        public void SyncPlayerHandClientRpc(ulong targetClientId, int[] cardIndices, ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            if (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
            {
                PlayerController pc = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
                if (pc != null) pc.ReplaceAllHandCards(cardIndices);
            }
        }

        public void SyncPlayerHand(TableSession session, ulong targetClientId)
        {
            if (session == null || session.GameModel == null) return;
            int team = (targetClientId == session.Player1Id) ? 1 : 2;
            List<int> hand = (team == 1) ? session.GameModel.P1Hand : session.GameModel.P2Hand;
            if (hand == null) return;

            int[] cards = hand.ToArray();
            ClientRpcParams rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { targetClientId } }
            };
            SyncPlayerHandClientRpc(targetClientId, cards, rpcParams);
        }

        [ClientRpc]
        public void RevealOpponentCardClientRpc(ulong targetClientId, int cardIndex, ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            if (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
            {
                PlayerController pc = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
                if (pc != null && masterDeck != null && cardIndex >= 0 && cardIndex < masterDeck.AvailableCards.Count)
                {
                    pc.RevealOpponentCard(cardIndex, masterDeck.AvailableCards[cardIndex]);
                }
            }
        }

        public void RevealOpponentCardToPlayer(TableSession session, ulong targetClientId, int cardIndex)
        {
            ClientRpcParams rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { targetClientId } }
            };
            RevealOpponentCardClientRpc(targetClientId, cardIndex, rpcParams);
        }

        [ClientRpc]
        public void ShowPowerToastClientRpc(string title, string message, ClientRpcParams clientRpcParams = default)
        {
            if (Trucardo.Core.UI.HUDController.Instance != null)
            {
                Trucardo.Core.UI.HUDController.Instance.ShowToast(title, message);
            }
        }

        public void SendPowerToast(TableSession session, ulong targetClientId, string title, string message)
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { targetClientId } }
            };
            ShowPowerToastClientRpc(title, message, clientRpcParams);
        }

        public void BroadcastPowerToast(TableSession session, string title, string message)
        {
            List<ulong> seatedHumans = new List<ulong>();
            if (session.Player1Seated && session.Player1Type == ParticipantType.Human) seatedHumans.Add(session.Player1Id);
            if (session.Player2Seated && session.Player2Type == ParticipantType.Human) seatedHumans.Add(session.Player2Id);
            if (seatedHumans.Count == 0) return;

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = seatedHumans.ToArray() }
            };
            ShowPowerToastClientRpc(title, message, clientRpcParams);
        }

        [ClientRpc]
        public void SyncPowerStatesClientRpc(
            bool pavaCalienteActive,
            bool forcedAcceptNextCanto,
            ulong forcedAcceptCallerId,
            bool p1LuzMala,
            bool p2LuzMala,
            ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
            {
                PlayerController pc = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.SyncPowerStates(pavaCalienteActive, forcedAcceptNextCanto, forcedAcceptCallerId, p1LuzMala, p2LuzMala);
                }
            }
        }

        [ClientRpc]
        public void SyncBotPowersClientRpc(string botPowersDelimited, ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null && NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
            {
                PlayerController pc = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
                if (pc != null)
                {
                    string[] powerIds = string.IsNullOrEmpty(botPowersDelimited)
                        ? new string[0]
                        : botPowersDelimited.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                    pc.SyncOpponentPowers(powerIds);
                }
            }
        }

        public void CheckStartPavaCalienteTimer(TableSession session)
        {
            if (session == null) return;

            CancelPavaCalienteTimer(session);

            if (!session.PavaCalienteActive || session.GameModel == null) return;

            if (session.GameModel.CurrentState != GameState.TurnoJugador && session.GameModel.CurrentState != GameState.EsperandoRespuestaCanto)
            {
                return;
            }

            ulong waitingPlayerId = ulong.MaxValue;
            if (session.GameModel.CurrentState == GameState.TurnoJugador)
            {
                waitingPlayerId = session.GameModel.CurrentTurnId;
            }
            else if (session.GameModel.CurrentState == GameState.EsperandoRespuestaCanto)
            {
                waitingPlayerId = (session.GameModel.LastCallerId == session.Player1Id) ? session.Player2Id : session.Player1Id;
            }

            if (waitingPlayerId == ulong.MaxValue) return;

            session.PavaTurnTimeoutCoroutine = StartCoroutine(PavaTurnTimeoutRoutine(session, waitingPlayerId, session.GameModel.CurrentState, session.GameModel.CurrentRoundIndex));
        }

        public void CancelPavaCalienteTimer(TableSession session)
        {
            if (session == null || session.PavaTurnTimeoutCoroutine == null) return;
            StopCoroutine(session.PavaTurnTimeoutCoroutine);
            session.PavaTurnTimeoutCoroutine = null;
        }

        private IEnumerator PavaTurnTimeoutRoutine(TableSession session, ulong targetPlayerId, GameState expectedState, int expectedRound)
        {
            yield return new WaitForSeconds(5.2f);

            if (session == null || !session.PavaCalienteActive || session.GameModel == null) yield break;
            if (session.GameModel.CurrentRoundIndex != expectedRound) yield break;

            if (session.GuampasPendingPlayerId == targetPlayerId)
            {
                CancelGuampasChoice(session);
            }
            if (session.GuitarraPendingPlayerId == targetPlayerId)
            {
                CancelGuitarraChoice(session);
            }

            if (session.GameModel.CurrentState == GameState.TurnoJugador && expectedState == GameState.TurnoJugador && session.GameModel.CurrentTurnId == targetPlayerId)
            {
                List<int> hand = (targetPlayerId == session.Player1Id) ? session.GameModel.P1Hand : session.GameModel.P2Hand;
                if (hand != null && hand.Count > 0)
                {
                    int cardToPlay = hand[0];
                    SendPowerToast(session, targetPlayerId, "¡PAVA CALIENTE!", "¡Tiempo agotado (5s)! Se jugó tu carta automáticamente.");
                    ProcessPlayCard(targetPlayerId, cardToPlay);
                }
            }
            else if (session.GameModel.CurrentState == GameState.EsperandoRespuestaCanto && expectedState == GameState.EsperandoRespuestaCanto)
            {
                ulong responderId = (session.GameModel.LastCallerId == session.Player1Id) ? session.Player2Id : session.Player1Id;
                if (responderId == targetPlayerId)
                {
                    bool forced = session.ForcedAcceptNextCanto && targetPlayerId != session.ForcedAcceptCallerId;
                    bool accepts = forced ? true : false;
                    SendPowerToast(session, targetPlayerId, "¡PAVA CALIENTE!", forced 
                        ? "¡Tiempo agotado (5s)! Aceptado por mandato presidencial." 
                        : "¡Tiempo agotado (5s)! Se rechazó el canto automáticamente.");
                    ProcessResponse(targetPlayerId, accepts);
                }
            }
        }

        public void SyncClientState(TableSession session)
        {
            List<ulong> seatedPlayers = new List<ulong>();
            if (session.Player1Seated && session.Player1Type == ParticipantType.Human) seatedPlayers.Add(session.Player1Id);
            if (session.Player2Seated && session.Player2Type == ParticipantType.Human) seatedPlayers.Add(session.Player2Id);
            if (seatedPlayers.Count == 0) return;

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = seatedPlayers.ToArray() }
            };
            UpdateClientStateClientRpc(
                session.GameModel.CurrentState,
                session.GameModel.CurrentTurnId,
                session.GameModel.CurrentRoundIndex,
                session.GameModel.ScoreTeam1,
                session.GameModel.ScoreTeam2,
                session.GameModel.CantoPendiente,
                session.GameModel.LastCallerId,
                session.GameModel.EstadoTruco,
                session.GameModel.EstadoEnvido,
                session.GameModel.EnvidoAcceptedPoints,
                EnvidoRules.GetAcceptedPoints(
                    session.GameModel.EstadoEnvido,
                    session.GameModel.ScoreTeam1,
                    session.GameModel.ScoreTeam2),
                session.Player1Id,
                clientRpcParams);

            SyncPowerStatesClientRpc(
                session.PavaCalienteActive,
                session.ForcedAcceptNextCanto,
                session.ForcedAcceptCallerId,
                session.P1LuzMalaActive,
                session.P2LuzMalaActive,
                clientRpcParams);

            if (session.Player2Type == ParticipantType.Bot)
            {
                string delimited = (session.BotAvailablePowerIds != null && session.BotAvailablePowerIds.Count > 0)
                    ? string.Join(",", session.BotAvailablePowerIds)
                    : "";
                SyncBotPowersClientRpc(delimited, clientRpcParams);
            }
        }

        public void PromptGuampasChoice(TableSession session, ulong targetClientId, int drawnCardIndex)
        {
            if (session == null || session.GameModel == null || masterDeck == null) return;
            int team = (targetClientId == session.Player1Id) ? 1 : 2;
            List<int> hand = (team == 1) ? session.GameModel.P1Hand : session.GameModel.P2Hand;
            if (hand == null || hand.Count == 0) return;

            if (drawnCardIndex < 0 || drawnCardIndex >= masterDeck.AvailableCards.Count) return;

            int[] handIndices = hand.ToArray();

            ClientRpcParams rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { targetClientId } }
            };
            PromptGuampasChoiceClientRpc(targetClientId, drawnCardIndex, handIndices, rpcParams);

            ulong opponentId = (targetClientId == session.Player1Id) ? session.Player2Id : session.Player1Id;
            if (opponentId != ulong.MaxValue && opponentId != targetClientId)
            {
                SendPowerToast(session, opponentId, "GUAMPAS", "Tu rival activó Guampas y está decidiendo si cambiar una carta.");
            }
        }

        [ClientRpc]
        public void PromptGuampasChoiceClientRpc(
            ulong targetClientId,
            int drawnCardIndex,
            int[] handIndices,
            ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClientId != targetClientId) return;

            if (NetworkManager.Singleton.SpawnManager != null && NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
            {
                PlayerController pc = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.PromptGuampasChoice(drawnCardIndex, handIndices);
                }
            }
        }

        public void CancelGuampasChoice(TableSession session)
        {
            if (session == null || session.GuampasPendingPlayerId == 0) return;
            ulong pendingId = session.GuampasPendingPlayerId;
            session.GuampasPendingPlayerId = 0;
            session.GuampasDrawnCardIndex = -1;

            ClientRpcParams rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { pendingId } }
            };
            DismissGuampasChoiceClientRpc(pendingId, rpcParams);
        }

        [ClientRpc]
        public void DismissGuampasChoiceClientRpc(ulong targetClientId, ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClientId != targetClientId) return;

            if (NetworkManager.Singleton.SpawnManager != null && NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
            {
                PlayerController pc = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.DismissGuampasChoice();
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SubmitGuampasChoiceServerRpc(int cardToReplaceIndex, ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            var session = GetSessionByPlayer(sender);
            if (session == null || session.GuampasPendingPlayerId != sender) return;

            int drawnCard = session.GuampasDrawnCardIndex;
            session.GuampasPendingPlayerId = 0;
            session.GuampasDrawnCardIndex = -1;

            if (drawnCard < 0 || masterDeck == null || drawnCard >= masterDeck.AvailableCards.Count) return;

            List<int> hand = (sender == session.Player1Id) ? session.GameModel.P1Hand : session.GameModel.P2Hand;
            if (hand == null) return;

            CardData drawnData = masterDeck.AvailableCards[drawnCard];
            ulong opponentId = (sender == session.Player1Id) ? session.Player2Id : session.Player1Id;

            if (cardToReplaceIndex >= 0 && hand.Contains(cardToReplaceIndex))
            {
                CardData replacedData = masterDeck.AvailableCards[cardToReplaceIndex];
                int slot = hand.IndexOf(cardToReplaceIndex);
                hand[slot] = drawnCard;
                SyncPlayerHand(session, sender);
                SendPowerToast(session, sender, "GUAMPAS", $"¡Reemplazaste tu {replacedData.Number} de {replacedData.Suit} por el {drawnData.Number} de {drawnData.Suit}!");
                if (opponentId != ulong.MaxValue && opponentId != sender)
                {
                    SendPowerToast(session, opponentId, "GUAMPAS", "El rival cambió una de sus cartas con Guampas.");
                }
            }
            else
            {
                SendPowerToast(session, sender, "GUAMPAS", $"Descartaste el {drawnData.Number} de {drawnData.Suit}. Conservás tu mano.");
                if (opponentId != ulong.MaxValue && opponentId != sender)
                {
                    SendPowerToast(session, opponentId, "GUAMPAS", "El rival decidió no cambiar y conservó su mano.");
                }
            }

            SyncClientState(session);
            TryScheduleBotAction(session);
            CheckStartPavaCalienteTimer(session);
        }

        public void PromptGuitarraChoice(TableSession session, ulong targetClientId)
        {
            if (session == null || session.GameModel == null) return;
            int team = (targetClientId == session.Player1Id) ? 1 : 2;
            List<int> myHand = (team == 1) ? session.GameModel.P1Hand : session.GameModel.P2Hand;
            List<int> oppHand = (team == 1) ? session.GameModel.P2Hand : session.GameModel.P1Hand;
            ulong opponentId = (team == 1) ? session.Player2Id : session.Player1Id;

            if (myHand == null || myHand.Count == 0 || oppHand == null || oppHand.Count == 0) return;

            session.GuitarraPendingPlayerId = targetClientId;

            int[] myCardsArray = myHand.ToArray();
            int[] oppSlotsArray = new int[oppHand.Count];
            var revealedSet = (team == 1) ? session.P1RevealedOpponentCards : session.P2RevealedOpponentCards;

            for (int i = 0; i < oppHand.Count; i++)
            {
                int cardIdx = oppHand[i];
                oppSlotsArray[i] = (revealedSet != null && revealedSet.Contains(cardIdx)) ? cardIdx : -1;
            }

            ClientRpcParams rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { targetClientId } }
            };
            PromptGuitarraChoiceClientRpc(targetClientId, myCardsArray, oppSlotsArray, rpcParams);

            if (opponentId != ulong.MaxValue && opponentId != targetClientId)
            {
                SendPowerToast(session, opponentId, "GUITARRA", "Tu rival activó la Guitarra y está eligiendo las cartas a intercambiar...");
            }
        }

        [ClientRpc]
        public void PromptGuitarraChoiceClientRpc(
            ulong targetClientId,
            int[] myHandCards,
            int[] opponentSlots,
            ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClientId != targetClientId) return;

            if (NetworkManager.Singleton.SpawnManager != null && NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
            {
                PlayerController pc = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.PromptGuitarraChoice(myHandCards, opponentSlots);
                }
            }
        }

        public void CancelGuitarraChoice(TableSession session)
        {
            if (session == null || session.GuitarraPendingPlayerId == 0) return;
            ulong pendingId = session.GuitarraPendingPlayerId;
            session.GuitarraPendingPlayerId = 0;

            ClientRpcParams rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { pendingId } }
            };
            DismissGuitarraChoiceClientRpc(pendingId, rpcParams);
        }

        [ClientRpc]
        public void DismissGuitarraChoiceClientRpc(ulong targetClientId, ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClientId != targetClientId) return;

            if (NetworkManager.Singleton.SpawnManager != null && NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
            {
                PlayerController pc = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.DismissGuitarraChoice();
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayFaconWithTargetServerRpc(int targetOpponentSlot, ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            var session = GetSessionByPlayer(sender);
            if (session == null || session.GameModel == null) return;
            if (masterDeck == null) return;

            int team = (sender == session.Player1Id) ? 1 : 2;
            List<int> opponentHand = (team == 1) ? session.GameModel.P2Hand : session.GameModel.P1Hand;
            ulong opponentId = (team == 1) ? session.Player2Id : session.Player1Id;

            if (opponentHand == null || targetOpponentSlot < 0 || targetOpponentSlot >= opponentHand.Count) return;

            NotifyPowerActivatedClientRpc(sender, "facon");

            int destroyedCardIndex = opponentHand[targetOpponentSlot];
            CardData destroyedCard = masterDeck.AvailableCards[destroyedCardIndex];

            // 1. Descartar la carta de la mano del rival
            opponentHand.RemoveAt(targetOpponentSlot);

            // 2. Darle una carta aleatoria nueva del mazo no repartido al rival
            int newCardIndex = GetUnusedCard(session);
            if (newCardIndex != -1)
            {
                opponentHand.Add(newCardIndex);
            }

            // Si estaba en reveladas, removerla
            if (team == 1 && session.P1RevealedOpponentCards != null) session.P1RevealedOpponentCards.Remove(destroyedCardIndex);
            if (team == 2 && session.P2RevealedOpponentCards != null) session.P2RevealedOpponentCards.Remove(destroyedCardIndex);

            // Notificar al atacante para que vea la carta que rompió antes de que se descarte
            ClientRpcParams rpcParamsSender = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { sender } }
            };
            RevealAndBreakOpponentCardClientRpc(sender, targetOpponentSlot, destroyedCardIndex, newCardIndex, rpcParamsSender);

            // Sincronizar manos
            SyncPlayerHand(session, sender);
            SyncPlayerHand(session, opponentId);

            SendPowerToast(session, sender, "FACÓN", 
                $"¡Le rompiste el {destroyedCard.Number} de {destroyedCard.Suit} al rival! Robó una nueva carta del mazo.");

            if (opponentId != ulong.MaxValue && opponentId != sender)
            {
                SendPowerToast(session, opponentId, "FACÓN", 
                    $"¡El rival usó el Facón y destrozó tu {destroyedCard.Number} de {destroyedCard.Suit}! Robaste una nueva carta del mazo.");
            }

            SyncClientState(session);
            TryScheduleBotAction(session);
            CheckStartPavaCalienteTimer(session);
        }

        [ClientRpc]
        public void RevealAndBreakOpponentCardClientRpc(
            ulong targetClientId,
            int slotIndex,
            int destroyedCardIndex,
            int newCardIndex,
            ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClientId != targetClientId) return;

            if (NetworkManager.Singleton.SpawnManager != null && NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
            {
                PlayerController pc = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.RevealAndBreakOpponentCard(slotIndex, destroyedCardIndex, newCardIndex);
                }
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayGuitarraWithTargetServerRpc(int myCardIndex, int targetOpponentSlot, ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            var session = GetSessionByPlayer(sender);
            if (session == null || session.GameModel == null) return;
            if (masterDeck == null) return;

            int team = (sender == session.Player1Id) ? 1 : 2;
            List<int> playerHand = (team == 1) ? session.GameModel.P1Hand : session.GameModel.P2Hand;
            List<int> opponentHand = (team == 1) ? session.GameModel.P2Hand : session.GameModel.P1Hand;
            ulong opponentId = (team == 1) ? session.Player2Id : session.Player1Id;

            if (playerHand == null || opponentHand == null) return;
            if (!playerHand.Contains(myCardIndex)) return;
            if (targetOpponentSlot < 0 || targetOpponentSlot >= opponentHand.Count) return;

            NotifyPowerActivatedClientRpc(sender, "guitarra");

            int oppCardIndex = opponentHand[targetOpponentSlot];

            // Intercambiar cartas
            playerHand.Remove(myCardIndex);
            playerHand.Add(oppCardIndex);

            opponentHand.RemoveAt(targetOpponentSlot);
            opponentHand.Add(myCardIndex);

            // Actualizar conjuntos de cartas reveladas
            if (team == 1)
            {
                if (session.P1RevealedOpponentCards != null) session.P1RevealedOpponentCards.Remove(oppCardIndex);
                if (session.P2RevealedOpponentCards != null) session.P2RevealedOpponentCards.Remove(myCardIndex);
            }
            else
            {
                if (session.P2RevealedOpponentCards != null) session.P2RevealedOpponentCards.Remove(oppCardIndex);
                if (session.P1RevealedOpponentCards != null) session.P1RevealedOpponentCards.Remove(myCardIndex);
            }

            SyncPlayerHand(session, sender);
            SyncPlayerHand(session, opponentId);

            CardData givenCard = masterDeck.AvailableCards[myCardIndex];
            CardData receivedCard = masterDeck.AvailableCards[oppCardIndex];

            SendPowerToast(session, sender, "GUITARRA", 
                $"¡Intercambio realizado! Diste tu {givenCard.Number} de {givenCard.Suit} y te quedaste con el {receivedCard.Number} de {receivedCard.Suit} del rival.");

            if (opponentId != ulong.MaxValue && opponentId != sender)
            {
                SendPowerToast(session, opponentId, "GUITARRA", 
                    $"¡Guitarreada rival! El rival te quitó una carta y te dio un {givenCard.Number} de {givenCard.Suit}.");
            }

            SyncClientState(session);
            TryScheduleBotAction(session);
            CheckStartPavaCalienteTimer(session);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SubmitGuitarraChoiceServerRpc(int myCardIndex, int targetOpponentSlot, ServerRpcParams rpcParams = default)
        {
            PlayGuitarraWithTargetServerRpc(myCardIndex, targetOpponentSlot, rpcParams);
        }
    }
}





