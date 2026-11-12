using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Posiciona los brazos y manos del Gaucho Músico sobre la guitarra (mediante IK analítico de 2 huesos)
    /// y anima el rasgueo (mano derecha) y el cambio de acordes en el mástil (mano izquierda).
    /// Si 'animateHands' está desactivado, mantiene las manos fijas sosteniendo la guitarra.
    /// </summary>
    [ExecuteAlways]
    public class GauchoGuitarPlayer : MonoBehaviour
    {
        [Header("Modo de Manos")]
        [Tooltip("Si está activo, anima las manos rasgueando y cambiando acordes. Si se desactiva, mantiene las manos fijas sobre la guitarra.")]
        public bool animateHands = true;

        [Header("Parámetros de Rasgueo (Mano Derecha)")]
        [Range(0.5f, 4f)]
        public float strumSpeed = 2.1f;
        [Range(0f, 0.08f)]
        public float strumAmplitude = 0.036f;
        [Range(0f, 35f)]
        public float wristStrumAngle = 18f;

        [Header("Parámetros de Mástil (Mano Izquierda)")]
        [Range(0.2f, 2f)]
        public float chordChangeSpeed = 0.55f;
        [Range(0f, 0.10f)]
        public float fretSlideAmplitude = 0.045f;
        [Range(0f, 20f)]
        public float leftWristFretAngle = 10f;

        [Header("Referencias (Auto-detectadas)")]
        [SerializeField] private Animator targetAnimator;
        [SerializeField] private Transform guitarTransform;
        [SerializeField] private Transform spine1Bone;
        [SerializeField] private Transform leftShoulder;
        [SerializeField] private Transform leftUpperArm;
        [SerializeField] private Transform leftForeArm;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightShoulder;
        [SerializeField] private Transform rightUpperArm;
        [SerializeField] private Transform rightForeArm;
        [SerializeField] private Transform rightHand;

        private void OnEnable()
        {
            CacheBones();
        }

        public void CacheBones()
        {
            if (targetAnimator == null)
                targetAnimator = GetComponentInChildren<Animator>();

            Transform searchRoot = targetAnimator != null ? targetAnimator.transform : transform;

            if (targetAnimator != null && targetAnimator.isHuman)
            {
                if (spine1Bone == null) spine1Bone = targetAnimator.GetBoneTransform(HumanBodyBones.Chest) ?? targetAnimator.GetBoneTransform(HumanBodyBones.Spine);
                if (leftShoulder == null) leftShoulder = targetAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder);
                if (leftUpperArm == null) leftUpperArm = targetAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                if (leftForeArm == null) leftForeArm = targetAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                if (leftHand == null) leftHand = targetAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
                if (rightShoulder == null) rightShoulder = targetAnimator.GetBoneTransform(HumanBodyBones.RightShoulder);
                if (rightUpperArm == null) rightUpperArm = targetAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                if (rightForeArm == null) rightForeArm = targetAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                if (rightHand == null) rightHand = targetAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            }

            if (spine1Bone == null) spine1Bone = FindBoneRecursive(searchRoot, "mixamorig:Spine1") ?? FindBoneRecursive(searchRoot, "Spine1");
            if (leftShoulder == null) leftShoulder = FindBoneRecursive(searchRoot, "mixamorig:LeftShoulder") ?? FindBoneRecursive(searchRoot, "LeftShoulder");
            if (leftUpperArm == null) leftUpperArm = FindBoneRecursive(searchRoot, "mixamorig:LeftArm") ?? FindBoneRecursive(searchRoot, "LeftArm");
            if (leftForeArm == null) leftForeArm = FindBoneRecursive(searchRoot, "mixamorig:LeftForeArm") ?? FindBoneRecursive(searchRoot, "LeftForeArm");
            if (leftHand == null) leftHand = FindBoneRecursive(searchRoot, "mixamorig:LeftHand") ?? FindBoneRecursive(searchRoot, "LeftHand");
            if (rightShoulder == null) rightShoulder = FindBoneRecursive(searchRoot, "mixamorig:RightShoulder") ?? FindBoneRecursive(searchRoot, "RightShoulder");
            if (rightUpperArm == null) rightUpperArm = FindBoneRecursive(searchRoot, "mixamorig:RightArm") ?? FindBoneRecursive(searchRoot, "RightArm");
            if (rightForeArm == null) rightForeArm = FindBoneRecursive(searchRoot, "mixamorig:RightForeArm") ?? FindBoneRecursive(searchRoot, "RightForeArm");
            if (rightHand == null) rightHand = FindBoneRecursive(searchRoot, "mixamorig:RightHand") ?? FindBoneRecursive(searchRoot, "RightHand");

            if (guitarTransform == null)
                guitarTransform = FindBoneRecursive(transform, "Guitarra");
        }

        private void LateUpdate()
        {
            ApplyGuitarAndArmPose(Application.isPlaying ? Time.time : 0f);
        }

        /// <summary>
        /// Aplica la orientación de la guitarra y resuelve el IK de ambos brazos.
        /// Puede invocarse también desde scripts de Editor para fijar la pose serializada.
        /// </summary>
        public void ApplyGuitarAndArmPose(float timeSeconds)
        {
            if (leftUpperArm == null || rightUpperArm == null || guitarTransform == null)
            {
                CacheBones();
                if (leftUpperArm == null || rightUpperArm == null || guitarTransform == null)
                    return;
            }

            Transform charRoot = targetAnimator != null ? targetAnimator.transform : transform;

            // 1. Asegurar posición y orientación canónica de la guitarra por delante del poncho (sobre Spine1)
            // En Guitarra.fbx:
            //   Local -Z (-forward) = Mástil hacia el clavijero
            //   Local +Z (+forward) = Base inferior de la caja
            //   Local +Y (+up)      = Tapa armónica / boca / cuerdas (hacia adelante)
            //   Local +X (+right)   = Aro superior de la caja
            if (spine1Bone != null && guitarTransform.parent == spine1Bone)
            {
                guitarTransform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                guitarTransform.localPosition = new Vector3(0.075f, -0.035f, 0.250f);

                // Dirección del mástil (-Z_guitar): hacia la izquierda (-X), arriba (+Y) y apenas hacia el hombro izquierdo (-Z)
                // para que el brazo izquierdo alcance cómodamente los trastes superiores sin quedarse corto
                Vector3 neckDirLocal = new Vector3(-0.91f, 0.41f, -0.04f).normalized;
                // Dirección de la tapa/cuerdas (+Y_guitar): hacia adelante (+Z) y apenas inclinada hacia arriba (+Y)
                Vector3 frontFaceDirLocal = new Vector3(-0.08f, 0.18f, 0.98f).normalized;

                Quaternion baseGuitarRot = Quaternion.LookRotation(-neckDirLocal, frontFaceDirLocal);

                if (animateHands && timeSeconds > 0f)
                {
                    // Leve balanceo natural de la guitarra al compás del rasgueo
                    float sway = Mathf.Sin(timeSeconds * strumSpeed * Mathf.PI) * 1.4f;
                    guitarTransform.localRotation = baseGuitarRot * Quaternion.Euler(sway, 0f, sway * 0.5f);
                }
                else
                {
                    guitarTransform.localRotation = baseGuitarRot;
                }
            }

            // Adelantar ambas clavículas (hombros) para que los brazos salgan por delante del poncho y alcancen el mástil y la boca con soltura
            if (rightShoulder != null)
            {
                Vector3 rightClavicleDir = (charRoot.right * 0.76f + charRoot.forward * 0.58f - charRoot.up * 0.28f).normalized;
                Quaternion rightClavDelta = Quaternion.FromToRotation(rightShoulder.up, rightClavicleDir);
                rightShoulder.rotation = rightClavDelta * rightShoulder.rotation;
            }

            if (leftShoulder != null)
            {
                Vector3 leftClavicleDir = (-charRoot.right * 0.74f + charRoot.forward * 0.64f - charRoot.up * 0.20f).normalized;
                Quaternion leftClavDelta = Quaternion.FromToRotation(leftShoulder.up, leftClavicleDir);
                leftShoulder.rotation = leftClavDelta * leftShoulder.rotation;
            }

            // Ejes de la guitarra en espacio global
            Vector3 gRight = guitarTransform.right;     // Perpendicular a las cuerdas (de aro inferior a aro superior)
            Vector3 gUp = guitarTransform.up;           // Normal a la tapa armónica (hacia afuera de las cuerdas)
            Vector3 gForward = guitarTransform.forward; // A lo largo de la guitarra (de clavijero a base; -gForward va hacia el clavijero)

            // 2. Calcular offsets de animación (o cero si es mano fija)
            float strumWave = 0f;
            float strumWristFlex = 0f;
            float fretSlide = 0f;
            float fretFingerFlex = 0f;

            if (animateHands && timeSeconds > 0f)
            {
                // Patrón de rasgueo folklórico (combinación de fundamental y armónico para simular rasgueo abajo-arriba-golpe)
                float phase = timeSeconds * strumSpeed * Mathf.PI * 2f;
                strumWave = Mathf.Sin(phase) * 0.75f + Mathf.Sin(phase * 2f + 0.4f) * 0.25f;
                strumWristFlex = Mathf.Sin(phase - 0.35f) * wristStrumAngle;

                // Desplazamiento suave entre trastes en el mástil (escalonado suave)
                float chordPhase = timeSeconds * chordChangeSpeed * Mathf.PI * 2f;
                float rawChord = Mathf.Sin(chordPhase) + 0.35f * Mathf.Sin(chordPhase * 3f);
                fretSlide = Mathf.Clamp(rawChord, -1f, 1f) * fretSlideAmplitude;
                fretFingerFlex = Mathf.Sin(phase * 0.5f) * leftWristFretAngle;
            }

            // 3. Resolver Brazo Derecho (Mano que rasguea por delante de la tapa/cuerdas sin meterse dentro de la guitarra)
            // En Guitarra.fbx:
            //   y = +0.148f saca la mano hacia afuera de la tapa armónica (y_tapa = 0.0825) para que no se meta en la caja
            //   z = +0.055f desplaza la mano un poco más hacia afuera/puente (hacia la derecha visual de la caja)
            //   x = +0.035f ubica la muñeca sobre el borde superior de las cuerdas
            Vector3 rightWristBase = guitarTransform.TransformPoint(new Vector3(0.035f, 0.148f, 0.055f));
            // Al rasguear, la mano cruza perpendicularmente las cuerdas (eje -gRight / +gRight)
            Vector3 rightWristTarget = rightWristBase
                - gRight * (strumWave * strumAmplitude)
                + gForward * (strumWave * strumAmplitude * 0.22f);

            // Hint del codo derecho: por delante del poncho y separado de la caja para que el antebrazo no atraviese la guitarra
            Vector3 rightElbowHint = charRoot.TransformPoint(new Vector3(0.35f, 0.62f, 0.34f));

            SolveTwoBoneArmIK(rightUpperArm, rightForeArm, rightHand, rightWristTarget, rightElbowHint);

            if (rightHand != null && rightForeArm != null)
            {
                // Orientar la mano derecha paralela a la tapa (+gUp leve) para que los dedos no se claven dentro de la guitarra
                Vector3 rightFingersDir = (-gRight * 0.82f - gForward * 0.56f + gUp * 0.08f).normalized;
                Quaternion aimFingers = Quaternion.FromToRotation(rightHand.up, rightFingersDir);
                rightHand.rotation = aimFingers * rightHand.rotation;

                if (animateHands && timeSeconds > 0f)
                {
                    // Giro de muñeca para el rasgueo
                    rightHand.rotation = Quaternion.AngleAxis(strumWristFlex, gForward) * rightHand.rotation;
                }
            }

            // 4. Resolver Brazo Izquierdo (Mano en los trastes, bajada verticalmente para que la palma/dedos queden a la altura del mástil)
            // En Guitarra.fbx:
            //   z = -0.45f ubica la mano en la sección media-alta de los trastes (cerca del clavijero)
            //   x = -0.082f baja la muñeca verticalmente por debajo del borde inferior del mástil para que los dedos queden sobre el diapasón
            //   y = +0.112f coloca la muñeca por delante del plano del diapasón para no atravesar la madera
            float neckZ = -0.45f + fretSlide;
            Vector3 leftWristTarget = guitarTransform.TransformPoint(new Vector3(-0.082f, 0.112f, neckZ));

            // Hint del codo izquierdo: más abajo y adelantado para acompañar la posición más baja de la mano izquierda
            Vector3 leftElbowHint = charRoot.TransformPoint(new Vector3(-0.28f, 0.48f, 0.30f));

            SolveTwoBoneArmIK(leftUpperArm, leftForeArm, leftHand, leftWristTarget, leftElbowHint);

            if (leftHand != null && leftForeArm != null)
            {
                // En el rig Mixamo del Gaucho:
                //   leftHand.up (+Y local)      = dirección de la muñeca hacia los dedos
                //   leftHand.forward (+Z local) = normal de la palma de la mano
                // Orientamos la palma mirando hacia el diapasón (-gUp) y los dedos subiendo en diagonal sobre los trastes (+gRight, -gForward)
                // con una leve salida hacia afuera (+gUp) para que la mano quede paralela al traste y por encima de las cuerdas
                Vector3 leftFingersDir = (gRight * 0.86f - gForward * 0.44f + gUp * 0.25f).normalized;
                Vector3 leftPalmNormal = (-gUp * 0.90f + gRight * 0.28f - gForward * 0.32f).normalized;
                leftHand.rotation = Quaternion.LookRotation(leftPalmNormal, leftFingersDir);

                if (animateHands && timeSeconds > 0f)
                {
                    leftHand.rotation = Quaternion.AngleAxis(fretFingerFlex, gRight) * leftHand.rotation;
                }
            }
        }

        private static void SolveTwoBoneArmIK(
            Transform upperArm,
            Transform foreArm,
            Transform hand,
            Vector3 wristTargetPos,
            Vector3 elbowHintPos)
        {
            if (upperArm == null || foreArm == null || hand == null)
                return;

            Vector3 rootPos = upperArm.position;
            float l1 = Vector3.Distance(upperArm.position, foreArm.position);
            float l2 = Vector3.Distance(foreArm.position, hand.position);

            if (l1 < 0.001f || l2 < 0.001f)
                return;

            Vector3 toTarget = wristTargetPos - rootPos;
            float dist = toTarget.magnitude;
            if (dist < 0.001f)
                return;

            float maxReach = (l1 + l2) * 0.992f;
            float minReach = Mathf.Abs(l1 - l2) * 1.008f;
            float clampedDist = Mathf.Clamp(dist, minReach, maxReach);
            Vector3 dirNorm = toTarget / dist;

            // Ley del coseno para hallar el ángulo del hombro hacia el codo
            float cosA = (l1 * l1 + clampedDist * clampedDist - l2 * l2) / (2f * l1 * clampedDist);
            cosA = Mathf.Clamp(cosA, -1f, 1f);
            float sinA = Mathf.Sqrt(Mathf.Max(0f, 1f - cosA * cosA));

            // Dirección del codo ortogonal a la línea hombro-muñeca
            Vector3 toHint = elbowHintPos - rootPos;
            Vector3 bendDir = toHint - Vector3.Dot(toHint, dirNorm) * dirNorm;
            if (bendDir.sqrMagnitude < 0.0001f)
            {
                bendDir = Vector3.down;
            }
            else
            {
                bendDir.Normalize();
            }

            Vector3 desiredElbowPos = rootPos + dirNorm * (l1 * cosA) + bendDir * (l1 * sinA);
            Vector3 desiredUpperDir = (desiredElbowPos - rootPos).normalized;

            // En el rig Mixamo del Gaucho, el eje local +Y (bone.up) apunta a lo largo del hueso hacia el siguiente nodo
            Quaternion upperDelta = Quaternion.FromToRotation(upperArm.up, desiredUpperDir);
            upperArm.rotation = upperDelta * upperArm.rotation;

            // Ahora foreArm.position está en desiredElbowPos; apuntar el antebrazo hacia wristTargetPos
            Vector3 desiredForeArmDir = (wristTargetPos - foreArm.position).normalized;
            Quaternion foreArmDelta = Quaternion.FromToRotation(foreArm.up, desiredForeArmDir);
            foreArm.rotation = foreArmDelta * foreArm.rotation;
        }

        private static Transform FindBoneRecursive(Transform parent, string exactName)
        {
            if (parent == null) return null;
            if (parent.name == exactName) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindBoneRecursive(parent.GetChild(i), exactName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
