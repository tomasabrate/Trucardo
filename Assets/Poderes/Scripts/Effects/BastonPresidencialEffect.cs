using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Efecto del Poder "Bastón Presidencial" (Legendario):
    /// Obliga al rival a aceptar (decir '¡Quiero!') a tu próximo canto.
    /// </summary>
    [CreateAssetMenu(fileName = "BastonPresidencialEffect", menuName = "Trucardo/Powers/Effects/Bastón Presidencial")]
    public class BastonPresidencialEffect : PowerEffect
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
                Debug.LogWarning("[Poder Bastón Presidencial] No se encontró GameServer.");
                return false;
            }

            var session = server.GetSessionByPlayer(context.PlayerId);
            if (session == null || session.GameModel == null)
            {
                Debug.LogWarning("[Poder Bastón Presidencial] No se encontró una sesión activa de juego.");
                return false;
            }

            session.ForcedAcceptNextCanto = true;
            session.ForcedAcceptCallerId = context.PlayerId;
            session.ForcedAcceptCasterTeam = context.Team;

            ulong rivalId = (context.Team == 1) ? session.Player2Id : session.Player1Id;
            server.SendPowerToast(session, context.PlayerId, "BASTÓN PRESIDENCIAL", "¡Orden de Mando! Tu rival estará obligado a aceptar tu próximo canto.");
            server.SendPowerToast(session, rivalId, "BASTÓN PRESIDENCIAL", "¡ORDEN PRESIDENCIAL! Tu rival te obliga a responder ¡QUIERO! en su próximo canto.");

            server.SyncClientState(session);
            Debug.Log("<color=#FFD700><b>[Poder Legendario: Bastón Presidencial]</b></color> ¡Orden de Mando! El rival está legalmente obligado a responder '¡QUIERO!' en tu próximo canto.");
            return true;
        }
    }
}
