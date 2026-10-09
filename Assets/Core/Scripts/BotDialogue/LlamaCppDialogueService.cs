using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Trucardo.Core.BotDialogue
{
    public sealed class LlamaCppDialogueService : IBotDialogueModel, IDisposable
    {
        [Serializable] private sealed class Message { public string role; public string content; }
        [Serializable] private sealed class TemplateOptions { public bool enable_thinking = false; }
        [Serializable] private sealed class Request
        {
            public string model;
            public Message[] messages;
            public float temperature;
            public int max_tokens;
            public bool stream = false;
            public int reasoning_budget = 0;
            public TemplateOptions chat_template_kwargs = new TemplateOptions();
        }
        [Serializable] private sealed class Response { public Choice[] choices; }
        [Serializable] private sealed class Choice { public Message message; public string finish_reason; }

        private readonly BotDialogueSettings settings;
        private readonly LocalLlmProcessManager process;
        private readonly Action<string> warnOnce;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private UnityWebRequest activeRequest;
        private bool busy;
        private bool disposed;
        private int failures;
        private bool unavailable;

        public LlamaCppDialogueService(BotDialogueSettings settings, LocalLlmProcessManager process, Action<string> warnOnce)
        { this.settings = settings; this.process = process; this.warnOnce = warnOnce; }

        public async Task<string> GenerateAsync(BotDialogueContext context, BotDialogueEvent ev,
            IReadOnlyList<string> recentPhrases, CancellationToken cancellation)
        {
            // Drop rather than queue: old comments must never delay newer gameplay.
            if (disposed || unavailable || busy || !process.IsReady) return null;
            busy = true;
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, lifetime.Token))
            {
                linked.CancelAfter(TimeSpan.FromSeconds(settings.requestTimeout));
                try
                {
                    using (var request = new UnityWebRequest(process.BaseUrl + "/v1/chat/completions", "POST"))
                    {
                        activeRequest = request;
                        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(
                            BuildRequestJson(settings, process.ModelId, context, ev, recentPhrases)));
                        request.downloadHandler = new DownloadHandlerBuffer();
                        request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                        request.timeout = settings.requestTimeout;
                        await DialogueHttp.SendAsync(request, linked.Token);
                        if (request.result != UnityWebRequest.Result.Success)
                            throw new InvalidOperationException("llama-server HTTP: " + request.error);
                        string text = ParseResponse(request.downloadHandler.text, ev);
                        failures = 0;
                        return text;
                    }
                }
                catch (OperationCanceledException)
                {
                    if (!cancellation.IsCancellationRequested && !lifetime.IsCancellationRequested)
                        Failed("Tiempo de generación agotado.");
                    return null;
                }
                catch (Exception exception) { Failed(exception.Message); return null; }
                finally { activeRequest = null; busy = false; }
            }
        }

        public static string ParseResponse(string json, BotDialogueEvent ev)
        {
            if (string.IsNullOrWhiteSpace(json) || json.Length > 32768) return null;
            try
            {
                var response = JsonUtility.FromJson<Response>(json);
                if (response?.choices == null || response.choices.Length == 0) return null;
                var choice = response.choices[0];
                if (choice == null || choice.finish_reason == "length") return null;
                return DialogueText.Filter(choice.message?.content, ev);
            }
            catch (ArgumentException) { return null; }
        }

        public static string BuildRequestJson(BotDialogueSettings settings, string modelId,
            BotDialogueContext context, BotDialogueEvent ev, IReadOnlyList<string> recentPhrases)
        {
            return JsonUtility.ToJson(new Request
            {
                model = modelId, temperature = settings.temperature, max_tokens = settings.maxTokens,
                messages = new[] { new Message { role = "system", content = DialoguePrompt.System },
                    new Message { role = "user", content = DialoguePrompt.User(context, ev, recentPhrases) } }
            });
        }

        private void Failed(string message)
        {
            if (disposed) return;
            warnOnce?.Invoke(message);
            if (++failures >= 3) unavailable = true;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            lifetime.Cancel();
            activeRequest?.Abort();
            // Pending continuations own and dispose their linked tokens and requests.
        }
    }
}
