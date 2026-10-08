using UnityEngine;

namespace Trucardo.Core.Interaction
{
    public interface IInteractable
    {
        bool CanInteract(GameObject interactor);
        string PromptText { get; }
        void Interact(GameObject interactor);
    }
}
