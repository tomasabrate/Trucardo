using System.Collections.Generic;
using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Efecto del Poder "Lazo":
    /// Solo en primera mano: descarta todas tus cartas y roba 3 cartas nuevas.
    /// </summary>
    [CreateAssetMenu(fileName = "LazoEffect", menuName = "Trucardo/Powers/Effects/Lazo")]
    public class LazoEffect : PowerEffect
    {
        public override bool CanExecute(PowerContext context, out string reason)
        {
            reason = string.Empty;
            GameModel model = context?.GameModel;
            GameServer server = Object.FindAnyObjectByType<GameServer>();
            var session = server != null ? server.GetSessionByPlayer(context.PlayerId) : null;
            if (session != null && session.GameModel != null)
            {
                model = session.GameModel;
            }

            if (model == null)
            {
                reason = "No hay partida activa.";
                return false;
            }

            List<int> playedCards = (context.Team == 1) ? model.P1PlayedCards : model.P2PlayedCards;
            if (model.CurrentRoundIndex != 0 || (playedCards != null && playedCards.Count > 0))
            {
                reason = "El Lazo solo puede usarse en la primera mano antes de tirar cartas.";
                return false;
            }

            return true;
        }

        public override bool Execute(PowerContext context)
        {
            GameServer server = Object.FindAnyObjectByType<GameServer>();
            if (server == null || server.masterDeck == null)
            {
                Debug.LogWarning("[Poder Lazo] No se encontró GameServer o masterDeck.");
                return false;
            }

            var session = server.GetSessionByPlayer(context.PlayerId);
            if (session == null || session.GameModel == null)
            {
                Debug.LogWarning("[Poder Lazo] No se encontró una sesión activa de juego.");
                return false;
            }

            List<int> playedCards = (context.Team == 1) ? session.GameModel.P1PlayedCards : session.GameModel.P2PlayedCards;
            if (session.GameModel.CurrentRoundIndex != 0 || (playedCards != null && playedCards.Count > 0))
            {
                Debug.LogWarning("<color=#8D6E63><b>[Poder Lazo]</b></color> El Lazo solo puede usarse en la primera mano antes de jugar cartas.");
                return false;
            }

            List<int> playerHand = (context.Team == 1) ? session.GameModel.P1Hand : session.GameModel.P2Hand;
            playerHand.Clear();

            for (int i = 0; i < 3; i++)
            {
                int newCard = server.GetUnusedCard(session);
                if (newCard != -1)
                {
                    playerHand.Add(newCard);
                }
            }

            server.SyncPlayerHand(session, context.PlayerId);
            server.SendPowerToast(session, context.PlayerId, "LAZO", "¡Has arrojado el lazo! Descartaste tu mano y recibiste 3 cartas nuevas.");

            server.SyncClientState(session);
            Debug.Log("<color=#8D6E63><b>[Poder Lazo]</b></color> ¡Has arrojado el lazo! Descartaste tu mano y recibiste 3 cartas completamente nuevas.");
            return true;
        }
    }
}
