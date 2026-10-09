using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Trucardo.Core.Rules;

namespace Trucardo.Core.Bots
{
    public sealed class BotBrain
    {
        // Calls measure gain over continuing play; responses compare actual stakes.
        private const double PlayBaseline = 0.5;
        private const double CardUtilityScale = 2;
        private const double CallUtilityScale = 0.35;
        private const double BaseEnvidoFoldProbability = 0.08;
        private const double EnvidoAggressionProbabilityRange = 0.10;
        private const double TrucoCallPremium = 0.22;
        private const double RaisePremium = 0.12;
        private const double AggressionWeight = 0.45;
        private const double RiskWeight = 0.35;
        private const double OpponentCallEvidence = 0.2;
        private const double MatchWinBonus = 8;
        private const double BluffBonus = 0.85;
        private const double BluffMinStrength = 0.18;
        private const double BluffMaxStrength = 0.6;
        private const double EstimatedFoldProbability = 0.12;
        private readonly BotProfile profile;
        private readonly Random random;
        public string LastEvaluationSummary { get; private set; } = string.Empty;

        public BotBrain(BotProfile profile = null, int seed = 0)
        {
            this.profile = profile ?? BotProfile.Create(BotDifficulty.Normal, BotPersonality.Balanced);
            random = new Random(seed);
        }

        public BotDecision Decide(BotObservation observation, bool includeDiagnostics = false)
        {
            LastEvaluationSummary = string.Empty;
            if (observation == null) return BotDecision.NoAction;
            bool responding = observation.GameState == GameState.EsperandoRespuestaCanto
                && observation.CantoPendiente != TipoCanto.Ninguno
                && observation.LastCallerId != observation.BotId;
            bool playing = observation.GameState == GameState.TurnoJugador
                && observation.CurrentTurnId == observation.BotId;
            if (!responding && !playing) return BotDecision.NoAction;

            var evaluation = new BotEvaluator(observation, profile, random);
            var decisions = new List<BotDecision>(8);
            var utilities = new List<double>(8);
            if (responding)
            {
                decisions.Add(BotDecision.Respond(true));
                decisions.Add(BotDecision.Respond(false));
                bool envido = observation.CantoPendiente == TipoCanto.Envido;
                utilities.Add(envido ? EvaluateRespondEnvido(observation, evaluation, true)
                    : EvaluateRespondTruco(observation, evaluation, true));
                utilities.Add(envido ? EvaluateRespondEnvido(observation, evaluation, false)
                    : EvaluateRespondTruco(observation, evaluation, false));
                AddEnvidoCallOptions(observation, evaluation, decisions, utilities, true);
            }
            else
            {
                AddPlayOptions(observation, evaluation, decisions, utilities);
                AddEnvidoCallOptions(observation, evaluation, decisions, utilities, false);
                if (observation.CanCallTruco && observation.EstadoTruco != NivelTruco.Vale4
                    && (observation.EstadoTruco == NivelTruco.Ninguno || observation.LastTrucoCallerId != observation.BotId))
                {
                    decisions.Add(BotDecision.CallTruco());
                    utilities.Add(EvaluateCallTruco(observation, evaluation));
                }
            }
            return Choose(observation, evaluation, decisions, utilities, includeDiagnostics);
        }

        private void AddPlayOptions(BotObservation observation, BotEvaluator evaluation,
            List<BotDecision> decisions, List<double> utilities)
        {
            double best = double.NegativeInfinity;
            for (int i = 0; i < observation.OwnHand.Count; i++)
                best = Math.Max(best, evaluation.EvaluatePlayCard(i));
            for (int i = 0; i < observation.OwnHand.Count; i++)
            {
                decisions.Add(BotDecision.PlayCard(observation.OwnHand[i].CardIndex));
                utilities.Add(PlayBaseline + CardUtilityScale * profile.DecisionQuality
                    * (evaluation.EvaluatePlayCard(i) - best));
            }
        }

