namespace Trucardo.Core.Bots
{
    public sealed class BotController
    {
        private readonly BotBrain brain;
        public string LastEvaluationSummary => brain?.LastEvaluationSummary ?? string.Empty;

        public BotController(BotBrain brain)
        {
            this.brain = brain;
        }

        public BotDecision Decide(BotObservation observation, bool includeDiagnostics = false)
        {
            if (brain == null) return BotDecision.NoAction;
            return brain.Decide(observation, includeDiagnostics);
        }
    }
}
