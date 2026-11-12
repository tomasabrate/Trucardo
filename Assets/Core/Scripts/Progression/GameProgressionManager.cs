using System;
using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Gestiona la progresión del jugador en el boliche, guardando las victorias
    /// contra los 8 gauchos y desbloqueando el Duelo Final contra el Bartender en la Mesa 9.
    /// </summary>
    public class GameProgressionManager : MonoBehaviour
    {
        private static GameProgressionManager instance;
        public static GameProgressionManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<GameProgressionManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("GameProgressionManager");
                        instance = go.AddComponent<GameProgressionManager>();
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        public const int RegularBotsCount = 8;
        private const string PrefKeyPrefix = "Trucardo_Defeated_Mesa_";

        public event Action<int, string> OnBotDefeated;
        public event Action OnAllBotsDefeated;
        public event Action OnProgressionUpdated;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        public bool IsBotDefeated(int tableNumber)
        {
            if (tableNumber < 1 || tableNumber > RegularBotsCount) return false;
            return PlayerPrefs.GetInt(PrefKeyPrefix + tableNumber, 0) == 1;
        }

        public void SetBotDefeated(int tableNumber, bool defeated = true)
        {
            if (tableNumber < 1 || tableNumber > RegularBotsCount) return;

            bool wasAlready = IsBotDefeated(tableNumber);
            PlayerPrefs.SetInt(PrefKeyPrefix + tableNumber, defeated ? 1 : 0);
            PlayerPrefs.Save();

            string botName = GameServer.Instance != null 
                ? GameServer.Instance.GetBotConfigForTable($"Mesa_{tableNumber}").botName 
                : $"Rival Mesa {tableNumber}";

            Debug.Log($"[Progression] Mesa {tableNumber} ({botName}) {(defeated ? "DERROTADO" : "reiniciado")}. ({GetDefeatedCount()}/{RegularBotsCount} superados)");

            if (!wasAlready && defeated)
            {
                OnBotDefeated?.Invoke(tableNumber, botName);
            }

            if (AreAllRegularBotsDefeated())
            {
                Debug.Log("<color=#4CAF50><b>[Progression] ¡TODOS LOS GAUCHOS DERROTADOS! El Bartender se dirige a la Mesa 9 para el Duelo Final.</b></color>");
                OnAllBotsDefeated?.Invoke();
                BartenderController.Instance?.GoToTable9();
            }

            OnProgressionUpdated?.Invoke();
        }

        public bool AreAllRegularBotsDefeated()
        {
            for (int i = 1; i <= RegularBotsCount; i++)
            {
                if (!IsBotDefeated(i)) return false;
            }
            return true;
        }

        public int GetDefeatedCount()
        {
            int count = 0;
            for (int i = 1; i <= RegularBotsCount; i++)
            {
                if (IsBotDefeated(i)) count++;
            }
            return count;
        }

        /// <summary>
        /// Marca a todos los bots regulares como derrotados para probar el evento del Bartender inmediatamente.
        /// </summary>
        [ContextMenu("Derrotar Todos (Modo Test)")]
        public void DefeatAllForTesting()
        {
            for (int i = 1; i <= RegularBotsCount; i++)
            {
                PlayerPrefs.SetInt(PrefKeyPrefix + i, 1);
            }
            PlayerPrefs.Save();

            Debug.Log("<color=#FF9800><b>[Progression] MODO TEST: Los 8 bots marcados como derrotados.</b></color>");
            OnAllBotsDefeated?.Invoke();
            OnProgressionUpdated?.Invoke();
            BartenderController.Instance?.GoToTable9();
        }

        [ContextMenu("Reiniciar Progresión a Cero")]
        public void ResetProgression()
        {
            for (int i = 1; i <= 9; i++)
            {
                PlayerPrefs.DeleteKey(PrefKeyPrefix + i);
            }
            PlayerPrefs.Save();
            Debug.Log("[Progression] Progresión reiniciada a cero.");
            OnProgressionUpdated?.Invoke();
        }
    }
}
