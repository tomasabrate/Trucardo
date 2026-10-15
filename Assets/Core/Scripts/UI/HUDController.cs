using System;
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

        private PlayerController localPlayer;
        private bool isSeated;
        private bool interfaceMode;
        private bool envidoPanelOpen;
        private bool isLocalTurn;
        private bool needsResponse;
        private bool canCallEnvido;
        private bool canCallRealEnvido;
        private bool canCallFaltaEnvido;
        private GameObject[] playerMalasMarks;
        private GameObject[] playerBuenasMarks;
        private GameObject[] opponentMalasMarks;
        private GameObject[] opponentBuenasMarks;

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
            RegisterButtonActions();
            ValidateReferences();
            playerMalasMarks = BuildTallyMarks(playerMalasTally, 1);
            playerBuenasMarks = BuildTallyMarks(playerBuenasTally, 16);
            opponentMalasMarks = BuildTallyMarks(opponentMalasTally, 1);
            opponentBuenasMarks = BuildTallyMarks(opponentBuenasTally, 16);

            if (salirDelMazoButton != null)
                salirDelMazoButton.interactable = false;

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
            UpdateVisibility();
        }

        public void SetPlayerSeated(bool seated)
        {
            isSeated = seated;
            if (!seated)
            {
                interfaceMode = false;
                envidoPanelOpen = false;
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
            bool allowFaltaEnvido)
        {
            if (turnText != null)
                turnText.text = turnMessage;

            UpdateScore(playerMalasMarks, playerBuenasMarks, playerTotalText, playerScore);
            UpdateScore(opponentMalasMarks, opponentBuenasMarks, opponentTotalText, opponentScore);

            isLocalTurn = localTurn;
            needsResponse = responseRequired;
            canCallEnvido = allowEnvido;
            canCallRealEnvido = allowRealEnvido;
            canCallFaltaEnvido = allowFaltaEnvido;
            UpdateVisibility();
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
                hudCanvas.enabled = isSeated;

            bool showInterface = isSeated && interfaceMode;
            bool showEnvidoOptions = showInterface && envidoPanelOpen;
            bool showResponse = showInterface && needsResponse && !envidoPanelOpen;
            bool showActionBar = showInterface;

            if (actionBarRoot != null) actionBarRoot.SetActive(showActionBar);
            if (envidoPanelRoot != null) envidoPanelRoot.SetActive(showEnvidoOptions);
            if (responsePanelRoot != null) responsePanelRoot.SetActive(showResponse);
            if (trucoButton != null)
            {
                trucoButton.gameObject.SetActive(true);
                trucoButton.interactable = isLocalTurn;
            }
            if (envidoButton != null)
            {
                envidoButton.gameObject.SetActive(true);
                envidoButton.interactable = canCallEnvido || canCallRealEnvido || canCallFaltaEnvido;
            }
            if (salirDelMazoButton != null)
            {
                salirDelMazoButton.gameObject.SetActive(true);
                salirDelMazoButton.interactable = false;
            }
            if (cantarEnvidoButton != null) cantarEnvidoButton.gameObject.SetActive(showEnvidoOptions && canCallEnvido);
            if (realEnvidoButton != null) realEnvidoButton.gameObject.SetActive(showEnvidoOptions && canCallRealEnvido);
            if (faltaEnvidoButton != null) faltaEnvidoButton.gameObject.SetActive(showEnvidoOptions && canCallFaltaEnvido);
            if (volverButton != null) volverButton.gameObject.SetActive(showEnvidoOptions);
            if (quieroButton != null) quieroButton.gameObject.SetActive(showResponse);
            if (noQuieroButton != null) noQuieroButton.gameObject.SetActive(showResponse);
        }

        private void CallTruco()
        {
            if (localPlayer != null)
                localPlayer.CallTrucoFromHud();
        }

        private void OpenEnvidoPanel()
        {
            if (!canCallEnvido && !canCallRealEnvido && !canCallFaltaEnvido)
                return;

            envidoPanelOpen = true;
            UpdateVisibility();
        }

        private void CallEnvido(EnvidoCall call)
        {
            if (localPlayer == null)
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

        private void CallEnvidoOption() => CallEnvido(EnvidoCall.Envido);
        private void CallRealEnvidoOption() => CallEnvido(EnvidoCall.RealEnvido);
        private void CallFaltaEnvidoOption() => CallEnvido(EnvidoCall.FaltaEnvido);
        private void AcceptCall() => RespondToCall(true);
        private void DeclineCall() => RespondToCall(false);
    }
}
