using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Trucardo.Core;

namespace Trucardo.EditorScripts
{
    [InitializeOnLoad]
    public static class PowerSetupEditor
    {
        static PowerSetupEditor()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            if (!SessionState.GetBool("Trucardo_FBX_Setup_Complete_v4", false))
            {
                SessionState.SetBool("Trucardo_FBX_Setup_Complete_v4", true);
                SetupPowersSystem();
            }
        }

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
        [MenuItem("Trucardo/Actualizar Modelos FBX de Poderes")]
        public static void SetupPowersSystem()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            EnsureFolder("Assets/Poderes");
            EnsureFolder("Assets/Poderes/Data");
            EnsureFolder("Assets/Poderes/Prefabs");
            EnsureFolder("Assets/Poderes/Materiales");
            EnsureFolder("Assets/Poderes/Modelos");
            EnsureFolder("Assets/Poderes/Modelos/Modelo FBX");
            EnsureFolder("Assets/Poderes/Texturas");
            EnsureFolder("Assets/Resources");

            // ================= 1. MODELOS FBX Y PREFABS 3D =================
            // Calibración de escalas para mesa de Truco (1.4m x 1.4m) con props de entre 11cm y 20cm:

            // 1. Facón: Cuchillo criollo gaucho (40cm real -> ~16cm en mesa)
            GameObject faconPrefab = CreateOrUpdatePowerPrefab(
                "Assets/Poderes/Prefabs/Prefab_Poder_Facon.prefab",
                "Assets/Poderes/Modelos/Modelo FBX/Facon.fbx",
                new Vector3(0.47f, 0.47f, 0.47f),
                new Vector3(0f, -30f, 0f),
                Vector3.zero,
                PrimitiveType.Cube,
                new Vector3(0.025f, 0.015f, 0.14f));

            // 2. Mate: Mate de calabaza con virola y bombilla (13.9cm real -> ~11.8cm en mesa)
            GameObject matePrefab = CreateOrUpdatePowerPrefab(
                "Assets/Poderes/Prefabs/Prefab_Poder_Mate.prefab",
                "Assets/Poderes/Modelos/Modelo FBX/Mate.fbx",
                new Vector3(1.00f, 1.00f, 1.00f),
                Vector3.zero,
                Vector3.zero,
                PrimitiveType.Cylinder,
                new Vector3(0.08f, 0.045f, 0.08f));

            // 3. Guampas: Cuerno criollo de buey (17.9cm real -> ~13.4cm en mesa)
            GameObject guampasPrefab = CreateOrUpdatePowerPrefab(
                "Assets/Poderes/Prefabs/Prefab_Poder_Guampas.prefab",
                "Assets/Poderes/Modelos/Modelo FBX/Guamba.fbx",
                new Vector3(0.88f, 0.88f, 0.88f),
                Vector3.zero,
                Vector3.zero,
                PrimitiveType.Capsule,
                new Vector3(0.045f, 0.07f, 0.045f));

            // 4. Lazo: Rollo trenzado de cuero con argolla (32.5cm real -> ~14.3cm en mesa)
            GameObject lazoPrefab = CreateOrUpdatePowerPrefab(
                "Assets/Poderes/Prefabs/Prefab_Poder_Lazo.prefab",
                "Assets/Poderes/Modelos/Modelo FBX/Lazo.fbx",
                new Vector3(0.52f, 0.52f, 0.52f),
                Vector3.zero,
                Vector3.zero,
                PrimitiveType.Cylinder,
                new Vector3(0.09f, 0.015f, 0.09f));

            // 5. Guitarra: Guitarra criolla clásica (98.7cm real -> ~19.7cm en mesa, acostada estilizada)
            GameObject guitarraPrefab = CreateOrUpdatePowerPrefab(
                "Assets/Poderes/Prefabs/Prefab_Poder_Guitarra.prefab",
                "Assets/Poderes/Modelos/Modelo FBX/Guitarra.fbx",
                new Vector3(0.23f, 0.23f, 0.23f),
                new Vector3(0f, 30f, 0f),
                Vector3.zero,
                PrimitiveType.Cube,
                new Vector3(0.065f, 0.02f, 0.12f));

