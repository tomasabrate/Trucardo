using System.Collections.Generic;
using Trucardo.Core;
using UnityEngine;
using Trucardo.Core.Rules;

namespace Trucardo.Core.Commands
{
    public class PlayCardCommand : BaseCommand
    {
        private ulong clientId;
        private int cardIndexInDeck;

        public PlayCardCommand(GameModel model, ulong clientId, int cardIndexInDeck) : base(model)
        {
            this.clientId = clientId;
            this.cardIndexInDeck = cardIndexInDeck;
        }

        public override void Execute()
        {
            if (model.CurrentState != GameState.TurnoJugador)
            {
                Debug.LogWarning("No se puede jugar carta ahora. Estado: " + model.CurrentState);
                return;
            }

            if (model.CurrentTurnId != clientId)
            {
                Debug.LogWarning("Turno incorrecto. Intentó jugar: " + clientId);
                return;
            }

            bool isP1 = IsPlayer1(clientId);
            List<int> hand = isP1 ? model.P1Hand : model.P2Hand;
            List<int> played = isP1 ? model.P1PlayedCards : model.P2PlayedCards;

            // Validar que tenga la carta en la mano
            if (!hand.Contains(cardIndexInDeck))
            {
                Debug.LogWarning("El jugador intentó tirar una carta que no tiene.");
                return;
            }

            // Mover carta de mano a mesa
            hand.Remove(cardIndexInDeck);
            played.Add(cardIndexInDeck);
            
            CardData playedCardData = model.DeckReference.AvailableCards[cardIndexInDeck];
            Debug.Log($"Jugador {(isP1 ? "1" : "2")} jugó el {playedCardData.Number} de {playedCardData.Suit}");

            // Chequear si el rival ya jugó en esta misma ronda
            List<int> enemyPlayed = isP1 ? model.P2PlayedCards : model.P1PlayedCards;
            
            if (enemyPlayed.Count == played.Count)
            {
                // Ambos jugaron, evaluar ganador de la mano actual
                EvaluateRound(isP1 ? cardIndexInDeck : enemyPlayed[played.Count - 1], 
                              !isP1 ? cardIndexInDeck : enemyPlayed[played.Count - 1]);
            }
            else
            {
                // Le toca al rival
                model.CurrentTurnId = isP1 ? model.Player2Id : model.Player1Id;
            }
        }

        private void EvaluateRound(int p1CardIdx, int p2CardIdx)
        {
            CardData c1 = model.DeckReference.AvailableCards[p1CardIdx];
            CardData c2 = model.DeckReference.AvailableCards[p2CardIdx];

            int idx = model.CurrentRoundIndex;

            if (c1.TrucoRank > c2.TrucoRank)
            {
                model.RoundWinners[idx] = ManoGanadaPor.Equipo1;
                model.CurrentTurnId = model.Player1Id; // Gana y es mano
                Debug.Log($"Mano {idx + 1} ganada por Equipo 1");
            }
            else if (c2.TrucoRank > c1.TrucoRank)
            {
                model.RoundWinners[idx] = ManoGanadaPor.Equipo2;
                model.CurrentTurnId = model.Player2Id;
                Debug.Log($"Mano {idx + 1} ganada por Equipo 2");
            }
            else
            {
                model.RoundWinners[idx] = ManoGanadaPor.Parda;
                // The first tied trick is led again by mano. Later ties end the hand.
                model.CurrentTurnId = model.ManoPlayerId;
                Debug.Log($"Mano {idx + 1} fue Parda");
            }

            model.CurrentRoundIndex++;
            CheckManoGanada();
        }

        private void CheckManoGanada()
        {
            var mano = model.ManoPlayerId == model.Player1Id ? ManoGanadaPor.Equipo1 : ManoGanadaPor.Equipo2;
            var winner = TrucoRules.ResolveHand(model.RoundWinners, model.CurrentRoundIndex, mano);
            if (winner != ManoGanadaPor.Nadie)
            {
                model.CurrentState = GameState.RondaTerminada;
                int puntos = TrucoRules.AcceptedTrucoPoints(model.EstadoTruco);
                model.AddPoints(winner == ManoGanadaPor.Equipo1 ? 1 : 2, puntos);

                Debug.Log($"¡Ronda Terminada! Puntos: P1={model.ScoreTeam1} | P2={model.ScoreTeam2}");
            }
        }
    }
}
