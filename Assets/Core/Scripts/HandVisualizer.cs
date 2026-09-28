using UnityEngine;
using System.Collections.Generic;

namespace Trucardo.Core
{
    public class HandVisualizer : MonoBehaviour
    {
        public List<CardVisual> cardsInHand = new List<CardVisual>();
        
        public float sideSpacing = 0.2f;
        public float heightOffset = 0.99f;
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
                
                Vector3 playerForward = transform.forward;
                Vector3 playerRight = transform.right;
                Vector3 playerPos = transform.position;
                
                // Si es la mano local, a 0.7m de distancia. Si es del oponente, a 1.8m de distancia.
                float forwardDistance = isOpponent ? 1.8f : 0.7f;
                // Si es del oponente, invertimos el spread para que se vea bien desde su perspectiva
                float finalSpread = isOpponent ? -spread : spread;

                Vector3 targetPos = playerPos + playerForward * forwardDistance + playerRight * finalSpread;
                
                if (tableCenter != null)
                {
                    targetPos.y = tableCenter.position.y + heightOffset;
                }
                else
                {
                    targetPos.y = playerPos.y + heightOffset;
                }

                                                                                bool isLeftSide = playerPos.x < (tableCenter != null ? tableCenter.position.x : 0f);
                float yRot = isLeftSide ? 90f : -90f;
                if (isOpponent) yRot = isLeftSide ? -90f : 90f;

                Quaternion targetRot = Quaternion.Euler(0, yRot, 0) * Quaternion.Euler(tiltAngle, 0, 0);
                if (tableCenter != null)
                {
                    targetRot = tableCenter.rotation * targetRot;
                }

                cardsInHand[i].AnimateTo(targetPos, targetRot, 0.4f, false);
            }
        }

        
    }
}







