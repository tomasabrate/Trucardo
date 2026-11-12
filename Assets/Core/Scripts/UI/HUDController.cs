using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Trucardo.Core.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace Trucardo.Core.UI
{
    public sealed class HUDController : MonoBehaviour
    {
        private static HUDController instance;

        [SerializeField] private Canvas hudCanvas;
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private Transform playerMalasTally;
        [SerializeField] private Transform playerBuenasTally;
        [SerializeField] private TMP_Text playerTotalText;
        [SerializeField] private Transform opponentMalasTally;
        [SerializeField] private Transform opponentBuenasTally;
        [SerializeField] private TMP_Text opponentTotalText;
        [SerializeField] private TMP_Text opponentNameText;
        [SerializeField] private GameObject actionBarRoot;
        [SerializeField] private GameObject envidoPanelRoot;
        [SerializeField] private GameObject responsePanelRoot;
        [SerializeField] private Button trucoButton;
        [SerializeField] private Button envidoButton;
        [SerializeField] private Button salirDelMazoButton;
        [SerializeField] private Button cantarEnvidoButton;
        [SerializeField] private Button realEnvidoButton;
        [SerializeField] private Button faltaEnvidoButton;
        [SerializeField] private Button volverButton;
        [SerializeField] private Button quieroButton;
        [SerializeField] private Button noQuieroButton;
        [SerializeField] private RoundPowersUI roundPowersUI;
        [SerializeField, Min(0f)] private float powerBannerTextSpacing = 0f;

        private PlayerController localPlayer;
        private bool isSeated;
        private bool interfaceMode;
        private bool envidoPanelOpen;
        private bool isLocalTurn;
        private bool needsResponse;
        private bool canCallEnvido;
        private bool canCallRealEnvido;
        private bool canCallFaltaEnvido;
        private bool matchFinished;
        private bool localPlayerWon;
        private bool postGameOpen;
        private Canvas endGameCanvas;
        private GameObject victoryPanelRoot;
        private GameObject defeatPanelRoot;
        private GameObject postGamePanelRoot;
        private GameObject visibleEndGamePanel;
        private Button victoryNextButton;
        private Button defeatNextButton;
        private Button rematchButton;
        private Button leaveTableButton;
        private GameObject[] playerMalasMarks;
        private GameObject[] playerBuenasMarks;
        private GameObject[] opponentMalasMarks;
        private GameObject[] opponentBuenasMarks;

        private GameObject scoreboardRoot;
        private Coroutine scoreboardCoroutine;
        private bool scoreboardTemporarilyVisible;

        private GameObject tabHintRoot;
        private TMP_Text tabHintText;

        private GameObject manoEnJuegoRoot;
        private TMP_Text manoEnJuegoText;

        private GameObject cantoAnnouncementRoot;
        private TMP_Text cantoAnnouncementText;

        private GameObject responseDetailsRoot;
        private TMP_Text responseDetailsText;

        private NivelTruco currentTrucoLevel = NivelTruco.Ninguno;
        private bool isWaitingCanto = false;
        private bool isWaitingLocalCaller = false;
        private TipoCanto currentPendingCanto = TipoCanto.Ninguno;
        private EstadoEnvido currentEnvidoState = EstadoEnvido.Ninguno;
        private int currentPendingEnvidoPoints = 0;
        private int currentEnvidoAcceptedPoints = 0;

        private bool bastonPresidencialActiveOnMe = false;
        private bool pavaCalienteActive = false;
        private float pavaTimeRemaining = 5.0f;

        private GameObject pavaTimerRoot;
        private TMP_Text pavaTimerText;

        private GameObject toastRoot;
        private CanvasGroup toastCanvasGroup;
        private TMP_Text toastTitleText;
        private TMP_Text toastMessageText;
        private Coroutine toastCoroutine;

        private GameObject guampasChoiceRoot;
        private TMP_Text guampasDrawnCardNameText;
        private TMP_Text guampasDrawnCardDetailsText;
        private Transform guampasButtonsContainer;
        private Button guampasCancelButton;
        private Action<int> onGuampasChoiceCallback;
        private List<GameObject> spawnedGuampasButtons = new List<GameObject>();

        private GameObject powerPromptRoot;
        private TMP_Text powerPromptTitle;
        private TMP_Text powerPromptInstruction;

        public static HUDController Instance => instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogError("Hay más de un HUDController activo en la escena.", this);
                enabled = false;
                return;
            }

            instance = this;
            BindEndGameUI();
            RegisterButtonActions();
            ValidateReferences();
            playerMalasMarks = BuildTallyMarks(playerMalasTally, 1);
            playerBuenasMarks = BuildTallyMarks(playerBuenasTally, 16);
            opponentMalasMarks = BuildTallyMarks(opponentMalasTally, 1);
            opponentBuenasMarks = BuildTallyMarks(opponentBuenasTally, 16);

            if (salirDelMazoButton != null)
                salirDelMazoButton.interactable = false;

            if (roundPowersUI == null)
            {
                roundPowersUI = GetComponentInChildren<RoundPowersUI>(true);
                if (roundPowersUI == null)
                {
                    GameObject powersObj = new GameObject("RoundPowersUI", typeof(RectTransform));
                    powersObj.transform.SetParent(hudCanvas != null ? hudCanvas.transform : transform, false);
                    roundPowersUI = powersObj.AddComponent<RoundPowersUI>();
                }
            }

            if (roundPowersUI != null)
            {
                roundPowersUI.Initialize(trucoButton, hudCanvas);
            }

            UpdateVisibility();
        }

        private void OnDestroy()
        {
            UnregisterButtonActions();
            if (instance == this)
            {
                UIInteractionAudio.SetEndGameAudioActive(false);
                instance = null;
            }
        }

        private void OnDisable()
        {
            if (instance != this)
                return;

            visibleEndGamePanel = null;
            UIInteractionAudio.SetEndGameAudioActive(false);
        }

        private void LateUpdate()
        {
            UpdateEndGameAudio();
        }

        public void AttachPlayer(PlayerController player)
        {
            if (player == null || !player.IsOwner)
                return;

            localPlayer = player;
            isSeated = false;
            interfaceMode = false;
            envidoPanelOpen = false;
            matchFinished = false;
            postGameOpen = false;
            UpdateVisibility();
        }

        public void DetachPlayer(PlayerController player)
        {
            if (localPlayer != player)
                return;

            localPlayer = null;
            isSeated = false;
            interfaceMode = false;
            envidoPanelOpen = false;
            matchFinished = false;
            localPlayerWon = false;
            postGameOpen = false;
            bastonPresidencialActiveOnMe = false;
            pavaCalienteActive = false;
            scoreboardTemporarilyVisible = false;
            if (scoreboardCoroutine != null)
            {
                StopCoroutine(scoreboardCoroutine);
                scoreboardCoroutine = null;
            }
            if (pavaTimerRoot != null) pavaTimerRoot.SetActive(false);
            if (toastRoot != null) toastRoot.SetActive(false);
            if (tabHintRoot != null) tabHintRoot.SetActive(false);
            if (manoEnJuegoRoot != null) manoEnJuegoRoot.SetActive(false);
            if (cantoAnnouncementRoot != null) cantoAnnouncementRoot.SetActive(false);
            if (responseDetailsRoot != null) responseDetailsRoot.SetActive(false);
            if (responseDetailsText != null) responseDetailsText.gameObject.SetActive(false);
            CloseGuampasChoice();
            CloseGuitarraChoice();
            ClearRoundPowers();
            UpdateVisibility();
        }

        public void SetPlayerSeated(bool seated)
        {
            isSeated = seated;
            if (!seated)
            {
                interfaceMode = false;
                envidoPanelOpen = false;
                bastonPresidencialActiveOnMe = false;
                pavaCalienteActive = false;
                scoreboardTemporarilyVisible = false;
                if (scoreboardCoroutine != null)
                {
                    StopCoroutine(scoreboardCoroutine);
                    scoreboardCoroutine = null;
                }
                if (pavaTimerRoot != null) pavaTimerRoot.SetActive(false);
                if (toastRoot != null) toastRoot.SetActive(false);
                if (tabHintRoot != null) tabHintRoot.SetActive(false);
                if (manoEnJuegoRoot != null) manoEnJuegoRoot.SetActive(false);
                if (cantoAnnouncementRoot != null) cantoAnnouncementRoot.SetActive(false);
                if (responseDetailsRoot != null) responseDetailsRoot.SetActive(false);
                if (responseDetailsText != null) responseDetailsText.gameObject.SetActive(false);
                CloseGuampasChoice();
                CloseGuitarraChoice();
                ClearRoundPowers();
            }

            UpdateVisibility();
        }

        public void SetInterfaceMode(bool enabled)
        {
            interfaceMode = isSeated && enabled;
            if (!interfaceMode)
                envidoPanelOpen = false;

            UpdateVisibility();
        }

        public void Refresh(
            string turnMessage,
            int playerScore,
            int opponentScore,
            bool localTurn,
            bool responseRequired,
            bool allowEnvido,
            bool allowRealEnvido,
            bool allowFaltaEnvido,
            bool gameFinished,
            bool playerWon,
            string opponentName = null,
            NivelTruco trucoLevel = NivelTruco.Ninguno,
            bool waitingCanto = false,
            bool isLocalCaller = false,
            TipoCanto pendingCanto = TipoCanto.Ninguno,
            EstadoEnvido envidoState = EstadoEnvido.Ninguno,
            int pendingEnvidoPoints = 0,
            int envidoAcceptedPoints = 0)
        {
            if (turnText != null)
                turnText.text = turnMessage;

            UpdateScore(playerMalasMarks, playerBuenasMarks, playerTotalText, playerScore);
            UpdateScore(opponentMalasMarks, opponentBuenasMarks, opponentTotalText, opponentScore);

            if (opponentNameText == null && opponentMalasTally != null && opponentMalasTally.parent != null)
            {
                var texts = opponentMalasTally.parent.GetComponentsInChildren<TMP_Text>(true);
                foreach (var t in texts)
                {
                    if (t.name == "PlayerName" || t.text.Equals("Rival", StringComparison.OrdinalIgnoreCase))
                    {
                        opponentNameText = t;
                        break;
                    }
                }
            }
            if (opponentNameText != null)
            {
                opponentNameText.text = !string.IsNullOrEmpty(opponentName) ? opponentName : "Rival";
                opponentNameText.enableAutoSizing = true;
                opponentNameText.fontSizeMin = 10f;
                opponentNameText.fontSizeMax = 36f;
                opponentNameText.textWrappingMode = TextWrappingModes.NoWrap;
                opponentNameText.overflowMode = TextOverflowModes.Ellipsis;
                RectTransform rt = opponentNameText.rectTransform;
                if (rt != null && rt.sizeDelta.x < 130f)
                {
                    rt.sizeDelta = new Vector2(135f, Mathf.Max(rt.sizeDelta.y, 14f));
                }
            }

            if (localTurn != isLocalTurn || responseRequired != needsResponse)
            {
                pavaTimeRemaining = 5.0f;
            }

            isLocalTurn = localTurn;
            needsResponse = responseRequired;
            canCallEnvido = allowEnvido;
            canCallRealEnvido = allowRealEnvido;
            canCallFaltaEnvido = allowFaltaEnvido;
            currentTrucoLevel = trucoLevel;
            isWaitingCanto = waitingCanto;
            isWaitingLocalCaller = isLocalCaller;
            currentPendingCanto = pendingCanto;
            currentEnvidoState = envidoState;
            currentPendingEnvidoPoints = pendingEnvidoPoints;
            currentEnvidoAcceptedPoints = envidoAcceptedPoints;

            if (gameFinished && !matchFinished)
                postGameOpen = false;
            matchFinished = gameFinished;
            localPlayerWon = playerWon;
            UpdateVisibility();
        }

        public void ShowScoreboardTemporarily(float duration = 5.0f)
        {
            if (scoreboardCoroutine != null)
            {
                StopCoroutine(scoreboardCoroutine);
            }
            if (gameObject.activeInHierarchy)
            {
                scoreboardCoroutine = StartCoroutine(ScoreboardTemporaryRoutine(duration));
            }
            else
            {
                scoreboardTemporarilyVisible = true;
            }
        }

        private IEnumerator ScoreboardTemporaryRoutine(float duration)
        {
            scoreboardTemporarilyVisible = true;
            UpdateVisibility();
            yield return new WaitForSeconds(duration);
            scoreboardTemporarilyVisible = false;
            scoreboardCoroutine = null;
            UpdateVisibility();
        }

        private void BindEndGameUI()
        {
            GameObject canvasObject = GameObject.Find("CanvasEndGame");
            Transform canvasTransform = canvasObject != null ? canvasObject.transform : null;
            if (canvasTransform != null)
            {
                endGameCanvas = canvasObject.GetComponent<Canvas>();
                victoryPanelRoot = FindChild(canvasTransform, "EndGameVictoryUI");
                defeatPanelRoot = FindChild(canvasTransform, "EndGameDefeatUI");
                postGamePanelRoot = FindChild(canvasTransform, "EndGamePostGame");
                victoryNextButton = FindButton(canvasTransform, "EndGameVictoryUI/ButtonSiguiente");
                defeatNextButton = FindButton(canvasTransform, "EndGameDefeatUI/ButtonSiguiente");
                rematchButton = FindButton(canvasTransform, "EndGamePostGame/ButtonRevancha");
                leaveTableButton = FindButton(canvasTransform, "EndGamePostGame/ButtonSalirDeMesa");
            }

            if (endGameCanvas == null || victoryPanelRoot == null || defeatPanelRoot == null
                || postGamePanelRoot == null || victoryNextButton == null || defeatNextButton == null
                || rematchButton == null || leaveTableButton == null)
            {
                Debug.LogError("CanvasEndGame no tiene la estructura o los botones requeridos.", this);
                return;
            }

            endGameCanvas.overrideSorting = true;
            endGameCanvas.sortingOrder = Mathf.Max(endGameCanvas.sortingOrder, 100);
            endGameCanvas.enabled = false;
            victoryPanelRoot.SetActive(false);
            defeatPanelRoot.SetActive(false);
            postGamePanelRoot.SetActive(false);
        }

        private static GameObject FindChild(Transform root, string path)
        {
            Transform child = root != null ? root.Find(path) : null;
            return child != null ? child.gameObject : null;
        }

        private static Button FindButton(Transform root, string path)
        {
            Transform child = root != null ? root.Find(path) : null;
            return child != null ? child.GetComponent<Button>() : null;
        }

        private void RegisterButtonActions()
        {
            if (trucoButton != null) trucoButton.onClick.AddListener(CallTruco);
            if (envidoButton != null) envidoButton.onClick.AddListener(OpenEnvidoPanel);
            if (cantarEnvidoButton != null) cantarEnvidoButton.onClick.AddListener(CallEnvidoOption);
            if (realEnvidoButton != null) realEnvidoButton.onClick.AddListener(CallRealEnvidoOption);
            if (faltaEnvidoButton != null) faltaEnvidoButton.onClick.AddListener(CallFaltaEnvidoOption);
            if (volverButton != null) volverButton.onClick.AddListener(CloseEnvidoPanel);
            if (quieroButton != null) quieroButton.onClick.AddListener(AcceptCall);
            if (noQuieroButton != null) noQuieroButton.onClick.AddListener(DeclineCall);
            if (victoryNextButton != null) victoryNextButton.onClick.AddListener(OpenPostGame);
            if (defeatNextButton != null) defeatNextButton.onClick.AddListener(OpenPostGame);
            if (rematchButton != null) rematchButton.onClick.AddListener(RestartMatch);
            if (leaveTableButton != null) leaveTableButton.onClick.AddListener(LeaveTable);
        }

        private void UnregisterButtonActions()
        {
            if (trucoButton != null) trucoButton.onClick.RemoveListener(CallTruco);
            if (envidoButton != null) envidoButton.onClick.RemoveListener(OpenEnvidoPanel);
            if (cantarEnvidoButton != null) cantarEnvidoButton.onClick.RemoveListener(CallEnvidoOption);
            if (realEnvidoButton != null) realEnvidoButton.onClick.RemoveListener(CallRealEnvidoOption);
            if (faltaEnvidoButton != null) faltaEnvidoButton.onClick.RemoveListener(CallFaltaEnvidoOption);
            if (volverButton != null) volverButton.onClick.RemoveListener(CloseEnvidoPanel);
            if (quieroButton != null) quieroButton.onClick.RemoveListener(AcceptCall);
            if (noQuieroButton != null) noQuieroButton.onClick.RemoveListener(DeclineCall);
            if (victoryNextButton != null) victoryNextButton.onClick.RemoveListener(OpenPostGame);
            if (defeatNextButton != null) defeatNextButton.onClick.RemoveListener(OpenPostGame);
            if (rematchButton != null) rematchButton.onClick.RemoveListener(RestartMatch);
            if (leaveTableButton != null) leaveTableButton.onClick.RemoveListener(LeaveTable);
        }

        private void ValidateReferences()
        {
            if (hudCanvas == null || turnText == null
                || playerMalasTally == null || playerBuenasTally == null || playerTotalText == null
                || opponentMalasTally == null || opponentBuenasTally == null || opponentTotalText == null
                || actionBarRoot == null || envidoPanelRoot == null || responsePanelRoot == null
                || salirDelMazoButton == null
                || trucoButton == null || envidoButton == null || cantarEnvidoButton == null || realEnvidoButton == null
                || faltaEnvidoButton == null || volverButton == null || quieroButton == null || noQuieroButton == null)
            {
                Debug.LogError("CanvasHUD tiene referencias de Inspector sin asignar.", this);
            }

            if ((playerMalasTally != null && playerMalasTally.childCount < 15)
                || (playerBuenasTally != null && playerBuenasTally.childCount < 15)
                || (opponentMalasTally != null && opponentMalasTally.childCount < 15)
                || (opponentBuenasTally != null && opponentBuenasTally.childCount < 15))
            {
                Debug.LogError("Cada contenedor MalasTally y BuenasTally necesita 15 marcas.", this);
            }
        }

        private GameObject[] BuildTallyMarks(Transform tally, int firstPoint)
        {
            GameObject[] marks = new GameObject[15];
            if (tally == null)
                return marks;

            for (int i = 0; i < tally.childCount; i++)
            {
                Transform mark = tally.GetChild(i);
                const string suffix = "Puntos";
                string markName = mark.name;
                if (!markName.EndsWith(suffix, StringComparison.Ordinal)
                    || !int.TryParse(markName.Substring(0, markName.Length - suffix.Length), out int point)
                    || point < firstPoint || point >= firstPoint + marks.Length)
                {
                    Debug.LogError($"Marca de puntaje con nombre inesperado: '{markName}'.", mark);
                    continue;
                }

                int index = point - firstPoint;
                if (marks[index] != null)
                {
                    Debug.LogError($"La marca '{point}Puntos' está duplicada en '{tally.name}'.", tally);
                    continue;
                }

                marks[index] = mark.gameObject;
            }

            for (int i = 0; i < marks.Length; i++)
            {
                if (marks[i] == null)
                    Debug.LogError($"Falta la marca '{firstPoint + i}Puntos' en '{tally.name}'.", tally);
            }

            return marks;
        }

        private void UpdateScore(GameObject[] malas, GameObject[] buenas, TMP_Text total, int score)
        {
            int cappedScore = Mathf.Clamp(score, 0, 30);
            if (total != null)
                total.text = cappedScore.ToString();

            SetTally(malas, Mathf.Min(cappedScore, 15));
            SetTally(buenas, Mathf.Max(0, cappedScore - 15));
        }

        private static void SetTally(GameObject[] marks, int points)
        {
            if (marks == null)
                return;

            for (int i = 0; i < marks.Length; i++)
            {
                if (marks[i] != null)
                    marks[i].SetActive(i < points);
            }
        }

        private Sprite GetWoodSprite()
        {
            if (trucoButton != null && trucoButton.image != null)
                return trucoButton.image.sprite;
            return null;
        }

        private TMP_FontAsset GetFontAsset()
        {
            if (trucoButton != null)
            {
                TMP_Text btnText = trucoButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null) return btnText.font;
            }
            return null;
        }

        private Material GetFontMaterial()
        {
            if (trucoButton != null)
            {
                TMP_Text btnText = trucoButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null) return btnText.fontSharedMaterial;
            }
            return null;
        }

        private void EnsureScoreboardRoot()
        {
            if (scoreboardRoot != null) return;

            if (playerMalasTally != null)
            {
                Transform t = playerMalasTally;
                Transform canvasT = hudCanvas != null ? hudCanvas.transform : transform;
                while (t != null && t.parent != null && t.parent != canvasT)
                {
                    t = t.parent;
                }
                if (t != null && t != canvasT)
                {
                    scoreboardRoot = t.gameObject;
                }
            }

            if (scoreboardRoot == null && hudCanvas != null)
            {
                Transform found = hudCanvas.transform.Find("Scoreboard");
                if (found != null) scoreboardRoot = found.gameObject;
            }
        }

        private void EnsureTabHintUI()
        {
            if (tabHintRoot != null) return;

            Transform canvasTransform = hudCanvas != null ? hudCanvas.transform : transform;
            Transform existing = canvasTransform.Find("TabHintBanner");
            if (existing != null)
            {
                tabHintRoot = existing.gameObject;
                tabHintText = existing.GetComponentInChildren<TMP_Text>();
                return;
            }

            Sprite woodSprite = GetWoodSprite();
            TMP_FontAsset fontAsset = GetFontAsset();
            Material fontMat = GetFontMaterial();

            GameObject banner = new GameObject("TabHintBanner", typeof(RectTransform), typeof(Image));
            banner.transform.SetParent(canvasTransform, false);
            tabHintRoot = banner;

            RectTransform rt = banner.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(25f, 25f);
            rt.sizeDelta = new Vector2(240f, 44f);

            Image img = banner.GetComponent<Image>();
            img.sprite = woodSprite;
            img.color = new Color(0.24f, 0.14f, 0.09f, 0.95f);
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(banner.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10f, 0f);
            textRt.offsetMax = new Vector2(-10f, 0f);

            tabHintText = textObj.GetComponent<TextMeshProUGUI>();
            tabHintText.font = fontAsset;
            if (fontMat != null) tabHintText.fontSharedMaterial = fontMat;
            tabHintText.fontSize = 16f;
            tabHintText.alignment = TextAlignmentOptions.Center;
            tabHintText.color = new Color(0.96f, 0.92f, 0.85f, 1f);
            tabHintText.text = "<b>[Tab]</b> Abrir Menú UI";
            tabHintText.raycastTarget = false;

            banner.SetActive(false);
        }

        private void EnsureManoEnJuegoUI()
        {
            if (manoEnJuegoRoot != null) return;

            Transform canvasTransform = hudCanvas != null ? hudCanvas.transform : transform;
            Transform existing = canvasTransform.Find("ManoEnJuegoBanner");
            if (existing != null)
            {
                manoEnJuegoRoot = existing.gameObject;
                manoEnJuegoText = existing.GetComponentInChildren<TMP_Text>();
                return;
            }

            Sprite woodSprite = GetWoodSprite();
            TMP_FontAsset fontAsset = GetFontAsset();
            Material fontMat = GetFontMaterial();

            GameObject banner = new GameObject("ManoEnJuegoBanner", typeof(RectTransform), typeof(Image));
            banner.transform.SetParent(canvasTransform, false);
            manoEnJuegoRoot = banner;

            RectTransform rt = banner.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(25f, -25f);
            rt.sizeDelta = new Vector2(320f, 44f);

            Image img = banner.GetComponent<Image>();
            img.sprite = woodSprite;
            img.color = new Color(0.24f, 0.14f, 0.09f, 0.95f);
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(banner.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10f, 0f);
            textRt.offsetMax = new Vector2(-10f, 0f);

            manoEnJuegoText = textObj.GetComponent<TextMeshProUGUI>();
            manoEnJuegoText.font = fontAsset;
            if (fontMat != null) manoEnJuegoText.fontSharedMaterial = fontMat;
            manoEnJuegoText.fontSize = 17f;
            manoEnJuegoText.fontStyle = FontStyles.Bold;
            manoEnJuegoText.alignment = TextAlignmentOptions.Center;
            manoEnJuegoText.color = new Color(0.96f, 0.92f, 0.85f, 1f);
            manoEnJuegoText.text = "Mano en Juego: 1 pt (Simple)";
            manoEnJuegoText.raycastTarget = false;

            banner.SetActive(false);
        }

        private void EnsureCantoAnnouncementUI()
        {
            if (cantoAnnouncementRoot != null) return;

            Transform canvasTransform = hudCanvas != null ? hudCanvas.transform : transform;
            Transform existing = canvasTransform.Find("CantoAnnouncementBanner");
            if (existing != null)
            {
                cantoAnnouncementRoot = existing.gameObject;
                cantoAnnouncementText = existing.GetComponentInChildren<TMP_Text>();
                return;
            }

            Sprite woodSprite = GetWoodSprite();
            TMP_FontAsset fontAsset = GetFontAsset();
            Material fontMat = GetFontMaterial();

            GameObject banner = new GameObject("CantoAnnouncementBanner", typeof(RectTransform), typeof(Image));
            banner.transform.SetParent(canvasTransform, false);
            cantoAnnouncementRoot = banner;

            RectTransform rt = banner.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -110f);
            rt.sizeDelta = new Vector2(520f, 54f);

            Image img = banner.GetComponent<Image>();
            img.sprite = woodSprite;
            img.color = new Color(0.22f, 0.13f, 0.08f, 0.98f);
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(banner.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(16f, 0f);
            textRt.offsetMax = new Vector2(-16f, 0f);

            cantoAnnouncementText = textObj.GetComponent<TextMeshProUGUI>();
            cantoAnnouncementText.font = fontAsset;
            if (fontMat != null) cantoAnnouncementText.fontSharedMaterial = fontMat;
            cantoAnnouncementText.fontSize = 22f;
            cantoAnnouncementText.fontStyle = FontStyles.Bold;
            cantoAnnouncementText.alignment = TextAlignmentOptions.Center;
            cantoAnnouncementText.color = new Color(1f, 0.88f, 0.35f, 1f);
            cantoAnnouncementText.text = "";
            cantoAnnouncementText.raycastTarget = false;

            banner.SetActive(false);
        }

        private void EnsureResponseDetailsUI()
        {
            if (responseDetailsRoot != null || responsePanelRoot == null) return;

            Transform existing = responsePanelRoot.transform.Find("CantoBetDetailsBanner");
            if (existing != null)
            {
                responseDetailsRoot = existing.gameObject;
                responseDetailsText = existing.GetComponentInChildren<TMP_Text>();
                return;
            }

            Sprite woodSprite = GetWoodSprite();
            TMP_FontAsset fontAsset = GetFontAsset();
            Material fontMat = GetFontMaterial();

            GameObject banner = new GameObject("CantoBetDetailsBanner", typeof(RectTransform), typeof(Image));
            banner.transform.SetParent(responsePanelRoot.transform, false);
            responseDetailsRoot = banner;

            RectTransform rt = banner.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -145f);
            rt.sizeDelta = new Vector2(500f, 44f);

            Image img = banner.GetComponent<Image>();
            img.sprite = woodSprite;
            img.color = new Color(0.18f, 0.10f, 0.06f, 0.98f);
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(banner.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12f, 0f);
            textRt.offsetMax = new Vector2(-12f, 0f);

            responseDetailsText = textObj.GetComponent<TextMeshProUGUI>();
            responseDetailsText.font = fontAsset;
            if (fontMat != null) responseDetailsText.fontSharedMaterial = fontMat;
            responseDetailsText.fontSize = 20f;
            responseDetailsText.fontStyle = FontStyles.Bold;
            responseDetailsText.alignment = TextAlignmentOptions.Center;
            responseDetailsText.color = new Color(1f, 0.92f, 0.40f, 1f);
            responseDetailsText.raycastTarget = false;

            banner.SetActive(false);
        }

        private void FormatButton(Button btn, string label, bool active, bool interactable, float minWidth = 265f)
        {
            if (btn == null) return;
            btn.gameObject.SetActive(active);
            btn.interactable = interactable;

            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                txt.text = label;
                txt.enableAutoSizing = true;
                txt.fontSizeMin = 13f;
                txt.fontSizeMax = 22f;
                txt.textWrappingMode = TextWrappingModes.NoWrap;
                txt.overflowMode = TextOverflowModes.Ellipsis;
            }

            RectTransform rt = btn.GetComponent<RectTransform>();
            if (rt != null && rt.sizeDelta.x < minWidth)
            {
                rt.sizeDelta = new Vector2(minWidth, rt.sizeDelta.y);
            }
        }

        private void UpdateVisibility()
        {
            EnsureScoreboardRoot();
            EnsureTabHintUI();
            EnsureManoEnJuegoUI();
            EnsureCantoAnnouncementUI();
            EnsureResponseDetailsUI();

            if (hudCanvas != null)
                hudCanvas.enabled = isSeated && !matchFinished;
            if (endGameCanvas != null)
                endGameCanvas.enabled = isSeated && matchFinished;

            bool showInterface = isSeated && interfaceMode;
            bool showEnvidoOptions = showInterface && envidoPanelOpen;
            bool showResponse = showInterface && needsResponse && !envidoPanelOpen;
            // Si el jugador debe responder a Envido, ocultar ActionBar para evitar colisión con Quiero/No Quiero.
            // Si debe responder a Truco, ActionBar se muestra para permitir cantar Re-Truco o Vale 4.
            bool showActionBar = showInterface && !matchFinished && (!showResponse || currentPendingCanto == TipoCanto.Truco);
            bool showEndGame = isSeated && matchFinished;

            // Turn indicator (empty frame in top center): ocultar si no hay texto de turno o si la partida no inició
            if (turnText != null)
            {
                Transform turnParent = turnText.transform.parent;
                bool hasTurnText = !string.IsNullOrEmpty(turnText.text);
                bool showTurnIndicator = isSeated && !matchFinished && hasTurnText;

                if (turnParent != null && turnParent != (hudCanvas != null ? hudCanvas.transform : transform))
                {
                    turnParent.gameObject.SetActive(showTurnIndicator);
                }
                else
                {
                    turnText.gameObject.SetActive(showTurnIndicator);
                }
            }

            // Scoreboard: visible ONLY with Tab OR temporarily at end of round / matchFinished
            if (scoreboardRoot != null)
            {
                bool showScoreboard = isSeated && (interfaceMode || scoreboardTemporarilyVisible || matchFinished);
                scoreboardRoot.SetActive(showScoreboard);
            }

            // Tab hint cartel (bottom-left): visible whenever seated and game not finished
            if (tabHintRoot != null)
            {
                tabHintRoot.SetActive(isSeated && !matchFinished);
                RectTransform rt = tabHintRoot.GetComponent<RectTransform>();
                if (tabHintText != null)
                {
                    if (interfaceMode)
                    {
                        if (rt != null) rt.sizeDelta = new Vector2(300f, 105f);
                        tabHintText.fontSize = 15f;
                        tabHintText.lineSpacing = 8f;
                        tabHintText.alignment = TextAlignmentOptions.Left;
                        tabHintText.text = " <b>[Tab]</b> Cerrar Menú UI\n <b>[1, 2, 3]</b> Jugar Carta\n <b>[P]</b> Levantarse de la mesa";
                    }
                    else
                    {
                        if (rt != null) rt.sizeDelta = new Vector2(250f, 44f);
                        tabHintText.fontSize = 16f;
                        tabHintText.lineSpacing = 0f;
                        tabHintText.alignment = TextAlignmentOptions.Center;
                        tabHintText.text = "<b>[Tab]</b> Abrir Menú UI";
                    }
                }
            }

            // Mano en juego cartel (top-left): visible ONLY with Tab
            if (manoEnJuegoRoot != null)
            {
                manoEnJuegoRoot.SetActive(isSeated && !matchFinished && interfaceMode);
                if (manoEnJuegoText != null)
                {
                    int trucoPts = (currentTrucoLevel == NivelTruco.Ninguno) ? 1 : (int)currentTrucoLevel;
                    string trucoDesc = currentTrucoLevel switch
                    {
                        NivelTruco.Truco => "¡Truco!",
                        NivelTruco.Retruco => "¡Re Truco!",
                        NivelTruco.Vale4 => "¡Vale Cuatro!",
                        _ => "Simple"
                    };
                    manoEnJuegoText.text = $"Mano en Juego: {trucoPts} pt(s) ({trucoDesc})";
                }
            }

            // Canto Announcement Banner (top center cartel): emergent when waiting for response
            if (cantoAnnouncementRoot != null)
            {
                bool showCanto = isSeated && !matchFinished && isWaitingCanto;
                cantoAnnouncementRoot.SetActive(showCanto);
                if (showCanto && cantoAnnouncementText != null)
                {
                    if (!isWaitingLocalCaller)
                    {
                        string pendingName = currentPendingCanto == TipoCanto.Envido
                            ? EnvidoRules.GetDisplayName(currentEnvidoState)
                            : (currentPendingCanto == TipoCanto.Truco
                                ? (currentTrucoLevel switch
                                {
                                    NivelTruco.Retruco => "Re Truco",
                                    NivelTruco.Vale4 => "Vale Cuatro",
                                    _ => "Truco"
                                })
                                : currentPendingCanto.ToString());
                        cantoAnnouncementText.text = $"¡El rival cantó {pendingName}!";
                    }
                    else
                    {
                        cantoAnnouncementText.text = "Esperando respuesta del rival...";
                    }
                }
            }

            // Subtitle bet details in ResponsePanel (shown when Tab is open and responding)
            if (responseDetailsRoot != null)
            {
                bool showBetDetails = showResponse && !isWaitingLocalCaller && currentPendingCanto == TipoCanto.Envido;
                responseDetailsRoot.SetActive(showBetDetails);
                if (showBetDetails && responseDetailsText != null)
                {
                    responseDetailsText.text = $"Apuesta: {currentPendingEnvidoPoints} pts  |  Al No Quiero: {EnvidoRules.GetRejectedPoints(currentEnvidoAcceptedPoints)} pts";
                }
            }

            // Reposition Pava if ManoEnJuego is visible so they don't overlap
            if (pavaTimerRoot != null && pavaTimerRoot.activeSelf)
            {
                PositionPavaTimerInTopRight();
            }

            if (actionBarRoot != null) actionBarRoot.SetActive(showActionBar);
            if (envidoPanelRoot != null) envidoPanelRoot.SetActive(showEnvidoOptions);
            if (responsePanelRoot != null) responsePanelRoot.SetActive(showResponse);
            if (victoryPanelRoot != null) victoryPanelRoot.SetActive(showEndGame && localPlayerWon && !postGameOpen);
            if (defeatPanelRoot != null) defeatPanelRoot.SetActive(showEndGame && !localPlayerWon && !postGameOpen);
            if (postGamePanelRoot != null) postGamePanelRoot.SetActive(showEndGame && postGameOpen);
            UpdateEndGameAudio();

            // Reposition Quiero and No Quiero so they never touch ActionBar
            if (showResponse)
            {
                if (quieroButton != null)
                {
                    RectTransform rtQ = quieroButton.GetComponent<RectTransform>();
                    if (rtQ != null) rtQ.anchoredPosition = new Vector2(-125f, -260f);
                }
                if (noQuieroButton != null)
                {
                    RectTransform rtNQ = noQuieroButton.GetComponent<RectTransform>();
                    if (rtNQ != null) rtNQ.anchoredPosition = new Vector2(125f, -260f);
                }
            }

            string trucoLabel = currentTrucoLevel switch
            {
                NivelTruco.Ninguno => "[Q] Cantar Truco",
                NivelTruco.Truco => "[Q] ¡Re Truco!",
                NivelTruco.Retruco => "[Q] ¡Vale Cuatro!",
                _ => "[Q] Cantar Truco"
            };
            bool trucoInteractable = isLocalTurn || (needsResponse && currentPendingCanto == TipoCanto.Truco);
            FormatButton(trucoButton, trucoLabel, showActionBar, trucoInteractable, 275f);

            bool envidoActive = showActionBar && (!showResponse || currentPendingCanto == TipoCanto.Envido);
            bool envidoInteractable = canCallEnvido || canCallRealEnvido || canCallFaltaEnvido;
            FormatButton(envidoButton, "[E] Envido", envidoActive, envidoInteractable, 265f);

            FormatButton(salirDelMazoButton, "Mazo", showActionBar && !showResponse, false, 240f);

            FormatButton(cantarEnvidoButton, "[E] Envido", showEnvidoOptions, canCallEnvido, 265f);
            FormatButton(realEnvidoButton, "[R] Real Envido", showEnvidoOptions, canCallRealEnvido, 265f);
            FormatButton(faltaEnvidoButton, "[F] Falta Envido", showEnvidoOptions, canCallFaltaEnvido, 265f);
            if (volverButton != null) volverButton.gameObject.SetActive(showEnvidoOptions);

            string quieroLabel = bastonPresidencialActiveOnMe ? "[A] ¡QUIERO! (Mandato)" : "[A] ¡Quiero!";
            FormatButton(quieroButton, quieroLabel, showResponse, true, 265f);
            FormatButton(noQuieroButton, "[N] No Quiero", showResponse && !bastonPresidencialActiveOnMe, true, 265f);
            if (bastonPresidencialActiveOnMe && noQuieroButton != null)
            {
                noQuieroButton.gameObject.SetActive(false);
            }

            if (roundPowersUI != null)
            {
                roundPowersUI.SetInterfaceMode(interfaceMode);
                roundPowersUI.UpdateGameState(isSeated, matchFinished);
            }
        }

        private void UpdateEndGameAudio()
        {
            GameObject resultPanel = null;
            if (endGameCanvas != null && endGameCanvas.isActiveAndEnabled)
            {
                if (victoryPanelRoot != null && victoryPanelRoot.activeInHierarchy)
                    resultPanel = victoryPanelRoot;
                else if (defeatPanelRoot != null && defeatPanelRoot.activeInHierarchy)
                    resultPanel = defeatPanelRoot;
            }

            UIInteractionAudio.SetEndGameAudioActive(resultPanel != null);
            if (resultPanel != null && resultPanel != visibleEndGamePanel)
            {
                bool played = resultPanel == victoryPanelRoot
                    ? UIInteractionAudio.PlayVictory()
                    : UIInteractionAudio.PlayDefeat();
                if (!played)
                    return;
            }

            visibleEndGamePanel = resultPanel;
        }

        public void AddPowerToRoundTimeline(PowerData power, bool isLocal)
        {
            if (roundPowersUI != null && power != null)
                roundPowersUI.AddPower(power, isLocal);
        }

        public void SetLocalPlayerActivePower(PowerData power)
        {
            AddPowerToRoundTimeline(power, true);
        }

        public void SetOpponentActivePower(PowerData power)
        {
            AddPowerToRoundTimeline(power, false);
        }

        public void ClearRoundPowers()
        {
            if (roundPowersUI != null)
                roundPowersUI.ClearPowers();
        }

        private void CallTruco()
        {
            if (localPlayer != null)
                localPlayer.CallTrucoFromHud();
        }

        private void OpenEnvidoPanel()
        {
            envidoPanelOpen = true;
            UpdateVisibility();
        }

        private void CallEnvido(EnvidoCall call)
        {
            bool canCall = call switch
            {
                EnvidoCall.Envido => canCallEnvido,
                EnvidoCall.RealEnvido => canCallRealEnvido,
                EnvidoCall.FaltaEnvido => canCallFaltaEnvido,
                _ => false
            };
            if (!canCall || localPlayer == null)
                return;

            localPlayer.CallEnvidoFromHud(call);
            envidoPanelOpen = false;
            UpdateVisibility();
        }

        private void CloseEnvidoPanel()
        {
            envidoPanelOpen = false;
            UpdateVisibility();
        }

        private void RespondToCall(bool accept)
        {
            if (localPlayer != null)
                localPlayer.RespondToCallFromHud(accept);
        }

        private void OpenPostGame()
        {
            if (!matchFinished)
                return;

            postGameOpen = true;
            UpdateVisibility();
        }

        private void RestartMatch()
        {
            if (localPlayer != null && matchFinished)
                localPlayer.RestartMatchFromHud();
        }

        private void LeaveTable()
        {
            if (localPlayer != null && matchFinished)
                localPlayer.StandUpFromEndGameHud();
        }

        public void SetBastonPresidencial(bool activeOnMe)
        {
            bastonPresidencialActiveOnMe = activeOnMe;
            if (activeOnMe && noQuieroButton != null)
            {
                noQuieroButton.gameObject.SetActive(false);
            }
            UpdateVisibility();
        }

        public void SetPavaCaliente(bool active)
        {
            pavaCalienteActive = active;
            if (active)
            {
                pavaTimeRemaining = 5.0f;
            }
            if (pavaTimerRoot != null)
            {
                pavaTimerRoot.SetActive(active && isSeated && !matchFinished);
            }
        }

        public void ShowToast(string title, string message, float duration = 6.5f)
        {
            EnsureToastUI();
            if (toastRoot == null) return;

            if (toastCoroutine != null)
            {
                StopCoroutine(toastCoroutine);
            }
            toastCoroutine = StartCoroutine(ToastRoutine(title, message, duration));
        }

        private IEnumerator ToastRoutine(string title, string message, float duration)
        {
            if (toastTitleText != null) toastTitleText.text = title;
            if (toastMessageText != null) toastMessageText.text = message;

            toastRoot.SetActive(true);
            if (toastCanvasGroup != null)
            {
                toastCanvasGroup.alpha = 0f;
                float elapsed = 0f;
                while (elapsed < 0.25f)
                {
                    elapsed += Time.deltaTime;
                    toastCanvasGroup.alpha = Mathf.Clamp01(elapsed / 0.25f);
                    yield return null;
                }
                toastCanvasGroup.alpha = 1f;

                yield return new WaitForSeconds(duration);

                elapsed = 0f;
                while (elapsed < 0.4f)
                {
                    elapsed += Time.deltaTime;
                    toastCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / 0.4f);
                    yield return null;
                }
                toastCanvasGroup.alpha = 0f;
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }
            toastRoot.SetActive(false);
            toastCoroutine = null;
        }

        private int lastDisplayedPavaTenth = -1;

        private void Update()
        {
            if (pavaCalienteActive && isSeated && !matchFinished)
            {
                if (pavaTimerRoot == null) EnsurePavaTimerUI();
                if (pavaTimerRoot != null && !pavaTimerRoot.activeSelf)
                    pavaTimerRoot.SetActive(true);

                if (isLocalTurn || needsResponse)
                {
                    pavaTimeRemaining -= Time.deltaTime;
                    if (pavaTimeRemaining < 0f) pavaTimeRemaining = 0f;

                    if (pavaTimerText != null)
                    {
                        int tenth = Mathf.RoundToInt(pavaTimeRemaining * 10f);
                        if (tenth != lastDisplayedPavaTenth)
                        {
                            lastDisplayedPavaTenth = tenth;
                            pavaTimerText.text = $"🔥 PAVA CALIENTE: {pavaTimeRemaining:F1}s";
                        }

                        if (pavaTimeRemaining <= 2.0f)
                        {
                            pavaTimerText.color = (Mathf.Sin(Time.time * 12f) > 0f) ? Color.red : new Color(1f, 0.45f, 0f, 1f);
                        }
                        else
                        {
                            pavaTimerText.color = new Color(1f, 0.95f, 0.85f, 1f);
                        }
                    }
                }
                else
                {
                    if (pavaTimerText != null && lastDisplayedPavaTenth != -999)
                    {
                        lastDisplayedPavaTenth = -999;
                        pavaTimerText.text = "🔥 PAVA RIVAL: APURADO";
                        pavaTimerText.color = new Color(0.9f, 0.7f, 0.5f, 1f);
                    }
                }
            }
            else
            {
                lastDisplayedPavaTenth = -1;
                if (pavaTimerRoot != null && pavaTimerRoot.activeSelf)
                    pavaTimerRoot.SetActive(false);
            }
        }

        private void EnsurePavaTimerUI()
        {
            if (pavaTimerRoot != null) return;

            Transform canvasTransform = hudCanvas != null ? hudCanvas.transform : transform;
            Transform existing = canvasTransform.Find("PavaCalienteTimer");
            if (existing != null)
            {
                pavaTimerRoot = existing.gameObject;
                pavaTimerText = existing.GetComponentInChildren<TMP_Text>();
                PositionPavaTimerInTopRight();
                return;
            }

            Sprite woodSprite = null;
            TMP_FontAsset fontAsset = null;
            Material fontMaterial = null;
            if (trucoButton != null)
            {
                if (trucoButton.image != null) woodSprite = trucoButton.image.sprite;
                TMP_Text btnText = trucoButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    fontAsset = btnText.font;
                    fontMaterial = btnText.fontSharedMaterial;
                }
            }

            GameObject timerObj = new GameObject("PavaCalienteTimer", typeof(RectTransform), typeof(Image));
            timerObj.transform.SetParent(canvasTransform, false);
            pavaTimerRoot = timerObj;

            RectTransform rt = timerObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280f, 44f);
            PositionPavaTimerInTopRight();

            Image img = timerObj.GetComponent<Image>();
            img.sprite = woodSprite;
            img.color = new Color(0.35f, 0.15f, 0.08f, 0.95f);
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(timerObj.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8f, 0f);
            textRt.offsetMax = new Vector2(-8f, 0f);

            pavaTimerText = textObj.GetComponent<TextMeshProUGUI>();
            pavaTimerText.font = fontAsset;
            if (fontMaterial != null) pavaTimerText.fontSharedMaterial = fontMaterial;
            pavaTimerText.text = "🔥 PAVA CALIENTE: 5.0s";
            pavaTimerText.fontSize = 18f;
            pavaTimerText.fontStyle = FontStyles.Bold;
            pavaTimerText.alignment = TextAlignmentOptions.Center;
            pavaTimerText.color = new Color(1f, 0.95f, 0.85f, 1f);
            pavaTimerText.raycastTarget = false;

            timerObj.SetActive(false);
        }

        private void PositionPavaTimerInTopRight()
        {
            RectTransform rt = pavaTimerRoot.GetComponent<RectTransform>();
            if (rt == null) return;

            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            float yPos = (manoEnJuegoRoot != null && manoEnJuegoRoot.activeSelf) ? -75f : -25f;
            rt.anchoredPosition = new Vector2(25f, yPos);
        }

        private void EnsureToastUI()
        {
            if (toastRoot != null)
            {
                ApplyPowerBannerTextSpacing();
                return;
            }

            Transform canvasTransform = hudCanvas != null ? hudCanvas.transform : transform;
            Transform existing = canvasTransform.Find("PowerToastBanner");
            if (existing != null)
            {
                toastRoot = existing.gameObject;
                toastCanvasGroup = existing.GetComponent<CanvasGroup>();
                toastTitleText = existing.Find("Title")?.GetComponent<TMP_Text>();
                toastMessageText = existing.Find("Message")?.GetComponent<TMP_Text>();
                RectTransform extRt = existing.GetComponent<RectTransform>();
                if (extRt != null)
                {
                    extRt.anchoredPosition = new Vector2(0f, -185f);
                    extRt.sizeDelta = new Vector2(620f, 108f);
                }
                if (toastTitleText != null)
                {
                    toastTitleText.fontSize = 24f;
                    toastTitleText.rectTransform.sizeDelta = new Vector2(-32f, 30f);
                }
                if (toastMessageText != null)
                {
                    toastMessageText.fontSize = 20f;
                    toastMessageText.fontSizeMin = 16f;
                    toastMessageText.fontSizeMax = 20f;
                    toastMessageText.enableAutoSizing = true;
                }
                RectTransform existingMessageRect = toastMessageText != null
                    ? toastMessageText.rectTransform
                    : null;
                if (existingMessageRect != null)
                    existingMessageRect.sizeDelta = new Vector2(-32f, -52f);
                ApplyPowerBannerTextSpacing();
                return;
            }

            Sprite woodSprite = null;
            TMP_FontAsset fontAsset = null;
            Material fontMaterial = null;
            if (trucoButton != null)
            {
                if (trucoButton.image != null) woodSprite = trucoButton.image.sprite;
                TMP_Text btnText = trucoButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    fontAsset = btnText.font;
                    fontMaterial = btnText.fontSharedMaterial;
                }
            }

            GameObject toastObj = new GameObject("PowerToastBanner", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            toastObj.transform.SetParent(canvasTransform, false);
            toastRoot = toastObj;
            toastCanvasGroup = toastObj.GetComponent<CanvasGroup>();

            RectTransform rt = toastObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -185f);
            rt.sizeDelta = new Vector2(620f, 108f);

            Image img = toastObj.GetComponent<Image>();
            img.sprite = woodSprite;
            img.color = new Color(0.20f, 0.12f, 0.08f, 0.96f);
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(toastObj.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0f, 1f);
            titleRt.anchoredPosition = new Vector2(16f, -6f);
            titleRt.sizeDelta = new Vector2(-32f, 30f);

            toastTitleText = titleObj.GetComponent<TextMeshProUGUI>();
            toastTitleText.font = fontAsset;
            if (fontMaterial != null) toastTitleText.fontSharedMaterial = fontMaterial;
            toastTitleText.text = "PODER";
            toastTitleText.fontSize = 24f;
            toastTitleText.fontStyle = FontStyles.Bold;
            toastTitleText.characterSpacing = 1.5f;
            toastTitleText.alignment = TextAlignmentOptions.Center;
            toastTitleText.color = new Color(0.95f, 0.75f, 0.45f, 1f);
            toastTitleText.raycastTarget = false;

            GameObject msgObj = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            msgObj.transform.SetParent(toastObj.transform, false);
            RectTransform msgRt = msgObj.GetComponent<RectTransform>();
            msgRt.anchorMin = new Vector2(0f, 0f);
            msgRt.anchorMax = new Vector2(1f, 1f);
            msgRt.pivot = new Vector2(0f, 0f);
            msgRt.anchoredPosition = new Vector2(16f, 6f);
            msgRt.sizeDelta = new Vector2(-32f, -52f);

            toastMessageText = msgObj.GetComponent<TextMeshProUGUI>();
            toastMessageText.font = fontAsset;
            if (fontMaterial != null) toastMessageText.fontSharedMaterial = fontMaterial;
            toastMessageText.text = "";
            toastMessageText.fontSize = 20f;
            toastMessageText.fontSizeMin = 16f;
            toastMessageText.fontSizeMax = 20f;
            toastMessageText.enableAutoSizing = true;
            toastMessageText.alignment = TextAlignmentOptions.Center;
            toastMessageText.color = new Color(0.92f, 0.90f, 0.85f, 1f);
            toastMessageText.raycastTarget = false;

            ApplyPowerBannerTextSpacing();
            toastObj.SetActive(false);
        }

        private void ApplyPowerBannerTextSpacing()
        {
            float messageTopOffset = -(36f + Mathf.Max(0f, powerBannerTextSpacing));

            if (toastMessageText != null)
            {
                RectTransform messageRect = toastMessageText.rectTransform;
                messageRect.offsetMin = new Vector2(16f, 6f);
                messageRect.offsetMax = new Vector2(-16f, messageTopOffset);
            }

            if (powerPromptInstruction != null)
            {
                RectTransform instructionRect = powerPromptInstruction.rectTransform;
                instructionRect.offsetMin = new Vector2(10f, 4f);
                instructionRect.offsetMax = new Vector2(-10f, messageTopOffset);
            }
        }

        private void CallEnvidoOption() => CallEnvido(EnvidoCall.Envido);
        private void CallRealEnvidoOption() => CallEnvido(EnvidoCall.RealEnvido);
        private void CallFaltaEnvidoOption() => CallEnvido(EnvidoCall.FaltaEnvido);
        private void AcceptCall() => RespondToCall(true);
        private void DeclineCall()
        {
            if (bastonPresidencialActiveOnMe)
            {
                ShowToast("BASTÓN PRESIDENCIAL", "¡No puedes rechazar bajo la orden del Bastón Presidencial!");
                return;
            }
            RespondToCall(false);
        }

        private void EnsureGuampasChoiceUI()
        {
            if (guampasChoiceRoot != null) return;

            Transform canvasTransform = hudCanvas != null ? hudCanvas.transform : transform;
            Transform existing = canvasTransform.Find("GuampasChoiceModal");
            if (existing != null)
            {
                guampasChoiceRoot = existing.gameObject;
                guampasDrawnCardNameText = existing.Find("CardBox/CardName")?.GetComponent<TMP_Text>();
                guampasDrawnCardDetailsText = existing.Find("CardBox/CardDetails")?.GetComponent<TMP_Text>();
                guampasButtonsContainer = existing.Find("ButtonsContainer");
                guampasCancelButton = existing.Find("CancelButton")?.GetComponent<Button>();
                if (guampasCancelButton != null)
                {
                    guampasCancelButton.onClick.RemoveAllListeners();
                    guampasCancelButton.onClick.AddListener(OnGuampasCancelClicked);
                }
                return;
            }

            Sprite woodSprite = null;
            TMP_FontAsset fontAsset = null;
            Material fontMaterial = null;
            if (trucoButton != null)
            {
                if (trucoButton.image != null) woodSprite = trucoButton.image.sprite;
                TMP_Text btnText = trucoButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    fontAsset = btnText.font;
                    fontMaterial = btnText.fontSharedMaterial;
                }
            }

            GameObject modalObj = new GameObject("GuampasChoiceModal", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            modalObj.transform.SetParent(canvasTransform, false);
            guampasChoiceRoot = modalObj;

            RectTransform rt = modalObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(560f, 360f);

            Image img = modalObj.GetComponent<Image>();
            img.sprite = woodSprite;
            img.color = new Color(0.18f, 0.10f, 0.06f, 0.98f);
            img.type = Image.Type.Simple;

            // Título
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(modalObj.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -14f);
            titleRt.sizeDelta = new Vector2(-40f, 36f);

            TMP_Text titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            titleTmp.font = fontAsset;
            if (fontMaterial != null) titleTmp.fontSharedMaterial = fontMaterial;
            titleTmp.text = "🐃 ¡PODER GUAMPAS!";
            titleTmp.fontSize = 22f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.characterSpacing = 1.2f;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.96f, 0.80f, 0.45f, 1f);

            // Subtítulo
            GameObject subObj = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(modalObj.transform, false);
            RectTransform subRt = subObj.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 1f);
            subRt.anchorMax = new Vector2(1f, 1f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.anchoredPosition = new Vector2(0f, -50f);
            subRt.sizeDelta = new Vector2(-40f, 24f);

            TMP_Text subTmp = subObj.GetComponent<TextMeshProUGUI>();
            subTmp.font = fontAsset;
            if (fontMaterial != null) subTmp.fontSharedMaterial = fontMaterial;
            subTmp.text = "Robaste una carta aleatoria del mazo:";
            subTmp.fontSize = 14f;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.90f, 0.88f, 0.82f, 1f);

            // Card Box Container
            GameObject cardBoxObj = new GameObject("CardBox", typeof(RectTransform), typeof(Image));
            cardBoxObj.transform.SetParent(modalObj.transform, false);
            RectTransform boxRt = cardBoxObj.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 1f);
            boxRt.anchorMax = new Vector2(0.5f, 1f);
            boxRt.pivot = new Vector2(0.5f, 1f);
            boxRt.anchoredPosition = new Vector2(0f, -78f);
            boxRt.sizeDelta = new Vector2(480f, 75f);

            Image boxImg = cardBoxObj.GetComponent<Image>();
            boxImg.sprite = woodSprite;
            boxImg.color = new Color(0.28f, 0.16f, 0.10f, 0.95f);
            boxImg.type = Image.Type.Simple;

            // Card Name inside Box
            GameObject cardNameObj = new GameObject("CardName", typeof(RectTransform), typeof(TextMeshProUGUI));
            cardNameObj.transform.SetParent(cardBoxObj.transform, false);
            RectTransform cardNameRt = cardNameObj.GetComponent<RectTransform>();
            cardNameRt.anchorMin = new Vector2(0f, 0.45f);
            cardNameRt.anchorMax = new Vector2(1f, 1f);
            cardNameRt.offsetMin = Vector2.zero;
            cardNameRt.offsetMax = Vector2.zero;

            guampasDrawnCardNameText = cardNameObj.GetComponent<TextMeshProUGUI>();
            guampasDrawnCardNameText.font = fontAsset;
            if (fontMaterial != null) guampasDrawnCardNameText.fontSharedMaterial = fontMaterial;
            guampasDrawnCardNameText.text = "🃏 Carta";
            guampasDrawnCardNameText.fontSize = 22f;
            guampasDrawnCardNameText.fontStyle = FontStyles.Bold;
            guampasDrawnCardNameText.alignment = TextAlignmentOptions.Center;
            guampasDrawnCardNameText.color = new Color(1f, 0.88f, 0.30f, 1f);

            // Card Details inside Box
            GameObject cardDetObj = new GameObject("CardDetails", typeof(RectTransform), typeof(TextMeshProUGUI));
            cardDetObj.transform.SetParent(cardBoxObj.transform, false);
            RectTransform cardDetRt = cardDetObj.GetComponent<RectTransform>();
            cardDetRt.anchorMin = new Vector2(0f, 0f);
            cardDetRt.anchorMax = new Vector2(1f, 0.45f);
            cardDetRt.offsetMin = Vector2.zero;
            cardDetRt.offsetMax = Vector2.zero;

            guampasDrawnCardDetailsText = cardDetObj.GetComponent<TextMeshProUGUI>();
            guampasDrawnCardDetailsText.font = fontAsset;
            if (fontMaterial != null) guampasDrawnCardDetailsText.fontSharedMaterial = fontMaterial;
            guampasDrawnCardDetailsText.text = "";
            guampasDrawnCardDetailsText.fontSize = 13f;
            guampasDrawnCardDetailsText.alignment = TextAlignmentOptions.Center;
            guampasDrawnCardDetailsText.color = new Color(0.85f, 0.82f, 0.75f, 1f);

            // Instruction Prompt
            GameObject promptObj = new GameObject("Prompt", typeof(RectTransform), typeof(TextMeshProUGUI));
            promptObj.transform.SetParent(modalObj.transform, false);
            RectTransform promptRt = promptObj.GetComponent<RectTransform>();
            promptRt.anchorMin = new Vector2(0f, 1f);
            promptRt.anchorMax = new Vector2(1f, 1f);
            promptRt.pivot = new Vector2(0.5f, 1f);
            promptRt.anchoredPosition = new Vector2(0f, -165f);
            promptRt.sizeDelta = new Vector2(-40f, 24f);

            TMP_Text promptTmp = promptObj.GetComponent<TextMeshProUGUI>();
            promptTmp.font = fontAsset;
            if (fontMaterial != null) promptTmp.fontSharedMaterial = fontMaterial;
            promptTmp.text = "¿Deseás cambiarla por una carta de tu mano?";
            promptTmp.fontSize = 14f;
            promptTmp.fontStyle = FontStyles.Bold;
            promptTmp.alignment = TextAlignmentOptions.Center;
            promptTmp.color = new Color(0.95f, 0.92f, 0.85f, 1f);

            // Hand Buttons Container
            GameObject contObj = new GameObject("ButtonsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            contObj.transform.SetParent(modalObj.transform, false);
            guampasButtonsContainer = contObj.transform;

            RectTransform contRt = contObj.GetComponent<RectTransform>();
            contRt.anchorMin = new Vector2(0.5f, 1f);
            contRt.anchorMax = new Vector2(0.5f, 1f);
            contRt.pivot = new Vector2(0.5f, 1f);
            contRt.anchoredPosition = new Vector2(0f, -195f);
            contRt.sizeDelta = new Vector2(490f, 65f);

            HorizontalLayoutGroup hlg = contObj.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // Cancel Button
            GameObject cancelObj = new GameObject("CancelButton", typeof(RectTransform), typeof(Image), typeof(Button));
            cancelObj.transform.SetParent(modalObj.transform, false);
            RectTransform cancelRt = cancelObj.GetComponent<RectTransform>();
            cancelRt.anchorMin = new Vector2(0.5f, 0f);
            cancelRt.anchorMax = new Vector2(0.5f, 0f);
            cancelRt.pivot = new Vector2(0.5f, 0f);
            cancelRt.anchoredPosition = new Vector2(0f, 22f);
            cancelRt.sizeDelta = new Vector2(340f, 42f);

            Image cancelImg = cancelObj.GetComponent<Image>();
            cancelImg.sprite = woodSprite;
            cancelImg.color = new Color(0.48f, 0.16f, 0.12f, 1f);
            cancelImg.type = Image.Type.Simple;

            guampasCancelButton = cancelObj.GetComponent<Button>();
            ColorBlock cbCancel = guampasCancelButton.colors;
            cbCancel.normalColor = new Color(0.48f, 0.16f, 0.12f, 1f);
            cbCancel.highlightedColor = new Color(0.62f, 0.22f, 0.18f, 1f);
            cbCancel.pressedColor = new Color(0.35f, 0.10f, 0.08f, 1f);
            guampasCancelButton.colors = cbCancel;
            guampasCancelButton.onClick.AddListener(OnGuampasCancelClicked);

            GameObject cancelTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            cancelTxtObj.transform.SetParent(cancelObj.transform, false);
            RectTransform cancelTxtRt = cancelTxtObj.GetComponent<RectTransform>();
            cancelTxtRt.anchorMin = Vector2.zero;
            cancelTxtRt.anchorMax = Vector2.one;

            TextMeshProUGUI cancelTmp = cancelTxtObj.GetComponent<TextMeshProUGUI>();
            cancelTmp.font = fontAsset;
            if (fontMaterial != null) cancelTmp.fontSharedMaterial = fontMaterial;
            cancelTmp.text = "❌ No cambiar (Conservar mano actual)";
            cancelTmp.fontSize = 13f;
            cancelTmp.fontStyle = FontStyles.Bold;
            cancelTmp.alignment = TextAlignmentOptions.Center;
            cancelTmp.color = new Color(0.96f, 0.92f, 0.88f, 1f);
            cancelTmp.raycastTarget = false;

            modalObj.SetActive(false);
        }

        public void ShowGuampasChoice(
            int drawnCardIndex,
            int[] handIndices,
            Action<int> onChoice)
        {
            EnsureGuampasChoiceUI();
            onGuampasChoiceCallback = onChoice;

            GameServer server = FindAnyObjectByType<GameServer>();
            DeckData deck = server != null ? server.masterDeck : null;

            string drawnCardName = $"Carta {drawnCardIndex}";
            int drawnCardRank = 0;
            int drawnCardEnvido = 0;

            if (deck != null && drawnCardIndex >= 0 && drawnCardIndex < deck.AvailableCards.Count)
            {
                CardData drawn = deck.AvailableCards[drawnCardIndex];
                drawnCardName = $"{drawn.Number} de {drawn.Suit}";
                drawnCardRank = drawn.TrucoRank;
                drawnCardEnvido = drawn.EnvidoValue;
            }

            if (guampasDrawnCardNameText != null)
            {
                guampasDrawnCardNameText.text = $"🃏 {drawnCardName}";
            }
            if (guampasDrawnCardDetailsText != null)
            {
                guampasDrawnCardDetailsText.text = $"Fuerza de Truco: {drawnCardRank}   |   Valor Envido: {drawnCardEnvido}";
            }

            foreach (var btn in spawnedGuampasButtons)
            {
                if (btn != null) Destroy(btn);
            }
            spawnedGuampasButtons.Clear();

            Sprite woodSprite = null;
            TMP_FontAsset fontAsset = null;
            Material fontMaterial = null;
            if (trucoButton != null)
            {
                if (trucoButton.image != null) woodSprite = trucoButton.image.sprite;
                TMP_Text btnText = trucoButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    fontAsset = btnText.font;
                    fontMaterial = btnText.fontSharedMaterial;
                }
            }

            if (handIndices != null)
            {
                for (int i = 0; i < handIndices.Length; i++)
                {
                    int cardIdx = handIndices[i];
                    string cardName = $"Carta {cardIdx}";
                    if (deck != null && cardIdx >= 0 && cardIdx < deck.AvailableCards.Count)
                    {
                        CardData c = deck.AvailableCards[cardIdx];
                        cardName = $"{c.Number} de {c.Suit}";
                    }

                    GameObject btnObj = new GameObject($"GuampasSwapBtn_{i}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                    btnObj.transform.SetParent(guampasButtonsContainer, false);
                    spawnedGuampasButtons.Add(btnObj);

                    LayoutElement le = btnObj.GetComponent<LayoutElement>();
                    le.preferredHeight = 52f;
                    le.flexibleWidth = 1f;

                    Image btnImg = btnObj.GetComponent<Image>();
                    btnImg.sprite = woodSprite;
                    btnImg.color = new Color(0.32f, 0.20f, 0.12f, 1f);
                    btnImg.type = Image.Type.Simple;

                    Button btnComp = btnObj.GetComponent<Button>();
                    ColorBlock cb = btnComp.colors;
                    cb.normalColor = new Color(0.32f, 0.20f, 0.12f, 1f);
                    cb.highlightedColor = new Color(0.50f, 0.32f, 0.20f, 1f);
                    cb.pressedColor = new Color(0.25f, 0.15f, 0.08f, 1f);
                    btnComp.colors = cb;

                    btnComp.onClick.AddListener(() =>
                    {
                        var cbAction = onGuampasChoiceCallback;
                        CloseGuampasChoice();
                        cbAction?.Invoke(cardIdx);
                    });

                    GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    txtObj.transform.SetParent(btnObj.transform, false);
                    RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                    txtRt.anchorMin = Vector2.zero;
                    txtRt.anchorMax = Vector2.one;
                    txtRt.offsetMin = new Vector2(4f, 4f);
                    txtRt.offsetMax = new Vector2(-4f, -4f);

                    TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
                    tmp.font = fontAsset;
                    if (fontMaterial != null) tmp.fontSharedMaterial = fontMaterial;
                    tmp.text = $"Cambiar por:\n<b>{cardName}</b>";
                    tmp.fontSize = 12f;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = new Color(0.95f, 0.92f, 0.85f, 1f);
                    tmp.raycastTarget = false;
                }
            }

            if (guampasChoiceRoot != null)
            {
                guampasChoiceRoot.SetActive(true);
                guampasChoiceRoot.transform.SetAsLastSibling();
            }
        }

        public void CloseGuampasChoice()
        {
            if (guampasChoiceRoot != null)
            {
                guampasChoiceRoot.SetActive(false);
            }
            foreach (var btn in spawnedGuampasButtons)
            {
                if (btn != null) Destroy(btn);
            }
            spawnedGuampasButtons.Clear();
            onGuampasChoiceCallback = null;
        }

        private void OnGuampasCancelClicked()
        {
            var cbAction = onGuampasChoiceCallback;
            CloseGuampasChoice();
            cbAction?.Invoke(-1);
        }

        private void EnsurePowerPromptUI()
        {
            if (powerPromptRoot != null)
            {
                ApplyPowerBannerTextSpacing();
                return;
            }

            Transform canvasTransform = hudCanvas != null ? hudCanvas.transform : transform;
            Transform existing = canvasTransform.Find("PowerPromptBanner");
            if (existing != null)
            {
                powerPromptRoot = existing.gameObject;
                powerPromptTitle = existing.Find("Title")?.GetComponent<TMP_Text>();
                powerPromptInstruction = existing.Find("Instruction")?.GetComponent<TMP_Text>();
                RectTransform extRt = existing.GetComponent<RectTransform>();
                if (extRt != null)
                {
                    extRt.anchoredPosition = new Vector2(0f, -185f);
                    extRt.sizeDelta = new Vector2(620f, 108f);
                }
                if (powerPromptTitle != null)
                {
                    powerPromptTitle.fontSize = 24f;
                    powerPromptTitle.rectTransform.sizeDelta = new Vector2(-20f, 30f);
                }
                if (powerPromptInstruction != null)
                {
                    powerPromptInstruction.fontSize = 20f;
                    powerPromptInstruction.fontSizeMin = 16f;
                    powerPromptInstruction.fontSizeMax = 20f;
                    powerPromptInstruction.enableAutoSizing = true;
                    powerPromptInstruction.rectTransform.offsetMin = new Vector2(10f, 4f);
                    powerPromptInstruction.rectTransform.offsetMax = new Vector2(-10f, -52f);
                }
                ApplyPowerBannerTextSpacing();
                return;
            }

            Sprite woodSprite = null;
            TMP_FontAsset fontAsset = null;
            Material fontMaterial = null;
            if (trucoButton != null)
            {
                if (trucoButton.image != null) woodSprite = trucoButton.image.sprite;
                TMP_Text btnText = trucoButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    fontAsset = btnText.font;
                    fontMaterial = btnText.fontSharedMaterial;
                }
            }

            GameObject promptObj = new GameObject("PowerPromptBanner", typeof(RectTransform), typeof(Image));
            promptObj.transform.SetParent(canvasTransform, false);
            powerPromptRoot = promptObj;

            RectTransform rt = promptObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -185f);
            rt.sizeDelta = new Vector2(620f, 108f);

            Image img = promptObj.GetComponent<Image>();
            img.sprite = woodSprite;
            img.color = new Color(0.18f, 0.10f, 0.06f, 0.96f);
            img.type = Image.Type.Simple;

            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(promptObj.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -6f);
            titleRt.sizeDelta = new Vector2(-20f, 30f);

            powerPromptTitle = titleObj.GetComponent<TextMeshProUGUI>();
            powerPromptTitle.font = fontAsset;
            if (fontMaterial != null) powerPromptTitle.fontSharedMaterial = fontMaterial;
            powerPromptTitle.fontSize = 24f;
            powerPromptTitle.fontStyle = FontStyles.Bold;
            powerPromptTitle.alignment = TextAlignmentOptions.Center;
            powerPromptTitle.color = new Color(0.96f, 0.82f, 0.45f, 1f);

            GameObject descObj = new GameObject("Instruction", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(promptObj.transform, false);
            RectTransform descRt = descObj.GetComponent<RectTransform>();
            descRt.anchorMin = new Vector2(0f, 0f);
            descRt.anchorMax = new Vector2(1f, 1f);
            descRt.pivot = new Vector2(0.5f, 0.5f);
            descRt.offsetMin = new Vector2(10f, 4f);
            descRt.offsetMax = new Vector2(-10f, -52f);

            powerPromptInstruction = descObj.GetComponent<TextMeshProUGUI>();
            powerPromptInstruction.font = fontAsset;
            if (fontMaterial != null) powerPromptInstruction.fontSharedMaterial = fontMaterial;
            powerPromptInstruction.fontSize = 20f;
            powerPromptInstruction.fontSizeMin = 16f;
            powerPromptInstruction.fontSizeMax = 20f;
            powerPromptInstruction.enableAutoSizing = true;
            powerPromptInstruction.alignment = TextAlignmentOptions.Center;
            powerPromptInstruction.color = new Color(0.95f, 0.92f, 0.88f, 1f);

            ApplyPowerBannerTextSpacing();
            promptObj.SetActive(false);
        }

        public void ShowPowerPrompt(string title, string instruction)
        {
            EnsurePowerPromptUI();
            if (powerPromptRoot == null) return;
            if (powerPromptTitle != null) powerPromptTitle.text = title;
            if (powerPromptInstruction != null) powerPromptInstruction.text = instruction;
            powerPromptRoot.SetActive(true);
            powerPromptRoot.transform.SetAsLastSibling();
        }

        public void HidePowerPrompt()
        {
            if (powerPromptRoot != null)
            {
                powerPromptRoot.SetActive(false);
            }
        }

        public void CloseGuitarraChoice()
        {
            HidePowerPrompt();
        }

        public void ShowGuitarraChoice(int[] myHandCards, int[] opponentSlots, Action<int, int> onChoice)
        {
            // Reemplazado por selección táctil 3D directa de cartas
        }
    }
}
