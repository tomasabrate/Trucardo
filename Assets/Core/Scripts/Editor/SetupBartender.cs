using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Trucardo.Core;

public class BartenderModelPostprocessor : AssetPostprocessor
{
    private void OnPreprocessModel()
    {
        if (assetPath == "Assets/Models/Personajes/Bartender/Bartender.fbx")
        {
            ModelImporter importer = assetImporter as ModelImporter;
            if (importer != null)
            {
                importer.globalScale = 1.0f;
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            }
        }
        else if (assetPath.StartsWith("Assets/Models/Personajes/Bartender/AnimacionesBartender/") && assetPath.EndsWith(".fbx"))
        {
            ModelImporter importer = assetImporter as ModelImporter;
            if (importer != null)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            }
        }
    }

    private void OnPreprocessAnimation()
    {
        if (assetPath.StartsWith("Assets/Models/Personajes/Bartender/AnimacionesBartender/") && assetPath.EndsWith(".fbx"))
        {
            ModelImporter importer = assetImporter as ModelImporter;
            if (importer != null)
            {
                ModelImporterClipAnimation[] clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                {
                    clips = importer.defaultClipAnimations;
                }
                if (clips != null && clips.Length > 0)
                {
                    for (int i = 0; i < clips.Length; i++)
                    {
                        clips[i].loopTime = true;
                        clips[i].keepOriginalPositionY = true;
                        clips[i].keepOriginalOrientation = false;
                        clips[i].keepOriginalPositionXZ = false;
                    }
                    importer.clipAnimations = clips;
                }
            }
        }
    }
}

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
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        string fbxPath = "Assets/Models/Personajes/Bartender/Bartender.fbx";
        string texPath = "Assets/Models/Personajes/Bartender/FullRender_MK1.png";
        string matPath = "Assets/Models/Personajes/Bartender/Mat_Bartender.mat";
        string prefabPath = "Assets/Core/Prefabs/BartenderPrefab.prefab";
        string animCtrlPath = "Assets/Models/Personajes/Bartender/BartenderAnimator.controller";

        string[] animPaths = new string[]
        {
            "Assets/Models/Personajes/Bartender/AnimacionesBartender/Parado.fbx",
            "Assets/Models/Personajes/Bartender/AnimacionesBartender/Walking.fbx",
            "Assets/Models/Personajes/Bartender/AnimacionesBartender/Bartending.fbx"
        };

        // 1. Material
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (mat == null)
        {
            mat = new Material(urpLit != null ? urpLit : Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else if (urpLit != null && mat.shader != urpLit)
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

        // 2. Configurar ModelImporter de Bartender.fbx
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer != null)
        {
            bool reimport = false;
            if (Mathf.Abs(importer.globalScale - 1.0f) > 0.01f)
            {
                importer.globalScale = 1.0f;
                reimport = true;
            }
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                reimport = true;
            }
            if (reimport)
            {
                importer.SaveAndReimport();
            }
        }

        // 3. Configurar clips de animación Humanoid en loop
        foreach (string animPath in animPaths)
        {
            ModelImporter animImporter = AssetImporter.GetAtPath(animPath) as ModelImporter;
            if (animImporter != null)
            {
                bool reimportAnim = false;
                if (animImporter.animationType != ModelImporterAnimationType.Human)
                {
                    animImporter.animationType = ModelImporterAnimationType.Human;
                    animImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    reimportAnim = true;
                }
                ModelImporterClipAnimation[] clips = animImporter.clipAnimations;
                if (clips == null || clips.Length == 0)
                {
                    clips = animImporter.defaultClipAnimations;
                    reimportAnim = true;
                }
                if (clips != null && clips.Length > 0)
                {
                    for (int i = 0; i < clips.Length; i++)
                    {
                        if (!clips[i].loopTime)
                        {
                            clips[i].loopTime = true;
                            reimportAnim = true;
                        }
                    }
                    if (reimportAnim)
                    {
                        animImporter.clipAnimations = clips;
                    }
                }
                if (reimportAnim)
                {
                    animImporter.SaveAndReimport();
                }
            }
        }

        // 4. Crear/Actualizar Prefab de Bartender con el Animator en Visuals (donde reside el Avatar Humanoid)
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
        visuals.transform.SetParent(root.transform, false);
        visuals.transform.localPosition = Vector3.zero;
        visuals.transform.localRotation = Quaternion.identity;
        visuals.transform.localScale = Vector3.one;

        Animator anim = visuals.GetComponent<Animator>();
        if (anim == null)
        {
            anim = visuals.AddComponent<Animator>();
        }
        anim.applyRootMotion = false;

        RuntimeAnimatorController animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animCtrlPath);
        if (animCtrl != null)
        {
            anim.runtimeAnimatorController = animCtrl;
        }

        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object asset in allAssets)
        {
            if (asset is Avatar avatar && avatar.isValid)
            {
                anim.avatar = avatar;
                break;
            }
        }

        BartenderController ctrl = root.AddComponent<BartenderController>();
        SerializedObject so = new SerializedObject(ctrl);
        SerializedProperty propAnim = so.FindProperty("animator");
        if (propAnim != null)
        {
            propAnim.objectReferenceValue = anim;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Renderer[] renderers = visuals.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.sharedMaterial = mat;
        }

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        // 5. Asignar en GameServer si existe en la escena activa
        GameServer server = Object.FindFirstObjectByType<GameServer>();
        if (server != null && savedPrefab != null)
        {
            server.bartenderPrefab = savedPrefab;
            EditorUtility.SetDirty(server);
        }

        Debug.Log($"[SetupBartender] BartenderPrefab y animaciones Humanoid configurados con éxito en {prefabPath}");
    }
}
