using UnityEngine;

namespace Trucardo.Core
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource), typeof(PlayerController), typeof(CharacterController))]
    public sealed class PlayerFootstepsController : MonoBehaviour
    {
        [SerializeField] private AudioClip woodSteps;
        [SerializeField] private AudioClip grassSteps;
        [SerializeField] private Material[] woodSurfaceMaterials;
        [SerializeField] private Material[] grassSurfaceMaterials;
        [SerializeField, Range(0f, 1f)] private float maximumVolume = 0.35f;
        [SerializeField, Min(0f)] private float fadeDuration = 0.08f;

        private PlayerController player;
        private CharacterController character;
        private AudioSource stepsSource;
        private ExteriorAmbienceController interior;
        private Vector3 previousPosition;
        private bool hasPreviousPosition;
        private float blend;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            character = GetComponent<CharacterController>();
            stepsSource = GetComponent<AudioSource>();
            stepsSource.playOnAwake = false;
            stepsSource.loop = true;
            stepsSource.spatialBlend = 0f;
            stepsSource.pitch = 1f;
            stepsSource.volume = 0f;
        }

        private void LateUpdate()
        {
            Vector3 position = transform.position;
            Vector3 displacement = position - previousPosition;
            bool canWalk = player.IsSpawned && player.IsOwner && player.isActiveAndEnabled
                && !player.netIsSeated.Value && !player.GameplayInputBlocked
                && Time.timeScale > 0f && !AudioListener.pause
                && character.enabled && character.isGrounded;
            float delta = Time.deltaTime;
            float horizontalDistanceSquared = displacement.x * displacement.x + displacement.z * displacement.z;
            float teleportDistance = Mathf.Max(0.5f, player.walkSpeed * 1.414214f * delta * 3f);
            bool walking = canWalk && hasPreviousPosition && delta > 0f
                && horizontalDistanceSquared > 0.0025f * delta * delta
                && displacement.sqrMagnitude <= teleportDistance * teleportDistance;
            previousPosition = position;
            hasPreviousPosition = canWalk;

            if (interior == null)
                interior = FindAnyObjectByType<ExteriorAmbienceController>();

            AudioClip desiredClip = null;
            if (walking)
            {
                if (interior != null && interior.IsOnAdditionalWoodFloor(position))
                    desiredClip = woodSteps;
                else if (TryGetGroundClip(position, out AudioClip groundClip))
                    desiredClip = groundClip;
                else if (interior != null && interior.TryGetInteriorState(position, out bool isInside))
                    desiredClip = isInside ? woodSteps : grassSteps;
            }

            // Finish fading the old surface before starting the new one.
            if (!stepsSource.isPlaying && desiredClip != null)
            {
                stepsSource.clip = desiredClip;
                blend = 0f;
                stepsSource.volume = 0f;
                stepsSource.Play();
            }

            float targetBlend = desiredClip != null && desiredClip == stepsSource.clip ? 1f : 0f;
            blend = fadeDuration <= 0f ? targetBlend
                : Mathf.MoveTowards(blend, targetBlend, Time.unscaledDeltaTime / fadeDuration);
            stepsSource.volume = blend * Mathf.Clamp01(maximumVolume);
            if (blend == 0f && stepsSource.isPlaying)
                stepsSource.Stop();
        }

        private bool TryGetGroundClip(Vector3 position, out AudioClip clip)
        {
            clip = null;
            Vector3 origin = new Vector3(position.x, character.bounds.min.y + 0.25f, position.z);
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1f, ~0, QueryTriggerInteraction.Ignore)
                || hit.normal.y < 0.5f || hit.collider.GetComponentInParent<PlayerController>() != null)
                return false;

            Renderer surface = hit.collider.GetComponent<Renderer>();
            if (surface == null) surface = hit.collider.GetComponentInParent<Renderer>();
            if (surface == null) return false;

            foreach (Material material in surface.sharedMaterials)
            {
                if (ContainsMaterial(woodSurfaceMaterials, material))
                {
                    clip = woodSteps;
                    return true;
                }
                if (ContainsMaterial(grassSurfaceMaterials, material))
                {
                    clip = grassSteps;
                    return true;
                }
            }
            return false;
        }

        private static bool ContainsMaterial(Material[] materials, Material candidate)
        {
            if (materials == null || candidate == null) return false;
            foreach (Material material in materials)
                if (material == candidate) return true;
            return false;
        }

        private void OnDisable()
        {
            if (stepsSource != null)
            {
                stepsSource.Stop();
                stepsSource.volume = 0f;
            }
            blend = 0f;
            hasPreviousPosition = false;
        }
    }
}
