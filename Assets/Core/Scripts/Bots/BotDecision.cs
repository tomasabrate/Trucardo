namespace Trucardo.Core.Bots
{
    public enum BotDecisionType
    {
        None,
        PlayCard,
        Respond
    }

    public sealed class BotDecision
    {
        public static BotDecision NoAction { get; } = new BotDecision(BotDecisionType.None, -1, false);

        public BotDecisionType Type { get; }
        public int CardIndex { get; }
        public bool Accepts { get; }

        private BotDecision(BotDecisionType type, int cardIndex, bool accepts)
        {
            Type = type;
            CardIndex = cardIndex;
            Accepts = accepts;
        }

        public static BotDecision PlayCard(int cardIndex)
        {
            return new BotDecision(BotDecisionType.PlayCard, cardIndex, false);
        }

        public static BotDecision Respond(bool accepts)
        {
            return new BotDecision(BotDecisionType.Respond, -1, accepts);
        }
    }
}
