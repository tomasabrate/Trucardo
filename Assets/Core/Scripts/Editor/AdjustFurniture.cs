using UnityEngine;
using UnityEditor;

public class AdjustFurniture : EditorWindow
{
    [MenuItem("Tools/Furniture/Make Bigger")]
    public static void AdjustBigger()
    {
        ScaleFurniture(1.5f, 1.2f, 1f, 1.2f, 0.1f);
    }

    [MenuItem("Tools/Furniture/Make Smaller")]
    public static void AdjustSmaller()
    {
        ScaleFurniture(0.8f, 0.8f, 0.8f, 0.8f, -0.1f);
    }

    [MenuItem("Tools/Furniture/Undo Last Adjust")]
    public static void UndoAdjust()
    {
        ScaleFurniture(1f / 1.5f, 1f / 1.2f, 1f, 1f / 1.2f, -0.1f);
    }

    [MenuItem("Tools/Furniture/Lower Table 20cm")]
    public static void LowerTable()
    {
        GameObject mesa = GameObject.Find("Mesa");
        if (mesa == null) mesa = GameObject.Find("Table");
        
        if (mesa != null)
        {
            Undo.RecordObject(mesa.transform, "Lower Table");
            mesa.transform.position -= new Vector3(0, 0.2f, 0);
            Debug.Log("Mesa bajada 20cm.");
        }
    }

    private static void ScaleFurniture(float tableScaleX, float tableScaleY, float tableScaleZ, float chairScale, float tableOffsetY)
    {
        GameObject mesa = GameObject.Find("Mesa");
        if (mesa == null) mesa = GameObject.Find("Table");
        
        if (mesa != null)
        {
            Undo.RecordObject(mesa.transform, "Adjust Furniture Scale");
            Vector3 currentScale = mesa.transform.localScale;
            mesa.transform.localScale = new Vector3(currentScale.x * tableScaleX, currentScale.y * tableScaleY, currentScale.z * tableScaleZ);
            mesa.transform.position += new Vector3(0, tableOffsetY, 0);
        }

        GameObject silla1 = GameObject.Find("Silla1");
        GameObject silla2 = GameObject.Find("Silla2");

        if (silla1 != null)
        {
            Undo.RecordObject(silla1.transform, "Adjust Furniture Scale");
            silla1.transform.localScale = silla1.transform.localScale * chairScale;
        }
        
        if (silla2 != null)
        {
            Undo.RecordObject(silla2.transform, "Adjust Furniture Scale");
            silla2.transform.localScale = silla2.transform.localScale * chairScale;
        }
        
        Debug.Log("Muebles ajustados.");
    }
}
