using Unity.Netcode;
using UnityEngine;
using TMPro;

namespace Trucardo.Core
{
    /// <summary>
    /// Controla un cartel de madera con número en estilo tiza sobre una mesa de juego.
    /// Solo es visible en modo Un Jugador (en Multijugador queda oculto).
    /// </summary>
    public class TableNumberSignController : MonoBehaviour
    {
        [SerializeField] private int tableNumber = 1;
        [SerializeField] private GameObject visualsRoot;
        [SerializeField] private TextMeshPro numberLabel;
        [SerializeField] private TextMeshPro subtitleLabel;

        private bool hasCachedState;
        private bool lastVisibleState;

        public int TableNumber => tableNumber;

        private void Awake()
        {
            ApplyVisibility(true);
        }

        private void OnEnable()
        {
            ApplyVisibility(true);
        }

        private void Start()
        {
            ApplyVisibility(true);
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            bool shouldShow = ShouldShowSigns();
            if (!hasCachedState || shouldShow != lastVisibleState)
            {
                ApplyVisibility(true);
            }
        }

        public void Configure(int number, GameObject visuals, TextMeshPro label, TextMeshPro subtitle)
        {
            tableNumber = number;
            visualsRoot = visuals;
            numberLabel = label;
            subtitleLabel = subtitle;
            UpdateLabelText();
            ApplyVisibility(true);
        }

        public void UpdateLabelText()
        {
            if (subtitleLabel != null)
            {
                subtitleLabel.text = "MESA";
            }
            if (numberLabel != null)
            {
                numberLabel.text = tableNumber.ToString();
            }
        }

        public static bool ShouldShowSigns()
        {
            if (!Application.isPlaying)
            {
                return true;
            }

            if (NetworkUI.selectedMode == GameMode.Host || NetworkUI.selectedMode == GameMode.Client)
            {
                return false;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                if (NetworkUI.selectedMode != GameMode.Singleplayer)
                {
                    if (GameServer.Instance != null && !GameServer.Instance.ShouldAutoFillBot())
                    {
                        return false;
                    }
                }
            }

            if (GameServer.Instance != null && !GameServer.Instance.ShouldAutoFillBot())
            {
                return false;
            }

            return true;
        }

        public void ApplyVisibility(bool force = false)
        {
            bool shouldShow = ShouldShowSigns();
            if (!force && hasCachedState && shouldShow == lastVisibleState) return;

            hasCachedState = true;
            lastVisibleState = shouldShow;

            GameObject target = visualsRoot != null ? visualsRoot : FindVisualsChild();
            if (target != null && target.activeSelf != shouldShow)
            {
                target.SetActive(shouldShow);
            }
        }

        private GameObject FindVisualsChild()
        {
            Transform child = transform.Find("Visuals");
            if (child != null)
            {
                visualsRoot = child.gameObject;
                return visualsRoot;
            }
            return null;
        }

        public static void RefreshAllVisibility()
        {
            TableNumberSignController[] signs = FindObjectsByType<TableNumberSignController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (TableNumberSignController sign in signs)
            {
                if (sign != null)
                {
                    sign.ApplyVisibility(true);
                }
            }
        }
    }
}
