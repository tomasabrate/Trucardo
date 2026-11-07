using System;
using Trucardo.Core.BotDialogue;
using Trucardo.Core.Bots;
using UnityEngine;

namespace Trucardo.Core
{
    public partial class GameServer
    {
        [Header("Local Bot Dialogue")]
        [SerializeField] private BotDialogueSettings botDialogue = new BotDialogueSettings();
        private BotDialogueRuntime dialogueRuntime;
        private bool dialogueWarning;

        private struct DialogueSnapshot
        {
            internal GameState State;
            internal int BotScore, OpponentScore, Played;
            internal TipoCanto Pending;
            internal EstadoEnvido Envido;
            internal NivelTruco Truco;
            internal ulong Caller;
        }

        private static DialogueSnapshot CaptureDialogue(TableSession session)
        {
            var model = session.GameModel;
            return new DialogueSnapshot { State = model.CurrentState, BotScore = model.ScoreTeam2,
                OpponentScore = model.ScoreTeam1, Played = model.P1PlayedCards.Count + model.P2PlayedCards.Count,
                Pending = model.CantoPendiente, Envido = model.EstadoEnvido, Truco = model.EstadoTruco, Caller = model.LastCallerId };
        }

        private void RegisterDialogue(TableSession session)
        {
            if (!IsServer || !botDialogue.enabled) return;
            try
            {
                dialogueRuntime = BotDialogueRuntime.Ensure(botDialogue);
                if (session.DialoguePersonality == default && session.DialoguePersonality != defaultBotPersonality)
                {
                    session.DialoguePersonality = defaultBotPersonality;
                }
                session.Player2Dialogue = dialogueRuntime?.Register(session.Player2Id);
            }
            catch (Exception exception) { DialogueFailed(exception); }
        }

        private void ReleaseDialogue(TableSession session)
        {
            if (dialogueRuntime != null) dialogueRuntime.Release(session.Player2Dialogue);
            else session.Player2Dialogue?.Dispose();
            session.Player2Dialogue = null;
        }

        private void ReleaseAllDialogue()
        {
            foreach (var session in sessions) ReleaseDialogue(session);
        }

        private void OnDisable() => ReleaseAllDialogue();

        private void DialogueHandStarted(TableSession session)
        {
            if (!IsServer || session.Player2Dialogue == null) return;
            session.Player2Dialogue.Invalidate();
            EmitDialogue(session, new BotDialogueEvent(BotDialogueEventType.HandStarted));
        }

        // Called only AFTER the command queue. Rejected/no-op commands produce no events.
        private void AfterDialogueAction(TableSession session, DialogueSnapshot before, ulong actor,
            DialogueDecision decision, DialogueCall call = DialogueCall.None)
        {
            if (!IsServer || session.Player2Dialogue == null) return;
            var after = CaptureDialogue(session);
            if (before.State == after.State && before.BotScore == after.BotScore && before.OpponentScore == after.OpponentScore
                && before.Played == after.Played && before.Pending == after.Pending && before.Envido == after.Envido
                && before.Truco == after.Truco && before.Caller == after.Caller) return;
            // Only invalidate when the round or match has truly ended, avoiding aborting in-flight comments prematurely
            if (after.State == GameState.RondaTerminada || after.State == GameState.PartidaTerminada)
                session.Player2Dialogue.Invalidate();
            bool botActed = actor == session.Player2Id;
            if (decision == DialogueDecision.Accept || decision == DialogueDecision.Reject)
                call = before.Pending == TipoCanto.Truco ? PendingTrucoName(before.Truco) : EnvidoName(before.Envido);
            var actualDecision = botActed ? decision : DialogueDecision.None;
            BotDialogueEvent ev = null;
            // Terminal results supersede a response/call in the same action, cancelling older requests.
            if (after.State == GameState.PartidaTerminada)
                ev = new BotDialogueEvent(after.BotScore >= 30 ? BotDialogueEventType.MatchWon : BotDialogueEventType.MatchLost, call, actualDecision);
            else if (after.State == GameState.RondaTerminada)
                ev = new BotDialogueEvent(after.BotScore > before.BotScore ? BotDialogueEventType.HandWon : BotDialogueEventType.HandLost, call, actualDecision);
            else if (decision == DialogueDecision.Call)
                ev = new BotDialogueEvent(botActed ? BotDialogueEventType.BotCalled : BotDialogueEventType.OpponentCalled, call, actualDecision);
            else if (botActed && (decision == DialogueDecision.Accept || decision == DialogueDecision.Reject))
                ev = new BotDialogueEvent(decision == DialogueDecision.Accept ? BotDialogueEventType.BotAccepted : BotDialogueEventType.BotRejected, call, actualDecision);
            else if (decision == DialogueDecision.PlayedCard)
                ev = new BotDialogueEvent(BotDialogueEventType.CardPlayed, DialogueCall.None, actualDecision);
            if (ev != null) EmitDialogue(session, ev);
        }

