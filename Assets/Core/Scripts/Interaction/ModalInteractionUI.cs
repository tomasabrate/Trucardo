using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Trucardo.Core.Interaction
{
    // A reusable UnityEvent action. It has no knowledge of the object that opened it.
    [RequireComponent(typeof(CanvasGroup))]
    [DefaultExecutionOrder(-1000)]
    public sealed class ModalInteractionUI : MonoBehaviour
    {
        [Tooltip("Input handlers to suspend while open, e.g. the pause component.")]
        [SerializeField] private Behaviour[] suspendWhileOpen;
        [Tooltip("Audio sources to mute only while this modal is open.")]
        [SerializeField] private AudioSource[] muteWhileOpen = System.Array.Empty<AudioSource>();
        private readonly Dictionary<AudioSource, bool> previousMuteStates = new Dictionary<AudioSource, bool>();
        private CanvasGroup group;
        private PlayerController player;
        private bool previousBlock;
        private CursorLockMode previousLock;
        private bool previousCursorVisible;
        private bool[] previousEnabled;
        private bool open;
        private bool restorePending;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            previousEnabled = new bool[suspendWhileOpen == null ? 0 : suspendWhileOpen.Length];
            SetVisible(false);
        }

        public void Open(GameObject interactor)
        {
            if (open || restorePending || interactor == null || Time.timeScale <= 0f) return;
            var owner = interactor.GetComponent<PlayerController>();
            if (owner == null || !owner.IsOwner || owner.GameplayInputBlocked) return;
            gameObject.SetActive(true);
            if (!isActiveAndEnabled) return;
            player = owner;
            previousBlock = player.ModalInputBlocked;
            previousLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            for (int i = 0; i < previousEnabled.Length; i++)
            {
                var behaviour = suspendWhileOpen[i];
                previousEnabled[i] = behaviour != null && behaviour.enabled;
                if (behaviour != null) behaviour.enabled = false;
            }
            open = true;
            if (muteWhileOpen != null)
            {
                foreach (AudioSource source in muteWhileOpen)
                {
                    if (source == null || previousMuteStates.ContainsKey(source)) continue;
                    previousMuteStates.Add(source, source.mute);
                    source.mute = true;
                }
            }
            player.ModalInputBlocked = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SetVisible(true);
        }

        private void Update()
        {
            if (open && (player == null || !player.IsSpawned || !player.IsOwner ||
                (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame))) Close();
        }

        public void Close()
        {
            if (!open) return;
            open = false;
            foreach (var state in previousMuteStates)
                if (state.Key != null) state.Key.mute = state.Value;
            previousMuteStates.Clear();
            SetVisible(false);
            if (player != null)
            {
                player.ModalInputBlocked = previousBlock;
                player.SuppressGameplayInputThisFrame();
            }
            player = null;
            Cursor.lockState = previousLock;
            Cursor.visible = previousCursorVisible;
            // Restore in LateUpdate so the closing Escape cannot also open pause.
            restorePending = true;
        }

        private void LateUpdate() => RestoreHandlers();

        private void RestoreHandlers()
        {
            if (!restorePending) return;
            restorePending = false;
            for (int i = 0; i < previousEnabled.Length; i++)
                if (suspendWhileOpen[i] != null) suspendWhileOpen[i].enabled = previousEnabled[i];
        }

        private void SetVisible(bool visible)
        {
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            // Keep the hierarchy active: existing page-turn coroutines can finish while hidden.
        }

        private void OnDisable()
        {
            Close();
            RestoreHandlers();
        }
    }
}
