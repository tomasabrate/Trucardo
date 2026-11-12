using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using Trucardo.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Trucardo.EditorScripts
{
    /// <summary>
    /// Aplica arreglos de escena (bancos de barra con SeatInteractable, activación de PosicionBartender
    /// para multijugador) y crea/coloca los carteles de madera con número en tiza en todas las mesas
    /// de juego (visibles únicamente en modo Un Jugador).
    /// </summary>
    [InitializeOnLoad]
    public static class SetupSceneFixesAndNumbers
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string PrefabPath = "Assets/Core/Prefabs/CartelMesaNumeroPrefab.prefab";
        private const string MaterialsFolder = "Assets/Models/BarRecursos/TableroRivales";
        private const string SlateMatPath = "Assets/Models/BarRecursos/TableroRivales/Mat_CartelMesa_Pizarra.mat";
        private const string DarkWoodMatPath = "Assets/Models/BarRecursos/TableroRivales/Mat_Tablero_MarcoOscuro.mat";
        private const string LightTrimMatPath = "Assets/Models/BarRecursos/TableroRivales/Mat_Tablero_BordeClaro.mat";
        private const string BrassMatPath = "Assets/Models/BarRecursos/TableroRivales/Mat_Tablero_HerrajeBronce.mat";
        private const string ChalkFontPath = "Assets/Art/Fonts/FrederickatheGreat-Regular SDF.asset";
        private const string WoodAlbedoPath = "Assets/Models/BarRecursos/Maderas/Wood_027_basecolor.jpg";
        private const string WoodNormalPath = "Assets/Models/BarRecursos/Maderas/Wood_027_normal.jpg";

        static SetupSceneFixesAndNumbers()
        {
            EditorApplication.update += RunOnceWhenReady;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EnsureInSampleScene(scene);
        }

        private static void RunOnceWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.update -= RunOnceWhenReady;

            Scene active = SceneManager.GetActiveScene();
            if (active.isLoaded && active.path == SampleScenePath)
            {
                EnsureInSampleScene(active);
            }
            else if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                BuildOrLoadPrefab();
            }
        }

        private static void EnsureInSampleScene(Scene scene)
        {
            if (!scene.isLoaded || scene.path != SampleScenePath) return;

            bool needsSetup = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null;

            if (!needsSetup)
            {
                foreach (EnsureBartender eb in Object.FindObjectsByType<EnsureBartender>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (eb != null && !eb.gameObject.activeSelf)
                    {
                        needsSetup = true;
                        break;
                    }
                }
            }

            if (!needsSetup)
            {
                foreach (GameObject go in scene.GetRootGameObjects())
                {
                    if (CheckNeedsBarStoolOrPlaqueSetup(go))
                    {
                        needsSetup = true;
                        break;
                    }
                }
            }

            if (!needsSetup)
            {
                TableNumberSignController[] signs = Object.FindObjectsByType<TableNumberSignController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (signs.Length < 9)
                {
                    needsSetup = true;
                }
            }

            if (needsSetup)
            {
                ApplyAll();
            }
        }

        private static bool CheckNeedsBarStoolOrPlaqueSetup(GameObject root)
        {
            if (root == null) return false;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "BanquitoLowPoly" && t.GetComponent<Collider>() != null && t.GetComponent<SeatInteractable>() == null)
                {
                    return true;
                }
            }
            return false;
        }

        [MenuItem("Trucardo/Aplicar Arreglos de Escena y Carteles")]
        public static void ApplyAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[SetupSceneFixesAndNumbers] Salí del modo Play antes de ejecutar este ajuste.");
                return;
            }

            bool dirty = false;

            dirty |= ActivateBartenderAndMusicianSpawners();
            dirty |= FixBarStoolsSeatInteractables();
            dirty |= CreateAndPlaceTableNumberPlaques();

            AssetDatabase.SaveAssets();

            Scene activeScene = SceneManager.GetActiveScene();
            if (dirty && activeScene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                if (activeScene.path == SampleScenePath)
                {
                    EditorSceneManager.SaveScene(activeScene);
                }
            }

            Debug.Log("<color=#4CAF50><b>[SetupSceneFixesAndNumbers] Bancos de barra, NPCs de bar/multijugador y carteles de tiza por mesa aplicados correctamente.</b></color>");
        }

        private static bool ActivateBartenderAndMusicianSpawners()
        {
            bool changed = false;

            foreach (EnsureBartender eb in Object.FindObjectsByType<EnsureBartender>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (eb == null) continue;
                if (!eb.gameObject.activeSelf)
                {
                    Undo.RecordObject(eb.gameObject, "Activate PosicionBartender");
                    eb.gameObject.SetActive(true);
                    EditorUtility.SetDirty(eb.gameObject);
                    changed = true;
                }
            }

            foreach (EnsureMusicianGaucho em in Object.FindObjectsByType<EnsureMusicianGaucho>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (em == null) continue;
                if (!em.gameObject.activeSelf)
                {
                    Undo.RecordObject(em.gameObject, "Activate MusicaBar");
                    em.gameObject.SetActive(true);
                    EditorUtility.SetDirty(em.gameObject);
                    changed = true;
                }
                SerializedObject so = new SerializedObject(em);
                SerializedProperty prop = so.FindProperty("musicoPrefab");
                if (prop != null && prop.objectReferenceValue == null)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Core/Prefabs/GauchoMusicoPrefab.prefab");
                    if (prefab != null)
                    {
                        prop.objectReferenceValue = prefab;
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(em);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private static bool FixBarStoolsSeatInteractables()
        {
            bool changed = false;
            List<SeatInteractable> stoolColliders = new List<SeatInteractable>();

            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t == null || t.name != "BanquitoLowPoly") continue;
                if (t.GetComponent<Collider>() == null) continue;

                SeatInteractable seat = t.GetComponent<SeatInteractable>();
                if (seat == null)
                {
                    seat = Undo.AddComponent<SeatInteractable>(t.gameObject);
                    EditorUtility.SetDirty(t.gameObject);
                    changed = true;
                }
                stoolColliders.Add(seat);
            }

            // Remover SeatInteractable duplicado en mallas superpuestas (ej. Banquilloalto) que comparten exactamente el mismo banco
            foreach (SeatInteractable existing in Object.FindObjectsByType<SeatInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (existing == null || existing.name == "BanquitoLowPoly") continue;
                if (TableDefinition.FindForSeat(existing.transform) != null) continue;

                string lower = existing.name.ToLowerInvariant();
                if (!lower.Contains("banquillo") && !lower.Contains("banquito")) continue;

                foreach (SeatInteractable primary in stoolColliders)
                {
                    if (primary == null) continue;
                    Vector2 delta = new Vector2(
                        existing.transform.position.x - primary.transform.position.x,
                        existing.transform.position.z - primary.transform.position.z);
                    if (delta.sqrMagnitude < 0.04f)
                    {
                        Undo.DestroyObjectImmediate(existing);
                        changed = true;
                        break;
                    }
                }
            }

            return changed;
        }

        private static bool CreateAndPlaceTableNumberPlaques()
        {
            TableDefinition.EnsureSceneConfiguration();
            GameObject prefab = BuildOrLoadPrefab();
            if (prefab == null) return false;

            bool changed = false;
            TableDefinition[] tables = TableDefinition.FindAll();

            foreach (TableDefinition table in tables)
            {
                if (table == null) continue;

                Match match = Regex.Match(table.TableId ?? table.name, @"(\d+)");
                if (!match.Success || !int.TryParse(match.Groups[1].Value, out int tableNum)) continue;
                if (tableNum < 1 || tableNum > 9) continue;

                if (table.Seats == null || table.Seats.Count == 0 || table.Seats[0] == null) continue;

                Transform seat0 = table.Seats[0].transform;
                ComputePlaqueTransformOnTable(table.transform, seat0.position, out Vector3 worldPos, out Quaternion worldRot);

                string signName = $"Cartel_Mesa_{tableNum}";
                List<GameObject> oldSigns = new List<GameObject>();
                foreach (Transform child in table.GetComponentsInChildren<Transform>(true))
                {
                    if (child == null || child == table.transform) continue;
                    if (child.name == signName || child.GetComponent<TableNumberSignController>() != null)
                    {
                        oldSigns.Add(child.gameObject);
                    }
                }
                foreach (GameObject old in oldSigns)
                {
                    if (old != null)
                    {
                        Undo.DestroyObjectImmediate(old);
                        changed = true;
                    }
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, table.transform);
                instance.name = signName;
                instance.transform.SetPositionAndRotation(worldPos, worldRot);

                TableNumberSignController ctrl = instance.GetComponent<TableNumberSignController>();
                if (ctrl != null)
                {
                    Transform visuals = instance.transform.Find("Visuals");
                    TextMeshPro numTmp = instance.transform.Find("Visuals/Marco_Tilt/Texto_Numero")?.GetComponent<TextMeshPro>();
                    TextMeshPro subTmp = instance.transform.Find("Visuals/Marco_Tilt/Texto_Subtitulo")?.GetComponent<TextMeshPro>();
                    ctrl.Configure(tableNum, visuals != null ? visuals.gameObject : null, numTmp, subTmp);
                    ctrl.ApplyVisibility(true);
                }

                PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
                EditorUtility.SetDirty(instance);
                changed = true;
            }

            return changed;
        }

        private static void ComputePlaqueTransformOnTable(
            Transform tableTransform,
            Vector3 playerSeatPos,
            out Vector3 worldPos,
            out Quaternion worldRot)
        {
            Renderer topRenderer = FindTabletopRenderer(tableTransform);
            Vector3 center = topRenderer != null ? topRenderer.bounds.center : tableTransform.position;
            float topY = topRenderer != null ? topRenderer.bounds.max.y : tableTransform.position.y + 0.95f;

            Vector3 towardTable = new Vector3(center.x - playerSeatPos.x, 0f, center.z - playerSeatPos.z);
            if (towardTable.sqrMagnitude < 0.001f)
            {
                towardTable = tableTransform.forward;
                towardTable.y = 0f;
            }
            towardTable.Normalize();

            Vector3 tableRight = Vector3.Cross(Vector3.up, towardTable).normalized;

            float halfSide = 0.70f;
            float halfDepth = 0.70f;
            if (topRenderer != null)
            {
                Vector3 lossy = topRenderer.transform.lossyScale;
                float minXZ = Mathf.Min(Mathf.Abs(lossy.x), Mathf.Abs(lossy.z)) * 0.5f;
                if (minXZ >= 0.40f && minXZ <= 1.50f)
                {
                    halfSide = minXZ;
                    halfDepth = minXZ;
                }
            }

            // Esquina frontal derecha del jugador (fuera del rango de cartas [-0.27..0.27] y poderes [-0.59..-0.41])
            float frontOffset = -Mathf.Min(0.54f, halfDepth - 0.14f);
            float rightOffset = Mathf.Min(0.49f, halfSide - 0.16f);

            worldPos = new Vector3(center.x, topY, center.z)
                + towardTable * frontOffset
                + tableRight * rightOffset;

            // El cartel tiene su cara frontal TextMeshPro mirando hacia su -Z local; orientarlo levemente hacia Banco A
            Vector3 seatPosFlat = new Vector3(playerSeatPos.x, worldPos.y, playerSeatPos.z);
            Vector3 awayFromSeat = (worldPos - seatPosFlat);
            awayFromSeat.y = 0f;
            if (awayFromSeat.sqrMagnitude < 0.001f) awayFromSeat = towardTable;
            awayFromSeat.Normalize();

            Vector3 forwardZ = Vector3.Lerp(towardTable, awayFromSeat, 0.40f).normalized;
            worldRot = Quaternion.LookRotation(forwardZ, Vector3.up);
        }

        private static Renderer FindTabletopRenderer(Transform tableTransform)
        {
            Renderer best = null;
            float bestScore = float.NegativeInfinity;

            foreach (Renderer r in tableTransform.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled || r.bounds.size.y > 0.40f) continue;
                if (r.GetComponentInParent<SeatInteractable>() != null) continue;
                if (r.GetComponentInParent<TableNumberSignController>() != null) continue;

                string lower = r.gameObject.name.ToLowerInvariant();
                if (lower.Contains("silla") || lower.Contains("banco") || lower.Contains("banquito")) continue;

                float areaXZ = r.bounds.size.x * r.bounds.size.z;
                if (areaXZ < 0.45f) continue;

                float score = r.bounds.max.y * 10f + areaXZ;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = r;
                }
            }

            return best;
        }

        private static GameObject BuildOrLoadPrefab()
        {
            EnsureFolders();

            Material slateMat = EnsureSlateMaterial();
            Material darkWood = LoadOrFallbackWoodMaterial(DarkWoodMatPath, new Color(0.24f, 0.14f, 0.08f, 1f), 0.20f);
            Material lightTrim = LoadOrFallbackWoodMaterial(LightTrimMatPath, new Color(0.44f, 0.28f, 0.16f, 1f), 0.26f);
            Material brassMat = EnsureSimpleColorMaterial(BrassMatPath, new Color(0.68f, 0.50f, 0.22f, 1f), 0.78f, 0.46f);
            TMP_FontAsset chalkFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChalkFontPath);

            GameObject root = new GameObject("CartelMesaNumeroPrefab");
            TableNumberSignController controller = root.AddComponent<TableNumberSignController>();

            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(root.transform, false);

            // Base de apoyo apoyada al ras en Y=0 de la superficie de la mesa
            CreateWoodBox("Base_Madera", visuals.transform, new Vector3(0f, 0.009f, 0.008f), Quaternion.identity, new Vector3(0.156f, 0.018f, 0.068f), darkWood);
            CreateWoodBox("Soporte_Trasero", visuals.transform, new Vector3(0f, 0.040f, 0.024f), Quaternion.Euler(-22f, 0f, 0f), new Vector3(0.036f, 0.062f, 0.018f), darkWood);

            // Atril inclinado +38° en X para que la cara frontal (-Z) mire hacia arriba y al jugador
            GameObject tilt = new GameObject("Marco_Tilt");
            tilt.transform.SetParent(visuals.transform, false);
            tilt.transform.localPosition = new Vector3(0f, 0.056f, -0.002f);
            tilt.transform.localRotation = Quaternion.Euler(38f, 0f, 0f);

            CreateWoodBox("Marco_Exterior", tilt.transform, Vector3.zero, Quaternion.identity, new Vector3(0.172f, 0.122f, 0.014f), darkWood);
            CreateWoodBox("Borde_Claro", tilt.transform, Vector3.zero, Quaternion.identity, new Vector3(0.158f, 0.108f, 0.016f), lightTrim);
            CreateWoodBox("Panel_Pizarra", tilt.transform, Vector3.zero, Quaternion.identity, new Vector3(0.146f, 0.096f, 0.018f), slateMat);

            // Tachuelas de bronce decorativas en las esquinas superiores del marco
            CreateWoodBox("Clavo_Izq", tilt.transform, new Vector3(-0.070f, 0.049f, -0.008f), Quaternion.identity, new Vector3(0.007f, 0.007f, 0.004f), brassMat);
            CreateWoodBox("Clavo_Der", tilt.transform, new Vector3(0.070f, 0.049f, -0.008f), Quaternion.identity, new Vector3(0.007f, 0.007f, 0.004f), brassMat);

            TextMeshPro subTmp = CreateChalkText(
                tilt.transform,
                "Texto_Subtitulo",
                new Vector3(0f, 0.029f, -0.0105f),
                new Vector2(0.140f, 0.028f),
                chalkFont,
                0.20f,
                0.12f,
                0.23f,
                new Color(0.84f, 0.79f, 0.67f, 0.94f),
                "MESA");

            TextMeshPro numTmp = CreateChalkText(
                tilt.transform,
                "Texto_Numero",
                new Vector3(0f, -0.012f, -0.0105f),
                new Vector2(0.140f, 0.062f),
                chalkFont,
                0.54f,
                0.28f,
                0.56f,
                new Color(0.96f, 0.93f, 0.85f, 0.98f),
                "1");

            SetLayerRecursively(visuals, 2); // Ignore Raycast para no interferir con clics en cartas

            controller.Configure(1, visuals, numTmp, subTmp);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static GameObject CreateWoodBox(string name, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale, Material mat)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPos;
            cube.transform.localRotation = localRot;
            cube.transform.localScale = localScale;

            Collider col = cube.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            MeshRenderer mr = cube.GetComponent<MeshRenderer>();
            if (mr != null && mat != null)
            {
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.On;
                mr.receiveShadows = true;
            }
            return cube;
        }

        private static TextMeshPro CreateChalkText(
            Transform parent,
            string name,
            Vector3 localPos,
            Vector2 rectSize,
            TMP_FontAsset fontAsset,
            float fontSize,
            float minSize,
            float maxSize,
            Color color,
            string initialText)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;

            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            RectTransform rt = tmp.rectTransform;
            rt.localScale = Vector3.one;
            rt.sizeDelta = rectSize;

            if (fontAsset != null) tmp.font = fontAsset;
            tmp.text = initialText;
            tmp.fontStyle = FontStyles.Bold;
            tmp.fontSize = fontSize;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = minSize;
            tmp.fontSizeMax = maxSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.color = color;

            return tmp;
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Core/Prefabs"))
                AssetDatabase.CreateFolder("Assets/Core", "Prefabs");
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
                AssetDatabase.CreateFolder("Assets/Models/BarRecursos", "TableroRivales");
        }

        private static Material EnsureSlateMaterial()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(SlateMatPath);
            if (mat == null)
            {
                mat = new Material(urpLit);
                AssetDatabase.CreateAsset(mat, SlateMatPath);
            }
            else if (mat.shader != urpLit)
            {
                mat.shader = urpLit;
            }

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(WoodAlbedoPath);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(WoodNormalPath);
            mat.SetColor("_BaseColor", new Color(0.11f, 0.095f, 0.08f, 1f));
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.05f);
            if (albedo != null)
            {
                mat.SetTexture("_BaseMap", albedo);
                mat.SetTextureScale("_BaseMap", new Vector2(1.5f, 1.5f));
            }
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", 0.35f);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material LoadOrFallbackWoodMaterial(string matPath, Color fallbackColor, float smoothness)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null) return existing;
            return EnsureSimpleColorMaterial(matPath, fallbackColor, 0f, smoothness);
        }

        private static Material EnsureSimpleColorMaterial(string matPath, Color color, float metallic, float smoothness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null) return mat;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(urpLit);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(mat, matPath);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