        private void AddEnvidoCallOptions(
            BotObservation observation,
            BotEvaluator evaluation,
            List<BotDecision> decisions,
            List<double> utilities,
            bool whileResponding)
        {
            AddEnvidoCallOption(EnvidoCall.Envido, observation.CanCallEnvido);
            AddEnvidoCallOption(EnvidoCall.RealEnvido, observation.CanCallRealEnvido);
            AddEnvidoCallOption(EnvidoCall.FaltaEnvido, observation.CanCallFaltaEnvido);

            void AddEnvidoCallOption(EnvidoCall call, bool legal)
            {
                if (!legal) return;
                decisions.Add(BotDecision.CallEnvido(call));
                utilities.Add(EvaluateCallEnvido(
                    observation, evaluation, call, whileResponding));
            }
        }

        private double EvaluateCallEnvido(
            BotObservation observation,
            BotEvaluator evaluation,
            EnvidoCall call,
            bool whileResponding)
        {
            double probability = EnvidoDecisionProbability(evaluation.EnvidoWinProbability);
            EstadoEnvido raisedState = EnvidoRules.Raise(observation.EstadoEnvido, call);
            int acceptedStake = EnvidoRules.GetAcceptedPoints(
                raisedState, observation.ScoreTeam1, observation.ScoreTeam2);
            // A personality/exposure gate for Falta only. Keep the existing utility,
            // stakes and response maths intact; decision noise cannot bypass this gate.
            if (call == EnvidoCall.FaltaEnvido
                && evaluation.EnvidoWinProbability < FaltaConfidenceThreshold(observation, acceptedStake))
                return double.NegativeInfinity;
            int rejectedStake = EnvidoRules.GetRejectedPoints(observation.PendingEnvidoPoints);
            bool resumesPendingTruco = whileResponding
                && observation.CantoPendiente == TipoCanto.Truco;
            double gain = EvaluateProposedEnvido(
                observation,
                evaluation,
                probability,
                acceptedStake,
                rejectedStake,
                resumesPendingTruco);
            return whileResponding ? gain : PlayBaseline + CallUtilityScale * gain;
        }

        private double FaltaConfidenceThreshold(BotObservation observation, int acceptedStake)
        {
            // Full exposure at 15+ points; one/two-point Falta remains a closing option.
            double exposure = Math.Max(0, Math.Min(1, (acceptedStake - 2) / 13.0));
            double threshold = 0.62 + 0.12 * (1 - profile.RiskTolerance)
                + 0.26 * exposure;
            if (observation.EstadoEnvido == EstadoEnvido.Ninguno)
                threshold += 0.04 * exposure; // Prefer escalation over opening with Falta.
            if (observation.OpponentScore >= observation.BotScore
                && observation.OpponentScore + acceptedStake >= 30)
                threshold += 0.015; // Losing this accepted bet ends the match.
            return Math.Max(0.62, Math.Min(0.999, threshold));
        }

        private double EvaluateProposedEnvido(
            BotObservation observation,
            BotEvaluator evaluation,
            double probability,
            int acceptedStake,
            int rejectedStake,
            bool resumesPendingTruco)
        {
            int botScore = observation.BotScore;
            int opponentScore = observation.OpponentScore;
            double winValue = PointValue(botScore, acceptedStake);
            double lossValue = PointValue(opponentScore, acceptedStake);
            double rejectionValue = PointValue(botScore, rejectedStake);
            double foldProbability = EnvidoFoldProbability(
                rejectedStake, acceptedStake);

            double foldUtility = rejectionValue;
            double winUtility = winValue;
            double lossUtility = -lossValue;
            if (resumesPendingTruco)
            {
                foldUtility += EvaluatePendingTrucoContinuation(
                    observation, evaluation, AddPoints(botScore, rejectedStake), opponentScore);
                winUtility += EvaluatePendingTrucoContinuation(
                    observation, evaluation, AddPoints(botScore, acceptedStake), opponentScore);
                lossUtility += EvaluatePendingTrucoContinuation(
                    observation, evaluation, botScore, AddPoints(opponentScore, acceptedStake));
            }

            double acceptedUtility = probability * winUtility
                + (1 - probability) * lossUtility
                - EnvidoRiskPenalty(
                    probability, botScore, opponentScore, lossValue);
            return foldProbability * foldUtility
                + (1 - foldProbability) * acceptedUtility;
        }

