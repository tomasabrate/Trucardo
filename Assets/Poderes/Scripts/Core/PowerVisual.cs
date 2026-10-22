using UnityEngine;
using Trucardo.Core.Interaction;

namespace Trucardo.Core
{
    /// <summary>
    /// Representación 3D interactuable de un poder sobre la mesa.
    /// </summary>
    public class PowerVisual : MonoBehaviour, IInteractable
    {
        public PowerData Data { get; private set; }
        public bool isOpponentPower = false;

        public string PromptText => Data != null ? Data.DisplayName : "Poder";

        public bool CanInteract(GameObject interactor) => Data != null && isActiveAndEnabled;

        public void Interact(GameObject interactor)
        {
            if (isOpponentPower || interactor == null) return;
            PlayerController player = interactor.GetComponent<PlayerController>();
            if (player != null)
            {
                player.OnPowerClicked(this);
            }
        }

        public void SetPowerData(PowerData data)
        {
            Data = data;
            string prefix = isOpponentPower ? "OpponentPower" : "Power";
            gameObject.name = data != null ? $"{prefix}_{data.PowerId}" : $"{prefix}_Empty";

            if (GetComponentInChildren<Collider>() == null)
            {
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(1.2f, 1.2f, 1.2f);
            }
        }

        public void AnimateTo(Vector3 targetPosition, Quaternion targetRotation, float duration = 0.4f)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateCoroutine(targetPosition, targetRotation, duration));
        }

        private System.Collections.IEnumerator AnimateCoroutine(
            Vector3 targetPosition,
            Quaternion targetRotation,
            float duration)
        {
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
                yield return null;
            }

            transform.SetPositionAndRotation(targetPosition, targetRotation);
        }
    }
}
