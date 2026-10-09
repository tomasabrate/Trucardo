using System.Collections.Generic;
using UnityEngine;

namespace Trucardo.Core.BotDialogue
{
    // Created lazily by authoritative GameServer when its first logical bot is registered.
    // Survives scene reloads; per-session controllers are released by GameServer.
    public sealed class BotDialogueRuntime : MonoBehaviour
    {
        private static BotDialogueRuntime instance;
        private readonly HashSet<BotDialogueController> controllers = new HashSet<BotDialogueController>();
        private LocalLlmProcessManager process;
        private LlamaCppDialogueService model;
        private IBotDialogueSink sink = new ConsoleBotDialogueSink();
        private BotDialogueSettings settings;
        private bool warned;

        public static BotDialogueRuntime Ensure(BotDialogueSettings configuration)
        {
            if (instance != null) return instance;
            if (!configuration.enabled) return null;
            configuration.Validate();
            var host = new GameObject("Local Bot Dialogue");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<BotDialogueRuntime>();
            instance.settings = configuration;
            instance.process = new LocalLlmProcessManager(configuration, Application.streamingAssetsPath, instance.WarnOnce);
            instance.model = new LlamaCppDialogueService(configuration, instance.process, instance.WarnOnce);
            _ = instance.StartLocalModelAsync();
            return instance;
        }

        private async System.Threading.Tasks.Task StartLocalModelAsync()
        {
            bool ready = await process.StartAsync();
            if (this != null && ready && settings.logDebug)
                Debug.Log("[Bot Dialogue] Modelo local listo en " + process.BaseUrl);
        }

        public BotDialogueController Register(ulong botId)
        {
            var controller = new BotDialogueController(botId, model, sink, settings, WarnOnce);
            controllers.Add(controller);
            return controller;
        }

        public void Release(BotDialogueController controller)
        {
            if (controller == null) return;
            controllers.Remove(controller);
            controller.Dispose();
        }

        public void SetSink(IBotDialogueSink replacement)
        {
            sink = replacement ?? throw new System.ArgumentNullException(nameof(replacement));
            foreach (var controller in controllers) controller.SetSink(sink);
        }

        private void WarnOnce(string message)
        {
            if (warned) return;
            warned = true;
            Debug.LogWarning("[Bot Dialogue] " + message + " El gameplay continúa sin depender del diálogo.");
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
        }
#if UNITY_EDITOR
        private void BeforeAssemblyReload()
        {
            Shutdown();
            DestroyImmediate(gameObject);
        }

        private void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode) Shutdown();
        }
#endif
        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
#endif
            Shutdown();
        }
        private void OnApplicationQuit() => Shutdown();
        private void OnDestroy() => Shutdown();

        private void Shutdown()
        {
            foreach (var controller in controllers) controller.Dispose();
            controllers.Clear();
            model?.Dispose();
            process?.Dispose();
            if (instance == this) instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            if (instance != null) instance.Shutdown();
            instance = null;
        }
    }
}
