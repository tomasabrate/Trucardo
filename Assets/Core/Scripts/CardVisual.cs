using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using Unity.Netcode;

namespace Trucardo.Core
{
    public class CardVisual : NetworkBehaviour
    {
        [Header("UI Elements")]
        public Text textNumber;
        public Text textNumberBottom;
        public Image iconImage;

        [Header("Sprites")]
        public Sprite spriteEspada;
        public Sprite spriteOro;
        public Sprite spriteCopa;
        public Sprite spriteBasto;

        private Coroutine currentAnimation;

        public NetworkVariable<int> netCardIndex = new NetworkVariable<int>(-1);

        public override void OnNetworkSpawn()
        {
            if (netCardIndex.Value != -1)
                ApplyCardData(netCardIndex.Value);

            netCardIndex.OnValueChanged += (oldVal, newVal) => {
                if (newVal != -1) ApplyCardData(newVal);
            };
        }

        private void ApplyCardData(int index)
        {
            GameServer server = FindAnyObjectByType<GameServer>();
            if (server != null && server.masterDeck != null && index >= 0 && index < server.masterDeck.AvailableCards.Count)
            {
                SetCardData(server.masterDeck.AvailableCards[index]);
            }
        }

        public void SetCardData(CardData data)
        {
            if (data != null)
            {
                if (iconImage != null && data.CardSprite != null)
                {
                    iconImage.sprite = data.CardSprite;
                }
            }
        }

        // Método para jugar la carta hacia el centro de la mesa (con efecto de voltereta)
        public void PlayToTable(Vector3 targetPosition, Quaternion targetRotation, float duration = 0.5f)
        {
            AnimateTo(targetPosition, targetRotation, duration, true);
        }

        // Método general para animar posiciones (ej. robar carta hacia la mano)
        public void AnimateTo(Vector3 endPos, Quaternion endRot, float duration, bool isFlipping)
        {
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(AnimateCardCoroutine(endPos, endRot, duration, isFlipping));
        }

        private IEnumerator AnimateCardCoroutine(Vector3 endPos, Quaternion endRot, float duration, bool isFlipping)
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeT = Mathf.SmoothStep(0, 1, t); // EaseInOut suave
                
                // Altura de la parábola matemática usando función Seno
                // Si la está lanzando a la mesa salta más (0.4f), si roba a la mano salta menos (0.2f)
                float height = isFlipping ? Mathf.Sin(t * Mathf.PI) * 0.4f : Mathf.Sin(t * Mathf.PI) * 0.2f;
                
                transform.position = Vector3.Lerp(startPos, endPos, easeT) + (Vector3.up * height);
                
                if (isFlipping)
                {
                    // Dar 2 vueltas completas en el aire (Flip effect)
                    Quaternion spin = Quaternion.AngleAxis(t * 360f * 2, transform.right);
                    transform.rotation = Quaternion.Lerp(startRot, endRot, easeT) * spin;
                }
                else
                {
                    transform.rotation = Quaternion.Lerp(startRot, endRot, easeT);
                }
                yield return null;
            }
            
            // Forzar los valores finales exactos al terminar para evitar que queden rotadas o flotando
            transform.position = endPos;
            transform.rotation = endRot;
            currentAnimation = null;
        }
    }
}
