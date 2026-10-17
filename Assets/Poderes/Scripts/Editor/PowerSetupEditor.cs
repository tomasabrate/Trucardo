using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Trucardo.Core;

namespace Trucardo.EditorScripts
{
    public static class PowerSetupEditor
    {
        [MenuItem("Trucardo/Configurar UI de Poderes Activos en Escena")]
        public static void SetupRoundPowersUIInScene()
        {
            var hud = Object.FindAnyObjectByType<Trucardo.Core.UI.HUDController>();
            if (hud == null)
            {
                Debug.LogWarning("[RoundPowersUI] No se encontró HUDController en la escena activa.");
                return;
            }

            var roundPowers = hud.GetComponentInChildren<Trucardo.Core.UI.RoundPowersUI>(true);
            if (roundPowers == null)
            {
                var hudCanvas = hud.GetComponentInChildren<Canvas>(true);
                GameObject powersObj = new GameObject("RoundPowersUI", typeof(RectTransform));
                powersObj.transform.SetParent(hudCanvas != null ? hudCanvas.transform : hud.transform, false);
                roundPowers = powersObj.AddComponent<Trucardo.Core.UI.RoundPowersUI>();
            }

            Button trucoBtn = hud.transform.Find("ActionBar/ButtonTruco")?.GetComponent<Button>();
            if (trucoBtn == null) trucoBtn = hud.GetComponentInChildren<Button>(true);

            var canvas = hud.GetComponentInChildren<Canvas>(true);
            roundPowers.EnsureUI(trucoBtn, canvas);
            EditorUtility.SetDirty(roundPowers);
            EditorUtility.SetDirty(hud);
            Debug.Log("<color=#4CAF50><b>[RoundPowersUI]</b></color> UI de Poderes Activos configurada exitosamente en la escena.");
        }

