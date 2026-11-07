using Trucardo.Core.Bots;

namespace Trucardo.Core.BotDialogue
{
    public enum EnvidoStrength { Weak, Medium, Strong, VeryStrong }
    public enum TrucoStrength { Weak, Medium, Strong }
    public enum DialogueConfidence { Low, Medium, High }
    public enum DialogueCall { None, Envido, RealEnvido, FaltaEnvido, Truco, Retruco, ValeCuatro }
    public enum DialogueDecision { None, Call, Accept, Reject, PlayedCard }
    public enum ScoreSituation { Empatados, GanandoComodo, GanandoAjustado, PerdiendoAjustado, PerdiendoFeo, DefinicionBuenas }
    public enum TrickState { PrimeraMano, GanamosPrimera, PerdimosPrimera, PardaPrimera, TerceraDefinitoria }
    public enum PlayerRole { BotEsMano, BotEsPie }
    public enum BotDialogueEventType { HandStarted, OpponentCalled, BotCalled, BotAccepted, BotRejected,
        CardPlayed, HandWon, HandLost, MatchWon, MatchLost }

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
        public ScoreSituation ScoreSituation { get; }
        public TrickState TrickState { get; }
        public PlayerRole Role { get; }
        public int TrickIndex { get; }
        public string PersonaDescription { get; }

        public BotDialogueContext(BotPersonality personality, int botScore, int opponentScore,
            EnvidoStrength envido, TrucoStrength truco, DialogueConfidence confidence, GameState phase,
            ScoreSituation scoreSituation = ScoreSituation.Empatados,
            TrickState trickState = TrickState.PrimeraMano,
            PlayerRole role = PlayerRole.BotEsMano,
            int trickIndex = 1,
            string personaDescription = null)
        {
            Personality = personality; BotScore = botScore; OpponentScore = opponentScore;
            Envido = envido; Truco = truco; Confidence = confidence; Phase = phase;
            ScoreSituation = scoreSituation; TrickState = trickState; Role = role;
            TrickIndex = trickIndex;
            PersonaDescription = personaDescription;
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
