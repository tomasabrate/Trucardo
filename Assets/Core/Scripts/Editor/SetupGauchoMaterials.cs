using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Trucardo.Core;

public class GauchoModelPostprocessor : AssetPostprocessor
{
    private void OnPreprocessModel()
    {
        if (assetPath == "Assets/Models/Personajes/Gaucho/guacho_boina.fbx" ||
            assetPath == "Assets/Models/Personajes/Gaucho/guacho_boina_poncho.fbx")
        {
            ModelImporter importer = assetImporter as ModelImporter;
            if (importer != null)
            {
                importer.globalScale = 1.5f;
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            }
        }
    }

    private void OnPostprocessModel(GameObject root)
    {
        if (assetPath == "Assets/Models/Personajes/Gaucho/guacho_boina.fbx")
        {
            Material matBoina1 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Personajes/Gaucho/Texturas/Mat_Gaucho_Boina_1.mat");
            if (matBoina1 != null)
            {
                SetupGauchoMaterials.ApplyMaterialsToVisualHierarchy(root.transform, matBoina1, null);
            }
        }
        else if (assetPath == "Assets/Models/Personajes/Gaucho/guacho_boina_poncho.fbx")
        {
            Material matBoina1 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Personajes/Gaucho/Texturas/Mat_Gaucho_Boina_1.mat");
            Material matPoncho1 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Personajes/Gaucho/Texturas/Mat_Gaucho_Poncho_1.mat");
            if (matBoina1 != null)
            {
                SetupGauchoMaterials.ApplyMaterialsToVisualHierarchy(root.transform, matBoina1, matPoncho1);
            }
        }
    }
}

[InitializeOnLoad]
public class SetupGauchoMaterials
{
    static SetupGauchoMaterials()
    {
        EditorApplication.delayCall += Setup;
    }

