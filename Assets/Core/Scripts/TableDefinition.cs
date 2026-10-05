using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Relación explícita entre una mesa y los dos bancos desde los que se puede jugar.
    /// </summary>
    public class TableDefinition : MonoBehaviour
    {
        [SerializeField] private string tableId = "";
        [SerializeField] private SeatInteractable[] seats = new SeatInteractable[2];

        private static readonly List<TableDefinition> allTables = new List<TableDefinition>();

        public string TableId => tableId;
        public IReadOnlyList<SeatInteractable> Seats => seats;

        private void Awake()
        {
            Register();
        }

        private void OnDestroy()
        {
            allTables.Remove(this);
        }

        public void Configure(string id, SeatInteractable firstSeat, SeatInteractable secondSeat)
        {
            tableId = id;
            seats = new[] { firstSeat, secondSeat };
        }

        public bool OwnsSeat(Transform seat)
        {
            if (seat == null) return false;

            for (int i = 0; i < seats.Length; i++)
            {
                if (seats[i] != null && (seats[i].transform == seat || seats[i].transform.IsChildOf(seat)))
                    return true;
            }

            return false;
        }

        public static TableDefinition FindForSeat(Transform seat)
        {
            if (seat == null) return null;

            TableDefinition nearestTable = FindAll()
                .Where(table => table != null)
                .OrderBy(table => HorizontalDistance(table.transform.position, seat.position))
                .FirstOrDefault();
            if (nearestTable != null)
            {
                return nearestTable;
            }

            Transform current = seat;
            while (current != null)
            {
                TableDefinition definition = current.GetComponent<TableDefinition>();
                if (definition != null && definition.OwnsSeat(seat))
                    return definition;
                current = current.parent;
            }

            for (int i = 0; i < allTables.Count; i++)
            {
                if (allTables[i] != null && allTables[i].OwnsSeat(seat))
                    return allTables[i];
            }

            return null;
        }

        public static TableDefinition FindById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            for (int i = 0; i < allTables.Count; i++)
            {
                if (allTables[i] != null && allTables[i].tableId == id)
                    return allTables[i];
            }

            return null;
        }

        public static TableDefinition[] FindAll()
        {
            return FindObjectsByType<TableDefinition>(FindObjectsSortMode.None);
        }

        public static void EnsureSceneConfiguration()
        {
            if (FindAll().Length > 0)
                return;

            GameObject group = GameObject.Find("Mesas_y_Sillas");
            if (group == null)
                return;

            List<Transform> tables = group.GetComponentsInChildren<Transform>(true)
                .Where(t => t != group.transform && IsTableName(t.name))
                .Where(t => t.parent == group.transform
                    || !IsTableName(t.parent != null ? t.parent.name : ""))
                .OrderBy(t => t.position.x)
                .ThenBy(t => t.position.z)
                .ToList();
            List<SeatInteractable> remainingSeats = group.GetComponentsInChildren<Transform>(true)
                .Where(t => IsSeatName(t.name))
                .Select(t => t.GetComponent<SeatInteractable>() ?? t.gameObject.AddComponent<SeatInteractable>())
                .ToList();

            for (int i = 0; i < tables.Count; i++)
            {
                List<SeatInteractable> nearestSeats = remainingSeats
                    .OrderBy(seat => HorizontalDistance(seat.transform.position, tables[i].position))
                    .Take(2)
                    .ToList();
                if (nearestSeats.Count != 2)
                    continue;

                string id = $"Mesa_{i + 1}";
                tables[i].name = id;
                nearestSeats[0].name = $"{id}_Banco_A";
                nearestSeats[1].name = $"{id}_Banco_B";
                TableDefinition definition = tables[i].gameObject.AddComponent<TableDefinition>();
                definition.Configure(id, nearestSeats[0], nearestSeats[1]);
                remainingSeats.Remove(nearestSeats[0]);
                remainingSeats.Remove(nearestSeats[1]);
            }
        }

        private void Register()
        {
            if (!allTables.Contains(this))
                allTables.Add(this);
        }

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            first.y = 0f;
            second.y = 0f;
            return Vector3.Distance(first, second);
        }

        private static bool IsTableName(string name)
        {
            string lowerName = name.ToLowerInvariant();
            return lowerName.Contains("mesa") && lowerName != "mesas_y_sillas";
        }

        private static bool IsSeatName(string name)
        {
            string lowerName = name.ToLowerInvariant();
            return lowerName.StartsWith("silla") || lowerName.StartsWith("banco");
        }
    }
}
