using System.Collections.Generic;
using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Efecto del Poder "Guitarra":
    /// Intercambia una de tus cartas por una del rival a ciegas.
    /// </summary>
    [CreateAssetMenu(fileName = "GuitarraEffect", menuName = "Trucardo/Powers/Effects/Guitarra")]
    public class GuitarraEffect : PowerEffect
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

            List<int> playerHand = (context.Team == 1) ? session.GameModel.P1Hand : session.GameModel.P2Hand;
            List<int> opponentHand = (context.Team == 1) ? session.GameModel.P2Hand : session.GameModel.P1Hand;

            if (playerHand == null || playerHand.Count == 0 || opponentHand == null || opponentHand.Count == 0)
            {
                reason = "No hay suficientes cartas para intercambiar.";
                return false;
            }

            return true;
        }

        public override bool Execute(PowerContext context)
        {
            GameServer server = Object.FindAnyObjectByType<GameServer>();
            if (server == null || server.masterDeck == null)
            {
                Debug.LogWarning("[Poder Guitarra] No se encontró GameServer o masterDeck.");
                return false;
            }

            var session = server.GetSessionByPlayer(context.PlayerId);
            if (session == null || session.GameModel == null)
            {
                Debug.LogWarning("[Poder Guitarra] No se encontró una sesión activa de juego.");
                return false;
            }

            List<int> playerHand = (context.Team == 1) ? session.GameModel.P1Hand : session.GameModel.P2Hand;
            List<int> opponentHand = (context.Team == 1) ? session.GameModel.P2Hand : session.GameModel.P1Hand;
            ulong rivalId = (context.Team == 1) ? session.Player2Id : session.Player1Id;

            if (playerHand == null || playerHand.Count == 0 || opponentHand == null || opponentHand.Count == 0)
            {
                Debug.Log("[Poder Guitarra] No hay suficientes cartas para intercambiar.");
                return false;
            }

            bool isHuman = (context.Team == 1 && session.Player1Type == ParticipantType.Human) ||
                           (context.Team == 2 && session.Player2Type == ParticipantType.Human);

            if (isHuman)
            {
                return true;
            }

            // Lógica para Bot:
            // 1. Dar la carta propia con menor TrucoRank
            int myCardIndex = playerHand[0];
            int lowestRank = int.MaxValue;
            foreach (int c in playerHand)
            {
                if (c >= 0 && c < server.masterDeck.AvailableCards.Count)
                {
                    int rank = server.masterDeck.AvailableCards[c].TrucoRank;
                    if (rank < lowestRank)
                    {
                        lowestRank = rank;
                        myCardIndex = c;
                    }
                }
            }

            // 2. Elegir carta del oponente: si conoce alguna revelada de buen valor, tomarla; sino elegir a ciegas
            var revealed = (context.Team == 1) ? session.P1RevealedOpponentCards : session.P2RevealedOpponentCards;
            int oppCardIndex = -1;
            int bestRevealedRank = -1;
            if (revealed != null)
            {
                foreach (int c in opponentHand)
                {
                    if (revealed.Contains(c) && c >= 0 && c < server.masterDeck.AvailableCards.Count)
                    {
                        int rank = server.masterDeck.AvailableCards[c].TrucoRank;
                        if (rank > bestRevealedRank)
                        {
                            bestRevealedRank = rank;
                            oppCardIndex = c;
                        }
                    }
                }
            }
            if (oppCardIndex == -1)
            {
                oppCardIndex = opponentHand[Random.Range(0, opponentHand.Count)];
            }

            // Intercambiar en GameModel
            playerHand.Remove(myCardIndex);
            playerHand.Add(oppCardIndex);

            opponentHand.Remove(oppCardIndex);
            opponentHand.Add(myCardIndex);

            // Actualizar cartas reveladas
            if (context.Team == 1)
            {
                if (session.P1RevealedOpponentCards != null) session.P1RevealedOpponentCards.Remove(oppCardIndex);
                if (session.P2RevealedOpponentCards != null) session.P2RevealedOpponentCards.Remove(myCardIndex);
            }
            else
            {
                if (session.P2RevealedOpponentCards != null) session.P2RevealedOpponentCards.Remove(oppCardIndex);
                if (session.P1RevealedOpponentCards != null) session.P1RevealedOpponentCards.Remove(myCardIndex);
            }

            CardData givenCard = server.masterDeck.AvailableCards[myCardIndex];
            CardData receivedCard = server.masterDeck.AvailableCards[oppCardIndex];

            // Sincronizar ambas manos
            server.SyncPlayerHand(session, context.PlayerId);
            server.SyncPlayerHand(session, rivalId);

            server.SendPowerToast(session, rivalId, "GUITARRA", 
                $"¡Guitarreada rival! El rival te quitó una carta y te dio un {givenCard.Number} de {givenCard.Suit}!");

            server.SyncClientState(session);
            Debug.Log($"<color=#D7CCC8><b>[Poder Guitarra]</b></color> El bot intercambió su <b>{givenCard.Number} de {givenCard.Suit}</b> por la carta <b>{receivedCard.Number} de {receivedCard.Suit}</b> del rival.");
            return true;
        }
    }
}
