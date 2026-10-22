using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

namespace Trucardo.Core.Interaction
{
    [DefaultExecutionOrder(-50)]
    public class InteractionController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private InteractionPromptUI promptUI;
        [SerializeField] private float maxDistance = 3f;
        [Tooltip("Include walls and other occluders as well as interactable objects.")]
        [SerializeField] private LayerMask raycastMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        private InputAction interact;
        private PlayerController player;
        private IInteractable target;
        private MonoBehaviour targetComponent;
        private Collider targetCollider;
        private int bindingIndex;
        private string bindingLabel = "";
        private bool enabledAction;

                        private void OnEnable()
        {
            if (inputActions == null) return;
            interact = inputActions.FindAction("Player/Interact", true);
            InputSystem.onActionChange += OnActionChange;
            RefreshBinding();
        }

        private void OnDisable()
        {
            if (enabledAction && interact != null) interact.Disable();
            InputSystem.onActionChange -= OnActionChange;
        }

        private void OnActionChange(object obj, InputActionChange change)
        {
            if (change == InputActionChange.BoundControlsChanged) RefreshBinding();
        }

        private void RefreshBinding()
        {
            if (interact == null) return;
            bindingIndex = interact.GetBindingIndex(InputBinding.MaskByGroup(interact.activeControl?.device?.name ?? "Keyboard"));
            bindingLabel = bindingIndex >= 0 ? interact.bindings[bindingIndex].ToDisplayString() : interact.bindings[0].ToDisplayString();
        }

        private void ReleaseInput()
        {
            if (enabledAction && interact != null)
            {
                interact.Disable();
                enabledAction = false;
            }
        }

                private void ClearTarget()
        {
            if (promptUI != null) promptUI.Hide();
            target = null;
            targetComponent = null;
            targetCollider = null;
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
            if (view == null) view = Camera.main;

            Transform raycastOrigin = view != null ? view.transform : player.transform.Find("Cabeza");
            if (raycastOrigin == null) raycastOrigin = player.transform; // Ultimate fallback

            target = null;
            targetComponent = null;
            targetCollider = null;

            if (raycastOrigin != null)
            {
                var hits = Physics.RaycastAll(raycastOrigin.position, raycastOrigin.forward, maxDistance, raycastMask, triggerInteraction);
                
                if (hits.Length == 0)
                {
                    hits = Physics.SphereCastAll(raycastOrigin.position, 0.05f, raycastOrigin.forward, maxDistance, raycastMask, triggerInteraction);
                }

                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in hits)
                {
                    if (hit.collider.transform.root == player.transform.root) continue;

                    var candidate = hit.collider.GetComponentInParent<IInteractable>();
                    var component = candidate as MonoBehaviour;
                    
                    if (component != null && component.isActiveAndEnabled && candidate.CanInteract(player.gameObject))
                    {
                        target = candidate;
                        targetComponent = component;
                        targetCollider = hit.collider;
                        break;
                    }
                }
            }

            if (interact.WasPerformedThisFrame())
            {
                if (interact.activeControl != null)
                {
                    bindingIndex = interact.GetBindingIndexForControl(interact.activeControl);
                    RefreshBinding();
                }

                if (target != null) target.Interact(player.gameObject);
                else player.HandleContextualInteract();
            }

            if (player == null || player.GameplayInputBlocked || targetComponent == null ||
                !targetComponent.isActiveAndEnabled || targetCollider == null || !targetCollider.enabled ||
                !targetCollider.gameObject.activeInHierarchy || !target.CanInteract(player.gameObject))
                ClearTarget();
            else if (promptUI != null)
            {
                string displayBinding = target is PowerVisual ? "" : bindingLabel;
                promptUI.Show(displayBinding, target.PromptText);
            }
        }
    }
}







