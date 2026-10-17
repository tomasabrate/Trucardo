using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Efecto del Poder "Mano de Dios":
    /// Cambia quién es la mano en la ronda actual.
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

            // Si se activa durante el turno del invocador antes de jugar carta en la baza, cede el tiro al rival
            if (session.GameModel.CurrentState == GameState.TurnoJugador)
            {
                if (session.GameModel.CurrentTurnId == context.PlayerId)
                {
                    session.GameModel.CurrentTurnId = rivalId;
                }
            }

            server.SendPowerToast(session, context.PlayerId, "MANO DE DIOS", 
                "¡Mano de Dios! Ahora sos la Mano de la ronda y le cediste el primer tiro al rival.");
            server.SendPowerToast(session, rivalId, "MANO DE DIOS", 
                "¡Mano de Dios! El rival usó la Mano de Dios: ¡Te toca tirar primero a vos!");

            server.SyncClientState(session);
            server.CheckStartPavaCalienteTimer(session);
            server.TryScheduleBotAction(session);

            Debug.Log($"<color=#FFD700><b>[Poder Mano de Dios]</b></color> ¡Cambió la mano! Invocador {context.PlayerId} es Mano. Turno actual de tirar: {session.GameModel.CurrentTurnId}.");
            return true;
        }
    }
}
