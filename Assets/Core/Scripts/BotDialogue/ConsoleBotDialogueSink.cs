using UnityEngine;

namespace Trucardo.Core.BotDialogue
{
    public sealed class ConsoleBotDialogueSink : IBotDialogueSink
    {
        public void Publish(ulong botId, BotDialogueContext context, BotDialogueEvent dialogueEvent, string text)
        {
            Debug.Log($"[Bot Dialogue][{context.Personality}] {text}");
        }
    }
}
