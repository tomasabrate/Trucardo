using UnityEngine;

namespace Trucardo.Core
{
    public class SeatInteractable : MonoBehaviour
    {
        [SerializeField] private GameObject currentOccupant;

        public GameObject CurrentOccupant
        {
            get => currentOccupant;
            set => currentOccupant = value;
        }

        public bool IsOccupied
        {
            get
            {
                if (currentOccupant != null)
                    return true;

                return IsPhysicallyOccupied();
            }
        }

        public bool IsPhysicallyOccupied(float maxDistance = 0.6f)
        {
            Vector3 seatPos = transform.position;

            // 1. Verificar si hay jugadores humanos sentados en este banco
            PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (PlayerController player in allPlayers)
            {
                Vector3 playerPos = player.transform.position;
                float horizDist = Mathf.Sqrt((playerPos.x - seatPos.x) * (playerPos.x - seatPos.x) + (playerPos.z - seatPos.z) * (playerPos.z - seatPos.z));
                if (horizDist < maxDistance && Mathf.Abs(playerPos.y - seatPos.y) < 1.2f)
                {
                    return true;
                }
            }

            // 2. Verificar si hay bots con BotMarker asignados o cerca de este banco
            BotMarker[] allBots = FindObjectsByType<BotMarker>(FindObjectsSortMode.None);
            foreach (BotMarker bot in allBots)
            {
                if (bot.assignedSeat == this)
                    return true;

                Vector3 botPos = bot.transform.position;
                float horizDist = Mathf.Sqrt((botPos.x - seatPos.x) * (botPos.x - seatPos.x) + (botPos.z - seatPos.z) * (botPos.z - seatPos.z));
                if (horizDist < maxDistance && Mathf.Abs(botPos.y - seatPos.y) < 1.2f)
                {
                    return true;
                }
            }

            // 3. Fallback: verificar cualquier GameObject con "Bot" en el nombre dentro del rango
            GameObject[] allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject go in allGameObjects)
            {
                if (go.name.StartsWith("Bot ", System.StringComparison.OrdinalIgnoreCase))
                {
                    Vector3 botPos = go.transform.position;
                    float horizDist = Mathf.Sqrt((botPos.x - seatPos.x) * (botPos.x - seatPos.x) + (botPos.z - seatPos.z) * (botPos.z - seatPos.z));
                    if (horizDist < maxDistance && Mathf.Abs(botPos.y - seatPos.y) < 1.2f)
                    {
                        return true;
                    }
                }
            }

            // 4. Si la mesa tiene este asiento reservado para el bot en Singleplayer (Banco B)
            TableDefinition table = TableDefinition.FindForSeat(transform);
            if (table != null && table.Seats != null && table.Seats.Count >= 2 && table.Seats[1] == this)
            {
                if (Trucardo.Core.NetworkUI.selectedMode == GameMode.Singleplayer || allBots.Length > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public float GetTopHeight()
        {
            float topHeight = float.NegativeInfinity;
            Renderer[] renderers = GetComponentsInChildren<Renderer>();

            foreach (Renderer objectRenderer in renderers)
            {
                if (objectRenderer.enabled)
                    topHeight = Mathf.Max(topHeight, objectRenderer.bounds.max.y);
            }

            if (!float.IsNegativeInfinity(topHeight))
                return topHeight;

            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider objectCollider in colliders)
            {
                if (objectCollider.enabled && !objectCollider.isTrigger)
                    topHeight = Mathf.Max(topHeight, objectCollider.bounds.max.y);
            }

            return float.IsNegativeInfinity(topHeight) ? transform.position.y : topHeight;
        }
    }

    public class BotMarker : MonoBehaviour
    {
        public SeatInteractable assignedSeat;
    }
}