        private void EmitDialogue(TableSession session, BotDialogueEvent ev)
        {
            try
            {
                var model = session.GameModel;
                // Only our bot's cards enter this local reduction. Nothing literal survives it.
                var cards = BuildBotCardObservations(model, model.P2Hand);
                cards.AddRange(BuildBotCardObservations(model, model.P2PlayedCards));
                int points = BotEvaluator.CalculateEnvido(cards);
                EnvidoStrength envido = points >= 31 ? EnvidoStrength.VeryStrong : points >= 27 ? EnvidoStrength.Strong
                    : points >= 20 ? EnvidoStrength.Medium : EnvidoStrength.Weak;
                // Coarse rank category for flavor, not a new gameplay evaluator.
                int maximumRank = 1, strongest = 0;
                foreach (var card in masterDeck.AvailableCards) if (card != null) maximumRank = Math.Max(maximumRank, card.TrucoRank);
                foreach (var card in cards) strongest = Math.Max(strongest, card.TrucoRank);
                double relative = (double)strongest / maximumRank;
                TrucoStrength truco = relative >= 0.8 ? TrucoStrength.Strong : relative >= 0.5 ? TrucoStrength.Medium : TrucoStrength.Weak;
                bool envidoEvent = ev.Call == DialogueCall.Envido || ev.Call == DialogueCall.RealEnvido || ev.Call == DialogueCall.FaltaEnvido;
                DialogueConfidence confidence = envidoEvent
                    ? (envido >= EnvidoStrength.Strong ? DialogueConfidence.High : envido == EnvidoStrength.Medium ? DialogueConfidence.Medium : DialogueConfidence.Low)
                    : (truco == TrucoStrength.Strong ? DialogueConfidence.High : truco == TrucoStrength.Medium ? DialogueConfidence.Medium : DialogueConfidence.Low);

                // Compute rich match situation and hand context for authentic Argentine dialogue
                int botScore = model.ScoreTeam2;
                int oppScore = model.ScoreTeam1;
                int diff = botScore - oppScore;
                ScoreSituation scoreSituation;
                if (botScore >= 25 || oppScore >= 25) scoreSituation = ScoreSituation.DefinicionBuenas;
                else if (diff == 0) scoreSituation = ScoreSituation.Empatados;
                else if (diff >= 5) scoreSituation = ScoreSituation.GanandoComodo;
                else if (diff > 0) scoreSituation = ScoreSituation.GanandoAjustado;
                else if (diff <= -5) scoreSituation = ScoreSituation.PerdiendoFeo;
                else scoreSituation = ScoreSituation.PerdiendoAjustado;

                int trickIndex = Mathf.Clamp(model.CurrentRoundIndex + 1, 1, 3);
                TrickState trickState;
                if (model.CurrentRoundIndex == 0)
                {
                    trickState = TrickState.PrimeraMano;
                }
                else if (model.CurrentRoundIndex == 1)
                {
                    var r0 = model.RoundWinners != null && model.RoundWinners.Length > 0 ? model.RoundWinners[0] : ManoGanadaPor.Nadie;
                    if (r0 == ManoGanadaPor.Equipo2) trickState = TrickState.GanamosPrimera;
                    else if (r0 == ManoGanadaPor.Equipo1) trickState = TrickState.PerdimosPrimera;
                    else if (r0 == ManoGanadaPor.Parda) trickState = TrickState.PardaPrimera;
                    else trickState = TrickState.PrimeraMano;
                }
                else
                {
                    trickState = TrickState.TerceraDefinitoria;
                }

                PlayerRole role = (model.ManoPlayerId == session.Player2Id) ? PlayerRole.BotEsMano : PlayerRole.BotEsPie;

                var context = new BotDialogueContext(session.DialoguePersonality, botScore, oppScore,
                    envido, truco, confidence, model.CurrentState,
                    scoreSituation, trickState, role, trickIndex,
                    session.DialoguePersonaDescription);
                _ = session.Player2Dialogue.ObserveAsync(context, ev);
            }
            catch (Exception exception) { DialogueFailed(exception); }
        }

        private void DialogueFailed(Exception exception)
        {
            if (dialogueWarning) return;
            dialogueWarning = true;
            Debug.LogWarning("[Bot Dialogue] Diálogo omitido: " + exception.Message);
        }

        private static DialogueCall ToDialogueCall(EnvidoCall call) => call == EnvidoCall.FaltaEnvido ? DialogueCall.FaltaEnvido
            : call == EnvidoCall.RealEnvido ? DialogueCall.RealEnvido : DialogueCall.Envido;
        private static DialogueCall EnvidoName(EstadoEnvido state) => state == EstadoEnvido.FaltaEnvido ? DialogueCall.FaltaEnvido
            : state == EstadoEnvido.RealEnvido || state == EstadoEnvido.EnvidoRealEnvido || state == EstadoEnvido.EnvidoEnvidoRealEnvido
                ? DialogueCall.RealEnvido : DialogueCall.Envido;
        private static DialogueCall PendingTrucoName(NivelTruco level) => level == NivelTruco.Ninguno ? DialogueCall.Truco
            : level == NivelTruco.Truco ? DialogueCall.Retruco : DialogueCall.ValeCuatro;
    }
}
