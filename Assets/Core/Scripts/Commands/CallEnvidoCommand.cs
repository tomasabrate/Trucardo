using Trucardo.Core;
using Trucardo.Core.Rules;
using UnityEngine;

namespace Trucardo.Core.Commands
{
    public class CallEnvidoCommand : BaseCommand
    {
        private ulong clientId;
        private EnvidoCall call;

        public CallEnvidoCommand(GameModel model, ulong clientId, EnvidoCall call) : base(model)
        {
            this.clientId = clientId;
            this.call = call;
        }

        public override void Execute()
        {
            int playedCardCount = IsPlayer1(clientId)
                ? model.P1PlayedCards.Count
                : model.P2PlayedCards.Count;
            if (!EnvidoRules.CanCallFromContext(
                    model.CurrentState,
                    model.CurrentTurnId,
                    model.CantoPendiente,
                    model.LastCallerId,
                    clientId,
                    model.CurrentRoundIndex,
                    playedCardCount,
                    model.EstadoTruco,
                    model.EstadoEnvido,
                    call))
                return;

            bool isRaise = model.CantoPendiente == TipoCanto.Envido;
            if (isRaise)
            {
                model.EnvidoAcceptedPoints = EnvidoRules.GetAcceptedPoints(
                    model.EstadoEnvido, model.ScoreTeam1, model.ScoreTeam2);
            }
            else
            {
                model.EnvidoCantado = true;
                model.EnvidoAcceptedPoints = 0;
                if (model.CantoPendiente == TipoCanto.Truco)
                    model.CantoAnterior = TipoCanto.Truco;
            }

            model.EstadoEnvido = EnvidoRules.Raise(model.EstadoEnvido, call);
            model.CurrentState = GameState.EsperandoRespuestaCanto;
            model.CantoPendiente = TipoCanto.Envido;
            model.LastCallerId = clientId;

            string callerName = IsPlayer1(clientId) ? "Jugador 1" : "Jugador 2";
            Debug.Log($"¡{callerName} cantó {EnvidoRules.GetDisplayName(model.EstadoEnvido)}! Esperando respuesta...");
        }
    }
}
