using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Trucardo.Core.Bots;
using Trucardo.Core.Rules;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace Trucardo.Core.BotDialogue.Editor
{
    // Explicit, opt-in integration harness. Never runs during normal gameplay or builds.
    [InitializeOnLoad]
    public static class BotDialogueValidation
    {
        private const string Stage = "Trucardo.DialogueValidation.Stage";
        private const string Pid = "Trucardo.DialogueValidation.Pid";
        private const string Report = "Logs/DialogueValidation/results.txt";
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        static BotDialogueValidation() { EditorApplication.playModeStateChanged += PlayState; }

        [MenuItem("Trucardo/Validation/Local dialogue and Falta (batch recommended)")]
        public static async void Run()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Report));
                File.WriteAllText(Report, "Trucardo validation\n");
                ValidateTuning();
                ValidateText();
                await ValidateController();
                await ValidateMatchEnd();
                await ValidateHttpFailure();
                await ValidateRuntime();
                SessionState.SetInt(Stage, 1);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception) { Fail(exception); }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        private static void Log(string message)
        {
            File.AppendAllText(Report, message + "\n");
            Debug.Log("[Dialogue validation] " + message);
        }

        private static BotDialogueContext Context() => new BotDialogueContext(BotPersonality.Aggressive,
            12, 8, EnvidoStrength.Strong, TrucoStrength.Strong, DialogueConfidence.High, GameState.TurnoJugador);

        private sealed class Sink : IBotDialogueSink
        {
            public int Count;
            public void Publish(ulong id, BotDialogueContext context, BotDialogueEvent ev, string text) { Count++; }
        }
        private sealed class DelayedModel : IBotDialogueModel
        {
            public int Calls;
            public TaskCompletionSource<string> Completion = new TaskCompletionSource<string>();
            public Task<string> GenerateAsync(BotDialogueContext context, BotDialogueEvent ev, IReadOnlyList<string> recent, CancellationToken cancellation)
            { Calls++; return Completion.Task; } // Deliberately ignores cancellation: controller must still drop it.
        }

        private static async Task ValidateController()
        {
            var settings = new BotDialogueSettings { cooldown = 0, baseSpeakProbability = 1, eventLifetime = 1 };
            var model = new DelayedModel();
            var sink = new Sink();
            var controller = new BotDialogueController(42, model, sink, settings, message => throw new Exception(message));
            var ev = new BotDialogueEvent(BotDialogueEventType.BotAccepted, DialogueCall.Truco, DialogueDecision.Accept);
            Task first = controller.ObserveAsync(Context(), ev);
            await controller.ObserveAsync(Context(), ev);
            Check(model.Calls == 1, "One active request per bot");
            controller.Invalidate(); // Includes terminal state, bot departure, and next action.
            model.Completion.SetResult("Quiero nomás, ahora bancate el canto.");
            await first;
            Check(sink.Count == 0, "Stale completion reached sink");
            model.Completion = new TaskCompletionSource<string>();
            Task second = controller.ObserveAsync(Context(), ev);
            controller.Dispose();
            model.Completion.SetResult("Quiero nomás, ahora bancate el canto.");
            await second;
            Check(sink.Count == 0, "Disposed bot reached sink");
            model = new DelayedModel();
            controller = new BotDialogueController(43, model, sink, settings, null);
            first = controller.ObserveAsync(Context(), ev);
            await Task.Delay(1100);
            model.Completion.SetResult("Quiero nomás, ahora bancate el canto.");
            await first;
            Check(sink.Count == 0, "Expired phrase reached sink");
            controller.Dispose();
            model = new DelayedModel();
            controller = new BotDialogueController(44, model, sink, settings, null);
            first = controller.ObserveAsync(Context(), ev);
            model.Completion.SetResult("Quiero nomás, ahora bancate el canto.");
            await first;
            Check(sink.Count == 1, "Eligible phrase missing from sink");
            var replacement = new Sink();
            controller.SetSink(replacement);
            model.Completion = new TaskCompletionSource<string>();
            first = controller.ObserveAsync(Context(), ev);
            model.Completion.SetResult("Quiero nomás, ahora bancate el canto!");
            await first;
            Check(replacement.Count == 0, "Repeated phrase reached replacement sink");
            model.Completion = new TaskCompletionSource<string>();
            first = controller.ObserveAsync(Context(), ev);
            model.Completion.SetResult("Mirá vos, hoy viniste con ganas.");
            await first;
            Check(replacement.Count == 1 && sink.Count == 1, "Sink replacement failed");
            controller.Dispose();
            Log("PASS controller: concurrency, late completion, disposal, TTL, anti-repetition, sink replacement");
        }

        private static void ValidateText()
        {
            var ev = new BotDialogueEvent(BotDialogueEventType.BotAccepted, DialogueCall.Truco, DialogueDecision.Accept);
            Check(LlamaCppDialogueService.ParseResponse("bad json", ev) == null, "Malformed JSON");
            Check(LlamaCppDialogueService.ParseResponse("{}", ev) == null, "Empty JSON");
            Check(LlamaCppDialogueService.ParseResponse("{\"choices\":[{\"message\":{\"content\":\"Quiero nomás, ahora bancate el canto.\"},\"finish_reason\":\"stop\"}]}", ev)
                == "Quiero nomás, ahora bancate el canto.", "UTF8 parser");
            Check(DialogueText.Filter("Tengo el siete de espada, mirá vos.", ev) == null, "Private card filter");
            Check(DialogueText.Filter("No quiero saber nada con ese canto.", ev) == null, "Contradictory response");
            Check(DialogueText.Filter("Te canto falta envido, preparate ahora.", ev) == null, "Invented call");
            Check(DialogueText.Filter("<think>pienso</think> Quiero nomás, vení tranquilo.", ev) == null, "Reasoning filter");
            foreach (var property in typeof(BotDialogueContext).GetProperties())
                Check(property.PropertyType.IsEnum || property.PropertyType == typeof(int), "Context allowlist: " + property.Name);
            var bluff = new BotDialogueContext(BotPersonality.Bluffer, 1, 2, EnvidoStrength.Weak, TrucoStrength.Weak, DialogueConfidence.Low, GameState.TurnoJugador);
            Check(!DialoguePrompt.User(bluff, ev, Array.Empty<string>()).Contains("Weak"), "Bluffer strength hidden");
            Log("PASS text: UTF8 content, invalid/empty JSON, private cards, wrong action, reasoning, context allowlist");
        }

        private static async Task ValidateMatchEnd()
        {
            var host = new GameObject("Dialogue bridge validation");
            var server = host.AddComponent<GameServer>();
            typeof(Unity.Netcode.NetworkBehaviour).GetProperty("IsServer").GetSetMethod(true).Invoke(server, new object[] { true });
            var deck = ScriptableObject.CreateInstance<DeckData>();
            server.masterDeck = deck;
            var model = new GameModel(deck) { CurrentState = GameState.TurnoJugador, Player1Id = 1, Player2Id = 2 };
            model.AddPoints(2, 29);
            var delayed = new DelayedModel();
            var sink = new Sink();
            var controller = new BotDialogueController(2, delayed, sink,
                new BotDialogueSettings { baseSpeakProbability = 1, cooldown = 0 }, null);
            var session = new TableSession { GameModel = model, Player2Id = 2, Player2Dialogue = controller };
            Task pending = controller.ObserveAsync(Context(), new BotDialogueEvent(BotDialogueEventType.BotAccepted, DialogueCall.Truco, DialogueDecision.Accept));
            object snapshot = typeof(GameServer).GetMethod("CaptureDialogue", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { session });
            model.AddPoints(2, 1);
            typeof(GameServer).GetMethod("AfterDialogueAction", Private).Invoke(server,
                new[] { session, snapshot, (object)(ulong)2, DialogueDecision.PlayedCard, DialogueCall.None });
            delayed.Completion.SetResult("Quiero nomás, ahora bancate el canto.");
            await pending;
            Check(sink.Count == 0 && model.CurrentState == GameState.PartidaTerminada && model.ScoreTeam2 == 30,
                "GameServer terminal bridge failed to invalidate or changed gameplay");
            controller.Dispose();
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(deck);
            Log("PASS GameServer bridge: match ends during pending request; stale text discarded, score unchanged");
        }

        private sealed class HttpStub : IDisposable
        {
            private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
            public int Delay;
            public int ProbeDelay;
            public string Body = "{}";
            public bool Compatible = true;
            public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
            public HttpStub() { listener.Start(); _ = Listen(); }
            private async Task Listen()
            {
                try
                {
                    while (!lifetime.IsCancellationRequested)
                    {
                        var client = await listener.AcceptTcpClientAsync();
                        _ = Respond(client);
                    }
                }
                catch (ObjectDisposedException) { }
                catch (SocketException) { }
            }
            private async Task Respond(TcpClient client)
            {
                using (client)
                {
                    try
                    {
                        var stream = client.GetStream();
                        var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
                        string first = await reader.ReadLineAsync();
                        string header;
                        int length = 0;
                        while (!string.IsNullOrEmpty(header = await reader.ReadLineAsync()))
                            if (header.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) int.TryParse(header.Substring(15).Trim(), out length);
                        // Responses do not depend on the body. Keep the socket open while UWR uploads it.
                        bool generation = first.Contains("chat/completions");
                        string body = generation ? Body : first.Contains("/health") ? "{\"status\":\"ok\"}"
                            : first.Contains("/props") ? "{\"model_path\":\"" + (Compatible ? "Qwen_Qwen3.5-0.8B-Q4_1.gguf" : "other.gguf") + "\"}"
                            : "{\"data\":[{\"id\":\"Qwen_Qwen3.5-0.8B-Q4_1.gguf\"}]}";
                        await Task.Delay(generation ? Delay : ProbeDelay, lifetime.Token);
                        byte[] bytes = Encoding.UTF8.GetBytes(body);
                        byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: " + bytes.Length + "\r\nConnection: close\r\n\r\n");
                        await stream.WriteAsync(response, 0, response.Length);
                        await stream.WriteAsync(bytes, 0, bytes.Length);
                    }
                    catch (IOException) { }
                    catch (OperationCanceledException) { }
                    catch (ObjectDisposedException) { }
                }
            }
            public void Dispose() { lifetime.Cancel(); listener.Stop(); }
        }

        private static async Task ValidateHttpFailure()
        {
            int warnings = 0;
            var settings = new BotDialogueSettings { port = 18086, requestTimeout = 1 };
            using (var stub = new HttpStub())
            {
                settings.port = stub.Port;
                using (var manager = new LocalLlmProcessManager(settings, "missing", message => warnings++))
                {
                    Check(await manager.StartAsync(), "Compatible HTTP stub reuse");
                    using (var service = new LlamaCppDialogueService(settings, manager, message => warnings++))
                    {
                        var ev = new BotDialogueEvent(BotDialogueEventType.HandWon);
                        stub.Body = "not json";
                        Check(await service.GenerateAsync(Context(), ev, Array.Empty<string>(), CancellationToken.None) == null, "Invalid HTTP JSON");
                        stub.Delay = 2500;
                        var watch = Stopwatch.StartNew();
                        Task<string> pending = service.GenerateAsync(Context(), ev, Array.Empty<string>(), CancellationToken.None);
                        Check(await service.GenerateAsync(Context(), ev, Array.Empty<string>(), CancellationToken.None) == null, "Global concurrency");
                        Check(await pending == null && watch.Elapsed.TotalSeconds < 2.4, "HTTP timeout bound");
                        stub.Dispose();
                        Check(await service.GenerateAsync(Context(), ev, Array.Empty<string>(), CancellationToken.None) == null, "Server down");
                    }
                }
            }
            using (var stub = new HttpStub { Compatible = false })
            {
                settings.port = stub.Port;
                using (var manager = new LocalLlmProcessManager(settings, "missing", message => warnings++))
                    Check(!await manager.StartAsync() && !manager.OwnedProcessId.HasValue, "Foreign server replaced");
            }
            using (var stub = new HttpStub { ProbeDelay = 3000 })
            {
                settings.port = stub.Port;
                settings.startupTimeout = 1;
                using (var manager = new LocalLlmProcessManager(settings, "missing", message => warnings++))
                {
                    var watch = Stopwatch.StartNew();
                    Check(!await manager.StartAsync() && watch.Elapsed.TotalSeconds < 2.4, "Startup timeout bound");
                }
                using (var manager = new LocalLlmProcessManager(settings, "missing", message => warnings++))
                {
                    Task<bool> start = manager.StartAsync();
                    manager.Dispose();
                    Check(!await start, "Dispose during startup");
                }
            }
            Log("PASS HTTP: invalid JSON, request/startup timeout, disposal during startup, server down, global concurrency, foreign port preservation");
        }

        private static async Task ValidateRuntime()
        {
            int warnings = 0;
            var missingSettings = new BotDialogueSettings { port = 18089, startupTimeout = 3 };
            using (var missing = new LocalLlmProcessManager(missingSettings, "Temp/DialogueValidation/Missing", message => warnings++))
            {
                Check(!await missing.StartAsync(), "Missing files must disable dialogue");
                Check(!await missing.StartAsync() && warnings == 1, "Only one missing-files warning");
            }
            Log("PASS missing runtime/model: disabled once without exception");
            string incomplete = Path.GetFullPath("Logs/DialogueValidation/IncompleteRuntime");
            Directory.CreateDirectory(Path.Combine(incomplete, "LLM", "Runtime"));
            Directory.CreateDirectory(Path.Combine(incomplete, "LLM", "Models"));
            File.Copy(Path.Combine(Application.streamingAssetsPath, "LLM", "Runtime", "llama-server.exe"),
                Path.Combine(incomplete, "LLM", "Runtime", "llama-server.exe"), true);
            File.WriteAllBytes(Path.Combine(incomplete, "LLM", "Models", missingSettings.modelFilename), Encoding.ASCII.GetBytes("GGUF"));
            using (var missingDlls = new LocalLlmProcessManager(missingSettings, incomplete, message => warnings++))
            {
                Check(!await missingDlls.StartAsync() && !missingDlls.OwnedProcessId.HasValue, "Missing DLL startup cleanup");
            }
            Log("PASS missing DLLs: failed startup cleaned up without fatal exception");
            var settings = new BotDialogueSettings { port = 18088, startupTimeout = 60, requestTimeout = 15 };
            int? pid;
            using (var manager = new LocalLlmProcessManager(settings, Application.streamingAssetsPath, Log))
            {
                Check(await manager.StartAsync(), "Real server startup/readiness");
                pid = manager.OwnedProcessId;
                Check(pid.HasValue, "Validation port already occupied; refusing ownership test on a foreign server");
                using (var reused = new LocalLlmProcessManager(settings, Application.streamingAssetsPath, Log))
                {
                    Check(await reused.StartAsync() && !reused.OwnedProcessId.HasValue, "Reuse compatible server");
                }
                Check(IsAlive(pid.Value), "Disposing borrowed server killed owner");
                using (var service = new LlamaCppDialogueService(settings, manager, Log))
                {
                    string phrase = null;
                    for (int attempt = 0; attempt < 3 && phrase == null; attempt++)
                    {
                        var ev = new BotDialogueEvent(BotDialogueEventType.BotAccepted, DialogueCall.Truco, DialogueDecision.Accept);
                        using (var request = new UnityWebRequest(manager.BaseUrl + "/v1/chat/completions", "POST"))
                        {
                            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(LlamaCppDialogueService.BuildRequestJson(
                                settings, manager.ModelId, Context(), ev, Array.Empty<string>())));
                            request.downloadHandler = new DownloadHandlerBuffer();
                            request.timeout = 15;
                            request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                            var operation = request.SendWebRequest();
                            while (!operation.isDone) await Task.Delay(50);
                            Log("REAL RESPONSE: " + request.downloadHandler.text);
                            phrase = LlamaCppDialogueService.ParseResponse(request.downloadHandler.text, ev);
                        }
                    }
                    Check(!string.IsNullOrWhiteSpace(phrase), "Real generation returned no acceptable phrase in 3 attempts");
                    Log("PASS real UTF8 UnityWebRequest phrase: " + phrase);
                    using (var cancelled = new CancellationTokenSource())
                    {
                        cancelled.Cancel();
                        Check(await service.GenerateAsync(Context(), new BotDialogueEvent(BotDialogueEventType.HandWon),
                            Array.Empty<string>(), cancelled.Token) == null, "Cancelled request");
                    }
                }
                Log("PASS readiness, shared server reuse, non-owner cleanup, request cancellation");
            }
            await Task.Delay(300);
            Check(!IsAlive(pid.Value), "Owned process orphaned");
            Log("PASS owned process shutdown, no orphan");
        }

        private static bool IsAlive(int pid)
        {
            try { using (var process = Process.GetProcessById(pid)) return !process.HasExited; }
            catch (ArgumentException) { return false; }
        }

        private static async void PlayState(PlayModeStateChange state)
        {
            try
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetInt(Stage, 0) == 1)
                {
                    var runtime = BotDialogueRuntime.Ensure(new BotDialogueSettings { port = 18087 });
                    var manager = (LocalLlmProcessManager)typeof(BotDialogueRuntime).GetField("process", Private).GetValue(runtime);
                    Check(await manager.StartAsync(), "Play Mode startup");
                    SessionState.SetInt(Pid, manager.OwnedProcessId.Value);
                    Scene previous = SceneManager.GetActiveScene();
                    var owner = new GameObject("Session owner during scene reload").AddComponent<GameServer>();
                    var delayed = new DelayedModel();
                    var sink = new Sink();
                    var sessionController = new BotDialogueController(77, delayed, sink,
                        new BotDialogueSettings { baseSpeakProbability = 1, cooldown = 0 }, null);
                    ((List<TableSession>)typeof(GameServer).GetField("sessions", Private).GetValue(owner)).Add(
                        new TableSession { Player2Dialogue = sessionController });
                    Task pending = sessionController.ObserveAsync(Context(), new BotDialogueEvent(BotDialogueEventType.HandWon));
                    Scene replacement = SceneManager.CreateScene("Dialogue validation replacement");
                    SceneManager.SetActiveScene(replacement);
                    var unload = SceneManager.UnloadSceneAsync(previous);
                    while (!unload.isDone) await Task.Delay(50);
                    delayed.Completion.SetResult("Te avisé que venía brava la mano.");
                    await pending;
                    Check(sink.Count == 0 && owner == null, "Scene unload retained old bot's output");
                    Check(runtime != null && manager.IsReady && IsAlive(SessionState.GetInt(Pid, 0)), "Scene transition lost shared runtime");
                    Check(BotDialogueRuntime.Ensure(new BotDialogueSettings()) == runtime, "Duplicate singleton after scene transition");
                    Log("PASS Play Mode start; scene unload/replacement retains one runtime/process");
                    SessionState.SetInt(Stage, 2);
                    EditorApplication.ExitPlaymode();
                }
                else if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetInt(Stage, 0) == 2)
                {
                    await Task.Delay(500);
                    Check(!IsAlive(SessionState.GetInt(Pid, 0)), "Play Mode stop left an orphan");
                    SessionState.EraseInt(Stage);
                    Log("PASS Play Mode stop cleanup\nALL TESTS PASSED");
                    if (Application.isBatchMode) EditorApplication.Exit(0);
                }
            }
            catch (Exception exception) { Fail(exception); }
        }

        private static void Fail(Exception exception)
        {
            SessionState.EraseInt(Stage);
            Log("FAILED: " + exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else Debug.LogException(exception);
        }

        private static List<BotCardObservation> Deck()
        {
            var result = new List<BotCardObservation>();
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
                foreach (int number in new[] { 1, 2, 3, 4, 5, 6, 7, 10, 11, 12 })
                {
                    int rank = number == 1 && suit == CardSuit.Espada ? 14 : number == 1 && suit == CardSuit.Basto ? 13
                        : number == 7 && suit == CardSuit.Espada ? 12 : number == 7 && suit == CardSuit.Oro ? 11
                        : number == 3 ? 10 : number == 2 ? 9 : number == 1 ? 8 : number == 12 ? 7 : number == 11 ? 6
                        : number == 10 ? 5 : number == 7 ? 4 : number == 6 ? 3 : number == 5 ? 2 : 1;
                    result.Add(new BotCardObservation(result.Count, suit, number, rank, number >= 10 ? 0 : number));
                }
            return result;
        }

        private static BotObservation Observation(List<BotCardObservation> deck, List<BotCardObservation> hand,
            int botScore, int opponentScore, EstadoEnvido state = EstadoEnvido.Ninguno, TipoCanto pending = TipoCanto.Ninguno)
        {
            return new BotObservation(2, pending == TipoCanto.Ninguno ? GameState.TurnoJugador : GameState.EsperandoRespuestaCanto,
                2, pending, 1, EnvidoRules.CanCall(state, EnvidoCall.Envido), EnvidoRules.CanCall(state, EnvidoCall.RealEnvido),
                EnvidoRules.CanCall(state, EnvidoCall.FaltaEnvido), false, opponentScore, botScore, 0,
                new ManoGanadaPor[3], state != EstadoEnvido.Ninguno, NivelTruco.Ninguno, hand,
                Array.Empty<BotCardObservation>(), Array.Empty<BotCardObservation>(), true, 1, 3,
                deck.Where(card => !hand.Contains(card)), state, 0, EnvidoRules.GetAcceptedPoints(state, opponentScore, botScore));
        }

        private static double Invoke(BotBrain brain, string method, params object[] args)
            => (double)typeof(BotBrain).GetMethod(method, Private).Invoke(brain, args);

        private static void ValidateTuning()
        {
            var deck = Deck();
            foreach (var personality in new[] { BotPersonality.Balanced, BotPersonality.Conservative, BotPersonality.Aggressive, BotPersonality.Risky, BotPersonality.Bluffer })
            {
                var profile = BotProfile.Create(BotDifficulty.Normal, personality);
                var brain = new BotBrain(profile, 123);
                foreach (var score in new[] { (bot: 0, rival: 0), (bot: 15, rival: 12), (bot: 29, rival: 20), (bot: 20, rival: 29) })
                {
                    int oldFalta = 0, newFalta = 0, envido = 0, real = 0;
                    var random = new System.Random(887);
                    double threshold = 0;
                    for (int sample = 0; sample < 400; sample++)
                    {
                        var hand = new List<BotCardObservation>();
                        while (hand.Count < 3) { var card = deck[random.Next(deck.Count)]; if (!hand.Contains(card)) hand.Add(card); }
                        var obs = Observation(deck, hand, score.bot, score.rival);
                        var evaluation = new BotEvaluator(obs, profile, new System.Random(sample));
                        int stake = EnvidoRules.GetFaltaEnvidoPoints(score.rival, score.bot);
                        threshold = Invoke(brain, "FaltaConfidenceThreshold", obs, stake);
                        double probability = Invoke(brain, "EnvidoDecisionProbability", evaluation.EnvidoWinProbability);
                        double originalFalta = 0.5 + 0.35 * Invoke(brain, "EvaluateProposedEnvido", obs, evaluation, probability, stake, 1, false);
                        double e = Invoke(brain, "EvaluateCallEnvido", obs, evaluation, EnvidoCall.Envido, false);
                        double r = Invoke(brain, "EvaluateCallEnvido", obs, evaluation, EnvidoCall.RealEnvido, false);
                        double f = Invoke(brain, "EvaluateCallEnvido", obs, evaluation, EnvidoCall.FaltaEnvido, false);
                        if (originalFalta > Math.Max(0.5, Math.Max(e, r))) oldFalta++;
                        if (f > Math.Max(0.5, Math.Max(e, r))) newFalta++;
                        else if (e > Math.Max(0.5, r)) envido++;
                        else if (r > 0.5) real++;
                        Check(f == originalFalta || double.IsNegativeInfinity(f), "Changed utility rather than Falta gate");
                    }
                    Check(newFalta <= oldFalta, "Falta frequency increased");
                    if (score.bot == 0 || score.bot == 15) Check(newFalta < oldFalta / 2, "Early/mid Falta insufficiently selective");
                    Log($"FALTA {personality} {score.bot}-{score.rival}: threshold={threshold:F3}; before={oldFalta}/400; after={newFalta}/400; E={envido}; Real={real}");
                }
            }
            var strong = new List<BotCardObservation> { deck[5], deck[6], deck[12] }; // 6+7 same suit = 33.
            var expert = new BotBrain(BotProfile.Create(BotDifficulty.Expert, BotPersonality.Balanced));
            foreach (EstadoEnvido state in Enum.GetValues(typeof(EstadoEnvido)))
            {
                if (state == EstadoEnvido.FaltaEnvido) continue;
                var obs = Observation(deck, strong, 29, 20, state, state == EstadoEnvido.Ninguno ? TipoCanto.Truco : TipoCanto.Envido);
                var evaluation = new BotEvaluator(obs, BotProfile.Create(BotDifficulty.Expert, BotPersonality.Balanced), new System.Random(1));
                Check(!double.IsNegativeInfinity(Invoke(expert, "EvaluateCallEnvido", obs, evaluation, EnvidoCall.FaltaEnvido, true)), "Strong closing Falta blocked");
                Check(expert.Decide(obs).Type != BotDecisionType.None, "No response during escalation/interrupted Truco");
            }
            Check(EnvidoRules.GetAcceptedPoints(EstadoEnvido.FaltaEnvido, 29, 20) == 1
                && EnvidoRules.GetRejectedPoints(7) == 7, "Falta can be less than previous bet");
            Log("PASS Falta: all escalation states, interrupted Truco, closing Falta, accepted 1 vs rejected 7");
        }
    }
}
