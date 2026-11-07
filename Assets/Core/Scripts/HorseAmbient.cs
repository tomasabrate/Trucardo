using System.Collections;
using UnityEngine;

namespace Trucardo.Environment
{
    /// <summary>
    /// Comportamiento ambiental del caballo animado.
    /// Maneja animacion procedimental de huesos (cabeza, cuello, cola, respiracion)
    /// y reproduccion de sonidos criollos (relincho y soplido) con audio espacial 3D.
    /// </summary>
    public class HorseAmbient : MonoBehaviour
    {
        [Header("Huesos (Auto-detectados si estan vacios)")]
        [SerializeField] private string targetHorseName = "";
        [SerializeField] private Transform horseRoot;
        [SerializeField] private Transform rootBone;
        [SerializeField] private Transform spineBone;
        [SerializeField] private Transform neckBone;
        [SerializeField] private Transform headBone;
        [SerializeField] private Transform tail01Bone;
        [SerializeField] private Transform tail02Bone;

        [Header("Audio Ambiental")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip relinchoClip;
        [SerializeField] private AudioClip soplidoClip;
        [SerializeField] private float minIntervalSeconds = 12f;
        [SerializeField] private float maxIntervalSeconds = 28f;
        [SerializeField] private float audioVolume = 0.85f;

        [Header("Ajustes de Animacion Procedimental")]
        [SerializeField] private bool proceduralAnimation = true;
        [SerializeField] private float breathingSpeed = 1.6f;
        [SerializeField] private float headTurnSpeed = 0.45f;
        [SerializeField] private float tailSwishSpeed = 2.2f;

        // Rotaciones base en T-pose / bind pose
        private Quaternion baseRootRot;
        private Quaternion baseSpineRot;
        private Quaternion baseNeckRot;
        private Quaternion baseHeadRot;
        private Quaternion baseTail1Rot;
        private Quaternion baseTail2Rot;

        private float randomSeed;
        private float nextLookTargetYaw;
        private float currentLookYaw;
        private float lookChangeTimer;

        private void Awake()
        {
            randomSeed = Random.Range(0f, 1000f);
            FindBones();
            SetupAudio();
        }

        private void Start()
        {
            StartCoroutine(AmbientAudioRoutine());
        }

        private void FindBones()
        {
            if (horseRoot == null && !string.IsNullOrEmpty(targetHorseName))
            {
                var horseObj = GameObject.Find(targetHorseName);
                if (horseObj != null) horseRoot = horseObj.transform;
            }

            if (horseRoot == null)
            {
                var horseObj = GameObject.Find("Caballo_Animado_Blanco") ?? GameObject.Find("Caballo_Animado_Marron") ?? GameObject.Find("Caballo_Animado");
                if (horseObj != null) horseRoot = horseObj.transform;
            }

            Transform[] allChildren = horseRoot != null
                ? horseRoot.GetComponentsInChildren<Transform>()
                : GetComponentsInChildren<Transform>();

            foreach (var t in allChildren)
            {
                string n = t.name.ToLower();
                if (rootBone == null && n == "root") rootBone = t;
                else if (spineBone == null && n == "spine") spineBone = t;
                else if (neckBone == null && n == "neck") neckBone = t;
                else if (headBone == null && n == "head") headBone = t;
                else if (tail01Bone == null && (n == "tail_01" || n == "tail1")) tail01Bone = t;
                else if (tail02Bone == null && (n == "tail_02" || n == "tail2")) tail02Bone = t;
            }

            if (rootBone != null) baseRootRot = rootBone.localRotation;
            if (spineBone != null) baseSpineRot = spineBone.localRotation;
            if (neckBone != null) baseNeckRot = neckBone.localRotation;
            if (headBone != null) baseHeadRot = headBone.localRotation;
            if (tail01Bone != null) baseTail1Rot = tail01Bone.localRotation;
            if (tail02Bone != null) baseTail2Rot = tail02Bone.localRotation;
        }

        private void SetupAudio()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            // Configuracion 3D para inmersion en pulperia
            audioSource.spatialBlend = 1.0f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 3.0f;
            audioSource.maxDistance = 30.0f;
            audioSource.playOnAwake = false;
            audioSource.volume = audioVolume;

            // Cargar clips desde Resources si no estan asignados
            if (relinchoClip == null)
            {
                relinchoClip = Resources.Load<AudioClip>("Sonidos/Efectos/Caballo Relinchando");
            }
            if (soplidoClip == null)
            {
                soplidoClip = Resources.Load<AudioClip>("Sonidos/Efectos/Caballo Soplando");
            }
        }

        private void LateUpdate()
        {
            if (!proceduralAnimation) return;

            float t = Time.time + randomSeed;

            // 1. Respiracion suave en pecho / columna
            float breath = Mathf.Sin(t * breathingSpeed);
            if (spineBone != null)
            {
                spineBone.localRotation = baseSpineRot * Quaternion.Euler(breath * 1.0f, 0f, 0f);
            }

            // 2. Mirada y cabeceo natural
            lookChangeTimer -= Time.deltaTime;
            if (lookChangeTimer <= 0f)
            {
                lookChangeTimer = Random.Range(3.5f, 7.0f);
                nextLookTargetYaw = Random.Range(-12f, 12f);
            }
            currentLookYaw = Mathf.Lerp(currentLookYaw, nextLookTargetYaw, Time.deltaTime * headTurnSpeed * 3f);

            float headNod = Mathf.Sin(t * 1.1f) * 3.5f + Mathf.Cos(t * 0.7f) * 2.0f;
            float headTilt = Mathf.Sin(t * 0.5f) * 2.5f;

            if (neckBone != null)
            {
                neckBone.localRotation = baseNeckRot * Quaternion.Euler(headNod * 0.5f, currentLookYaw * 0.4f, headTilt * 0.5f);
            }

            if (headBone != null)
            {
                headBone.localRotation = baseHeadRot * Quaternion.Euler(headNod * 0.7f, currentLookYaw * 0.6f, headTilt * 0.7f);
            }

            // 3. Cola espantando moscas (movimiento tipico campestre)
            float tailWag = Mathf.Sin(t * tailSwishSpeed) * 14f;
            // Doble coletazo ocasional
            float burst = Mathf.Max(0f, Mathf.Sin(t * 0.35f) - 0.7f) * 3.3f;
            tailWag += Mathf.Sin(t * 7f) * burst * 20f;

            if (tail01Bone != null)
            {
                tail01Bone.localRotation = baseTail1Rot * Quaternion.Euler(burst * -6f, tailWag, 0f);
            }
            if (tail02Bone != null)
            {
                tail02Bone.localRotation = baseTail2Rot * Quaternion.Euler(burst * -10f, tailWag * 1.35f, 0f);
            }
        }

        private IEnumerator AmbientAudioRoutine()
        {
            // Espera inicial aleatoria para no sonar inmediatamente al arrancar escena
            yield return new WaitForSeconds(Random.Range(3f, 8f));

            while (true)
            {
                float interval = Random.Range(minIntervalSeconds, maxIntervalSeconds);
                yield return new WaitForSeconds(interval);

                if (audioSource != null && audioSource.isActiveAndEnabled)
                {
                    // 70% soplido / bufido tranquilo, 30% relincho
                    bool isSoplido = Random.value < 0.72f;
                    AudioClip clipToPlay = isSoplido ? soplidoClip : relinchoClip;

                    if (clipToPlay != null)
                    {
                        audioSource.pitch = Random.Range(0.94f, 1.06f);
                        audioSource.PlayOneShot(clipToPlay, audioVolume);
                    }
                }
            }
        }
    }
}
