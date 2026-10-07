namespace Trucardo.Core.Bots
{
    public enum BotDecisionType
    {
        None,
        PlayCard,
        Respond,
        CallEnvido,
        CallTruco
    }

    public sealed class BotDecision
    {
        public static BotDecision NoAction { get; }
            = new BotDecision(BotDecisionType.None, -1, false, EnvidoCall.Ninguno);

        public BotDecisionType Type { get; }
        public int CardIndex { get; }
        public bool Accepts { get; }
        public EnvidoCall EnvidoCall { get; }

        private BotDecision(
            BotDecisionType type, int cardIndex, bool accepts, EnvidoCall envidoCall)
        {
            Type = type;
            CardIndex = cardIndex;
            Accepts = accepts;
            EnvidoCall = envidoCall;
        }

        public static BotDecision PlayCard(int cardIndex)
        {
            return new BotDecision(BotDecisionType.PlayCard, cardIndex, false, EnvidoCall.Ninguno);
        }

        public static BotDecision Respond(bool accepts)
        {
            return new BotDecision(BotDecisionType.Respond, -1, accepts, EnvidoCall.Ninguno);
        }

        public static BotDecision CallEnvido(EnvidoCall call)
        {
            return new BotDecision(BotDecisionType.CallEnvido, -1, false, call);
        }

        public static BotDecision CallTruco()
        {
            return new BotDecision(BotDecisionType.CallTruco, -1, false, EnvidoCall.Ninguno);
        }
    }
}
