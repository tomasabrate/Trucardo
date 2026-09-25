using UnityEngine;
using UnityEditor;
using System.IO;

public class FixAnimations : EditorWindow
{
    [MenuItem("Tools/Fix Animations Rig")]
    public static void FixRig()
    {
        string animFolder = "Assets/Art/Personajes/Animaciones/Core";
        string modelPath = "Assets/Art/Personajes/Gaucho/guacho_base.fbx";

        ModelImporter baseImporter = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (baseImporter == null)
        {
            Debug.LogError("No se encontro guacho_base.fbx");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { animFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                // Use "Create From This Model" para las animaciones, así genera su propio Avatar humanoide
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
                Debug.Log("Arreglado rig de: " + path);
            }
        }
        
        Debug.Log("TODAS LAS ANIMACIONES FUERON CONVERTIDAS A HUMANOIDE EXITOSAMENTE.");
    }
}
