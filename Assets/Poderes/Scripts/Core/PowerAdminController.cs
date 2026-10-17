using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Trucardo.Core.Tests;

namespace Trucardo.Core
{
    public enum AdminTab
    {
        Poderes = 0,
        Cartas = 1,
        Partida = 2,
        Tests = 3
    }

    /// <summary>
    /// Controlador de Modo Administrador / Pruebas para Trucardo.
    /// Incluye:
    /// 1. Gestión de Poderes (Spawn, pruebas de UI activa, forzar poderes).
    /// 2. Selección y Edición de Cartas en Mano (selector por palo/número, slots, presets y mano rival).
    /// 3. Control de Partida (puntos, estados, buenas/malas, reinicio).
    /// 4. Suite de Tests Automatizados (Jerarquía Truco, Envido, Poderes).
    /// Se abre/cierra con la tecla F1.
    /// </summary>
    public class PowerAdminController : MonoBehaviour
    {
        private static PowerAdminController instance;
        public static PowerAdminController Instance => instance;

        [Header("Configuración")]
        public bool isVisible = false;
        public PowerCatalog powerCatalog;
        public DeckData deckData;

        // Estado del Panel
        private AdminTab currentTab = AdminTab.Cartas;
        private PowerData overrideNextPower = null;
        private Rect windowRect = new Rect(20, 30, 520, 640);
        private Vector2 scrollPosition = Vector2.zero;

        // Estado de Selección de Cartas
        private int selectedSlot = 0; // 0, 1 o 2
        private int selectedSuitFilter = -1; // -1: Todas, 0: Espada, 1: Basto, 2: Oro, 3: Copa
        private bool targetOpponent = false; // false = Yo, true = Rival
        private bool showOpponentCards = false;

        // Estado de Poderes
        private int powerRarityFilter = -1; // -1: Todos, 0: Común, 1: Legendario

        // Estado de Tests
        private TestRunSummary testSummary = null;
        private string testCategoryFilter = "Todos";

        public bool HasAdminPowerOverride => overrideNextPower != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (instance == null)
            {
                GameObject adminObj = new GameObject("[PowerAdminController]");
                instance = adminObj.AddComponent<PowerAdminController>();
                DontDestroyOnLoad(adminObj);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            LoadCatalog();
            LoadDeck();
        }

        private void LoadCatalog()
        {
            if (powerCatalog == null)
            {
                powerCatalog = Resources.Load<PowerCatalog>("PowerCatalog");
            }
#if UNITY_EDITOR
            if (powerCatalog == null)
            {
                powerCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<PowerCatalog>("Assets/Poderes/Data/PowerCatalog.asset");
            }
#endif
        }

        private void LoadDeck()
        {
            if (deckData == null)
            {
                GameServer server = FindAnyObjectByType<GameServer>();
                if (server != null && server.masterDeck != null)
                {
                    deckData = server.masterDeck;
                }
            }
            if (deckData == null)
            {
                deckData = Resources.Load<DeckData>("MainDeck");
            }
#if UNITY_EDITOR
            if (deckData == null)
            {
                deckData = UnityEditor.AssetDatabase.LoadAssetAtPath<DeckData>("Assets/Data/MainDeck.asset");
            }
#endif
        }

        public PowerData ConsumeAdminPowerOverride()
        {
            PowerData power = overrideNextPower;
            overrideNextPower = null;
            return power;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                ToggleAdmin();
            }
        }

