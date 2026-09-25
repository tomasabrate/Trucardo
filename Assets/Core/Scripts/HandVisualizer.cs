using System.Collections.Generic;
using UnityEngine;

namespace Trucardo.Core
{
    public class HandVisualizer : MonoBehaviour
    {
        [Header("Configuración Absoluta")]
        public float absoluteZ = -0.6f; // Z en la mesa donde se colocan
        public float sideSpacing = 0.35f;
        public float absoluteY = 1.05f; // Altura absoluta sobre la mesa
        public float tiltAngle = -35f; 
        
        [Header("Referencias")]
        public Transform tableCenter; // <--- AGREGADO: Centro de la mesa principal
        public List<CardVisual> cardsInHand = new List<CardVisual>();
        
        public void AddCardToHand(CardVisual newCard)
        {
            if (!cardsInHand.Contains(newCard))
            {
                cardsInHand.Add(newCard);
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
            Vector3 centerPos = tableCenter != null ? tableCenter.position : Vector3.zero;

            for (int i = 0; i < cardsInHand.Count; i++)
            {
                if (cardsInHand[i] == null) continue;
                
                float xOffset = (i - 1) * sideSpacing;
                if (cardsInHand.Count == 2) xOffset = (i == 0) ? -sideSpacing / 2f : sideSpacing / 2f;
                else if (cardsInHand.Count == 1) xOffset = 0f;

                if (absoluteZ > 0) xOffset = -xOffset;
                
                // Se le suma la posición del centro de la mesa
                Vector3 targetPos = new Vector3(centerPos.x + xOffset, centerPos.y + absoluteY, centerPos.z + absoluteZ);
                
                float yRot = (absoluteZ < 0) ? 0f : 180f; 
                Quaternion targetRot = Quaternion.Euler(tiltAngle, yRot, 0f);
                
                cardsInHand[i].AnimateTo(targetPos, targetRot, 0.4f, false);
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                Vector3 centerPos = tableCenter != null ? tableCenter.position : Vector3.zero;

                for (int i = 0; i < cardsInHand.Count; i++)
                {
                    if (cardsInHand[i] == null) continue;
                    
                    float xOffset = (i - 1) * sideSpacing;
                    if (cardsInHand.Count == 2) xOffset = (i == 0) ? -sideSpacing / 2f : sideSpacing / 2f;
                    else if (cardsInHand.Count == 1) xOffset = 0f;
                    if (absoluteZ > 0) xOffset = -xOffset;
                    
                    Vector3 targetPos = new Vector3(centerPos.x + xOffset, centerPos.y + absoluteY, centerPos.z + absoluteZ);
                    float yRot = (absoluteZ < 0) ? 0f : 180f; 
                    Quaternion targetRot = Quaternion.Euler(tiltAngle, yRot, 0f);
                    
                    cardsInHand[i].transform.position = targetPos;
                    cardsInHand[i].transform.rotation = targetRot;
                }
            }
        }
    }
}
