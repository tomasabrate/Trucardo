using System;
using System.Collections.Generic;
using UnityEngine;

namespace Trucardo.Core
{
    public enum GameState
    {
        EsperandoJugadores,
        Repartiendo,
        TurnoJugador,
        EsperandoRespuestaCanto,
        RondaTerminada,
        PartidaTerminada
    }

    public enum ManoGanadaPor
    {
        Nadie,
        Equipo1,
        Equipo2,
        Parda
    }

    public enum NivelTruco
    {
        Ninguno = 0,
        Truco = 2,
        Retruco = 3,
        Vale4 = 4
    }

    public enum TipoCanto
    {
        Ninguno,
        Envido,
        Truco
    }

    public enum EnvidoCall
    {
        Ninguno,
        Envido,
        RealEnvido,
        FaltaEnvido
    }

    public enum EstadoEnvido
    {
        Ninguno,
        Envido,
        EnvidoEnvido,
        RealEnvido,
        EnvidoRealEnvido,
        EnvidoEnvidoRealEnvido,
        FaltaEnvido
    }

    /// <summary>
    /// El Modelo del Servidor.
    /// Calcula puntos y turnos puramente en memoria, sin depender de GameObjects.
    /// </summary>
    public class GameModel
    {
        public int ScoreTeam1 { get; private set; }
        public int ScoreTeam2 { get; private set; }

        public ActionQueue actionQueue;
        public DeckData DeckReference { get; private set; }

        // Estados
        public GameState CurrentState { get; set; } = GameState.EsperandoJugadores;
        
        // Turnos (IDs de red)
        public ulong CurrentTurnId { get; set; }
        public ulong Player1Id { get; set; }
        public ulong Player2Id { get; set; }
        public ulong ManoPlayerId { get; private set; }
        private bool hasStartedHand;

        // Cartas en Mano de cada jugador (índices en el mazo maestro)
        public List<int> P1Hand { get; private set; } = new List<int>();
        public List<int> P2Hand { get; private set; } = new List<int>();

        // Cartas jugadas en la mesa (Listas donde la pos 0 es la 1ra mano, pos 1 es la 2da, etc)
        public List<int> P1PlayedCards { get; private set; } = new List<int>();
        public List<int> P2PlayedCards { get; private set; } = new List<int>();

        public ManoGanadaPor[] RoundWinners { get; private set; } = new ManoGanadaPor[3];
        public int CurrentRoundIndex { get; set; } = 0; // 0, 1 o 2

        // Estado de Cantos
        public bool EnvidoCantado { get; set; } = false;
        public EstadoEnvido EstadoEnvido { get; set; } = EstadoEnvido.Ninguno;
        public int EnvidoAcceptedPoints { get; set; }
        public NivelTruco EstadoTruco { get; set; } = NivelTruco.Ninguno;
        public TipoCanto CantoPendiente { get; set; } = TipoCanto.Ninguno;
        public TipoCanto CantoAnterior { get; set; } = TipoCanto.Ninguno;
        
        // Quién cantó para saber quién debe responder
        public ulong LastCallerId { get; set; } 
        public ulong LastTrucoCallerId { get; set; }

        public GameModel(DeckData deckRef)
        {
            actionQueue = new ActionQueue();
            DeckReference = deckRef;
        }

        public void ResetGame()
        {
            ScoreTeam1 = 0;
            ScoreTeam2 = 0;
            hasStartedHand = false;
            ResetHand();
        }

        public void ResetHand()
        {
            ManoPlayerId = !hasStartedHand || ManoPlayerId == Player2Id ? Player1Id : Player2Id;
            hasStartedHand = true;
            CurrentTurnId = ManoPlayerId;
            CurrentState = GameState.Repartiendo;
            P1Hand.Clear();
            P2Hand.Clear();
            P1PlayedCards.Clear();
            P2PlayedCards.Clear();
            CurrentRoundIndex = 0;
            EnvidoCantado = false;
            EstadoEnvido = EstadoEnvido.Ninguno;
            EnvidoAcceptedPoints = 0;
            EstadoTruco = NivelTruco.Ninguno;
            CantoPendiente = TipoCanto.Ninguno;
            CantoAnterior = TipoCanto.Ninguno;
            LastCallerId = 0;
            LastTrucoCallerId = 0;
            
            for (int i = 0; i < RoundWinners.Length; i++)
            {
                RoundWinners[i] = ManoGanadaPor.Nadie;
            }
        }

        public void DealCardsToPlayer(int team, List<int> cards)
        {
            if (team == 1) P1Hand.AddRange(cards);
            else if (team == 2) P2Hand.AddRange(cards);
        }

        public void AddPoints(int team, int points)
        {
            if (CurrentState == GameState.PartidaTerminada) return;
            if (team == 1) ScoreTeam1 += points;
            else if (team == 2) ScoreTeam2 += points;

            CheckWinCondition();
        }

        private void CheckWinCondition()
        {
            if (ScoreTeam1 < 30 && ScoreTeam2 < 30) return;
            CurrentState = GameState.PartidaTerminada;
            CurrentTurnId = ulong.MaxValue;
            CantoPendiente = TipoCanto.Ninguno;
            CantoAnterior = TipoCanto.Ninguno;
            Debug.Log(ScoreTeam1 >= 30 ? "Ganó el Equipo 1!" : "Ganó el Equipo 2!");
        }
    }
}