        [MenuItem("Trucardo/Configurar Sistema de Poderes (Completo)")]
        public static void SetupPowersSystem()
        {
            EnsureFolder("Assets/Poderes");
            EnsureFolder("Assets/Poderes/Data");
            EnsureFolder("Assets/Poderes/Prefabs");
            EnsureFolder("Assets/Poderes/Materiales");
            EnsureFolder("Assets/Poderes/Modelos");
            EnsureFolder("Assets/Poderes/Texturas");
            EnsureFolder("Assets/Resources");

            // ================= 1. MATERIALES =================
            Material faconMat = GetOrCreateMaterial("Assets/Poderes/Materiales/Mat_Poder_Facon.mat", new Color(0.75f, 0.78f, 0.82f));
            Material mateMat = GetOrCreateMaterial("Assets/Poderes/Materiales/Mat_Poder_Mate.mat", new Color(0.22f, 0.35f, 0.15f));
            Material guampasMat = GetOrCreateMaterial("Assets/Poderes/Materiales/Mat_Poder_Guampas.mat", new Color(0.92f, 0.88f, 0.75f));
            Material lazoMat = GetOrCreateMaterial("Assets/Poderes/Materiales/Mat_Poder_Lazo.mat", new Color(0.55f, 0.32f, 0.14f));
            Material guitarraMat = GetOrCreateMaterial("Assets/Poderes/Materiales/Mat_Poder_Guitarra.mat", new Color(0.65f, 0.40f, 0.20f));
            Material pavaMat = GetOrCreateMaterial("Assets/Poderes/Materiales/Mat_Poder_PavaCaliente.mat", new Color(0.20f, 0.20f, 0.22f));
            Material manoMat = GetOrCreateMaterial("Assets/Poderes/Materiales/Mat_Poder_ManoDeDios.mat", new Color(1.0f, 0.84f, 0.0f));
            Material boleadorasMat = GetOrCreateMaterial("Assets/Poderes/Materiales/Mat_Poder_Boleadoras.mat", new Color(0.35f, 0.25f, 0.20f));
            Material ceiboMat = GetOrCreateMaterial("Assets/Poderes/Materiales/Mat_Poder_Ceibo.mat", new Color(0.85f, 0.10f, 0.15f));
            Material luzMalaMat = GetOrCreateMaterial("Assets/Poderes/Materiales/Mat_Poder_LuzMala.mat", new Color(0.20f, 1.0f, 0.40f));
            Material bastonMat = GetOrCreateMaterial("Assets/Poderes/Materiales/Mat_Poder_BastonPresidencial.mat", new Color(0.30f, 0.18f, 0.10f));

            // ================= 2. PREFABS 3D =================
            GameObject faconPrefab = GetOrCreatePrefab("Assets/Poderes/Prefabs/Prefab_Poder_Facon.prefab", PrimitiveType.Cube, faconMat, new Vector3(0.025f, 0.015f, 0.14f));
            GameObject matePrefab = GetOrCreatePrefab("Assets/Poderes/Prefabs/Prefab_Poder_Mate.prefab", PrimitiveType.Cylinder, mateMat, new Vector3(0.08f, 0.045f, 0.08f));
            GameObject guampasPrefab = GetOrCreatePrefab("Assets/Poderes/Prefabs/Prefab_Poder_Guampas.prefab", PrimitiveType.Capsule, guampasMat, new Vector3(0.045f, 0.07f, 0.045f));
            GameObject lazoPrefab = GetOrCreatePrefab("Assets/Poderes/Prefabs/Prefab_Poder_Lazo.prefab", PrimitiveType.Cylinder, lazoMat, new Vector3(0.09f, 0.015f, 0.09f));
            GameObject guitarraPrefab = GetOrCreatePrefab("Assets/Poderes/Prefabs/Prefab_Poder_Guitarra.prefab", PrimitiveType.Cube, guitarraMat, new Vector3(0.065f, 0.02f, 0.12f));
            GameObject pavaPrefab = GetOrCreatePrefab("Assets/Poderes/Prefabs/Prefab_Poder_PavaCaliente.prefab", PrimitiveType.Cylinder, pavaMat, new Vector3(0.07f, 0.055f, 0.07f));
            GameObject manoPrefab = GetOrCreatePrefab("Assets/Poderes/Prefabs/Prefab_Poder_ManoDeDios.prefab", PrimitiveType.Sphere, manoMat, new Vector3(0.08f, 0.08f, 0.08f));
            GameObject boleadorasPrefab = GetOrCreatePrefab("Assets/Poderes/Prefabs/Prefab_Poder_Boleadoras.prefab", PrimitiveType.Sphere, boleadorasMat, new Vector3(0.065f, 0.065f, 0.065f));
            GameObject ceiboPrefab = GetOrCreatePrefab("Assets/Poderes/Prefabs/Prefab_Poder_Ceibo.prefab", PrimitiveType.Cylinder, ceiboMat, new Vector3(0.06f, 0.04f, 0.06f));
            GameObject luzMalaPrefab = GetOrCreatePrefab("Assets/Poderes/Prefabs/Prefab_Poder_LuzMala.prefab", PrimitiveType.Sphere, luzMalaMat, new Vector3(0.075f, 0.075f, 0.075f));
            GameObject bastonPrefab = GetOrCreatePrefab("Assets/Poderes/Prefabs/Prefab_Poder_BastonPresidencial.prefab", PrimitiveType.Cylinder, bastonMat, new Vector3(0.02f, 0.08f, 0.02f));

            // ================= 3. EFECTOS SCRIPTABLEOBJECT =================
            FaconEffect faconEffect = GetOrCreateEffect<FaconEffect>("Assets/Poderes/Data/FaconEffect.asset");
            MateEffect mateEffect = GetOrCreateEffect<MateEffect>("Assets/Poderes/Data/MateEffect.asset");
            GuampasEffect guampasEffect = GetOrCreateEffect<GuampasEffect>("Assets/Poderes/Data/GuampasEffect.asset");
            LazoEffect lazoEffect = GetOrCreateEffect<LazoEffect>("Assets/Poderes/Data/LazoEffect.asset");
            GuitarraEffect guitarraEffect = GetOrCreateEffect<GuitarraEffect>("Assets/Poderes/Data/GuitarraEffect.asset");
            PavaCalienteEffect pavaEffect = GetOrCreateEffect<PavaCalienteEffect>("Assets/Poderes/Data/PavaCalienteEffect.asset");
            ManoDeDiosEffect manoEffect = GetOrCreateEffect<ManoDeDiosEffect>("Assets/Poderes/Data/ManoDeDiosEffect.asset");
            BoleadorasEffect boleadorasEffect = GetOrCreateEffect<BoleadorasEffect>("Assets/Poderes/Data/BoleadorasEffect.asset");
            CeiboEffect ceiboEffect = GetOrCreateEffect<CeiboEffect>("Assets/Poderes/Data/CeiboEffect.asset");
            LuzMalaEffect luzMalaEffect = GetOrCreateEffect<LuzMalaEffect>("Assets/Poderes/Data/LuzMalaEffect.asset");
            BastonPresidencialEffect bastonEffect = GetOrCreateEffect<BastonPresidencialEffect>("Assets/Poderes/Data/BastonPresidencialEffect.asset");

            // ================= 4. POWER DATA =================
            // 1. Facón (Común)
            PowerData faconPower = GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_Facon.asset",
                "facon",
                "Facón",
                "Rompe una carta a elección del rival y robas una carta nueva del mazo.",
                "Momento de activación: Durante el turno del jugador antes de jugar su carta de la baza.\nMecánica: Destruye una carta de la mano del rival y roba inmediatamente una carta nueva del mazo para sumarla a tu mano.",
                "Cuchillo criollo gaucho / Facón tradicional con mango de alpaca/asta y vaina de cuero.",
                "Provisorio",
                PowerRarity.Comun,
                faconPrefab,
                faconEffect);

