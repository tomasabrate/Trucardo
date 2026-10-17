using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Efecto del Poder "Boleadoras":
    /// Traba al rival: le impide recibir su próximo poder en la siguiente ronda.
    /// </summary>
    [CreateAssetMenu(fileName = "BoleadorasEffect", menuName = "Trucardo/Powers/Effects/Boleadoras")]
    public class BoleadorasEffect : PowerEffect
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
                Debug.LogWarning("[Poder Boleadoras] No se encontró GameServer.");
                return false;
            }

            var session = server.GetSessionByPlayer(context.PlayerId);
            if (session == null || session.GameModel == null)
            {
                Debug.LogWarning("[Poder Boleadoras] No se encontró una sesión activa de juego.");
                return false;
            }

            ulong rivalId = (context.Team == 1) ? session.Player2Id : session.Player1Id;

            if (context.Team == 1)
            {
                session.P2Boleado = true;
            }
            else
            {
                session.P1Boleado = true;
            }

            server.SendPowerToast(session, context.PlayerId, "BOLEADORAS", "¡Boleaste al rival! Quedó trabado y no recibirá poder en la próxima ronda.");
            server.SendPowerToast(session, rivalId, "BOLEADORAS", "¡BOLEADO! Quedaste trabado y no recibirás poder en la próxima ronda.");

            Debug.Log("<color=#795548><b>[Poder Boleadoras]</b></color> ¡Boleaste al rival! Se encuentra trabado y no recibirá ningún poder en la próxima ronda.");
            return true;
        }
    }
}
