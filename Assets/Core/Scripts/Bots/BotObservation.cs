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
        public bool CanCallEnvido { get; }
        public bool CanCallTruco { get; }
        public int ScoreTeam1 { get; }
        public int ScoreTeam2 { get; }
        public int CurrentRoundIndex { get; }
        public IReadOnlyList<ManoGanadaPor> RoundWinners { get; }
        public bool EnvidoCantado { get; }
        public NivelTruco EstadoTruco { get; }
        public IReadOnlyList<BotCardObservation> OwnHand { get; }
        public IReadOnlyList<BotCardObservation> Player1PlayedCards { get; }
        public IReadOnlyList<BotCardObservation> Player2PlayedCards { get; }

        public BotObservation(
            ulong botId,
            GameState gameState,
            ulong currentTurnId,
            TipoCanto cantoPendiente,
            ulong lastCallerId,
            bool canCallEnvido,
            bool canCallTruco,
            int scoreTeam1,
            int scoreTeam2,
            int currentRoundIndex,
            IEnumerable<ManoGanadaPor> roundWinners,
            bool envidoCantado,
            NivelTruco estadoTruco,
            IEnumerable<BotCardObservation> ownHand,
            IEnumerable<BotCardObservation> player1PlayedCards,
            IEnumerable<BotCardObservation> player2PlayedCards)
        {
            BotId = botId;
            GameState = gameState;
            CurrentTurnId = currentTurnId;
            CantoPendiente = cantoPendiente;
            LastCallerId = lastCallerId;
            CanCallEnvido = canCallEnvido;
            CanCallTruco = canCallTruco;
            ScoreTeam1 = scoreTeam1;
            ScoreTeam2 = scoreTeam2;
            CurrentRoundIndex = currentRoundIndex;
            RoundWinners = new List<ManoGanadaPor>(roundWinners).AsReadOnly();
            EnvidoCantado = envidoCantado;
            EstadoTruco = estadoTruco;
            OwnHand = new List<BotCardObservation>(ownHand).AsReadOnly();
            Player1PlayedCards = new List<BotCardObservation>(player1PlayedCards).AsReadOnly();
            Player2PlayedCards = new List<BotCardObservation>(player2PlayedCards).AsReadOnly();
        }
    }
}
