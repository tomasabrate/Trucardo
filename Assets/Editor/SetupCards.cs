using UnityEngine;
using UnityEditor;

public class SetupCards
{
    [MenuItem("Tools/Setup Cards Prefab")]
    public static void RunSetup()
    {
        string prefabPath = "Assets/Core/Prefabs/CardPrefab.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        
        // Target sizes
        float canvasWidth = 700f;
        float canvasHeight = canvasWidth * (2296f / 1644f); // ~977.6156
        float meshZ = canvasHeight * 0.0002f; // ~0.195523
        
        // 1. Update Mesh Scale
        Transform meshTransform = prefabRoot.transform.Find("Mesh");
        if (meshTransform != null)
        {
            Vector3 scale = meshTransform.localScale;
            scale.z = meshZ;
            meshTransform.localScale = scale;
        }
        
        // 2. Update CardCanvas Size
        Transform cardCanvas = prefabRoot.transform.Find("CardCanvas");
        if (cardCanvas != null)
        {
            RectTransform rt = cardCanvas.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(canvasWidth, canvasHeight);
        }
        
        // 3. Update BackCanvas Size
        Transform backCanvas = prefabRoot.transform.Find("BackCanvas");
        if (backCanvas != null)
        {
            RectTransform rt = backCanvas.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(canvasWidth, canvasHeight);
        }
        
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        
        Debug.Log("Cards setup completed! Model resized to match image aspect ratio.");
    }
}
