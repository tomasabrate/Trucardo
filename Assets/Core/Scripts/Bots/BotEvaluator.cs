using System;
using System.Collections.Generic;
using Trucardo.Core.Rules;

namespace Trucardo.Core.Bots
{
    /// <summary>
    /// One evaluation per decision, using only copied observations. Envido samples possible
    /// hands; Truco uses a small deterministic trick-outcome tree, not gameplay rollouts.
    /// </summary>
    public sealed class BotEvaluator
    {
        private const double BestReplyWeight = 0.55;
        private const double ConservationWeight = 0.08;
        private const double UnknownStrength = 0.5;
        private const double RankCostWeight = 0.25;
        private readonly BotObservation observation;
        private readonly BotProfile profile;
        private readonly double[] playUtilities;
        private readonly Dictionary<int, (double Win, double Tie)> rankProbabilities
            = new Dictionary<int, (double Win, double Tie)>();
        public int OwnEnvido { get; }
        public double EnvidoWinProbability { get; }
        public int EnvidoCombinationsEvaluated { get; private set; }
        public double TrucoStrength { get; }

        public BotEvaluator(BotObservation observation, BotProfile profile, Random random)
        {
            this.observation = observation;
            this.profile = profile;
            var originalCards = new List<BotCardObservation>(observation.OwnHand);
            originalCards.AddRange(observation.Player2PlayedCards);
            OwnEnvido = CalculateEnvido(originalCards);
            EnvidoWinProbability = observation.CanCallEnvido
                || observation.CanCallRealEnvido
                || observation.CanCallFaltaEnvido
                || observation.CantoPendiente == TipoCanto.Envido
                ? EstimateEnvido(random) : UnknownStrength;

            playUtilities = new double[observation.OwnHand.Count];
            int maxRank = 1;
            foreach (var own in observation.OwnHand) maxRank = Math.Max(maxRank, own.TrucoRank);
            foreach (var unseen in observation.UnseenCards) maxRank = Math.Max(maxRank, unseen.TrucoRank);
            double strongest = 0;
            for (int i = 0; i < playUtilities.Length; i++)
            {
                var card = observation.OwnHand[i];
                double strength = EstimateTruco(card);
                strongest = Math.Max(strongest, strength);
                // Percentiles tie when we hold all the highest cards. Rank still lets
                // us conserve the more expensive of two currently unbeatable cards.
                double cost = (1 - RankCostWeight) * CardPercentile(card)
                    + RankCostWeight * card.TrucoRank / maxRank;
                playUtilities[i] = strength - ConservationWeight * profile.CardConservation * cost;
            }
            // A player can receive a call after having led the current trick.
            TrucoStrength = observation.Player2PlayedCards.Count > observation.CurrentRoundIndex
                ? EstimateTruco(observation.Player2PlayedCards[observation.CurrentRoundIndex])
                : strongest;
        }

        public static int CalculateEnvido(IReadOnlyList<BotCardObservation> cards)
        {
            int best = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                best = Math.Max(best, cards[i].EnvidoValue);
                for (int j = i + 1; j < cards.Count; j++)
                    if (cards[i].Suit == cards[j].Suit)
                        best = Math.Max(best, 20 + cards[i].EnvidoValue + cards[j].EnvidoValue);
            }
            return best;
        }

        private double EstimateEnvido(Random random)
        {
            int hidden = observation.OpponentHiddenCardCount;
            int known = observation.Player1PlayedCards.Count;
            int available = observation.UnseenCards.Count;
            if (hidden < 0 || hidden > 3 || known + hidden != 3 || available < hidden)
                return UnknownStrength;

            var opponent = new BotCardObservation[3];
            for (int i = 0; i < known; i++) opponent[i] = observation.Player1PlayedCards[i];
            int combinations = 1;
            for (int i = 1; i <= hidden; i++) combinations = combinations * (available - i + 1) / i;
            int wins = 0;
            if (combinations <= profile.SampleBudget)
            {
                CountEnvidoCombinations(opponent, known, 0, ref wins);
            }
            else
            {
                // Uniform sampling without replacement inside each candidate hand.
                // A tiny reusable index buffer avoids allocating a shuffled deck per sample.
                var selected = new int[hidden];
                for (int sample = 0; sample < profile.SampleBudget; sample++)
                {
                    for (int i = 0; i < hidden; i++)
                    {
                        int index;
                        bool duplicate;
                        do
                        {
                            index = random.Next(available);
                            duplicate = false;
                            for (int j = 0; j < i; j++) duplicate |= selected[j] == index;
                        } while (duplicate);
                        selected[i] = index;
                        opponent[known + i] = observation.UnseenCards[index];
                    }
                    if (WinsEnvido(opponent)) wins++;
                    EnvidoCombinationsEvaluated++;
                }
            }
            return EnvidoCombinationsEvaluated == 0 ? UnknownStrength : (double)wins / EnvidoCombinationsEvaluated;
        }

        private void CountEnvidoCombinations(BotCardObservation[] cards, int position, int start, ref int wins)
        {
            if (position == cards.Length)
            {
                if (WinsEnvido(cards)) wins++;
                EnvidoCombinationsEvaluated++;
                return;
            }
            int last = observation.UnseenCards.Count - (cards.Length - position);
            for (int i = start; i <= last; i++)
            {
                cards[position] = observation.UnseenCards[i];
                CountEnvidoCombinations(cards, position + 1, i + 1, ref wins);
            }
        }

