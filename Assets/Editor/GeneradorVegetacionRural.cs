using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class GeneradorVegetacionRural
{
    private const string Carpeta = "Assets/Models/BarRecursos/Vegetacion";
    private const string MallasCarpeta = Carpeta + "/Mallas";
    private const string MaterialesCarpeta = Carpeta + "/Materiales";
    private const string VersionEditorPrefs = "Trucardo.GeneradorVegetacionRural.Version";
    private const string VersionEscenaPrefs = "Trucardo.GeneradorVegetacionRural.EscenaVersion";
    private const int VersionActual = 2;
    private const int VersionEscenaActual = 1;
    private const float NivelSueloExterior = -0.12f;

    static GeneradorVegetacionRural()
    {
        EditorApplication.delayCall += InicializarVegetacionYEscena;
    }

    private static void InicializarVegetacionYEscena()
    {
        CrearVegetacionFaltante();

        Scene escenaActiva = SceneManager.GetActiveScene();
        if (escenaActiva.IsValid() && escenaActiva.name == "SampleScene")
        {
            bool faltaEnEscena = GameObject.Find("Vegetacion_Entorno") == null;
            bool actualizarEscena = EditorPrefs.GetInt(VersionEscenaPrefs, 0) < VersionEscenaActual;
            if (faltaEnEscena || actualizarEscena)
            {
                DistribuirVegetacionEnEscena();
                EditorPrefs.SetInt(VersionEscenaPrefs, VersionEscenaActual);
            }
        }
    }

    [MenuItem("Trucardo/Crear vegetacion rural faltante")]
    public static void CrearVegetacionFaltante()
    {
        bool actualizar = EditorPrefs.GetInt(VersionEditorPrefs, 0) < VersionActual;
        GenerarVegetacion(actualizar);
        EditorPrefs.SetInt(VersionEditorPrefs, VersionActual);
    }

    [MenuItem("Trucardo/Recrear vegetacion rural (Arboles, Arbustos y Pasto)")]
    public static void RecrearVegetacion()
    {
        GenerarVegetacion(true);
        EditorPrefs.SetInt(VersionEditorPrefs, VersionActual);
    }

    [MenuItem("Trucardo/Distribuir vegetacion en el entorno")]
    public static void DistribuirVegetacionEnEscena()
    {
        GenerarVegetacion(false);

        GameObject arbol01 = AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + "/Arbol_01.prefab");
        GameObject arbol02 = AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + "/Arbol_02.prefab");
        GameObject arbusto01 = AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + "/Arbusto_01.prefab");
        GameObject arbusto02 = AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + "/Arbusto_02.prefab");
        GameObject pasto01 = AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + "/Pasto_01.prefab");
        GameObject pasto02 = AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + "/Pasto_02.prefab");

        if (arbol01 == null || arbol02 == null || arbusto01 == null || arbusto02 == null || pasto01 == null || pasto02 == null)
        {
            Debug.LogError("Faltan prefabs de vegetacion en " + Carpeta);
            return;
        }

        GameObject existente = GameObject.Find("Vegetacion_Entorno");
        if (existente != null)
        {
            Object.DestroyImmediate(existente);
        }

        GameObject contenedor = new GameObject("Vegetacion_Entorno");
        Transform grpPastoInterior = new GameObject("Pastos_Interior_Valla").transform;
        grpPastoInterior.SetParent(contenedor.transform, false);

        Transform grpExteriorMedio = new GameObject("Vegetacion_Exterior_Cercana").transform;
        grpExteriorMedio.SetParent(contenedor.transform, false);

        Transform grpArbolesLejanos = new GameObject("Arboles_Lejanos_Horizonte").transform;
        grpArbolesLejanos.SetParent(contenedor.transform, false);

        // =====================================================================
        // 1. ADENTRO DE LAS VALLAS: SOLO PASTOS (Pasto_01 y Pasto_02)
        // Limites de la valla: X [-16.8, 15.2], Z [-15.5, 17.4]
        // Edificio + Galeria: X [-10.8, 10.8], Z [-10.9, 12.5]
        // Camino de entrada frente al porton: X [-2.5, 1.5], Z [-15.7, -10.5]
        // =====================================================================
        (float x, float z, float rot, float esc, bool tipo2)[] pastosInterior =
        {
            // Patio delantero izquierdo (entre galeria y valla sur/oeste)
            (-4.2f, -14.8f,  15f, 1.05f, false),
            (-4.6f, -14.4f,  85f, 0.90f, true),
            (-7.5f, -14.9f, 140f, 1.10f, true),
            (-9.8f, -13.6f,  42f, 0.95f, false),
            (-12.4f, -14.7f, 210f, 1.15f, false),
            (-13.0f, -14.2f, 305f, 0.88f, true),
            (-15.5f, -14.6f,  65f, 1.08f, true),
            (-14.2f, -11.8f, 175f, 0.96f, false),
            (-6.2f, -11.4f, 250f, 0.85f, true),
            (-9.2f, -11.2f,  12f, 1.00f, false),

            // Patio delantero derecho (entre galeria y valla sur/este)
            ( 3.2f, -14.9f,  55f, 1.00f, true),
            ( 3.7f, -14.4f, 190f, 0.88f, false),
            ( 6.8f, -14.8f, 120f, 1.12f, false),
            ( 9.5f, -13.8f, 275f, 0.95f, true),
            (12.2f, -14.6f,  33f, 1.06f, false),
            (14.3f, -14.5f, 160f, 1.15f, true),
            (13.8f, -11.6f,  92f, 0.92f, false),
            ( 5.4f, -11.3f, 310f, 0.86f, true),
            ( 8.6f, -11.2f, 215f, 1.02f, false),

            // Corredor lateral Oeste (X entre -16.4 y -11.0)
            (-16.1f,  -8.4f,  48f, 1.10f, false),
            (-15.6f,  -7.9f, 165f, 0.90f, true),
            (-11.4f,  -5.2f, 280f, 0.95f, true),
            (-15.8f,  -2.6f, 110f, 1.05f, false),
            (-13.2f,   0.8f,  25f, 0.92f, true),
            (-16.0f,   4.5f, 200f, 1.12f, false),
            (-11.3f,   6.4f, 330f, 0.88f, false),
            (-15.4f,   9.8f,  75f, 1.04f, true),
            (-15.9f,  13.5f, 145f, 1.10f, false),

            // Corredor lateral Este (X entre 11.0 y 14.8)
            (14.5f,  -8.2f, 130f, 1.08f, true),
            (11.3f,  -4.8f,  40f, 0.90f, false),
            (14.6f,  -1.5f, 225f, 1.05f, false),
            (14.1f,  -1.0f, 315f, 0.86f, true),
            (12.8f,   3.2f,  80f, 0.94f, true),
            (14.5f,   7.4f, 195f, 1.12f, false),
            (11.4f,   9.2f, 260f, 0.88f, true),
            (14.3f,  12.8f,  18f, 1.02f, false),

            // Patio trasero Norte (Z entre 12.8 y 17.0)
            (-13.8f,  16.6f,  95f, 1.14f, true),
            ( -9.4f,  16.8f, 210f, 1.02f, false),
            ( -8.9f,  16.3f,  35f, 0.88f, true),
            ( -4.5f,  16.7f, 155f, 1.08f, false),
            ( -1.2f,  13.2f, 290f, 0.90f, true),
            (  2.6f,  16.8f,  60f, 1.05f, false),
            (  7.4f,  16.5f, 180f, 1.12f, true),
            ( 12.6f,  16.7f, 305f, 0.96f, false)
        };

        for (int i = 0; i < pastosInterior.Length; i++)
        {
            var p = pastosInterior[i];
            GameObject prefab = p.tipo2 ? pasto02 : pasto01;
            InstanciarElemento(
                prefab,
                grpPastoInterior,
                (p.tipo2 ? "Pasto_02_Int_" : "Pasto_01_Int_") + (i + 1).ToString("00"),
                new Vector3(p.x, NivelSueloExterior, p.z),
                p.rot,
                p.esc);
        }

        // =====================================================================
        // 2. AFUERA DE LAS VALLAS (CERCANO / MEDIO): ARBOLES, ARBUSTOS Y PASTOS
        // Equilibrado, sin sobrecargar y dejando libre el frente del porton y el molinete
        // =====================================================================
        (float x, float z, float rot, float esc, bool tipo2)[] arbolesMedios =
        {
            // Frente Sur-Oeste y Sur-Este (enmarcando la llegada a la pulperia)
            (-11.5f, -21.5f,  35f, 1.05f, false),
            (-21.0f, -18.5f, 140f, 0.96f, true),
            (  9.8f, -20.8f, 220f, 1.02f, true),
            ( 19.5f, -17.5f,  75f, 1.10f, false),

            // Lado Oeste (alrededor del campo del molinete, respetando Molinete en -21.6, -1.5)
            (-24.5f, -10.2f, 190f, 0.94f, false),
            (-26.0f,   7.5f, 310f, 1.08f, true),
            (-20.5f,  16.2f,  45f, 0.98f, false),

            // Lado Este (visibles desde las ventanas derechas y el patio)
            ( 20.5f,  -8.5f, 115f, 1.04f, true),
            ( 22.8f,   1.8f, 265f, 1.12f, false),
            ( 19.8f,  12.4f,  20f, 0.95f, true),

            // Lado Norte (detras de la pulperia)
            (-12.5f,  23.5f, 165f, 1.06f, true),
            ( -1.5f,  25.0f,  85f, 1.14f, false),
            ( 11.2f,  23.0f, 240f, 0.96f, false),
            ( 20.0f,  21.5f, 330f, 1.02f, true)
        };

        for (int i = 0; i < arbolesMedios.Length; i++)
        {
            var a = arbolesMedios[i];
            GameObject prefab = a.tipo2 ? arbol02 : arbol01;
            InstanciarElemento(
                prefab,
                grpExteriorMedio,
                (a.tipo2 ? "Arbol_02_Med_" : "Arbol_01_Med_") + (i + 1).ToString("00"),
                new Vector3(a.x, NivelSueloExterior, a.z),
                a.rot,
                a.esc);
        }

        (float x, float z, float rot, float esc, bool tipo2)[] arbustosExteriores =
        {
            // Borde exterior de la valla Sur
            ( -6.8f, -17.2f,  25f, 1.00f, false),
            (-14.5f, -17.5f, 145f, 1.12f, true),
            (  5.8f, -17.0f, 210f, 0.95f, true),
            ( 13.5f, -17.4f,  80f, 1.08f, false),

            // Cerca de los arboles medios Sur
            (-13.2f, -20.2f, 190f, 0.92f, true),
            ( 11.4f, -19.6f, 300f, 1.05f, false),
            ( 18.2f, -15.8f,  45f, 0.96f, true),

            // Exterior Oeste y zona del molinete
            (-18.6f, -11.5f, 115f, 1.04f, false),
            (-19.2f,  -4.8f, 250f, 0.90f, true),
            (-18.5f,   4.2f,  60f, 1.10f, false),
            (-24.2f,   5.8f, 175f, 0.95f, true),
            (-18.8f,  13.8f, 320f, 1.02f, true),

            // Exterior Este
            ( 17.2f, -10.4f,  95f, 1.06f, true),
            ( 17.0f,  -3.2f, 205f, 0.94f, false),
            ( 21.2f,   0.4f,  30f, 1.12f, true),
            ( 17.4f,   6.8f, 155f, 0.98f, false),
            ( 18.2f,  14.2f, 275f, 1.05f, false),

            // Exterior Norte
            (-14.2f,  19.2f,  40f, 1.08f, false),
            ( -6.4f,  19.5f, 185f, 0.96f, true),
            (  3.5f,  19.4f, 295f, 1.10f, false),
            ( 12.8f,  19.0f, 125f, 0.92f, true),
            ( -0.2f,  23.4f, 220f, 1.04f, true)
        };

        for (int i = 0; i < arbustosExteriores.Length; i++)
        {
            var b = arbustosExteriores[i];
            GameObject prefab = b.tipo2 ? arbusto02 : arbusto01;
            InstanciarElemento(
                prefab,
                grpExteriorMedio,
                (b.tipo2 ? "Arbusto_02_Ext_" : "Arbusto_01_Ext_") + (i + 1).ToString("00"),
                new Vector3(b.x, NivelSueloExterior, b.z),
                b.rot,
                b.esc);
        }

        (float x, float z, float rot, float esc, bool tipo2)[] pastosExteriores =
        {
            ( -4.2f, -16.8f,  40f, 1.10f, false),
            ( -7.4f, -16.9f, 120f, 0.95f, true),
            (-12.0f, -19.8f, 210f, 1.15f, false),
            (-19.8f, -17.2f,  85f, 1.05f, true),
            (  3.8f, -16.7f, 160f, 1.00f, true),
            (  8.8f, -19.5f, 290f, 1.12f, false),
            ( 15.8f, -16.8f,  15f, 0.96f, true),
            (-20.2f,  -2.2f, 135f, 1.10f, false),
            (-22.8f,  -0.8f, 245f, 0.95f, true),
            (-18.2f,  -8.0f,  50f, 1.04f, true),
            (-18.0f,   1.5f, 190f, 1.08f, false),
            (-18.3f,   9.2f, 310f, 0.92f, true),
            (-24.8f,   8.8f,  75f, 1.15f, false),
            ( 16.6f,  -6.5f, 140f, 1.06f, false),
            ( 16.5f,   2.4f, 260f, 0.94f, true),
            ( 21.8f,   3.0f,  25f, 1.12f, false),
            ( 16.8f,  10.5f, 175f, 1.00f, true),
            ( 19.0f,  13.5f, 330f, 1.08f, false),
            ( -9.5f,  18.8f,  60f, 1.10f, true),
            ( -2.4f,  18.9f, 200f, 0.96f, false),
            (  6.8f,  18.7f, 110f, 1.05f, true),
            ( 14.5f,  18.6f, 280f, 1.12f, false),
            (-11.2f,  22.2f,  35f, 1.08f, false),
            ( 10.4f,  21.8f, 150f, 0.98f, true)
        };

        for (int i = 0; i < pastosExteriores.Length; i++)
        {
            var p = pastosExteriores[i];
            GameObject prefab = p.tipo2 ? pasto02 : pasto01;
            InstanciarElemento(
                prefab,
                grpExteriorMedio,
                (p.tipo2 ? "Pasto_02_Ext_" : "Pasto_01_Ext_") + (i + 1).ToString("00"),
                new Vector3(p.x, NivelSueloExterior, p.z),
                p.rot,
                p.esc);
        }

        // =====================================================================
        // 3. MAS LEJOS EN EL CAMPO: ARBOLES EN EL HORIZONTE (32m a 75m)
        // =====================================================================
        (float x, float z, float rot, float esc, bool tipo2)[] arbolesLejanos =
        {
            // Horizonte Sur
            (-36.0f, -34.0f,  25f, 1.22f, false),
            (-19.5f, -42.0f, 145f, 1.15f, true),
            ( -6.5f, -46.0f, 260f, 1.28f, false),
            ( 12.0f, -43.5f,  80f, 1.18f, true),
            ( 28.5f, -38.0f, 195f, 1.25f, false),
            ( 44.0f, -30.0f, 310f, 1.12f, true),

            // Horizonte Oeste
            (-48.0f, -18.0f,  60f, 1.30f, true),
            (-42.0f,  -4.5f, 175f, 1.16f, false),
            (-52.0f,  12.0f, 290f, 1.26f, false),
            (-38.5f,  26.0f,  40f, 1.18f, true),
            (-46.0f,  38.0f, 130f, 1.24f, false),

            // Horizonte Este
            ( 42.0f, -14.0f, 215f, 1.20f, false),
            ( 49.0f,   3.5f,  95f, 1.28f, true),
            ( 39.5f,  19.0f, 335f, 1.14f, false),
            ( 47.0f,  34.0f, 160f, 1.22f, true),

            // Horizonte Norte
            (-26.0f,  44.0f, 240f, 1.25f, true),
            ( -9.0f,  48.5f,  15f, 1.32f, false),
            (  8.5f,  46.0f, 120f, 1.18f, true),
            ( 24.5f,  43.0f, 275f, 1.24f, false),
            ( 36.0f,  49.0f,  55f, 1.16f, true)
        };

        for (int i = 0; i < arbolesLejanos.Length; i++)
        {
            var a = arbolesLejanos[i];
            GameObject prefab = a.tipo2 ? arbol02 : arbol01;
            InstanciarElemento(
                prefab,
                grpArbolesLejanos,
                (a.tipo2 ? "Arbol_02_Lejano_" : "Arbol_01_Lejano_") + (i + 1).ToString("00"),
                new Vector3(a.x, NivelSueloExterior, a.z),
                a.rot,
                a.esc);
        }

        Scene escena = SceneManager.GetActiveScene();
        if (escena.IsValid() && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }

        Debug.Log("<color=#4CAF50><b>[Vegetacion Rural]</b></color> Entorno poblado con exito: pastos dentro de la valla, vegetacion equilibrada afuera y arboles en el horizonte.");
    }

    private static void InstanciarElemento(
        GameObject prefab, Transform padre, string nombre, Vector3 posicion, float rotacionY, float escala)
    {
        GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
        if (instancia == null)
        {
            instancia = Object.Instantiate(prefab, padre);
        }
        instancia.name = nombre;
        instancia.transform.localPosition = posicion;
        instancia.transform.localRotation = Quaternion.Euler(0f, rotacionY, 0f);
        instancia.transform.localScale = new Vector3(escala, escala, escala);
    }

    private static void GenerarVegetacion(bool recrear)
    {
        AsegurarCarpetas();

        Texture2D texCorteza = CargarOCrearTextura("Albedo_CortezaRural.asset", 19, 0);
        Texture2D normalCorteza = CargarOCrearTextura("Normal_CortezaRural.asset", 37, 1);
        Texture2D roughCorteza = CargarOCrearTextura("Roughness_CortezaRural.asset", 53, 2);

        Texture2D texFollaje = CargarOCrearTextura("Albedo_FollajeRural.asset", 71, 3);
        Texture2D normalFollaje = CargarOCrearTextura("Normal_FollajeRural.asset", 89, 1);
        Texture2D roughFollaje = CargarOCrearTextura("Roughness_FollajeRural.asset", 107, 2);

        if (texCorteza == null || normalCorteza == null || roughCorteza == null ||
            texFollaje == null || normalFollaje == null || roughFollaje == null)
        {
            Debug.LogError("No se pudieron inicializar las texturas de vegetacion rural.");
            return;
        }

        CrearArbol01(texCorteza, normalCorteza, roughCorteza, texFollaje, normalFollaje, roughFollaje, recrear);
        CrearArbol02(texCorteza, normalCorteza, roughCorteza, texFollaje, normalFollaje, roughFollaje, recrear);

        CrearArbusto01(texCorteza, normalCorteza, roughCorteza, texFollaje, normalFollaje, roughFollaje, recrear);
        CrearArbusto02(texCorteza, normalCorteza, roughCorteza, texFollaje, normalFollaje, roughFollaje, recrear);

        CrearPasto01(texFollaje, normalFollaje, roughFollaje, recrear);
        CrearPasto02(texFollaje, normalFollaje, roughFollaje, recrear);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // =========================================================================
    // 1. ARBOL 01: Ombu / Algarrobo Pampeano (Copa ancha, tronco nudoso y raices)
    // =========================================================================
    private static void CrearArbol01(
        Texture2D texCorteza, Texture2D normalCorteza, Texture2D roughCorteza,
        Texture2D texFollaje, Texture2D normalFollaje, Texture2D roughFollaje, bool recrear)
    {
        const string nombre = "Arbol_01";
        string prefabPath = Carpeta + "/" + nombre + ".prefab";
        if (!recrear && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        Material matTronco = CrearMaterial("Mat_Corteza_Arbol_01", new Color(0.31f, 0.22f, 0.15f), texCorteza, normalCorteza, roughCorteza, 0.12f);
        Material matRaices = CrearMaterial("Mat_CortezaOscura_Arbol_01", new Color(0.24f, 0.17f, 0.11f), texCorteza, normalCorteza, roughCorteza, 0.10f);
        Material matCopaOscura = CrearMaterial("Mat_FollajeOscuro_Arbol_01", new Color(0.15f, 0.28f, 0.13f), texFollaje, normalFollaje, roughFollaje, 0.14f);
        Material matCopaMedia = CrearMaterial("Mat_FollajeMedio_Arbol_01", new Color(0.23f, 0.39f, 0.17f), texFollaje, normalFollaje, roughFollaje, 0.15f);
        Material matCopaClara = CrearMaterial("Mat_FollajeClaro_Arbol_01", new Color(0.33f, 0.49f, 0.21f), texFollaje, normalFollaje, roughFollaje, 0.16f);

        GameObject raiz = new GameObject(nombre);

        MeshBuilder mbTronco = new MeshBuilder();
        MeshBuilder mbRaices = new MeshBuilder();
        MeshBuilder mbCopaOscura = new MeshBuilder();
        MeshBuilder mbCopaMedia = new MeshBuilder();
        MeshBuilder mbCopaClara = new MeshBuilder();

        Vector3[] ptsTronco =
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(0.04f, 0.55f, -0.03f),
            new Vector3(-0.05f, 1.15f, 0.04f),
            new Vector3(0.03f, 1.75f, 0.02f),
            new Vector3(0.0f, 2.25f, -0.02f)
        };
        float[] radTronco = { 0.44f, 0.35f, 0.30f, 0.26f, 0.18f };
        mbTronco.AddLowPolyTube(ptsTronco, radTronco, 7, 101, true, true);

        for (int i = 0; i < 5; i++)
        {
            float ang = (i * 72f + (i % 2) * 14f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            Vector3 perp = new Vector3(-dir.z, 0f, dir.x);
            Vector3[] ptsRaiz =
            {
                new Vector3(dir.x * 0.20f, 0.48f, dir.z * 0.20f),
                new Vector3(dir.x * 0.48f, 0.22f, dir.z * 0.48f) + perp * 0.06f,
                new Vector3(dir.x * 0.82f, 0.06f, dir.z * 0.82f) - perp * 0.05f,
                new Vector3(dir.x * 1.05f, -0.02f, dir.z * 1.05f)
            };
            float[] radRaiz = { 0.18f, 0.14f, 0.09f, 0.03f };
            mbRaices.AddLowPolyTube(ptsRaiz, radRaiz, 5, 200 + i * 17, false, true);
        }

        Vector3[][] ramasPrincipales =
        {
            new[] { new Vector3(0.02f, 1.65f, 0.01f), new Vector3(0.55f, 2.15f, 0.35f), new Vector3(1.15f, 2.55f, 0.62f), new Vector3(1.55f, 2.78f, 0.75f) },
            new[] { new Vector3(-0.03f, 1.70f, 0.02f), new Vector3(-0.62f, 2.20f, 0.28f), new Vector3(-1.22f, 2.62f, 0.50f), new Vector3(-1.60f, 2.85f, 0.58f) },
            new[] { new Vector3(0.01f, 1.78f, -0.03f), new Vector3(0.28f, 2.28f, -0.60f), new Vector3(0.52f, 2.68f, -1.18f), new Vector3(0.68f, 2.92f, -1.52f) },
            new[] { new Vector3(-0.02f, 1.82f, -0.02f), new Vector3(-0.48f, 2.35f, -0.52f), new Vector3(-0.95f, 2.75f, -0.98f), new Vector3(-1.25f, 3.00f, -1.25f) },
            new[] { new Vector3(0.0f, 2.10f, 0.0f), new Vector3(0.12f, 2.75f, 0.08f), new Vector3(0.05f, 3.35f, -0.04f) }
        };
        float[][] radiosRamas =
        {
            new[] { 0.20f, 0.15f, 0.11f, 0.06f },
            new[] { 0.21f, 0.15f, 0.11f, 0.06f },
            new[] { 0.19f, 0.14f, 0.10f, 0.05f },
            new[] { 0.18f, 0.13f, 0.09f, 0.05f },
            new[] { 0.18f, 0.13f, 0.07f }
        };

        for (int r = 0; r < ramasPrincipales.Length; r++)
        {
            mbTronco.AddLowPolyTube(ramasPrincipales[r], radiosRamas[r], 6, 300 + r * 23, false, true);
        }

        mbCopaOscura.AddLowPolyFoliageBlob(new Vector3(1.45f, 2.65f, 0.72f), new Vector3(1.10f, 0.62f, 1.05f), 2, 401);
        mbCopaOscura.AddLowPolyFoliageBlob(new Vector3(-1.50f, 2.72f, 0.55f), new Vector3(1.15f, 0.65f, 1.00f), 2, 402);
        mbCopaOscura.AddLowPolyFoliageBlob(new Vector3(0.62f, 2.75f, -1.42f), new Vector3(1.08f, 0.60f, 1.02f), 2, 403);
        mbCopaOscura.AddLowPolyFoliageBlob(new Vector3(-1.18f, 2.82f, -1.15f), new Vector3(1.05f, 0.62f, 1.05f), 2, 404);
        mbCopaOscura.AddLowPolyFoliageBlob(new Vector3(0.0f, 2.68f, 0.05f), new Vector3(1.35f, 0.65f, 1.30f), 2, 405);
        mbCopaOscura.AddLowPolyFoliageBlob(new Vector3(0.15f, 2.70f, 1.25f), new Vector3(0.95f, 0.56f, 0.90f), 2, 406);

        mbCopaMedia.AddLowPolyFoliageBlob(new Vector3(1.05f, 3.15f, 0.55f), new Vector3(1.25f, 0.75f, 1.20f), 2, 501);
        mbCopaMedia.AddLowPolyFoliageBlob(new Vector3(-1.10f, 3.20f, 0.45f), new Vector3(1.28f, 0.78f, 1.18f), 2, 502);
        mbCopaMedia.AddLowPolyFoliageBlob(new Vector3(0.45f, 3.22f, -1.05f), new Vector3(1.20f, 0.74f, 1.15f), 2, 503);
        mbCopaMedia.AddLowPolyFoliageBlob(new Vector3(-0.85f, 3.25f, -0.85f), new Vector3(1.18f, 0.72f, 1.12f), 2, 504);
        mbCopaMedia.AddLowPolyFoliageBlob(new Vector3(0.05f, 3.35f, 0.0f), new Vector3(1.45f, 0.82f, 1.40f), 2, 505);
        mbCopaMedia.AddLowPolyFoliageBlob(new Vector3(1.25f, 3.05f, -0.45f), new Vector3(0.95f, 0.62f, 0.92f), 2, 506);
        mbCopaMedia.AddLowPolyFoliageBlob(new Vector3(-0.20f, 3.12f, 1.10f), new Vector3(1.05f, 0.66f, 0.98f), 2, 507);

        mbCopaClara.AddLowPolyFoliageBlob(new Vector3(0.0f, 3.92f, 0.02f), new Vector3(1.30f, 0.75f, 1.25f), 2, 601);
        mbCopaClara.AddLowPolyFoliageBlob(new Vector3(0.72f, 3.72f, 0.38f), new Vector3(1.00f, 0.65f, 0.96f), 2, 602);
        mbCopaClara.AddLowPolyFoliageBlob(new Vector3(-0.75f, 3.75f, 0.30f), new Vector3(1.02f, 0.66f, 0.98f), 2, 603);
        mbCopaClara.AddLowPolyFoliageBlob(new Vector3(0.15f, 3.70f, -0.68f), new Vector3(0.98f, 0.64f, 0.94f), 2, 604);
        mbCopaClara.AddLowPolyFoliageBlob(new Vector3(-0.48f, 3.68f, -0.55f), new Vector3(0.92f, 0.60f, 0.90f), 2, 605);

        CrearHijoMalla(raiz.transform, "Tronco_Y_Ramas", GuardarMalla(mbTronco.ToMesh(nombre + "_Tronco"), nombre + "_Tronco.asset"), matTronco);
        CrearHijoMalla(raiz.transform, "Raices_Base", GuardarMalla(mbRaices.ToMesh(nombre + "_Raices"), nombre + "_Raices.asset"), matRaices);
        CrearHijoMalla(raiz.transform, "Copa_Oscura", GuardarMalla(mbCopaOscura.ToMesh(nombre + "_CopaOscura"), nombre + "_CopaOscura.asset"), matCopaOscura);
        CrearHijoMalla(raiz.transform, "Copa_Media", GuardarMalla(mbCopaMedia.ToMesh(nombre + "_CopaMedia"), nombre + "_CopaMedia.asset"), matCopaMedia);
        CrearHijoMalla(raiz.transform, "Copa_Clara", GuardarMalla(mbCopaClara.ToMesh(nombre + "_CopaClara"), nombre + "_CopaClara.asset"), matCopaClara);

        CapsuleCollider col = raiz.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 1.5f, 0f);
        col.radius = 0.38f;
        col.height = 3.0f;

        GuardarPrefab(raiz, prefabPath);
    }

    // =========================================================================
    // 2. ARBOL 02: Sauce Criollo / Tala Pampeano (Tronco bifurcado, copa estilizada)
    // =========================================================================
    private static void CrearArbol02(
        Texture2D texCorteza, Texture2D normalCorteza, Texture2D roughCorteza,
        Texture2D texFollaje, Texture2D normalFollaje, Texture2D roughFollaje, bool recrear)
    {
        const string nombre = "Arbol_02";
        string prefabPath = Carpeta + "/" + nombre + ".prefab";
        if (!recrear && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        Material matTronco = CrearMaterial("Mat_Corteza_Arbol_02", new Color(0.36f, 0.28f, 0.21f), texCorteza, normalCorteza, roughCorteza, 0.12f);
        Material matCopaOscura = CrearMaterial("Mat_FollajeOscuro_Arbol_02", new Color(0.18f, 0.30f, 0.17f), texFollaje, normalFollaje, roughFollaje, 0.14f);
        Material matCopaMedia = CrearMaterial("Mat_FollajeMedio_Arbol_02", new Color(0.27f, 0.42f, 0.23f), texFollaje, normalFollaje, roughFollaje, 0.15f);
        Material matCopaClara = CrearMaterial("Mat_FollajeClaro_Arbol_02", new Color(0.39f, 0.49f, 0.25f), texFollaje, normalFollaje, roughFollaje, 0.16f);

        GameObject raiz = new GameObject(nombre);

        MeshBuilder mbTronco = new MeshBuilder();
        MeshBuilder mbCopaOscura = new MeshBuilder();
        MeshBuilder mbCopaMedia = new MeshBuilder();
        MeshBuilder mbCopaClara = new MeshBuilder();

        Vector3[] ptsBase =
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(0.05f, 0.65f, 0.02f),
            new Vector3(0.10f, 1.35f, -0.02f),
            new Vector3(0.28f, 2.25f, 0.12f),
            new Vector3(0.52f, 3.15f, 0.25f),
            new Vector3(0.65f, 3.85f, 0.32f)
        };
        float[] radBase = { 0.34f, 0.27f, 0.23f, 0.17f, 0.12f, 0.06f };
        mbTronco.AddLowPolyTube(ptsBase, radBase, 6, 701, true, true);

        Vector3[] ptsSecundario =
        {
            new Vector3(0.08f, 1.25f, -0.02f),
            new Vector3(-0.28f, 2.05f, -0.18f),
            new Vector3(-0.62f, 2.82f, -0.32f),
            new Vector3(-0.82f, 3.45f, -0.40f)
        };
        float[] radSecundario = { 0.20f, 0.15f, 0.11f, 0.05f };
        mbTronco.AddLowPolyTube(ptsSecundario, radSecundario, 6, 702, false, true);

        mbTronco.AddLowPolyTube(
            new[] { new Vector3(0.24f, 2.10f, 0.10f), new Vector3(0.85f, 2.65f, -0.35f), new Vector3(1.22f, 3.05f, -0.58f) },
            new[] { 0.13f, 0.09f, 0.04f }, 5, 703, false, true);

        mbTronco.AddLowPolyTube(
            new[] { new Vector3(-0.25f, 1.95f, -0.15f), new Vector3(-0.35f, 2.55f, 0.55f), new Vector3(-0.48f, 2.98f, 0.95f) },
            new[] { 0.12f, 0.08f, 0.04f }, 5, 704, false, true);

        for (int i = 0; i < 4; i++)
        {
            float ang = (i * 90f + 25f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            mbTronco.AddLowPolyTube(
                new[] { new Vector3(dir.x * 0.14f, 0.35f, dir.z * 0.14f), new Vector3(dir.x * 0.42f, 0.12f, dir.z * 0.42f), new Vector3(dir.x * 0.62f, -0.02f, dir.z * 0.62f) },
                new[] { 0.13f, 0.09f, 0.03f }, 5, 710 + i, false, true);
        }

        mbCopaOscura.AddLowPolyFoliageBlob(new Vector3(0.62f, 3.55f, 0.30f), new Vector3(1.05f, 0.72f, 1.00f), 2, 801);
        mbCopaOscura.AddLowPolyFoliageBlob(new Vector3(-0.78f, 3.25f, -0.38f), new Vector3(0.98f, 0.68f, 0.95f), 2, 802);
        mbCopaOscura.AddLowPolyFoliageBlob(new Vector3(1.18f, 2.90f, -0.55f), new Vector3(0.85f, 0.60f, 0.82f), 2, 803);
        mbCopaOscura.AddLowPolyFoliageBlob(new Vector3(-0.45f, 2.85f, 0.88f), new Vector3(0.88f, 0.62f, 0.85f), 2, 804);
        mbCopaOscura.AddLowPolyFoliageBlob(new Vector3(0.05f, 3.15f, -0.05f), new Vector3(1.10f, 0.70f, 1.05f), 2, 805);

        mbCopaMedia.AddLowPolyFoliageBlob(new Vector3(0.55f, 4.10f, 0.25f), new Vector3(1.18f, 0.82f, 1.12f), 2, 811);
        mbCopaMedia.AddLowPolyFoliageBlob(new Vector3(-0.68f, 3.75f, -0.32f), new Vector3(1.08f, 0.76f, 1.02f), 2, 812);
        mbCopaMedia.AddLowPolyFoliageBlob(new Vector3(1.02f, 3.35f, -0.45f), new Vector3(0.92f, 0.68f, 0.90f), 2, 813);
        mbCopaMedia.AddLowPolyFoliageBlob(new Vector3(-0.35f, 3.32f, 0.75f), new Vector3(0.95f, 0.68f, 0.92f), 2, 814);
        mbCopaMedia.AddLowPolyFoliageBlob(new Vector3(0.02f, 3.78f, 0.02f), new Vector3(1.22f, 0.80f, 1.15f), 2, 815);

        mbCopaClara.AddLowPolyFoliageBlob(new Vector3(0.38f, 4.68f, 0.15f), new Vector3(1.02f, 0.72f, 0.98f), 2, 821);
        mbCopaClara.AddLowPolyFoliageBlob(new Vector3(-0.45f, 4.25f, -0.20f), new Vector3(0.90f, 0.66f, 0.88f), 2, 822);
        mbCopaClara.AddLowPolyFoliageBlob(new Vector3(0.05f, 4.42f, -0.08f), new Vector3(0.95f, 0.68f, 0.92f), 2, 823);
        mbCopaClara.AddLowPolyFoliageBlob(new Vector3(0.85f, 3.82f, -0.30f), new Vector3(0.75f, 0.55f, 0.72f), 2, 824);

        CrearHijoMalla(raiz.transform, "Tronco_Y_Ramas", GuardarMalla(mbTronco.ToMesh(nombre + "_Tronco"), nombre + "_Tronco.asset"), matTronco);
        CrearHijoMalla(raiz.transform, "Copa_Oscura", GuardarMalla(mbCopaOscura.ToMesh(nombre + "_CopaOscura"), nombre + "_CopaOscura.asset"), matCopaOscura);
        CrearHijoMalla(raiz.transform, "Copa_Media", GuardarMalla(mbCopaMedia.ToMesh(nombre + "_CopaMedia"), nombre + "_CopaMedia.asset"), matCopaMedia);
        CrearHijoMalla(raiz.transform, "Copa_Clara", GuardarMalla(mbCopaClara.ToMesh(nombre + "_CopaClara"), nombre + "_CopaClara.asset"), matCopaClara);

        CapsuleCollider col = raiz.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0.05f, 1.5f, 0f);
        col.radius = 0.30f;
        col.height = 3.0f;

        GuardarPrefab(raiz, prefabPath);
    }

    // =========================================================================
    // 3. ARBUSTO 01: Espinillo / Chilca Pampeana (Redondeado tupido con brotes)
    // =========================================================================
    private static void CrearArbusto01(
        Texture2D texCorteza, Texture2D normalCorteza, Texture2D roughCorteza,
        Texture2D texFollaje, Texture2D normalFollaje, Texture2D roughFollaje, bool recrear)
    {
        const string nombre = "Arbusto_01";
        string prefabPath = Carpeta + "/" + nombre + ".prefab";
        if (!recrear && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        Material matRamas = CrearMaterial("Mat_Ramas_Arbusto_01", new Color(0.32f, 0.23f, 0.15f), texCorteza, normalCorteza, roughCorteza, 0.12f);
        Material matOscuro = CrearMaterial("Mat_FollajeOscuro_Arbusto_01", new Color(0.16f, 0.29f, 0.14f), texFollaje, normalFollaje, roughFollaje, 0.14f);
        Material matMedio = CrearMaterial("Mat_FollajeMedio_Arbusto_01", new Color(0.25f, 0.42f, 0.18f), texFollaje, normalFollaje, roughFollaje, 0.15f);
        Material matBrote = CrearMaterial("Mat_FollajeBrote_Arbusto_01", new Color(0.45f, 0.53f, 0.21f), texFollaje, normalFollaje, roughFollaje, 0.16f);

        GameObject raiz = new GameObject(nombre);

        MeshBuilder mbRamas = new MeshBuilder();
        MeshBuilder mbOscuro = new MeshBuilder();
        MeshBuilder mbMedio = new MeshBuilder();
        MeshBuilder mbBrote = new MeshBuilder();

        for (int i = 0; i < 6; i++)
        {
            float ang = (i * 60f + 15f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            mbRamas.AddLowPolyTube(
                new[]
                {
                    new Vector3(dir.x * 0.04f, 0f, dir.z * 0.04f),
                    new Vector3(dir.x * 0.18f, 0.18f, dir.z * 0.18f),
                    new Vector3(dir.x * 0.34f, 0.36f, dir.z * 0.34f)
                },
                new[] { 0.045f, 0.032f, 0.018f }, 5, 900 + i, false, true);
        }

        mbOscuro.AddLowPolyFoliageBlob(new Vector3(0.0f, 0.28f, 0.0f), new Vector3(0.46f, 0.28f, 0.44f), 2, 911);
        mbOscuro.AddLowPolyFoliageBlob(new Vector3(0.28f, 0.24f, 0.18f), new Vector3(0.34f, 0.24f, 0.32f), 2, 912);
        mbOscuro.AddLowPolyFoliageBlob(new Vector3(-0.30f, 0.25f, 0.14f), new Vector3(0.35f, 0.24f, 0.33f), 2, 913);
        mbOscuro.AddLowPolyFoliageBlob(new Vector3(0.05f, 0.24f, -0.28f), new Vector3(0.36f, 0.23f, 0.34f), 2, 914);

        mbMedio.AddLowPolyFoliageBlob(new Vector3(0.02f, 0.46f, 0.02f), new Vector3(0.44f, 0.32f, 0.42f), 2, 921);
        mbMedio.AddLowPolyFoliageBlob(new Vector3(0.24f, 0.40f, -0.14f), new Vector3(0.32f, 0.26f, 0.30f), 2, 922);
        mbMedio.AddLowPolyFoliageBlob(new Vector3(-0.22f, 0.42f, -0.16f), new Vector3(0.33f, 0.27f, 0.31f), 2, 923);
        mbMedio.AddLowPolyFoliageBlob(new Vector3(-0.12f, 0.38f, 0.25f), new Vector3(0.31f, 0.25f, 0.30f), 2, 924);

        mbBrote.AddLowPolyFoliageBlob(new Vector3(0.0f, 0.64f, 0.0f), new Vector3(0.30f, 0.22f, 0.28f), 1, 931);
        mbBrote.AddLowPolyFoliageBlob(new Vector3(0.18f, 0.56f, 0.12f), new Vector3(0.22f, 0.18f, 0.22f), 1, 932);
        mbBrote.AddLowPolyFoliageBlob(new Vector3(-0.16f, 0.55f, -0.08f), new Vector3(0.23f, 0.18f, 0.21f), 1, 933);

        CrearHijoMalla(raiz.transform, "Ramas_Base", GuardarMalla(mbRamas.ToMesh(nombre + "_Ramas"), nombre + "_Ramas.asset"), matRamas);
        CrearHijoMalla(raiz.transform, "Follaje_Oscuro", GuardarMalla(mbOscuro.ToMesh(nombre + "_Oscuro"), nombre + "_Oscuro.asset"), matOscuro);
        CrearHijoMalla(raiz.transform, "Follaje_Medio", GuardarMalla(mbMedio.ToMesh(nombre + "_Medio"), nombre + "_Medio.asset"), matMedio);
        CrearHijoMalla(raiz.transform, "Brotes_Claros", GuardarMalla(mbBrote.ToMesh(nombre + "_Brotes"), nombre + "_Brotes.asset"), matBrote);

        GuardarPrefab(raiz, prefabPath);
    }

    // =========================================================================
    // 4. ARBUSTO 02: Matorral / Jarilla Criolla Baja (Ancho, achaparrado y seco)
    // =========================================================================
    private static void CrearArbusto02(
        Texture2D texCorteza, Texture2D normalCorteza, Texture2D roughCorteza,
        Texture2D texFollaje, Texture2D normalFollaje, Texture2D roughFollaje, bool recrear)
    {
        const string nombre = "Arbusto_02";
        string prefabPath = Carpeta + "/" + nombre + ".prefab";
        if (!recrear && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        Material matRamas = CrearMaterial("Mat_Ramas_Arbusto_02", new Color(0.35f, 0.25f, 0.16f), texCorteza, normalCorteza, roughCorteza, 0.11f);
        Material matOscuro = CrearMaterial("Mat_FollajeOscuro_Arbusto_02", new Color(0.21f, 0.28f, 0.15f), texFollaje, normalFollaje, roughFollaje, 0.13f);
        Material matMedio = CrearMaterial("Mat_FollajeMedio_Arbusto_02", new Color(0.32f, 0.40f, 0.20f), texFollaje, normalFollaje, roughFollaje, 0.14f);
        Material matSeco = CrearMaterial("Mat_FollajeSeco_Arbusto_02", new Color(0.48f, 0.44f, 0.23f), texFollaje, normalFollaje, roughFollaje, 0.14f);

        GameObject raiz = new GameObject(nombre);

        MeshBuilder mbRamas = new MeshBuilder();
        MeshBuilder mbOscuro = new MeshBuilder();
        MeshBuilder mbMedio = new MeshBuilder();
        MeshBuilder mbSeco = new MeshBuilder();

        for (int i = 0; i < 8; i++)
        {
            float ang = (i * 45f + (i % 2) * 12f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            float largo = (i % 2 == 0) ? 0.58f : 0.44f;
            float alto = (i % 2 == 0) ? 0.42f : 0.30f;
            mbRamas.AddLowPolyTube(
                new[]
                {
                    new Vector3(dir.x * 0.05f, 0f, dir.z * 0.05f),
                    new Vector3(dir.x * (largo * 0.55f), alto * 0.55f, dir.z * (largo * 0.55f)),
                    new Vector3(dir.x * largo, alto, dir.z * largo)
                },
                new[] { 0.040f, 0.025f, 0.008f }, 5, 1000 + i, false, true);
        }

        mbOscuro.AddLowPolyFoliageBlob(new Vector3(-0.26f, 0.20f, 0.12f), new Vector3(0.38f, 0.20f, 0.34f), 2, 1011);
        mbOscuro.AddLowPolyFoliageBlob(new Vector3(0.28f, 0.19f, -0.10f), new Vector3(0.36f, 0.19f, 0.35f), 2, 1012);
        mbOscuro.AddLowPolyFoliageBlob(new Vector3(0.02f, 0.18f, 0.28f), new Vector3(0.34f, 0.18f, 0.30f), 2, 1013);
        mbOscuro.AddLowPolyFoliageBlob(new Vector3(-0.04f, 0.18f, -0.26f), new Vector3(0.35f, 0.18f, 0.32f), 2, 1014);

        mbMedio.AddLowPolyFoliageBlob(new Vector3(0.0f, 0.32f, 0.0f), new Vector3(0.42f, 0.25f, 0.38f), 2, 1021);
        mbMedio.AddLowPolyFoliageBlob(new Vector3(-0.34f, 0.29f, -0.08f), new Vector3(0.32f, 0.22f, 0.30f), 2, 1022);
        mbMedio.AddLowPolyFoliageBlob(new Vector3(0.32f, 0.30f, 0.16f), new Vector3(0.33f, 0.22f, 0.29f), 2, 1023);
        mbMedio.AddLowPolyFoliageBlob(new Vector3(0.10f, 0.28f, -0.28f), new Vector3(0.28f, 0.20f, 0.27f), 1, 1024);

        mbSeco.AddLowPolyFoliageBlob(new Vector3(-0.14f, 0.44f, 0.08f), new Vector3(0.26f, 0.18f, 0.24f), 1, 1031);
        mbSeco.AddLowPolyFoliageBlob(new Vector3(0.18f, 0.42f, -0.06f), new Vector3(0.25f, 0.17f, 0.24f), 1, 1032);
        mbSeco.AddLowPolyFoliageBlob(new Vector3(-0.38f, 0.35f, 0.18f), new Vector3(0.20f, 0.15f, 0.19f), 1, 1033);
        mbSeco.AddLowPolyFoliageBlob(new Vector3(0.36f, 0.34f, -0.20f), new Vector3(0.21f, 0.15f, 0.20f), 1, 1034);

        CrearHijoMalla(raiz.transform, "Ramas_Secas", GuardarMalla(mbRamas.ToMesh(nombre + "_Ramas"), nombre + "_Ramas.asset"), matRamas);
        CrearHijoMalla(raiz.transform, "Follaje_Oscuro", GuardarMalla(mbOscuro.ToMesh(nombre + "_Oscuro"), nombre + "_Oscuro.asset"), matOscuro);
        CrearHijoMalla(raiz.transform, "Follaje_Medio", GuardarMalla(mbMedio.ToMesh(nombre + "_Medio"), nombre + "_Medio.asset"), matMedio);
        CrearHijoMalla(raiz.transform, "Follaje_Seco", GuardarMalla(mbSeco.ToMesh(nombre + "_Seco"), nombre + "_Seco.asset"), matSeco);

        GuardarPrefab(raiz, prefabPath);
    }

    // =========================================================================
    // 5. PASTO 01: Mata de Pasto Pampeano (Manojo de briznas 3D que salen del suelo)
    // =========================================================================
    private static void CrearPasto01(
        Texture2D texFollaje, Texture2D normalFollaje, Texture2D roughFollaje, bool recrear)
    {
        const string nombre = "Pasto_01";
        string prefabPath = Carpeta + "/" + nombre + ".prefab";
        if (!recrear && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        Material matOscuro = CrearMaterial("Mat_BriznasOscuras_Pasto_01", new Color(0.18f, 0.33f, 0.14f), texFollaje, normalFollaje, roughFollaje, 0.15f);
        Material matMedio = CrearMaterial("Mat_BriznasMedias_Pasto_01", new Color(0.27f, 0.45f, 0.19f), texFollaje, normalFollaje, roughFollaje, 0.16f);
        Material matClaro = CrearMaterial("Mat_BriznasClaras_Pasto_01", new Color(0.42f, 0.54f, 0.23f), texFollaje, normalFollaje, roughFollaje, 0.17f);

        GameObject raiz = new GameObject(nombre);

        MeshBuilder mbOscuro = new MeshBuilder();
        MeshBuilder mbMedio = new MeshBuilder();
        MeshBuilder mbClaro = new MeshBuilder();

        const int totalBriznas = 18;
        for (int i = 0; i < totalBriznas; i++)
        {
            float angulo = (i * (360f / totalBriznas) + HashSigned(i, 1101) * 11f) * Mathf.Deg2Rad;
            float distBase = 0.015f + (i % 3) * 0.018f;
            Vector3 origen = new Vector3(Mathf.Cos(angulo) * distBase, 0f, Mathf.Sin(angulo) * distBase);

            float altura = 0.26f + (i % 3) * 0.065f + Hash01(i, 1102) * 0.05f;
            float curvatura = 0.08f + (i % 4) * 0.035f;
            float ancho = 0.024f + (i % 2) * 0.006f;

            if (i % 3 == 0)
                mbOscuro.AddGrassBlade3D(origen, angulo, altura * 0.85f, curvatura * 1.15f, ancho);
            else if (i % 3 == 1)
                mbMedio.AddGrassBlade3D(origen, angulo, altura, curvatura, ancho);
            else
                mbClaro.AddGrassBlade3D(origen, angulo, altura * 1.08f, curvatura * 0.85f, ancho * 0.9f);
        }

        CrearHijoMalla(raiz.transform, "Briznas_Oscuras", GuardarMalla(mbOscuro.ToMesh(nombre + "_Oscuras"), nombre + "_Oscuras.asset"), matOscuro);
        CrearHijoMalla(raiz.transform, "Briznas_Medias", GuardarMalla(mbMedio.ToMesh(nombre + "_Medias"), nombre + "_Medias.asset"), matMedio);
        CrearHijoMalla(raiz.transform, "Briznas_Claras", GuardarMalla(mbClaro.ToMesh(nombre + "_Claras"), nombre + "_Claras.asset"), matClaro);

        GuardarPrefab(raiz, prefabPath);
    }

    // =========================================================================
    // 6. PASTO 02: Hierba Silvestre / Coiron Bajo (Manojos irregulares y espigas)
    // =========================================================================
    private static void CrearPasto02(
        Texture2D texFollaje, Texture2D normalFollaje, Texture2D roughFollaje, bool recrear)
    {
        const string nombre = "Pasto_02";
        string prefabPath = Carpeta + "/" + nombre + ".prefab";
        if (!recrear && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        Material matVerde = CrearMaterial("Mat_HierbaVerde_Pasto_02", new Color(0.25f, 0.38f, 0.18f), texFollaje, normalFollaje, roughFollaje, 0.14f);
        Material matPajizo = CrearMaterial("Mat_HierbaPajiza_Pasto_02", new Color(0.56f, 0.49f, 0.25f), texFollaje, normalFollaje, roughFollaje, 0.15f);
        Material matEspiga = CrearMaterial("Mat_EspigaSeca_Pasto_02", new Color(0.68f, 0.58f, 0.31f), texFollaje, normalFollaje, roughFollaje, 0.16f);

        GameObject raiz = new GameObject(nombre);

        MeshBuilder mbVerde = new MeshBuilder();
        MeshBuilder mbPajizo = new MeshBuilder();
        MeshBuilder mbEspiga = new MeshBuilder();

        Vector3[] centrosManojo =
        {
            new Vector3(-0.07f, 0f, 0.04f),
            new Vector3(0.08f, 0f, 0.02f),
            new Vector3(0.0f, 0f, -0.07f)
        };

        int idx = 0;
        for (int c = 0; c < centrosManojo.Length; c++)
        {
            int briznasEnManojo = 7;
            for (int b = 0; b < briznasEnManojo; b++)
            {
                idx++;
                float ang = (b * (360f / briznasEnManojo) + c * 37f + HashSigned(idx, 1201) * 14f) * Mathf.Deg2Rad;
                Vector3 origen = centrosManojo[c] + new Vector3(Mathf.Cos(ang) * 0.015f, 0f, Mathf.Sin(ang) * 0.015f);
                float altura = 0.18f + (idx % 3) * 0.045f + Hash01(idx, 1202) * 0.04f;
                float curvatura = 0.07f + (b % 3) * 0.035f;
                float ancho = 0.020f + (b % 2) * 0.005f;

                if ((b + c) % 2 == 0)
                    mbVerde.AddGrassBlade3D(origen, ang, altura, curvatura, ancho);
                else
                    mbPajizo.AddGrassBlade3D(origen, ang, altura * 0.95f, curvatura * 1.15f, ancho * 0.9f);
            }
        }

        for (int e = 0; e < 3; e++)
        {
            float ang = (e * 115f + 20f) * Mathf.Deg2Rad;
            Vector3 baseTallo = centrosManojo[e] + new Vector3(0.01f, 0f, -0.01f);
            Vector3 medioTallo = baseTallo + new Vector3(Mathf.Cos(ang) * 0.04f, 0.18f, Mathf.Sin(ang) * 0.04f);
            Vector3 puntaTallo = baseTallo + new Vector3(Mathf.Cos(ang) * 0.09f, 0.34f + e * 0.03f, Mathf.Sin(ang) * 0.09f);

            mbPajizo.AddLowPolyTube(
                new[] { baseTallo, medioTallo, puntaTallo },
                new[] { 0.009f, 0.007f, 0.004f }, 4, 1250 + e, false, true);

            Vector3 dirEspiga = (puntaTallo - medioTallo).normalized;
            mbEspiga.AddSpikeHead(puntaTallo - dirEspiga * 0.02f, dirEspiga, 0.075f, 0.022f);
        }

        CrearHijoMalla(raiz.transform, "Hierba_Verde", GuardarMalla(mbVerde.ToMesh(nombre + "_Verde"), nombre + "_Verde.asset"), matVerde);
        CrearHijoMalla(raiz.transform, "Hierba_Pajiza", GuardarMalla(mbPajizo.ToMesh(nombre + "_Pajiza"), nombre + "_Pajiza.asset"), matPajizo);
        CrearHijoMalla(raiz.transform, "Espigas_Secas", GuardarMalla(mbEspiga.ToMesh(nombre + "_Espigas"), nombre + "_Espigas.asset"), matEspiga);

        GuardarPrefab(raiz, prefabPath);
    }

    // =========================================================================
    // CONSTRUCTOR DE GEOMETRIA LOW POLY (FLAT SHADED)
    // =========================================================================
    private class MeshBuilder
    {
        private readonly List<Vector3> _verts = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<int> _tris = new List<int>();

        public void AddFlatTri(Vector3 a, Vector3 b, Vector3 c, Vector2 uvA, Vector2 uvB, Vector2 uvC)
        {
            int start = _verts.Count;
            _verts.Add(a);
            _verts.Add(b);
            _verts.Add(c);
            _uvs.Add(uvA);
            _uvs.Add(uvB);
            _uvs.Add(uvC);
            _tris.Add(start);
            _tris.Add(start + 1);
            _tris.Add(start + 2);
        }

        public void AddFlatQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
        {
            AddFlatTri(a, b, c, uvA, uvB, uvC);
            AddFlatTri(a, c, d, uvA, uvC, uvD);
        }

        public void AddLowPolyTube(Vector3[] path, float[] radii, int sides, int seed, bool capBottom, bool capTop)
        {
            int rings = path.Length;
            Vector3[][] ringVerts = new Vector3[rings][];

            for (int i = 0; i < rings; i++)
            {
                ringVerts[i] = new Vector3[sides];
                Vector3 tangent;
                if (i == 0) tangent = (path[1] - path[0]).normalized;
                else if (i == rings - 1) tangent = (path[rings - 1] - path[rings - 2]).normalized;
                else tangent = (path[i + 1] - path[i - 1]).normalized;

                Vector3 upRef = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) > 0.92f ? Vector3.forward : Vector3.up;
                Vector3 right = Vector3.Cross(tangent, upRef).normalized;
                Vector3 forward = Vector3.Cross(right, tangent).normalized;

                float twist = i * 0.22f;
                for (int s = 0; s < sides; s++)
                {
                    float a = (s / (float)sides) * Mathf.PI * 2f + twist;
                    float jitter = 1f + HashSigned(i * 31 + s, seed) * 0.10f;
                    float r = radii[i] * jitter;
                    Vector3 offset = (right * Mathf.Cos(a) + forward * Mathf.Sin(a)) * r;
                    ringVerts[i][s] = path[i] + offset;
                }
            }

            for (int i = 0; i < rings - 1; i++)
            {
                float v0 = i / (float)(rings - 1);
                float v1 = (i + 1) / (float)(rings - 1);
                for (int s = 0; s < sides; s++)
                {
                    int sNext = (s + 1) % sides;
                    float u0 = s / (float)sides;
                    float u1 = (s + 1) / (float)sides;

                    Vector3 a = ringVerts[i][s];
                    Vector3 b = ringVerts[i][sNext];
                    Vector3 c = ringVerts[i + 1][sNext];
                    Vector3 d = ringVerts[i + 1][s];

                    AddFlatQuad(a, d, c, b, new Vector2(u0, v0), new Vector2(u0, v1), new Vector2(u1, v1), new Vector2(u1, v0));
                }
            }

            if (capBottom && radii[0] > 0.001f)
            {
                Vector3 center = path[0];
                for (int s = 0; s < sides; s++)
                {
                    int sNext = (s + 1) % sides;
                    AddFlatTri(center, ringVerts[0][s], ringVerts[0][sNext], new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.right);
                }
            }

            if (capTop && radii[rings - 1] > 0.001f)
            {
                Vector3 center = path[rings - 1];
                for (int s = 0; s < sides; s++)
                {
                    int sNext = (s + 1) % sides;
                    AddFlatTri(center, ringVerts[rings - 1][sNext], ringVerts[rings - 1][s], new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.right);
                }
            }
        }

        public void AddLowPolyFoliageBlob(Vector3 center, Vector3 scale, int subdivisions, int seed)
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            List<Vector3> verts = new List<Vector3>
            {
                new Vector3(-1f,  t, 0f).normalized,
                new Vector3( 1f,  t, 0f).normalized,
                new Vector3(-1f, -t, 0f).normalized,
                new Vector3( 1f, -t, 0f).normalized,
                new Vector3(0f, -1f,  t).normalized,
                new Vector3(0f,  1f,  t).normalized,
                new Vector3(0f, -1f, -t).normalized,
                new Vector3(0f,  1f, -t).normalized,
                new Vector3( t, 0f, -1f).normalized,
                new Vector3( t, 0f,  1f).normalized,
                new Vector3(-t, 0f, -1f).normalized,
                new Vector3(-t, 0f,  1f).normalized
            };

            List<int[]> faces = new List<int[]>
            {
                new[] {0, 11, 5}, new[] {0, 5, 1}, new[] {0, 1, 7}, new[] {0, 7, 10}, new[] {0, 10, 11},
                new[] {1, 5, 9}, new[] {5, 11, 4}, new[] {11, 10, 2}, new[] {10, 7, 6}, new[] {7, 1, 8},
                new[] {3, 9, 4}, new[] {3, 4, 2}, new[] {3, 2, 6}, new[] {3, 6, 8}, new[] {3, 8, 9},
                new[] {4, 9, 5}, new[] {2, 4, 11}, new[] {6, 2, 10}, new[] {8, 6, 7}, new[] {9, 8, 1}
            };

            for (int sub = 0; sub < subdivisions; sub++)
            {
                Dictionary<long, int> midCache = new Dictionary<long, int>();
                List<int[]> nextFaces = new List<int[]>(faces.Count * 4);

                int GetMid(int i1, int i2)
                {
                    long min = Mathf.Min(i1, i2);
                    long max = Mathf.Max(i1, i2);
                    long key = (min << 32) | (uint)max;
                    if (midCache.TryGetValue(key, out int idx)) return idx;
                    Vector3 m = ((verts[i1] + verts[i2]) * 0.5f).normalized;
                    idx = verts.Count;
                    verts.Add(m);
                    midCache[key] = idx;
                    return idx;
                }

                for (int f = 0; f < faces.Count; f++)
                {
                    int a = faces[f][0];
                    int b = faces[f][1];
                    int c = faces[f][2];
                    int ab = GetMid(a, b);
                    int bc = GetMid(b, c);
                    int ca = GetMid(c, a);

                    nextFaces.Add(new[] { a, ab, ca });
                    nextFaces.Add(new[] { b, bc, ab });
                    nextFaces.Add(new[] { c, ca, bc });
                    nextFaces.Add(new[] { ab, bc, ca });
                }
                faces = nextFaces;
            }

            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i];
                float n1 = Mathf.Sin(v.x * 3.7f + seed * 1.13f) * Mathf.Cos(v.y * 4.1f - seed * 0.71f);
                float n2 = Mathf.Sin(v.z * 3.3f + v.x * 2.5f + seed * 2.07f);
                float disp = 1f + (n1 * 0.14f + n2 * 0.10f);

                float yFactor = v.y < 0f ? 0.78f : 1.0f;
                verts[i] = center + new Vector3(v.x * scale.x * disp, v.y * scale.y * disp * yFactor, v.z * scale.z * disp);
            }

            for (int f = 0; f < faces.Count; f++)
            {
                Vector3 va = verts[faces[f][0]];
                Vector3 vb = verts[faces[f][1]];
                Vector3 vc = verts[faces[f][2]];
                AddFlatTri(
                    va, vb, vc,
                    new Vector2(va.x * 0.5f + 0.5f, va.y * 0.5f + 0.5f),
                    new Vector2(vb.x * 0.5f + 0.5f, vb.y * 0.5f + 0.5f),
                    new Vector2(vc.x * 0.5f + 0.5f, vc.y * 0.5f + 0.5f));
            }
        }

        public void AddGrassBlade3D(Vector3 origin, float angleRad, float height, float bendOut, float baseWidth)
        {
            Vector3 forward = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad));
            Vector3 right = new Vector3(-forward.z, 0f, forward.x);

            const int segments = 4;
            Vector3[] leftEdge = new Vector3[segments];
            Vector3[] centerSpine = new Vector3[segments];
            Vector3[] rightEdge = new Vector3[segments];

            for (int s = 0; s < segments; s++)
            {
                float t = s / (float)(segments - 1);
                float y = height * Mathf.Sin(t * Mathf.PI * 0.5f);
                float outDist = bendOut * (t * t);
                float width = baseWidth * (1f - t * 0.85f);
                float vDepth = width * 0.35f;

                Vector3 center = origin + forward * outDist + new Vector3(0f, y, 0f);
                centerSpine[s] = center - forward * vDepth;
                leftEdge[s] = center - right * (width * 0.5f);
                rightEdge[s] = center + right * (width * 0.5f);
            }

            float topOut = bendOut * 1.28f;
            Vector3 tip = origin + forward * topOut + new Vector3(0f, height * 1.04f, 0f);

            for (int s = 0; s < segments - 1; s++)
            {
                float v0 = s / (float)segments;
                float v1 = (s + 1) / (float)segments;

                AddFlatQuad(leftEdge[s], leftEdge[s + 1], centerSpine[s + 1], centerSpine[s],
                    new Vector2(0f, v0), new Vector2(0f, v1), new Vector2(0.5f, v1), new Vector2(0.5f, v0));
                AddFlatQuad(centerSpine[s], centerSpine[s + 1], rightEdge[s + 1], rightEdge[s],
                    new Vector2(0.5f, v0), new Vector2(0.5f, v1), new Vector2(1f, v1), new Vector2(1f, v0));

                AddFlatQuad(rightEdge[s], rightEdge[s + 1], centerSpine[s + 1], centerSpine[s],
                    new Vector2(1f, v0), new Vector2(1f, v1), new Vector2(0.5f, v1), new Vector2(0.5f, v0));
                AddFlatQuad(centerSpine[s], centerSpine[s + 1], leftEdge[s + 1], leftEdge[s],
                    new Vector2(0.5f, v0), new Vector2(0.5f, v1), new Vector2(0f, v1), new Vector2(0f, v0));
            }

            int last = segments - 1;
            AddFlatTri(leftEdge[last], tip, centerSpine[last], new Vector2(0f, 0.8f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.8f));
            AddFlatTri(centerSpine[last], tip, rightEdge[last], new Vector2(0.5f, 0.8f), new Vector2(0.5f, 1f), new Vector2(1f, 0.8f));
            AddFlatTri(rightEdge[last], tip, centerSpine[last], new Vector2(1f, 0.8f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.8f));
            AddFlatTri(centerSpine[last], tip, leftEdge[last], new Vector2(0.5f, 0.8f), new Vector2(0.5f, 1f), new Vector2(0f, 0.8f));
        }

        public void AddSpikeHead(Vector3 basePos, Vector3 dir, float length, float radius)
        {
            Vector3 upRef = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.9f ? Vector3.forward : Vector3.up;
            Vector3 r1 = Vector3.Cross(dir, upRef).normalized * radius;
            Vector3 r2 = Vector3.Cross(dir, r1).normalized * radius;

            Vector3 mid = basePos + dir * (length * 0.45f);
            Vector3 tip = basePos + dir * length;

            Vector3[] ring = { mid + r1, mid + r2, mid - r1, mid - r2 };
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                AddFlatTri(basePos, ring[next], ring[i], new Vector2(0.5f, 0f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f));
                AddFlatTri(ring[i], ring[next], tip, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 1f));
            }
        }

        public Mesh ToMesh(string meshName)
        {
            Mesh mesh = new Mesh { name = meshName };
            if (_verts.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(_verts);
            mesh.SetUVs(0, _uvs);
            mesh.SetTriangles(_tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    // =========================================================================
    // UTILIDADES DE TEXTURAS, MATERIALES Y ARCHIVOS
    // =========================================================================
    private static float Hash01(int x, int seed)
    {
        int n = x * 374761393 + seed * 668265263;
        n = (n ^ (n >> 13)) * 1274126177;
        return ((n ^ (n >> 16)) & 0x7fffffff) / (float)0x7fffffff;
    }

    private static float HashSigned(int x, int seed)
    {
        return Hash01(x, seed) * 2f - 1f;
    }

    private static Texture2D CargarOCrearTextura(string nombre, int semilla, int tipo)
    {
        string ruta = MaterialesCarpeta + "/" + nombre;
        Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        bool crear = textura == null;
        const int resolucion = 128;

        if (crear)
        {
            textura = new Texture2D(resolucion, resolucion, TextureFormat.RGBA32, true, tipo == 1 || tipo == 2)
            {
                name = System.IO.Path.GetFileNameWithoutExtension(nombre),
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4
            };
        }

        Color[] pixeles = new Color[resolucion * resolucion];
        for (int y = 0; y < resolucion; y++)
        {
            for (int x = 0; x < resolucion; x++)
            {
                float n1 = Mathf.PerlinNoise((x + semilla) * 0.09f, (y + semilla) * 0.04f);
                float n2 = Mathf.PerlinNoise((x + semilla * 2) * 0.18f, (y + semilla * 3) * 0.18f);

                if (tipo == 1)
                {
                    Vector3 n = new Vector3((n1 - 0.5f) * 0.28f, (n2 - 0.5f) * 0.28f, 1f).normalized;
                    pixeles[y * resolucion + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
                else if (tipo == 2)
                {
                    float rough = Mathf.Clamp01(0.78f + (n1 - 0.5f) * 0.10f + (n2 - 0.5f) * 0.08f);
                    pixeles[y * resolucion + x] = new Color(0.2f, 0f, 0f, 1f - rough);
                }
                else if (tipo == 0)
                {
                    float v = Mathf.Clamp(0.90f + (n1 - 0.5f) * 0.16f + (n2 - 0.5f) * 0.10f, 0.74f, 1f);
                    pixeles[y * resolucion + x] = new Color(v, v * 0.96f, v * 0.92f, 1f);
                }
                else
                {
                    float v = Mathf.Clamp(0.94f + (n2 - 0.5f) * 0.10f, 0.85f, 1f);
                    pixeles[y * resolucion + x] = new Color(v, v, v * 0.97f, 1f);
                }
            }
        }

        textura.SetPixels(pixeles);
        textura.Apply(true, false);
        if (crear) AssetDatabase.CreateAsset(textura, ruta);
        else EditorUtility.SetDirty(textura);
        return textura;
    }

    private static Material CrearMaterial(
        string nombre, Color color, Texture2D albedo, Texture2D normal, Texture2D roughness, float smoothness)
    {
        string ruta = MaterialesCarpeta + "/" + nombre + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;
            material = new Material(shader) { name = nombre };
            AssetDatabase.CreateAsset(material, ruta);
        }

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", albedo);
        if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", normal);
        if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", 0.22f);
        if (material.HasProperty("_MetallicGlossMap")) material.SetTexture("_MetallicGlossMap", roughness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        material.EnableKeyword("_NORMALMAP");
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CrearHijoMalla(Transform padre, string nombre, Mesh malla, Material material)
    {
        GameObject obj = new GameObject(nombre);
        obj.transform.SetParent(padre, false);
        obj.AddComponent<MeshFilter>().sharedMesh = malla;
        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
    }

    private static Mesh GuardarMalla(Mesh mesh, string nombreArchivo)
    {
        string ruta = MallasCarpeta + "/" + nombreArchivo;
        Mesh existente = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        if (existente == null)
        {
            AssetDatabase.CreateAsset(mesh, ruta);
            return mesh;
        }
        EditorUtility.CopySerialized(mesh, existente);
        Object.DestroyImmediate(mesh);
        EditorUtility.SetDirty(existente);
        return existente;
    }

    private static void AsegurarCarpetas()
    {
        AsegurarCarpeta(Carpeta);
        AsegurarCarpeta(MallasCarpeta);
        AsegurarCarpeta(MaterialesCarpeta);
    }

    private static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = System.IO.Path.GetDirectoryName(ruta).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(padre)) AsegurarCarpeta(padre);
        AssetDatabase.CreateFolder(padre, System.IO.Path.GetFileName(ruta));
    }

    private static void GuardarPrefab(GameObject objeto, string ruta)
    {
        PrefabUtility.SaveAsPrefabAsset(objeto, ruta);
        Object.DestroyImmediate(objeto);
    }
}
