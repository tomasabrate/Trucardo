using System;

namespace Trucardo.Core.Bots
{
    public enum BotDifficulty { Easy, Normal, Hard, Expert }
    public enum BotPersonality { Balanced, Aggressive, Conservative, Bluffer, Risky }

    /// <summary>Immutable tuning. Difficulty changes evaluation, never the observation.</summary>
    public sealed class BotProfile
    {
        public double DecisionQuality { get; }
        public double Aggression { get; }
        public double RiskTolerance { get; }
        public double BluffFrequency { get; }
        public double EnvidoAggression { get; }
        public double CardConservation { get; }
        public double DecisionNoise { get; }
        public int SampleBudget { get; }

        public BotProfile(double decisionQuality, double aggression, double riskTolerance,
            double bluffFrequency, double envidoAggression, double cardConservation,
            double decisionNoise, int sampleBudget)
        {
            DecisionQuality = Clamp(decisionQuality);
            Aggression = Clamp(aggression);
            RiskTolerance = Clamp(riskTolerance);
            BluffFrequency = Clamp(bluffFrequency);
            EnvidoAggression = Clamp(envidoAggression);
            CardConservation = Clamp(cardConservation);
            DecisionNoise = Clamp(decisionNoise);
            SampleBudget = Math.Max(1, Math.Min(10000, sampleBudget));
        }

        private static double Clamp(double value)
        {
            return double.IsNaN(value) ? 0.5 : Math.Max(0, Math.Min(1, value));
        }

        public static BotProfile Create(BotDifficulty difficulty, BotPersonality personality)
        {
            double quality, noise;
            int budget;
            switch (difficulty)
            {
                case BotDifficulty.Easy: quality = 0.55; noise = 0.12; budget = 96; break;
                case BotDifficulty.Hard: quality = 0.95; noise = 0.02; budget = 2048; break;
                case BotDifficulty.Expert: quality = 1; noise = 0; budget = 10000; break;
                default: quality = 0.8; noise = 0.06; budget = 512; break;
            }

            double aggression = 0.5, risk = 0.5, bluff = 0.04, envido = 0.5, conservation = 0.5;
            switch (personality)
            {
                case BotPersonality.Aggressive:
                    aggression = 0.85; risk = 0.7; bluff = 0.10; envido = 0.75; conservation = 0.35;
                    break;
                case BotPersonality.Conservative:
                    aggression = 0.2; risk = 0.2; bluff = 0.01; envido = 0.25; conservation = 0.85;
                    break;
                case BotPersonality.Bluffer:
                    aggression = 0.65; risk = 0.6; bluff = 0.25; envido = 0.5; conservation = 0.6;
                    break;
                case BotPersonality.Risky:
                    aggression = 0.7; risk = 0.95; bluff = 0.12; envido = 0.7; conservation = 0.25;
                    break;
            }
            return new BotProfile(quality, aggression, risk, bluff, envido, conservation, noise, budget);
        }
    }
}
