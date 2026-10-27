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
        private Button victoryNextButton;
        private Button defeatNextButton;
        private Button rematchButton;
        private Button leaveTableButton;
        private GameObject[] playerMalasMarks;
        private GameObject[] playerBuenasMarks;
        private GameObject[] opponentMalasMarks;
        private GameObject[] opponentBuenasMarks;

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
                instance = null;
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
            if (pavaTimerRoot != null) pavaTimerRoot.SetActive(false);
            if (toastRoot != null) toastRoot.SetActive(false);
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
                if (pavaTimerRoot != null) pavaTimerRoot.SetActive(false);
                if (toastRoot != null) toastRoot.SetActive(false);
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
            bool playerWon)
        {
            if (turnText != null)
                turnText.text = turnMessage;

            UpdateScore(playerMalasMarks, playerBuenasMarks, playerTotalText, playerScore);
            UpdateScore(opponentMalasMarks, opponentBuenasMarks, opponentTotalText, opponentScore);

            if (localTurn != isLocalTurn || responseRequired != needsResponse)
            {
                pavaTimeRemaining = 5.0f;
            }

            isLocalTurn = localTurn;
            needsResponse = responseRequired;
            canCallEnvido = allowEnvido;
            canCallRealEnvido = allowRealEnvido;
            canCallFaltaEnvido = allowFaltaEnvido;
            if (gameFinished && !matchFinished)
                postGameOpen = false;
            matchFinished = gameFinished;
            localPlayerWon = playerWon;
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

        private void UpdateVisibility()
        {
            if (hudCanvas != null)
                hudCanvas.enabled = isSeated && !matchFinished;
            if (endGameCanvas != null)
                endGameCanvas.enabled = isSeated && matchFinished;

            bool showInterface = isSeated && interfaceMode;
            bool showEnvidoOptions = showInterface && envidoPanelOpen;
            bool showResponse = showInterface && needsResponse && !envidoPanelOpen;
            bool showActionBar = showInterface && !matchFinished;
            bool showEndGame = isSeated && matchFinished;

            if (actionBarRoot != null) actionBarRoot.SetActive(showActionBar);
            if (envidoPanelRoot != null) envidoPanelRoot.SetActive(showEnvidoOptions);
            if (responsePanelRoot != null) responsePanelRoot.SetActive(showResponse);
            if (victoryPanelRoot != null) victoryPanelRoot.SetActive(showEndGame && localPlayerWon && !postGameOpen);
            if (defeatPanelRoot != null) defeatPanelRoot.SetActive(showEndGame && !localPlayerWon && !postGameOpen);
            if (postGamePanelRoot != null) postGamePanelRoot.SetActive(showEndGame && postGameOpen);
            if (trucoButton != null)
            {
                trucoButton.gameObject.SetActive(true);
                trucoButton.interactable = isLocalTurn;
            }
            if (envidoButton != null)
            {
                envidoButton.gameObject.SetActive(true);
                envidoButton.interactable = true;
            }
            if (salirDelMazoButton != null)
            {
                salirDelMazoButton.gameObject.SetActive(true);
                salirDelMazoButton.interactable = false;
            }
            if (cantarEnvidoButton != null) cantarEnvidoButton.gameObject.SetActive(showEnvidoOptions);
            if (realEnvidoButton != null) realEnvidoButton.gameObject.SetActive(showEnvidoOptions);
            if (faltaEnvidoButton != null) faltaEnvidoButton.gameObject.SetActive(showEnvidoOptions);
            if (volverButton != null) volverButton.gameObject.SetActive(showEnvidoOptions);
            if (quieroButton != null)
            {
                quieroButton.gameObject.SetActive(showResponse);
                TMP_Text txt = quieroButton.GetComponentInChildren<TMP_Text>();
                if (txt != null)
                {
                    txt.text = bastonPresidencialActiveOnMe ? "¡QUIERO! (Mandato)" : "¡Quiero!";
                }
            }
            if (noQuieroButton != null)
            {
                noQuieroButton.gameObject.SetActive(showResponse && !bastonPresidencialActiveOnMe);
            }

            if (roundPowersUI != null)
            {
                roundPowersUI.UpdateGameState(isSeated, matchFinished);
            }
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

        public void ShowToast(string title, string message, float duration = 4.0f)
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

        private void Update()
        {
            EnsurePavaTimerUI();
            if (pavaCalienteActive && isSeated && !matchFinished)
            {
                if (pavaTimerRoot != null && !pavaTimerRoot.activeSelf)
                    pavaTimerRoot.SetActive(true);

                if (isLocalTurn || needsResponse)
                {
                    pavaTimeRemaining -= Time.deltaTime;
                    if (pavaTimeRemaining < 0f) pavaTimeRemaining = 0f;

                    if (pavaTimerText != null)
                    {
                        pavaTimerText.text = $"🔥 PAVA CALIENTE: {pavaTimeRemaining:F1}s";
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
                    if (pavaTimerText != null)
                    {
                        pavaTimerText.text = "🔥 PAVA RIVAL: APURADO";
                        pavaTimerText.color = new Color(0.9f, 0.7f, 0.5f, 1f);
                    }
                }
            }
            else
            {
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
            rt.anchoredPosition = new Vector2(20f, -20f);
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
        private void DeclineCall() => RespondToCall(false);

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
