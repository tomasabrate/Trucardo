using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class SetupCards
{
    [MenuItem("Tools/Setup Cards Prefab")]
    public static void RunSetup()
    {
        string prefabPath = "Assets/Core/Prefabs/CardPrefab.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        
        float canvasWidth = 700f;
        float canvasHeight = canvasWidth * (2296f / 1644f);
        float meshZ = canvasHeight * 0.0002f;
        
        Transform meshTransform = prefabRoot.transform.Find("Mesh");
        if (meshTransform != null)
        {
            Vector3 scale = meshTransform.localScale;
            scale.z = meshZ;
            meshTransform.localScale = scale;
            
            MeshFilter mf = meshTransform.GetComponent<MeshFilter>();
            if (mf != null)
            {
                Mesh roundedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Cartas/RoundedCard.obj");
                if (roundedMesh != null) mf.mesh = roundedMesh;
            }
        }
        
        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/WhiteCard.mat");
        if (mat != null)
        {
            Color newColor;
            if (ColorUtility.TryParseHtmlString("#e8dcc4", out newColor))
            {
                mat.color = newColor;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", newColor);
                EditorUtility.SetDirty(mat);
            }
        }
        
        // Ensure Front Canvas and Icono exist
        Transform cardCanvas = prefabRoot.transform.Find("CardCanvas");
        Image frontIconImage = null;
        if (cardCanvas != null)
        {
            RectTransform rt = cardCanvas.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(canvasWidth, canvasHeight);
            
            // Recreate Icono if missing
            Transform icono = cardCanvas.Find("Icono");
            if (icono == null)
            {
                GameObject go = new GameObject("Icono");
                icono = go.transform;
                icono.SetParent(cardCanvas, false);
                
                RectTransform irt = go.AddComponent<RectTransform>();
                irt.anchorMin = Vector2.zero;
                irt.anchorMax = Vector2.one;
                irt.offsetMin = Vector2.zero;
                irt.offsetMax = Vector2.zero;
                
                go.AddComponent<Image>();
            }
            frontIconImage = icono.GetComponent<Image>();
        }
        
        // Ensure Back Canvas and Icono exist
        Transform backCanvas = prefabRoot.transform.Find("BackCanvas");
        if (backCanvas != null)
        {
            RectTransform rt = backCanvas.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(canvasWidth, canvasHeight);
            
            Transform icono = backCanvas.Find("Icono");
            if (icono == null)
            {
                GameObject go = new GameObject("Icono");
                icono = go.transform;
                icono.SetParent(backCanvas, false);
                
                RectTransform irt = go.AddComponent<RectTransform>();
                irt.anchorMin = Vector2.zero;
                irt.anchorMax = Vector2.one;
                irt.offsetMin = Vector2.zero;
                irt.offsetMax = Vector2.zero;
                
                go.AddComponent<Image>();
            }
            
            Image backImage = icono.GetComponent<Image>();
            if (backImage != null)
            {
                Sprite dorso = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Cartas/dorso_carta.jpg");
                if (dorso == null) dorso = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Cartas/dorso_carta.png");
                if (dorso != null) backImage.sprite = dorso;
            }
        }
        
        // Fix CardVisual reference
        Trucardo.Core.CardVisual visual = prefabRoot.GetComponent<Trucardo.Core.CardVisual>();
        if (visual != null && frontIconImage != null)
        {
            visual.iconImage = frontIconImage;
        }
        
        // Remove any old Mask objects just in case
        RemoveMask(cardCanvas);
        RemoveMask(backCanvas);
        
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        
        AssetDatabase.SaveAssets();
        Debug.Log("Cards setup completed! UI Recreated and References restored.");
    }
    
    private static void RemoveMask(Transform canvas)
    {
        if (canvas == null) return;
        Transform maskObj = canvas.Find("Mask");
        if (maskObj != null)
        {
            GameObject.DestroyImmediate(maskObj.gameObject);
        }
    }
}
