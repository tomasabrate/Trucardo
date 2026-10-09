namespace Trucardo.Core.BotDialogue
{
    public interface IBotDialogueSink
    {
        void Publish(ulong botId, BotDialogueContext context, BotDialogueEvent dialogueEvent, string text);
    }
}
