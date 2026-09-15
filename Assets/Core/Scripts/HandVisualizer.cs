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
                ArrangeHand(); // Re-acomodar el abanico con las cartas restantes
            }
        }
        
        // Calcula y posiciona las cartas usando coordenadas absolutas de la mesa
        private void ArrangeHand()
        {
            for (int i = 0; i < cardsInHand.Count; i++)
            {
                if (cardsInHand[i] == null) continue;
                
                // Separación horizontal (ej. Izquierda, Centro, Derecha)
                float xOffset = (i - 1) * sideSpacing;
                
                // Si solo quedan 2 cartas, centramos el offset
                if (cardsInHand.Count == 2) xOffset = (i == 0) ? -sideSpacing / 2f : sideSpacing / 2f;
                else if (cardsInHand.Count == 1) xOffset = 0f;

                // Si estamos en el lado positivo de Z (+0.6), invertimos el X para que se vea de izq a der desde ese lado
                if (absoluteZ > 0) xOffset = -xOffset;
                
                Vector3 targetPos = new Vector3(xOffset, absoluteY, absoluteZ);
                
                // Rotar para que la carta mire hacia el centro de la mesa
                float yRot = (absoluteZ < 0) ? 0f : 180f; 
                Quaternion targetRot = Quaternion.Euler(tiltAngle, yRot, 0f);
                
                // Animar el re-acomodo en la mano
                cardsInHand[i].AnimateTo(targetPos, targetRot, 0.4f, false);
            }
        }

        // Se ejecuta automáticamente en Unity cuando cambias un valor en el Inspector
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                for (int i = 0; i < cardsInHand.Count; i++)
                {
                    if (cardsInHand[i] == null) continue;
                    
                    float xOffset = (i - 1) * sideSpacing;
                    if (cardsInHand.Count == 2) xOffset = (i == 0) ? -sideSpacing / 2f : sideSpacing / 2f;
                    else if (cardsInHand.Count == 1) xOffset = 0f;
                    if (absoluteZ > 0) xOffset = -xOffset;
                    
                    Vector3 targetPos = new Vector3(xOffset, absoluteY, absoluteZ);
                    float yRot = (absoluteZ < 0) ? 0f : 180f; 
                    Quaternion targetRot = Quaternion.Euler(tiltAngle, yRot, 0f);
                    
                    // Cuando editas en el inspector forzamos el cambio instantáneo para que lo veas fluido
                    cardsInHand[i].transform.position = targetPos;
                    cardsInHand[i].transform.rotation = targetRot;
                }
            }
        }
    }
}
