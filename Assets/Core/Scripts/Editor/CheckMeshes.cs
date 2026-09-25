using UnityEngine;
using UnityEditor;

public class CheckMeshes : EditorWindow
{
    [MenuItem("Tools/Check Meshes")]
    public static void Check()
    {
        string fbxPath = "Assets/Art/Personajes/Gaucho/guacho_base.fbx";
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbx != null)
        {
            Renderer[] renderers = fbx.GetComponentsInChildren<Renderer>();
            Debug.Log("ENCONTRE " + renderers.Length + " RENDERERS EN EL FBX:");
            foreach (var r in renderers)
            {
                Debug.Log("- " + r.gameObject.name);
            }
        }
    }
}
