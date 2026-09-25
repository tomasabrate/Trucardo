using UnityEngine;
using UnityEditor;

public class GeneradorBar : EditorWindow
{
    public GameObject prefabMesa;
    public GameObject prefabSilla;

    [MenuItem("Tools/Construir Bar Automático")]
    public static void MostrarVentana()
    {
        GetWindow<GeneradorBar>("Creador de Bar");
    }

    void OnGUI()
    {
        GUILayout.Label("Configuración de Modelos (BAR MEJORADO)", EditorStyles.boldLabel);
        
        prefabMesa = (GameObject)EditorGUILayout.ObjectField("Prefab Mesa", prefabMesa, typeof(GameObject), false);
        prefabSilla = (GameObject)EditorGUILayout.ObjectField("Prefab Silla", prefabSilla, typeof(GameObject), false);

        GUILayout.Space(10);

        if (GUILayout.Button("Construir Bar"))
        {
            if (prefabMesa == null || prefabSilla == null)
            {
                EditorUtility.DisplayDialog("Faltan modelos", "Por favor arrastra los prefabs de Mesa y Silla.", "OK");
                return;
            }
            
            GameObject viejo = GameObject.Find("Escena_Bar_Generada");
            if (viejo != null) DestroyImmediate(viejo);

            ConstruirBar(prefabMesa, prefabSilla);
        }
    }

