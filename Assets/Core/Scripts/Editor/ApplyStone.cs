using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class ApplyStone
{
    [MenuItem("Tools/Poner Paredes de Piedra")]
    public static void Apply()
    {
        string basePath = "Assets/Models/BarRecursos/";
        
        Material matPared = ApplyToMat(basePath + "Mat_Pared_Norte.mat", "Piedra/Old Stone Wall", "old_stone_wall");
        ApplyToMat(basePath + "Mat_Pared_Sur.mat", "Piedra/Old Stone Wall", "old_stone_wall");
        ApplyToMat(basePath + "Mat_Pared_Este.mat", "Piedra/Old Stone Wall", "old_stone_wall");
        ApplyToMat(basePath + "Mat_Pared_Oeste.mat", "Piedra/Old Stone Wall", "old_stone_wall");
        
        // Crear o cargar un material separado para los pedazos chicos (arriba y abajo de puertas/ventanas)
        // para que podamos ajustar su tamaño (tiling) sin afectar la pared grande.
        string matChicosPath = basePath + "Mat_Pared_Chica.mat";
        Material matChicos = AssetDatabase.LoadAssetAtPath<Material>(matChicosPath);
        if (matChicos == null)
        {
            matChicos = new Material(matPared);
            AssetDatabase.CreateAsset(matChicos, matChicosPath);
        }
        else
        {
            matChicos.CopyPropertiesFromMaterial(matPared);
        }
        
        // Ajustamos un poco el tiling para que coincida mejor. El usuario lo puede afinar luego.
        matChicos.SetTextureScale("_MainTex", new Vector2(0.3f, 0.3f));
        matChicos.SetTextureScale("_BaseMap", new Vector2(0.3f, 0.3f));
        matChicos.SetTextureScale("_BumpMap", new Vector2(0.3f, 0.3f));
        EditorUtility.SetDirty(matChicos);

        // Asignar pared grande
        AssignMaterialTo("Muro", matPared); 
        AssignMaterialTo("Lateral", matPared);    
        AssignMaterialTo("Pilar", matPared); 
        AssignMaterialTo("Techo", matPared); 
        
        // Asignar pared chica
        AssignMaterialTo("Bajo", matChicos); 
        AssignMaterialTo("Alto", matChicos);

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Paredes de piedra aplicadas y material separado para ajustes.");
    }

    private static Material ApplyToMat(string matPath, string folderPath, string filePrefix)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null) return null;

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit != null && mat.shader != urpLit) mat.shader = urpLit;

        string texBasePath = "Assets/Resources/Texturas/" + folderPath + "/" + filePrefix;
        Texture2D diff = AssetDatabase.LoadAssetAtPath<Texture2D>(texBasePath + "_diff_4k.jpg");
        Texture2D norm = AssetDatabase.LoadAssetAtPath<Texture2D>(texBasePath + "_nor_gl_4k.jpg");
        
        if (diff != null) 
        {
            mat.SetTexture("_MainTex", diff);
            mat.SetTexture("_BaseMap", diff); 
        }
        if (norm != null) 
        {
            mat.SetTexture("_BumpMap", norm);
            mat.EnableKeyword("_NORMALMAP");
        }
        
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void AssignMaterialTo(string namePart, Material mat)
    {
        if (mat == null) return;
        GameObject[] objs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject obj in objs)
        {
            if (obj.name.ToLower().Contains(namePart.ToLower()))
            {
                MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>(true);
                foreach (MeshRenderer mr in renderers)
                {
                    Material[] newMats = new Material[mr.sharedMaterials.Length];
                    for(int i=0; i<newMats.Length; i++) newMats[i] = mat;
                    mr.sharedMaterials = newMats;
                    EditorUtility.SetDirty(mr.gameObject);
                }
            }
        }
    }
}