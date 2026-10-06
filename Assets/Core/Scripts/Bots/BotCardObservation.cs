namespace Trucardo.Core.Bots
{
    public sealed class BotCardObservation
    {
        public int CardIndex { get; }
        public CardSuit Suit { get; }
        public int Number { get; }
        public int TrucoRank { get; }
        public int EnvidoValue { get; }

        public BotCardObservation(
            int cardIndex,
            CardSuit suit,
            int number,
            int trucoRank,
            int envidoValue)
        {
            CardIndex = cardIndex;
            Suit = suit;
            Number = number;
            TrucoRank = trucoRank;
            EnvidoValue = envidoValue;
        }
    }
}