        public void ToggleAdmin()
        {
            isVisible = !isVisible;
            if (isVisible)
            {
                LoadCatalog();
                LoadDeck();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                PlayerController localPlayer = FindLocalPlayer();
                if (localPlayer != null)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        private PlayerController FindLocalPlayer()
        {
            PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude);
            foreach (var p in players)
            {
                if (p.IsOwner) return p;
            }
            return null;
        }

        private void OnGUI()
        {
            if (!isVisible) return;

            GUI.skin.window.fontSize = 13;
            windowRect = GUI.Window(9999, windowRect, DrawWindow, "⚙️ TRUCARDO — Modo Administrador & Pruebas (F1)");
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();

            // Pestañas Superiores
            GUILayout.BeginHorizontal();
            string[] tabNames = { "🔮 Poderes", "🃏 Cartas", "📊 Partida", "🧪 Tests" };
            for (int i = 0; i < tabNames.Length; i++)
            {
                bool isCurrent = ((int)currentTab == i);
                GUI.backgroundColor = isCurrent ? new Color(0.4f, 0.8f, 1f, 1f) : Color.white;
                if (GUILayout.Button(tabNames[i], GUILayout.Height(30)))
                {
                    currentTab = (AdminTab)i;
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(495), GUILayout.Height(530));

            switch (currentTab)
            {
                case AdminTab.Poderes:
                    DrawPowersTab();
                    break;
                case AdminTab.Cartas:
                    DrawCardsTab();
                    break;
                case AdminTab.Partida:
                    DrawMatchTab();
                    break;
                case AdminTab.Tests:
                    DrawTestsTab();
                    break;
            }

            GUILayout.EndScrollView();

            GUILayout.Space(4);
            if (GUILayout.Button("Cerrar Panel [F1]", GUILayout.Height(28)))
            {
                ToggleAdmin();
            }

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 10000, 25));
        }

