using System.Collections.Generic;
using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Ordena los poderes sobre la mesa en una cuadrícula compacta en el sector izquierdo (perspectiva jugador).
    /// Soporta tanto los poderes del jugador local como los del oponente frente a él.
    /// </summary>
    public class PowerHandVisualizer : MonoBehaviour
    {
        public List<PowerVisual> powersOnTable = new List<PowerVisual>();
        public Transform tableCenter;
        public bool isOpponent = false;

        [Header("Configuración de Cuadrícula en Mesa")]
        public float baseLateralOffset = -0.48f; // Sector izquierdo de la mesa (respecto al jugador)
        public float colSpacing = 0.15f;         // Separación entre columnas (ampliada a 15cm para mejor aprovechamiento del espacio)
        public float rowSpacing = 0.15f;         // Separación entre filas (ampliada a 15cm)
        public float playerBaseDepth = -0.42f;   // Profundidad en el lado del jugador (frente a sus cartas)
        public float opponentBaseDepth = 0.42f;  // Profundidad en el lado del oponente (frente a las cartas rivales)
        public float heightOffset = 0.82f;
        public float modelCenterHeight = 0.005f; // Base del modelo apoyada directamente sobre la superficie de la mesa (5mm anti z-fighting)

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

        public void ClearPowers()
        {
            for (int i = powersOnTable.Count - 1; i >= 0; i--)
            {
                if (powersOnTable[i] != null)
                {
                    Destroy(powersOnTable[i].gameObject);
                }
            }
            powersOnTable.Clear();
        }

        public void RefreshLayout()
        {
            ArrangePowers();
        }

        private void ArrangePowers()
        {
            if (powersOnTable.Count == 0) return;

            Vector3 playerPosition = transform.position;
            Vector3 towardTable = tableCenter != null
                ? tableCenter.position - playerPosition
                : transform.forward;
            towardTable.y = 0f;
            if (towardTable.sqrMagnitude < 0.001f)
            {
                towardTable = transform.forward;
                towardTable.y = 0f;
            }
            towardTable.Normalize();

            Vector3 tableRight = Vector3.Cross(Vector3.up, towardTable).normalized;
            Vector3 centerAnchor = tableCenter != null ? tableCenter.position : playerPosition + towardTable * 1.0f;
            float surfaceHeight = GetTableSurfaceHeight(playerPosition.y + heightOffset);

            for (int i = 0; i < powersOnTable.Count; i++)
            {
                if (powersOnTable[i] == null) continue;

                // Cuadrícula de 2 columnas x 2 filas
                int col = i % 2;
                int row = i / 2;

                // Desplazamiento lateral (ambos en el lado izquierdo desde el punto de vista del jugador)
                float lateral = baseLateralOffset + (col == 0 ? -colSpacing * 0.5f : colSpacing * 0.5f);

                // Profundidad según sea local o rival
                float depth;
                if (!isOpponent)
                {
                    // Lado del jugador local: fila 0 más cercana al jugador, fila 1 más hacia el centro
                    depth = playerBaseDepth + (row == 0 ? -rowSpacing * 0.5f : rowSpacing * 0.5f);
                }
                else
                {
                    // Lado del rival: fila 0 más cercana al rival, fila 1 más hacia el centro
                    depth = opponentBaseDepth + (row == 0 ? rowSpacing * 0.5f : -rowSpacing * 0.5f);
                }

                Vector3 targetPosition = centerAnchor
                    + towardTable * depth
                    + tableRight * lateral;
                targetPosition.y = surfaceHeight + modelCenterHeight;

                float yRotation = Mathf.Atan2(towardTable.x, towardTable.z) * Mathf.Rad2Deg;
                powersOnTable[i].AnimateTo(
                    targetPosition,
                    Quaternion.Euler(0f, yRotation, 0f),
                    0.35f);
            }
        }

        private float GetTableSurfaceHeight(float fallbackHeight)
        {
            if (tableCenter == null) return fallbackHeight;

            Renderer[] renderers = tableCenter.GetComponentsInChildren<Renderer>(true);
            float highestSurface = float.NegativeInfinity;

            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled || renderer.bounds.size.y > 1.5f) continue;

                if (renderer.bounds.max.y > highestSurface)
                {
                    highestSurface = renderer.bounds.max.y;
                }
            }

            return float.IsNegativeInfinity(highestSurface)
                ? fallbackHeight
                : highestSurface;
        }
    }
}
