using System.Collections.Generic;
using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Efecto del Poder "Mate":
    /// Revela una de las cartas ocultas del oponente en la mesa.
    /// </summary>
    [CreateAssetMenu(fileName = "MateEffect", menuName = "Trucardo/Powers/Effects/Mate")]
    public class MateEffect : PowerEffect
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
                reason = "El oponente no tiene cartas en mano para revelar.";
                return false;
            }

            return true;
        }

        public override bool Execute(PowerContext context)
        {
            GameServer server = Object.FindAnyObjectByType<GameServer>();
            if (server == null || server.masterDeck == null)
            {
                Debug.LogWarning("[Poder Mate] No se encontró GameServer o masterDeck.");
                return false;
            }

            var session = server.GetSessionByPlayer(context.PlayerId);
            if (session == null || session.GameModel == null)
            {
                Debug.LogWarning("[Poder Mate] No se encontró una sesión activa de juego.");
                return false;
            }

            List<int> opponentHand = (context.Team == 1) ? session.GameModel.P2Hand : session.GameModel.P1Hand;
            if (opponentHand == null || opponentHand.Count == 0)
            {
                Debug.Log("[Poder Mate] El oponente no tiene cartas en mano para revelar.");
                return false;
            }

            int cardIndexToReveal = opponentHand[Random.Range(0, opponentHand.Count)];
            if (cardIndexToReveal < 0 || cardIndexToReveal >= server.masterDeck.AvailableCards.Count) return false;
            CardData cardData = server.masterDeck.AvailableCards[cardIndexToReveal];

            // Registrar en el conjunto de cartas reveladas de la sesión para sinergia (ej. Guitarra)
            if (context.Team == 1 && session.P1RevealedOpponentCards != null)
            {
                session.P1RevealedOpponentCards.Add(cardIndexToReveal);
            }
            else if (context.Team == 2 && session.P2RevealedOpponentCards != null)
            {
                session.P2RevealedOpponentCards.Add(cardIndexToReveal);
            }

            ulong rivalId = (context.Team == 1) ? session.Player2Id : session.Player1Id;

            server.RevealOpponentCardToPlayer(session, context.PlayerId, cardIndexToReveal);
            server.SendPowerToast(session, context.PlayerId, "MATE", $"¡Descubriste una carta del rival: {cardData.Number} de {cardData.Suit}!");

            if (rivalId != ulong.MaxValue && rivalId != context.PlayerId)
            {
                server.SendPowerToast(session, rivalId, "MATE RIVAL", $"¡El rival tomó un mate y espió tu {cardData.Number} de {cardData.Suit}!");
            }

            Debug.Log($"<color=#4CAF50><b>[Poder Mate]</b></color> ¡Has descubierto una carta del rival: <b>{cardData.Number} de {cardData.Suit}</b>!");
            return true;
        }
    }
}
