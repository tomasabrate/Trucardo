using System.Collections.Generic;
using Trucardo.Core;
using UnityEngine;
using System.Linq;

namespace Trucardo.Core.Commands
{
    public class RespondCommand : BaseCommand
    {
        private ulong clientId;
        private bool accepts;

        public RespondCommand(GameModel model, ulong clientId, bool accepts) : base(model)
        {
            this.clientId = clientId;
            this.accepts = accepts;
        }

        public override void Execute()
        {
            if (model.CurrentState != GameState.EsperandoRespuestaCanto) return;
            
            // Quien responde tiene que ser el jugador contrario al que cantó
            if (model.LastCallerId == clientId)
            {
                Debug.LogWarning("No puedes responder a tu propio canto.");
                return;
            }

            string responderName = IsPlayer1(clientId) ? "Jugador 1" : "Jugador 2";
            Debug.Log($"{responderName} dice: {(accepts ? "¡QUIERO!" : "¡NO QUIERO!")}");

            if (model.CantoPendiente == TipoCanto.Envido)
            {
                ResolveEnvido();
            }
            else if (model.CantoPendiente == TipoCanto.Truco)
            {
                ResolveTruco();
            }

            // Volvemos al juego normal si no terminó la ronda
            if (model.CurrentState == GameState.EsperandoRespuestaCanto)
            {
                if (model.CantoPendiente == TipoCanto.Envido && model.CantoAnterior == TipoCanto.Truco)
                {
                    // Restaurar el canto de Truco que quedó pausado
                    model.CantoPendiente = TipoCanto.Truco;
                    model.CantoAnterior = TipoCanto.Ninguno;
                    model.LastCallerId = model.LastTrucoCallerId;
                    Debug.Log("Volviendo al canto de Truco pendiente...");
                }
                else
                {
                    model.CurrentState = GameState.TurnoJugador;
                    model.CantoPendiente = TipoCanto.Ninguno;
                }
            }
        }

        private void ResolveEnvido()
        {
            if (accepts)
            {
                int p1Points = CalculateEnvido(model.P1Hand, model.P1PlayedCards);
                int p2Points = CalculateEnvido(model.P2Hand, model.P2PlayedCards);

                Debug.Log($"Puntos de Envido: P1 ({p1Points}) vs P2 ({p2Points})");

                if (p1Points >= p2Points) // En caso de empate gana el mano (por ahora P1 en este MVP)
                {
                    Debug.Log("Jugador 1 gana el Envido (+2 pts)");
                    model.AddPoints(1, 2);
                }
                else
                {
                    Debug.Log("Jugador 2 gana el Envido (+2 pts)");
                    model.AddPoints(2, 2);
                }
            }
            else
            {
                // No quiso el envido, 1 punto para el que cantó
                int winnerTeam = IsPlayer1(model.LastCallerId) ? 1 : 2;
                Debug.Log($"Jugador {(IsPlayer1(clientId) ? "2" : "1")} gana 1 punto por rechazo de Envido.");
                model.AddPoints(winnerTeam, 1);
            }
        }

        private void ResolveTruco()
        {
            if (accepts)
            {
                if (model.EstadoTruco == NivelTruco.Ninguno) model.EstadoTruco = NivelTruco.Truco;
                else if (model.EstadoTruco == NivelTruco.Truco) model.EstadoTruco = NivelTruco.Retruco;
                else if (model.EstadoTruco == NivelTruco.Retruco) model.EstadoTruco = NivelTruco.Vale4;
                
                Debug.Log($"El nivel del Truco subió a: {model.EstadoTruco}");
            }
            else
            {
                // No quiso el Truco, la ronda termina inmediatamente
                int winnerTeam = IsPlayer1(model.LastCallerId) ? 1 : 2;
                int puntosGanados = (model.EstadoTruco == NivelTruco.Ninguno) ? 1 : (int)model.EstadoTruco;
                
                Debug.Log($"No quiso el truco. Equipo {winnerTeam} gana {puntosGanados} puntos. Fin de ronda.");
                model.AddPoints(winnerTeam, puntosGanados);
                model.CurrentState = GameState.RondaTerminada;
            }
        }

        private int CalculateEnvido(List<int> handCards, List<int> playedCards)
        {
            List<CardData> allCards = new List<CardData>();
            foreach (int i in handCards) allCards.Add(model.DeckReference.AvailableCards[i]);
            foreach (int i in playedCards) allCards.Add(model.DeckReference.AvailableCards[i]);

            if (allCards.Count == 0) return 0;

            int maxEnvido = 0;
            // Revisar cada par de cartas del mismo palo
            for (int i = 0; i < allCards.Count; i++)
            {
                for (int j = i + 1; j < allCards.Count; j++)
                {
                    if (allCards[i].Suit == allCards[j].Suit)
                    {
                        int pts = 20 + allCards[i].EnvidoValue + allCards[j].EnvidoValue;
                        if (pts > maxEnvido) maxEnvido = pts;
                    }
                }
                // Si no combinan, su mayor carta suelta
                if (allCards[i].EnvidoValue > maxEnvido) maxEnvido = allCards[i].EnvidoValue;
            }

            return maxEnvido;
        }
    }
}
