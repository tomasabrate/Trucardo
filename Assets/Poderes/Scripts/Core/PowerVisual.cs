using System.Collections.Generic;
using UnityEngine;
using Trucardo.Core.Interaction;

namespace Trucardo.Core
{
    /// <summary>
    /// Representación 3D interactuable de un poder sobre la mesa.
    /// Incorpora realce cromático natural y autoiluminación sutil de apoyo ambiental (Emission tenue)
    /// con acabado mate rústico, en total sintonía con la atmósfera gauchesca y las texturas de la pulpería.
    /// </summary>
    public class PowerVisual : MonoBehaviour, IInteractable
    {
        public PowerData Data { get; private set; }
        public bool isOpponentPower = false;

        private bool hasEnhancedMaterials = false;

        public string PromptText => Data != null ? Data.DisplayName : "Poder";

        public bool CanInteract(GameObject interactor) => Data != null && isActiveAndEnabled;

        private void Awake()
        {
            ApplyLuminousEnhancement();
        }

        private void Start()
        {
            ApplyLuminousEnhancement();
        }

        public void Interact(GameObject interactor)
        {
            if (isOpponentPower || interactor == null) return;
            PlayerController player = interactor.GetComponent<PlayerController>();
            if (player != null)
            {
                player.OnPowerClicked(this);
            }
        }

        public void SetPowerData(PowerData data)
        {
            Data = data;
            string prefix = isOpponentPower ? "OpponentPower" : "Power";
            gameObject.name = data != null ? $"{prefix}_{data.PowerId}" : $"{prefix}_Empty";

            if (GetComponentInChildren<Collider>() == null)
            {
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(0.14f, 0.12f, 0.14f);
                collider.center = new Vector3(0f, 0.06f, 0f);
            }

            hasEnhancedMaterials = false;
            ApplyLuminousEnhancement();
        }

        /// <summary>
        /// Ajusta los materiales del modelo 3D con colores criollos naturales y una emisión suave de apoyo,
        /// con acabado mate rústico (sin brillos plásticos) para armonizar con la madera y la pulpería.
        /// </summary>
        public void ApplyLuminousEnhancement()
        {
            if (hasEnhancedMaterials) return;

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0) return;

            string powerId = Data != null ? Data.PowerId : ResolvePowerIdFromName();

            foreach (Renderer rend in renderers)
            {
                if (rend == null) continue;
                Material[] materials = rend.materials;
                if (materials == null || materials.Length == 0) continue;

                for (int i = 0; i < materials.Length; i++)
                {
                    Material mat = materials[i];
                    if (mat == null) continue;

                    string matName = mat.name.ToLowerInvariant();
                    TuneMaterial(mat, powerId, matName);
                }

                rend.materials = materials;
            }

            hasEnhancedMaterials = true;
        }

        private string ResolvePowerIdFromName()
        {
            string n = gameObject.name.ToLowerInvariant();
            if (n.Contains("facon")) return "facon";
            if (n.Contains("mate")) return "mate";
            if (n.Contains("guampa")) return "guampas";
            if (n.Contains("lazo")) return "lazo";
            if (n.Contains("guitarra")) return "guitarra";
            if (n.Contains("pava")) return "pava_caliente";
            if (n.Contains("mano_de_dios") || n.Contains("manodedios")) return "mano_de_dios";
            if (n.Contains("boleadoras")) return "boleadoras";
            if (n.Contains("ceibo")) return "ceibo";
            if (n.Contains("luz_mala") || n.Contains("luzmala")) return "luz_mala";
            if (n.Contains("baston")) return "baston_presidencial";
            return "";
        }

        private void TuneMaterial(Material mat, string powerId, string matName)
        {
            Color baseCol;
            Color emissiveCol;
            float smoothness = 0.08f;

            if (GetThemeColors(powerId, matName, out baseCol, out emissiveCol, out smoothness))
            {
                ApplyToMaterial(mat, baseCol, emissiveCol, smoothness);
                return;
            }

            // Fallback procedimental rústico: mantiene el color equilibrado y agrega apoyo tenue
            Color current = mat.HasProperty("_BaseColor")
                ? mat.GetColor("_BaseColor")
                : (mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white);

            Color.RGBToHSV(current, out float h, out float s, out float v);
            v = Mathf.Clamp(v, 0.35f, 0.85f);
            s = Mathf.Clamp(s, 0.15f, 0.65f);
            Color naturalBase = Color.HSVToRGB(h, s, v);
            naturalBase.a = current.a;

            // Emisión de apoyo muy sutil (8%), sin sensación de foco ni brillo desmedido
            Color softEmissive = naturalBase * 0.08f;
            softEmissive.a = 1f;

            ApplyToMaterial(mat, naturalBase, softEmissive, 0.08f);
        }

