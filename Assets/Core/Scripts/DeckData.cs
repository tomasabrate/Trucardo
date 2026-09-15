using System.Collections.Generic;
using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Contenedor de todas las cartas disponibles en el juego.
    /// El Dealer lo usará para instanciar el mazo al iniciar la partida.
    /// </summary>
    [CreateAssetMenu(fileName = "DeckData", menuName = "Trucardo/Deck Data")]
    public class DeckData : ScriptableObject
    {
        [Tooltip("Lista con las 40 cartas de la baraja española para el Truco.")]
        public List<CardData> AvailableCards = new List<CardData>();
    }
}
