using Trucardo.Core;
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
            if (model.CurrentState != GameState.TurnoJugador) return;
            
            if (model.EstadoTruco == NivelTruco.Vale4)
            {
                Debug.LogWarning("Ya no se puede subir más el Truco (Vale 4 máximo).");
                return;
            }

            if (model.LastTrucoCallerId == clientId && model.EstadoTruco != NivelTruco.Ninguno)
            {
                Debug.LogWarning("El que cantó previamente no puede volver a subir el Truco.");
                return;
            }

            model.CurrentState = GameState.EsperandoRespuestaCanto;
            model.CantoPendiente = TipoCanto.Truco;
            model.LastCallerId = clientId;
            model.LastTrucoCallerId = clientId;

            string callerName = IsPlayer1(clientId) ? "Jugador 1" : "Jugador 2";
            string cantoString = model.EstadoTruco == NivelTruco.Ninguno ? "TRUCO" : 
                                 (model.EstadoTruco == NivelTruco.Truco ? "RETRUCO" : "VALE 4");
                                 
            Debug.Log($"¡{callerName} cantó {cantoString}! Esperando respuesta...");
        }
    }
}
