using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Trucardo.Core
{
    /// <summary>
    /// Gestiona la red desde el lado del Servidor y delega la lógica al GameModel.
    /// También actúa como el "Dealer" de la partida.
    /// </summary>
    public class GameServer : NetworkBehaviour
    {
        [Header("Datos Generales")]
        [Tooltip("Referencia al MainDeck generado con las 40 cartas.")]
        public DeckData masterDeck;

        // Instancia del Modelo (Lógica pura separada de la red)
        public GameModel gameModel;

        // Control de los dos jugadores (Fase 1: MVP 1v1)
        private ulong player1Id = ulong.MaxValue;
        private ulong player2Id = ulong.MaxValue;
        
        private bool player1Seated = false;
        private bool player2Seated = false;

        private void Awake()
        {
            gameModel = new GameModel(masterDeck);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }

            Debug.Log("Servidor de Trucardo Iniciado.");
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (player1Id == ulong.MaxValue)
            {
                player1Id = clientId;
                gameModel.Player1Id = clientId;
                Debug.Log($"Jugador 1 asignado al ClientID: {clientId}");
            }
            else if (player2Id == ulong.MaxValue)
            {
                player2Id = clientId;
                gameModel.Player2Id = clientId;
                Debug.Log($"Jugador 2 asignado al ClientID: {clientId}");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayerSitDownServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            
            if (sender == player1Id) player1Seated = true;
            else if (sender == player2Id) player2Seated = true;

            Debug.Log($"El Jugador {sender} se ha sentado en la mesa.");

            CheckStartGame();
        }

        [Header("Cartas")]
        public GameObject cardPrefab;
        private List<GameObject> tableCards = new List<GameObject>();

        [ServerRpc(RequireOwnership = false)]
        public void PlayCardServerRpc(int cardIndex, ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            
            // Validar turno en el servidor ANTES de hacer nada visual
            if (gameModel.CurrentState != GameState.TurnoJugador) return;
            if (gameModel.CurrentTurnId != sender) return;
            
            // Validar que la tenga en la mano
            bool isP1 = (sender == player1Id);
            var hand = isP1 ? gameModel.P1Hand : gameModel.P2Hand;
            if (!hand.Contains(cardIndex)) return;

            gameModel.actionQueue.EnqueueCommand(new Trucardo.Core.Commands.PlayCardCommand(gameModel, sender, cardIndex));
            gameModel.actionQueue.ProcessNext();

            SpawnCardOnTable(sender, cardIndex);
            
            NotifyOpponentPlayedCardClientRpc(sender);

            if (gameModel.CurrentState == GameState.RondaTerminada)
            {
                if (!IsInvoking(nameof(StartRound)))
                {
                    Invoke(nameof(StartRound), 3f);
                }
            }
            
            SyncClientState();
        }

        private void SpawnCardOnTable(ulong playerId, int cardIndex)
        {
            if (cardPrefab == null) return;

            GameObject c = Instantiate(cardPrefab);
            NetworkObject netObj = c.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();

            // Encontrar la posición de la mano del jugador
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
            // Obtener el índice de la carta jugada de forma segura usando el estado del juego
            int playerPlayedCount = 0;
            if (playerId == player1Id) playerPlayedCount = 2 - gameModel.P1Hand.Count;
            else if (playerId == player2Id) playerPlayedCount = 2 - gameModel.P2Hand.Count;

            // Ubicación en base a la mesa:
            // Para que las cartas de AMBOS jugadores se tiren de izquierda a derecha en el mismo sentido global:
            float xSpacing = 0.22f;
            float xOffset = -xSpacing + (playerPlayedCount * xSpacing);
            
            // Separación del centro de la mesa
            float zOffset = (playerId == player1Id) ? -0.18f : 0.18f;
            
            // Bajamos las cartas 20cm (antes 0.952f, ahora 0.752f) para coincidir con la mesa baja
            Vector3 targetPos = new Vector3(xOffset, 0.752f, zOffset); 
            
            // Rotar para que queden derechas mirando hacia el jugador que las tiró
            float yRot = (playerId == player1Id) ? 0 : 180;
            Quaternion targetRot = Quaternion.Euler(0, yRot, 0);
            
            // Animar (el NetworkTransform sincronizará este movimiento a todos los clientes)
            CardVisual visual = c.GetComponent<CardVisual>();
            if (visual != null) 
            {
                visual.netCardIndex.Value = cardIndex;
                visual.PlayToTable(targetPos, targetRot, 0.6f);
            }

            tableCards.Add(c);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CallEnvidoServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            gameModel.actionQueue.EnqueueCommand(new Trucardo.Core.Commands.CallEnvidoCommand(gameModel, sender));
            gameModel.actionQueue.ProcessNext();
            SyncClientState();
        }

        [ServerRpc(RequireOwnership = false)]
        public void CallTrucoServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            gameModel.actionQueue.EnqueueCommand(new Trucardo.Core.Commands.CallTrucoCommand(gameModel, sender));
            gameModel.actionQueue.ProcessNext();
            SyncClientState();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RespondServerRpc(bool accepts, ServerRpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            gameModel.actionQueue.EnqueueCommand(new Trucardo.Core.Commands.RespondCommand(gameModel, sender, accepts));
            gameModel.actionQueue.ProcessNext();

            if (gameModel.CurrentState == GameState.RondaTerminada)
            {
                if (!IsInvoking(nameof(StartRound)))
                {
                    Invoke(nameof(StartRound), 3f);
                }
            }
            SyncClientState();
        }

        private void CheckStartGame()
        {
            if (player1Seated && player2Seated)
            {
                Debug.Log("¡Ambos jugadores están sentados! El Dealer comenzará a repartir.");
                StartRound();
            }
        }

        private void StartRound()
        {
            gameModel.ResetHand();

            foreach (var c in tableCards)
            {
                if (c != null)
                {
                    NetworkObject netObj = c.GetComponent<NetworkObject>();
                    if (netObj != null && netObj.IsSpawned) netObj.Despawn();
                    Destroy(c);
                }
            }
            tableCards.Clear();

            List<int> deckIndices = new List<int>();
            for (int i = 0; i < 40; i++) deckIndices.Add(i);
            
            // Mezclar
            for (int i = 0; i < deckIndices.Count; i++)
            {
                int temp = deckIndices[i];
                int randomIndex = UnityEngine.Random.Range(i, deckIndices.Count);
                deckIndices[i] = deckIndices[randomIndex];
                deckIndices[randomIndex] = temp;
            }

            int[] p1Cards = new int[] { deckIndices[0], deckIndices[1], deckIndices[2] };
            int[] p2Cards = new int[] { deckIndices[3], deckIndices[4], deckIndices[5] };

            gameModel.DealCardsToPlayer(1, new List<int>(p1Cards));
            gameModel.DealCardsToPlayer(2, new List<int>(p2Cards));

            ClientRpcParams clientRpcParamsP1 = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { player1Id } }
            };
            ReceiveCardsClientRpc(p1Cards, clientRpcParamsP1);

            ClientRpcParams clientRpcParamsP2 = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { player2Id } }
            };
            ReceiveCardsClientRpc(p2Cards, clientRpcParamsP2);

            // Turno del Jugador 1 por defecto al arrancar
            gameModel.CurrentState = GameState.TurnoJugador;
            gameModel.CurrentTurnId = player1Id;
            Debug.Log($"Turno actual: Jugador 1 ({player1Id})");
            
            // Sincronizar UI de clientes
            SyncClientState();
        }

        [ClientRpc]
        private void ReceiveCardsClientRpc(int[] cardIndices, ClientRpcParams clientRpcParams = default)
        {
            Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] ¡He recibido mis cartas!");
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
        private void UpdateClientStateClientRpc(GameState state, ulong turnId, int p1Score, int p2Score, TipoCanto canto, ulong lastCaller, NivelTruco estadoTruco, ulong p1Id)
        {
            if (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
            {
                PlayerController pc = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<PlayerController>();
                if (pc != null) pc.UpdateUIState(state, turnId, p1Score, p2Score, canto, lastCaller, estadoTruco, p1Id);
            }
        }

        private void SyncClientState()
        {
            UpdateClientStateClientRpc(gameModel.CurrentState, gameModel.CurrentTurnId, gameModel.ScoreTeam1, gameModel.ScoreTeam2, gameModel.CantoPendiente, gameModel.LastCallerId, gameModel.EstadoTruco, player1Id);
        }
    }
}
