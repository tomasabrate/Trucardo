using System.Collections.Generic;
using UnityEngine;

namespace Trucardo.Core
{
    [CreateAssetMenu(fileName = "PowerCatalog", menuName = "Trucardo/Power Catalog")]
    public class PowerCatalog : ScriptableObject
    {
        public List<PowerData> AvailablePowers = new List<PowerData>();

        public PowerData GetRandomPower(PowerRarity? rarityFilter = null)
        {
            List<PowerData> candidates = new List<PowerData>();
            foreach (var power in AvailablePowers)
            {
                if (power == null || !power.IsValid() || power.PowerId == "test_power") continue;
                if (rarityFilter.HasValue && power.Rarity != rarityFilter.Value) continue;
                candidates.Add(power);
            }

            if (candidates.Count == 0)
            {
                // Fallback to any valid power if filter yielded none
                foreach (var power in AvailablePowers)
                {
                    if (power != null && power.IsValid() && power.PowerId != "test_power") candidates.Add(power);
                }
            }

            if (candidates.Count == 0) return null;
            return candidates[Random.Range(0, candidates.Count)];
        }

        public PowerData GetPowerById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return AvailablePowers.Find(p => p != null && p.PowerId == id);
        }
    }
}
