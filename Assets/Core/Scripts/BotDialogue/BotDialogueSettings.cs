using System;
using UnityEngine;

namespace Trucardo.Core.BotDialogue
{
    [Serializable]
    public sealed class BotDialogueSettings
    {
        public bool enabled = true;
        [Range(1024, 65535)] public int port = 8080;
        public string modelFilename = "Qwen_Qwen3.5-0.8B-Q4_1.gguf";
        [Min(1)] public int startupTimeout = 60;
        [Min(1)] public int requestTimeout = 8;
        [Range(16, 64)] public int maxTokens = 48;
        [Range(0, 2)] public float temperature = 0.9f;
        [Min(0)] public float cooldown = 3.5f;
        [Range(0, 1)] public float baseSpeakProbability = 0.85f;
        public bool logDebug;
        [Range(512, 4096)] public int contextSize = 2048;
        [Range(1, 8)] public int threads = 4;
        [Min(1)] public float eventLifetime = 8;

        internal void Validate()
        {
            port = Mathf.Clamp(port, 1024, 65535);
            startupTimeout = Mathf.Clamp(startupTimeout, 1, 180);
            requestTimeout = Mathf.Clamp(requestTimeout, 1, 60);
            maxTokens = Mathf.Clamp(maxTokens, 16, 64);
            temperature = Mathf.Clamp(temperature, 0, 2);
            contextSize = Mathf.Clamp(contextSize, 512, 4096);
            threads = Mathf.Clamp(threads, 1, 8);
            cooldown = Mathf.Max(0, cooldown);
            eventLifetime = Mathf.Clamp(eventLifetime, 1, 30);
            baseSpeakProbability = Mathf.Clamp01(baseSpeakProbability);
        }
    }
}
