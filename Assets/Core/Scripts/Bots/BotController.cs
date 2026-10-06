namespace Trucardo.Core.Bots
{
    public sealed class BotController
    {
        private readonly BotBrain brain;

        public BotController(BotBrain brain)
        {
            this.brain = brain;
        }

        public BotDecision Decide(BotObservation observation)
        {
            if (brain == null) return BotDecision.NoAction;
            return brain.Decide(observation);
        }
    }
}
