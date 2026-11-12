using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Controla el comportamiento del Bartender en la zona de la barra, y su transición
    /// a la Mesa 9 cuando todos los gauchos han sido derrotados (o al pulsar el botón de prueba).
    /// </summary>
    public class BartenderController : MonoBehaviour
    {
        public static BartenderController Instance { get; private set; }

        [Header("Animación")]
        [SerializeField] private Animator animator;
        [SerializeField] private string idleStateName = "Idle";
        [SerializeField] private string walkStateName = "Walk";
        [SerializeField] private string bartendingStateName = "Bartending";
        [SerializeField] private string seatedStateName = "Seated";

        [Header("Ajuste de Suelo y Pies")]
        [Tooltip("Desplazamiento vertical del modelo visual para apoyar los pies en el suelo sin flotar.")]
        [SerializeField] private float visualYOffset = -0.09f;

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

        public enum State
        {
            Idle,
            Walking,
            Bartending,
            WalkingToTable,
            SeatedAtTable
        }

        public State CurrentState => currentState;

        private State currentState = State.Idle;
        private float stateTimer = 0f;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool isInitialized = false;

        private List<Vector3> tableWaypoints = new List<Vector3>();
        private int currentWaypointIndex = 0;
        private SeatInteractable assignedSeatTable9;
        private TableDefinition table9Def;

        private static bool IsMultiplayerSession()
        {
            return NetworkUI.selectedMode == GameMode.Host || NetworkUI.selectedMode == GameMode.Client;
        }

        private void Awake()
        {
            Instance = this;
            ApplyVisualOffset();

            if (FindAnyObjectByType<GameProgressionManager>() == null)
            {
                GameObject progObj = new GameObject("GameProgressionManager");
                progObj.AddComponent<GameProgressionManager>();
            }

            if (!IsMultiplayerSession() && FindAnyObjectByType<BartenderTestButton>() == null)
            {
                GameObject testBtnObj = new GameObject("BartenderTestButton");
                testBtnObj.AddComponent<BartenderTestButton>();
            }
        }

        private void OnValidate()
        {
            ApplyVisualOffset();
        }

        private void ApplyVisualOffset()
        {
            Transform visuals = transform.Find("Visuals");
            if (visuals == null && animator != null)
            {
                visuals = animator.transform;
            }
            if (visuals != null)
            {
                Vector3 lp = visuals.localPosition;
                lp.y = visualYOffset;
                visuals.localPosition = lp;
            }
        }

        public void Initialize(Vector3 center, Quaternion defaultRot)
        {
            barCenter = center;
            defaultFacingEuler = defaultRot.eulerAngles;
            isInitialized = true;
            ApplyVisualOffset();
        }

        private void Start()
        {
            if (!isInitialized)
            {
                barCenter = transform.position;
                defaultFacingEuler = transform.eulerAngles;
            }

            ApplyVisualOffset();

            if (animator == null || animator.avatar == null)
            {
                Animator[] anims = GetComponentsInChildren<Animator>(true);
                foreach (var a in anims)
                {
                    if (a.avatar != null && a.avatar.isValid)
                    {
                        animator = a;
                        break;
                    }
                }
                if (animator == null && anims.Length > 0)
                {
                    animator = anims[anims.Length - 1];
                }
            }
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }

            targetRotation = Quaternion.Euler(defaultFacingEuler);

            // El Bartender SIEMPRE comienza en su puesto de trabajo detrás de la barra.
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
                case State.WalkingToTable:
                    UpdateWalkingToTable();
                    break;
                case State.SeatedAtTable:
                    // Se encuentra sentado en Mesa 9 aguardando al jugador
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
                    targetRotation = Quaternion.Euler(defaultFacingEuler);
                    UpdateAnimator(0f, true);
                    break;
            }
        }

        private void UpdateIdle()
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
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

        /// <summary>
        /// Reubica al Bartender detrás de la barra en su posición y rotación original en Idle.
        /// </summary>
        public void ResetToBar()
        {
            if (assignedSeatTable9 != null && assignedSeatTable9.CurrentOccupant == gameObject)
            {
                assignedSeatTable9.CurrentOccupant = null;
            }

            transform.position = barCenter;
            transform.rotation = Quaternion.Euler(defaultFacingEuler);
            currentState = State.Idle;
            stateTimer = 2.0f;
            ApplyVisualOffset();
            UpdateAnimator(0f, false);
            Debug.Log("<color=#FF9800><b>[Bartender] Regresó a su posición detrás de la barra.</b></color>");
        }

        /// <summary>
        /// Inicia la caminata del Bartender hacia la Mesa 9 para tomar asiento contra la pared.
        /// </summary>
        public void GoToTable9(bool instant = false)
        {
            if (IsMultiplayerSession())
            {
                return;
            }

            if (!instant)
            {
                if (currentState == State.WalkingToTable)
                {
                    Debug.Log("[Bartender] Ya se encuentra caminando hacia la Mesa 9.");
                    return;
                }
                if (currentState == State.SeatedAtTable)
                {
                    Debug.Log("[Bartender] Ya se encuentra sentado en la Mesa 9.");
                    return;
                }
            }

            table9Def = TableDefinition.FindById("Mesa_9");
            if (table9Def == null)
            {
                TableDefinition[] all = TableDefinition.FindAll();
                foreach (var t in all)
                {
                    if (t != null && t.TableId != null && t.TableId.Contains("9"))
                    {
                        table9Def = t;
                        break;
                    }
                }
            }

            if (table9Def == null)
            {
                Debug.LogWarning("[Bartender] No se encontró Mesa_9 para el Duelo Final.");
                return;
            }

            SeatInteractable wallSeat = null;
            SeatInteractable playerSeat = null;

            var validSeats = table9Def.GetComponentsInChildren<SeatInteractable>(true)
                .Where(s => Vector3.Distance(table9Def.transform.position, s.transform.position) <= 2.2f && s.GetComponentsInChildren<Renderer>(true).Length > 0)
                .ToList();

            if (validSeats.Count >= 2)
            {
                wallSeat = validSeats.OrderBy(s => s.transform.position.x).First();
                playerSeat = validSeats.OrderBy(s => s.transform.position.x).Last();
                table9Def.Configure("Mesa_9", playerSeat, wallSeat);
                wallSeat.name = "Mesa_9_Banco_B";
                playerSeat.name = "Mesa_9_Banco_A";
                assignedSeatTable9 = wallSeat;
            }
            else if (table9Def.Seats != null && table9Def.Seats.Count >= 2 && table9Def.Seats[0] != null && table9Def.Seats[1] != null)
            {
                // El banco contra la pared es el que tiene menor X en mundo (más cercano a x = -9.1)
                if (table9Def.Seats[0].transform.position.x < table9Def.Seats[1].transform.position.x)
                {
                    wallSeat = table9Def.Seats[0];
                    playerSeat = table9Def.Seats[1];
                }
                else
                {
                    wallSeat = table9Def.Seats[1];
                    playerSeat = table9Def.Seats[0];
                }

                // Asegurar que el banco del Bartender quede como Seats[1] (Banco B) y el jugador en Seats[0] (Banco A)
                table9Def.Configure("Mesa_9", playerSeat, wallSeat);
                wallSeat.name = "Mesa_9_Banco_B";
                playerSeat.name = "Mesa_9_Banco_A";
                assignedSeatTable9 = wallSeat;
            }

            Vector3 seatPos = assignedSeatTable9 != null 
                ? assignedSeatTable9.transform.position 
                : table9Def.transform.position + new Vector3(-1.2f, 0f, 0f);

            if (instant)
            {
                SnapToTable9Seat();
                return;
            }

            // Waypoints precisos para salir por el lado libre del mostrador (z ≈ 10.6)
            // y cruzar por la puerta del separador (x ≈ -2.33, z ≈ 10.6) sin atravesar muebles ni paredes:
            tableWaypoints.Clear();
            // 1. Desplazarse por detrás del mostrador hacia el extremo libre
            tableWaypoints.Add(new Vector3(3.8f, transform.position.y, 8.5f));
            // 2. Dirigirse hacia la abertura al final del mostrador lateral (que termina en z ≈ 9.96)
            tableWaypoints.Add(new Vector3(3.5f, transform.position.y, 10.6f));
            // 3. Cruzar el lado libre del mostrador hacia el pasillo central
            tableWaypoints.Add(new Vector3(1.5f, transform.position.y, 10.6f));
            tableWaypoints.Add(new Vector3(0.0f, transform.position.y, 10.6f));
            // 4. Cruzar la puerta abierta del separador (en x ≈ -2.33, z ≈ 10.6)
            tableWaypoints.Add(new Vector3(-2.33f, transform.position.y, 10.6f));
            // 5. Ingresar a la sala privada de la Mesa 9
            tableWaypoints.Add(new Vector3(-5.0f, transform.position.y, 10.6f));
            // 6. Rodear la mesa por detrás (z ≈ 10.6) hacia el lado de la pared (x = seatPos.x)
            tableWaypoints.Add(new Vector3(seatPos.x, transform.position.y, 10.5f));
            // 7. Llegar al banco contra la pared
            tableWaypoints.Add(new Vector3(seatPos.x, transform.position.y, seatPos.z));

            currentWaypointIndex = 0;
            currentState = State.WalkingToTable;
            UpdateAnimator(1f, false);
            Debug.Log("<color=#FF9800><b>[Bartender] El Patrón sale por el lado libre de la barra, cruza la puerta y se dirige a la Mesa 9 para el Duelo Final.</b></color>");
        }

        private void UpdateWalkingToTable()
        {
            if (tableWaypoints == null || currentWaypointIndex >= tableWaypoints.Count)
            {
                SnapToTable9Seat();
                return;
            }

            Vector3 target = tableWaypoints[currentWaypointIndex];
            Vector3 diff = target - transform.position;
            diff.y = 0f;
            float dist = diff.magnitude;

            if (dist > 0.25f)
            {
                Vector3 dir = diff / dist;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * turnSpeed);
                transform.position = Vector3.MoveTowards(transform.position, target, 1.35f * Time.deltaTime);
                UpdateAnimator(1f, false);
            }
            else
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= tableWaypoints.Count)
                {
                    SnapToTable9Seat();
                }
            }
        }

        private void SnapToTable9Seat()
        {
            if (table9Def == null)
                table9Def = TableDefinition.FindById("Mesa_9");

            Vector3 tablePos = table9Def != null ? table9Def.transform.position : transform.position;
            Vector3 seatPos = assignedSeatTable9 != null ? assignedSeatTable9.transform.position : transform.position;

            Vector3 toTable = tablePos - seatPos;
            toTable.y = 0;
            Vector3 forward = toTable.sqrMagnitude > 0.001f ? toTable.normalized : transform.forward;

            transform.position = seatPos + (Vector3.up * 0.35f) + (forward * 0.15f);
            transform.rotation = Quaternion.LookRotation(forward);

            if (assignedSeatTable9 != null)
            {
                assignedSeatTable9.CurrentOccupant = gameObject;
                var marker = gameObject.GetComponent<BotMarker>() ?? gameObject.AddComponent<BotMarker>();
                marker.assignedSeat = assignedSeatTable9;
            }

            currentState = State.SeatedAtTable;

            var server = FindAnyObjectByType<GameServer>();
            server?.OnBartenderSeatedAtTable9();

            if (animator != null && animator.isActiveAndEnabled)
            {
                foreach (var p in animator.parameters)
                {
                    if (p.name == "Speed") animator.SetFloat("Speed", 0f);
                    if (p.name == "IsBartending") animator.SetBool("IsBartending", false);
                    if (p.name == "IsSeated") animator.SetBool("IsSeated", true);
                }

                var current = animator.GetCurrentAnimatorStateInfo(0);
                if (!current.IsName(seatedStateName) && !animator.IsInTransition(0))
                {
                    animator.CrossFadeInFixedTime(seatedStateName, 0.3f);
                }
            }

            Debug.Log("<color=#4CAF50><b>[Bartender] El Patrón está sentado en la Mesa 9 contra la pared. ¡El Duelo Final está listo!</b></color>");
        }

        private void UpdateAnimator(float speed, bool isBartending)
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return;

            foreach (var p in animator.parameters)
            {
                if (p.name == "Speed") animator.SetFloat("Speed", speed);
                if (p.name == "IsBartending") animator.SetBool("IsBartending", isBartending);
                if (p.name == "IsSeated") animator.SetBool("IsSeated", false);
            }

            var current = animator.GetCurrentAnimatorStateInfo(0);

            if (isBartending)
            {
                if (!current.IsName(bartendingStateName) && !animator.IsInTransition(0))
                {
                    animator.CrossFadeInFixedTime(bartendingStateName, 0.25f);
                }
            }
            else if (speed > 0.1f)
            {
                // Solo llamar a CrossFade si no está ya reproduciendo o transicionando a Walk,
                // para no reiniciar la animación a tiempo 0 en cada fotograma.
                if (!current.IsName(walkStateName) && !animator.IsInTransition(0))
                {
                    animator.CrossFadeInFixedTime(walkStateName, 0.2f);
                }
            }
            else
            {
                if (!current.IsName(idleStateName) && !animator.IsInTransition(0))
                {
                    animator.CrossFadeInFixedTime(idleStateName, 0.25f);
                }
            }
        }
    }
}
