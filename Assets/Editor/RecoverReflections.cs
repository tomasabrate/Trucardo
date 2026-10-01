using UnityEngine;
using UnityEditor;
using System.IO;

public class RecoverReflections : EditorWindow
{
    [MenuItem("Trucardo/Recuperar: Bajar Reflejos del Bar")]
    public static void LowerReflections()
    {
        string folderPath = "Assets/Models/BarRecursos";
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });
        
        int count = 0;
        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
            {
                // Lower smoothness / glossiness
                if (mat.HasProperty("_Smoothness"))
                {
                    mat.SetFloat("_Smoothness", 0.05f);
                    count++;
                }
                else if (mat.HasProperty("_Glossiness"))
                {
                    mat.SetFloat("_Glossiness", 0.05f);
                    count++;
                }
                EditorUtility.SetDirty(mat);
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"Se ha bajado el reflejo (Smoothness/Glossiness) a 0.05 en {count} materiales de la carpeta {folderPath}.");
    }
}
