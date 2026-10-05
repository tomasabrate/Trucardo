using System.Collections.Generic;
using UnityEngine;

namespace Trucardo.Core
{
    [CreateAssetMenu(fileName = "PowerCatalog", menuName = "Trucardo/Power Catalog")]
    public class PowerCatalog : ScriptableObject
    {
        public List<PowerData> AvailablePowers = new List<PowerData>();
    }
}
