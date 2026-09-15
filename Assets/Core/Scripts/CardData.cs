using UnityEngine;

namespace Trucardo.Core
{
    public enum CardSuit
    {
        Espada,
        Basto,
        Oro,
        Copa
    }

    /// <summary>
    /// Estructura de datos para cada carta. 
    /// Permite a los Game Designers balancear o modificar propiedades desde el Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "Trucardo/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Información Básica")]
        public CardSuit Suit;
        [Tooltip("El número de la carta (1 al 12, omitiendo 8 y 9)")]
        public int Number;

        [Header("Reglas del Truco")]
        [Tooltip("Poder relativo en el Truco. Mayor número = le gana a cartas de menor número.")]
        public int TrucoRank;
        
        [Tooltip("Valor sumatorio para el Envido (Ej: el 7 vale 7, los reyes valen 0).")]
        public int EnvidoValue;

        [Header("Visuales (Fase 1 / Fase 2)")]
        [Tooltip("El Prefab 3D que se instanciará en la mesa cuando el jugador tire esta carta.")]
        public GameObject CardPrefab;
        
        [Tooltip("Textura o imagen 2D para la UI (opcional si se maneja 100% 3D).")]
        public Sprite CardSprite;
    }
}
