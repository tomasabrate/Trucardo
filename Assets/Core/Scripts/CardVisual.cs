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
        public Sprite backSprite;
        public Sprite spriteEspada;
        public Sprite spriteOro;
        public Sprite spriteCopa;
        public Sprite spriteBasto;

        private Coroutine currentAnimation;
        public NetworkVariable<int> netCardIndex = new NetworkVariable<int>(-1);

        private GameObject luzMalaOverlayObj;
        private Image luzMalaOverlayImage;
        private Light luzMalaPointLight;
        private Coroutine luzMalaPulseCoroutine;
        private static Sprite s_LuzMalaGlowSprite;

        private void Awake()
        {
            EnsureVisualReferences();
        }

        private void OnDisable()
        {
            TeardownLuzMalaBlindingEffect();
        }

        public override void OnNetworkSpawn()
        {
            EnsureVisualReferences();

            if (netCardIndex.Value != -1)
                ApplyCardData(netCardIndex.Value);

            netCardIndex.OnValueChanged += (oldVal, newVal) => {
                if (newVal != -1) ApplyCardData(newVal);
            };
        }

        private void EnsureVisualReferences()
        {
            if (iconImage == null)
            {
                iconImage = GetOrCreateImage("CardCanvas", "Icono");
            }

            Image backImage = GetOrCreateImage("BackCanvas", "Icono");
            if (backImage != null && backImage.sprite == null && backSprite != null)
            {
                backImage.sprite = backSprite;
            }
        }

        private Image GetOrCreateImage(string canvasName, string imageName)
        {
            Transform canvasTransform = transform.Find(canvasName);
            if (canvasTransform == null)
            {
                return null;
            }

            Transform imageTransform = canvasTransform.Find(imageName);
            Image image = imageTransform != null ? imageTransform.GetComponent<Image>() : null;
            if (image != null)
            {
                ConfigureImageRect(image);
                return image;
            }

            GameObject imageObject = new GameObject(imageName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(canvasTransform, false);

            image = imageObject.GetComponent<Image>();
            ConfigureImageRect(image);
            return image;
        }

        private void ConfigureImageRect(Image image)
        {
            RectTransform rectTransform = image.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private void ApplyCardData(int index)
        {
            GameServer server = FindAnyObjectByType<GameServer>();
            if (server != null && server.masterDeck != null && index >= 0 && index < server.masterDeck.AvailableCards.Count)
            {
                SetCardData(server.masterDeck.AvailableCards[index]);
            }
        }

        private CardData currentCardData;
        private bool isBlanked = false;

        public void SetCardData(CardData data)
        {
            currentCardData = data;
            if (data != null)
            {
                if (iconImage != null && data.CardSprite != null)
                {
                    if (isBlanked)
                    {
                        iconImage.sprite = null;
                        iconImage.color = new Color(0.98f, 0.99f, 0.98f, 1f);
                    }
                    else
                    {
                        iconImage.sprite = data.CardSprite;
                        iconImage.color = Color.white;
                    }
                }
            }
        }

        public void SetBlanked(bool blanked)
        {
            isBlanked = blanked;
            EnsureVisualReferences();

            if (textNumber != null) textNumber.enabled = !blanked;
            if (textNumberBottom != null) textNumberBottom.enabled = !blanked;

            if (iconImage != null)
            {
                if (blanked)
                {
                    iconImage.sprite = null;
                    iconImage.color = new Color(0.98f, 0.99f, 0.98f, 1f);
                }
                else
                {
                    iconImage.color = Color.white;
                    if (currentCardData != null && currentCardData.CardSprite != null)
                    {
                        iconImage.sprite = currentCardData.CardSprite;
                    }
                }
            }

            if (blanked)
            {
                SetupLuzMalaBlindingEffect();
            }
            else
            {
                TeardownLuzMalaBlindingEffect();
            }
        }

        private void SetupLuzMalaBlindingEffect()
        {
            Transform cardCanvas = transform.Find("CardCanvas");
            if (cardCanvas != null)
            {
                if (luzMalaOverlayObj == null)
                {
                    Transform existing = cardCanvas.Find("LuzMalaGlowOverlay");
                    if (existing != null) luzMalaOverlayObj = existing.gameObject;
                }

                if (luzMalaOverlayObj == null)
                {
                    luzMalaOverlayObj = new GameObject("LuzMalaGlowOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    luzMalaOverlayObj.transform.SetParent(cardCanvas, false);
                    luzMalaOverlayObj.transform.SetAsLastSibling();

                    RectTransform rt = luzMalaOverlayObj.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = new Vector2(-12f, -12f);
                    rt.offsetMax = new Vector2(12f, 12f);

                    luzMalaOverlayImage = luzMalaOverlayObj.GetComponent<Image>();
                    luzMalaOverlayImage.sprite = GetOrCreateGlowSprite();
                    luzMalaOverlayImage.type = Image.Type.Simple;
                    luzMalaOverlayImage.raycastTarget = false;
                }
                else
                {
                    luzMalaOverlayImage = luzMalaOverlayObj.GetComponent<Image>();
                }

                if (luzMalaOverlayObj != null) luzMalaOverlayObj.SetActive(true);
            }

            if (luzMalaPointLight == null)
            {
                Transform existingLight = transform.Find("LuzMalaPointLight");
                if (existingLight != null)
                {
                    luzMalaPointLight = existingLight.GetComponent<Light>();
                }
                else
                {
                    GameObject lightObj = new GameObject("LuzMalaPointLight", typeof(Light));
                    lightObj.transform.SetParent(transform, false);
                    lightObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
                    luzMalaPointLight = lightObj.GetComponent<Light>();
                    luzMalaPointLight.type = LightType.Point;
                    luzMalaPointLight.color = new Color(0.72f, 1f, 0.90f);
                    luzMalaPointLight.range = 0.65f;
                    luzMalaPointLight.intensity = 2.4f;
                    luzMalaPointLight.shadows = LightShadows.None;
                }
            }

            if (luzMalaPointLight != null)
            {
                luzMalaPointLight.shadows = LightShadows.None;
                luzMalaPointLight.gameObject.SetActive(true);
            }

            if (luzMalaPulseCoroutine != null) StopCoroutine(luzMalaPulseCoroutine);
            luzMalaPulseCoroutine = StartCoroutine(LuzMalaPulseCoroutine());
        }

        private void TeardownLuzMalaBlindingEffect()
        {
            if (luzMalaPulseCoroutine != null)
            {
                StopCoroutine(luzMalaPulseCoroutine);
                luzMalaPulseCoroutine = null;
            }

            if (luzMalaOverlayObj != null)
            {
                luzMalaOverlayObj.SetActive(false);
            }

            if (luzMalaPointLight != null)
            {
                luzMalaPointLight.gameObject.SetActive(false);
            }
        }

        private IEnumerator LuzMalaPulseCoroutine()
        {
            RectTransform overlayRt = luzMalaOverlayObj != null ? luzMalaOverlayObj.GetComponent<RectTransform>() : null;

            while (isBlanked)
            {
                float time = Time.time;
                float pulse = Mathf.Sin(time * 5f);
                float flicker = Mathf.PerlinNoise(time * 12f, 0f) * 0.15f;

                if (luzMalaOverlayImage != null)
                {
                    float alpha = Mathf.Clamp(0.85f + pulse * 0.12f + flicker, 0.70f, 1f);
                    luzMalaOverlayImage.color = new Color(0.92f, 1f, 0.95f, alpha);
                }

                if (overlayRt != null)
                {
                    float s = 1f + (pulse * 0.04f);
                    overlayRt.localScale = new Vector3(s, s, 1f);
                }

                if (luzMalaPointLight != null)
                {
                    luzMalaPointLight.intensity = 2.2f + pulse * 0.5f + flicker * 2f;
                }

                yield return null;
            }
        }

        private static Sprite GetOrCreateGlowSprite()
        {
            if (s_LuzMalaGlowSprite != null) return s_LuzMalaGlowSprite;
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float maxDist = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    float t = Mathf.Clamp01(1f - dist);
                    // Suave curva radial hacia los bordes
                    float alpha = t * t * (3f - 2f * t);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            s_LuzMalaGlowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return s_LuzMalaGlowSprite;
        }

        // Método para jugar la carta hacia el centro de la mesa (con efecto de voltereta)
        public void PlayToTable(Vector3 targetPosition, Quaternion targetRotation, float duration = 0.5f)
        {
            if (isBlanked)
            {
                SetBlanked(false);
            }
            AnimateTo(targetPosition, targetRotation, duration, true);
        }

        // Método general para animar posiciones (ej. robar carta hacia la mano)
        public void AnimateTo(Vector3 endPos, Quaternion endRot, float duration, bool isFlipping, float arcHeight = 0.2f)
        {
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(AnimateCardCoroutine(endPos, endRot, duration, isFlipping, arcHeight));
        }

        private IEnumerator AnimateCardCoroutine(Vector3 endPos, Quaternion endRot, float duration, bool isFlipping, float arcHeight)
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
                // Si la está lanzando a la mesa salta más (0.4f), si roba a la mano salta menos (arcHeight)
                float height = isFlipping ? Mathf.Sin(t * Mathf.PI) * 0.4f : Mathf.Sin(t * Mathf.PI) * arcHeight;
                
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
