using UnityEngine;
using UnityEditor;

public class FixLoopTime : EditorWindow
{
    [MenuItem("Tools/Fix Animations Loop")]
    public static void FixLoop()
    {
        string animFolder = "Assets/Art/Personajes/Animaciones/Core";
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { animFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                if (clips == null || clips.Length == 0)
                {
                    clips = importer.clipAnimations;
                }
                
                if (clips != null && clips.Length > 0)
                {
                    bool changed = false;
                    for (int i = 0; i < clips.Length; i++)
                    {
                        // Queremos que todas hagan loop excepto las transiciones (Stand To Sit, Sit To Stand)
                        if (!path.Contains("To")) 
                        {
                            clips[i].loopTime = true;
                            changed = true;
                        }
                    }
                    if (changed)
                    {
                        importer.clipAnimations = clips;
                        importer.SaveAndReimport();
                        Debug.Log("Loop Time activado en: " + path);
                    }
                }
            }
        }
        Debug.Log("Animaciones puestas en bucle exitosamente!");
    }
}
