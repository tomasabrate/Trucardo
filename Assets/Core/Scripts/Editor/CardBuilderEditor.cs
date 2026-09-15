using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Trucardo.Core;

public class CardBuilderEditor : EditorWindow
{
    [MenuItem("Trucardo/Configurar Cartas Minimalistas")]
    public static void ConfigurarCartas()
    {
        string prefabPath = "Assets/Core/Prefabs/CardPrefab.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("No se encontró el CardPrefab en " + prefabPath);
            return;
        }

        // 1. Configurar Texturas a Sprite
        ConfigurarTextura("Assets/Art/Cartas/cartaEspada.png");
        ConfigurarTextura("Assets/Art/Cartas/cartaOro.png");
        ConfigurarTextura("Assets/Art/Cartas/cartaCopa.png");
        ConfigurarTextura("Assets/Art/Cartas/cartaBasto.png");

        Sprite sprEspada = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Cartas/cartaEspada.png");
        Sprite sprOro = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Cartas/cartaOro.png");
        Sprite sprCopa = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Cartas/cartaCopa.png");
        Sprite sprBasto = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Cartas/cartaBasto.png");

        // 2. Cargar prefab para editar
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);

        // 3. Limpiar viejos TextMesh 3D
        Transform textNumOld = contents.transform.Find("TextNumber");
        if (textNumOld != null) DestroyImmediate(textNumOld.gameObject);
        
        Transform textSuitOld = contents.transform.Find("TextSuit");
        if (textSuitOld != null) DestroyImmediate(textSuitOld.gameObject);

        // 4. Crear Canvas World Space
        Transform canvasTrans = contents.transform.Find("CardCanvas");
        GameObject canvasObj;
        if (canvasTrans != null)
        {
            canvasObj = canvasTrans.gameObject;
        }
        else
        {
            canvasObj = new GameObject("CardCanvas");
            canvasObj.transform.SetParent(contents.transform);
        }
        
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        if (canvas == null) canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        if (canvasObj.GetComponent<CanvasScaler>() == null) canvasObj.AddComponent<CanvasScaler>();
        if (canvasObj.GetComponent<GraphicRaycaster>() == null) canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.localPosition = new Vector3(0, 0.0011f, 0); // Ligeramente arriba de la carta (0.001 en y del cubo achatado)
        // La carta original era Cube achatado en Y. El mesh mide 1x1x1 original. Escala local 0.07, 0.002, 0.125.
        // Eje vertical real de la carta al girarla suele ser Z o X, pero viendo la rotación original de los textos: 
        // m_LocalRotation: {x: 0.7071068, y: 0, z: 0, w: 0.7071068} -> X girado 90 grados.
        canvasRect.localRotation = Quaternion.Euler(90, 0, 0); 
        canvasRect.sizeDelta = new Vector3(700, 1250); // Resolución en pixeles 700x1250
        // Aumentado al 200% (0.0001 -> 0.0002)
        canvasRect.localScale = new Vector3(0.0002f, 0.0002f, 0.0002f); 

        // Escalar la malla (Mesh) al 200%
        Transform meshTrans = contents.transform.Find("Mesh");
        if (meshTrans != null)
        {
            meshTrans.localScale = new Vector3(0.14f, 0.002f, 0.25f);
        }
        
        // 5. Crear Imagen Icono (Centro)
        Transform iconTrans = canvasObj.transform.Find("Icono");
        GameObject iconObj = iconTrans != null ? iconTrans.gameObject : new GameObject("Icono");
        iconObj.transform.SetParent(canvasObj.transform, false);
        Image iconImage = iconObj.GetComponent<Image>();
        if (iconImage == null) iconImage = iconObj.AddComponent<Image>();
        iconImage.preserveAspect = true;
        
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.15f, 0.15f);
        iconRect.anchorMax = new Vector2(0.85f, 0.85f);
        iconRect.offsetMin = Vector2.zero; 
        iconRect.offsetMax = Vector2.zero;

        // 6. Hacer la carta blanca pura (ajustar material)
        MeshRenderer meshRenderer = contents.GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            Material mat = meshRenderer.sharedMaterial;
            mat.shader = Shader.Find("Universal Render Pipeline/Unlit"); 
            if (mat.shader == null) mat.shader = Shader.Find("Unlit/Color"); 
            mat.color = Color.white;
        }

        // 6.b Crear Texto Numero (Arriba Derecha)
        Transform numTrans = canvasObj.transform.Find("Numero");
        GameObject numObj = numTrans != null ? numTrans.gameObject : new GameObject("Numero");
        numObj.transform.SetParent(canvasObj.transform, false);
        Text numText = numObj.GetComponent<Text>();
        if (numText == null) numText = numObj.AddComponent<Text>();
        
        numText.text = "1";
        numText.resizeTextForBestFit = true;
        numText.resizeTextMinSize = 50;
        numText.resizeTextMaxSize = 800;
        numText.alignment = TextAnchor.MiddleCenter;
        numText.color = Color.black;
        numText.fontStyle = FontStyle.Bold;
        
        RectTransform numRect = numObj.GetComponent<RectTransform>();
        numRect.anchorMin = new Vector2(0.7f, 0.7f);
        numRect.anchorMax = new Vector2(1, 1);
        numRect.offsetMin = new Vector2(-20, -20);
        numRect.offsetMax = new Vector2(-20, -20);
        numRect.localEulerAngles = Vector3.zero;

        // 6.c Crear Texto Numero (Abajo Izquierda - Invertido)
        Transform numBottomTrans = canvasObj.transform.Find("NumeroAbajo");
        GameObject numBottomObj = numBottomTrans != null ? numBottomTrans.gameObject : new GameObject("NumeroAbajo");
        numBottomObj.transform.SetParent(canvasObj.transform, false);
        Text numBottomText = numBottomObj.GetComponent<Text>();
        if (numBottomText == null) numBottomText = numBottomObj.AddComponent<Text>();
        
        numBottomText.text = "1";
        numBottomText.resizeTextForBestFit = true;
        numBottomText.resizeTextMinSize = 50;
        numBottomText.resizeTextMaxSize = 800;
        numBottomText.alignment = TextAnchor.MiddleCenter;
        numBottomText.color = Color.black;
        numBottomText.fontStyle = FontStyle.Bold;
        
        RectTransform numBottomRect = numBottomObj.GetComponent<RectTransform>();
        numBottomRect.anchorMin = new Vector2(0, 0);
        numBottomRect.anchorMax = new Vector2(0.3f, 0.3f);
        numBottomRect.offsetMin = new Vector2(20, 20);
        numBottomRect.offsetMax = new Vector2(20, 20);
        numBottomRect.localEulerAngles = new Vector3(0, 0, 180); // Invertido

        // 7. Configurar CardVisual
        CardVisual cardVisual = contents.GetComponent<CardVisual>();
        if (cardVisual != null)
        {
            cardVisual.textNumber = numText;
            cardVisual.textNumberBottom = numBottomText;
            cardVisual.iconImage = iconImage;
            cardVisual.spriteEspada = sprEspada;
            cardVisual.spriteOro = sprOro;
            cardVisual.spriteCopa = sprCopa;
            cardVisual.spriteBasto = sprBasto;
        }

        // 8. Guardar y limpiar
        PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
        
        Debug.Log("¡Cartas minimalistas configuradas con éxito!");
    }

    private static void ConfigurarTextura(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }
    }
}
