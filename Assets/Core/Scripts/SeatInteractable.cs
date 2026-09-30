using UnityEngine;

namespace Trucardo.Core
{
    public class SeatInteractable : MonoBehaviour
    {
        public float GetTopHeight()
        {
            float topHeight = float.NegativeInfinity;
            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            foreach (Renderer objectRenderer in renderers)
            {
                if (objectRenderer.enabled)
                    topHeight = Mathf.Max(topHeight, objectRenderer.bounds.max.y);
            }

            if (!float.IsNegativeInfinity(topHeight))
                return topHeight;

            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider objectCollider in colliders)
            {
                if (objectCollider.enabled && !objectCollider.isTrigger)
                    topHeight = Mathf.Max(topHeight, objectCollider.bounds.max.y);
            }

            return float.IsNegativeInfinity(topHeight) ? transform.position.y : topHeight;
        }
    }
}