        private double EvaluateCallTruco(BotObservation observation, BotEvaluator evaluation)
        {
            double probability = PerceivedProbability(evaluation.TrucoStrength);
            int current = TrucoRules.AcceptedTrucoPoints(observation.EstadoTruco);
            int raised = TrucoRules.RaisedTrucoPoints(observation.EstadoTruco);
            double continuing = ExpectedPoints(observation, probability, current);
            // Acceptance selects somewhat stronger opponents: a tunable prior, not hidden knowledge.
            double acceptedProbability = ConditionOnCall(probability);
            double called = EstimatedFoldProbability * PointValue(observation.BotScore, current)
                + (1 - EstimatedFoldProbability) * ExpectedPoints(observation, acceptedProbability, raised);
            double gain = called - continuing - TrucoCallPremium - RaisePremium * (raised - 2);
            gain += AggressionWeight * (profile.Aggression - 0.5) + RiskAdjustment(observation);

            // Bluff mediocre hands only, with room to lose the stake and no match win
            // available just by playing. Randomness modifies an already evaluated action.
            if (evaluation.TrucoStrength >= BluffMinStrength && evaluation.TrucoStrength <= BluffMaxStrength
                && observation.OpponentScore + raised < 30 && observation.BotScore + current < 30
                && random.NextDouble() < profile.BluffFrequency)
                gain += BluffBonus;
            return PlayBaseline + CallUtilityScale * gain;
        }

        private double EvaluateRespondEnvido(BotObservation observation, BotEvaluator evaluation, bool accepts)
        {
            int rejectedStake = EnvidoRules.GetRejectedPoints(observation.EnvidoAcceptedPoints);
            if (!accepts) return -PointValue(observation.OpponentScore, rejectedStake);
            double probability = EnvidoDecisionProbability(evaluation.EnvidoWinProbability);
            double lossValue = PointValue(observation.OpponentScore, observation.PendingEnvidoPoints);
            return ExpectedPoints(observation, probability, observation.PendingEnvidoPoints)
                - EnvidoRiskPenalty(
                    probability,
                    observation.BotScore,
                    observation.OpponentScore,
                    lossValue);
        }

        private double EvaluateRespondTruco(BotObservation observation, BotEvaluator evaluation, bool accepts)
        {
            return EvaluateRespondTrucoAtScores(
                observation,
                evaluation,
                accepts,
                observation.BotScore,
                observation.OpponentScore);
        }

        private double EvaluateRespondTrucoAtScores(
            BotObservation observation,
            BotEvaluator evaluation,
            bool accepts,
            int botScore,
            int opponentScore)
        {
            int current = TrucoRules.AcceptedTrucoPoints(observation.EstadoTruco);
            if (!accepts) return -PointValue(opponentScore, current);
            double probability = ConditionOnCall(PerceivedProbability(evaluation.TrucoStrength));
            return ExpectedPoints(
                    botScore,
                    opponentScore,
                    probability,
                    TrucoRules.RaisedTrucoPoints(observation.EstadoTruco))
                + RiskAdjustment(botScore, opponentScore)
                + AggressionWeight * (profile.Aggression - 0.5);
        }

        private double EvaluatePendingTrucoContinuation(
            BotObservation observation,
            BotEvaluator evaluation,
            int botScore,
            int opponentScore)
        {
            if (botScore >= 30 || opponentScore >= 30) return 0;
            return Math.Max(
                EvaluateRespondTrucoAtScores(
                    observation, evaluation, true, botScore, opponentScore),
                EvaluateRespondTrucoAtScores(
                    observation, evaluation, false, botScore, opponentScore));
        }

        private double EnvidoDecisionProbability(double probability)
        {
            double conditioned = ConditionOnCall(PerceivedProbability(probability));
            double personalityShift = EnvidoAggressionProbabilityRange
                * (profile.EnvidoAggression - 0.5);
            return Math.Max(0, Math.Min(1, conditioned + personalityShift));
        }

        private static double EnvidoFoldProbability(
            int rejectionStake,
            int acceptedStake)
        {
            if (rejectionStake <= 0 || acceptedStake <= 0)
                return BaseEnvidoFoldProbability;
            // Do not reward raises where folding pays more than accepting risks.
            double relativeReward = Math.Min(1, (double)acceptedStake / rejectionStake);
            return BaseEnvidoFoldProbability * relativeReward;
        }