    public static void ConstruirBar(GameObject mesaPrefab, GameObject sillaPrefab)
    {
        if (!AssetDatabase.IsValidFolder("Assets/BarRecursos"))
            AssetDatabase.CreateFolder("Assets", "BarRecursos");

        GameObject root = new GameObject("Escena_Bar_Generada");

        Material matSuelo = CrearMaterial("Mat_Suelo", new Color(0.25f, 0.15f, 0.08f));
        Material matPared = CrearMaterial("Mat_Pared_General", new Color(0.4f, 0.25f, 0.15f));
        Material matBarra = CrearMaterial("Mat_Barra", new Color(0.2f, 0.1f, 0.05f));
        Material matTecho = CrearMaterial("Mat_Techo", new Color(0.1f, 0.1f, 0.1f)); 

        // SUELO Y TECHO (Techo más alto: Y=4.1)
        CrearCubo(root.transform, "Suelo", new Vector3(0, 0.02f, 0), new Vector3(18, 0.04f, 12), matSuelo);
        CrearCubo(root.transform, "Techo", new Vector3(0, 4.1f, 0), new Vector3(18.2f, 0.2f, 12.2f), matTecho);

        // PAREDES (Altura ajustada a 4 metros)
        GameObject paredOeste = new GameObject("Pared_Izquierda");
        paredOeste.transform.SetParent(root.transform);
        CrearCubo(paredOeste.transform, "Muro_Fondo", new Vector3(-9.1f, 2f, 4f), new Vector3(0.2f, 4f, 4.2f), matPared);
        CrearCubo(paredOeste.transform, "Muro_Frente", new Vector3(-9.1f, 2f, -4f), new Vector3(0.2f, 4f, 4.2f), matPared);
        CrearCubo(paredOeste.transform, "BajoVentana", new Vector3(-9.1f, 0.5f, 0f), new Vector3(0.2f, 1f, 4f), matPared);
        // Hueco para ventana más alto para acomodar el modelo curvo
        CrearCubo(paredOeste.transform, "AltoVentana", new Vector3(-9.1f, 3.25f, 0f), new Vector3(0.2f, 1.5f, 4f), matPared);

        GameObject paredEste = new GameObject("Pared_Derecha");
        paredEste.transform.SetParent(root.transform);
        CrearCubo(paredEste.transform, "Muro_Fondo", new Vector3(9.1f, 2f, 4f), new Vector3(0.2f, 4f, 4.2f), matPared);
        CrearCubo(paredEste.transform, "Muro_Frente", new Vector3(9.1f, 2f, -4f), new Vector3(0.2f, 4f, 4.2f), matPared);
        CrearCubo(paredEste.transform, "BajoVentana", new Vector3(9.1f, 0.5f, 0f), new Vector3(0.2f, 1f, 4f), matPared);
        CrearCubo(paredEste.transform, "AltoVentana", new Vector3(9.1f, 3.25f, 0f), new Vector3(0.2f, 1.5f, 4f), matPared);

        GameObject paredNorte = new GameObject("Pared_Arriba");
        paredNorte.transform.SetParent(root.transform);
        CrearCubo(paredNorte.transform, "Muro_Completo", new Vector3(0, 2f, 6.1f), new Vector3(18.4f, 4f, 0.2f), matPared);

        GameObject paredSur = new GameObject("Pared_Abajo");
        paredSur.transform.SetParent(root.transform);
        CrearCubo(paredSur.transform, "Pilar_Izq", new Vector3(-8f, 2f, -6.1f), new Vector3(2f, 4f, 0.2f), matPared);
        CrearCubo(paredSur.transform, "Pilar_MidIzq", new Vector3(-2.5f, 2f, -6.1f), new Vector3(3f, 4f, 0.2f), matPared);
        CrearCubo(paredSur.transform, "Pilar_MidDer", new Vector3(2.5f, 2f, -6.1f), new Vector3(3f, 4f, 0.2f), matPared);
        CrearCubo(paredSur.transform, "Pilar_Der", new Vector3(8f, 2f, -6.1f), new Vector3(2f, 4f, 0.2f), matPared);
        
        CrearCubo(paredSur.transform, "BajoVentana_Izq", new Vector3(-5.5f, 0.5f, -6.1f), new Vector3(3f, 1f, 0.2f), matPared);
        CrearCubo(paredSur.transform, "AltoVentana_Izq", new Vector3(-5.5f, 3.25f, -6.1f), new Vector3(3f, 1.5f, 0.2f), matPared);
        CrearCubo(paredSur.transform, "Alto_Puerta", new Vector3(0f, 3f, -6.1f), new Vector3(2f, 2f, 0.2f), matPared);
        CrearCubo(paredSur.transform, "BajoVentana_Der", new Vector3(5.5f, 0.5f, -6.1f), new Vector3(3f, 1f, 0.2f), matPared);
        CrearCubo(paredSur.transform, "AltoVentana_Der", new Vector3(5.5f, 3.25f, -6.1f), new Vector3(3f, 1.5f, 0.2f), matPared);

        // LA BARRA (Diseño realista con mostrador, mueble atrás y espacio para NPC)
        GameObject barra = new GameObject("Barra_Bar");
        barra.transform.SetParent(root.transform);
        // Mostrador principal adelante
        CrearCubo(barra.transform, "Mostrador_Principal", new Vector3(0, 0.6f, 3.5f), new Vector3(10f, 1.2f, 1f), matBarra);
        // Mueble de botellas atrás (pegado a la pared norte)
        CrearCubo(barra.transform, "Mueble_Bebidas", new Vector3(0, 1.5f, 5.8f), new Vector3(8f, 3f, 0.6f), matBarra);
        // Cerramientos laterales para la barra
        CrearCubo(barra.transform, "Lateral_Izq", new Vector3(-4.5f, 0.6f, 4.6f), new Vector3(1f, 1.2f, 1.2f), matBarra);
        CrearCubo(barra.transform, "Lateral_Der", new Vector3(4.5f, 0.6f, 4.6f), new Vector3(1f, 1.2f, 1.2f), matBarra);
        
        // Luces de la barra (Cálidas como velas)
        Color luzVelaColor = new Color(1f, 0.65f, 0.25f);
        CrearLuz(barra.transform, "Luz_Barra_1", new Vector3(-3f, 3f, 3.5f), luzVelaColor, 8f, 2f);
        CrearLuz(barra.transform, "Luz_Barra_2", new Vector3(0f, 3f, 3.5f), luzVelaColor, 8f, 2f);
        CrearLuz(barra.transform, "Luz_Barra_3", new Vector3(3f, 3f, 3.5f), luzVelaColor, 8f, 2f);

        // LAS MESAS Y SILLAS
        Vector3[] posicionesDibujo = {
            new Vector3(-6.5f, 0.05f, 2.0f),   
            new Vector3(-6.5f, 0.05f, -2.5f),  
            new Vector3(-2.5f, 0.05f, 2.0f),   
            new Vector3(-2.5f, 0.05f, -2.5f),  
            new Vector3(1.5f, 0.05f, -1.5f),   // MESA PRINCIPAL (Movida a la derecha para no chocar)
            new Vector3(5.5f, 0.05f, 2.0f),    // Derecha Arriba (Movida a la derecha)
            new Vector3(5.5f, 0.05f, -2.5f),   // Derecha Abajo (Movida a la derecha)
        };

        GameObject grupoMesas = new GameObject("Mesas_y_Sillas");
        grupoMesas.transform.SetParent(root.transform);

        for (int i = 0; i < posicionesDibujo.Length; i++)
        {
            Vector3 pos = posicionesDibujo[i];
            
            GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(mesaPrefab);
            m.transform.position = pos;
            m.transform.SetParent(grupoMesas.transform);

            if (i == 4) 
            {
                m.name = "Mesa_Principal";
                m.tag = "Finish";
            }

            GameObject s1 = (GameObject)PrefabUtility.InstantiatePrefab(sillaPrefab);
            s1.transform.position = new Vector3(pos.x - 0.9f, pos.y, pos.z);
            s1.transform.rotation = Quaternion.Euler(0, 90, 0); 
            s1.transform.SetParent(m.transform);

            GameObject s2 = (GameObject)PrefabUtility.InstantiatePrefab(sillaPrefab);
            s2.transform.position = new Vector3(pos.x + 0.9f, pos.y, pos.z);
            s2.transform.rotation = Quaternion.Euler(0, -90, 0); 
            s2.transform.SetParent(m.transform);
            
            // LUCES TODAS TONO CÁLIDO VELA
            CrearLuz(m.transform, "Luz_Vela", new Vector3(0, 1.2f, 0.3f), luzVelaColor, 3f, 3f);
            CrearLuz(m.transform, "Luz_Ambiente", new Vector3(0, 3.2f, -0.3f), luzVelaColor, 10f, 1f);
        }

        Light[] luces = Object.FindObjectsOfType<Light>();
        foreach (Light l in luces)
        {
            if (l.type == LightType.Directional)
            {
                l.transform.rotation = Quaternion.Euler(80f, -30f, 0f); 
                l.intensity = 0.05f; 
                l.color = new Color(0.8f, 0.9f, 1f); 
            }
        }

        Debug.Log("¡Nuevo diseño de Bar generado con ajustes finos!");
    }

    private static Material CrearMaterial(string nombre, Color color)
    {
        string path = $"Assets/BarRecursos/{nombre}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        
        Shader shaderElegido = Shader.Find("Universal Render Pipeline/Lit");
        if (shaderElegido == null) shaderElegido = Shader.Find("Standard");

        if (mat == null)
        {
            mat = new Material(shaderElegido) { color = color };
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = shaderElegido;
            mat.color = color;
        }
        return mat;
    }

    private static GameObject CrearCubo(Transform parent, string nombre, Vector3 pos, Vector3 escala, Material mat)
    {
        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = nombre;
        cubo.transform.SetParent(parent);
        cubo.transform.localPosition = pos;
        cubo.transform.localScale = escala;
        cubo.GetComponent<Renderer>().sharedMaterial = mat;
        return cubo;
    }

    private static void CrearLuz(Transform parent, string nombre, Vector3 posLocal, Color color, float rango, float intensidad)
    {
        GameObject luzObj = new GameObject(nombre);
        luzObj.transform.SetParent(parent);
        luzObj.transform.localPosition = posLocal; 
        Light luz = luzObj.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = color;
        luz.range = rango;
        luz.intensity = intensidad; 
    }
}