        // =========================================================================
        // PESTAÑA: CARTAS
        // =========================================================================
        private void DrawCardsTab()
        {
            PlayerController player = FindLocalPlayer();
            LoadDeck();

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 13 };
            GUIStyle cardBoxStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter };

            // 1. MANO ACTUAL
            GUILayout.BeginVertical("box");
            GUILayout.Label("🎴 Tu Mano Actual:", headerStyle);

            int[] currentCards = player != null ? player.GetCurrentCards() : new int[0];

            GUILayout.BeginHorizontal();
            for (int slot = 0; slot < 3; slot++)
            {
                bool isSelected = (selectedSlot == slot && !targetOpponent);
                int cardIndex = (currentCards != null && slot < currentCards.Length) ? currentCards[slot] : -1;
                CardData cardData = (deckData != null && cardIndex >= 0 && cardIndex < deckData.AvailableCards.Count)
                    ? deckData.AvailableCards[cardIndex] : null;

                GUI.backgroundColor = isSelected ? new Color(0.3f, 0.9f, 0.4f, 1f) : Color.white;
                GUILayout.BeginVertical("box", GUILayout.Width(150), GUILayout.Height(70));

                string slotTitle = isSelected ? $"★ Slot {slot + 1} (Elegido)" : $"Slot {slot + 1}";
                GUILayout.Label($"<b>{slotTitle}</b>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });

                if (cardData != null)
                {
                    string suitIcon = GetSuitIcon(cardData.Suit);
                    GUILayout.Label($"<b>{cardData.Number} {suitIcon} {cardData.Suit}</b>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
                    GUILayout.Label($"Rank: {cardData.TrucoRank} | Env: {cardData.EnvidoValue}", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10 });
                }
                else
                {
                    GUILayout.Label("<i>(Vacío / Jugada)</i>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
                }

                if (GUILayout.Button(isSelected ? "Seleccionado" : "Elegir Slot", GUILayout.Height(20)))
                {
                    selectedSlot = slot;
                    targetOpponent = false;
                }

                GUILayout.EndVertical();
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            // Destino de asignación: Mi mano vs Mano Rival
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = !targetOpponent ? new Color(0.3f, 0.9f, 0.4f, 1f) : Color.white;
            if (GUILayout.Button("👤 Asignar a Mi Mano", GUILayout.Height(24)))
            {
                targetOpponent = false;
            }
            GUI.backgroundColor = targetOpponent ? new Color(1f, 0.5f, 0.3f, 1f) : Color.white;
            if (GUILayout.Button("🤖 Asignar a Mano Rival", GUILayout.Height(24)))
            {
                targetOpponent = true;
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            // Inspección del rival
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(showOpponentCards ? "Ocultar Cartas del Rival" : "👁️ Inspeccionar Cartas del Rival", GUILayout.Height(22)))
            {
                showOpponentCards = !showOpponentCards;
            }
            GUILayout.EndHorizontal();

            if (showOpponentCards && player != null)
            {
                List<int> oppCards = player.AdminGetOpponentCards();
                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>Cartas en Mano del Rival:</b>", new GUIStyle(GUI.skin.label) { richText = true });
                if (oppCards != null && oppCards.Count > 0)
                {
                    for (int i = 0; i < oppCards.Count; i++)
                    {
                        int oppIdx = oppCards[i];
                        CardData oppCard = (deckData != null && oppIdx >= 0 && oppIdx < deckData.AvailableCards.Count)
                            ? deckData.AvailableCards[oppIdx] : null;
                        string info = oppCard != null ? $"{oppCard.Number} {GetSuitIcon(oppCard.Suit)} {oppCard.Suit} (Rank {oppCard.TrucoRank} | Env {oppCard.EnvidoValue})" : $"Índice #{oppIdx}";
                        GUILayout.Label($"  • Carta {i + 1}: {info}");
                    }
                }
                else
                {
                    GUILayout.Label("  <i>(Sin cartas o rival no disponible)</i>", new GUIStyle(GUI.skin.label) { richText = true });
                }
                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();

            GUILayout.Space(8);

            // 2. SELECTOR DE CARTA POR PALO
            GUILayout.BeginVertical("box");
            string targetLabel = targetOpponent ? "<color=#FF7043>Mano del Rival</color>" : "<color=#4CAF50>Tu Mano</color>";
            GUILayout.Label($"🎯 Selector de Carta (Asignando a Slot {selectedSlot + 1} de {targetLabel}):", new GUIStyle(headerStyle) { richText = true });

            // Filtros de Palo
            GUILayout.BeginHorizontal();
            string[] suitNames = { "✨ Todas", "⚔️ Espada", "🪵 Basto", "🪙 Oro", "🏆 Copa" };
            for (int s = -1; s <= 3; s++)
            {
                bool isSuitSelected = (selectedSuitFilter == s);
                GUI.backgroundColor = isSuitSelected ? new Color(0.4f, 0.8f, 1f, 1f) : Color.white;
                if (GUILayout.Button(suitNames[s + 1], GUILayout.Height(24)))
                {
                    selectedSuitFilter = s;
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            // Grilla de Cartas
            if (deckData != null && deckData.AvailableCards != null)
            {
                int columns = 2;
                int currentCol = 0;
                GUILayout.BeginVertical();

                for (int i = 0; i < deckData.AvailableCards.Count; i++)
                {
                    CardData card = deckData.AvailableCards[i];
                    if (card == null) continue;

                    if (selectedSuitFilter >= 0 && (int)card.Suit != selectedSuitFilter)
                        continue;

                    if (currentCol == 0) GUILayout.BeginHorizontal();

                    string suitIcon = GetSuitIcon(card.Suit);
                    string label = $"{card.Number} {suitIcon} {card.Suit} [R:{card.TrucoRank}|E:{card.EnvidoValue}]";

                    if (GUILayout.Button(label, GUILayout.Height(26), GUILayout.Width(235)))
                    {
                        ApplyCardToSelectedSlot(player, i);
                    }

                    currentCol++;
                    if (currentCol >= columns)
                    {
                        GUILayout.EndHorizontal();
                        currentCol = 0;
                    }
                }

                if (currentCol > 0)
                {
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.Label("No se encontró el mazo principal (MainDeck).");
            }

            GUILayout.EndVertical();

            GUILayout.Space(8);

            // 3. PRESETS RÁPIDOS (COMBOS CLÁSICOS)
            GUILayout.BeginVertical("box");
            GUILayout.Label("⚡ Presets de Manos Clásicas:", headerStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("⚔️ 1E, 1B, 7E\n(Mano Invencible)", GUILayout.Height(36)))
            {
                ApplyPreset(player, CardSuit.Espada, 1, CardSuit.Basto, 1, CardSuit.Espada, 7);
            }
            if (GUILayout.Button("🔥 Envido 33\n(7E, 6E, 1E)", GUILayout.Height(36)))
            {
                ApplyPreset(player, CardSuit.Espada, 7, CardSuit.Espada, 6, CardSuit.Espada, 1);
            }
            if (GUILayout.Button("🔥 Envido 32\n(7O, 5O, 1B)", GUILayout.Height(36)))
            {
                ApplyPreset(player, CardSuit.Oro, 7, CardSuit.Oro, 5, CardSuit.Basto, 1);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🔥 Envido 31\n(7C, 4C, 2E)", GUILayout.Height(36)))
            {
                ApplyPreset(player, CardSuit.Copa, 7, CardSuit.Copa, 4, CardSuit.Espada, 2);
            }
            if (GUILayout.Button("👑 Tres Reyes\n(12E, 12B, 12O)", GUILayout.Height(36)))
            {
                ApplyPreset(player, CardSuit.Espada, 12, CardSuit.Basto, 12, CardSuit.Oro, 12);
            }
            if (GUILayout.Button("🎲 Tres 3s\n(3E, 3B, 3O)", GUILayout.Height(36)))
            {
                ApplyPreset(player, CardSuit.Espada, 3, CardSuit.Basto, 3, CardSuit.Oro, 3);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("🪵 Mano Baja\n(4C, 4O, 5B)", GUILayout.Height(36)))
            {
                ApplyPreset(player, CardSuit.Copa, 4, CardSuit.Oro, 4, CardSuit.Basto, 5);
            }
            if (GUILayout.Button("🎲 Mano Aleatoria\n(3 Cartas al Azar)", GUILayout.Height(36)))
            {
                ApplyRandomHand(player);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void ApplyCardToSelectedSlot(PlayerController player, int cardIndex)
        {
            if (player == null) return;

            if (!targetOpponent)
            {
                player.AdminSetSlotCard(selectedSlot, cardIndex);
            }
            else
            {
                List<int> oppCards = player.AdminGetOpponentCards();
                while (oppCards.Count <= selectedSlot) oppCards.Add(-1);
                oppCards[selectedSlot] = cardIndex;
                player.AdminSetOpponentCards(oppCards.ToArray());
            }
        }

        private void ApplyPreset(PlayerController player, CardSuit s1, int n1, CardSuit s2, int n2, CardSuit s3, int n3)
        {
            if (player == null) return;
            LoadDeck();

            int c1 = FindCardIndex(s1, n1);
            int c2 = FindCardIndex(s2, n2);
            int c3 = FindCardIndex(s3, n3);

            if (c1 != -1 && c2 != -1 && c3 != -1)
            {
                int[] hand = new int[] { c1, c2, c3 };
                if (!targetOpponent) player.AdminSetCards(hand);
                else player.AdminSetOpponentCards(hand);
            }
        }

        private void ApplyRandomHand(PlayerController player)
        {
            if (player == null || deckData == null || deckData.AvailableCards == null) return;

            List<int> indices = new List<int>();
            while (indices.Count < 3)
            {
                int rand = UnityEngine.Random.Range(0, deckData.AvailableCards.Count);
                if (!indices.Contains(rand)) indices.Add(rand);
            }

            int[] hand = indices.ToArray();
            if (!targetOpponent) player.AdminSetCards(hand);
            else player.AdminSetOpponentCards(hand);
        }

        private int FindCardIndex(CardSuit suit, int number)
        {
            if (deckData == null || deckData.AvailableCards == null) return -1;
            for (int i = 0; i < deckData.AvailableCards.Count; i++)
            {
                var c = deckData.AvailableCards[i];
                if (c != null && c.Suit == suit && c.Number == number) return i;
            }
            return -1;
        }

        private string GetSuitIcon(CardSuit suit)
        {
            switch (suit)
            {
                case CardSuit.Espada: return "⚔️";
                case CardSuit.Basto: return "🪵";
                case CardSuit.Oro: return "🪙";
                case CardSuit.Copa: return "🏆";
                default: return "";
            }
        }

        // =========================================================================
        // PESTAÑA: PODERES
        // =========================================================================
        private void DrawPowersTab()
        {
            LoadCatalog();
            PlayerController player = FindLocalPlayer();

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 13 };

            GUILayout.BeginVertical("box");
            GUILayout.Label("🔮 Gestión de Poderes", titleStyle);

            string overrideText = overrideNextPower != null
                ? $"<color=#4CAF50><b>{overrideNextPower.DisplayName}</b></color>"
                : "<i>(Aleatorio / Ninguno)</i>";
            GUILayout.Label($"Próximo poder forzado: {overrideText}", new GUIStyle(GUI.skin.label) { richText = true });

            if (GUILayout.Button("Desactivar Forzado (Usar Aleatorio)"))
            {
                overrideNextPower = null;
            }

            GUILayout.Space(6);
            GUILayout.Label("Filtro por Rareza:");
            GUILayout.BeginHorizontal();
            string[] rarities = { "Todos", "Comunes", "Legendarios" };
            for (int r = -1; r <= 1; r++)
            {
                bool isSel = (powerRarityFilter == r);
                GUI.backgroundColor = isSel ? new Color(0.4f, 0.8f, 1f, 1f) : Color.white;
                if (GUILayout.Button(rarities[r + 1], GUILayout.Height(24)))
                {
                    powerRarityFilter = r;
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Catálogo de Poderes:");

            if (powerCatalog != null && powerCatalog.AvailablePowers != null)
            {
                foreach (var power in powerCatalog.AvailablePowers)
                {
                    if (power == null) continue;
                    if (powerRarityFilter == 0 && power.Rarity != PowerRarity.Comun) continue;
                    if (powerRarityFilter == 1 && power.Rarity != PowerRarity.Legendario) continue;

                    GUILayout.BeginHorizontal();
                    bool isSelected = (overrideNextPower == power);
                    string rarityBadge = power.Rarity == PowerRarity.Legendario ? "★ [LEG]" : "[COM]";
                    string btnLabel = isSelected ? $"✓ {rarityBadge} {power.DisplayName}" : $"{rarityBadge} {power.DisplayName}";

                    GUI.backgroundColor = isSelected ? new Color(0.3f, 0.9f, 0.4f, 1f) : Color.white;
                    if (GUILayout.Button(btnLabel, GUILayout.Height(26)))
                    {
                        overrideNextPower = power;
                    }
                    GUI.backgroundColor = Color.white;

                    if (GUILayout.Button("Spawnear", GUILayout.Width(85), GUILayout.Height(26)))
                    {
                        if (player != null) player.SpawnPowerOnTable(power);
                    }
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(8);
            if (GUILayout.Button("Limpiar Todos los Poderes de la Mesa", GUILayout.Height(26)))
            {
                if (player != null)
                {
                    var visualizer = player.GetComponent<PowerHandVisualizer>();
                    if (visualizer != null) visualizer.ClearPowers();
                }
            }

            GUILayout.Space(8);
            GUILayout.Label("🧪 Probar UI de Poderes de la Ronda (Izquierda):");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Activar Mi Poder", GUILayout.Height(26)))
            {
                if (player != null)
                {
                    PowerData power = overrideNextPower != null ? overrideNextPower : (powerCatalog != null ? powerCatalog.GetRandomPower() : null);
                    player.AdminTestActivatePower(power, true);
                }
            }
            if (GUILayout.Button("Activar Poder Rival", GUILayout.Height(26)))
            {
                if (player != null)
                {
                    PowerData power = overrideNextPower != null ? overrideNextPower : (powerCatalog != null ? powerCatalog.GetRandomPower() : null);
                    player.AdminTestActivatePower(power, false);
                }
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Limpiar UI de Poderes Activos", GUILayout.Height(26)))
            {
                if (Trucardo.Core.UI.HUDController.Instance != null)
                {
                    Trucardo.Core.UI.HUDController.Instance.ClearRoundPowers();
                }
            }

            GUILayout.EndVertical();
        }

        // =========================================================================
        // PESTAÑA: PARTIDA
        // =========================================================================
        private void DrawMatchTab()
        {
            PlayerController player = FindLocalPlayer();
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 13 };

            GUILayout.BeginVertical("box");
            GUILayout.Label("📊 Estado y Marcador de la Partida", headerStyle);

            var (p1Score, p2Score) = player != null ? player.AdminGetScores() : (0, 0);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>Jugador 1:</b> {p1Score} pts", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
            GUILayout.Label($"<b>Jugador 2 (Rival):</b> {p2Score} pts", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Ajustes Rápidos de Puntaje:");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1 P1")) player?.AdminSetScores(p1Score + 1, p2Score);
            if (GUILayout.Button("+5 P1")) player?.AdminSetScores(p1Score + 5, p2Score);
            if (GUILayout.Button("-1 P1")) player?.AdminSetScores(Mathf.Max(0, p1Score - 1), p2Score);
            GUILayout.Space(10);
            if (GUILayout.Button("+1 P2")) player?.AdminSetScores(p1Score, p2Score + 1);
            if (GUILayout.Button("+5 P2")) player?.AdminSetScores(p1Score, p2Score + 5);
            if (GUILayout.Button("-1 P2")) player?.AdminSetScores(p1Score, Mathf.Max(0, p2Score - 1));
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Presets de Partida:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("0 a 0 (Inicio)")) player?.AdminSetScores(0, 0);
            if (GUILayout.Button("12 - 10 (Malas)")) player?.AdminSetScores(12, 10);
            if (GUILayout.Button("15 - 14 (A Buenas)")) player?.AdminSetScores(15, 14);
            if (GUILayout.Button("29 - 29 (Match Point)")) player?.AdminSetScores(29, 29);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Controles de Ronda:");
            if (GUILayout.Button("🔄 Reiniciar Partida Completa", GUILayout.Height(30)))
            {
                player?.RestartMatchFromHud();
            }

            GUILayout.EndVertical();
        }

        // =========================================================================
        // PESTAÑA: TESTS
        // =========================================================================
        private void DrawTestsTab()
        {
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 13 };

            GUILayout.BeginVertical("box");
            GUILayout.Label("🧪 Suite de Tests Automatizados de Trucardo", headerStyle);
            GUILayout.Label("Verifica la jerarquía de las 40 cartas, resolución de manos, reglas y cálculo de Envido, y el catálogo y efectos de poderes.");

            GUILayout.Space(6);
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.35f, 1f);
            if (GUILayout.Button("▶️ EJECUTAR TODOS LOS TESTS AUTOMÁTICOS", GUILayout.Height(36)))
            {
                testSummary = TrucardoTestRunner.RunAllTests();
            }
            GUI.backgroundColor = Color.white;

            if (testSummary != null)
            {
                GUILayout.Space(8);
                string statusColor = testSummary.Failed == 0 ? "#4CAF50" : "#FF5252";
                GUILayout.Label($"<b>Resultado:</b> <color={statusColor}>{testSummary.Passed}/{testSummary.Total} Tests Pasados ({testSummary.Failed} fallados)</color>",
                    new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });

                // Filtros de Categoría
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                string[] categories = { "Todos", "Jerarquía", "Resolución Truco", "Cálculo Envido", "Reglas Envido", "Catálogo de Poderes", "Efectos Poderes", "Solo Fallados" };
                for (int i = 0; i < categories.Length; i++)
                {
                    bool isSel = testCategoryFilter == categories[i];
                    GUI.backgroundColor = isSel ? new Color(0.4f, 0.8f, 1f, 1f) : Color.white;
                    if (GUILayout.Button(categories[i], GUILayout.Height(22)))
                    {
                        testCategoryFilter = categories[i];
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                GUILayout.Space(6);
                GUILayout.Label("Detalle de Pruebas:");

                foreach (var report in testSummary.Reports)
                {
                    if (testCategoryFilter == "Solo Fallados" && report.Passed) continue;
                    if (testCategoryFilter != "Todos" && testCategoryFilter != "Solo Fallados" && report.Category != testCategoryFilter) continue;

                    string icon = report.Passed ? "<color=#4CAF50>✔</color>" : "<color=#FF5252>✖</color>";
                    GUILayout.Label($"{icon} <b>[{report.Category}]</b> {report.Name} — <i>{report.Message}</i>",
                        new GUIStyle(GUI.skin.label) { richText = true });
                }
            }

            GUILayout.EndVertical();
        }
    }
}