    [MenuItem("Tools/Setup Gaucho Materials")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        string texPlayerPath = "Assets/Models/Personajes/Gaucho/Texturas/guacho_texture_1.png";
        string texBotPath = "Assets/Models/Personajes/Gaucho/Texturas/guacho_texture_2.png";
        string texBoina1Path = "Assets/Models/Personajes/Gaucho/Texturas/guacho_boina_texture_1.png";
        string texBoina2Path = "Assets/Models/Personajes/Gaucho/Texturas/guacho_boina_texture_2.png";
        string texPoncho1Path = "Assets/Models/Personajes/Gaucho/Texturas/guacho_poncho_texture_1.png";
        string texPoncho2Path = "Assets/Models/Personajes/Gaucho/Texturas/guacho_poncho_texture_2.png";

        string matPlayerPath = "Assets/Models/Personajes/Gaucho/Texturas/Mat_Gaucho_Player.mat";
        string matBotPath = "Assets/Models/Personajes/Gaucho/Texturas/Mat_Gaucho_Bot.mat";
        string matBoina1Path = "Assets/Models/Personajes/Gaucho/Texturas/Mat_Gaucho_Boina_1.mat";
        string matBoina2Path = "Assets/Models/Personajes/Gaucho/Texturas/Mat_Gaucho_Boina_2.mat";
        string matPoncho1Path = "Assets/Models/Personajes/Gaucho/Texturas/Mat_Gaucho_Poncho_1.mat";
        string matPoncho2Path = "Assets/Models/Personajes/Gaucho/Texturas/Mat_Gaucho_Poncho_2.mat";

        string fbxBasePath = "Assets/Models/Personajes/Gaucho/guacho_base.fbx";
        string fbxBoinaPath = "Assets/Models/Personajes/Gaucho/guacho_boina.fbx";
        string fbxBoinaPonchoPath = "Assets/Models/Personajes/Gaucho/guacho_boina_poncho.fbx";

        string playerPrefabPath = "Assets/Core/Prefabs/PlayerPrefab.prefab";
        string botPrefabPath = "Assets/Core/Prefabs/BotPrefab.prefab";
        string botBoina1PrefabPath = "Assets/Core/Prefabs/BotBoinaPrefab_1.prefab";
        string botBoina2PrefabPath = "Assets/Core/Prefabs/BotBoinaPrefab_2.prefab";
        string botBoinaPoncho1PrefabPath = "Assets/Core/Prefabs/BotBoinaPonchoPrefab_1.prefab";
        string botBoinaPoncho2PrefabPath = "Assets/Core/Prefabs/BotBoinaPonchoPrefab_2.prefab";

        // 1. Importar texturas y crear/actualizar materiales URP Lit
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        Material matPlayer = CreateOrUpdateMaterial(matPlayerPath, texPlayerPath, urpLit);
        Material matBot = CreateOrUpdateMaterial(matBotPath, texBotPath, urpLit);
        Material matBoina1 = CreateOrUpdateMaterial(matBoina1Path, texBoina1Path, urpLit);
        Material matBoina2 = CreateOrUpdateMaterial(matBoina2Path, texBoina2Path, urpLit);
        Material matPoncho1 = CreateOrUpdateMaterial(matPoncho1Path, texPoncho1Path, urpLit);
        Material matPoncho2 = CreateOrUpdateMaterial(matPoncho2Path, texPoncho2Path, urpLit);

        AssetDatabase.SaveAssets();

        // 2. Asegurar configuración Humanoid + escala 1.5 en los FBX nuevos y asignarles sus materiales por defecto
        EnsureHumanoidModelImporter(fbxBoinaPath, matBoina1, null);
        EnsureHumanoidModelImporter(fbxBoinaPonchoPath, matBoina1, matPoncho1);

        // 3. Configurar PlayerPrefab con Mat_Gaucho_Player
        GameObject playerPrefabObj = PrefabUtility.LoadPrefabContents(playerPrefabPath);
        if (playerPrefabObj != null)
        {
            Transform visuals = playerPrefabObj.transform.Find("Visuals");
            if (visuals != null)
            {
                ApplyMaterialsToVisualHierarchy(visuals, matPlayer, null);
            }
            PrefabUtility.SaveAsPrefabAsset(playerPrefabObj, playerPrefabPath);
            PrefabUtility.UnloadPrefabContents(playerPrefabObj);
        }

        // 4. Crear/Actualizar los Prefabs de Bots (Sombrero, Boina 1/2, Boina+Poncho 1/2)
        GameObject savedBotBase = CreateOrUpdateBotVariantPrefab(playerPrefabPath, botPrefabPath, fbxBasePath, matBot, null);
        GameObject savedBotBoina1 = CreateOrUpdateBotVariantPrefab(playerPrefabPath, botBoina1PrefabPath, fbxBoinaPath, matBoina1, null);
        GameObject savedBotBoina2 = CreateOrUpdateBotVariantPrefab(playerPrefabPath, botBoina2PrefabPath, fbxBoinaPath, matBoina2, null);
        GameObject savedBotPoncho1 = CreateOrUpdateBotVariantPrefab(playerPrefabPath, botBoinaPoncho1PrefabPath, fbxBoinaPonchoPath, matBoina1, matPoncho1);
        GameObject savedBotPoncho2 = CreateOrUpdateBotVariantPrefab(playerPrefabPath, botBoinaPoncho2PrefabPath, fbxBoinaPonchoPath, matBoina2, matPoncho2);

        List<GameObject> variantList = new List<GameObject>();
        if (savedBotBase != null) variantList.Add(savedBotBase);
        if (savedBotBoina1 != null) variantList.Add(savedBotBoina1);
        if (savedBotPoncho1 != null) variantList.Add(savedBotPoncho1);
        if (savedBotBoina2 != null) variantList.Add(savedBotBoina2);
        if (savedBotPoncho2 != null) variantList.Add(savedBotPoncho2);

        // 5. Actualizar GameServer.prefab si existe
        string gameServerPrefabPath = "Assets/Core/Prefabs/GameServer.prefab";
        GameObject gsPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(gameServerPrefabPath);
        if (gsPrefabAsset != null)
        {
            GameObject gsContents = PrefabUtility.LoadPrefabContents(gameServerPrefabPath);
            if (gsContents != null)
            {
                GameServer gsComp = gsContents.GetComponent<GameServer>();
                if (gsComp != null)
                {
                    if (savedBotBase != null) gsComp.botPrefab = savedBotBase;
                    gsComp.botVariantPrefabs = new List<GameObject>(variantList);
                    PrefabUtility.SaveAsPrefabAsset(gsContents, gameServerPrefabPath);
                }
                PrefabUtility.UnloadPrefabContents(gsContents);
            }
        }

        // 6. Asignar botPrefab y botVariantPrefabs en el GameServer de la escena activa
        GameServer server = Object.FindFirstObjectByType<GameServer>();
        if (server != null && savedBotBase != null)
        {
            server.botPrefab = savedBotBase;
            server.botVariantPrefabs = new List<GameObject>(variantList);
            EditorUtility.SetDirty(server);

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isLoaded && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }
        }

