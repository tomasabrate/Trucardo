namespace Trucardo.Core.Bots
{
    public enum BotDecisionType
    {
        None,
        PlayCard
    }

    public sealed class BotDecision
    {
        public static BotDecision NoAction { get; } = new BotDecision(BotDecisionType.None, -1);

        public BotDecisionType Type { get; }
        public int CardIndex { get; }

        private BotDecision(BotDecisionType type, int cardIndex)
        {
            Type = type;
            CardIndex = cardIndex;
        }

        public static BotDecision PlayCard(int cardIndex)
        {
            return new BotDecision(BotDecisionType.PlayCard, cardIndex);
        }
    }
}
