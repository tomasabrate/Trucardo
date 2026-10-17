using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Efecto del Poder "Pava Caliente":
    /// El rival tiene solo 5 segundos por turno durante toda la mano, o jugará al azar/apurado.
    /// </summary>
    [CreateAssetMenu(fileName = "PavaCalienteEffect", menuName = "Trucardo/Powers/Effects/Pava Caliente")]
    public class PavaCalienteEffect : PowerEffect
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
                Debug.LogWarning("[Poder Pava Caliente] No se encontró GameServer.");
                return false;
            }

            var session = server.GetSessionByPlayer(context.PlayerId);
            if (session == null || session.GameModel == null)
            {
                Debug.LogWarning("[Poder Pava Caliente] No se encontró una sesión activa de juego.");
                return false;
            }

            session.PavaCalienteActive = true;
            session.PavaCalienteCasterId = context.PlayerId;

            server.BroadcastPowerToast(session, "PAVA CALIENTE", "¡La pava está hirviendo! Tienen un límite estricto de 5 segundos por turno.");
            server.CheckStartPavaCalienteTimer(session);
            server.TryScheduleBotAction(session);
            server.SyncClientState(session);

            Debug.Log("<color=#FF5722><b>[Poder Pava Caliente]</b></color> ¡La pava está hirviendo! El rival tiene un tiempo límite estricto de 5 segundos por turno.");
            return true;
        }
    }
}
