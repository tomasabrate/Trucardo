using UnityEngine;
using UnityEditor;
using System.IO;
using Trucardo.Core;

namespace Trucardo.EditorScripts
{
    public class DeckGenerator
    {
        [MenuItem("Trucardo/Generar Mazo Base (40 Cartas)")]
        public static void GenerateDeck()
        {
            string folderPath = "Assets/Data/Cards";
            
            // Crear carpetas si no existen
            if (!Directory.Exists("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            if (!Directory.Exists(folderPath)) AssetDatabase.CreateFolder("Assets/Data", "Cards");

            DeckData deckData = ScriptableObject.CreateInstance<DeckData>();

            CardSuit[] suits = { CardSuit.Espada, CardSuit.Basto, CardSuit.Oro, CardSuit.Copa };
            int[] numbers = { 1, 2, 3, 4, 5, 6, 7, 10, 11, 12 };

            foreach (var suit in suits)
            {
                foreach (var number in numbers)
                {
                    CardData card = ScriptableObject.CreateInstance<CardData>();
                    card.Suit = suit;
                    card.Number = number;
                    card.EnvidoValue = (number >= 10) ? 0 : number;
                    card.TrucoRank = GetTrucoRank(number, suit);

                    string assetPath = $"{folderPath}/Card_{suit}_{number}.asset";
                    AssetDatabase.CreateAsset(card, assetPath);
                    deckData.AvailableCards.Add(card);
                }
            }

            AssetDatabase.CreateAsset(deckData, "Assets/Data/MainDeck.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("¡Mazo de 40 cartas generado exitosamente en Assets/Data!");
        }

        private static int GetTrucoRank(int number, CardSuit suit)
        {
            // Jerarquía del Truco: Mayor número = gana
            if (number == 1 && suit == CardSuit.Espada) return 14;
            if (number == 1 && suit == CardSuit.Basto) return 13;
            if (number == 7 && suit == CardSuit.Espada) return 12;
            if (number == 7 && suit == CardSuit.Oro) return 11;
            
            if (number == 3) return 10;
            if (number == 2) return 9;
            if (number == 1 && (suit == CardSuit.Copa || suit == CardSuit.Oro)) return 8;
            
            if (number == 12) return 7;
            if (number == 11) return 6;
            if (number == 10) return 5;
            
            if (number == 7 && (suit == CardSuit.Copa || suit == CardSuit.Basto)) return 4;
            if (number == 6) return 3;
            if (number == 5) return 2;
            if (number == 4) return 1;

            return 0; // Error / Fallback
        }
    }
}
