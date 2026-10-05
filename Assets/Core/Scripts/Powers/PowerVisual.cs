using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Representación 3D interactuable de un poder sobre la mesa.
    /// </summary>
    public class PowerVisual : MonoBehaviour
    {
        public PowerData Data { get; private set; }

        public void SetPowerData(PowerData data)
        {
            Data = data;
            gameObject.name = data != null ? $"Power_{data.PowerId}" : "Power_Empty";

            if (GetComponentInChildren<Collider>() == null)
            {
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(0.6f, 0.12f, 0.9f);
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
