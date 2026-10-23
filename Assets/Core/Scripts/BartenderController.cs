using System.Collections;
using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Controla el comportamiento autónomo del Bartender en la zona de la barra.
    /// Pasa la mayor parte del tiempo parado (Idle), y de vez en cuando camina
    /// a lo largo del mostrador o realiza la animación de atender/limpiar la barra.
    /// </summary>
    public class BartenderController : MonoBehaviour
    {
        [Header("Animación")]
        [SerializeField] private Animator animator;
        [SerializeField] private string idleStateName = "Idle";
        [SerializeField] private string walkStateName = "Walk";
        [SerializeField] private string bartendingStateName = "Bartending";

        [Header("Zona de la Barra (Patrullaje)")]
        [Tooltip("Posición central de referencia detrás de la barra")]
        [SerializeField] private Vector3 barCenter = new Vector3(6.05f, 0f, 8.2f);

        [Tooltip("Rango de movimiento lateral a lo largo del mostrador (Eje X)")]
        [SerializeField] private float patrolRangeX = 1.6f;

        [Tooltip("Rango de profundidad detrás del mostrador (Eje Z)")]
        [SerializeField] private float depthRangeZ = 0.2f;

        [Tooltip("Orientación frontal predeterminada mirando al salón")]
        [SerializeField] private Vector3 defaultFacingEuler = new Vector3(0f, 180f, 0f);

        [Header("Velocidades")]
        [SerializeField] private float walkSpeed = 1.1f;
        [SerializeField] private float turnSpeed = 4.0f;

        [Header("Tiempos de Comportamiento")]
        [Tooltip("Tiempo que pasa parado en Idle (la mayor parte del tiempo)")]
        [SerializeField] private Vector2 idleDurationRange = new Vector2(4f, 8f);

        [Tooltip("Tiempo que pasa haciendo la animación de la barra")]
        [SerializeField] private Vector2 bartendingDurationRange = new Vector2(4f, 7f);

        private enum State
        {
            Idle,
            Walking,
            Bartending
        }

        private State currentState = State.Idle;
        private float stateTimer = 0f;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool isInitialized = false;

        public void Initialize(Vector3 center, Quaternion defaultRot)
        {
            barCenter = center;
            defaultFacingEuler = defaultRot.eulerAngles;
            isInitialized = true;
        }

        private void Start()
        {
            if (!isInitialized)
            {
                barCenter = transform.position;
                defaultFacingEuler = transform.eulerAngles;
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            targetRotation = Quaternion.Euler(defaultFacingEuler);

            // Breve espera inicial de 2s para que empiece a actuar rápido al darle Play en el Editor
            currentState = State.Idle;
            stateTimer = 2.0f;
            UpdateAnimator(0f, false);
        }

        private void Update()
        {
            switch (currentState)
            {
                case State.Idle:
                    UpdateIdle();
                    break;
                case State.Walking:
                    UpdateWalking();
                    break;
                case State.Bartending:
                    UpdateBartending();
                    break;
            }
        }

        private void EnterState(State newState)
        {
            currentState = newState;

            switch (newState)
            {
                case State.Idle:
                    stateTimer = Random.Range(idleDurationRange.x, idleDurationRange.y);
                    targetRotation = Quaternion.Euler(defaultFacingEuler);
                    UpdateAnimator(0f, false);
                    break;

                case State.Walking:
                    // Elegir un punto a lo largo del mostrador que esté al menos a 0.5m de distancia
                    float candidateX = barCenter.x;
                    for (int i = 0; i < 5; i++)
                    {
                        candidateX = barCenter.x + Random.Range(-patrolRangeX, patrolRangeX);
                        if (Mathf.Abs(candidateX - transform.position.x) > 0.4f) break;
                    }

                    float randZ = barCenter.z + Random.Range(-depthRangeZ, depthRangeZ);
                    targetPosition = new Vector3(candidateX, transform.position.y, randZ);

                    Vector3 moveDir = targetPosition - transform.position;
                    moveDir.y = 0f;
                    if (moveDir.sqrMagnitude > 0.01f)
                    {
                        targetRotation = Quaternion.LookRotation(moveDir.normalized);
                    }
                    stateTimer = 10f; // Timeout de seguridad
                    UpdateAnimator(1f, false);
                    break;

                case State.Bartending:
                    stateTimer = Random.Range(bartendingDurationRange.x, bartendingDurationRange.y);
                    // Mira de frente al mostrador mientras trabaja
                    targetRotation = Quaternion.Euler(defaultFacingEuler);
                    UpdateAnimator(0f, true);
                    break;
            }
        }

        private void UpdateIdle()
        {
            // Girar suavemente hacia el salón
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                // Mayoría del tiempo parado: 45% caminar, 40% bartending, 15% otro ciclo idle
                float roll = Random.value;
                if (roll < 0.45f)
                {
                    EnterState(State.Walking);
                }
                else if (roll < 0.85f)
                {
                    EnterState(State.Bartending);
                }
                else
                {
                    EnterState(State.Idle);
                }
            }
        }

        private void UpdateWalking()
        {
            Vector3 diff = targetPosition - transform.position;
            diff.y = 0f;
            float dist = diff.magnitude;

            if (dist > 0.12f)
            {
                Vector3 dir = diff / dist;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * turnSpeed);
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, walkSpeed * Time.deltaTime);

                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    EnterState(State.Idle);
                }
            }
            else
            {
                // Llegó a su destino en la barra, regresa a Idle
                EnterState(State.Idle);
            }
        }

        private void UpdateBartending()
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                EnterState(State.Idle);
            }
        }

        private void UpdateAnimator(float speed, bool isBartending)
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return;

            // 1. Sincronización mediante parámetros estándar del Animator
            foreach (var p in animator.parameters)
            {
                if (p.name == "Speed") animator.SetFloat("Speed", speed);
                if (p.name == "IsBartending") animator.SetBool("IsBartending", isBartending);
                if (p.name == "IsSeated") animator.SetBool("IsSeated", false);
            }

            // 2. Play / CrossFade directo de estados si existen
            if (isBartending)
            {
                if (animator.HasState(0, Animator.StringToHash(bartendingStateName)))
                {
                    animator.CrossFadeInFixedTime(bartendingStateName, 0.3f);
                }
            }
            else if (speed > 0.1f)
            {
                if (animator.HasState(0, Animator.StringToHash(walkStateName)))
                {
                    animator.CrossFadeInFixedTime(walkStateName, 0.25f);
                }
            }
            else
            {
                if (animator.HasState(0, Animator.StringToHash(idleStateName)))
                {
                    animator.CrossFadeInFixedTime(idleStateName, 0.3f);
                }
            }
        }
    }
}
