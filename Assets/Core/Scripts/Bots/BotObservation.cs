using System.Collections.Generic;

namespace Trucardo.Core.Bots
{
    public sealed class BotObservation
    {
        public ulong BotId { get; }
        public GameState GameState { get; }
        public ulong CurrentTurnId { get; }
        public TipoCanto CantoPendiente { get; }
        public ulong LastCallerId { get; }
        public ulong LastTrucoCallerId { get; }
        public bool BotIsMano { get; }
        public int OpponentHiddenCardCount { get; }
        public int BotScore => ScoreTeam2;
        public int OpponentScore => ScoreTeam1;
        public bool CanCallEnvido { get; }
        public bool CanCallRealEnvido { get; }
        public bool CanCallFaltaEnvido { get; }
        public bool CanCallTruco { get; }
        public int ScoreTeam1 { get; }
        public int ScoreTeam2 { get; }
        public int CurrentRoundIndex { get; }
        public IReadOnlyList<ManoGanadaPor> RoundWinners { get; }
        public bool EnvidoCantado { get; }
        public NivelTruco EstadoTruco { get; }
        public EstadoEnvido EstadoEnvido { get; }
        public int EnvidoAcceptedPoints { get; }
        public int PendingEnvidoPoints { get; }
        public IReadOnlyList<BotCardObservation> OwnHand { get; }
        public IReadOnlyList<BotCardObservation> Player1PlayedCards { get; }
        public IReadOnlyList<BotCardObservation> Player2PlayedCards { get; }
        public IReadOnlyList<BotCardObservation> UnseenCards { get; }

        public BotObservation(
            ulong botId,
            GameState gameState,
            ulong currentTurnId,
            TipoCanto cantoPendiente,
            ulong lastCallerId,
            bool canCallEnvido,
            bool canCallRealEnvido,
            bool canCallFaltaEnvido,
            bool canCallTruco,
            int scoreTeam1,
            int scoreTeam2,
            int currentRoundIndex,
            IEnumerable<ManoGanadaPor> roundWinners,
            bool envidoCantado,
            NivelTruco estadoTruco,
            IEnumerable<BotCardObservation> ownHand,
            IEnumerable<BotCardObservation> player1PlayedCards,
            IEnumerable<BotCardObservation> player2PlayedCards,
            bool botIsMano,
            ulong lastTrucoCallerId,
            int opponentHiddenCardCount,
            IEnumerable<BotCardObservation> unseenCards,
            EstadoEnvido estadoEnvido,
            int envidoAcceptedPoints,
            int pendingEnvidoPoints)
        {
            BotId = botId;
            GameState = gameState;
            CurrentTurnId = currentTurnId;
            CantoPendiente = cantoPendiente;
            LastCallerId = lastCallerId;
            LastTrucoCallerId = lastTrucoCallerId;
            BotIsMano = botIsMano;
            OpponentHiddenCardCount = opponentHiddenCardCount;
            CanCallEnvido = canCallEnvido;
            CanCallRealEnvido = canCallRealEnvido;
            CanCallFaltaEnvido = canCallFaltaEnvido;
            CanCallTruco = canCallTruco;
            ScoreTeam1 = scoreTeam1;
            ScoreTeam2 = scoreTeam2;
            CurrentRoundIndex = currentRoundIndex;
            RoundWinners = new List<ManoGanadaPor>(roundWinners).AsReadOnly();
            EnvidoCantado = envidoCantado;
            EstadoTruco = estadoTruco;
            EstadoEnvido = estadoEnvido;
            EnvidoAcceptedPoints = envidoAcceptedPoints;
            PendingEnvidoPoints = pendingEnvidoPoints;
            OwnHand = new List<BotCardObservation>(ownHand).AsReadOnly();
            Player1PlayedCards = new List<BotCardObservation>(player1PlayedCards).AsReadOnly();
            Player2PlayedCards = new List<BotCardObservation>(player2PlayedCards).AsReadOnly();
            UnseenCards = new List<BotCardObservation>(unseenCards).AsReadOnly();
        }
    }
}