        Debug.Log("[SetupGauchoMaterials] Materiales y variantes de Gaucho (Sombrero, Boina, Boina+Poncho) configurados con éxito.");
    }

    private static Material CreateOrUpdateMaterial(string matPath, string texPath, Shader shader)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null)
        {
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(shader != null ? shader : Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else if (shader != null && mat.shader != shader)
        {
            mat.shader = shader;
        }

        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
        }
        mat.SetColor("_BaseColor", Color.white);
        mat.SetColor("_Color", Color.white);
        mat.SetFloat("_Smoothness", 0.1f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void EnsureHumanoidModelImporter(string fbxPath, Material defaultBodyAndBeretMat, Material defaultPonchoMat)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        bool needsReimport = false;
        if (Mathf.Abs(importer.globalScale - 1.5f) > 0.001f)
        {
            importer.globalScale = 1.5f;
            needsReimport = true;
        }
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            needsReimport = true;
        }

        if (defaultBodyAndBeretMat != null)
        {
            var map = importer.GetExternalObjectMap();
            var idMat0 = new AssetImporter.SourceAssetIdentifier(typeof(Material), "Material_0.002");
            var idBeret = new AssetImporter.SourceAssetIdentifier(typeof(Material), "Beret_Wool");

            if (!map.ContainsKey(idMat0) || map[idMat0] != defaultBodyAndBeretMat)
            {
                importer.AddRemap(idMat0, defaultBodyAndBeretMat);
                needsReimport = true;
            }
            if (!map.ContainsKey(idBeret) || map[idBeret] != defaultBodyAndBeretMat)
            {
                importer.AddRemap(idBeret, defaultBodyAndBeretMat);
                needsReimport = true;
            }
            if (defaultPonchoMat != null)
            {
                var idPoncho = new AssetImporter.SourceAssetIdentifier(typeof(Material), "Poncho_Wool");
                if (!map.ContainsKey(idPoncho) || map[idPoncho] != defaultPonchoMat)
                {
                    importer.AddRemap(idPoncho, defaultPonchoMat);
                    needsReimport = true;
                }
            }
        }

        if (needsReimport)
        {
            importer.SaveAndReimport();
        }
    }

    private static GameObject CreateOrUpdateBotVariantPrefab(
        string templatePlayerPrefabPath,
        string targetBotPrefabPath,
        string fbxPath,
        Material bodyAndBeretMat,
        Material ponchoMat)
    {
        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null)
        {
            Debug.LogWarning($"[SetupGauchoMaterials] No se encontró el FBX en {fbxPath}");
            return null;
        }

        GameObject rootObj = PrefabUtility.LoadPrefabContents(templatePlayerPrefabPath);
        if (rootObj == null) return null;

        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Core/Prefabs/GauchoAnimator.controller");

        Transform oldVisuals = rootObj.transform.Find("Visuals");
        if (oldVisuals != null)
        {
            if (controller == null)
            {
                Animator oldAnim = oldVisuals.GetComponent<Animator>();
                if (oldAnim != null) controller = oldAnim.runtimeAnimatorController;
            }
            Object.DestroyImmediate(oldVisuals.gameObject);
        }

        GameObject visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
        visualInstance.name = "Visuals";
        visualInstance.transform.SetParent(rootObj.transform, false);
        visualInstance.transform.localPosition = Vector3.zero;
        visualInstance.transform.localRotation = Quaternion.identity;
        visualInstance.transform.localScale = Vector3.one;

        Animator anim = visualInstance.GetComponent<Animator>();
        if (anim == null)
        {
            anim = visualInstance.AddComponent<Animator>();
        }
        anim.applyRootMotion = false;
        if (controller != null)
        {
            anim.runtimeAnimatorController = controller;
        }

        Avatar fbxAvatar = LoadAvatarFromFbx(fbxPath);
        if (fbxAvatar != null)
        {
            anim.avatar = fbxAvatar;
        }

        ApplyMaterialsToVisualHierarchy(visualInstance.transform, bodyAndBeretMat, ponchoMat);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(rootObj, targetBotPrefabPath);
        PrefabUtility.UnloadPrefabContents(rootObj);
        return savedPrefab;
    }

    private static Avatar LoadAvatarFromFbx(string fbxPath)
    {
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object asset in allAssets)
        {
            if (asset is Avatar avatar && avatar.isValid)
            {
                return avatar;
            }
        }
        return null;
    }

    public static void ApplyMaterialsToVisualHierarchy(Transform visualsRoot, Material bodyAndBeretMat, Material ponchoMat)
    {
        if (visualsRoot == null || bodyAndBeretMat == null) return;

        Renderer[] renderers = visualsRoot.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            string rName = r.gameObject.name.ToLowerInvariant();
            if (rName.Contains("poncho"))
            {
                r.sharedMaterial = ponchoMat != null ? ponchoMat : bodyAndBeretMat;
            }
            else if (rName.Contains("body"))
            {
                int slotCount = r.sharedMaterials != null ? Mathf.Max(1, r.sharedMaterials.Length) : 1;
                if (slotCount <= 1)
                {
                    r.sharedMaterial = bodyAndBeretMat;
                }
                else
                {
                    Material[] mats = new Material[slotCount];
                    mats[0] = bodyAndBeretMat;
                    for (int i = 1; i < slotCount; i++)
                    {
                        mats[i] = ponchoMat != null ? ponchoMat : bodyAndBeretMat;
                    }
                    r.sharedMaterials = mats;
                }
            }
            else
            {
                int slotCount = r.sharedMaterials != null ? Mathf.Max(1, r.sharedMaterials.Length) : 1;
                if (slotCount <= 1)
                {
                    r.sharedMaterial = bodyAndBeretMat;
                }
                else
                {
                    Material[] mats = new Material[slotCount];
                    for (int i = 0; i < slotCount; i++)
                    {
                        mats[i] = bodyAndBeretMat;
                    }
                    r.sharedMaterials = mats;
                }
            }
        }
    }
}
