using Unity.Netcode;
using UnityEngine;

namespace Trucardo.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class ExteriorAmbienceController : MonoBehaviour
    {
        [Header("Interior del bar")]
        [SerializeField] private Transform barSpace;
        [SerializeField] private Vector2 interiorCenter = new Vector2(0.0033f, 3.0884f);
        [SerializeField] private Vector2 interiorSize = new Vector2(20.508953f, 18.150248f);

        [Header("Pisos de madera adicionales")]
        [SerializeField] private Renderer[] additionalWoodFloors;

        [Header("Ambiente exterior")]
        [SerializeField, Range(0f, 1f)] private float maximumVolume = 0.35f;
        [SerializeField, Min(0f)] private float transitionDuration = 1f;

        private AudioSource ambienceSource;
        private float exteriorBlend;

        private void Awake()
        {
            ambienceSource = GetComponent<AudioSource>();
            ambienceSource.playOnAwake = false;
            ambienceSource.loop = true;
            ambienceSource.spatialBlend = 0f;
            ambienceSource.volume = 0f;
        }

        private void Update()
        {
            float targetBlend = 0f;
            NetworkManager manager = NetworkManager.Singleton;
            NetworkObject localPlayer = manager != null && manager.IsClient
                ? manager.LocalClient?.PlayerObject
                : null;

            // Only this client's player controls its ambience, including on the host.
            if (localPlayer != null && localPlayer.IsSpawned
                && TryGetInteriorState(localPlayer.transform.position, out bool isInside))
            {
                targetBlend = isInside ? 0f : 1f;

                // Keep the loop running silently inside; crossing never restarts it.
                if (ambienceSource.clip != null && !ambienceSource.isPlaying && !AudioListener.pause)
                    ambienceSource.Play();
            }

            exteriorBlend = transitionDuration <= 0f
                ? targetBlend
                : Mathf.MoveTowards(exteriorBlend, targetBlend, Time.deltaTime / transitionDuration);
            ambienceSource.volume = exteriorBlend * Mathf.Clamp01(maximumVolume);
        }

        public bool TryGetInteriorState(Vector3 worldPosition, out bool isInside)
        {
            isInside = false;
            if (barSpace == null) return false;

            Vector3 position = barSpace.InverseTransformPoint(worldPosition);
            Vector2 halfSize = interiorSize * 0.5f;
            isInside = Mathf.Abs(position.x - interiorCenter.x) <= halfSize.x
                && Mathf.Abs(position.z - interiorCenter.y) <= halfSize.y;
            return true;
        }

        public bool IsOnAdditionalWoodFloor(Vector3 worldPosition)
        {
            if (additionalWoodFloors == null) return false;
            foreach (Renderer floor in additionalWoodFloors)
            {
                if (floor == null || !floor.enabled || !floor.gameObject.activeInHierarchy) continue;
                Bounds bounds = floor.bounds;
                // Visible decking may have no usable collider. Classify only its footprint
                // near ground level, leaving the interior ambience zone unchanged.
                if (worldPosition.x >= bounds.min.x && worldPosition.x <= bounds.max.x
                    && worldPosition.z >= bounds.min.z && worldPosition.z <= bounds.max.z
                    && Mathf.Abs(worldPosition.y - bounds.max.y) <= 0.3f)
                    return true;
            }
            return false;
        }

        private void OnDisable()
        {
            if (ambienceSource != null)
            {
                ambienceSource.Stop();
                ambienceSource.volume = 0f;
            }
            exteriorBlend = 0f;
        }
    }
}
