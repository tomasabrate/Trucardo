using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Trucardo.Core.UI
{
    /// <summary>
    /// Interfaz visual que muestra la línea temporal de poderes activados en la ronda actual en el sector izquierdo de la pantalla.
    /// Acumula cronológicamente los poderes jugados tanto por el jugador local como por el rival, reiniciándose cada ronda.
    /// </summary>
    public class RoundPowersUI : MonoBehaviour
    {
        [Header("Contenedor Principal")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text headerText;

        private readonly List<GameObject> timelineCards = new List<GameObject>();
        private bool isSeated;
        private bool matchFinished;

        private Sprite cachedWoodSprite;
        private TMP_FontAsset cachedFontAsset;
        private Material cachedFontMaterial;

        public void Initialize(Button templateButton, Canvas hudCanvas)
        {
            EnsureUI(templateButton, hudCanvas);
            UpdateVisibility();
        }

        public void AddPower(PowerData power, bool isLocal)
        {
            if (power == null) return;
            EnsureUI(null, null);

            if (panelRoot == null) return;

            GameObject cardObj = CreateTimelineCard(power, isLocal);
            if (cardObj != null)
            {
                timelineCards.Add(cardObj);
                cardObj.SetActive(true);
                if (gameObject.activeInHierarchy)
                {
                    StartCoroutine(PunchAnimation(cardObj.transform));
                }
            }

            UpdateVisibility();
        }

        public void SetLocalPower(PowerData power)
        {
            if (power != null) AddPower(power, true);
        }

        public void SetOpponentPower(PowerData power)
        {
            if (power != null) AddPower(power, false);
        }

        public void ClearPowers()
        {
            foreach (GameObject card in timelineCards)
            {
                if (card != null) Destroy(card);
            }
            timelineCards.Clear();
            UpdateVisibility();
        }

        public void UpdateGameState(bool seated, bool finished)
        {
            isSeated = seated;
            matchFinished = finished;
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            bool shouldShow = isSeated && !matchFinished && timelineCards.Count > 0;
            if (panelRoot != null)
            {
                panelRoot.SetActive(shouldShow);
            }
        }

        private GameObject CreateTimelineCard(PowerData power, bool isLocal)
        {
            GameObject cardObj = new GameObject($"PowerCard_{timelineCards.Count}_{power.PowerId}", 
                typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Outline));
            cardObj.transform.SetParent(panelRoot.transform, false);

            LayoutElement le = cardObj.GetComponent<LayoutElement>();
            le.preferredHeight = 126f;
            le.minHeight = 126f;
            le.preferredWidth = 340f;

            Image img = cardObj.GetComponent<Image>();
            img.sprite = cachedWoodSprite;
            img.color = isLocal 
                ? new Color(0.42f, 0.26f, 0.16f, 0.98f)   // Madera oscura cálida con tinte criollo
                : new Color(0.38f, 0.18f, 0.14f, 0.98f);  // Caoba rojizo para el rival
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            // Borde / Filete criollo decorativo
            Outline outline = cardObj.GetComponent<Outline>();
            outline.effectColor = isLocal 
                ? new Color(0.92f, 0.74f, 0.32f, 0.85f)  // Dorado criollo
                : new Color(0.95f, 0.40f, 0.22f, 0.85f); // Naranja brasa
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Contenedor interno con VerticalLayoutGroup para garantizar alineación perfecta
            GameObject contentObj = new GameObject("CardContent", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentObj.transform.SetParent(cardObj.transform, false);
            RectTransform contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.offsetMin = new Vector2(18f, 10f);
            contentRt.offsetMax = new Vector2(-18f, -10f);

            VerticalLayoutGroup vlg = contentObj.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 3f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 1. Etiqueta de Autor ("★ VOS ACTIVASTE" vs "☠ RIVAL ACTIVÓ")
            GameObject tagObj = new GameObject("TagText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            tagObj.transform.SetParent(contentObj.transform, false);
            LayoutElement tagLe = tagObj.GetComponent<LayoutElement>();
            tagLe.preferredHeight = 16f;
            tagLe.minHeight = 16f;

            TMP_Text tagTmp = tagObj.GetComponent<TextMeshProUGUI>();
            tagTmp.font = cachedFontAsset;
            if (cachedFontMaterial != null) tagTmp.fontSharedMaterial = cachedFontMaterial;
            tagTmp.text = isLocal ? "★ VOS ACTIVASTE" : "☠ RIVAL ACTIVÓ";
            tagTmp.fontSize = 12f;
            tagTmp.fontStyle = FontStyles.Bold;
            tagTmp.characterSpacing = 1.2f;
            tagTmp.color = isLocal 
                ? new Color(1f, 0.85f, 0.45f, 1f)  // Dorado cálido
                : new Color(1f, 0.50f, 0.35f, 1f); // Naranja / rojizo
            tagTmp.alignment = TextAlignmentOptions.Left;
            tagTmp.raycastTarget = false;

            // 2. Nombre del Poder
            GameObject nameObj = new GameObject("PowerNameText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            nameObj.transform.SetParent(contentObj.transform, false);
            LayoutElement nameLe = nameObj.GetComponent<LayoutElement>();
            nameLe.preferredHeight = 24f;
            nameLe.minHeight = 22f;

            TMP_Text nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
            nameTmp.font = cachedFontAsset;
            if (cachedFontMaterial != null) nameTmp.fontSharedMaterial = cachedFontMaterial;
            nameTmp.text = power.DisplayName.ToUpper();
            nameTmp.fontSize = 17f;
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = new Color(1f, 0.98f, 0.92f, 1f);
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.raycastTarget = false;

            // 3. Descripción breve del efecto
            GameObject descObj = new GameObject("PowerDescText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            descObj.transform.SetParent(contentObj.transform, false);
            LayoutElement descLe = descObj.GetComponent<LayoutElement>();
            descLe.preferredHeight = 58f;
            descLe.minHeight = 44f;

            TMP_Text descTmp = descObj.GetComponent<TextMeshProUGUI>();
            descTmp.font = cachedFontAsset;
            if (cachedFontMaterial != null) descTmp.fontSharedMaterial = cachedFontMaterial;
            descTmp.text = power.GetEffectiveShortDescription();
            descTmp.fontSize = 13f;
            descTmp.fontStyle = FontStyles.Normal;
            descTmp.enableAutoSizing = true;
            descTmp.fontSizeMin = 10.5f;
            descTmp.fontSizeMax = 13.5f;
            descTmp.textWrappingMode = TextWrappingModes.Normal;
            descTmp.overflowMode = TextOverflowModes.Ellipsis;
            descTmp.color = new Color(0.92f, 0.90f, 0.84f, 1f);
            descTmp.alignment = TextAlignmentOptions.TopLeft;
            descTmp.raycastTarget = false;

            return cardObj;
        }

        private IEnumerator PunchAnimation(Transform target)
        {
            if (target == null) yield break;

            Vector3 originalScale = Vector3.one;
            float elapsed = 0f;
            float duration = 0.28f;

            target.localScale = originalScale * 0.75f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scaleFactor = Mathf.Sin(t * Mathf.PI * 0.8f) * 0.22f + 1f;
                if (t >= 0.75f)
                {
                    scaleFactor = Mathf.Lerp(scaleFactor, 1f, (t - 0.75f) / 0.25f);
                }
                target.localScale = originalScale * scaleFactor;
                yield return null;
            }

            target.localScale = originalScale;
        }

        public void EnsureUI(Button templateButton, Canvas hudCanvas)
        {
            if (panelRoot != null) return;

            Transform canvasTransform = hudCanvas != null ? hudCanvas.transform : null;
            if (canvasTransform == null)
            {
                Canvas anyCanvas = FindAnyObjectByType<Canvas>();
                if (anyCanvas != null) canvasTransform = anyCanvas.transform;
                else canvasTransform = transform;
            }

            Transform existing = canvasTransform.Find("ActivePowersPanel");
            if (existing != null)
            {
                panelRoot = existing.gameObject;
                RectTransform extPanelRect = panelRoot.GetComponent<RectTransform>();
                if (extPanelRect != null)
                {
                    extPanelRect.anchoredPosition = new Vector2(25f, 20f);
                    extPanelRect.sizeDelta = new Vector2(340f, 280f);
                }
                return;
            }

            if (templateButton != null)
            {
                if (templateButton.image != null) cachedWoodSprite = templateButton.image.sprite;
                TMP_Text btnText = templateButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    cachedFontAsset = btnText.font;
                    cachedFontMaterial = btnText.fontSharedMaterial;
                }
            }

            if (cachedWoodSprite == null)
            {
                Button anyBtn = FindAnyObjectByType<Button>();
                if (anyBtn != null && anyBtn.image != null) cachedWoodSprite = anyBtn.image.sprite;
            }

            if (cachedFontAsset == null)
            {
                TMP_Text anyText = FindAnyObjectByType<TMP_Text>();
                if (anyText != null)
                {
                    cachedFontAsset = anyText.font;
                    cachedFontMaterial = anyText.fontSharedMaterial;
                }
            }

            // Contenedor Principal (Panel izquierdo en posición fija)
            GameObject panelObj = new GameObject("ActivePowersPanel", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panelObj.transform.SetParent(canvasTransform, false);
            panelRoot = panelObj;

            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0.5f);
            panelRect.anchorMax = new Vector2(0f, 0.5f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(25f, 20f);
            panelRect.sizeDelta = new Vector2(340f, 280f);

            VerticalLayoutGroup vlg = panelObj.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = panelObj.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Encabezado "PODERES DE LA RONDA"
            GameObject headerObj = new GameObject("Header", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            headerObj.transform.SetParent(panelObj.transform, false);

            LayoutElement headerLe = headerObj.GetComponent<LayoutElement>();
            headerLe.preferredHeight = 32f;
            headerLe.minHeight = 32f;

            Image headerImg = headerObj.GetComponent<Image>();
            headerImg.sprite = cachedWoodSprite;
            headerImg.color = new Color(0.26f, 0.16f, 0.10f, 0.98f);
            headerImg.type = Image.Type.Simple;
            headerImg.raycastTarget = false;

            GameObject headerTextObj = new GameObject("HeaderText", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerTextObj.transform.SetParent(headerObj.transform, false);
            RectTransform headerTextRect = headerTextObj.GetComponent<RectTransform>();
            headerTextRect.anchorMin = Vector2.zero;
            headerTextRect.anchorMax = Vector2.one;
            headerTextRect.offsetMin = Vector2.zero;
            headerTextRect.offsetMax = Vector2.zero;

            headerText = headerTextObj.GetComponent<TextMeshProUGUI>();
            headerText.font = cachedFontAsset;
            if (cachedFontMaterial != null) headerText.fontSharedMaterial = cachedFontMaterial;
            headerText.text = "🔮 PODERES EN JUEGO";
            headerText.fontSize = 14f;
            headerText.fontStyle = FontStyles.Bold;
            headerText.characterSpacing = 1.5f;
            headerText.color = new Color(0.96f, 0.88f, 0.72f, 1f);
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.raycastTarget = false;

            panelObj.SetActive(false);
        }
    }
}
