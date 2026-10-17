using System.Collections.Generic;
using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Efecto del Poder "Guampas":
    /// Roba una carta aleatoria del mazo; el jugador puede elegir cambiarla por una carta de su mano, o conservarla si no la quiere.
    /// </summary>
    [CreateAssetMenu(fileName = "GuampasEffect", menuName = "Trucardo/Powers/Effects/Guampas")]
    public class GuampasEffect : PowerEffect
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
                // Si había quedado un estado pendiente previo, se limpia para no trabar el poder
                if (session.GuampasPendingPlayerId == context.PlayerId)
                {
                    session.GuampasPendingPlayerId = 0;
                    session.GuampasDrawnCardIndex = -1;
                }
            }

            if (model == null)
            {
                reason = "No hay partida activa.";
                return false;
            }

            List<int> playerHand = (context.Team == 1) ? model.P1Hand : model.P2Hand;
            if (playerHand == null || playerHand.Count == 0)
            {
                reason = "No tienes cartas en mano para cambiar.";
                return false;
            }

            return true;
        }

        public override bool Execute(PowerContext context)
        {
            GameServer server = Object.FindAnyObjectByType<GameServer>();
            if (server == null || server.masterDeck == null)
            {
                Debug.LogWarning("[Poder Guampas] No se encontró GameServer o masterDeck.");
                return false;
            }

            var session = server.GetSessionByPlayer(context.PlayerId);
            if (session == null || session.GameModel == null)
            {
                Debug.LogWarning("[Poder Guampas] No se encontró una sesión activa de juego.");
                return false;
            }

            List<int> playerHand = (context.Team == 1) ? session.GameModel.P1Hand : session.GameModel.P2Hand;
            if (playerHand == null || playerHand.Count == 0)
            {
                Debug.Log("[Poder Guampas] No tienes cartas en mano para evaluar.");
                return false;
            }

            // Robar una carta aleatoria del mazo restante
            int drawnCardIndex = server.GetUnusedCard(session);
            if (drawnCardIndex == -1 || drawnCardIndex >= server.masterDeck.AvailableCards.Count)
            {
                server.SendPowerToast(session, context.PlayerId, "GUAMPAS", "No quedan cartas disponibles en el mazo.");
                return false;
            }

            bool isBot = (context.PlayerId == session.Player1Id && session.Player1Type == ParticipantType.Bot) ||
                         (context.PlayerId == session.Player2Id && session.Player2Type == ParticipantType.Bot) ||
                         (context.PlayerId >= ulong.MaxValue - 100);

            if (isBot)
            {
                CardData drawnCard = server.masterDeck.AvailableCards[drawnCardIndex];

                // El bot busca su carta más débil
                int worstCardIndex = playerHand[0];
                int worstCardRank = server.masterDeck.AvailableCards[worstCardIndex].TrucoRank;
                for (int i = 1; i < playerHand.Count; i++)
                {
                    CardData c = server.masterDeck.AvailableCards[playerHand[i]];
                    if (c.TrucoRank < worstCardRank)
                    {
                        worstCardIndex = playerHand[i];
                        worstCardRank = c.TrucoRank;
                    }
                }

                CardData worstCard = server.masterDeck.AvailableCards[worstCardIndex];

                // Si la robada supera a la peor de su mano, la reemplaza; si no, la descarta
                if (drawnCard.TrucoRank > worstCardRank)
                {
                    int slotIndex = playerHand.IndexOf(worstCardIndex);
                    if (slotIndex != -1)
                    {
                        playerHand[slotIndex] = drawnCardIndex;
                    }
                    server.SyncPlayerHand(session, context.PlayerId);
                    server.BroadcastPowerToast(session, "GUAMPAS", "El bot usó Guampas y cambió una carta de su mano.");
                    Debug.Log($"[Guampas Bot] Cambió su carta {worstCard.Number} de {worstCard.Suit} por el {drawnCard.Number} de {drawnCard.Suit}.");
                }
                else
                {
                    server.BroadcastPowerToast(session, "GUAMPAS", "El bot usó Guampas pero decidió conservar su mano.");
                    Debug.Log($"[Guampas Bot] Descartó el {drawnCard.Number} de {drawnCard.Suit} y conservó su mano.");
                }

                server.SyncClientState(session);
                return true;
            }
            else
            {
                // Jugador humano: registrar estado pendiente en sesión y abrir la selección en la UI
                session.GuampasPendingPlayerId = context.PlayerId;
                session.GuampasDrawnCardIndex = drawnCardIndex;
                server.PromptGuampasChoice(session, context.PlayerId, drawnCardIndex);
                return true;
            }
        }
    }
}
