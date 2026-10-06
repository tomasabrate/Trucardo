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
        public IReadOnlyList<int> OwnHand { get; }

        public BotObservation(
            ulong botId,
            GameState gameState,
            ulong currentTurnId,
            TipoCanto cantoPendiente,
            ulong lastCallerId,
            bool canCallEnvido,
            bool canCallTruco,
            IEnumerable<int> ownHand)
        {
            BotId = botId;
            GameState = gameState;
            CurrentTurnId = currentTurnId;
            CantoPendiente = cantoPendiente;
            LastCallerId = lastCallerId;
            CanCallEnvido = canCallEnvido;
            CanCallTruco = canCallTruco;
            OwnHand = new List<int>(ownHand).AsReadOnly();
        }
    }
}
