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
                if (seats[i] != null && (seats[i].transform == seat || seats[i].transform.IsChildOf(seat) || seat.IsChildOf(seats[i].transform)))
                    return true;
            }

            return false;
        }

        public static bool IsNonGameSeat(Transform seat)
        {
            if (seat == null) return false;
            Transform current = seat;
            while (current != null)
            {
                string n = current.name.ToLowerInvariant();
                // Asientos de descanso específicos: barra del bar, mostrador, exterior o patio
                if (n.Contains("barra") || n.Contains("exterior") || n.Contains("mostrador") || n.Contains("patio"))
                    return true;
                current = current.parent;
            }
            return false;
        }

        public static TableDefinition FindForSeat(Transform seat)
        {
            if (seat == null) return null;

            // 1. Buscar en todas las mesas registradas o activas cuál es la dueña explícita de este asiento
            TableDefinition[] all = FindAll();
            foreach (var table in all)
            {
                if (table != null && table.OwnsSeat(seat))
                    return table;
            }

            for (int i = 0; i < allTables.Count; i++)
            {
                if (allTables[i] != null && allTables[i].OwnsSeat(seat))
                    return allTables[i];
            }

            // 2. Si el asiento está en la jerarquía de una mesa con TableDefinition
            Transform current = seat;
            while (current != null)
            {
                TableDefinition definition = current.GetComponent<TableDefinition>();
                if (definition != null)
                    return definition;
                current = current.parent;
            }

            // 3. Si el asiento es hijo directo o indirecto de una mesa a distancia válida de juego (<= 2.2m)
            foreach (var table in all)
            {
                if (table != null && seat.IsChildOf(table.transform) && HorizontalDistance(table.transform.position, seat.position) <= 2.2f)
                    return table;
            }

            // 4. Si es un asiento de descanso explícito (barra, exterior, mostrador), no vincular a mesas de juego
            if (IsNonGameSeat(seat))
                return null;

            // 5. Fallback por proximidad a la mesa de juego más cercana (<= 2.0m)
            TableDefinition nearestTable = null;
            float nearestDistance = float.MaxValue;
            foreach (var table in all)
            {
                if (table == null) continue;
                float distance = HorizontalDistance(table.transform.position, seat.position);
                if (distance < nearestDistance && distance <= 2.0f)
                {
                    nearestDistance = distance;
                    nearestTable = table;
                }
            }

            return nearestTable;
        }

        public static TableDefinition FindById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            for (int i = 0; i < allTables.Count; i++)
            {
                if (allTables[i] != null && allTables[i].tableId == id)
                    return allTables[i];
            }

            TableDefinition[] all = FindAll();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].tableId == id)
                    return all[i];
            }

            return null;
        }

        public static TableDefinition[] FindAll()
        {
            return FindObjectsByType<TableDefinition>(FindObjectsSortMode.None);
        }

        public static void EnsureSceneConfiguration()
        {
            TableDefinition[] existing = FindAll();
            bool allConfiguredProperly = existing.Length >= 9 && existing.All(t => 
                t.Seats != null && t.Seats.Count == 2 && t.Seats[0] != null && t.Seats[1] != null &&
                HorizontalDistance(t.transform.position, t.Seats[0].transform.position) <= 2.2f &&
                HorizontalDistance(t.transform.position, t.Seats[1].transform.position) <= 2.2f &&
                (
                    (t.TableId != null && t.TableId.Contains("9"))
                        ? (t.Seats[0].transform.position.x > t.Seats[1].transform.position.x)
                        : (t.transform.InverseTransformPoint(t.Seats[0].transform.position).x > t.transform.InverseTransformPoint(t.Seats[1].transform.position).x)
                )
            );
            if (allConfiguredProperly)
                return;

            GameObject group = GameObject.Find("Mesas_y_Sillas");
            if (group == null)
                return;

            // Encontrar todas las mesas de juego (Mesa_1 a Mesa_9)
            List<Transform> tables = new List<Transform>();
            for (int i = 0; i < group.transform.childCount; i++)
            {
                Transform child = group.transform.GetChild(i);
                if (IsTableName(child.name))
                {
                    tables.Add(child);
                }
            }

            if (tables.Count == 0)
            {
                tables = group.GetComponentsInChildren<Transform>(true)
                    .Where(t => t != group.transform && IsTableName(t.name))
                    .Where(t => t.parent == group.transform || !IsTableName(t.parent != null ? t.parent.name : ""))
                    .ToList();
            }

            foreach (var tableTransform in tables)
            {
                string id = tableTransform.name; // Ej: Mesa_1

                // Limpiar objetos vacíos o lejanos que pudieran haber quedado emparentados por error
                for (int i = tableTransform.childCount - 1; i >= 0; i--)
                {
                    Transform child = tableTransform.GetChild(i);
                    if (IsSeatName(child.name))
                    {
                        bool isDistant = HorizontalDistance(tableTransform.position, child.position) > 2.2f;
                        bool hasNoRenderers = child.GetComponentsInChildren<Renderer>(true).Length == 0;
                        if (isDistant && hasNoRenderers)
                        {
                            if (Application.isPlaying) Destroy(child.gameObject);
                            else DestroyImmediate(child.gameObject);
                        }
                    }
                }

                // Cada mesa de juego tiene sus 2 sillas como hijas directas en la escena
                List<SeatInteractable> mySeats = new List<SeatInteractable>();

                for (int i = 0; i < tableTransform.childCount; i++)
                {
                    Transform child = tableTransform.GetChild(i);
                    if (IsSeatName(child.name) && HorizontalDistance(tableTransform.position, child.position) <= 2.2f)
                    {
                        // Asegurar el componente SeatInteractable en la raíz de la silla
                        SeatInteractable seat = child.GetComponent<SeatInteractable>();
                        if (seat == null) seat = child.gameObject.AddComponent<SeatInteractable>();

                        // Remover componentes SeatInteractable redundantes en los sub-hijos (ej. BanquitoLowPoly)
                        // para que solo exista UN único SeatInteractable por silla física
                        var subSeats = child.GetComponentsInChildren<SeatInteractable>(true);
                        foreach (var sub in subSeats)
                        {
                            if (sub != seat)
                            {
                                if (Application.isPlaying) Destroy(sub);
                                else DestroyImmediate(sub);
                            }
                        }

                        mySeats.Add(seat);
                    }
                }

                // Fallback de seguridad si no encontró 2 sillas por hijos directos (<= 2m)
                if (mySeats.Count < 2)
                {
                    var nearby = group.GetComponentsInChildren<Transform>(true)
                        .Where(t => IsSeatName(t.name) && t != tableTransform && !t.IsChildOf(tableTransform))
                        .Where(t => HorizontalDistance(tableTransform.position, t.position) <= 2.0f)
                        .OrderBy(t => HorizontalDistance(tableTransform.position, t.position));

                    foreach (var cand in nearby)
                    {
                        Transform chairRoot = cand;
                        if (chairRoot.parent != null && IsSeatName(chairRoot.parent.name))
                            chairRoot = chairRoot.parent;

                        SeatInteractable s = chairRoot.GetComponent<SeatInteractable>() ?? chairRoot.gameObject.AddComponent<SeatInteractable>();
                        if (!mySeats.Contains(s))
                        {
                            mySeats.Add(s);
                            if (mySeats.Count == 2) break;
                        }
                    }
                }

                if (mySeats.Count < 2)
                {
                    Debug.LogWarning($"[TableDefinition] Mesa {id} no pudo encontrar 2 asientos ({mySeats.Count} encontrados).");
                    continue;
                }

                if (id == "Mesa_9" || id.Contains("9"))
                {
                    // En Mesa 9, el Bartender se sienta en el banco contra la pared (Banco B / Seats[1]),
                    // y el jugador se sienta en el banco del salón mirando hacia la pared (Banco A / Seats[0]).
                    // El banco contra la pared tiene menor X en mundo (x ≈ -8.4), y el del salón mayor X (x ≈ -5.9).
                    SeatInteractable wall = mySeats.OrderBy(s => s.transform.position.x).First();
                    SeatInteractable player = mySeats.OrderBy(s => s.transform.position.x).Last();
                    mySeats = new List<SeatInteractable> { player, wall };
                }
                else
                {
                    // Ordenar por posición local en X respecto a la mesa:
                    // local X > 0 es Banco A (Jugador Local)
                    // local X < 0 es Banco B (Rival / Bot)
                    mySeats = mySeats.OrderByDescending(s => tableTransform.InverseTransformPoint(s.transform.position).x).ToList();
                }

                mySeats[0].name = $"{id}_Banco_A";
                mySeats[1].name = $"{id}_Banco_B";

                TableDefinition definition = tableTransform.GetComponent<TableDefinition>();
                if (definition == null) definition = tableTransform.gameObject.AddComponent<TableDefinition>();
                definition.Configure(id, mySeats[0], mySeats[1]);
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
