using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Trucardo.Core;
using System.IO;

[InitializeOnLoad]
public class SetupBartender
{
    static SetupBartender()
    {
        EditorApplication.delayCall += Setup;
    }

    [MenuItem("Tools/Setup Bartender Model")]
    public static void Setup()
    {
        string fbxPath = "Assets/Models/Personajes/Bartender/Bartender.fbx";
        string texPath = "Assets/Models/Personajes/Bartender/FullRender_MK1.png";
        string matPath = "Assets/Models/Personajes/Bartender/Mat_Bartender.mat";
        string prefabPath = "Assets/Core/Prefabs/BartenderPrefab.prefab";

        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);

        // 1. Configurar ModelImporter
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer != null)
        {
            importer.animationType = ModelImporterAnimationType.None;
            importer.SaveAndReimport();
        }

        // 2. Material
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (mat == null)
        {
            mat = new Material(urpLit != null ? urpLit : Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else if (urpLit != null)
        {
            mat.shader = urpLit;
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
        }
        mat.SetFloat("_Smoothness", 0.1f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        // 3. Crear Prefab de Bartender
        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null)
        {
            Debug.LogError("[SetupBartender] No se pudo cargar Bartender.fbx");
            return;
        }

        GameObject root = new GameObject("BartenderPrefab");

        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.height = 2.275f;
        col.center = new Vector3(0, 1.1375f, 0);
        col.radius = 0.44f;

        GameObject visuals = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
        visuals.name = "Visuals";
        visuals.transform.SetParent(root.transform);
        visuals.transform.localPosition = Vector3.zero;
        visuals.transform.localRotation = Quaternion.identity;
        visuals.transform.localScale = Vector3.one;

        Renderer[] renderers = visuals.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.sharedMaterial = mat;
        }

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log($"[SetupBartender] BartenderPrefab creado con éxito en {prefabPath}");

        // 4. Asignar en GameServer si existe en la escena activa
        GameServer server = Object.FindFirstObjectByType<GameServer>();
        if (server != null && savedPrefab != null)
        {
            server.bartenderPrefab = savedPrefab;
            EditorUtility.SetDirty(server);
            Debug.Log("[SetupBartender] bartenderPrefab asignado al GameServer de la escena.");

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }
        }
    }
}
