using Trucardo.Core.Bots;

namespace Trucardo.Core.BotDialogue
{
    public enum EnvidoStrength { Weak, Medium, Strong, VeryStrong }
    public enum TrucoStrength { Weak, Medium, Strong }
    public enum DialogueConfidence { Low, Medium, High }
    public enum DialogueCall { None, Envido, RealEnvido, FaltaEnvido, Truco, Retruco, ValeCuatro }
    public enum DialogueDecision { None, Call, Accept, Reject, PlayedCard }
    public enum BotDialogueEventType { HandStarted, OpponentCalled, BotCalled, BotAccepted, BotRejected,
        HandWon, HandLost, MatchWon, MatchLost }

    // An immutable allowlist. No model, observation, cards, commands or callbacks can cross this boundary.
    public sealed class BotDialogueContext
    {
        public BotPersonality Personality { get; }
        public int BotScore { get; }
        public int OpponentScore { get; }
        public EnvidoStrength Envido { get; }
        public TrucoStrength Truco { get; }
        public DialogueConfidence Confidence { get; }
        public GameState Phase { get; }

        public BotDialogueContext(BotPersonality personality, int botScore, int opponentScore,
            EnvidoStrength envido, TrucoStrength truco, DialogueConfidence confidence, GameState phase)
        {
            Personality = personality; BotScore = botScore; OpponentScore = opponentScore;
            Envido = envido; Truco = truco; Confidence = confidence; Phase = phase;
        }
    }

    public sealed class BotDialogueEvent
    {
        public BotDialogueEventType Type { get; }
        public DialogueCall Call { get; }
        public DialogueDecision Decision { get; }
        public BotDialogueEvent(BotDialogueEventType type, DialogueCall call = DialogueCall.None,
            DialogueDecision decision = DialogueDecision.None)
        { Type = type; Call = call; Decision = decision; }
    }
}