            // 2. Mate (Común)
            PowerData matePower = GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_Mate.asset",
                "mate",
                "Mate",
                "Revela una carta de la mano del rival durante 5 segundos.",
                "Momento de activación: En cualquier momento durante el turno del jugador mientras el oponente conserve cartas en mano.\nMecánica: Revela una carta oculta de la mano del rival durante 5 segundos hacia el jugador local.",
                "Mate de calabaza curada con virola labrada y bombilla de alpaca.",
                "Provisorio implementado",
                PowerRarity.Comun,
                matePrefab,
                mateEffect);

            // 3. Guampas (Común)
            PowerData guampasPower = GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_Guampas.asset",
                "guampas",
                "Guampas",
                "Roba una carta del mazo; si es mejor que una de tu mano, la reemplaza.",
                "Momento de activación: En el turno del jugador antes de tirar una carta a la mesa.\nMecánica: Toma una carta del mazo y la evalúa contra tu mano, reemplazando la carta más débil si resulta ventajoso.",
                "Par de cuernos de buey / guampas criollas pulidas.",
                "Provisorio",
                PowerRarity.Comun,
                guampasPrefab,
                guampasEffect);

            // 4. Lazo (Común)
            PowerData lazoPower = GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_Lazo.asset",
                "lazo",
                "Lazo",
                "Solo en primera mano: descarta todas tus cartas y roba 3 cartas nuevas.",
                "Momento de activación: Únicamente durante la primera mano (baza 0), antes de haber jugado ninguna carta.\nMecánica: Descarta las 3 cartas iniciales completas al mazo y recibe 3 cartas nuevas extraídas del mazo.",
                "Lazo gaucho trenzado de cuero crudo en rollo con argolla.",
                "Provisorio",
                PowerRarity.Comun,
                lazoPrefab,
                lazoEffect);

            // 5. Guitarra (Común)
            PowerData guitarraPower = GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_Guitarra.asset",
                "guitarra",
                "Guitarra",
                "Intercambia una de tus cartas por una del rival a ciegas.",
                "Momento de activación: Durante el turno del jugador antes de tirar su carta.\nMecánica: Intercambia una carta al azar de tu mano con una de las cartas restantes del oponente sin conocerla previamente.",
                "Guitarra criolla clásica de madera con roseta y cuerdas de nylon.",
                "Provisorio",
                PowerRarity.Comun,
                guitarraPrefab,
                guitarraEffect);

            // 6. Pava Caliente (Común)
            PowerData pavaPower = GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_PavaCaliente.asset",
                "pava_caliente",
                "Pava Caliente",
                "El rival tiene solo 5 segundos por turno durante toda la mano, o jugará al azar.",
                "Momento de activación: En cualquier momento durante la ronda.\nMecánica: Aplica un temporizador de presión extrema: el rival dispone de únicamente 5 segundos por cada decisión de juego.",
                "Pava de campo tiznada de chapa enlozada negra para fuego a leña.",
                "Provisorio",
                PowerRarity.Comun,
                pavaPrefab,
                pavaEffect);

            // 7. Mano de Dios (Común)
            PowerData manoPower = GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_ManoDeDios.asset",
                "mano_de_dios",
                "Mano de Dios",
                "Cambia quién es 'Mano' en la ronda actual a tu favor.",
                "Momento de activación: Durante la ronda, preferentemente antes del desenlace de empates.\nMecánica: Invierte la condición de quién es Mano de la partida, otorgando victoria automática en empates de Envido y ventajas de Truco.",
                "Escultura dorada de una mano abierta apuntando hacia el cielo.",
                "Provisorio implementado",
                PowerRarity.Comun,
                manoPrefab,
                manoEffect);

            // 8. Boleadoras (Común)
            PowerData boleadorasPower = GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_Boleadoras.asset",
                "boleadoras",
                "Boleadoras",
                "Traba al rival: le impide recibir su próximo poder en la siguiente ronda.",
                "Momento de activación: Durante cualquier momento de la ronda en curso.\nMecánica: Bloquea al rival colocándole el estado Boleado, impidiéndole recibir poderes en la mesa en la próxima ronda.",
                "Boleadoras gauchas de tres ramales trenzados con bolas de piedra.",
                "Provisorio",
                PowerRarity.Comun,
                boleadorasPrefab,
                boleadorasEffect);

            // 9. Ceibo (Legendario)
            PowerData ceiboPower = GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_Ceibo.asset",
                "ceibo",
                "Ceibo",
                "Cambia el palo de todas tus cartas al mismo palo aleatorio (alto Envido asegurado).",
                "Momento de activación: En cualquier momento antes de jugar todas las cartas (ideal antes de cantar Envido).\nMecánica: Transforma el palo de todas tus cartas al mismo palo manteniendo sus números, garantizando un Envido legendario.",
                "Ramillete florecido del Ceibo (Flor Nacional Argentina) con pétalos carmesí.",
                "Provisorio",
                PowerRarity.Legendario,
                ceiboPrefab,
                ceiboEffect);

            // 10. Luz Mala (Legendario)
            PowerData luzMalaPower = GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_LuzMala.asset",
                "luz_mala",
                "Luz Mala",
                "Vuelve blancas y mezcla las cartas del rival: no sabrá cuál es cuál al jugar.",
                "Momento de activación: Durante el turno del jugador antes de que el rival juegue sus cartas clave.\nMecánica: Envuelve las cartas del oponente en un halo espectral blanco, forzándolo a jugar a ciegas.",
                "Fuego fatuo espectral / candil antiguo con resplandor verdoso fantasmal.",
                "Provisorio",
                PowerRarity.Legendario,
                luzMalaPrefab,
                luzMalaEffect);

            // 11. Bastón Presidencial (Legendario)
            PowerData bastonPower = GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_BastonPresidencial.asset",
                "baston_presidencial",
                "Bastón Presidencial",
                "Obliga al rival a aceptar (decir '¡Quiero!') a tu próximo canto.",
                "Momento de activación: Justo antes de efectuar un canto (Envido o Truco).\nMecánica: Impone autoridad absoluta sobre la voluntad del oponente: bloquea la opción 'No Quiero' y fuerza la respuesta '¡Quiero!'.",
                "Bastón de mando presidencial de madera de urunday con empuñadura y regatón de plata cincelada.",
                "Provisorio",
                PowerRarity.Legendario,
                bastonPrefab,
                bastonEffect);

            // ================= 5. POWER CATALOG =================
            const string catalogPath = "Assets/Poderes/Data/PowerCatalog.asset";
            const string resourcesCatalogPath = "Assets/Resources/PowerCatalog.asset";

            PowerCatalog catalog = AssetDatabase.LoadAssetAtPath<PowerCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PowerCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            catalog.AvailablePowers.Clear();
            catalog.AvailablePowers.Add(faconPower);
            catalog.AvailablePowers.Add(matePower);
            catalog.AvailablePowers.Add(guampasPower);
            catalog.AvailablePowers.Add(lazoPower);
            catalog.AvailablePowers.Add(guitarraPower);
            catalog.AvailablePowers.Add(pavaPower);
            catalog.AvailablePowers.Add(manoPower);
            catalog.AvailablePowers.Add(boleadorasPower);
            catalog.AvailablePowers.Add(ceiboPower);
            catalog.AvailablePowers.Add(luzMalaPower);
            catalog.AvailablePowers.Add(bastonPower);
            EditorUtility.SetDirty(catalog);

            // Guardar copia en Resources para carga automática en runtime
            AssetDatabase.CopyAsset(catalogPath, resourcesCatalogPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=#4CAF50><b>[Poderes]</b></color> Catálogo completo configurado exitosamente: ¡11 Poderes listos para jugar!");
        }

        private static Material GetOrCreateMaterial(string path, Color color)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader) { color = color };
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.color = color;
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static GameObject GetOrCreatePrefab(string path, PrimitiveType shape, Material material, Vector3 scale)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject tempObj = GameObject.CreatePrimitive(shape);
            tempObj.transform.localScale = scale;
            if (material != null)
            {
                var renderer = tempObj.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = material;
            }

            // Asegurar PowerVisual
            if (tempObj.GetComponent<PowerVisual>() == null)
            {
                tempObj.AddComponent<PowerVisual>();
            }

            // Asegurar Collider
            if (tempObj.GetComponent<Collider>() == null)
            {
                tempObj.AddComponent<BoxCollider>();
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempObj, path);
            Object.DestroyImmediate(tempObj);
            return prefab;
        }

        private static T GetOrCreateEffect<T>(string path) where T : PowerEffect
        {
            T effect = AssetDatabase.LoadAssetAtPath<T>(path);
            if (effect == null)
            {
                effect = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(effect, path);
            }
            return effect;
        }

        private static PowerData GetOrCreatePowerData(
            string path,
            string id,
            string displayName,
            string shortDescription,
            string detailedDescription,
            string objectConcept,
            string modelStatus,
            PowerRarity rarity,
            GameObject prefab,
            PowerEffect effect)
        {
            PowerData power = AssetDatabase.LoadAssetAtPath<PowerData>(path);
            if (power == null)
            {
                power = ScriptableObject.CreateInstance<PowerData>();
                AssetDatabase.CreateAsset(power, path);
            }

            power.PowerId = id;
            power.DisplayName = displayName;
            power.Description = shortDescription;
            power.ShortDescription = shortDescription;
            power.DetailedDescription = detailedDescription;
            power.ObjectConcept = objectConcept;
            power.ModelStatus = modelStatus;
            power.Rarity = rarity;
            power.WorldPrefab = prefab;
            power.Effect = effect;

            EditorUtility.SetDirty(power);
            return power;
        }

        private static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
