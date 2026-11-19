using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Controla el tablero 3D de la pared del bar que muestra el progreso contra los 8 gauchos
    /// y mantiene el suspenso del Duelo Final contra El Patrón (Bartender) en la Mesa 9.
    /// Se actualiza automáticamente ante cualquier cambio de progresión.
    /// </summary>
    public class RivalBoardController : MonoBehaviour
    {
        private const string PrefKeyPrefix = "Trucardo_Defeated_Mesa_";
        private const int RegularBotsCount = GameProgressionManager.RegularBotsCount;

        [Header("Referencias de Texto 3D")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text[] rowNameTexts = new TMP_Text[RegularBotsCount];
        [SerializeField] private TMP_Text[] rowStatusTexts = new TMP_Text[RegularBotsCount];
        [SerializeField] private TMP_Text bossTitleText;
        [SerializeField] private TMP_Text bossDetailText;

        [Header("Detalles Visuales")]
        [SerializeField] private Renderer bossPlaqueRenderer;
        [SerializeField] private Color bossPlaqueLockedColor = new Color(0.62f, 0.40f, 0.32f, 1f);
        [SerializeField] private Color bossPlaqueUnlockedColor = new Color(1.00f, 0.68f, 0.40f, 1f);

        private GameProgressionManager subscribedManager;
        private MaterialPropertyBlock bossPlaquePropBlock;
        private int lastDefeatedMask = -1;
        private float nextPollTime = 0f;

        private void Awake()
        {
            ResolveMissingReferences();
        }

        private void OnEnable()
        {
            ResolveMissingReferences();
            if (Application.isPlaying)
            {
                TrySubscribeToProgression();
            }
            RefreshBoard();
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                TrySubscribeToProgression();
            }
            RefreshBoard();
        }

        private void OnDisable()
        {
            UnsubscribeFromProgression();
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (subscribedManager == null)
            {
                TrySubscribeToProgression();
            }

            if (Time.unscaledTime >= nextPollTime)
            {
                nextPollTime = Time.unscaledTime + 0.4f;
                int currentMask = ComputeDefeatedMask();
                if (currentMask != lastDefeatedMask)
                {
                    RefreshBoard();
                }
            }
        }

        public void ConfigureReferences(
            TMP_Text title,
            TMP_Text progress,
            TMP_Text[] rowNames,
            TMP_Text[] rowStatuses,
            TMP_Text bossTitle,
            TMP_Text bossDetail,
            Renderer bossPlaque)
        {
            titleText = title;
            progressText = progress;
            rowNameTexts = rowNames;
            rowStatusTexts = rowStatuses;
            bossTitleText = bossTitle;
            bossDetailText = bossDetail;
            bossPlaqueRenderer = bossPlaque;
        }

        private void ResolveMissingReferences()
        {
            if (titleText == null) titleText = FindChildTMP("Texto_Titulo");
            if (progressText == null) progressText = FindChildTMP("Texto_Progreso");
            if (bossTitleText == null) bossTitleText = FindChildTMP("Texto_Jefe_Titulo");
            if (bossDetailText == null) bossDetailText = FindChildTMP("Texto_Jefe_Detalle");

            if (rowNameTexts == null || rowNameTexts.Length != RegularBotsCount)
            {
                rowNameTexts = new TMP_Text[RegularBotsCount];
            }
            if (rowStatusTexts == null || rowStatusTexts.Length != RegularBotsCount)
            {
                rowStatusTexts = new TMP_Text[RegularBotsCount];
            }

            for (int i = 0; i < RegularBotsCount; i++)
            {
                int tableNum = i + 1;
                if (rowNameTexts[i] == null) rowNameTexts[i] = FindChildTMP($"Fila_{tableNum}_Nombre");
                if (rowStatusTexts[i] == null) rowStatusTexts[i] = FindChildTMP($"Fila_{tableNum}_Estado");
            }

            if (bossPlaqueRenderer == null)
            {
                Transform plaque = FindChildRecursive(transform, "Placa_Patron");
                if (plaque != null) bossPlaqueRenderer = plaque.GetComponent<Renderer>();
            }
        }

        private TMP_Text FindChildTMP(string childName)
        {
            Transform found = FindChildRecursive(transform, childName);
            return found != null ? found.GetComponent<TMP_Text>() : null;
        }

        private static Transform FindChildRecursive(Transform root, string targetName)
        {
            if (root == null) return null;
            if (root.name == targetName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindChildRecursive(root.GetChild(i), targetName);
                if (match != null) return match;
            }
            return null;
        }

        private void TrySubscribeToProgression()
        {
            GameProgressionManager manager = GameProgressionManager.Instance;
            if (manager == null || manager == subscribedManager) return;

            UnsubscribeFromProgression();
            subscribedManager = manager;
            subscribedManager.OnBotDefeated += HandleBotDefeated;
            subscribedManager.OnAllBotsDefeated += HandleProgressionChanged;
            subscribedManager.OnProgressionUpdated += HandleProgressionChanged;
        }

        private void UnsubscribeFromProgression()
        {
            if (subscribedManager == null) return;

            subscribedManager.OnBotDefeated -= HandleBotDefeated;
            subscribedManager.OnAllBotsDefeated -= HandleProgressionChanged;
            subscribedManager.OnProgressionUpdated -= HandleProgressionChanged;
            subscribedManager = null;
        }

        private void HandleBotDefeated(int tableNumber, string botName)
        {
            RefreshBoard();
        }

        private void HandleProgressionChanged()
        {
            RefreshBoard();
        }

        private int ComputeDefeatedMask()
        {
            int mask = 0;
            for (int i = 1; i <= RegularBotsCount; i++)
            {
                if (IsTableDefeated(i))
                {
                    mask |= (1 << (i - 1));
                }
            }
            return mask;
        }

        private bool IsTableDefeated(int tableNumber)
        {
            if (Application.isPlaying && subscribedManager != null)
            {
                return subscribedManager.IsBotDefeated(tableNumber);
            }
            return PlayerPrefs.GetInt(PrefKeyPrefix + tableNumber, 0) == 1;
        }

        private static string GetBotDisplayName(int tableNumber)
        {
            return GameProgressionManager.GetBotDisplayName(tableNumber);
        }

        [ContextMenu("Actualizar Tablero Ahora")]
        public void RefreshBoard()
        {
            ResolveMissingReferences();

            int defeatedCount = 0;
            int mask = 0;

            for (int i = 1; i <= RegularBotsCount; i++)
            {
                bool defeated = IsTableDefeated(i);
                if (defeated)
                {
                    defeatedCount++;
                    mask |= (1 << (i - 1));
                }

                string botName = GetBotDisplayName(i);
                TMP_Text nameTxt = rowNameTexts[i - 1];
                TMP_Text statusTxt = rowStatusTexts[i - 1];

                if (nameTxt != null)
                {
                    if (defeated)
                    {
                        nameTxt.text = $"[X] {i}. <s>{botName}</s>";
                        nameTxt.color = new Color(0.69f, 0.61f, 0.50f, 1f);
                    }
                    else
                    {
                        nameTxt.text = $"[ ] {i}. {botName}";
                        nameTxt.color = new Color(0.97f, 0.91f, 0.77f, 1f);
                    }
                }

                if (statusTxt != null)
                {
                    if (defeated)
                    {
                        statusTxt.text = "DERROTADO";
                        statusTxt.color = new Color(0.55f, 0.90f, 0.45f, 1f);
                    }
                    else
                    {
                        statusTxt.text = $"MESA {i}";
                        statusTxt.color = new Color(0.94f, 0.75f, 0.39f, 1f);
                    }
                }
            }

            lastDefeatedMask = mask;
            bool bossUnlocked = defeatedCount >= RegularBotsCount;
            int remaining = RegularBotsCount - defeatedCount;

            if (titleText != null)
            {
                titleText.text = "DUELOS DE LA PULPERÍA";
                titleText.color = new Color(0.98f, 0.87f, 0.58f, 1f);
            }

            if (progressText != null)
            {
                if (bossUnlocked)
                {
                    progressText.text = "¡LOS 8 PARROQUIANOS DERROTADOS (8 / 8)!";
                    progressText.color = new Color(1f, 0.85f, 0.34f, 1f);
                }
                else
                {
                    progressText.text = $"Rivales derrotados: {defeatedCount} / {RegularBotsCount}";
                    progressText.color = new Color(0.94f, 0.85f, 0.68f, 1f);
                }
            }

            if (bossTitleText != null)
            {
                if (bossUnlocked)
                {
                    bossTitleText.text = "¡EL PATRÓN TE ESPERA EN MESA 9!";
                    bossTitleText.color = new Color(1f, 0.84f, 0.30f, 1f);
                }
                else
                {
                    bossTitleText.text = "???  -  EL PATRÓN (MESA 9)  -  ???";
                    bossTitleText.color = new Color(0.96f, 0.62f, 0.30f, 1f);
                }
            }

            if (bossDetailText != null)
            {
                if (bossUnlocked)
                {
                    bossDetailText.text = "El duelo final de la pulpería está abierto.\nAcércate a la Mesa 9 y reclama el título.";
                    bossDetailText.color = new Color(0.99f, 0.93f, 0.80f, 1f);
                }
                else if (defeatedCount == 0)
                {
                    bossDetailText.text = "El último rival aguarda entre las sombras...\nDerrota a los 8 gauchos para desafiarlo.";
                    bossDetailText.color = new Color(0.90f, 0.80f, 0.64f, 1f);
                }
                else if (remaining == 1)
                {
                    bossDetailText.text = "¡Solo queda UN parroquiano en pie!\nEl Patrón ya afila el mazo en la oscuridad...";
                    bossDetailText.color = new Color(0.98f, 0.78f, 0.48f, 1f);
                }
                else
                {
                    bossDetailText.text = $"El dueño aún observa detrás del mostrador...\nVence a {remaining} gauchos más para enfrentarlo.";
                    bossDetailText.color = new Color(0.92f, 0.82f, 0.66f, 1f);
                }
            }

            if (Application.isPlaying && bossPlaqueRenderer != null)
            {
                if (bossPlaquePropBlock == null) bossPlaquePropBlock = new MaterialPropertyBlock();
                bossPlaqueRenderer.GetPropertyBlock(bossPlaquePropBlock);
                bossPlaquePropBlock.SetColor("_BaseColor", bossUnlocked ? bossPlaqueUnlockedColor : bossPlaqueLockedColor);
                bossPlaqueRenderer.SetPropertyBlock(bossPlaquePropBlock);
            }
        }
    }
}
