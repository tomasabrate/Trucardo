using UnityEngine;
using UnityEngine.Events;

namespace Trucardo.Core.Interaction
{
    [DisallowMultipleComponent]
    public class InteractableObject : MonoBehaviour, IInteractable
    {
        [SerializeField] private string promptText = "Interactuar";
        [SerializeField] private bool available = true;
        [Tooltip("Dynamic GameObject receives the local player. Configure any action here.")]
        [SerializeField] private UnityEvent<GameObject> onInteract = new UnityEvent<GameObject>();

        public string PromptText => promptText;
        public bool CanInteract(GameObject interactor) => isActiveAndEnabled && available;
        public void SetAvailable(bool value) => available = value;

                        public void Interact(GameObject interactor)
        {
            if (CanInteract(interactor)) onInteract.Invoke(interactor);
        }
    }
}


