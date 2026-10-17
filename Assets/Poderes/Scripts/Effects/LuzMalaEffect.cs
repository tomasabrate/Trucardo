using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Efecto del Poder "Luz Mala" (Legendario):
    /// Vuelve blancas y ciega las cartas del rival: no sabrá cuál es cuál al jugar y jugará a ciegas.
    /// </summary>
    [CreateAssetMenu(fileName = "LuzMalaEffect", menuName = "Trucardo/Powers/Effects/Luz Mala")]
    public class LuzMalaEffect : PowerEffect
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
                Debug.LogWarning("[Poder Luz Mala] No se encontró GameServer.");
                return false;
            }

            var session = server.GetSessionByPlayer(context.PlayerId);
            if (session == null || session.GameModel == null)
            {
                Debug.LogWarning("[Poder Luz Mala] No se encontró una sesión activa de juego.");
                return false;
            }

            ulong rivalId = (context.Team == 1) ? session.Player2Id : session.Player1Id;

            if (context.Team == 1)
            {
                session.P2LuzMalaActive = true;
            }
            else
            {
                session.P1LuzMalaActive = true;
            }

            server.SendPowerToast(session, context.PlayerId, "LUZ MALA", "¡Luz Mala invocada! Las cartas del rival quedaron sumidas en niebla y jugará a ciegas.");
            server.SendPowerToast(session, rivalId, "LUZ MALA", "¡LUZ MALA! Un resplandor espectral ciega tus cartas: quedaron en blanco y jugarás a ciegas.");

            server.TryScheduleBotAction(session);
            server.SyncClientState(session);

            Debug.Log("<color=#00E676><b>[Poder Legendario: Luz Mala]</b></color> ¡Se ha manifestado la Luz Mala en la mesa! Las cartas del rival quedaron sumidas en niebla espectral y jugará a ciegas.");
            return true;
        }
    }
}
