using System.IO;
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
    /// Crea los materiales, el prefab 3D low-poly y coloca el Tablero de Rivales en la pared del bar
    /// (Mueble_Bebidas (2), entre la esquina del músico y la puerta del Almacén).
    /// </summary>
    [InitializeOnLoad]
    public static class SetupRivalBoard
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string BoardObjectName = "Tablero_Rivales";
        private const string PrefabPath = "Assets/Core/Prefabs/TableroRivalesPrefab.prefab";
        private const string MaterialsFolder = "Assets/Models/BarRecursos/TableroRivales";

        private const string FontDisplayPath = "Assets/Art/Fonts/JacquesFrancoisShadow-Regular SDF.asset";
        private const string FontBodyPath = "Assets/Art/Fonts/IMFellEnglish-Regular SDF.asset";
        private const string FontItalicPath = "Assets/Art/Fonts/IMFellEnglish-Italic SDF.asset";

        static SetupRivalBoard()
        {
            EditorApplication.update += RunOnceWhenReady;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EnsureBoardInSampleSceneIfNeeded(scene);
        }

        private static void RunOnceWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.update -= RunOnceWhenReady;

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isLoaded && activeScene.path == ScenePath)
            {
                EnsureBoardInSampleSceneIfNeeded(activeScene);
            }
            else if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                BuildPrefabOnly();
            }
        }

        private static void EnsureBoardInSampleSceneIfNeeded(Scene scene)
        {
            if (!scene.isLoaded || scene.path != ScenePath) return;

            GameObject existing = GameObject.Find(BoardObjectName);
            bool prefabExists = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;

            if (existing == null || existing.GetComponent<RivalBoardController>() == null || !prefabExists)
            {
                Setup();
            }
            else
            {
                RivalBoardController ctrl = existing.GetComponent<RivalBoardController>();
                if (ctrl != null)
                {
                    ctrl.RefreshBoard();
                }
            }
        }

        [MenuItem("Trucardo/Configurar Tablero de Rivales")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[SetupRivalBoard] Salí del modo Play antes de regenerar el Tablero de Rivales.");
                return;
            }

            EnsureFolders();

            BoardMaterials mats = CreateOrUpdateBoardMaterials();
            ComputeWallPlacement(out Vector3 boardPos, out Quaternion boardRot);

            DestroyExistingByName(BoardObjectName);

            GameObject boardRoot = BuildBoardHierarchy(BoardObjectName, boardPos, boardRot, mats);
            GameObject savedPrefab = SavePrefabAndConnect(boardRoot);

            RivalBoardController ctrl = boardRoot.GetComponent<RivalBoardController>();
            if (ctrl != null)
            {
                ctrl.RefreshBoard();
            }

            EditorUtility.SetDirty(boardRoot);
            AssetDatabase.SaveAssets();

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                if (activeScene.path == ScenePath)
                {
                    EditorSceneManager.SaveScene(activeScene);
                }
            }

            Debug.Log($"<color=#4CAF50><b>[SetupRivalBoard] Tablero de Rivales creado y guardado ({(savedPrefab != null ? PrefabPath : "sin prefab")}).</b></color>");
        }

        private static void BuildPrefabOnly()
        {
            EnsureFolders();
            BoardMaterials mats = CreateOrUpdateBoardMaterials();

            GameObject tempRoot = BuildBoardHierarchy(BoardObjectName, Vector3.zero, Quaternion.identity, mats);
            RivalBoardController ctrl = tempRoot.GetComponent<RivalBoardController>();
            if (ctrl != null) ctrl.RefreshBoard();

            PrefabUtility.SaveAsPrefabAsset(tempRoot, PrefabPath);
            Object.DestroyImmediate(tempRoot);
            AssetDatabase.SaveAssets();
        }

        private static GameObject SavePrefabAndConnect(GameObject boardRoot)
        {
            if (boardRoot == null) return null;
            return PrefabUtility.SaveAsPrefabAssetAndConnect(boardRoot, PrefabPath, InteractionMode.AutomatedAction);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Core/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/Core", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                string parent = Path.GetDirectoryName(MaterialsFolder)?.Replace("\\", "/") ?? "Assets/Models/BarRecursos";
                string leaf = Path.GetFileName(MaterialsFolder);
                AssetDatabase.CreateFolder(parent, leaf);
            }
        }

        private static void DestroyExistingByName(string objName)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            foreach (GameObject rootObj in scene.GetRootGameObjects())
            {
                if (rootObj != null && rootObj.name == objName)
                {
                    Undo.DestroyObjectImmediate(rootObj);
                }
            }
        }

        /// <summary>
        /// Calcula la posición y rotación sobre la cara del muro Mueble_Bebidas (2) que mira al salón del bar (+X).
        /// </summary>
        private static void ComputeWallPlacement(out Vector3 position, out Quaternion rotation)
        {
            GameObject wall = GameObject.Find("Barra_Bar/Mueble_Bebidas (2)") ?? GameObject.Find("Mueble_Bebidas (2)");
            if (wall != null)
            {
                Transform wt = wall.transform;
                Vector3 wallOutward = wt.TransformDirection(Vector3.back).normalized;
                Vector3 wallRight = wt.TransformDirection(Vector3.right).normalized;
                float halfThickness = wt.lossyScale.z * 0.5f;

                // Centrado en el sector libre de la pared entre la Bandera y el arco de Almacén
                position = wt.position
                    + wallOutward * halfThickness
                    + wallRight * 0.10f
                    + Vector3.up * 0.04f;
                rotation = wt.rotation;
                return;
            }

            // Respaldo con las coordenadas medidas de Mueble_Bebidas (2) en SampleScene
            Quaternion fallbackWallRot = Quaternion.Euler(0f, -89.555f, 0f);
            Vector3 fallbackWallPos = new Vector3(-2.3255f, 1.9861f, 7.3726f);
            Vector3 outward = fallbackWallRot * Vector3.back;
            Vector3 right = fallbackWallRot * Vector3.right;
            position = fallbackWallPos + outward * 0.19f + right * 0.10f + Vector3.up * 0.04f;
            rotation = fallbackWallRot;
        }

        private struct BoardMaterials
        {
            public Material DarkFrame;
            public Material LightRim;
            public Material InnerPanel;
            public Material BossPlaque;
            public Material DarkIron;
            public Material BrassHardware;
        }

        private static BoardMaterials CreateOrUpdateBoardMaterials()
        {
            BoardMaterials mats = new BoardMaterials
            {
                DarkFrame = CreateWoodMaterial(
                    $"{MaterialsFolder}/Mat_Tablero_MarcoOscuro.mat",
                    "Assets/Resources/Texturas/Maderas/Dark Wooden Planks/dark_wooden_planks_diff_4k.jpg",
                    "Assets/Resources/Texturas/Maderas/Dark Wooden Planks/dark_wooden_planks_nor_gl_4k.jpg",
                    new Color(0.68f, 0.50f, 0.36f, 1f),
                    new Vector2(2f, 2f),
                    0.12f,
                    0.6f),

                LightRim = CreateWoodMaterial(
                    $"{MaterialsFolder}/Mat_Tablero_BordeClaro.mat",
                    "Assets/Resources/Texturas/Maderas/Wood Table Large/wood_table_large_diff_4k.jpg",
                    "Assets/Resources/Texturas/Maderas/Wood Table Large/wood_table_large_nor_gl_4k.jpg",
                    new Color(0.95f, 0.72f, 0.44f, 1f),
                    new Vector2(2f, 1f),
                    0.24f,
                    0.65f),

                InnerPanel = CreateWoodMaterial(
                    $"{MaterialsFolder}/Mat_Tablero_Panel.mat",
                    "Assets/Resources/Texturas/Maderas/Weathered Plank Siding/weathered_plank_siding_diff_4k.jpg",
                    "Assets/Resources/Texturas/Maderas/Weathered Plank Siding/weathered_plank_siding_nor_gl_4k.jpg",
                    new Color(0.76f, 0.58f, 0.44f, 1f),
                    new Vector2(2f, 2f),
                    0.08f,
                    0.5f),

                BossPlaque = CreateWoodMaterial(
                    $"{MaterialsFolder}/Mat_Tablero_PlacaPatron.mat",
                    "Assets/Resources/Texturas/Maderas/Dark Wooden Planks/dark_wooden_planks_diff_4k.jpg",
                    "Assets/Resources/Texturas/Maderas/Dark Wooden Planks/dark_wooden_planks_nor_gl_4k.jpg",
                    new Color(0.62f, 0.40f, 0.32f, 1f),
                    new Vector2(2f, 1f),
                    0.14f,
                    0.55f),

                DarkIron = CreateMetalMaterial(
                    $"{MaterialsFolder}/Mat_Tablero_HerrajeHierro.mat",
                    new Color(0.16f, 0.14f, 0.13f, 1f),
                    0.65f,
                    0.35f),

                BrassHardware = CreateMetalMaterial(
                    $"{MaterialsFolder}/Mat_Tablero_HerrajeBronce.mat",
                    new Color(0.68f, 0.50f, 0.22f, 1f),
                    0.82f,
                    0.46f)
            };

            return mats;
        }

        private static Material CreateWoodMaterial(
            string matPath,
            string albedoPath,
            string normalPath,
            Color baseColor,
            Vector2 tiling,
            float smoothness,
            float bumpScale)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(urpLit);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else if (mat.shader != urpLit)
            {
                mat.shader = urpLit;
            }

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", smoothness);

            if (albedo != null)
            {
                mat.SetTexture("_BaseMap", albedo);
                mat.SetTextureScale("_BaseMap", tiling);
            }

            bool normalReady = false;
            if (normal != null)
            {
                TextureImporter importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                normalReady = importer != null && importer.textureType == TextureImporterType.NormalMap;
            }

            if (normalReady)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetTextureScale("_BumpMap", tiling);
                mat.SetFloat("_BumpScale", bumpScale);
                mat.EnableKeyword("_NORMALMAP");
            }
            else
            {
                mat.SetTexture("_BumpMap", null);
                mat.DisableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateMetalMaterial(string matPath, Color baseColor, float metallic, float smoothness)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(urpLit);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else if (mat.shader != urpLit)
            {
                mat.shader = urpLit;
            }

            mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            mat.DisableKeyword("_NORMALMAP");

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject BuildBoardHierarchy(string rootName, Vector3 worldPos, Quaternion worldRot, BoardMaterials mats)
        {
            GameObject root = new GameObject(rootName);
            root.transform.position = worldPos;
            root.transform.rotation = worldRot;
            root.transform.localScale = Vector3.one;

            // --- Estructura de Madera Low-Poly (local -Z apunta hacia el salón) ---
            GameObject estructura = new GameObject("Estructura");
            estructura.transform.SetParent(root.transform, false);

            // Panel interior de madera tostada (lleva el BoxCollider del cartel)
            CreateBoxPart(estructura.transform, "Tablero_Fondo", new Vector3(0f, 0f, -0.025f), new Vector3(2.16f, 2.34f, 0.05f), mats.InnerPanel, keepCollider: true);

            // Marco exterior grueso oscuro
            CreateBoxPart(estructura.transform, "Marco_Exterior_Superior", new Vector3(0f, 1.205f, -0.050f), new Vector3(2.38f, 0.13f, 0.10f), mats.DarkFrame);
            CreateBoxPart(estructura.transform, "Marco_Exterior_Inferior", new Vector3(0f, -1.205f, -0.050f), new Vector3(2.38f, 0.13f, 0.10f), mats.DarkFrame);
            CreateBoxPart(estructura.transform, "Marco_Exterior_Izquierdo", new Vector3(-1.125f, 0f, -0.050f), new Vector3(0.13f, 2.28f, 0.10f), mats.DarkFrame);
            CreateBoxPart(estructura.transform, "Marco_Exterior_Derecho", new Vector3(1.125f, 0f, -0.050f), new Vector3(0.13f, 2.28f, 0.10f), mats.DarkFrame);

            // Moldura interior resaltada en roble claro para destacar el tablero contra la pared oscura
            CreateBoxPart(estructura.transform, "Marco_Interior_Superior", new Vector3(0f, 1.12f, -0.062f), new Vector3(2.14f, 0.045f, 0.06f), mats.LightRim);
            CreateBoxPart(estructura.transform, "Marco_Interior_Inferior", new Vector3(0f, -1.12f, -0.062f), new Vector3(2.14f, 0.045f, 0.06f), mats.LightRim);
            CreateBoxPart(estructura.transform, "Marco_Interior_Izquierdo", new Vector3(-1.04f, 0f, -0.062f), new Vector3(0.045f, 2.20f, 0.06f), mats.LightRim);
            CreateBoxPart(estructura.transform, "Marco_Interior_Derecho", new Vector3(1.04f, 0f, -0.062f), new Vector3(0.045f, 2.20f, 0.06f), mats.LightRim);

            // Copete superior escalonado de pulpería
            CreateBoxPart(estructura.transform, "Copete_Base", new Vector3(0f, 1.315f, -0.045f), new Vector3(1.56f, 0.09f, 0.09f), mats.DarkFrame);
            CreateBoxPart(estructura.transform, "Copete_Moldura", new Vector3(0f, 1.37f, -0.058f), new Vector3(1.32f, 0.04f, 0.068f), mats.LightRim);
            CreateBoxPart(estructura.transform, "Copete_Remate", new Vector3(0f, 1.41f, -0.045f), new Vector3(0.84f, 0.06f, 0.08f), mats.DarkFrame);

            // Esquinas metálicas y remaches low-poly de bronce en los 4 vértices
            Vector2[] cornerOffsets =
            {
                new Vector2(-1.125f, 1.205f),
                new Vector2(1.125f, 1.205f),
                new Vector2(-1.125f, -1.205f),
                new Vector2(1.125f, -1.205f)
            };
            for (int c = 0; c < cornerOffsets.Length; c++)
            {
                Vector2 xy = cornerOffsets[c];
                CreateBoxPart(estructura.transform, $"Esquina_Hierro_{c + 1}", new Vector3(xy.x, xy.y, -0.104f), new Vector3(0.15f, 0.15f, 0.014f), mats.DarkIron);
                CreateBoxPart(estructura.transform, $"Clavo_Bronce_{c + 1}", new Vector3(xy.x, xy.y, -0.114f), new Vector3(0.055f, 0.055f, 0.018f), mats.BrassHardware);
            }

            // Barras divisorias y placa empotrada del Jefe Final
            CreateBoxPart(estructura.transform, "Barra_Cabecera", new Vector3(0f, 0.635f, -0.058f), new Vector3(1.94f, 0.030f, 0.024f), mats.LightRim);
            CreateBoxPart(estructura.transform, "Barra_Divisoria_Jefe", new Vector3(0f, -0.465f, -0.064f), new Vector3(2.02f, 0.050f, 0.034f), mats.LightRim);
            CreateBoxPart(estructura.transform, "Clavo_Divisoria_Izq", new Vector3(-0.92f, -0.465f, -0.084f), new Vector3(0.042f, 0.042f, 0.014f), mats.BrassHardware);
            CreateBoxPart(estructura.transform, "Clavo_Divisoria_Der", new Vector3(0.92f, -0.465f, -0.084f), new Vector3(0.042f, 0.042f, 0.014f), mats.BrassHardware);

            GameObject bossPlaqueObj = CreateBoxPart(estructura.transform, "Placa_Patron", new Vector3(0f, -0.785f, -0.058f), new Vector3(1.96f, 0.56f, 0.016f), mats.BossPlaque);
            CreateBoxPart(estructura.transform, "Borde_Patron_Izq", new Vector3(-0.97f, -0.785f, -0.063f), new Vector3(0.025f, 0.56f, 0.026f), mats.LightRim);
            CreateBoxPart(estructura.transform, "Borde_Patron_Der", new Vector3(0.97f, -0.785f, -0.063f), new Vector3(0.025f, 0.56f, 0.026f), mats.LightRim);

            // --- Textos 3D (TextMeshPro mira hacia su local -Z, alineado con el frente del cartel) ---
            TMP_FontAsset fontDisplay = LoadFontWithFallback(FontDisplayPath);
            TMP_FontAsset fontBody = LoadFontWithFallback(FontBodyPath, fontDisplay);
            TMP_FontAsset fontItalic = LoadFontWithFallback(FontItalicPath, fontBody);

            GameObject textos = new GameObject("Textos");
            textos.transform.SetParent(root.transform, false);

            TMP_Text titleTmp = CreateWorldText(
                textos.transform,
                "Texto_Titulo",
                new Vector3(0f, 0.94f, -0.060f),
                new Vector2(1.92f, 0.22f),
                fontDisplay,
                FontStyles.Bold,
                1.65f,
                1.10f,
                1.75f,
                TextAlignmentOptions.Center,
                new Color(0.98f, 0.87f, 0.58f, 1f));

            TMP_Text progressTmp = CreateWorldText(
                textos.transform,
                "Texto_Progreso",
                new Vector3(0f, 0.74f, -0.060f),
                new Vector2(1.92f, 0.14f),
                fontBody,
                FontStyles.Bold,
                1.02f,
                0.75f,
                1.08f,
                TextAlignmentOptions.Center,
                new Color(0.94f, 0.85f, 0.68f, 1f));

            int botsCount = GameProgressionManager.RegularBotsCount;
            TMP_Text[] rowNames = new TMP_Text[botsCount];
            TMP_Text[] rowStatuses = new TMP_Text[botsCount];

            for (int i = 0; i < botsCount; i++)
            {
                int tableNum = i + 1;
                float rowY = 0.540f - i * 0.130f;

                rowNames[i] = CreateWorldText(
                    textos.transform,
                    $"Fila_{tableNum}_Nombre",
                    new Vector3(-0.23f, rowY, -0.060f),
                    new Vector2(1.42f, 0.125f),
                    fontBody,
                    FontStyles.Bold,
                    1.02f,
                    0.72f,
                    1.06f,
                    TextAlignmentOptions.MidlineLeft,
                    new Color(0.97f, 0.91f, 0.77f, 1f));

                rowStatuses[i] = CreateWorldText(
                    textos.transform,
                    $"Fila_{tableNum}_Estado",
                    new Vector3(0.73f, rowY, -0.060f),
                    new Vector2(0.44f, 0.125f),
                    fontBody,
                    FontStyles.Bold,
                    0.88f,
                    0.65f,
                    0.92f,
                    TextAlignmentOptions.MidlineRight,
                    new Color(0.94f, 0.75f, 0.39f, 1f));
            }

            TMP_Text bossTitleTmp = CreateWorldText(
                textos.transform,
                "Texto_Jefe_Titulo",
                new Vector3(0f, -0.60f, -0.076f),
                new Vector2(1.88f, 0.17f),
                fontDisplay,
                FontStyles.Bold,
                1.22f,
                0.90f,
                1.28f,
                TextAlignmentOptions.Center,
                new Color(0.96f, 0.62f, 0.30f, 1f));

            TMP_Text bossDetailTmp = CreateWorldText(
                textos.transform,
                "Texto_Jefe_Detalle",
                new Vector3(0f, -0.86f, -0.076f),
                new Vector2(1.86f, 0.30f),
                fontItalic,
                FontStyles.Normal,
                0.88f,
                0.68f,
                0.92f,
                TextAlignmentOptions.Center,
                new Color(0.90f, 0.80f, 0.64f, 1f),
                allowWrapping: true);

            RivalBoardController controller = root.AddComponent<RivalBoardController>();
            controller.ConfigureReferences(
                titleTmp,
                progressTmp,
                rowNames,
                rowStatuses,
                bossTitleTmp,
                bossDetailTmp,
                bossPlaqueObj.GetComponent<Renderer>());

            return root;
        }

        private static GameObject CreateBoxPart(
            Transform parent,
            string partName,
            Vector3 localPos,
            Vector3 localScale,
            Material material,
            bool keepCollider = false)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = localScale;

            Renderer rend = part.GetComponent<Renderer>();
            if (rend != null && material != null)
            {
                rend.sharedMaterial = material;
            }

            if (!keepCollider)
            {
                Collider col = part.GetComponent<Collider>();
                if (col != null)
                {
                    Object.DestroyImmediate(col);
                }
            }

            return part;
        }

        private static TMP_FontAsset LoadFontWithFallback(string preferredPath, TMP_FontAsset secondary = null)
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(preferredPath);
            if (font != null) return font;
            if (secondary != null) return secondary;
            return TMP_Settings.defaultFontAsset;
        }

        private static TMP_Text CreateWorldText(
            Transform parent,
            string objName,
            Vector3 localPos,
            Vector2 rectSize,
            TMP_FontAsset fontAsset,
            FontStyles style,
            float fontSize,
            float minSize,
            float maxSize,
            TextAlignmentOptions alignment,
            Color color,
            bool allowWrapping = false)
        {
            GameObject go = new GameObject(objName);
            go.transform.SetParent(parent, false);

            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.localPosition = localPos;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            rt.sizeDelta = rectSize;

            if (fontAsset != null)
            {
                tmp.font = fontAsset;
            }

            tmp.fontStyle = style;
            tmp.fontSize = fontSize;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = minSize;
            tmp.fontSizeMax = maxSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.margin = Vector4.zero;
            tmp.textWrappingMode = allowWrapping ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.richText = true;

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            return tmp;
        }
    }
}
