using System.Collections.Generic;

namespace Trucardo.Core.Bots
{
    public sealed class BotObservation
    {
        public ulong BotId { get; }
        public GameState GameState { get; }
        public ulong CurrentTurnId { get; }
        public IReadOnlyList<int> OwnHand { get; }

        public BotObservation(
            ulong botId,
            GameState gameState,
            ulong currentTurnId,
            IEnumerable<int> ownHand)
        {
            BotId = botId;
            GameState = gameState;
            CurrentTurnId = currentTurnId;
            OwnHand = new List<int>(ownHand).AsReadOnly();
        }
    }
}
