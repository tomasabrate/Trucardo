using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Trucardo.Core.BotDialogue
{
    internal static class DialogueHttp
    {
        // Invoked on Unity's main thread; continuations retain UnitySynchronizationContext.
        // A bounded 50ms timer, not a per-frame Update or busy loop. Abort also stays on main thread.
        internal static async Task SendAsync(UnityWebRequest request, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            request.redirectLimit = 0; // Local means local, including on an occupied port.
            var operation = request.SendWebRequest();
            try
            {
                while (!operation.isDone) await Task.Delay(50, cancellation);
                cancellation.ThrowIfCancellationRequested();
            }
            catch
            {
                request.Abort();
                throw;
            }
        }
    }
}
