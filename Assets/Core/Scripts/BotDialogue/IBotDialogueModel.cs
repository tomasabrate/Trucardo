using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trucardo.Core.BotDialogue
{
    public interface IBotDialogueModel
    {
        Task<string> GenerateAsync(BotDialogueContext context, BotDialogueEvent dialogueEvent,
            IReadOnlyList<string> recentPhrases, CancellationToken cancellation);
    }
}
