using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using Trucardo.Core.Bots;

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
        public Coroutine PendingBotActionCoroutine;
        public Coroutine PendingStartRoundCoroutine;
        public List<GameObject> TableCards = new List<GameObject>();
        public bool IsStartingRound = false;
    }

    public class GameServer : NetworkBehaviour
    {
        private const ulong FirstLogicalBotId = ulong.MaxValue - 1;

        [Header("Datos Generales")]
        public DeckData masterDeck;
        [SerializeField] private bool autoFillPlayer2WithBot = false;
        [SerializeField, Min(0f)] private float botActionDelaySeconds = 0.75f;
        [Header("Cartas")]
        public GameObject cardPrefab;

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
            Debug.Log("Servidor de Trucardo Iniciado. Soportando m�ltiples mesas.");
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }

        public override void OnNetworkDespawn()
        {
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

        private TableSession GetSessionByPlayer(ulong clientId)
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

        private void TryAutoFillPlayer2WithBot(TableSession session)
        {
            if (!autoFillPlayer2WithBot) return;
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
            session.Player2BotController = new BotController(new BotBrain());

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
            session.Player2Id = ulong.MaxValue;
            session.Player2Seated = false;
            session.Player2Type = ParticipantType.None;
        }

        private void TryScheduleBotAction(TableSession session)
        {
            if (session == null || session.PendingBotActionCoroutine != null) return;
            if (!CanPlayer2BotTakeAction(session, session.Player2Id)) return;

            ulong botId = session.Player2Id;
            session.PendingBotActionCoroutine = StartCoroutine(ProcessBotTurnDelayed(session, botId));
        }

        private IEnumerator ProcessBotTurnDelayed(TableSession session, ulong botId)
        {
            yield return new WaitForSeconds(botActionDelaySeconds);
            session.PendingBotActionCoroutine = null;

            if (!CanPlayer2BotTakeAction(session, botId)) yield break;

            BotObservation observation = BuildBotObservation(session, botId);
            BotDecision decision = session.Player2BotController.Decide(observation);

            if (decision == null) yield break;

            if (decision.Type == BotDecisionType.Respond && CanPlayer2BotRespond(session, botId))
            {
                ProcessResponse(botId, decision.Accepts);
            }
            else if (decision.Type == BotDecisionType.CallEnvido && CanPlayer2BotCallEnvido(session, botId))
            {
                ProcessCallEnvido(botId);
            }
            else if (decision.Type == BotDecisionType.CallTruco && CanPlayer2BotCallTruco(session, botId))
            {
                ProcessCallTruco(botId);
            }
            else if (decision.Type == BotDecisionType.PlayCard && CanPlayer2BotPlayCard(session, botId))
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
                CanPlayer2BotCallEnvido(session, botId),
                CanPlayer2BotCallTruco(session, botId),
                model.ScoreTeam1,
                model.ScoreTeam2,
                model.CurrentRoundIndex,
                model.RoundWinners,
                model.EnvidoCantado,
                model.EstadoTruco,
                BuildBotCardObservations(model, model.P2Hand),
                BuildBotCardObservations(model, model.P1PlayedCards),
                BuildBotCardObservations(model, model.P2PlayedCards));
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

        private bool CanPlayer2BotCallEnvido(TableSession session, ulong expectedBotId)
        {
            return IsPlayer2BotActive(session, expectedBotId)
                && session.GameModel.CurrentState == GameState.TurnoJugador
                && session.GameModel.CurrentTurnId == expectedBotId
                && session.GameModel.CurrentRoundIndex == 0
                && !session.GameModel.EnvidoCantado
                && session.GameModel.CantoPendiente == TipoCanto.Ninguno
                && session.GameModel.EstadoTruco == NivelTruco.Ninguno
                && session.GameModel.P2PlayedCards.Count == 0;
        }

        private bool CanPlayer2BotCallTruco(TableSession session, ulong expectedBotId)
        {
            return IsPlayer2BotActive(session, expectedBotId)
                && session.GameModel.CurrentState == GameState.TurnoJugador
                && session.GameModel.CurrentTurnId == expectedBotId
                && session.GameModel.CantoPendiente == TipoCanto.Ninguno
                && session.GameModel.EstadoTruco == NivelTruco.Ninguno;
        }

        private bool CanPlayer2BotTakeAction(TableSession session, ulong expectedBotId)
        {
            return CanPlayer2BotRespond(session, expectedBotId)
                || CanPlayer2BotCallEnvido(session, expectedBotId)
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

            session.GameModel.actionQueue.EnqueueCommand(new Trucardo.Core.Commands.PlayCardCommand(session.GameModel, actorId, cardIndex));
            session.GameModel.actionQueue.ProcessNext();

            SpawnCardOnTable(session, actorId, cardIndex);
            
            NotifyOpponentPlayedCardClientRpc(actorId);

            if (session.GameModel.CurrentState == GameState.RondaTerminada)
            {
                ScheduleStartRound(session, 3f);
            }
            
            SyncClientState(session);
            TryScheduleBotAction(session);
        }

        private void SpawnCardOnTable(TableSession session, ulong playerId, int cardIndex)
        {
            if (cardPrefab == null) return;

            GameObject c = Instantiate(cardPrefab);
            NetworkObject netObj = c.GetComponent<NetworkObject>();
            // Spawn later

            Vector3 startPos = Vector3.zero;
            Quaternion startRot = Quaternion.identity;
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                if (client.Value.ClientId == playerId)
                {
                    startPos = client.Value.PlayerObject.transform.position + client.Value.PlayerObject.transform.forward * 0.8f;
                    startPos.y = 0.80f; 
                    
                    Vector3 forwardOnTable = client.Value.PlayerObject.transform.forward;
                    forwardOnTable.y = 0;
                    startRot = Quaternion.LookRotation(forwardOnTable, Vector3.up);
                    startRot *= Quaternion.Euler(-35f, 0f, 0f); 
                }
            }

            c.transform.position = startPos;
            c.transform.rotation = startRot;
            if (netObj != null) netObj.Spawn();
            int playerPlayedCount = 0;
            if (playerId == session.Player1Id) playerPlayedCount = 2 - session.GameModel.P1Hand.Count;
            else if (playerId == session.Player2Id) playerPlayedCount = 2 - session.GameModel.P2Hand.Count;

                        float zSpacing = 0.22f;
            // Para que queden enfrentadas en la mesa, ambas se propagan en la misma direccion fisica (+Z a -Z)
            // Jugador 1 (-X) lo vera de izquierda a derecha, Jugador 2 (+X) lo vera de derecha a izquierda
            float zOffset = zSpacing - (playerPlayedCount * zSpacing);
            
                        PlayerController pc = null;
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.ClientId == playerId)
                {
                    pc = client.PlayerObject.GetComponent<PlayerController>();
                    break;
                }
            }
            
            Vector3 tableCenter = session.Table.transform.position;
            bool isLeftSide = pc != null && pc.transform.position.x < tableCenter.x;
                        float xOffset = isLeftSide ? -0.2f : 0.2f;
            Vector3 targetPos = new Vector3(xOffset, 0f, zOffset);
            
            // Y de la misma forma determinamos la rotacion
            float yRot = isLeftSide ? 90f : -90f;
            Quaternion targetRot = Quaternion.Euler(0, yRot, 0);
            
            Transform mesa = session.Table != null ? session.Table.transform : null;

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
        public void CallEnvidoServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            ProcessCallEnvido(sender);
        }

        internal void ProcessCallEnvido(ulong actorId)
        {
            var session = GetSessionByPlayer(actorId);
            if (session == null) return;
            if (actorId != session.Player1Id && actorId != session.Player2Id) return;

            session.GameModel.actionQueue.EnqueueCommand(new Trucardo.Core.Commands.CallEnvidoCommand(session.GameModel, actorId));
            session.GameModel.actionQueue.ProcessNext();
            SyncClientState(session);
            TryScheduleBotAction(session);
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

            session.GameModel.actionQueue.EnqueueCommand(new Trucardo.Core.Commands.CallTrucoCommand(session.GameModel, actorId));
            session.GameModel.actionQueue.ProcessNext();
            SyncClientState(session);
            TryScheduleBotAction(session);
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

            session.GameModel.actionQueue.EnqueueCommand(new Trucardo.Core.Commands.RespondCommand(session.GameModel, actorId, accepts));
            session.GameModel.actionQueue.ProcessNext();

            if (session.GameModel.CurrentState == GameState.RondaTerminada)
            {
                ScheduleStartRound(session, 3f);
            }
            SyncClientState(session);
            TryScheduleBotAction(session);
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

        private void StartRound(TableSession session)
        {
            session.GameModel.ResetHand();
            ClearTableCards(session);

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
            session.GameModel.CurrentTurnId = session.Player1Id;
            
            SyncClientState(session);
            TryScheduleBotAction(session);
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
        private void UpdateClientStateClientRpc(GameState state, ulong turnId, int p1Score, int p2Score, TipoCanto canto, ulong lastCaller, NivelTruco estadoTruco, ulong p1Id, ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
            {
                PlayerController pc = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
                if (pc != null) pc.UpdateUIState(state, turnId, p1Score, p2Score, canto, lastCaller, estadoTruco, p1Id);
            }
        }

        private void SyncClientState(TableSession session)
        {
            List<ulong> seatedPlayers = new List<ulong>();
            if (session.Player1Seated && session.Player1Type == ParticipantType.Human) seatedPlayers.Add(session.Player1Id);
            if (session.Player2Seated && session.Player2Type == ParticipantType.Human) seatedPlayers.Add(session.Player2Id);
            if (seatedPlayers.Count == 0) return;

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = seatedPlayers.ToArray() }
            };
            UpdateClientStateClientRpc(session.GameModel.CurrentState, session.GameModel.CurrentTurnId, session.GameModel.ScoreTeam1, session.GameModel.ScoreTeam2, session.GameModel.CantoPendiente, session.GameModel.LastCallerId, session.GameModel.EstadoTruco, session.Player1Id, clientRpcParams);
        }
    }
}