        private void ApplyToMaterial(Material mat, Color baseColor, Color emissionColor, float smoothness)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);

            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);

            // Acabado mate rústico que sintoniza con las texturas del juego (baja rugosidad especular)
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

            mat.enableInstancing = true;
        }

        private bool GetThemeColors(string powerId, string matName, out Color baseCol, out Color emissiveCol, out float smoothness)
        {
            baseCol = Color.white;
            emissiveCol = Color.black;
            smoothness = 0.08f;

            switch (powerId)
            {
                case "facon":
                    if (matName.Contains("acero"))
                    {
                        baseCol = Hex("#B4BCC2"); // Acero al carbono forjado tradicional
                        emissiveCol = Hex("#181C20");
                        smoothness = 0.20f;
                        return true;
                    }
                    if (matName.Contains("metal"))
                    {
                        baseCol = Hex("#CCD2D6"); // Alpaca antigua
                        emissiveCol = Hex("#1E2226");
                        smoothness = 0.22f;
                        return true;
                    }
                    if (matName.Contains("madera"))
                    {
                        baseCol = Hex("#6B3C20"); // Cuero/madera curtida mate
                        emissiveCol = Hex("#120803");
                        smoothness = 0.08f;
                        return true;
                    }
                    break;

                case "mate":
                    if (matName.Contains("calabaza"))
                    {
                        baseCol = Hex("#A65E2E"); // Calabaza curada natural
                        emissiveCol = Hex("#180D05");
                        smoothness = 0.08f;
                        return true;
                    }
                    if (matName.Contains("metal"))
                    {
                        baseCol = Hex("#C4C9CC"); // Virola de alpaca sobria
                        emissiveCol = Hex("#1C1F22");
                        smoothness = 0.20f;
                        return true;
                    }
                    if (matName.Contains("yerba"))
                    {
                        baseCol = Hex("#637828"); // Yerba mate estacionada
                        emissiveCol = Hex("#0F1404");
                        smoothness = 0.06f;
                        return true;
                    }
                    if (matName.Contains("bombilla"))
                    {
                        baseCol = Hex("#CFCEBD"); // Bombilla tradicional
                        emissiveCol = Hex("#1E1D16");
                        smoothness = 0.22f;
                        return true;
                    }
                    break;

                case "guampas":
                    if (matName.Contains("cuernoclaro") || matName.Contains("claro"))
                    {
                        baseCol = Hex("#D6C7B0"); // Asta/hueso vacuno claro
                        emissiveCol = Hex("#1E1B16");
                        smoothness = 0.10f;
                        return true;
                    }
                    if (matName.Contains("cuernooscuro") || matName.Contains("oscuro"))
                    {
                        baseCol = Hex("#4C3A30"); // Veta oscura de cuerno
                        emissiveCol = Hex("#0E0A07");
                        smoothness = 0.08f;
                        return true;
                    }
                    if (matName.Contains("plata"))
                    {
                        baseCol = Hex("#C8CDD0");
                        emissiveCol = Hex("#1D2022");
                        smoothness = 0.20f;
                        return true;
                    }
                    if (matName.Contains("yerba"))
                    {
                        baseCol = Hex("#637828");
                        emissiveCol = Hex("#0F1404");
                        smoothness = 0.06f;
                        return true;
                    }
                    if (matName.Contains("bombilla"))
                    {
                        baseCol = Hex("#CFCEBD");
                        emissiveCol = Hex("#1E1D16");
                        smoothness = 0.22f;
                        return true;
                    }
                    break;

                case "lazo":
                    if (matName.Contains("cuero_claro"))
                    {
                        baseCol = Hex("#BA8650"); // Cuero crudo sobado
                        emissiveCol = Hex("#1A1106");
                        smoothness = 0.06f;
                        return true;
                    }
                    if (matName.Contains("cuero_medio"))
                    {
                        baseCol = Hex("#9E6C3A");
                        emissiveCol = Hex("#150C04");
                        smoothness = 0.06f;
                        return true;
                    }
                    if (matName.Contains("cuero_oscuro") || matName.Contains("tiento"))
                    {
                        baseCol = Hex("#7E5028");
                        emissiveCol = Hex("#100803");
                        smoothness = 0.06f;
                        return true;
                    }
                    if (matName.Contains("metal"))
                    {
                        baseCol = Hex("#98A0A6"); // Argolla de hierro forjado
                        emissiveCol = Hex("#15181C");
                        smoothness = 0.18f;
                        return true;
                    }
                    break;

                case "guitarra":
                    if (matName.Contains("cuerpo_madera"))
                    {
                        baseCol = Hex("#BA6E2E"); // Tapa y aros de cedro rústico
                        emissiveCol = Hex("#1A0D04");
                        smoothness = 0.10f;
                        return true;
                    }
                    if (matName.Contains("mastil") || matName.Contains("paleta"))
                    {
                        baseCol = Hex("#743E1B"); // Mástil de nogal criollo
                        emissiveCol = Hex("#100702");
                        smoothness = 0.08f;
                        return true;
                    }
                    if (matName.Contains("diapason") || matName.Contains("puente"))
                    {
                        baseCol = Hex("#362820"); // Madera oscura
                        emissiveCol = Hex("#080503");
                        smoothness = 0.06f;
                        return true;
                    }
                    if (matName.Contains("crema"))
                    {
                        baseCol = Hex("#D8D0BA"); // Cejilla de hueso
                        emissiveCol = Hex("#1C1A14");
                        smoothness = 0.08f;
                        return true;
                    }
                    if (matName.Contains("roseta"))
                    {
                        baseCol = Hex("#9C3826"); // Roseta tradicional apagada
                        emissiveCol = Hex("#180704");
                        smoothness = 0.08f;
                        return true;
                    }
                    break;

                case "pava_caliente":
                    if (matName.Contains("metal_claro"))
                    {
                        baseCol = Hex("#4E535A"); // Chapa de campo
                        emissiveCol = Hex("#120E0A");
                        smoothness = 0.14f;
                        return true;
                    }
                    if (matName.Contains("metal_medio"))
                    {
                        baseCol = Hex("#3E4248"); // Tizne de leña
                        emissiveCol = Hex("#100B08");
                        smoothness = 0.12f;
                        return true;
                    }
                    if (matName.Contains("metal_oscuro"))
                    {
                        baseCol = Hex("#2E3034"); // Base chamuscada
                        emissiveCol = Hex("#0C0806");
                        smoothness = 0.10f;
                        return true;
                    }
                    if (matName.Contains("madera"))
                    {
                        baseCol = Hex("#8C5024");
                        emissiveCol = Hex("#120803");
                        smoothness = 0.06f;
                        return true;
                    }
                    break;

                case "mano_de_dios":
                    if (matName.Contains("balon"))
                    {
                        baseCol = Hex("#C8A840"); // Dorado bronce envejecido
                        emissiveCol = Hex("#201804");
                        smoothness = 0.18f;
                        return true;
                    }
                    if (matName.Contains("nube_a"))
                    {
                        baseCol = Hex("#DFE3E6"); // Nube mate
                        emissiveCol = Hex("#181B1E");
                        smoothness = 0.06f;
                        return true;
                    }
                    if (matName.Contains("nube_b"))
                    {
                        baseCol = Hex("#C6D4DF");
                        emissiveCol = Hex("#141A20");
                        smoothness = 0.06f;
                        return true;
                    }
                    if (matName.Contains("nube_c"))
                    {
                        baseCol = Hex("#A8BFCF");
                        emissiveCol = Hex("#101720");
                        smoothness = 0.06f;
                        return true;
                    }
                    break;

                case "boleadoras":
                    if (matName.Contains("soga_clara"))
                    {
                        baseCol = Hex("#B68652"); // Tiento sobado
                        emissiveCol = Hex("#181006");
                        smoothness = 0.06f;
                        return true;
                    }
                    if (matName.Contains("soga_oscura"))
                    {
                        baseCol = Hex("#946234");
                        emissiveCol = Hex("#120B04");
                        smoothness = 0.06f;
                        return true;
                    }
                    if (matName.Contains("bola_madera") || matName.Contains("bola"))
                    {
                        baseCol = Hex("#929AA0"); // Piedra lisa de río
                        emissiveCol = Hex("#14171A");
                        smoothness = 0.08f;
                        return true;
                    }
                    if (matName.Contains("mango"))
                    {
                        baseCol = Hex("#7A441E");
                        emissiveCol = Hex("#100702");
                        smoothness = 0.06f;
                        return true;
                    }
                    if (matName.Contains("atadura"))
                    {
                        baseCol = Hex("#4C382A");
                        emissiveCol = Hex("#090604");
                        smoothness = 0.05f;
                        return true;
                    }
                    break;

                case "ceibo":
                    if (matName.Contains("petalo"))
                    {
                        baseCol = Hex("#BD2232"); // Flor nacional carmesí profunda (no fluorescente)
                        emissiveCol = Hex("#220406");
                        smoothness = 0.08f;
                        return true;
                    }
                    if (matName.Contains("caliz") || matName.Contains("hoja"))
                    {
                        baseCol = Hex("#4C6C2C"); // Verde hoja natural
                        emissiveCol = Hex("#091004");
                        smoothness = 0.06f;
                        return true;
                    }
                    if (matName.Contains("tallo"))
                    {
                        baseCol = Hex("#5A3822"); // Rama leñosa
                        emissiveCol = Hex("#0B0603");
                        smoothness = 0.05f;
                        return true;
                    }
                    if (matName.Contains("crema"))
                    {
                        baseCol = Hex("#D8D0B8");
                        emissiveCol = Hex("#1A1810");
                        smoothness = 0.06f;
                        return true;
                    }
                    break;

                case "luz_mala":
                    if (matName.Contains("nucleo") || matName.Contains("glow"))
                    {
                        baseCol = Hex("#78D8CC"); // Fuego fatuo tenue y misterioso
                        emissiveCol = Hex("#153C35");
                        smoothness = 0.18f;
                        return true;
                    }
                    if (matName.Contains("menta"))
                    {
                        baseCol = Hex("#50B8A6");
                        emissiveCol = Hex("#0E2A24");
                        smoothness = 0.14f;
                        return true;
                    }
                    if (matName.Contains("celeste") || matName.Contains("turquesa") || matName.Contains("llama"))
                    {
                        baseCol = Hex("#489EB2");
                        emissiveCol = Hex("#0C222C");
                        smoothness = 0.14f;
                        return true;
                    }
                    if (matName.Contains("base") || matName.Contains("azul"))
                    {
                        baseCol = Hex("#243A60");
                        emissiveCol = Hex("#060F20");
                        smoothness = 0.10f;
                        return true;
                    }
                    break;

                case "baston_presidencial":
                    if (matName.Contains("madera"))
                    {
                        baseCol = Hex("#702816"); // Urunday rústico noble
                        emissiveCol = Hex("#120502");
                        smoothness = 0.12f;
                        return true;
                    }
                    if (matName.Contains("plata"))
                    {
                        baseCol = Hex("#CBD0D4"); // Plata cincelada mate
                        emissiveCol = Hex("#1F2226");
                        smoothness = 0.22f;
                        return true;
                    }
                    if (matName.Contains("oro"))
                    {
                        baseCol = Hex("#C6A43C"); // Oro viejo
                        emissiveCol = Hex("#201804");
                        smoothness = 0.20f;
                        return true;
                    }
                    if (matName.Contains("celeste"))
                    {
                        baseCol = Hex("#6894BD"); // Cinta patria suave
                        emissiveCol = Hex("#101C28");
                        smoothness = 0.08f;
                        return true;
                    }
                    if (matName.Contains("blanco"))
                    {
                        baseCol = Hex("#D6DADF");
                        emissiveCol = Hex("#1C1F22");
                        smoothness = 0.08f;
                        return true;
                    }
                    if (matName.Contains("gris"))
                    {
                        baseCol = Hex("#3E4044");
                        emissiveCol = Hex("#0A0B0C");
                        smoothness = 0.08f;
                        return true;
                    }
                    break;
            }

            return false;
        }

        private static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color c)) return c;
            return Color.white;
        }

        public void AnimateTo(Vector3 targetPosition, Quaternion targetRotation, float duration = 0.4f)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateCoroutine(targetPosition, targetRotation, duration));
        }

        private System.Collections.IEnumerator AnimateCoroutine(
            Vector3 targetPosition,
            Quaternion targetRotation,
            float duration)
        {
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
                yield return null;
            }

            transform.SetPositionAndRotation(targetPosition, targetRotation);
        }
    }
}
