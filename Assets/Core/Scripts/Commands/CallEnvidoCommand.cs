using Trucardo.Core;
using UnityEngine;

namespace Trucardo.Core.Commands
{
    public class CallEnvidoCommand : BaseCommand
    {
        private ulong clientId;

        public CallEnvidoCommand(GameModel model, ulong clientId) : base(model)
        {
            this.clientId = clientId;
        }

        public override void Execute()
        {
            if (model.CurrentState != GameState.TurnoJugador && 
                !(model.CurrentState == GameState.EsperandoRespuestaCanto && model.CantoPendiente == TipoCanto.Truco && model.LastCallerId != clientId)) 
            {
                return;
            }
            if (model.CurrentRoundIndex != 0)
            {
                Debug.LogWarning("El Envido solo se puede cantar en la primera mano.");
                return;
            }
            if (model.EnvidoCantado)
            {
                Debug.LogWarning("Ya se cantó el Envido en esta ronda.");
                return;
            }
            
            // Permitimos el canto
            model.EnvidoCantado = true;
            
            if (model.CantoPendiente == TipoCanto.Truco) 
            {
                model.CantoAnterior = TipoCanto.Truco;
            }
            
            model.CurrentState = GameState.EsperandoRespuestaCanto;
            model.CantoPendiente = TipoCanto.Envido;
            model.LastCallerId = clientId;

            string callerName = IsPlayer1(clientId) ? "Jugador 1" : "Jugador 2";
            Debug.Log($"¡{callerName} cantó ENVIDO! Esperando respuesta...");
        }
    }
}
