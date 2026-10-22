using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Efecto del Poder "Mano de Dios":
    /// Cambia quién es la mano en la ronda actual y desempata a su favor.
    /// </summary>
    [CreateAssetMenu(fileName = "ManoDeDiosEffect", menuName = "Trucardo/Powers/Effects/Mano de Dios")]
    public class ManoDeDiosEffect : PowerEffect
    {
        public override bool CanExecute(PowerContext context, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public override bool Execute(PowerContext context)
        {
            GameServer server = Object.FindAnyObjectByType<GameServer>();
            if (server == null)
            {
                Debug.LogWarning("[Poder Mano de Dios] No se encontró GameServer.");
                return false;
            }

            var session = server.GetSessionByPlayer(context.PlayerId);
            if (session == null || session.GameModel == null)
            {
                Debug.LogWarning("[Poder Mano de Dios] No se encontró una sesión activa de juego.");
                return false;
            }

            // El invocador pasa a ser la Mano oficial de la ronda (desempata a su favor en Envido y Truco)
            session.GameModel.ManoPlayerId = context.PlayerId;

            ulong rivalId = (context.Team == 1) ? session.Player2Id : session.Player1Id;

            // Verificamos si la baza actual recién empieza (ambos tienen la misma cantidad de cartas jugadas en mesa)
            bool isBazaStart = session.GameModel.P1PlayedCards.Count == session.GameModel.P2PlayedCards.Count;
            bool cededTurn = false;

            // Solo se cede el tiro al rival si la baza está empezando (nadie jugó aún en esta baza)
            // y le tocaba al invocador abrir la baza.
            // Si la baza ya comenzó y el rival ya tiró su carta, el invocador DEBE responder con su carta para completarla.
            if (session.GameModel.CurrentState == GameState.TurnoJugador && isBazaStart)
            {
                if (session.GameModel.CurrentTurnId == context.PlayerId)
                {
                    session.GameModel.CurrentTurnId = rivalId;
                    cededTurn = true;
                }
            }

            if (cededTurn)
            {
                server.SendPowerToast(session, context.PlayerId, "MANO DE DIOS", 
                    "¡Mano de Dios! Ahora sos la Mano de la ronda y le cediste el primer tiro al rival.");
                server.SendPowerToast(session, rivalId, "MANO DE DIOS", 
                    "¡Mano de Dios! El rival usó la Mano de Dios: ¡Te toca tirar primero a vos!");
            }
            else
            {
                server.SendPowerToast(session, context.PlayerId, "MANO DE DIOS", 
                    "¡Mano de Dios! Ahora sos la Mano de la ronda (ganás empates). ¡Te toca responder con tu carta!");
                server.SendPowerToast(session, rivalId, "MANO DE DIOS", 
                    "¡Mano de Dios! El rival usó la Mano de Dios: ahora es la Mano de la ronda.");
            }

            server.SyncClientState(session);
            server.CheckStartPavaCalienteTimer(session);

            // Solo programar acción del bot si el turno actual le pertenece al rival/bot
            if (session.GameModel.CurrentTurnId == rivalId)
            {
                server.TryScheduleBotAction(session);
            }

            Debug.Log($"<color=#FFD700><b>[Poder Mano de Dios]</b></color> ¡Cambió la mano! Invocador {context.PlayerId} es Mano. Turno actual de tirar: {session.GameModel.CurrentTurnId} (CedeTurno: {cededTurn}, BazaStart: {isBazaStart}).");
            return true;
        }
    }
}
