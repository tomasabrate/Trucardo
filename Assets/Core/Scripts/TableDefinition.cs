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

            // 1. Buscar en todas las mesas activas cuál es la dueña explícita de este asiento
            TableDefinition[] all = FindAll();
            foreach (var table in all)
            {
                if (table != null && table.OwnsSeat(seat))
                    return table;
            }

            // 2. Buscar en la lista estática registrada
            for (int i = 0; i < allTables.Count; i++)
            {
                if (allTables[i] != null && allTables[i].OwnsSeat(seat))
                    return allTables[i];
            }

            // 3. Si el asiento tiene una TableDefinition en sus ancestros Y está a distancia válida de juego (<= 2m)
            Transform current = seat;
            while (current != null)
            {
                TableDefinition definition = current.GetComponent<TableDefinition>();
                if (definition != null && (definition.OwnsSeat(seat) || (seat.IsChildOf(definition.transform) && HorizontalDistance(definition.transform.position, seat.position) <= 2.0f)))
                    return definition;
                current = current.parent;
            }

            // 4. Si el asiento es hijo directo o indirecto de la mesa y está a distancia válida de juego (<= 2m)
            foreach (var table in all)
            {
                if (table != null && seat.IsChildOf(table.transform) && HorizontalDistance(table.transform.position, seat.position) <= 2.0f)
                    return table;
            }

            // Si es un banco de la barra, banco de afuera, o cualquier asiento no vinculado a una mesa de juego:
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
                .ToList();
            
            List<SeatInteractable> remainingSeats = group.GetComponentsInChildren<Transform>(true)
                .Where(t => t.gameObject.activeInHierarchy && IsSeatName(t.name))
                .Select(t => t.GetComponent<SeatInteractable>() ?? t.gameObject.AddComponent<SeatInteractable>())
                .ToList();

            foreach (var tableTransform in tables)
            {
                string id = tableTransform.name; // Respetar el nombre original en jerarquía (e.g., Mesa_9)
                
                // Buscar asientos que sean Hijos de esta mesa específica ordenados por posición local Y a distancia válida (<= 2m)
                List<SeatInteractable> mySeats = remainingSeats
                    .Where(s => s.transform.IsChildOf(tableTransform) && HorizontalDistance(tableTransform.position, s.transform.position) <= 2.0f)
                    .OrderBy(s => s.transform.localPosition.x)
                    .Take(2)
                    .ToList();

                // Si no tiene 2 asientos hijos, buscamos los más cercanos globalmente
                if (mySeats.Count < 2)
                {
                    int needed = 2 - mySeats.Count;
                    var nearest = remainingSeats
                        .Where(s => !s.transform.IsChildOf(tableTransform))
                        .OrderBy(seat => HorizontalDistance(seat.transform.position, tableTransform.position))
                        .Take(needed);
                    mySeats.AddRange(nearest);
                }

                if (mySeats.Count != 2)
                    continue;

                // Ordenar SIEMPRE por posición local en X respecto a la mesa
                // local X < 0 es Banco A, local X > 0 es Banco B
                mySeats = mySeats.OrderBy(s => tableTransform.InverseTransformPoint(s.transform.position).x).ToList();

                mySeats[0].name = $"{id}_Banco_A";
                mySeats[1].name = $"{id}_Banco_B";
                TableDefinition definition = tableTransform.gameObject.AddComponent<TableDefinition>();
                definition.Configure(id, mySeats[0], mySeats[1]);
                
                remainingSeats.Remove(mySeats[0]);
                remainingSeats.Remove(mySeats[1]);
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

        public static bool IsTableName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string lowerName = name.ToLowerInvariant();
            return lowerName.Contains("mesa") && lowerName != "mesas_y_sillas" && !lowerName.Contains("mesalarga");
        }

        public static bool IsSeatName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string lowerName = name.ToLowerInvariant();
            return lowerName.Contains("banco") || lowerName.Contains("banquito") || lowerName.Contains("silla") || lowerName.Contains("seat");
        }
    }
}
