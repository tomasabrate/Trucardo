using System.Collections.Generic;
using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Efecto del Poder "Facón":
    /// Rompe una carta a elección del rival.
    /// </summary>
    [CreateAssetMenu(fileName = "FaconEffect", menuName = "Trucardo/Powers/Effects/Facón")]
    public class FaconEffect : PowerEffect
    {
        public override bool CanExecute(PowerContext context, out string reason)
        {
            reason = string.Empty;
            GameServer server = Object.FindAnyObjectByType<GameServer>();
            if (server == null)
            {
                reason = "No se encontró el servidor.";
                return false;
            }

            var session = server.GetSessionByPlayer(context.PlayerId);
            if (session == null || session.GameModel == null)
            {
                reason = "No hay partida activa.";
                return false;
            }

            List<int> opponentHand = (context.Team == 1) ? session.GameModel.P2Hand : session.GameModel.P1Hand;
            if (opponentHand == null || opponentHand.Count == 0)
            {
                reason = "El oponente no tiene cartas en mano para destruir.";
                return false;
            }

            return true;
        }

        public override bool Execute(PowerContext context)
        {
            GameServer server = Object.FindAnyObjectByType<GameServer>();
            if (server == null || server.masterDeck == null)
            {
                Debug.LogWarning("[Poder Facón] No se encontró GameServer o masterDeck.");
                return false;
            }

            var session = server.GetSessionByPlayer(context.PlayerId);
            if (session == null || session.GameModel == null)
            {
                Debug.LogWarning("[Poder Facón] No se encontró una sesión activa de juego.");
                return false;
            }

            List<int> opponentHand = (context.Team == 1) ? session.GameModel.P2Hand : session.GameModel.P1Hand;
            List<int> playerHand = (context.Team == 1) ? session.GameModel.P1Hand : session.GameModel.P2Hand;
            ulong rivalId = (context.Team == 1) ? session.Player2Id : session.Player1Id;

            if (opponentHand == null || opponentHand.Count == 0)
            {
                Debug.Log("[Poder Facón] El oponente no tiene cartas en mano para destruir.");
                return false;
            }

            // Lógica para Bot: si conoce alguna revelada de buen valor, la rompe; si no, elige al azar
            var revealed = (context.Team == 1) ? session.P1RevealedOpponentCards : session.P2RevealedOpponentCards;
            int targetSlot = -1;
            int bestRank = -1;
            if (revealed != null)
            {
                for (int i = 0; i < opponentHand.Count; i++)
                {
                    int c = opponentHand[i];
                    if (revealed.Contains(c) && c >= 0 && c < server.masterDeck.AvailableCards.Count)
                    {
                        int rank = server.masterDeck.AvailableCards[c].TrucoRank;
                        if (rank > bestRank)
                        {
                            bestRank = rank;
                            targetSlot = i;
                        }
                    }
                }
            }
            if (targetSlot == -1)
            {
                targetSlot = Random.Range(0, opponentHand.Count);
            }

            // 1. Destruir la carta elegida de la mano del rival
            int cardIndexToDestroy = opponentHand[targetSlot];
            opponentHand.RemoveAt(targetSlot);
            CardData destroyedCard = server.masterDeck.AvailableCards[cardIndexToDestroy];

            // 2. Darle una carta aleatoria nueva del mazo al rival
            int newCardIndex = server.GetUnusedCard(session);
            if (newCardIndex != -1)
            {
                opponentHand.Add(newCardIndex);
            }

            // Si la carta estaba en las cartas reveladas del rival, removerla
            if (context.Team == 1 && session.P1RevealedOpponentCards != null)
            {
                session.P1RevealedOpponentCards.Remove(cardIndexToDestroy);
            }
            else if (context.Team == 2 && session.P2RevealedOpponentCards != null)
            {
                session.P2RevealedOpponentCards.Remove(cardIndexToDestroy);
            }

            // Notificaciones
            server.SendPowerToast(session, rivalId, "FACÓN", 
                $"¡El rival usó el Facón y destrozó tu {destroyedCard.Number} de {destroyedCard.Suit}! Robaste una nueva carta del mazo.");

            // Sincronizar ambas manos
            server.SyncPlayerHand(session, context.PlayerId);
            server.SyncPlayerHand(session, rivalId);

            server.SyncClientState(session);
            return true;
        }
    }
}
