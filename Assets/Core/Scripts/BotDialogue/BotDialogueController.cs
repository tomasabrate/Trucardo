using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Trucardo.Core.Bots;

namespace Trucardo.Core.BotDialogue
{
    // Main-thread orchestration. Its RNG never touches Unity.Random or BotBrain's RNG.
    public sealed class BotDialogueController : IDisposable
    {
        private readonly ulong botId;
        private readonly IBotDialogueModel model;
        private readonly BotDialogueSettings settings;
        private readonly Action<string> reportFailure;
        private readonly Random random = new Random();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly List<string> recent = new List<string>(3);
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private CancellationTokenSource pending;
        private IBotDialogueSink sink;
        private double nextSpeech;
        private long revision;
        private bool busy;
        private bool disposed;
        public bool IsBusy => busy;

        public BotDialogueController(ulong botId, IBotDialogueModel model, IBotDialogueSink sink,
            BotDialogueSettings settings, Action<string> reportFailure)
        {
            this.botId = botId; this.model = model; this.sink = sink;
            this.settings = settings; this.reportFailure = reportFailure;
        }

        public void SetSink(IBotDialogueSink replacement) => sink = replacement ?? throw new ArgumentNullException(nameof(replacement));

        public void Invalidate()
        {
            revision++;
            pending?.Cancel();
        }

        public Task ObserveAsync(BotDialogueContext context, BotDialogueEvent dialogueEvent)
        {
            if (disposed || busy || !settings.enabled || clock.Elapsed.TotalSeconds < nextSpeech)
                return Task.CompletedTask;
            double personality = context.Personality == BotPersonality.Conservative ? 0.75
                : context.Personality == BotPersonality.Balanced ? 0.95 : 1.15;
            double importance = dialogueEvent.Type == BotDialogueEventType.HandStarted ? 0.85
                : dialogueEvent.Type == BotDialogueEventType.CardPlayed ? 0.75
                : dialogueEvent.Type == BotDialogueEventType.MatchWon || dialogueEvent.Type == BotDialogueEventType.MatchLost ? 2.0
                : dialogueEvent.Type == BotDialogueEventType.HandWon || dialogueEvent.Type == BotDialogueEventType.HandLost ? 1.25
                : dialogueEvent.Call == DialogueCall.FaltaEnvido || dialogueEvent.Call == DialogueCall.ValeCuatro ? 1.5 : 1.1;
            if (random.NextDouble() >= Math.Min(1, settings.baseSpeakProbability * personality * importance))
                return Task.CompletedTask;
            busy = true;
            nextSpeech = clock.Elapsed.TotalSeconds + (settings.cooldown * (1.2 - 0.2 * personality));
            pending = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            pending.CancelAfter(TimeSpan.FromSeconds(settings.eventLifetime));
            return SpeakAsync(context, dialogueEvent, revision, pending);
        }

        private async Task SpeakAsync(BotDialogueContext context, BotDialogueEvent dialogueEvent,
            long expectedRevision, CancellationTokenSource request)
        {
            try
            {
                string text = await model.GenerateAsync(context, dialogueEvent, recent.ToArray(), request.Token);
                if (disposed || request.IsCancellationRequested || revision != expectedRevision
                    || string.IsNullOrWhiteSpace(text)) return;
                string normalized = DialogueText.Normalize(text);
                foreach (string previous in recent)
                    if (DialogueText.Normalize(previous) == normalized) return;
                sink.Publish(botId, context, dialogueEvent, text);
                if (recent.Count == 3) recent.RemoveAt(0);
                recent.Add(text);
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { reportFailure?.Invoke(exception.Message); }
            finally
            {
                if (pending == request) pending = null;
                request.Dispose();
                busy = false;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Invalidate();
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }
}
