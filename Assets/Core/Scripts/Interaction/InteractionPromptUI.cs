using TMPro;
using UnityEngine;

namespace Trucardo.Core.Interaction
{
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        private string lastBinding;
        private string lastPrompt;

        private void Awake() => Hide();

        public void Show(string binding, string prompt)
        {
            if (label == null) return;
            if (binding != lastBinding || prompt != lastPrompt)
            {
                label.text = string.IsNullOrEmpty(binding) ? prompt : $"[{binding}] {prompt}";
                lastBinding = binding;
                lastPrompt = prompt;
            }
            if (!label.gameObject.activeSelf) label.gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (label != null && label.gameObject.activeSelf) label.gameObject.SetActive(false);
        }

        private void OnDisable() => Hide();
    }
}
