using System.Collections.Generic;
using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Efecto del Poder "Ceibo" (Legendario):
    /// Cambia el palo de todas tus cartas al mismo palo aleatorio (alto Envido asegurado).
    /// </summary>
    [CreateAssetMenu(fileName = "CeiboEffect", menuName = "Trucardo/Powers/Effects/Ceibo")]
    public class CeiboEffect : PowerEffect
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
            if (playerHand == null || playerHand.Count == 0)
            {
                reason = "No tienes cartas en mano para transformar.";
                return false;
            }

            return true;
        }

        public override bool Execute(PowerContext context)
        {
            GameServer server = Object.FindAnyObjectByType<GameServer>();
            if (server == null || server.masterDeck == null)
            {
                Debug.LogWarning("[Poder Ceibo] No se encontró GameServer o masterDeck.");
                return false;
            }

            var session = server.GetSessionByPlayer(context.PlayerId);
            if (session == null || session.GameModel == null)
            {
                Debug.LogWarning("[Poder Ceibo] No se encontró una sesión activa de juego.");
                return false;
            }

            List<int> playerHand = (context.Team == 1) ? session.GameModel.P1Hand : session.GameModel.P2Hand;
            if (playerHand == null || playerHand.Count == 0)
            {
                Debug.Log("[Poder Ceibo] No tienes cartas en mano para transformar.");
                return false;
            }

            // Elegir palo objetivo: preferir el palo de la carta con mayor envido actual
            CardSuit targetSuit = (CardSuit)Random.Range(0, 4);
            int maxEnvidoVal = -1;
            foreach (int cIdx in playerHand)
            {
                CardData cd = server.masterDeck.AvailableCards[cIdx];
                if (cd.EnvidoValue > maxEnvidoVal)
                {
                    maxEnvidoVal = cd.EnvidoValue;
                    targetSuit = cd.Suit;
                }
            }

            List<int> newHandIndices = new List<int>();

            for (int i = 0; i < playerHand.Count; i++)
            {
                int currentCardIdx = playerHand[i];
                CardData currentCard = server.masterDeck.AvailableCards[currentCardIdx];

                // Buscar la carta con el mismo número pero con targetSuit
                int matchIdx = currentCardIdx;
                for (int d = 0; d < server.masterDeck.AvailableCards.Count; d++)
                {
                    CardData deckCard = server.masterDeck.AvailableCards[d];
                    if (deckCard.Number == currentCard.Number && deckCard.Suit == targetSuit)
                    {
                        matchIdx = d;
                        break;
                    }
                }
                newHandIndices.Add(matchIdx);
            }

            playerHand.Clear();
            playerHand.AddRange(newHandIndices);

            server.SyncPlayerHand(session, context.PlayerId);
            server.SendPowerToast(session, context.PlayerId, "CEIBO", 
                $"¡Flor del Ceibo florecida! Todas tus cartas ahora son del palo {targetSuit}. ¡Envido asegurado!");

            server.SyncClientState(session);
            Debug.Log($"<color=#E91E63><b>[Poder Legendario: Ceibo]</b></color> ¡Flor del Ceibo florecida! Todas tus cartas ahora son del palo <b>{targetSuit}</b>. ¡Envido legendario asegurado!");
            return true;
        }
    }
}
