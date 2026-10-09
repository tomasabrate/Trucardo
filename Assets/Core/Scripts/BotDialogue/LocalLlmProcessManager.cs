using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Trucardo.Core.BotDialogue
{
    public sealed class LocalLlmProcessManager : IDisposable
    {
        [Serializable] private sealed class Models { public Model[] data; }
        [Serializable] private sealed class Model { public string id; }
        [Serializable] private sealed class Properties { public string model_path; }
        private readonly BotDialogueSettings settings;
        private readonly string assetsPath;
        private readonly Action<string> warnOnce;
        private readonly object processLock = new object();
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private Process owned;
        private OwnedProcessJob job;
        private UnityWebRequest activeRequest;
        private Task<bool> startup;
        private bool disposed;
        public bool IsReady { get; private set; }
        public string ModelId { get; private set; }
        public string BaseUrl => "http://127.0.0.1:" + settings.port;
        public int? OwnedProcessId { get { lock (processLock) return owned?.Id; } }

        public LocalLlmProcessManager(BotDialogueSettings settings, string streamingAssetsPath, Action<string> warnOnce)
        { this.settings = settings; assetsPath = streamingAssetsPath; this.warnOnce = warnOnce; }

        public Task<bool> StartAsync() => startup ?? (startup = StartCoreAsync());

        private async Task<bool> StartCoreAsync()
        {
            if (disposed || !settings.enabled) return false;
#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
            warnOnce?.Invoke("El runtime local incluido requiere Windows.");
            return false;
#else
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token))
            {
                deadline.CancelAfter(TimeSpan.FromSeconds(settings.startupTimeout));
                try
                {
                    if (string.IsNullOrWhiteSpace(settings.modelFilename)
                        || Path.GetFileName(settings.modelFilename) != settings.modelFilename
                        || settings.modelFilename.IndexOfAny(new[] { '"', '\r', '\n' }) >= 0)
                        throw new InvalidOperationException("Nombre de modelo inválido.");
                    var probe = await GetAsync("/health", deadline.Token);
                    if (probe.status > 0)
                    {
                        // A foreign HTTP process owns the port. Never replace it or terminate it.
                        if (probe.status != 200 || !await CompatibleAsync(deadline.Token))
                            throw new InvalidOperationException("Puerto ocupado por un servidor no compatible o todavía no listo.");
                        IsReady = true;
                        return true;
                    }
                    string executable = Path.Combine(assetsPath, "LLM", "Runtime", "llama-server.exe");
                    string model = Path.Combine(assetsPath, "LLM", "Models", settings.modelFilename);
                    if (!HasMagic(executable, "MZ") || !HasMagic(model, "GGUF"))
                        throw new FileNotFoundException("Falta llama-server.exe o el GGUF real (posible puntero Git LFS) en StreamingAssets/LLM.");
                    string arguments = "--model \"" + model + "\" --alias \"" + settings.modelFilename
                        + "\" --host 127.0.0.1 --port " + settings.port + " --ctx-size " + settings.contextSize
                        + " --reasoning off --reasoning-budget 0 --no-webui --offline --parallel 1 --threads "
                        + settings.threads + " --threads-batch " + settings.threads + " --poll 0 --log-disable";
                    await Task.Run(() => Launch(executable, arguments), deadline.Token);
                    while (true)
                    {
                        deadline.Token.ThrowIfCancellationRequested();
                        lock (processLock)
                            if (owned == null || owned.HasExited)
                                throw new InvalidOperationException("llama-server terminó al iniciar. Comprobá las DLL de Runtime.");
                        probe = await GetAsync("/health", deadline.Token);
                        if (probe.status == 200 && await CompatibleAsync(deadline.Token))
                        {
                            IsReady = true;
                            return true;
                        }
                        await Task.Delay(250, deadline.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    if (!disposed) warnOnce?.Invoke("Tiempo de inicio de llama-server agotado.");
                }
                catch (Exception exception) { if (!disposed) warnOnce?.Invoke(exception.Message); }
                StopOwned();
                return false;
            }
#endif
        }

        private void Launch(string executable, string arguments)
        {
            // Start can touch DLLs/disk: run off the main thread. Disposal also covers a start/stop race.
            var info = new ProcessStartInfo(executable, arguments)
            {
                WorkingDirectory = Path.GetDirectoryName(executable), UseShellExecute = false,
                CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            // Ignore inherited llama configuration (URLs, tools, remote RPC, model overrides).
            foreach (string key in new System.Collections.Generic.List<string>(info.Environment.Keys))
                if (key.StartsWith("LLAMA_", StringComparison.OrdinalIgnoreCase)) info.Environment.Remove(key);
            Process child = Process.Start(info);
            if (child == null) throw new InvalidOperationException("No se pudo iniciar llama-server.");
            try
            {
                child.OutputDataReceived += Drain;
                child.ErrorDataReceived += Drain;
                child.BeginOutputReadLine();
                child.BeginErrorReadLine();
                lock (processLock)
                {
                    if (disposed || lifetime.IsCancellationRequested)
                        throw new OperationCanceledException();
                    job = new OwnedProcessJob(child.Handle);
                    owned = child;
                }
            }
            catch
            {
                if (!child.HasExited) child.Kill();
                child.Dispose();
                throw;
            }
        }

        private static void Drain(object sender, DataReceivedEventArgs args) { }

        private static bool HasMagic(string path, string magic)
        {
            if (!File.Exists(path)) return false;
            using (var stream = File.OpenRead(path))
                foreach (char character in magic) if (stream.ReadByte() != character) return false;
            return true;
        }

        private async Task<bool> CompatibleAsync(CancellationToken cancellation)
        {
            var properties = await GetAsync("/props", cancellation);
            var models = await GetAsync("/v1/models", cancellation);
            if (properties.status != 200 || models.status != 200) return false;
            try
            {
                var props = JsonUtility.FromJson<Properties>(properties.body);
                var list = JsonUtility.FromJson<Models>(models.body);
                if (props == null || string.IsNullOrEmpty(props.model_path)
                    || !string.Equals(Path.GetFileName(props.model_path), settings.modelFilename, StringComparison.OrdinalIgnoreCase)
                    || list?.data == null || list.data.Length != 1 || string.IsNullOrEmpty(list.data[0]?.id)) return false;
                ModelId = list.data[0].id;
                return true;
            }
            catch (ArgumentException) { return false; }
        }

        private async Task<(long status, string body)> GetAsync(string route, CancellationToken cancellation)
        {
            using (var request = UnityWebRequest.Get(BaseUrl + route))
            {
                activeRequest = request;
                request.timeout = 2;
                try
                {
                    await DialogueHttp.SendAsync(request, cancellation);
                    return (request.responseCode, request.downloadHandler.text);
                }
                finally { activeRequest = null; }
            }
        }

        private void StopOwned()
        {
            lock (processLock)
            {
                job?.Dispose();
                job = null;
                if (owned == null) return;
                try { if (!owned.HasExited) owned.Kill(); }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception) { }
                finally { owned.Dispose(); owned = null; }
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            IsReady = false;
            lifetime.Cancel();
            activeRequest?.Abort();
            StopOwned(); // Kill only our retained Process/Job; never a name/PID lookup or WaitForExit.
        }
    }
}