            // 6. Pava Caliente: Pava de campo enlozada tiznada (22.3cm real -> ~12.9cm en mesa)
            GameObject pavaPrefab = CreateOrUpdatePowerPrefab(
                "Assets/Poderes/Prefabs/Prefab_Poder_PavaCaliente.prefab",
                "Assets/Poderes/Modelos/Modelo FBX/Pava.fbx",
                new Vector3(0.68f, 0.68f, 0.68f),
                new Vector3(0f, -45f, 0f),
                Vector3.zero,
                PrimitiveType.Cylinder,
                new Vector3(0.07f, 0.055f, 0.07f));

            // 7. Mano de Dios: Escultura dorada de la mano mítica con nubes (34.8cm real -> ~13.9cm en mesa)
            GameObject manoPrefab = CreateOrUpdatePowerPrefab(
                "Assets/Poderes/Prefabs/Prefab_Poder_ManoDeDios.prefab",
                "Assets/Poderes/Modelos/Modelo FBX/ManoDeDios.fbx",
                new Vector3(0.47f, 0.47f, 0.47f),
                Vector3.zero,
                Vector3.zero,
                PrimitiveType.Sphere,
                new Vector3(0.095f, 0.095f, 0.095f));

            // 8. Boleadoras: Tres ramales con bolas de piedra (47.3cm real -> ~15.1cm en mesa)
            GameObject boleadorasPrefab = CreateOrUpdatePowerPrefab(
                "Assets/Poderes/Prefabs/Prefab_Poder_Boleadoras.prefab",
                "Assets/Poderes/Modelos/Modelo FBX/Boleadoras.fbx",
                new Vector3(0.38f, 0.38f, 0.38f),
                Vector3.zero,
                Vector3.zero,
                PrimitiveType.Sphere,
                new Vector3(0.065f, 0.065f, 0.065f));

            // 9. Ceibo: Rama florecida de Flor Nacional del Ceibo (30cm real -> ~12.6cm en mesa)
            GameObject ceiboPrefab = CreateOrUpdatePowerPrefab(
                "Assets/Poderes/Prefabs/Prefab_Poder_Ceibo.prefab",
                "Assets/Poderes/Modelos/Modelo FBX/Ceibo.fbx",
                new Vector3(0.50f, 0.50f, 0.50f),
                Vector3.zero,
                Vector3.zero,
                PrimitiveType.Cylinder,
                new Vector3(0.06f, 0.04f, 0.06f));

            // 10. Luz Mala: Fuego fatuo espectral (V2 optimizada para prop de mano/mesa, 16.5cm real -> ~13.2cm)
            string luzMalaFbx = File.Exists(Path.Combine(Application.dataPath, "Poderes/Modelos/Modelo FBX/LuzMalaV2.fbx"))
                ? "Assets/Poderes/Modelos/Modelo FBX/LuzMalaV2.fbx"
                : "Assets/Poderes/Modelos/Modelo FBX/LuzMala.fbx";
            GameObject luzMalaPrefab = CreateOrUpdatePowerPrefab(
                "Assets/Poderes/Prefabs/Prefab_Poder_LuzMala.prefab",
                luzMalaFbx,
                new Vector3(0.94f, 0.94f, 0.94f),
                Vector3.zero,
                Vector3.zero,
                PrimitiveType.Sphere,
                new Vector3(0.075f, 0.075f, 0.075f));

            // 11. Bastón Presidencial: Bastón de mando de urunday y plata (80cm real -> ~17.6cm en mesa)
            GameObject bastonPrefab = CreateOrUpdatePowerPrefab(
                "Assets/Poderes/Prefabs/Prefab_Poder_BastonPresidencial.prefab",
                "Assets/Poderes/Modelos/Modelo FBX/Baston.fbx",
                new Vector3(0.25f, 0.25f, 0.25f),
                new Vector3(0f, 45f, 0f),
                Vector3.zero,
                PrimitiveType.Cylinder,
                new Vector3(0.02f, 0.08f, 0.02f));

            // ================= 2. EFECTOS SCRIPTABLEOBJECT =================
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

