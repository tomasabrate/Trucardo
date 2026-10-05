using UnityEngine;
using System.Collections.Generic;

namespace Trucardo.Core
{
    public class HandVisualizer : MonoBehaviour
    {
        public List<CardVisual> cardsInHand = new List<CardVisual>();
        
        public float sideSpacing = 0.2f;
        public float heightOffset = 0.82f;
        public float forwardDistance = 1.0f;
        public float lateralOffset = 0f;
        public float tableEdgeDistance = 0.61f;
        public float tiltAngle = -35f;
        public bool isOpponent = false;
        public Transform tableCenter; // Keep for height reference if needed, but not required

        public void AddCardToHand(CardVisual card)
        {
            if (!cardsInHand.Contains(card))
            {
                cardsInHand.Add(card);
                ArrangeHand();
            }
        }

        public void RefreshLayout()
        {
            ArrangeHand();
        }

        public bool HasCards()
        {
            return cardsInHand != null && cardsInHand.Count > 0;
        }
        
        public void RemoveCardFromHand(CardVisual cardPlayed)
        {
            if (cardsInHand.Contains(cardPlayed))
            {
                cardsInHand.Remove(cardPlayed);
                ArrangeHand();
            }
        }

        private void ArrangeHand()
        {
            if (cardsInHand == null || cardsInHand.Count == 0) return;

            float totalWidth = (cardsInHand.Count - 1) * sideSpacing;
            float startSpread = -totalWidth / 2;

            for (int i = 0; i < cardsInHand.Count; i++)
            {
                if (cardsInHand[i] == null) continue;

                float spread = startSpread + (i * sideSpacing);
                
                Vector3 playerPos = transform.position;
                Vector3 towardTable = tableCenter != null
                    ? tableCenter.position - playerPos
                    : transform.forward;
                towardTable.y = 0f;
                if (towardTable.sqrMagnitude < 0.001f)
                {
                    towardTable = transform.forward;
                    towardTable.y = 0f;
                }
                towardTable.Normalize();

                float finalSpread = (isOpponent ? -spread : spread) + lateralOffset;
                Vector3 tableRight = Vector3.Cross(Vector3.up, towardTable).normalized;
                float edgeOffset = isOpponent ? tableEdgeDistance : -tableEdgeDistance;
                Vector3 targetPos = tableCenter != null
                    ? tableCenter.position + towardTable * edgeOffset + tableRight * finalSpread
                    : playerPos + towardTable * forwardDistance + tableRight * finalSpread;

                targetPos.y = GetTableSurfaceHeight(playerPos.y + heightOffset);

                float yRot = Mathf.Atan2(towardTable.x, towardTable.z) * Mathf.Rad2Deg;
                float playerTilt = isOpponent ? -tiltAngle : tiltAngle;
                Quaternion targetRot = Quaternion.Euler(playerTilt, yRot, 0f);

                cardsInHand[i].AnimateTo(targetPos, targetRot, 0.4f, false);
            }
        }

        private float GetTableSurfaceHeight(float fallbackHeight)
        {
            if (tableCenter == null)
            {
                return fallbackHeight;
            }

            float highestSurface = float.NegativeInfinity;
            Renderer[] renderers = tableCenter.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled || renderer.bounds.size.y > 1.5f)
                {
                    continue;
                }

                if (renderer.bounds.max.y > highestSurface)
                {
                    highestSurface = renderer.bounds.max.y;
                }
            }

            return float.IsNegativeInfinity(highestSurface)
                ? fallbackHeight
                : highestSurface + 0.05f;
        }
    }
}
