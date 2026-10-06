using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

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
        public List<GameObject> TableCards = new List<GameObject>();
        public bool IsStartingRound = false;
    }

    public class GameServer : NetworkBehaviour
    {
        [Header("Datos Generales")]
        public DeckData masterDeck;
        [Header("Cartas")]
        public GameObject cardPrefab;

        private List<TableSession> sessions = new List<TableSession>();

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
                if (session.Player1Id == clientId) { session.Player1Id = ulong.MaxValue; session.Player1Seated = false; session.Player1Type = ParticipantType.None; }
                if (session.Player2Id == clientId) { session.Player2Id = ulong.MaxValue; session.Player2Seated = false; session.Player2Type = ParticipantType.None; }
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

                [ServerRpc(RequireOwnership = false)]
        public void PlayerStandUpServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            var session = GetSessionByPlayer(sender);
            if (session != null)
            {
                if (session.Player1Id == sender) { session.Player1Id = ulong.MaxValue; session.Player1Seated = false; session.Player1Type = ParticipantType.None; }
                if (session.Player2Id == sender) { session.Player2Id = ulong.MaxValue; session.Player2Seated = false; session.Player2Type = ParticipantType.None; }
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
                if (oldSession.Player1Id == sender) { oldSession.Player1Id = ulong.MaxValue; oldSession.Player1Seated = false; oldSession.Player1Type = ParticipantType.None; }
                if (oldSession.Player2Id == sender) { oldSession.Player2Id = ulong.MaxValue; oldSession.Player2Seated = false; oldSession.Player2Type = ParticipantType.None; }
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

            CheckStartGame(session);
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
                if (!session.IsStartingRound)
                {
                    StartCoroutine(StartRoundDelayed(session, 3f));
                }
            }
            
            SyncClientState(session);
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
                if (!session.IsStartingRound)
                {
                    StartCoroutine(StartRoundDelayed(session, 3f));
                }
            }
            SyncClientState(session);
        }

        private void CheckStartGame(TableSession session)
        {
            bool canStart = session.Player1Seated && session.Player2Seated;

            if (canStart && session.GameModel.CurrentState == GameState.EsperandoJugadores)
            {
                Debug.Log($"Jugador listo en {session.TableId}! Repartiendo.");
                StartRound(session);
            }
        }

        private IEnumerator StartRoundDelayed(TableSession session, float delay)
        {
            session.IsStartingRound = true;
            yield return new WaitForSeconds(delay);
            session.IsStartingRound = false;
            StartRound(session);
        }

        private void StartRound(TableSession session)
        {
            session.GameModel.ResetHand();

            foreach (var c in session.TableCards)
            {
                if (c != null)
                {
                    NetworkObject netObj = c.GetComponent<NetworkObject>();
                    if (netObj != null && netObj.IsSpawned) netObj.Despawn();
                    Destroy(c);
                }
            }
            session.TableCards.Clear();

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