        private bool WinsEnvido(IReadOnlyList<BotCardObservation> opponent)
        {
            int theirs = CalculateEnvido(opponent);
            return OwnEnvido > theirs || (OwnEnvido == theirs && observation.BotIsMano);
        }

        public double EvaluatePlayCard(int handIndex)
        {
            // Once all outcomes are equally lost, throw the cheapest card. If several
            // cards secure the hand, also prefer the cheapest sufficient winner/tie.
            return playUtilities[handIndex];
        }

        public double CardPercentile(BotCardObservation card)
        {
            RankProbabilities(card, out double win, out double tie);
            return win + tie * 0.5;
        }

        private void RankProbabilities(BotCardObservation card, out double win, out double tie)
        {
            if (rankProbabilities.TryGetValue(card.TrucoRank, out var cached))
            {
                win = cached.Win;
                tie = cached.Tie;
                return;
            }
            int lower = 0, equal = 0;
            foreach (var possible in observation.UnseenCards)
            {
                if (possible.TrucoRank < card.TrucoRank) lower++;
                else if (possible.TrucoRank == card.TrucoRank) equal++;
            }
            int count = observation.UnseenCards.Count;
            win = count == 0 ? UnknownStrength : (double)lower / count;
            tie = count == 0 ? 0 : (double)equal / count;
            rankProbabilities[card.TrucoRank] = (win, tie);
        }

        private double EstimateTruco(BotCardObservation currentCard)
        {
            int round = observation.CurrentRoundIndex;
            if (round < 0 || round >= 3) return UnknownStrength;
            var outcomes = new ManoGanadaPor[3];
            for (int i = 0; i < round; i++) outcomes[i] = observation.RoundWinners[i];
            var remaining = new List<BotCardObservation>(3);
            foreach (var card in observation.OwnHand)
                if (card.CardIndex != currentCard.CardIndex) remaining.Add(card);
            // Continuation is intentionally simple: spend stronger remaining cards first.
            remaining.Sort((a, b) => b.TrucoRank.CompareTo(a.TrucoRank));
            remaining.Insert(0, currentCard);
            bool leads = observation.Player1PlayedCards.Count <= round;
            return ContinueHand(outcomes, round, remaining, 0, leads);
        }

        private double ContinueHand(ManoGanadaPor[] outcomes, int round,
            List<BotCardObservation> cards, int position, bool botLeads)
        {
            var mano = observation.BotIsMano ? ManoGanadaPor.Equipo2 : ManoGanadaPor.Equipo1;
            var result = TrucoRules.ResolveHand(outcomes, round, mano);
            if (result != ManoGanadaPor.Nadie) return result == ManoGanadaPor.Equipo2 ? 1 : 0;
            if (round >= 3 || position >= cards.Count) return UnknownStrength;

            double win, tie;
            if (round == observation.CurrentRoundIndex && observation.Player1PlayedCards.Count > round)
            {
                int comparison = cards[position].TrucoRank.CompareTo(observation.Player1PlayedCards[round].TrucoRank);
                win = comparison > 0 ? 1 : 0;
                tie = comparison == 0 ? 1 : 0;
            }
            else
            {
                RankProbabilities(cards[position], out win, out tie);
                if (botLeads)
                {
                    // Blend a random reply with the rival's best available reply.
                    // Hypergeometric CDF respects the number of hidden cards, without
                    // pretending we know which cards they actually hold.
                    int publicCurrent = observation.Player1PlayedCards.Count > observation.CurrentRoundIndex ? 1 : 0;
                    int count = Math.Max(1, observation.OpponentHiddenCardCount - position + publicCurrent);
                    int n = observation.UnseenCards.Count;
                    double bestWin = AllBelow((int)Math.Round(win * n), n, count);
                    double bestTie = AllBelow((int)Math.Round((win + tie) * n), n, count) - bestWin;
                    win = (1 - BestReplyWeight) * win + BestReplyWeight * bestWin;
                    tie = (1 - BestReplyWeight) * tie + BestReplyWeight * bestTie;
                }
            }
            outcomes[round] = ManoGanadaPor.Equipo2;
            double value = win * ContinueHand(outcomes, round + 1, cards, position + 1, true);
            outcomes[round] = ManoGanadaPor.Equipo1;
            value += Math.Max(0, 1 - win - tie) * ContinueHand(outcomes, round + 1, cards, position + 1, false);
            outcomes[round] = ManoGanadaPor.Parda;
            value += tie * ContinueHand(outcomes, round + 1, cards, position + 1, botLeads);
            outcomes[round] = ManoGanadaPor.Nadie;
            return value;
        }

        private static double AllBelow(int lower, int total, int draws)
        {
            if (total == 0) return UnknownStrength;
            double probability = 1;
            for (int i = 0; i < Math.Min(draws, total); i++)
            {
                if (lower <= i) return 0;
                probability *= (double)(lower - i) / (total - i);
            }
            return probability;
        }
    }
}
