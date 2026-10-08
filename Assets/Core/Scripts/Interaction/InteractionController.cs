using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Trucardo.Core.Interaction
{
    // Runs after the player's look/movement, so the ray uses the rendered viewpoint.
    [DefaultExecutionOrder(100)]
    public sealed class InteractionController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private InteractionPromptUI promptUI;
        [SerializeField, Min(0.01f)] private float maxDistance = 3f;
        [Tooltip("Include walls and other occluders as well as interactable objects.")]
        [SerializeField] private LayerMask raycastMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        private InputAction interact;
        private bool enabledAction;
        private PlayerController player;
        private IInteractable target;
        private MonoBehaviour targetComponent;
        private Collider targetCollider;
        private string bindingLabel;
        private int bindingIndex = 0;

        private void OnEnable()
        {
            if (inputActions == null) return;
            interact = inputActions.FindAction("Player/Interact", true);
            InputSystem.onActionChange += OnActionChange;
            RefreshBinding();
        }

        private void OnActionChange(object changed, InputActionChange change)
        {
            if (change == InputActionChange.BoundControlsChanged) RefreshBinding();
        }

        private void RefreshBinding()
        {
            if (interact == null) return;
            bindingLabel = bindingIndex >= 0 && bindingIndex < interact.bindings.Count
                ? interact.GetBindingDisplayString(bindingIndex) : interact.GetBindingDisplayString();
        }

        private void Update()
        {
            var network = NetworkManager.Singleton;
            var localObject = network != null && network.IsClient && network.LocalClient != null
                ? network.LocalClient.PlayerObject : null;
            if (localObject == null)
            {
                player = null;
                ReleaseInput();
                ClearTarget();
                return;
            }
            if (player == null || player.gameObject != localObject.gameObject)
                player = localObject.GetComponent<PlayerController>();

            if (player == null || !player.IsOwner || !player.isActiveAndEnabled || interact == null)
            {
                ReleaseInput();
                ClearTarget();
                return;
            }
            if (!interact.enabled)
            {
                interact.Enable();
                enabledAction = true;
            }
            if (player.GameplayInputBlocked || Time.timeScale <= 0f)
            {
                ClearTarget();
                return;
            }

            Camera view = player.InteractionCamera;
            target = null;
            targetComponent = null;
            targetCollider = null;
            if (view != null && view.isActiveAndEnabled && Physics.Raycast(view.transform.position,
                view.transform.forward, out RaycastHit hit, maxDistance, raycastMask, triggerInteraction))
            {
                var candidate = hit.collider.GetComponentInParent<IInteractable>();
                var component = candidate as MonoBehaviour;
                if (component != null && component.isActiveAndEnabled && candidate.CanInteract(player.gameObject))
                {
                    target = candidate;
                    targetComponent = component;
                    targetCollider = hit.collider;
                }
            }

            if (interact.WasPerformedThisFrame())
            {
                if (interact.activeControl != null)
                {
                    bindingIndex = interact.GetBindingIndexForControl(interact.activeControl);
                    RefreshBinding();
                }
                // A valid physical receiver consumes the press even if its action disables/destroys it.
                if (target != null) target.Interact(player.gameObject);
                else player.HandleContextualInteract();
            }

            if (player == null || player.GameplayInputBlocked || targetComponent == null ||
                !targetComponent.isActiveAndEnabled || targetCollider == null || !targetCollider.enabled ||
                !targetCollider.gameObject.activeInHierarchy || !target.CanInteract(player.gameObject))
                ClearTarget();
            else if (promptUI != null) promptUI.Show(bindingLabel, target.PromptText);
        }

        private void ClearTarget()
        {
            target = null;
            targetComponent = null;
            targetCollider = null;
            if (promptUI != null) promptUI.Hide();
        }

        private void ReleaseInput()
        {
            if (enabledAction && interact != null) interact.Disable();
            enabledAction = false;
        }

        private void OnDisable()
        {
            InputSystem.onActionChange -= OnActionChange;
            ReleaseInput();
            ClearTarget();
        }
    }
}
