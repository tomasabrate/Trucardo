using Trucardo.Core;
using Trucardo.Core.Rules;
using UnityEngine;

namespace Trucardo.Core.Commands
{
    public class CallTrucoCommand : BaseCommand
    {
        private ulong clientId;

        public CallTrucoCommand(GameModel model, ulong clientId) : base(model)
        {
            this.clientId = clientId;
        }

        public override void Execute()
        {
            if (!TrucoRules.CanCallFromContext(
                    model.CurrentState,
                    model.CurrentTurnId,
                    model.CantoPendiente,
                    model.LastCallerId,
                    model.LastTrucoCallerId,
                    clientId,
                    model.EstadoTruco))
            {
                Debug.LogWarning("No se puede cantar o subir el Truco en el contexto actual.");
                return;
            }

            bool isRaise = model.CurrentState == GameState.EsperandoRespuestaCanto
                && model.CantoPendiente == TipoCanto.Truco;
            if (isRaise)
            {
                model.EstadoTruco = TrucoRules.GetNextLevel(model.EstadoTruco);
            }

            model.CurrentState = GameState.EsperandoRespuestaCanto;
            model.CantoPendiente = TipoCanto.Truco;
            model.LastCallerId = clientId;
            model.LastTrucoCallerId = clientId;

            string callerName = IsPlayer1(clientId) ? "Jugador 1" : "Jugador 2";
            string cantoString = TrucoRules.GetDisplayName(
                TrucoRules.GetPendingTrucoLevel(model.EstadoTruco)).ToUpperInvariant();

            Debug.Log($"¡{callerName} cantó {cantoString}! Esperando respuesta...");
        }
    }
}