            // ================= 3. POWER DATA =================
            GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_Facon.asset",
                "facon",
                "Facón",
                "Rompé una carta a elección del rival (con clic); se revela y se le repone otra del mazo.",
                "Momento de activación: Durante el turno del jugador antes de tirar una carta a la mesa.\nMecánica: Hacé clic sobre la carta del rival que quieras destruir. Se revelará brevemente cuál era antes de descartarla, y el rival recibirá una nueva carta aleatoria del mazo.",
                "Cuchillo criollo gaucho / Facón tradicional con mango de alpaca/asta y vaina de cuero.",
                "Modelo 3D Final Low-Poly",
                PowerRarity.Comun,
                faconPrefab,
                faconEffect);

            GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_Mate.asset",
                "mate",
                "Mate",
                "Revela una carta de la mano del rival durante 5 segundos (ambos saben cuál es).",
                "Momento de activación: En cualquier momento durante el turno del jugador mientras el oponente conserve cartas en mano.\nMecánica: Revela una carta oculta de la mano del rival durante 5 segundos hacia el jugador local, avisándole al rival cuál carta le fue vista.",
                "Mate de calabaza curada con virola labrada y bombilla de alpaca.",
                "Modelo 3D Final Low-Poly",
                PowerRarity.Comun,
                matePrefab,
                mateEffect);

            GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_Guampas.asset",
                "guampas",
                "Guampas",
                "Roba una carta del mazo; elegí con clic izquierdo por cuál cambiarla, o clic derecho para descartarla.",
                "Momento de activación: En el turno del jugador antes de tirar una carta a la mesa.\nMecánica: Toma una carta del mazo y permite al jugador elegir en 3D si reemplazar una carta de su mano o descartarla conservando su mano original.",
                "Par de cuernos de buey / guampas criollas pulidas.",
                "Modelo 3D Final Low-Poly",
                PowerRarity.Comun,
                guampasPrefab,
                guampasEffect);

            GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_Lazo.asset",
                "lazo",
                "Lazo",
                "Solo en primera mano: descarta todas tus cartas y roba 3 cartas nuevas.",
                "Momento de activación: Únicamente durante la primera mano (baza 0), antes de haber jugado ninguna carta.\nMecánica: Descarta las 3 cartas iniciales completas al mazo y recibe 3 cartas nuevas extraídas del mazo.",
                "Lazo gaucho trenzado de cuero crudo en rollo con argolla.",
                "Modelo 3D Final Low-Poly",
                PowerRarity.Comun,
                lazoPrefab,
                lazoEffect);

            GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_Guitarra.asset",
                "guitarra",
                "Guitarra",
                "Intercambia una de tus cartas por una del rival eligiendo ambas cartas con clic directo en 3D.",
                "Momento de activación: Durante el turno del jugador antes de tirar su carta.\nMecánica: El jugador hace clic en una carta propia (se eleva como confirmación) y luego hace clic en la carta rival deseada a ciegas para intercambiarlas.",
                "Guitarra criolla clásica de madera con roseta y cuerdas de nylon.",
                "Modelo 3D Final Low-Poly",
                PowerRarity.Comun,
                guitarraPrefab,
                guitarraEffect);

            GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_PavaCaliente.asset",
                "pava_caliente",
                "Pava Caliente",
                "El rival tiene solo 5 segundos por turno durante toda la mano, o jugará al azar.",
                "Momento de activación: En cualquier momento durante la ronda.\nMecánica: Aplica un temporizador de presión extrema: el rival dispone de únicamente 5 segundos por cada decisión de juego.",
                "Pava de campo tiznada de chapa enlozada negra para fuego a leña.",
                "Modelo 3D Final Low-Poly",
                PowerRarity.Comun,
                pavaPrefab,
                pavaEffect);

            GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_ManoDeDios.asset",
                "mano_de_dios",
                "Mano de Dios",
                "Sos Mano de la ronda (ganás empates) y le cedés el tiro de la baza al rival.",
                "Momento de activación: Durante el turno del jugador antes de tirar su carta o resolver empates.\nMecánica: Asigna la condición de Mano de la ronda y cede el turno de tiro al oponente.",
                "Escultura dorada de una mano abierta apuntando hacia el cielo.",
                "Modelo 3D Final Low-Poly",
                PowerRarity.Comun,
                manoPrefab,
                manoEffect);

            GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_Boleadoras.asset",
                "boleadoras",
                "Boleadoras",
                "Traba al rival: le impide recibir su próximo poder en la siguiente ronda.",
                "Momento de activación: Durante cualquier momento de la ronda en curso.\nMecánica: Bloquea al rival colocándole el estado Boleado, impidiéndole recibir poderes en la mesa en la próxima ronda.",
                "Boleadoras gauchas de tres ramales trenzados con bolas de piedra.",
                "Modelo 3D Final Low-Poly",
                PowerRarity.Comun,
                boleadorasPrefab,
                boleadorasEffect);

            GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_Ceibo.asset",
                "ceibo",
                "Ceibo",
                "Cambia el palo de todas tus cartas al mismo palo aleatorio (alto Envido asegurado).",
                "Momento de activación: En cualquier momento antes de jugar todas las cartas (ideal antes de cantar Envido).\nMecánica: Transforma el palo de todas tus cartas al mismo palo manteniendo sus números, garantizando un Envido legendario.",
                "Ramillete florecido del Ceibo (Flor Nacional Argentina) con pétalos carmesí.",
                "Modelo 3D Final Low-Poly",
                PowerRarity.Legendario,
                ceiboPrefab,
                ceiboEffect);

            GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_LuzMala.asset",
                "luz_mala",
                "Luz Mala",
                "Vuelve blancas y mezcla las cartas del rival: no sabrá cuál es cuál al jugar.",
                "Momento de activación: Durante el turno del jugador antes de que el rival juegue sus cartas clave.\nMecánica: Envuelve las cartas del oponente en un halo espectral blanco, forzándolo a jugar a ciegas.",
                "Fuego fatuo espectral / candil antiguo con resplandor verdoso fantasmal.",
                "Modelo 3D Final Low-Poly",
                PowerRarity.Legendario,
                luzMalaPrefab,
                luzMalaEffect);

            GetOrCreatePowerData(
                "Assets/Poderes/Data/Poder_BastonPresidencial.asset",
                "baston_presidencial",
                "Bastón Presidencial",
                "Obliga al rival a aceptar (decir '¡Quiero!') a tu próximo canto.",
                "Momento de activación: Justo antes de efectuar un canto (Envido o Truco).\nMecánica: Impone autoridad absoluta sobre la voluntad del oponente: bloquea la opción 'No Quiero' y fuerza la respuesta '¡Quiero!'.",
                "Bastón de mando presidencial de madera de urunday con empuñadura y regatón de plata cincelada.",
                "Modelo 3D Final Low-Poly",
                PowerRarity.Legendario,
                bastonPrefab,
                bastonEffect);

            // ================= 4. POWER CATALOG =================
            const string catalogPath = "Assets/Poderes/Data/PowerCatalog.asset";
            const string resourcesCatalogPath = "Assets/Resources/PowerCatalog.asset";

            PowerCatalog catalog = AssetDatabase.LoadAssetAtPath<PowerCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PowerCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            catalog.AvailablePowers.Clear();
            string[] allPowerGuids = AssetDatabase.FindAssets("t:PowerData", new[] { "Assets/Poderes/Data" });
            foreach (string guid in allPowerGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                PowerData powerData = AssetDatabase.LoadAssetAtPath<PowerData>(assetPath);
                if (powerData != null && powerData.PowerId != "test_power" && !assetPath.Contains("Prueba") && !catalog.AvailablePowers.Contains(powerData))
                {
                    catalog.AvailablePowers.Add(powerData);
                }
            }

            EditorUtility.SetDirty(catalog);
            if (File.Exists(resourcesCatalogPath))
            {
                AssetDatabase.DeleteAsset(resourcesCatalogPath);
            }
            AssetDatabase.CopyAsset(catalogPath, resourcesCatalogPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#4CAF50><b>[Poderes]</b> Todos los modelos FBX reales han sido configurados e integrados exitosamente en sus respectivos Prefabs y PowerData.</color>");
        }

        private static GameObject CreateOrUpdatePowerPrefab(
            string prefabPath,
            string fbxPath,
            Vector3 modelScale,
            Vector3 modelRotation,
            Vector3 modelOffset,
            PrimitiveType fallbackPrimitive,
            Vector3 fallbackScale)
        {
            GameObject fbxAsset = !string.IsNullOrEmpty(fbxPath) ? AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath) : null;
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

            GameObject rootObj = new GameObject(prefabName);
            rootObj.AddComponent<PowerVisual>();

            Bounds totalBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasBounds = false;

            if (fbxAsset != null)
            {
                GameObject visualChild = null;
                if (PrefabUtility.IsPartOfPrefabAsset(fbxAsset))
                {
                    visualChild = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
                }
                if (visualChild == null)
                {
                    visualChild = Object.Instantiate(fbxAsset);
                }

                visualChild.name = "VisualModel";
                visualChild.transform.SetParent(rootObj.transform, false);
                visualChild.transform.localPosition = modelOffset;
                visualChild.transform.localRotation = Quaternion.Euler(modelRotation);
                visualChild.transform.localScale = modelScale;

                // Destruir colliders internos del FBX si existieran
                Collider[] childColliders = visualChild.GetComponentsInChildren<Collider>(true);
                foreach (var c in childColliders)
                {
                    Object.DestroyImmediate(c);
                }

                // Asignación de materiales y texturas específicas si corresponde (ej. Mano de Dios)
                if (prefabName.Contains("ManoDeDios"))
                {
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Poderes/Modelos/Modelo FBX/ManoDeDios_Textura.png");
                    Material balonMat = GetOrCreateTextureMaterial("Assets/Poderes/Materiales/Mat_ManoDeDios_Balon.mat", tex, HexToColor("#201804"));
                    Material nubeAMat = GetOrCreateMaterial("Assets/Poderes/Materiales/Mat_ManoDeDios_NubeA.mat", HexToColor("#DFE3E6"), HexToColor("#181B1E"));
                    Material nubeBMat = GetOrCreateMaterial("Assets/Poderes/Materiales/Mat_ManoDeDios_NubeB.mat", HexToColor("#C6D4DF"), HexToColor("#141A20"));
                    Material nubeCMat = GetOrCreateMaterial("Assets/Poderes/Materiales/Mat_ManoDeDios_NubeC.mat", HexToColor("#A8BFCF"), HexToColor("#101720"));

                    Renderer rend = visualChild.GetComponentInChildren<Renderer>(true);
                    if (rend != null)
                    {
                        rend.sharedMaterials = new Material[] { balonMat, nubeAMat, nubeBMat, nubeCMat };
                    }
                }

                Renderer[] renderers = visualChild.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (!hasBounds)
                    {
                        totalBounds = r.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        totalBounds.Encapsulate(r.bounds);
                    }
                }
            }
            else
            {
                GameObject primitive = GameObject.CreatePrimitive(fallbackPrimitive);
                primitive.name = "VisualPrimitive";
                primitive.transform.SetParent(rootObj.transform, false);
                primitive.transform.localScale = fallbackScale;

                Collider primCol = primitive.GetComponent<Collider>();
                if (primCol != null) Object.DestroyImmediate(primCol);

                totalBounds = new Bounds(Vector3.up * (fallbackScale.y * 0.5f), fallbackScale);
                hasBounds = true;
            }

            // Collider ergonómico en el Root para interacción táctil en primera persona
            BoxCollider collider = rootObj.AddComponent<BoxCollider>();
            if (hasBounds)
            {
                Vector3 localCenter = rootObj.transform.InverseTransformPoint(totalBounds.center);
                Vector3 localSize = rootObj.transform.InverseTransformVector(totalBounds.size);
                localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

                // Mínimo ergonómico ampliado para clic infalible con nuevos tamaños
                localSize = Vector3.Max(localSize, new Vector3(0.14f, 0.12f, 0.14f));
                collider.center = localCenter;
                collider.size = localSize;
            }
            else
            {
                collider.center = new Vector3(0f, 0.06f, 0f);
                collider.size = new Vector3(0.14f, 0.12f, 0.14f);
            }

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(rootObj, prefabPath);
            Object.DestroyImmediate(rootObj);
            return savedPrefab;
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

        private static Material GetOrCreateTextureMaterial(string path, Texture2D texture, Color? emission = null)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }

            mat.color = Color.white;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (texture != null)
            {
                mat.mainTexture = texture;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
            }
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission.Value);
            }
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateMaterial(string path, Color color, Color? emission = null)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }

            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission.Value);
            }
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color c)) return c;
            return Color.white;
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
