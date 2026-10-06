namespace Trucardo.Core.Bots
{
    public sealed class BotBrain
    {
        public BotDecision Decide(BotObservation observation)
        {
            if (observation == null) return BotDecision.NoAction;
            if (observation.GameState != GameState.TurnoJugador) return BotDecision.NoAction;
            if (observation.CurrentTurnId != observation.BotId) return BotDecision.NoAction;
            if (observation.OwnHand.Count == 0) return BotDecision.NoAction;

            return BotDecision.PlayCard(observation.OwnHand[0]);
        }
    }
}
