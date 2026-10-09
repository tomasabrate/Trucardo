using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

[InitializeOnLoad]
public static class RestoreFloorTexture
{
    static RestoreFloorTexture()
    {
        EditorApplication.delayCall += Execute;
    }

    [MenuItem("Tools/Restaurar Textura Suelo Bar")]
    public static void Execute()
    {
        // 1. Restaurar propiedades de Mat_Suelo.mat
        string matSueloPath = "Assets/Models/BarRecursos/Mat_Suelo.mat";
        Material matSuelo = AssetDatabase.LoadAssetAtPath<Material>(matSueloPath);

        string diffPath = "Assets/Resources/Texturas/Maderas/Wood Shutter/wood_shutter_diff_4k.jpg";
        string normPath = "Assets/Resources/Texturas/Maderas/Wood Shutter/wood_shutter_nor_gl_4k.jpg";

        Texture2D diffTex = AssetDatabase.LoadAssetAtPath<Texture2D>(diffPath);
        Texture2D normTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normPath);

        if (matSuelo != null)
        {
            if (diffTex != null)
            {
                matSuelo.SetTexture("_BaseMap", diffTex);
                matSuelo.SetTexture("_MainTex", diffTex);
            }
            if (normTex != null)
            {
                matSuelo.SetTexture("_BumpMap", normTex);
                matSuelo.EnableKeyword("_NORMALMAP");
            }

            matSuelo.SetTextureScale("_BaseMap", new Vector2(8f, 8f));
            matSuelo.SetTextureScale("_MainTex", new Vector2(8f, 8f));
            matSuelo.SetTextureScale("_BumpMap", new Vector2(8f, 8f));
            matSuelo.SetFloat("_Smoothness", 0.077f);
            matSuelo.SetColor("_BaseColor", Color.white);
            matSuelo.SetColor("_Color", Color.white);

            EditorUtility.SetDirty(matSuelo);
            AssetDatabase.SaveAssets();
            Debug.Log("[RestoreFloorTexture] Mat_Suelo.mat restaurado exitosamente con Wood Shutter.");
        }

        // 2. Asignar Mat_Suelo al GameObject Suelo del bar
        GameObject sueloObj = GameObject.Find("Escena_Bar_Generada/Suelo");
        if (sueloObj == null)
        {
            sueloObj = GameObject.Find("Suelo");
        }

        if (sueloObj != null && matSuelo != null)
        {
            MeshRenderer mr = sueloObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = matSuelo;
                EditorUtility.SetDirty(sueloObj);
                EditorUtility.SetDirty(mr);
                Debug.Log($"[RestoreFloorTexture] Material Mat_Suelo asignado exitosamente a '{sueloObj.name}'.");
            }
        }

        // 3. Eliminar el archivo de material creado por accidente al arrastrar la textura
        string accidentalMat = "Assets/Resources/Texturas/Maderas/Dark Wooden Planks/Materials/dark_wooden_planks_diff_4k.mat";
        if (File.Exists(accidentalMat))
        {
            AssetDatabase.DeleteAsset(accidentalMat);
            Debug.Log("[RestoreFloorTexture] Material accidental dark_wooden_planks_diff_4k.mat eliminado.");
        }

        string accidentalDir = "Assets/Resources/Texturas/Maderas/Dark Wooden Planks/Materials";
        if (Directory.Exists(accidentalDir))
        {
            string[] files = Directory.GetFiles(accidentalDir);
            if (files.Length == 0)
            {
                AssetDatabase.DeleteAsset(accidentalDir);
                Debug.Log("[RestoreFloorTexture] Carpeta accidental Materials eliminada.");
            }
        }

        // 4. Guardar la escena abierta
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.isLoaded)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log($"[RestoreFloorTexture] Escena '{activeScene.name}' guardada correctamente.");
        }

        AssetDatabase.Refresh();
    }
}
