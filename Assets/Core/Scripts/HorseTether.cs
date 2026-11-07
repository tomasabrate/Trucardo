using UnityEngine;

namespace Trucardo.Environment
{
    /// <summary>
    /// Soga que ata el caballo a la valla o palenque.
    /// Actualiza dinamicamente una curva con catenaria (caida por gravedad)
    /// mediante un LineRenderer entre la cabezada/bocado del caballo y el larguero de la valla.
    /// </summary>
    [ExecuteAlways]
    public class HorseTether : MonoBehaviour
    {
        [Header("Puntos de Amarre")]
        [Tooltip("Punto de amarre en la valla/palenque (si es nulo usa fenceWorldPosition)")]
        [SerializeField] private Transform fenceAnchor;
        [SerializeField] private Vector3 fenceWorldPosition = new Vector3(1.4f, 0.90f, -15.72f);

        [Tooltip("Punto en la cabeza o bocado del caballo")]
        [SerializeField] private Transform horseAnchor;
        [SerializeField] private string targetHorseName = "";
        [SerializeField] private bool isAnimatedTether = false;

        [Header("Propiedades de la Soga")]
        [Range(4, 20)]
        [SerializeField] private int segmentCount = 10;
        [SerializeField] private float ropeWidth = 0.024f;
        [SerializeField] private float sagAmount = 0.10f;
        [SerializeField] private Color ropeColor = new Color(0.76f, 0.68f, 0.52f);
        [SerializeField] private Material customMaterial;

        private LineRenderer lineRenderer;
        private Vector3[] positions;

        private void Awake()
        {
            SetupLineRenderer();
        }

        private void Start()
        {
            SetupLineRenderer();
            UpdateTether();
        }

        private void OnEnable()
        {
            SetupLineRenderer();
            UpdateTether();
        }

        public void SetAnchors(Transform fence, Transform horse)
        {
            fenceAnchor = fence;
            horseAnchor = horse;
            UpdateTether();
        }

        public void SetFencePosition(Vector3 worldPos)
        {
            fenceWorldPosition = worldPos;
            UpdateTether();
        }

        private void SetupLineRenderer()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
                if (lineRenderer == null)
                {
                    lineRenderer = gameObject.AddComponent<LineRenderer>();
                }
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = ropeWidth;
            lineRenderer.endWidth = ropeWidth;
            lineRenderer.numCapVertices = 4;
            lineRenderer.numCornerVertices = 4;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            lineRenderer.receiveShadows = true;

            if (lineRenderer.sharedMaterial == null)
            {
                if (customMaterial != null)
                {
                    lineRenderer.sharedMaterial = customMaterial;
                }
                else
                {
                    // Shader URP Lit o Standard fallback
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null) shader = Shader.Find("Standard");
                    if (shader == null) shader = Shader.Find("Sprites/Default");

                    Material mat = new Material(shader)
                    {
                        name = "Mat_Soga_Runtime",
                        color = ropeColor
                    };
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", ropeColor);
                    }
                    lineRenderer.sharedMaterial = mat;
                }
            }
        }

        private void LateUpdate()
        {
            UpdateTether();
        }

        public void UpdateTether()
        {
            if (lineRenderer == null) SetupLineRenderer();

            Vector3 startPos = fenceAnchor != null ? fenceAnchor.position : fenceWorldPosition;
            Vector3 endPos;

            if (horseAnchor != null)
            {
                endPos = horseAnchor.position;
            }
            else
            {
                GameObject target = !string.IsNullOrEmpty(targetHorseName) ? GameObject.Find(targetHorseName) : null;
                if (target == null)
                {
                    string fallbackName = isAnimatedTether ? "Caballo_Animado" : "Caballo_Estatico";
                    target = GameObject.Find(fallbackName);
                }
                Transform targetTransform = target != null ? target.transform : transform;

                Transform head = targetTransform.Find("HorseArmatureObj/Root/Spine/Neck/Head");
                if (head == null)
                {
                    Transform[] allTransforms = targetTransform.GetComponentsInChildren<Transform>();
                    foreach (var t in allTransforms)
                    {
                        if (t.name.Equals("Head", System.StringComparison.OrdinalIgnoreCase))
                        {
                            head = t;
                            break;
                        }
                    }
                }

                if (head != null)
                {
                    endPos = head.position + head.forward * 0.15f - head.up * 0.05f;
                }
                else
                {
                    // Para caballo: altura aproximada de la trompa
                    endPos = targetTransform.position + targetTransform.forward * 1.35f + Vector3.up * 1.45f;
                }
            }

            if (positions == null || positions.Length != segmentCount + 1)
            {
                positions = new Vector3[segmentCount + 1];
                lineRenderer.positionCount = segmentCount + 1;
            }

            for (int i = 0; i <= segmentCount; i++)
            {
                float t = i / (float)segmentCount;
                // Interpolar linea recta + catenaria cuadratica por peso de soga
                Vector3 p = Vector3.Lerp(startPos, endPos, t);
                float sag = sagAmount * 4f * t * (1f - t);
                p.y -= sag;
                positions[i] = p;
            }

            lineRenderer.SetPositions(positions);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = ropeColor;
            Vector3 p0 = fenceAnchor != null ? fenceAnchor.position : fenceWorldPosition;
            Vector3 p1 = horseAnchor != null ? horseAnchor.position : transform.position + Vector3.up * 1.4f;
            Gizmos.DrawWireSphere(p0, 0.05f);
            Gizmos.DrawWireSphere(p1, 0.05f);
            Gizmos.DrawLine(p0, p1);
        }
    }
}