        private double EnvidoRiskPenalty(
            double probability,
            int botScore,
            int opponentScore,
            double downsideValue)
        {
            if (downsideValue <= 0) return 0;
            double maximumLossValue = PointValue(
                opponentScore, Math.Max(0, 30 - opponentScore));
            if (maximumLossValue <= 0) return 0;

            double scorePressure = (opponentScore - botScore) / 30.0;
            double riskSensitivity = Math.Max(0, Math.Min(1,
                (1 - profile.RiskTolerance) * (1 - scorePressure)));
            // Exposure makes downside convex: two points early are a small fraction
            // of the match, while a terminal loss receives the full profile penalty.
            double exposure = Math.Min(1, downsideValue / maximumLossValue);
            return (1 - probability) * downsideValue * riskSensitivity * exposure;
        }

        private double PerceivedProbability(double probability)
        {
            return 0.5 + (probability - 0.5) * profile.DecisionQuality;
        }

        private static double ConditionOnCall(double probability)
        {
            return probability * (1 - OpponentCallEvidence * (1 - probability));
        }

        private double RiskAdjustment(BotObservation observation)
        {
            return RiskAdjustment(observation.BotScore, observation.OpponentScore);
        }

        private double RiskAdjustment(int botScore, int opponentScore)
        {
            double scorePressure = (opponentScore - botScore) / 30.0;
            return RiskWeight * (profile.RiskTolerance - 0.5 + scorePressure);
        }

        private static int AddPoints(int score, int points)
        {
            return Math.Min(30, score + points);
        }

        private static double PointValue(int score, int points)
        {
            int distance = Math.Max(0, 30 - score);
            return Math.Min(points, distance) + (distance > 0 && points >= distance ? MatchWinBonus : 0);
        }

        private static double ExpectedPoints(BotObservation observation, double probability, int stake)
        {
            return ExpectedPoints(
                observation.BotScore, observation.OpponentScore, probability, stake);
        }

        private static double ExpectedPoints(
            int botScore, int opponentScore, double probability, int stake)
        {
            return probability * PointValue(botScore, stake)
                - (1 - probability) * PointValue(opponentScore, stake);
        }

        private BotDecision Choose(BotObservation observation, BotEvaluator evaluation,
            List<BotDecision> decisions, List<double> utilities, bool diagnostics)
        {
            if (decisions.Count == 0) return BotDecision.NoAction;
            var summary = diagnostics ? new StringBuilder() : null;
            if (summary != null)
            {
                summary.AppendFormat(CultureInfo.InvariantCulture,
                    "[BOT] Envido: {0}; P(win envido): {1}; combinations: {2}; Truco strength: {3:F3}\n",
                    evaluation.OwnEnvido,
                    evaluation.EnvidoCombinationsEvaluated == 0 ? "n/a" : evaluation.EnvidoWinProbability.ToString("F3", CultureInfo.InvariantCulture),
                    evaluation.EnvidoCombinationsEvaluated, evaluation.TrucoStrength);
                if (observation.Player1PlayedCards.Count > observation.CurrentRoundIndex)
                {
                    var opponent = observation.Player1PlayedCards[observation.CurrentRoundIndex];
                    summary.AppendLine($"Opponent card: {opponent.Number} {opponent.Suit}");
                }
            }
            int best = 0;
            double bestUtility = double.NegativeInfinity;
            for (int i = 0; i < decisions.Count; i++)
            {
                double utility = utilities[i] + (random.NextDouble() * 2 - 1) * profile.DecisionNoise;
                if (summary != null)
                    summary.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1:F3} -> {2:F3}\n",
                        Describe(decisions[i], observation), utilities[i], utility);
                if (utility > bestUtility)
                {
                    best = i;
                    bestUtility = utility;
                }
            }
            if (summary != null)
            {
                summary.Append("Chosen: ").Append(Describe(decisions[best], observation));
                LastEvaluationSummary = summary.ToString();
            }
            return decisions[best];
        }

        private static string Describe(BotDecision decision, BotObservation observation)
        {
            if (decision.Type == BotDecisionType.Respond) return $"Respond({decision.Accepts})";
            if (decision.Type == BotDecisionType.CallEnvido)
                return $"CallEnvido({decision.EnvidoCall})";
            if (decision.Type == BotDecisionType.PlayCard)
                foreach (var card in observation.OwnHand)
                    if (card.CardIndex == decision.CardIndex) return $"PlayCard({card.Number} {card.Suit})";
            return decision.Type.ToString();
        }
    }
}
