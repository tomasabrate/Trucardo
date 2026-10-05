using System.Collections.Generic;
using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Ordena los poderes del jugador a la izquierda de sus cartas.
    /// </summary>
    public class PowerHandVisualizer : MonoBehaviour
    {
        public List<PowerVisual> powersOnTable = new List<PowerVisual>();
        public Transform tableCenter;
        public float sideSpacing = 0.7f;
        public float heightOffset = 0.84f;
        public float tableEdgeDistance = 2.4f;
        public float forwardDistance = 0.35f;
        public float lateralOffset = -0.95f;

        public void AddPower(PowerVisual power)
        {
            if (!powersOnTable.Contains(power))
            {
                powersOnTable.Add(power);
                ArrangePowers();
            }
        }

        public void RemovePower(PowerVisual power)
        {
            if (powersOnTable.Remove(power))
            {
                ArrangePowers();
            }
        }

        private void ArrangePowers()
        {
            if (powersOnTable.Count == 0) return;

            Vector3 playerPosition = transform.position;
            Vector3 towardTable = tableCenter != null
                ? tableCenter.position - playerPosition
                : transform.forward;
            towardTable.y = 0f;
            towardTable = towardTable.sqrMagnitude > 0.001f
                ? towardTable.normalized
                : transform.forward;
            towardTable.y = 0f;
            towardTable.Normalize();

            Vector3 tableRight = Vector3.Cross(Vector3.up, towardTable).normalized;
            float totalWidth = (powersOnTable.Count - 1) * sideSpacing;

            for (int i = 0; i < powersOnTable.Count; i++)
            {
                if (powersOnTable[i] == null) continue;

                float spread = i * sideSpacing - totalWidth / 2f;
                Vector3 targetPosition = playerPosition
                    + towardTable * forwardDistance
                    + tableRight * (lateralOffset + spread);
                targetPosition.y = GetTableSurfaceHeight(playerPosition.y + heightOffset);

                float yRotation = Mathf.Atan2(towardTable.x, towardTable.z) * Mathf.Rad2Deg;
                powersOnTable[i].AnimateTo(
                    targetPosition,
                    Quaternion.Euler(0f, yRotation, -35f));
            }
        }

        private float GetTableSurfaceHeight(float fallbackHeight)
        {
            if (tableCenter == null) return fallbackHeight;

            Renderer[] renderers = tableCenter.GetComponentsInChildren<Renderer>(true);
            Renderer surface = null;
            float largestArea = 0f;

            foreach (Renderer renderer in renderers)
            {
                Bounds bounds = renderer.bounds;
                if (bounds.size.y > 1.5f) continue;

                float area = bounds.size.x * bounds.size.z;
                if (area > largestArea)
                {
                    largestArea = area;
                    surface = renderer;
                }
            }

            return surface != null ? surface.bounds.max.y + 0.02f : fallbackHeight;
        }
    }
}
