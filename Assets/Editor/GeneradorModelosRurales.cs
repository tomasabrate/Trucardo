using System.Collections.Generic;
using TMPro;
using Trucardo.Core;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GeneradorModelosRurales
{
    private const string Carpeta = "Assets/Models/BarRecursos";
    private const string MaterialPath = "Assets/Resources/Texturas/Maderas/Dark Wooden Planks/Mat_Banquito.mat";
    private const string BancoPath = Carpeta + "/BancoLargo.prefab";
    private const string PortalPath = Carpeta + "/PortalCartel.prefab";
    private const string ChimeneaPath = Carpeta + "/ChimeneaLenia.prefab";
    private const string EstufaPath = Carpeta + "/EstufaLenia.prefab";
    private const string MamposteriaPath = Carpeta + "/Mat_Pared_Norte.mat";
    private const string HierroPath = Carpeta + "/Mat_EstufaHierro.mat";
    private const string FuegoPath = Carpeta + "/Mat_EstufaFuego.mat";
    private const string BrasaPath = Carpeta + "/Mat_EstufaBrasa.mat";
    private const string InteriorPath = Carpeta + "/Mat_EstufaInterior.mat";
    private const string VallaPath = Carpeta + "/VallaRural.prefab";
    private const string PortonPath = Carpeta + "/PortonRural.prefab";
    private const string HerrajesPath = Carpeta + "/Mat_HerrajesNegros.mat";
    private const string MolinetePath = Carpeta + "/MolineteRural.prefab";
    private const string MolineteMetalPath = Carpeta + "/Mat_MolineteMetal.mat";
    private const string FarolPath = Carpeta + "/FarolAceite.prefab";
    private const string FarolColgantePath = Carpeta + "/FarolColgante.prefab";
    private const string FarolVidrioPath = Carpeta + "/Mat_FarolVidrio.mat";
    private const string FarolLatonPath = Carpeta + "/Mat_FarolLaton.mat";
    private const string EstanteriaEpocaPath = Carpeta + "/EstanteriaEpoca.prefab";
    private const string CandelabroPulperiaPath = Carpeta + "/CandelabroPulperia.prefab";
    private const string VentanaPulperiaPath = Carpeta + "/VentanaPulperia.prefab";
    private const string PersianasMaderaPulperiaPath = Carpeta + "/PersianasMaderaPulperia.prefab";
    private const string PuertaPulperiaPath = Carpeta + "/PuertaPulperia.prefab";
    private const string VentanaVidrioPath = Carpeta + "/Mat_VentanaVidrio.mat";
    private const string EmbutidosCarpeta = Carpeta + "/Embutidos";
    private const string EmbutidosMaterialesCarpeta = EmbutidosCarpeta + "/Materiales";
    private const string EmbutidosMallasCarpeta = EmbutidosCarpeta + "/Mallas";
    private const string BarrilesCarpeta = Carpeta + "/Barriles";
    private const string BarrilesMaterialesCarpeta = BarrilesCarpeta + "/Materiales";
    private const string BarrilesMallasCarpeta = BarrilesCarpeta + "/Mallas";

    static GeneradorModelosRurales()
    {
        EditorApplication.delayCall += GenerarModelosFaltantes;
    }

    [MenuItem("Trucardo/Crear modelos rurales faltantes")]
    public static void GenerarModelosFaltantes()
    {
        Material madera = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Material piedra = AssetDatabase.LoadAssetAtPath<Material>(MamposteriaPath);
        if (madera == null)
        {
            Debug.LogError("No se encontro Mat_Banquito en " + MaterialPath);
            return;
        }
        if (piedra == null)
        {
            Debug.LogError("No se encontro el material de mamposteria en " + MamposteriaPath);
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(BancoPath) == null)
            CrearBanco(madera);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PortalPath) == null)
            CrearPortal(madera);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ChimeneaPath) == null)
        {
            Material interiorChimenea = CargarOCrearMaterial(InteriorPath, new Color(0.035f, 0.018f, 0.012f), 0f, 0.1f);
            CrearChimenea(piedra, interiorChimenea);
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(EstufaPath) == null)
        {
            Material hierro = CargarOCrearMaterial(HierroPath, new Color(0.12f, 0.13f, 0.14f), 0.72f, 0.38f);
            Material fuego = CargarOCrearMaterial(FuegoPath, new Color(0.95f, 0.18f, 0.025f), 0f, 0.2f, new Color(1f, 0.12f, 0.015f) * 2f);
            Material brasa = CargarOCrearMaterial(BrasaPath, new Color(1f, 0.56f, 0.08f), 0f, 0.2f, new Color(1f, 0.35f, 0.025f) * 2f);
            Material interior = CargarOCrearMaterial(InteriorPath, new Color(0.035f, 0.018f, 0.012f), 0f, 0.1f);
            if (hierro == null || fuego == null || brasa == null || interior == null)
            {
                Debug.LogError("No se pudieron crear los materiales de la estufa.");
                return;
            }

            CrearEstufa(hierro, fuego, brasa, interior, madera, piedra);
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(VallaPath) == null)
            CrearValla(madera);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PortonPath) == null)
        {
            Material herrajes = CargarOCrearMaterial(HerrajesPath, new Color(0.075f, 0.065f, 0.055f), 0.78f, 0.3f);
            if (herrajes == null)
            {
                Debug.LogError("No se pudieron crear los herrajes del porton.");
                return;
            }

            CrearPorton(madera, herrajes);
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(MolinetePath) == null)
        {
            Material metalMolinete = ObtenerMaterialMolinete();
            if (metalMolinete == null) return;
            CrearMolinete(metalMolinete, madera);
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(FarolPath) == null)
            CrearFarolDeAceite();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(FarolColgantePath) == null)
            CrearFarolColganteDeAceite();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(EstanteriaEpocaPath) == null)
            CrearEstanteriaEpoca();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(CandelabroPulperiaPath) == null)
            CrearCandelabroEpoca();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(VentanaPulperiaPath) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(PersianasMaderaPulperiaPath) == null)
            CrearVentanaPulperia();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PuertaPulperiaPath) == null)
            CrearPuertaPulperia();
        CrearEmbutidosFaltantes(false);
        CrearBarrilesFaltantes(false);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Modelos rurales revisados en " + Carpeta);
    }

    [MenuItem("Trucardo/Recrear valla y porton rurales")]
    public static void RecrearVallasRurales()
    {
        Material madera = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (madera == null)
        {
            Debug.LogError("No se encontro Mat_Banquito en " + MaterialPath);
            return;
        }

        Material herrajes = CargarOCrearMaterial(HerrajesPath, new Color(0.075f, 0.065f, 0.055f), 0.78f, 0.3f);
        if (herrajes == null)
        {
            Debug.LogError("No se pudieron crear los herrajes del porton.");
            return;
        }

        CrearValla(madera);
        CrearPorton(madera, herrajes);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Prefabs VallaRural y PortonRural actualizados.");
    }

    [MenuItem("Trucardo/Recrear molinete rural")]
    public static void RecrearMolineteRural()
    {
        Material madera = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Material metalMolinete = ObtenerMaterialMolinete();
        if (madera == null || metalMolinete == null)
        {
            Debug.LogError("Falta el material de madera o metal para crear el molinete.");
            return;
        }

        CrearMolinete(metalMolinete, madera);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Prefab MolineteRural actualizado.");
    }

    [MenuItem("Trucardo/Recrear farol de aceite")]
    public static void RecrearFarolDeAceite()
    {
        CrearFarolDeAceite();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Trucardo/Recrear farol colgante")]
    public static void RecrearFarolColgante()
    {
        CrearFarolColganteDeAceite();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Trucardo/Recrear estanteria de epoca")]
    public static void RecrearEstanteriaEpoca()
    {
        Material madera = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (madera == null)
        {
            Debug.LogError("No se encontro Mat_Banquito en " + MaterialPath);
            return;
        }

        CrearEstanteriaEpoca();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Prefab EstanteriaEpoca actualizado.");
    }

    [MenuItem("Trucardo/Recrear candelabro de epoca")]
    public static void RecrearCandelabroEpoca()
    {
        CrearCandelabroEpoca();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Prefab CandelabroEpoca actualizado.");
    }

    [MenuItem("Trucardo/Recrear ventana de pulpería")]
    public static void RecrearVentanaPulperia()
    {
        CrearVentanaPulperia(true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Trucardo/Recrear puerta de pulpería")]
    public static void RecrearPuertaPulperia()
    {
        CrearPuertaPulperia();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Prefab PuertaPulperia actualizado con la hoja abierta por defecto.");
    }

    [MenuItem("Trucardo/Recrear colección de chorizos y jamones")]
    public static void RecrearColeccionEmbutidos()
    {
        CrearEmbutidosFaltantes(true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Colección de chorizos y jamones individuales actualizada en " + EmbutidosCarpeta);
    }

    [MenuItem("Trucardo/Recrear colección de barriles")]
    public static void RecrearColeccionBarriles()
    {
        CrearBarrilesFaltantes(true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Colección de barriles individuales actualizada en " + BarrilesCarpeta);
    }

    private struct ConfiguracionBarril
    {
        public string Nombre;
        public float Altura;
        public float Diametro;
        public int Duelas;
        public int Aros;
        public float Deformacion;
        public float Inclinacion;
        public float Desgaste;
        public float HundimientoTapa;
        public Color Madera;
    }

    private static void CrearBarrilesFaltantes(bool recrear)
    {
        AsegurarCarpeta(BarrilesCarpeta);
        AsegurarCarpeta(BarrilesMaterialesCarpeta);
        AsegurarCarpeta(BarrilesMallasCarpeta);

        Texture2D texturaMadera = CargarOCrearTexturaBarril("Textura_MaderaBarril.asset", 41, 0);
        Texture2D texturaHierro = CargarOCrearTexturaBarril("Textura_HierroBarril.asset", 83, 1);
        Texture2D normalMadera = CargarOCrearTexturaBarril("Normal_MaderaBarril.asset", 59, 2);
        Texture2D normalHierro = CargarOCrearTexturaBarril("Normal_HierroBarril.asset", 97, 2);
        Texture2D suavidadMadera = CargarOCrearTexturaBarril("Suavidad_MaderaBarril.asset", 113, 3);
        Texture2D suavidadHierro = CargarOCrearTexturaBarril("Suavidad_HierroBarril.asset", 131, 4);
        if (texturaMadera == null || texturaHierro == null || normalMadera == null || normalHierro == null ||
            suavidadMadera == null || suavidadHierro == null)
        {
            Debug.LogError("No se pudieron crear las texturas de la colección de barriles.");
            return;
        }

        Material hierro = CargarOCrearMaterialBarril("Mat_HierroForgado", new Color(0.19f, 0.17f, 0.14f),
            texturaHierro, normalHierro, suavidadHierro, true);
        if (hierro == null) return;

        ConfiguracionBarril[] variantes =
        {
            new ConfiguracionBarril { Nombre = "Barril_01", Altura = 0.90f, Diametro = 0.66f, Duelas = 16, Aros = 5, Deformacion = 0.018f, Inclinacion = 0f, Desgaste = 0.82f, HundimientoTapa = 0.012f, Madera = new Color(0.45f, 0.27f, 0.18f) },
            new ConfiguracionBarril { Nombre = "Barril_02", Altura = 1.15f, Diametro = 0.85f, Duelas = 20, Aros = 6, Deformacion = 0.012f, Inclinacion = 0f, Desgaste = 0.50f, HundimientoTapa = 0.006f, Madera = new Color(0.54f, 0.31f, 0.20f) },
            new ConfiguracionBarril { Nombre = "Barril_03", Altura = 0.60f, Diametro = 0.50f, Duelas = 14, Aros = 4, Deformacion = 0.028f, Inclinacion = 0f, Desgaste = 0.98f, HundimientoTapa = 0.021f, Madera = new Color(0.42f, 0.40f, 0.34f) },
            new ConfiguracionBarril { Nombre = "Barril_04", Altura = 1.10f, Diametro = 0.55f, Duelas = 16, Aros = 5, Deformacion = 0.009f, Inclinacion = 0f, Desgaste = 0.56f, HundimientoTapa = 0.004f, Madera = new Color(0.50f, 0.31f, 0.20f) },
            new ConfiguracionBarril { Nombre = "Barril_05", Altura = 0.70f, Diametro = 0.90f, Duelas = 20, Aros = 5, Deformacion = 0.021f, Inclinacion = 0f, Desgaste = 0.70f, HundimientoTapa = 0.014f, Madera = new Color(0.48f, 0.32f, 0.22f) },
            new ConfiguracionBarril { Nombre = "Barril_06", Altura = 0.85f, Diametro = 0.70f, Duelas = 18, Aros = 6, Deformacion = 0.032f, Inclinacion = 0f, Desgaste = 0.94f, HundimientoTapa = 0.018f, Madera = new Color(0.36f, 0.23f, 0.17f) },
            new ConfiguracionBarril { Nombre = "Barril_07", Altura = 0.80f, Diametro = 0.65f, Duelas = 15, Aros = 4, Deformacion = 0.044f, Inclinacion = -0.035f, Desgaste = 0.75f, HundimientoTapa = 0.009f, Madera = new Color(0.57f, 0.40f, 0.28f) }
        };

        for (int i = 0; i < variantes.Length; i++)
            CrearBarrilIndividual(variantes[i], hierro, texturaMadera, normalMadera, suavidadMadera, recrear);
    }

    private static void CrearBarrilIndividual(ConfiguracionBarril perfil, Material hierro, Texture2D texturaMadera,
        Texture2D normalMadera, Texture2D suavidadMadera, bool recrear)
    {
        Material[] materialesMadera = new Material[3];
        float[] tonos = { 0.88f, 1f, 1.12f };
        for (int i = 0; i < materialesMadera.Length; i++)
        {
            Color tono = perfil.Madera * tonos[i];
            materialesMadera[i] = CargarOCrearMaterialBarril(perfil.Nombre + "_Madera_0" + (i + 1), tono,
                texturaMadera, normalMadera, suavidadMadera, false);
            if (materialesMadera[i] == null) return;
        }

        string prefabPath = BarrilesCarpeta + "/" + perfil.Nombre + ".prefab";
        if (!recrear && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        int semilla = System.Array.IndexOf(new[] { "Barril_01", "Barril_02", "Barril_03", "Barril_04", "Barril_05", "Barril_06", "Barril_07" }, perfil.Nombre) + 37;
        Mesh meshDuelas = GuardarMallaBarril(CrearMallaDuelasBarril(perfil, semilla), perfil.Nombre + "_Duelas.asset");
        Mesh meshSuperior = GuardarMallaBarril(CrearMallaTapaBarril(perfil, true, semilla), perfil.Nombre + "_TapaSuperior.asset");
        Mesh meshInferior = GuardarMallaBarril(CrearMallaTapaBarril(perfil, false, semilla), perfil.Nombre + "_TapaInferior.asset");
        if (meshDuelas == null || meshSuperior == null || meshInferior == null) return;

        GameObject raiz = new GameObject(perfil.Nombre);
        GameObject duelas = new GameObject("Duelas");
        duelas.transform.SetParent(raiz.transform, false);
        duelas.AddComponent<MeshFilter>().sharedMesh = meshDuelas;
        duelas.AddComponent<MeshRenderer>().sharedMaterials = materialesMadera;

        GameObject aros = new GameObject("Aros");
        aros.transform.SetParent(raiz.transform, false);
        for (int i = 0; i < perfil.Aros; i++)
        {
            float t = perfil.Aros == 1 ? 0.5f : 0.075f + i * (0.85f / (perfil.Aros - 1));
            string nombreAro = "Aro_0" + (i + 1);
            Mesh meshAro = GuardarMallaBarril(CrearMallaAroBarril(perfil, t, semilla + i * 13),
                perfil.Nombre + "_" + nombreAro + ".asset");
            GameObject aro = new GameObject(nombreAro);
            aro.transform.SetParent(aros.transform, false);
            aro.AddComponent<MeshFilter>().sharedMesh = meshAro;
            aro.AddComponent<MeshRenderer>().sharedMaterial = hierro;
        }

        GameObject tapaSuperior = new GameObject("TapaSuperior");
        tapaSuperior.transform.SetParent(raiz.transform, false);
        tapaSuperior.AddComponent<MeshFilter>().sharedMesh = meshSuperior;
        tapaSuperior.AddComponent<MeshRenderer>().sharedMaterials = materialesMadera;

        GameObject tapaInferior = new GameObject("TapaInferior");
        tapaInferior.transform.SetParent(raiz.transform, false);
        tapaInferior.AddComponent<MeshFilter>().sharedMesh = meshInferior;
        tapaInferior.AddComponent<MeshRenderer>().sharedMaterials = materialesMadera;

        GuardarPrefab(raiz, prefabPath);
    }

    private static Mesh CrearMallaDuelasBarril(ConfiguracionBarril perfil, int semilla)
    {
        const int filas = 12;
        const int segmentosAncho = 3;
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int>[] submallas = { new List<int>(), new List<int>(), new List<int>() };
        float radio = perfil.Diametro * 0.5f;
        float grosor = Mathf.Clamp(perfil.Diametro * 0.038f, 0.019f, 0.034f);
        float huecoAngular = 0.021f + perfil.Desgaste * 0.009f;

        for (int duela = 0; duela < perfil.Duelas; duela++)
        {
            int grupoMaterial = duela % 3;
            List<int> triangulos = submallas[grupoMaterial];
            float paso = Mathf.PI * 2f / perfil.Duelas;
            float anguloInicio = duela * paso + huecoAngular * 0.5f;
            float anguloFin = (duela + 1) * paso - huecoAngular * 0.5f;
            int externo = vertices.Count;
            AgregarSuperficieDuela(vertices, uv, perfil, radio, grosor, filas, segmentosAncho,
                anguloInicio, anguloFin, duela, semilla, true);
            AgregarTriangulosSuperficieDuela(triangulos, externo, filas, segmentosAncho, true);

            int interno = vertices.Count;
            AgregarSuperficieDuela(vertices, uv, perfil, radio, grosor, filas, segmentosAncho,
                anguloInicio, anguloFin, duela, semilla, false);
            AgregarTriangulosSuperficieDuela(triangulos, interno, filas, segmentosAncho, false);

            int columnas = segmentosAncho + 1;
            for (int fila = 0; fila < filas; fila++)
            {
                int exteriorIzquierdo = externo + fila * columnas;
                int exteriorDerecho = exteriorIzquierdo + segmentosAncho;
                int interiorIzquierdo = interno + fila * columnas;
                int interiorDerecho = interiorIzquierdo + segmentosAncho;
                AgregarCuadrilatero(triangulos, exteriorIzquierdo, interiorIzquierdo, exteriorIzquierdo + columnas, interiorIzquierdo + columnas);
                AgregarCuadrilatero(triangulos, exteriorDerecho, exteriorDerecho + columnas, interiorDerecho, interiorDerecho + columnas);
            }

            int exteriorFinal = externo + filas * columnas;
            int interiorFinal = interno + filas * columnas;
            for (int columna = 0; columna < segmentosAncho; columna++)
            {
                AgregarCuadrilatero(triangulos, externo + columna, externo + columna + 1, interno + columna, interno + columna + 1);
                AgregarCuadrilatero(triangulos, exteriorFinal + columna, interiorFinal + columna, exteriorFinal + columna + 1, interiorFinal + columna + 1);
            }
        }

        Mesh mesh = new Mesh { name = "Malla_Duelas_" + perfil.Nombre };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.subMeshCount = submallas.Length;
        for (int i = 0; i < submallas.Length; i++) mesh.SetTriangles(submallas[i], i);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AgregarSuperficieDuela(List<Vector3> vertices, List<Vector2> uv,
        ConfiguracionBarril perfil, float radio, float grosor, int filas, int segmentosAncho,
        float anguloInicio, float anguloFin, int duela, int semilla, bool exterior)
    {
        for (int fila = 0; fila <= filas; fila++)
        {
            float t = fila / (float)filas;
            for (int segmento = 0; segmento <= segmentosAncho; segmento++)
            {
                float u = segmento / (float)segmentosAncho;
                float angulo = Mathf.Lerp(anguloInicio, anguloFin, u);
                float variacion = 1f + perfil.Deformacion * 0.18f * Mathf.Sin(duela * 1.71f + t * 9f + semilla)
                    + perfil.Desgaste * 0.008f * Mathf.Sin(t * 47f + u * 11f + duela);
                float radioExterior = RadioBarril(perfil, t, radio, duela, semilla) * variacion;
                float r = exterior ? radioExterior : radioExterior - grosor;
                vertices.Add(PuntoBarril(perfil, t, angulo, r, duela));
                uv.Add(new Vector2(u, t));
            }
        }
    }

    private static void AgregarTriangulosSuperficieDuela(List<int> triangulos, int inicio,
        int filas, int segmentosAncho, bool exterior)
    {
        int columnas = segmentosAncho + 1;
        for (int fila = 0; fila < filas; fila++)
        {
            for (int segmento = 0; segmento < segmentosAncho; segmento++)
            {
                int abajoIzquierda = inicio + fila * columnas + segmento;
                int abajoDerecha = abajoIzquierda + 1;
                int arribaIzquierda = abajoIzquierda + columnas;
                int arribaDerecha = arribaIzquierda + 1;
                if (exterior)
                {
                    triangulos.Add(abajoIzquierda); triangulos.Add(arribaIzquierda); triangulos.Add(abajoDerecha);
                    triangulos.Add(abajoDerecha); triangulos.Add(arribaIzquierda); triangulos.Add(arribaDerecha);
                }
                else
                {
                    triangulos.Add(abajoIzquierda); triangulos.Add(abajoDerecha); triangulos.Add(arribaIzquierda);
                    triangulos.Add(abajoDerecha); triangulos.Add(arribaDerecha); triangulos.Add(arribaIzquierda);
                }
            }
        }
    }

    private static void AgregarCuadrilatero(List<int> triangulos, int a, int b, int c, int d)
    {
        triangulos.Add(a); triangulos.Add(b); triangulos.Add(c);
        triangulos.Add(b); triangulos.Add(d); triangulos.Add(c);
    }

    private static Mesh CrearMallaAroBarril(ConfiguracionBarril perfil, float t, int semilla)
    {
        const int segmentos = 40;
        List<Vector3> vertices = new List<Vector3>(segmentos * 4);
        List<Vector2> uv = new List<Vector2>(segmentos * 4);
        List<int> triangulos = new List<int>(segmentos * 24);
        float radio = perfil.Diametro * 0.5f;
        float centroY = t * perfil.Altura;
        float alturaAro = Mathf.Clamp(perfil.Altura * 0.024f, 0.019f, 0.031f);
        float grosor = Mathf.Clamp(perfil.Diametro * 0.026f, 0.012f, 0.021f);

        for (int i = 0; i < segmentos; i++)
        {
            float angulo = i * Mathf.PI * 2f / segmentos;
            float irregularidad = perfil.Desgaste * 0.004f * Mathf.Sin(angulo * 7f + semilla)
                + 0.002f * Mathf.Sin(angulo * 13f + semilla * 0.4f);
            float radioExterior = RadioBarril(perfil, t, radio, i, semilla) + irregularidad + grosor;
            float radioInterior = radioExterior - grosor;
            float desplazamientoY = perfil.Deformacion * 0.015f * Mathf.Sin(angulo * 3f + semilla);
            Vector3 punto = PuntoBarril(perfil, t, angulo, radioExterior, i);
            float yAbajo = centroY - alturaAro * 0.5f + desplazamientoY;
            float yArriba = centroY + alturaAro * 0.5f + desplazamientoY;
            vertices.Add(new Vector3(punto.x, yAbajo, punto.z));
            vertices.Add(new Vector3(punto.x, yArriba, punto.z));
            Vector3 interior = PuntoBarril(perfil, t, angulo, radioInterior, i);
            vertices.Add(new Vector3(interior.x, yAbajo, interior.z));
            vertices.Add(new Vector3(interior.x, yArriba, interior.z));
            uv.Add(new Vector2(i / (float)segmentos, 0f));
            uv.Add(new Vector2(i / (float)segmentos, 1f));
            uv.Add(new Vector2(i / (float)segmentos, 0f));
            uv.Add(new Vector2(i / (float)segmentos, 1f));
        }

        for (int i = 0; i < segmentos; i++)
        {
            int actual = i * 4;
            int siguiente = ((i + 1) % segmentos) * 4;
            AgregarCuadrilatero(triangulos, actual, siguiente, actual + 1, siguiente + 1);
            AgregarCuadrilatero(triangulos, actual + 2, actual + 3, siguiente + 2, siguiente + 3);
            AgregarCuadrilatero(triangulos, actual + 1, siguiente + 1, actual + 3, siguiente + 3);
            AgregarCuadrilatero(triangulos, actual, actual + 2, siguiente, siguiente + 2);
        }

        Mesh mesh = new Mesh { name = "Malla_Aro_" + perfil.Nombre };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CrearMallaTapaBarril(ConfiguracionBarril perfil, bool superior, int semilla)
    {
        const int tablones = 11;
        List<Vector3> vertices = new List<Vector3>(tablones * 8);
        List<Vector2> uv = new List<Vector2>(tablones * 8);
        List<int>[] submallas = { new List<int>(), new List<int>(), new List<int>() };
        float t = superior ? 0.975f : 0.025f;
        float radio = RadioBarril(perfil, t, perfil.Diametro * 0.5f, 0, semilla) - perfil.Diametro * 0.045f;
        float grosor = Mathf.Clamp(perfil.Altura * 0.025f, 0.018f, 0.027f);
        float y = superior ? perfil.Altura - perfil.HundimientoTapa : 0.012f;
        float paso = radio * 2f / tablones;

        for (int i = 0; i < tablones; i++)
        {
            float x0 = -radio + i * paso + 0.0015f;
            float x1 = -radio + (i + 1) * paso - 0.0015f;
            float borde = Mathf.Max(Mathf.Abs(x0), Mathf.Abs(x1));
            float mitadZ = Mathf.Sqrt(Mathf.Max(0.001f, radio * radio - borde * borde));
            float ondulacion = perfil.Desgaste * 0.002f * Mathf.Sin(i * 8.7f + semilla);
            float yTablon = y + ondulacion;
            int inicio = vertices.Count;
            vertices.Add(new Vector3(x0, yTablon, -mitadZ));
            vertices.Add(new Vector3(x1, yTablon, -mitadZ));
            vertices.Add(new Vector3(x1, yTablon, mitadZ));
            vertices.Add(new Vector3(x0, yTablon, mitadZ));
            vertices.Add(new Vector3(x0, yTablon - grosor, -mitadZ));
            vertices.Add(new Vector3(x1, yTablon - grosor, -mitadZ));
            vertices.Add(new Vector3(x1, yTablon - grosor, mitadZ));
            vertices.Add(new Vector3(x0, yTablon - grosor, mitadZ));
            uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f)); uv.Add(new Vector2(1f, 1f)); uv.Add(new Vector2(0f, 1f));
            uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f)); uv.Add(new Vector2(1f, 1f)); uv.Add(new Vector2(0f, 1f));
            List<int> triangulos = submallas[i % 3];
            triangulos.Add(inicio); triangulos.Add(inicio + 2); triangulos.Add(inicio + 1);
            triangulos.Add(inicio); triangulos.Add(inicio + 3); triangulos.Add(inicio + 2);
            triangulos.Add(inicio + 4); triangulos.Add(inicio + 5); triangulos.Add(inicio + 6);
            triangulos.Add(inicio + 4); triangulos.Add(inicio + 6); triangulos.Add(inicio + 7);
            AgregarCuadrilatero(triangulos, inicio, inicio + 1, inicio + 4, inicio + 5);
            AgregarCuadrilatero(triangulos, inicio + 1, inicio + 2, inicio + 5, inicio + 6);
            AgregarCuadrilatero(triangulos, inicio + 2, inicio + 3, inicio + 6, inicio + 7);
            AgregarCuadrilatero(triangulos, inicio + 3, inicio, inicio + 7, inicio + 4);
        }

        Mesh mesh = new Mesh { name = superior ? "Malla_TapaSuperior_" + perfil.Nombre : "Malla_TapaInferior_" + perfil.Nombre };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.subMeshCount = submallas.Length;
        for (int i = 0; i < submallas.Length; i++) mesh.SetTriangles(submallas[i], i);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static float RadioBarril(ConfiguracionBarril perfil, float t, float radio, int semilla, int duela)
    {
        float barriga = Mathf.Sin(t * Mathf.PI);
        float perfilBase = 0.84f + 0.16f * barriga;
        float asimetria = perfil.Deformacion * 0.22f * Mathf.Sin(t * Mathf.PI * 2f + semilla * 0.2f);
        float variacionDuelas = perfil.Deformacion * 0.12f * Mathf.Sin(duela * 1.7f + t * 12f + semilla);
        return radio * (perfilBase + asimetria + variacionDuelas);
    }

    private static Vector3 PuntoBarril(ConfiguracionBarril perfil, float t, float angulo, float radio, int indice)
    {
        float centroX = perfil.Inclinacion * t + perfil.Deformacion * 0.12f * Mathf.Sin(t * Mathf.PI * 2f + indice * 0.13f);
        float centroZ = perfil.Deformacion * 0.08f * Mathf.Sin(t * Mathf.PI * 2f + 1.2f);
        return new Vector3(centroX + Mathf.Cos(angulo) * radio, t * perfil.Altura, centroZ + Mathf.Sin(angulo) * radio);
    }

    private static Texture2D CargarOCrearTexturaBarril(string nombre, int semilla, int tipo)
    {
        string ruta = BarrilesMaterialesCarpeta + "/" + nombre;
        Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        bool crearTextura = textura == null;

        const int resolucion = 128;
        if (crearTextura)
        {
            textura = new Texture2D(resolucion, resolucion, TextureFormat.RGBA32, true, tipo == 2 || tipo >= 3)
            {
                name = System.IO.Path.GetFileNameWithoutExtension(nombre),
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
        }
        Color[] pixeles = new Color[resolucion * resolucion];
        for (int y = 0; y < resolucion; y++)
        {
            for (int x = 0; x < resolucion; x++)
            {
                float veta = Mathf.PerlinNoise((x + semilla) * 0.025f, (y + semilla) * 0.13f);
                float fibra = Mathf.PerlinNoise((x + semilla * 2) * 0.09f, (y + semilla) * 0.21f);
                float nudo = Mathf.PerlinNoise((x + semilla) * 0.045f, (y - semilla) * 0.045f);
                if (tipo == 0)
                {
                    float valor = Mathf.Clamp01(0.77f + (veta - 0.5f) * 0.42f + (fibra - 0.5f) * 0.18f);
                    if (nudo > 0.72f) valor *= 0.78f;
                    if (fibra < 0.24f) valor *= 0.86f;
                    pixeles[y * resolucion + x] = new Color(valor, valor * 0.94f, valor * 0.87f, 1f);
                }
                else if (tipo == 1)
                {
                    float oxido = Mathf.InverseLerp(0.55f, 0.82f, nudo) * Mathf.Clamp01((fibra + veta) * 0.7f);
                    float oscuro = 0.22f + fibra * 0.24f;
                    Color hierroColor = Color.Lerp(new Color(oscuro * 0.68f, oscuro * 0.61f, oscuro * 0.53f), new Color(0.43f, 0.22f, 0.11f), oxido * 0.7f);
                    pixeles[y * resolucion + x] = new Color(hierroColor.r, hierroColor.g, hierroColor.b, 1f);
                }
                else if (tipo == 2)
                {
                    float dx = Mathf.PerlinNoise((x + 1 + semilla) * 0.045f, (y + semilla) * 0.08f)
                        - Mathf.PerlinNoise((x - 1 + semilla) * 0.045f, (y + semilla) * 0.08f);
                    float dy = Mathf.PerlinNoise((x + semilla) * 0.045f, (y + 1 + semilla) * 0.08f)
                        - Mathf.PerlinNoise((x + semilla) * 0.045f, (y - 1 + semilla) * 0.08f);
                    Vector3 normal = new Vector3(-dx * 3.2f, -dy * 3.2f, 1f).normalized;
                    pixeles[y * resolucion + x] = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
                }
                else
                {
                    float suavidad = tipo == 3 ? 0.16f + veta * 0.1f + fibra * 0.04f : 0.2f + veta * 0.14f + fibra * 0.08f;
                    pixeles[y * resolucion + x] = new Color(tipo == 4 ? 0.66f : 0.0f, 0f, 0f, Mathf.Clamp01(suavidad));
                }
            }
        }
        textura.SetPixels(pixeles);
        textura.Apply(true, false);
        if (crearTextura) AssetDatabase.CreateAsset(textura, ruta);
        else EditorUtility.SetDirty(textura);
        return textura;
    }

    private static Material CargarOCrearMaterialBarril(string nombre, Color color, Texture2D textura,
        Texture2D normal, Texture2D mapaSuavidad, bool esHierro)
    {
        string ruta = BarrilesMaterialesCarpeta + "/" + nombre + ".mat";
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
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", textura);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", textura);
        if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", normal);
        if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", esHierro ? 0.24f : 0.38f);
        if (material.HasProperty("_MetallicGlossMap")) material.SetTexture("_MetallicGlossMap", mapaSuavidad);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", esHierro ? 0.64f : 0f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", esHierro ? 0.27f : 0.19f);
        material.EnableKeyword("_NORMALMAP");
        material.EnableKeyword("_METALLICGLOSSMAP");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Mesh GuardarMallaBarril(Mesh malla, string nombreArchivo)
    {
        string ruta = BarrilesMallasCarpeta + "/" + nombreArchivo;
        Mesh existente = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        if (existente == null)
        {
            AssetDatabase.CreateAsset(malla, ruta);
            return malla;
        }

        EditorUtility.CopySerialized(malla, existente);
        Object.DestroyImmediate(malla);
        EditorUtility.SetDirty(existente);
        return existente;
    }

    private static void CrearEmbutidosFaltantes(bool recrear)
    {
        AsegurarCarpeta(EmbutidosCarpeta);
        AsegurarCarpeta(EmbutidosMaterialesCarpeta);
        AsegurarCarpeta(EmbutidosMallasCarpeta);

        Texture2D texturaChorizo = CargarOCrearTexturaEmbutido("Textura_Chorizo.asset", 31, false);
        Texture2D texturaJamon = CargarOCrearTexturaEmbutido("Textura_Jamon.asset", 73, true);
        Texture2D texturaCuerda = CargarOCrearTexturaEmbutido("Textura_Yute.asset", 119, false);
        if (texturaChorizo == null || texturaJamon == null || texturaCuerda == null)
        {
            Debug.LogError("No se pudieron crear las texturas de la colección de embutidos.");
            return;
        }

        float[] longitudesChorizo = { 0.24f, 0.31f, 0.21f, 0.34f, 0.27f };
        float[] anchosChorizo = { 0.048f, 0.056f, 0.044f, 0.051f, 0.059f };
        float[] curvaturasChorizo = { 0.018f, -0.028f, 0.008f, 0.035f, -0.012f };
        Color[] coloresChorizo =
        {
            new Color(0.73f, 0.37f, 0.29f), new Color(0.62f, 0.27f, 0.21f),
            new Color(0.78f, 0.43f, 0.32f), new Color(0.57f, 0.25f, 0.2f),
            new Color(0.69f, 0.32f, 0.24f)
        };
        for (int i = 0; i < longitudesChorizo.Length; i++)
        {
            string nombre = "Chorizo_0" + (i + 1);
            CrearEmbutidoIndividual(nombre, false, longitudesChorizo[i], anchosChorizo[i],
                curvaturasChorizo[i], 0.82f + i * 0.045f, coloresChorizo[i], texturaChorizo, texturaCuerda, recrear);
        }

        float[] longitudesJamon = { 0.66f, 0.78f, 0.71f, 0.62f };
        float[] anchosJamon = { 0.28f, 0.31f, 0.25f, 0.30f };
        float[] curvaturasJamon = { 0.035f, -0.052f, 0.018f, -0.028f };
        Color[] coloresJamon =
        {
            new Color(0.62f, 0.39f, 0.29f), new Color(0.72f, 0.48f, 0.34f),
            new Color(0.55f, 0.32f, 0.25f), new Color(0.68f, 0.43f, 0.31f)
        };
        for (int i = 0; i < longitudesJamon.Length; i++)
        {
            string nombre = "Jamon_0" + (i + 1);
            CrearEmbutidoIndividual(nombre, true, longitudesJamon[i], anchosJamon[i],
                curvaturasJamon[i], 1.17f + i * 0.11f, coloresJamon[i], texturaJamon, texturaCuerda, recrear);
        }
    }

    private static void CrearEmbutidoIndividual(string nombre, bool esJamon, float longitud, float ancho,
        float curvatura, float secado, Color color, Texture2D textura, Texture2D texturaCuerda, bool recrear)
    {
        string prefabPath = EmbutidosCarpeta + "/" + nombre + ".prefab";
        if (!recrear && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        Material material = CargarOCrearMaterialEmbutido(nombre, color, textura);
        Material materialCuerda = CargarOCrearMaterialEmbutido("Mat_Yute", new Color(0.67f, 0.53f, 0.36f), texturaCuerda);
        if (material == null || materialCuerda == null) return;

        int semilla = nombre.GetHashCode();
        Mesh mallaCarne = CrearMallaOrganica(nombre + "Mesh", longitud, ancho, curvatura, esJamon, semilla, secado);
        Mesh mallaCarneAsset = GuardarMallaEmbutido(mallaCarne, nombre + "Mesh.asset");
        Mesh mallaCuerda = CrearMallaCuerda(nombre + "CuerdaMesh", longitud, ancho, esJamon, semilla);
        Mesh mallaCuerdaAsset = GuardarMallaEmbutido(mallaCuerda, nombre + "CuerdaMesh.asset");
        if (mallaCarneAsset == null || mallaCuerdaAsset == null) return;

        GameObject raiz = new GameObject(nombre);
        GameObject alimento = new GameObject(esJamon ? "JamonMesh" : "ChorizoMesh");
        alimento.transform.SetParent(raiz.transform, false);
        alimento.AddComponent<MeshFilter>().sharedMesh = mallaCarneAsset;
        alimento.AddComponent<MeshRenderer>().sharedMaterial = material;

        GameObject cuerda = new GameObject("Cuerda");
        cuerda.transform.SetParent(raiz.transform, false);
        cuerda.AddComponent<MeshFilter>().sharedMesh = mallaCuerdaAsset;
        cuerda.AddComponent<MeshRenderer>().sharedMaterial = materialCuerda;

        GuardarPrefab(raiz, prefabPath);
    }

    private static Mesh CrearMallaOrganica(string nombre, float longitud, float ancho, float curvatura,
        bool esJamon, int semilla, float secado)
    {
        const int anillos = 36;
        const int segmentos = 20;
        List<Vector3> vertices = new List<Vector3>((anillos + 1) * segmentos + 2);
        List<Vector2> uv = new List<Vector2>(vertices.Capacity);
        List<int> triangulos = new List<int>(anillos * segmentos * 6 + segmentos * 6);
        float profundidad = ancho * (esJamon ? 0.72f : 0.92f);

        for (int anillo = 0; anillo <= anillos; anillo++)
        {
            float t = anillo / (float)anillos;
            float y = (0.5f - t) * longitud;
            float centroX = curvatura * Mathf.Sin(t * Mathf.PI) + curvatura * 0.22f * Mathf.Sin(t * Mathf.PI * 2f + semilla * 0.001f);
            float perfil;
            if (esJamon)
            {
                float hombro = Mathf.Exp(-Mathf.Pow((t - 0.34f) / 0.34f, 2f));
                perfil = 0.34f + 0.66f * hombro;
                perfil *= 0.94f + 0.06f * Mathf.Sin(t * Mathf.PI * 1.4f + semilla);
            }
            else
            {
                perfil = 0.82f + 0.13f * Mathf.Sin(t * Mathf.PI) + 0.045f * Mathf.Sin(t * Mathf.PI * 3f + semilla);
            }

            for (int segmento = 0; segmento < segmentos; segmento++)
            {
                float angulo = segmento * Mathf.PI * 2f / segmentos;
                float ondulacion = 1f
                    + 0.025f * Mathf.Sin(angulo * 4f + t * 43f + semilla)
                    + 0.018f * Mathf.Sin(angulo * 7f - t * 31f + semilla * 0.37f)
                    + (0.012f + secado * 0.006f) * Mathf.Sin(t * 90f + angulo * 2f + semilla);
                float radioX = ancho * 0.5f * perfil * ondulacion;
                float radioZ = profundidad * 0.5f * perfil * ondulacion;
                vertices.Add(new Vector3(centroX + Mathf.Cos(angulo) * radioX, y, Mathf.Sin(angulo) * radioZ));
                uv.Add(new Vector2(segmento / (float)segmentos, t * 2f));
            }
        }

        for (int anillo = 0; anillo < anillos; anillo++)
        {
            for (int segmento = 0; segmento < segmentos; segmento++)
            {
                int actual = anillo * segmentos + segmento;
                int siguiente = anillo * segmentos + (segmento + 1) % segmentos;
                int inferior = actual + segmentos;
                int inferiorSiguiente = siguiente + segmentos;
                triangulos.Add(actual); triangulos.Add(siguiente); triangulos.Add(inferior);
                triangulos.Add(siguiente); triangulos.Add(inferiorSiguiente); triangulos.Add(inferior);
            }
        }

        int centroSuperior = vertices.Count;
        vertices.Add(new Vector3(curvatura * 0.22f * Mathf.Sin(semilla * 0.001f), longitud * 0.5f, 0f));
        uv.Add(new Vector2(0.5f, 0f));
        int centroInferior = vertices.Count;
        vertices.Add(new Vector3(0f, -longitud * 0.5f, 0f));
        uv.Add(new Vector2(0.5f, 1f));
        for (int segmento = 0; segmento < segmentos; segmento++)
        {
            int siguiente = (segmento + 1) % segmentos;
            triangulos.Add(centroSuperior); triangulos.Add(siguiente); triangulos.Add(segmento);
            int baseInferior = anillos * segmentos;
            triangulos.Add(centroInferior); triangulos.Add(baseInferior + segmento); triangulos.Add(baseInferior + siguiente);
        }

        Mesh mesh = new Mesh { name = nombre };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CrearMallaCuerda(string nombre, float longitud, float ancho, bool esJamon, int semilla)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> triangulos = new List<int>();
        float radioComida = ancho * (esJamon ? 0.26f : 0.33f);
        float yAtadura = longitud * 0.5f - (esJamon ? 0.035f : 0.012f);
        AgregarTuboCuerda(vertices, uv, triangulos, new[]
        {
            new Vector3(0f, yAtadura - 0.035f, 0f), new Vector3(0f, yAtadura + 0.01f, 0f),
            new Vector3(0.012f, yAtadura + 0.07f, 0f), new Vector3(0f, yAtadura + 0.13f, 0f)
        }, esJamon ? 0.009f : 0.0045f, semilla);

        List<Vector3> vuelta = new List<Vector3>();
        for (int i = 0; i <= 32; i++)
        {
            float angulo = i * Mathf.PI * 2f / 32f;
            vuelta.Add(new Vector3(Mathf.Cos(angulo) * radioComida, yAtadura, Mathf.Sin(angulo) * radioComida));
        }
        AgregarTuboCuerda(vertices, uv, triangulos, vuelta, esJamon ? 0.009f : 0.0045f, semilla + 11);

        float radioLazo = esJamon ? 0.065f : 0.037f;
        float alturaLazo = yAtadura + (esJamon ? 0.19f : 0.12f);
        List<Vector3> lazo = new List<Vector3>();
        for (int i = 0; i <= 36; i++)
        {
            float angulo = i * Mathf.PI * 2f / 36f;
            lazo.Add(new Vector3(Mathf.Sin(angulo) * radioLazo, alturaLazo + Mathf.Cos(angulo) * radioLazo * 1.2f,
                Mathf.Sin(angulo * 2f) * radioLazo * 0.12f));
        }
        AgregarTuboCuerda(vertices, uv, triangulos, lazo, esJamon ? 0.009f : 0.005f, semilla + 23);

        for (int cola = 0; cola < 2; cola++)
        {
            float lado = cola == 0 ? -1f : 1f;
            AgregarTuboCuerda(vertices, uv, triangulos, new[]
            {
                new Vector3(0f, yAtadura + 0.015f, 0f),
                new Vector3(lado * radioComida * 0.55f, yAtadura + 0.035f, radioComida * 0.65f),
                new Vector3(lado * radioComida * 0.82f, yAtadura + 0.005f, radioComida * 0.35f)
            }, esJamon ? 0.008f : 0.004f, semilla + cola * 7);
        }

        Mesh mesh = new Mesh { name = nombre };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AgregarTuboCuerda(List<Vector3> vertices, List<Vector2> uv, List<int> triangulos,
        IList<Vector3> puntos, float radio, int semilla)
    {
        const int lados = 7;
        if (puntos.Count < 2) return;
        int inicio = vertices.Count;
        for (int anillo = 0; anillo < puntos.Count; anillo++)
        {
            Vector3 tangente = (puntos[Mathf.Min(anillo + 1, puntos.Count - 1)] - puntos[Mathf.Max(anillo - 1, 0)]).normalized;
            Vector3 referencia = Mathf.Abs(Vector3.Dot(tangente, Vector3.up)) > 0.92f ? Vector3.forward : Vector3.up;
            Vector3 ejeA = Vector3.Cross(tangente, referencia).normalized;
            Vector3 ejeB = Vector3.Cross(tangente, ejeA).normalized;
            for (int lado = 0; lado < lados; lado++)
            {
                float angulo = lado * Mathf.PI * 2f / lados;
                float fibra = 1f + 0.07f * Mathf.Sin(angulo * 3f + anillo * 0.9f + semilla);
                vertices.Add(puntos[anillo] + (ejeA * Mathf.Cos(angulo) + ejeB * Mathf.Sin(angulo)) * radio * fibra);
                uv.Add(new Vector2(lado / (float)lados, anillo * 0.35f));
            }
        }

        for (int anillo = 0; anillo < puntos.Count - 1; anillo++)
        {
            for (int lado = 0; lado < lados; lado++)
            {
                int a = inicio + anillo * lados + lado;
                int b = inicio + anillo * lados + (lado + 1) % lados;
                int c = a + lados;
                int d = b + lados;
                triangulos.Add(a); triangulos.Add(b); triangulos.Add(c);
                triangulos.Add(b); triangulos.Add(d); triangulos.Add(c);
            }
        }
    }

    private static Texture2D CargarOCrearTexturaEmbutido(string nombre, int semilla, bool grasa)
    {
        string ruta = EmbutidosMaterialesCarpeta + "/" + nombre;
        Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (textura != null) return textura;

        const int resolucion = 128;
        textura = new Texture2D(resolucion, resolucion, TextureFormat.RGBA32, true) { name = System.IO.Path.GetFileNameWithoutExtension(nombre) };
        Color[] pixeles = new Color[resolucion * resolucion];
        for (int y = 0; y < resolucion; y++)
        {
            for (int x = 0; x < resolucion; x++)
            {
                float amplio = Mathf.PerlinNoise((x + semilla) * 0.055f, (y + semilla) * 0.055f);
                float fino = Mathf.PerlinNoise((x + semilla * 2) * 0.19f, (y + semilla * 3) * 0.19f);
                float manchas = Mathf.PerlinNoise((x + semilla) * 0.025f, (y - semilla) * 0.025f);
                float valor = Mathf.Clamp01(0.7f + (amplio - 0.5f) * 0.28f + (fino - 0.5f) * 0.13f);
                if (grasa && manchas > 0.68f) valor = Mathf.Lerp(valor, 0.97f, (manchas - 0.68f) * 1.8f);
                if (manchas < 0.27f) valor *= 0.88f;
                pixeles[y * resolucion + x] = new Color(valor, valor * (grasa ? 0.96f : 0.91f), valor * (grasa ? 0.87f : 0.84f), 1f);
            }
        }
        textura.SetPixels(pixeles);
        textura.wrapMode = TextureWrapMode.Repeat;
        textura.filterMode = FilterMode.Bilinear;
        textura.Apply(true, false);
        AssetDatabase.CreateAsset(textura, ruta);
        return textura;
    }

    private static Material CargarOCrearMaterialEmbutido(string nombre, Color color, Texture2D textura)
    {
        string ruta = EmbutidosMaterialesCarpeta + "/Mat_" + nombre + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;
            material = new Material(shader) { name = "Mat_" + nombre };
            AssetDatabase.CreateAsset(material, ruta);
        }

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", textura);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", textura);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.16f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Mesh GuardarMallaEmbutido(Mesh malla, string nombreArchivo)
    {
        string ruta = EmbutidosMallasCarpeta + "/" + nombreArchivo;
        Mesh existente = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        if (existente == null)
        {
            AssetDatabase.CreateAsset(malla, ruta);
            return malla;
        }

        EditorUtility.CopySerialized(malla, existente);
        Object.DestroyImmediate(malla);
        EditorUtility.SetDirty(existente);
        return existente;
    }

    private static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = System.IO.Path.GetDirectoryName(ruta).Replace('\\', '/');
        string nombre = System.IO.Path.GetFileName(ruta);
        if (!AssetDatabase.IsValidFolder(padre)) AsegurarCarpeta(padre);
        AssetDatabase.CreateFolder(padre, nombre);
    }

    private static void CrearEstanteriaEpoca()
    {
        Material madera = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Material metal = CargarOCrearMaterial(HerrajesPath, new Color(0.075f, 0.065f, 0.055f), 0.78f, 0.3f);
        Material cristal = CargarOCrearMaterial(FarolVidrioPath, new Color(0.7f, 0.9f, 1f, 0.25f), 0f, 0.3f);
        if (madera == null || metal == null || cristal == null)
        {
            Debug.LogError("Faltan materiales para crear la estantería de época.");
            return;
        }

        GameObject estanteria = new GameObject("EstanteriaEpoca");
        estanteria.transform.position = Vector3.zero;

        CrearCaja(estanteria.transform, "Base_Madera", new Vector3(0f, 0.1f, 0f), new Vector3(2.4f, 0.14f, 0.7f), madera);
        CrearCaja(estanteria.transform, "Lado_Izquierdo", new Vector3(-1.15f, 2.0f, 0f), new Vector3(0.1f, 4.1f, 0.58f), madera);
        CrearCaja(estanteria.transform, "Lado_Derecho", new Vector3(1.15f, 2.0f, 0f), new Vector3(0.1f, 4.1f, 0.58f), madera);
        CrearCaja(estanteria.transform, "Trasera", new Vector3(0f, 2.0f, -0.28f), new Vector3(2.15f, 4.0f, 0.08f), madera);

        float[] alturas = { 0.6f, 1.5f, 2.4f, 3.3f };
        for (int i = 0; i < alturas.Length; i++)
        {
            float y = alturas[i];
            CrearCaja(estanteria.transform, "Estante_" + (i + 1), new Vector3(0f, y, 0f), new Vector3(2.1f, 0.08f, 0.48f), madera);
            CrearCaja(estanteria.transform, "Borde_" + (i + 1), new Vector3(0f, y, 0.22f), new Vector3(1.9f, 0.04f, 0.04f), metal);

            for (int j = 0; j < 4; j++)
            {
                float x = -0.7f + j * 0.46f;
                GameObject slot = new GameObject("Slot_" + (i + 1) + "_" + (j + 1));
                slot.transform.SetParent(estanteria.transform, false);
                slot.transform.localPosition = new Vector3(x, y + 0.14f, 0.08f);
                slot.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

                GameObject botella = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                botella.name = "Botella_Vacia";
                botella.transform.SetParent(slot.transform, false);
                botella.transform.localPosition = Vector3.zero;
                botella.transform.localScale = new Vector3(0.14f, 0.24f, 0.14f);
                botella.GetComponent<Renderer>().sharedMaterial = cristal;
            }
        }

        CrearCaja(estanteria.transform, "Corona_Superior", new Vector3(0f, 3.95f, 0f), new Vector3(2.25f, 0.1f, 0.58f), madera);
        CrearCaja(estanteria.transform, "Perno_Izquierdo", new Vector3(-0.9f, 3.95f, 0.12f), new Vector3(0.08f, 0.2f, 0.06f), metal);
        CrearCaja(estanteria.transform, "Perno_Derecho", new Vector3(0.9f, 3.95f, 0.12f), new Vector3(0.08f, 0.2f, 0.06f), metal);

        GameObject soporte = new GameObject("Soporte_Central");
        soporte.transform.SetParent(estanteria.transform, false);
        soporte.transform.localPosition = new Vector3(0f, 2.0f, 0.0f);
        CrearCaja(soporte.transform, "Columna_Central", new Vector3(0f, 0f, 0f), new Vector3(0.12f, 4.0f, 0.12f), madera);

        GuardarPrefab(estanteria, EstanteriaEpocaPath);
        Debug.Log("Prefab EstanteriaEpoca actualizado a versión alta y espaciosa.");
    }

    private static void CrearCandelabroEpoca()
    {
        Material metal = CargarOCrearMaterial(HerrajesPath, new Color(0.12f, 0.10f, 0.08f), 0.86f, 0.36f);
        Material fuego = CargarOCrearMaterial(FuegoPath, new Color(0.95f, 0.18f, 0.025f), 0f, 0.2f, new Color(1f, 0.72f, 0.35f) * 1.5f);
        Material materialVela = CargarOCrearMaterial(InteriorPath, new Color(0.96f, 0.92f, 0.72f), 0f, 0.1f);
        if (metal == null || fuego == null || materialVela == null)
        {
            Debug.LogError("Faltan materiales para crear el candelabro de época.");
            return;
        }

        GameObject candelabro = new GameObject("CandelabroPulperia");
        candelabro.transform.position = Vector3.zero;

        GameObject argollaSuperior = new GameObject("ArgollaSuperior");
        argollaSuperior.transform.SetParent(candelabro.transform, false);
        argollaSuperior.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        CrearCilindro(argollaSuperior.transform, "Aro_Superior", Vector3.zero, new Vector3(0.25f, 0.06f, 0.25f), metal);

        GameObject ganchoSuperior = new GameObject("GanchoSuperior");
        ganchoSuperior.transform.SetParent(candelabro.transform, false);
        ganchoSuperior.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        CrearVigaEntre(ganchoSuperior.transform, "Gancho", new Vector3(0f, 0f, 0f), new Vector3(0f, -0.12f, 0f), 0.03f, metal);

        for (int cadena = 0; cadena < 3; cadena++)
        {
            float angulo = cadena * (120f * Mathf.Deg2Rad);
            GameObject cadenaPadre = new GameObject("Cadena_0" + (cadena + 1));
            cadenaPadre.transform.SetParent(candelabro.transform, false);

            for (int eslabon = 0; eslabon < 10; eslabon++)
            {
                GameObject link = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                link.name = "Eslabon_" + eslabon;
                link.transform.SetParent(cadenaPadre.transform, false);

                float radioX = 0.11f + ((eslabon % 2 == 0) ? 0.02f : 0f);
                float radioZ = 0.11f + ((eslabon % 2 == 0) ? 0f : 0.02f);
                float altura = -0.15f - eslabon * 0.08f;
                Vector3 posicion = new Vector3(Mathf.Cos(angulo) * 0.18f, altura, Mathf.Sin(angulo) * 0.18f);
                link.transform.localPosition = posicion;
                link.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                link.transform.localScale = new Vector3(radioX, 0.045f, radioZ);
                link.GetComponent<Renderer>().sharedMaterial = metal;
            }
        }

        GameObject aro = new GameObject("AroPrincipal");
        aro.transform.SetParent(candelabro.transform, false);
        aro.transform.localPosition = new Vector3(0f, -0.55f, 0f);

        int segmentosAro = 24;
        for (int segmento = 0; segmento < segmentosAro; segmento++)
        {
            float angulo = segmento * 360f / segmentosAro * Mathf.Deg2Rad;
            float radio = 0.55f + Mathf.Sin(segmento * 1.7f) * 0.01f;
            float rotaY = (segmento * 360f / segmentosAro) + 90f;
            Vector3 posicion = new Vector3(Mathf.Cos(angulo) * radio, 0f, Mathf.Sin(angulo) * radio);
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = "Segmento_" + segmento;
            segment.transform.SetParent(aro.transform, false);
            segment.transform.localPosition = posicion;
            segment.transform.localRotation = Quaternion.Euler(0f, rotaY, 0f);
            segment.transform.localScale = new Vector3(0.13f, 0.04f, 0.28f + (segmento % 3) * 0.02f);
            segment.GetComponent<Renderer>().sharedMaterial = metal;
        }

        for (int remache = 0; remache < 12; remache++)
        {
            float angulo = remache * 30f * Mathf.Deg2Rad;
            Vector3 posicion = new Vector3(Mathf.Cos(angulo) * 0.48f, 0.01f, Mathf.Sin(angulo) * 0.48f);
            GameObject remacheObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            remacheObj.name = "Remache_" + remache;
            remacheObj.transform.SetParent(aro.transform, false);
            remacheObj.transform.localPosition = posicion;
            remacheObj.transform.localScale = new Vector3(0.05f, 0.035f, 0.05f);
            remacheObj.GetComponent<Renderer>().sharedMaterial = metal;
        }

        GameObject centroInferior = new GameObject("CentroInferior");
        centroInferior.transform.SetParent(candelabro.transform, false);
        centroInferior.transform.localPosition = new Vector3(0f, -0.68f, 0f);
        CrearCilindro(centroInferior.transform, "Disco_Central", Vector3.zero, new Vector3(0.16f, 0.04f, 0.16f), metal);

        for (int brazo = 0; brazo < 6; brazo++)
        {
            float angulo = brazo * 60f * Mathf.Deg2Rad;
            GameObject brazoObjeto = new GameObject("Brazo_0" + (brazo + 1));
            brazoObjeto.transform.SetParent(candelabro.transform, false);

            Vector3 puntoA = new Vector3(Mathf.Cos(angulo) * 0.48f, -0.57f, Mathf.Sin(angulo) * 0.48f);
            Vector3 puntoB = new Vector3(Mathf.Cos(angulo) * 0.58f, -0.36f, Mathf.Sin(angulo) * 0.58f);
            Vector3 puntoC = new Vector3(Mathf.Cos(angulo) * 0.72f, -0.15f, Mathf.Sin(angulo) * 0.72f);
            Vector3 puntoD = new Vector3(Mathf.Cos(angulo) * 0.83f, -0.04f, Mathf.Sin(angulo) * 0.83f);

            CrearVigaEntre(brazoObjeto.transform, "Segmento_1", puntoA, puntoB, 0.04f, metal);
            CrearVigaEntre(brazoObjeto.transform, "Segmento_2", puntoB, puntoC, 0.04f, metal);
            CrearVigaEntre(brazoObjeto.transform, "Segmento_3", puntoC, puntoD, 0.04f, metal);

            Vector3 posicionPortavela = new Vector3(Mathf.Cos(angulo) * 0.79f, -0.02f, Mathf.Sin(angulo) * 0.79f);
            GameObject portavela = new GameObject("Portavela_0" + (brazo + 1));
            portavela.transform.SetParent(candelabro.transform, false);
            portavela.transform.localPosition = posicionPortavela;
            CrearCilindro(portavela.transform, "Plato", Vector3.zero, new Vector3(0.12f, 0.02f, 0.12f), metal);
            CrearCilindro(portavela.transform, "Soporte", new Vector3(0f, -0.07f, 0f), new Vector3(0.035f, 0.08f, 0.035f), metal);

            GameObject vela = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            vela.name = "Vela_0" + (brazo + 1);
            vela.transform.SetParent(candelabro.transform, false);
            vela.transform.localPosition = new Vector3(posicionPortavela.x, 0.08f, posicionPortavela.z);
            vela.transform.localScale = new Vector3(0.05f, 0.16f, 0.05f);
            vela.GetComponent<Renderer>().sharedMaterial = materialVela;

            GameObject llama = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            llama.name = "Llama_0" + (brazo + 1);
            llama.transform.SetParent(candelabro.transform, false);
            llama.transform.localPosition = new Vector3(posicionPortavela.x, 0.18f, posicionPortavela.z);
            llama.transform.localScale = new Vector3(0.05f, 0.08f, 0.05f);
            llama.GetComponent<Renderer>().sharedMaterial = fuego;

            Light luz = llama.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = new Color(1f, 0.68f, 0.28f);
            luz.intensity = 0.7f;
            luz.range = 1.8f;
            luz.shadows = LightShadows.None;
        }

        GuardarPrefab(candelabro, CandelabroPulperiaPath);
        Debug.Log("Prefab CandelabroPulperia actualizado para una pulpería rural con aro pesado, 6 brazos y 3 cadenas.");
    }

    private static void CrearVentanaPulperia(bool recrear = false)
    {
        Material madera = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Material hierro = CargarOCrearMaterial(HerrajesPath, new Color(0.075f, 0.065f, 0.055f), 0.78f, 0.3f);
        Material vidrio = ObtenerMaterialVidrioVentana();
        if (madera == null || hierro == null || vidrio == null)
        {
            Debug.LogError("Faltan materiales para crear la ventana de pulpería.");
            return;
        }

        if (recrear || AssetDatabase.LoadAssetAtPath<GameObject>(VentanaPulperiaPath) == null)
        {
            GameObject ventana = new GameObject("VentanaPulperia");
            Transform marco = new GameObject("Marco").transform;
            marco.SetParent(ventana.transform, false);

            CrearCaja(marco, "Dintel", new Vector3(0f, 0.56f, 0f), new Vector3(1f, 0.08f, 0.2f), madera);
            CrearCaja(marco, "Umbral", new Vector3(0f, -0.56f, 0f), new Vector3(1f, 0.08f, 0.2f), madera);
            CrearCaja(marco, "Jamba_Izquierda", new Vector3(-0.46f, 0f, 0f), new Vector3(0.08f, 1.04f, 0.2f), madera);
            CrearCaja(marco, "Jamba_Derecha", new Vector3(0.46f, 0f, 0f), new Vector3(0.08f, 1.04f, 0.2f), madera);

            Transform vidrios = new GameObject("Seis_Paneles_de_Vidrio").transform;
            vidrios.SetParent(ventana.transform, false);
            for (int columna = 0; columna < 2; columna++)
            {
                for (int fila = 0; fila < 3; fila++)
                {
                    float x = columna == 0 ? -0.195f : 0.195f;
                    float y = 0.34f - fila * 0.34f;
                    CrearCaja(vidrios, "Vidrio_" + (fila * 2 + columna + 1), new Vector3(x, y, 0.025f), new Vector3(0.36f, 0.31f, 0.012f), vidrio);
                }
            }

            CrearCaja(marco, "Parante_Central", new Vector3(0f, 0f, 0.05f), new Vector3(0.035f, 0.96f, 0.1f), madera);
            for (int travesano = 0; travesano < 2; travesano++)
                CrearCaja(marco, "Travesano_Vidrio_" + (travesano + 1), new Vector3(0f, 0.17f - travesano * 0.34f, 0.05f), new Vector3(0.88f, 0.035f, 0.1f), madera);

            GuardarPrefab(ventana, VentanaPulperiaPath);
        }

        if (recrear || AssetDatabase.LoadAssetAtPath<GameObject>(PersianasMaderaPulperiaPath) == null)
        {
            GameObject persianas = new GameObject("PersianasMaderaPulperia");
            CrearVariantePersianas(persianas.transform, "Cerrada", madera, hierro, 0f, true);
            CrearVariantePersianas(persianas.transform, "Abierta", madera, hierro, 100f, false);
            CrearVariantePersianas(persianas.transform, "Entreabierta", madera, hierro, 42f, false);
            GuardarPrefab(persianas, PersianasMaderaPulperiaPath);
        }

        Debug.Log("Prefabs VentanaPulperia y PersianasMaderaPulperia actualizados.");
    }

    private static void CrearPuertaPulperia()
    {
        Material madera = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Material hierro = CargarOCrearMaterial(HerrajesPath, new Color(0.075f, 0.065f, 0.055f), 0.78f, 0.3f);
        if (madera == null || hierro == null)
        {
            Debug.LogError("Faltan materiales para crear la puerta de pulpería.");
            return;
        }

        GameObject puerta = new GameObject("PuertaPulperia");
        Transform marco = new GameObject("Marco").transform;
        marco.SetParent(puerta.transform, false);
        CrearCaja(marco, "MarcoIzquierdo", new Vector3(0.08f, 1.05f, 0f), new Vector3(0.14f, 2.1f, 0.17f), madera);
        CrearCaja(marco, "MarcoDerecho", new Vector3(1.12f, 1.05f, 0f), new Vector3(0.14f, 2.1f, 0.17f), madera);
        CrearCaja(marco, "Dintel", new Vector3(0.6f, 2.04f, 0f), new Vector3(1.2f, 0.14f, 0.18f), madera);

        Transform hoja = new GameObject("HojaPuerta").transform;
        hoja.SetParent(puerta.transform, false);
        hoja.localPosition = new Vector3(0.08f, 1.05f, 0f);

        const int cantidadTablas = 6;
        const float anchoTabla = 0.16f;
        for (int tabla = 0; tabla < cantidadTablas; tabla++)
        {
            float variacionX = (tabla % 2 == 0 ? -1f : 1f) * 0.004f;
            float variacionZ = (tabla % 3 - 1) * 0.003f;
            CrearPiezaPuerta(hoja, "Tabla_0" + (tabla + 1),
                new Vector3(0.08f + tabla * anchoTabla + variacionX, variacionX, variacionZ),
                new Vector3(anchoTabla - 0.006f, 1.98f - Mathf.Abs(variacionX) * 2f, 0.06f), madera);
        }

        CrearPiezaPuerta(hoja, "TravesañoSuperior", new Vector3(0.48f, 0.72f, 0.065f), new Vector3(0.9f, 0.1f, 0.045f), madera);
        CrearPiezaPuerta(hoja, "TravesañoInferior", new Vector3(0.48f, -0.72f, 0.065f), new Vector3(0.9f, 0.1f, 0.045f), madera);
        CrearBarraPuerta(hoja, "RefuerzoDiagonal", new Vector3(0.06f, -0.86f, 0.075f), new Vector3(0.9f, 0.86f, 0.075f), 0.1f, 0.05f, madera);

        for (int bisagra = 0; bisagra < 3; bisagra++)
        {
            float y = -0.88f + bisagra * 0.88f;
            GameObject bisagraMarco = new GameObject("Bisagra_0" + (bisagra + 1));
            bisagraMarco.transform.SetParent(puerta.transform, false);
            CrearPiezaPuerta(bisagraMarco.transform, "Barril", new Vector3(0.08f, y + 1.05f, 0.025f), new Vector3(0.055f, 0.12f, 0.055f), hierro);
            CrearPiezaPuerta(bisagraMarco.transform, "Placa_Marco", new Vector3(0.015f, y + 1.05f, 0.02f), new Vector3(0.13f, 0.09f, 0.025f), hierro);

            CrearPiezaPuerta(hoja, "Brazo_Bisagra_0" + (bisagra + 1), new Vector3(0.17f, y, 0.095f), new Vector3(0.2f, 0.055f, 0.025f), hierro);
            CrearRemachePuerta(hoja, "Remache_Bisagra_0" + (bisagra + 1) + "A", new Vector3(0.12f, y, 0.112f), hierro);
            CrearRemachePuerta(hoja, "Remache_Bisagra_0" + (bisagra + 1) + "B", new Vector3(0.22f, y, 0.112f), hierro);
        }

        CrearPiezaPuerta(hoja, "Placa_Cerrojo", new Vector3(0.82f, 0.05f, 0.095f), new Vector3(0.18f, 0.14f, 0.025f), hierro);
        CrearBarraPuerta(hoja, "Cerrojo", new Vector3(0.77f, 0.08f, 0.12f), new Vector3(0.98f, 0.08f, 0.12f), 0.035f, 0.035f, hierro);
        CrearRemachePuerta(hoja, "Remache_Cerrojo_1", new Vector3(0.78f, 0.02f, 0.115f), hierro);
        CrearRemachePuerta(hoja, "Remache_Cerrojo_2", new Vector3(0.88f, 0.02f, 0.115f), hierro);

        CrearPiezaPuerta(hoja, "Placa_Manijon", new Vector3(0.78f, -0.1f, 0.1f), new Vector3(0.1f, 0.16f, 0.025f), hierro);
        CrearBarraPuerta(hoja, "Manijon_Lateral", new Vector3(0.81f, -0.16f, 0.16f), new Vector3(0.81f, -0.31f, 0.16f), 0.028f, 0.028f, hierro);
        CrearBarraPuerta(hoja, "Manijon_Inferior", new Vector3(0.81f, -0.31f, 0.16f), new Vector3(0.91f, -0.31f, 0.16f), 0.028f, 0.028f, hierro);
        CrearBarraPuerta(hoja, "Manijon_Superior", new Vector3(0.91f, -0.31f, 0.16f), new Vector3(0.91f, -0.16f, 0.16f), 0.028f, 0.028f, hierro);

        BoxCollider hojaCollider = hoja.gameObject.AddComponent<BoxCollider>();
        hojaCollider.center = new Vector3(0.48f, 0f, 0f);
        hojaCollider.size = new Vector3(0.96f, 2f, 0.12f);

        PuertaPulperiaControlador controlador = puerta.AddComponent<PuertaPulperiaControlador>();
        controlador.Configurar(hoja);

        GuardarPrefab(puerta, PuertaPulperiaPath);
    }

    private static void CrearPiezaPuerta(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material)
    {
        GameObject pieza = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pieza.name = nombre;
        pieza.transform.SetParent(padre, false);
        pieza.transform.localPosition = posicion;
        pieza.transform.localScale = escala;
        pieza.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(pieza.GetComponent<Collider>());
    }

    private static void CrearBarraPuerta(Transform padre, string nombre, Vector3 inicio, Vector3 fin, float ancho, float profundidad, Material material)
    {
        GameObject barra = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barra.name = nombre;
        barra.transform.SetParent(padre, false);
        Vector3 direccion = fin - inicio;
        barra.transform.localPosition = (inicio + fin) * 0.5f;
        barra.transform.localRotation = Quaternion.FromToRotation(Vector3.right, direccion.normalized);
        barra.transform.localScale = new Vector3(direccion.magnitude, ancho, profundidad);
        barra.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(barra.GetComponent<Collider>());
    }

    private static void CrearRemachePuerta(Transform padre, string nombre, Vector3 posicion, Material hierro)
    {
        GameObject remache = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        remache.name = nombre;
        remache.transform.SetParent(padre, false);
        remache.transform.localPosition = posicion;
        remache.transform.localScale = Vector3.one * 0.025f;
        remache.GetComponent<Renderer>().sharedMaterial = hierro;
        Object.DestroyImmediate(remache.GetComponent<Collider>());
    }

    private static void CrearVariantePersianas(Transform padre, string nombre, Material madera, Material hierro, float angulo, bool pestilloActivo)
    {
        GameObject variante = new GameObject("Variante_" + nombre);
        variante.transform.SetParent(padre, false);
        variante.SetActive(nombre == "Cerrada");

        CrearHojaPersiana(variante.transform, "Hoja_Izquierda", madera, hierro, -0.42f, 1f, -angulo);
        CrearHojaPersiana(variante.transform, "Hoja_Derecha", madera, hierro, 0.42f, -1f, angulo);

        if (pestilloActivo)
        {
            CrearCaja(variante.transform, "Pestillo_Central", new Vector3(0f, 0f, 0.19f), new Vector3(0.16f, 0.035f, 0.025f), hierro);
            CrearVigaEntre(variante.transform, "Eje_Pestillo", new Vector3(0f, -0.035f, 0.19f), new Vector3(0f, 0.035f, 0.19f), 0.025f, hierro);
        }
    }

    private static void CrearHojaPersiana(Transform padre, string nombre, Material madera, Material hierro, float xBisagra, float direccion, float angulo)
    {
        GameObject hoja = new GameObject(nombre);
        hoja.transform.SetParent(padre, false);
        hoja.transform.localPosition = new Vector3(xBisagra, 0f, 0.105f);
        hoja.transform.localRotation = Quaternion.Euler(0f, angulo, 0f);

        float ancho = 0.38f;
        CrearCaja(hoja.transform, "Travesano_Superior", new Vector3(direccion * ancho * 0.5f, 0.53f, 0f), new Vector3(ancho, 0.06f, 0.075f), madera);
        CrearCaja(hoja.transform, "Travesano_Inferior", new Vector3(direccion * ancho * 0.5f, -0.53f, 0f), new Vector3(ancho, 0.06f, 0.075f), madera);
        for (int liston = 0; liston < 9; liston++)
        {
            float y = -0.48f + liston * 0.12f;
            float inclinacion = (liston % 3 - 1) * 1.5f;
            CrearCaja(hoja.transform, "Liston_Horizontal_" + (liston + 1), new Vector3(direccion * ancho * 0.5f, y, 0.045f), new Vector3(ancho - 0.035f, 0.075f, 0.055f), madera);
            Transform listonObjeto = hoja.transform.Find("Liston_Horizontal_" + (liston + 1));
            listonObjeto.localRotation = Quaternion.Euler(0f, 0f, inclinacion);
        }

        CrearCaja(hoja.transform, "Montante_Bisagras", new Vector3(direccion * 0.035f, 0f, 0.05f), new Vector3(0.07f, 1.12f, 0.09f), madera);
        CrearCaja(hoja.transform, "Montante_Libre", new Vector3(direccion * (ancho - 0.035f), 0f, 0.05f), new Vector3(0.06f, 1.12f, 0.09f), madera);

        for (int bisagra = 0; bisagra < 3; bisagra++)
        {
            float y = -0.4f + bisagra * 0.4f;
            CrearCaja(hoja.transform, "Bisagra_Hierro_" + (bisagra + 1), new Vector3(direccion * 0.05f, y, 0.105f), new Vector3(0.18f, 0.035f, 0.018f), hierro);
            GameObject perno = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            perno.name = "Perno_Bisagra_" + (bisagra + 1);
            perno.transform.SetParent(hoja.transform, false);
            perno.transform.localPosition = new Vector3(0f, y, 0.12f);
            perno.transform.localScale = new Vector3(0.035f, 0.025f, 0.035f);
            perno.GetComponent<Renderer>().sharedMaterial = hierro;
        }
    }

    private static Material ObtenerMaterialVidrioVentana()
    {
        Material vidrio = AssetDatabase.LoadAssetAtPath<Material>(VentanaVidrioPath);
        if (vidrio == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;

            vidrio = new Material(shader);
            vidrio.name = "Mat_VentanaVidrio";
            AssetDatabase.CreateAsset(vidrio, VentanaVidrioPath);
        }

        Color tono = new Color(0.66f, 0.76f, 0.74f, 0.22f);
        if (vidrio.HasProperty("_BaseColor")) vidrio.SetColor("_BaseColor", tono);
        if (vidrio.HasProperty("_Color")) vidrio.SetColor("_Color", tono);
        if (vidrio.HasProperty("_Metallic")) vidrio.SetFloat("_Metallic", 0f);
        if (vidrio.HasProperty("_Smoothness")) vidrio.SetFloat("_Smoothness", 0.72f);
        if (vidrio.HasProperty("_Surface")) vidrio.SetFloat("_Surface", 1f);
        if (vidrio.HasProperty("_SrcBlend")) vidrio.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (vidrio.HasProperty("_DstBlend")) vidrio.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (vidrio.HasProperty("_ZWrite")) vidrio.SetFloat("_ZWrite", 0f);
        vidrio.SetOverrideTag("RenderType", "Transparent");
        vidrio.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        vidrio.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(vidrio);
        return vidrio;
    }

    private static void CrearFarolColganteDeAceite()
    {
        Material metal = ObtenerMaterialMolinete();
        Material fuego = CargarOCrearMaterial(FuegoPath, new Color(0.95f, 0.18f, 0.025f), 0f, 0.2f, new Color(1f, 0.12f, 0.015f) * 2f);
        Material interior = CargarOCrearMaterial(InteriorPath, new Color(0.035f, 0.018f, 0.012f), 0f, 0.1f);
        Material laton = CargarOCrearMaterial(FarolLatonPath, new Color(0.38f, 0.22f, 0.08f), 0.72f, 0.4f);
        Material vidrio = ObtenerMaterialVidrioFarol();
        if (metal == null || fuego == null || interior == null || laton == null || vidrio == null)
        {
            Debug.LogError("Faltan materiales para construir el farol colgante.");
            return;
        }

        GameObject farol = new GameObject("FarolColgante");

        CrearVigaEntre(farol.transform, "Gancho_de_Techo", new Vector3(0f, -0.02f, 0f), new Vector3(0f, -0.09f, 0f), 0.035f, metal);
        CrearVigaEntre(farol.transform, "Eslabon_de_Cadena_1", new Vector3(0f, -0.08f, 0f), new Vector3(0f, -0.15f, 0f), 0.025f, metal);
        CrearVigaEntre(farol.transform, "Eslabon_de_Cadena_2", new Vector3(0.015f, -0.14f, 0f), new Vector3(-0.015f, -0.21f, 0f), 0.025f, metal);
        CrearVigaEntre(farol.transform, "Eslabon_de_Cadena_3", new Vector3(0f, -0.2f, 0f), new Vector3(0f, -0.27f, 0f), 0.025f, metal);

        CrearVigaEntre(farol.transform, "Asa_Izquierda", new Vector3(-0.12f, -0.25f, 0f), new Vector3(-0.13f, -0.34f, 0f), 0.025f, metal);
        CrearVigaEntre(farol.transform, "Asa_Superior", new Vector3(-0.13f, -0.34f, 0f), new Vector3(0.13f, -0.34f, 0f), 0.025f, metal);
        CrearVigaEntre(farol.transform, "Asa_Derecha", new Vector3(0.13f, -0.34f, 0f), new Vector3(0.12f, -0.25f, 0f), 0.025f, metal);

        CrearCilindro(farol.transform, "Capuchon_Superior", new Vector3(0f, -0.37f, 0f), new Vector3(0.24f, 0.045f, 0.24f), metal);
        CrearEsfera(farol.transform, "Techo_del_Farol", new Vector3(0f, -0.395f, 0f), new Vector3(0.24f, 0.09f, 0.24f), metal);
        CrearCilindro(farol.transform, "Vaso_de_Vidrio", new Vector3(0f, -0.56f, 0f), new Vector3(0.17f, 0.18f, 0.17f), vidrio);
        CrearAroHorizontal(farol.transform, "Aro_Inferior", 0.22f, -0.68f, 12, 0.02f, metal);
        CrearAroHorizontal(farol.transform, "Aro_Superior", 0.22f, -0.44f, 12, 0.02f, metal);

        for (int angulo = 0; angulo < 360; angulo += 90)
        {
            float radianes = angulo * Mathf.Deg2Rad;
            Vector3 inicio = new Vector3(Mathf.Cos(radianes) * 0.1f, -0.69f, Mathf.Sin(radianes) * 0.1f);
            Vector3 fin = new Vector3(Mathf.Cos(radianes) * 0.1f, -0.43f, Mathf.Sin(radianes) * 0.1f);
            CrearVigaEntre(farol.transform, "Protector_de_Vidrio", inicio, fin, 0.025f, metal);
        }

        CrearCilindro(farol.transform, "Deposito_de_Aceite", new Vector3(0f, -0.75f, 0f), new Vector3(0.25f, 0.075f, 0.25f), metal);
        CrearCilindro(farol.transform, "Quemador_de_Laton", new Vector3(0f, -0.68f, 0f), new Vector3(0.16f, 0.045f, 0.16f), laton);
        CrearCilindro(farol.transform, "Mecha", new Vector3(0f, -0.64f, 0f), new Vector3(0.03f, 0.045f, 0.03f), interior);
        CrearLlama(farol.transform, "Llama_del_Farol", new Vector3(0f, -0.62f, 0.08f), 0.06f, 0.12f, fuego);
        CrearCaja(farol.transform, "Regulador_de_Mecha", new Vector3(0f, -0.69f, 0.14f), new Vector3(0.055f, 0.025f, 0.025f), laton);

        GameObject luzObjeto = new GameObject("Resplandor_del_Farol");
        luzObjeto.transform.SetParent(farol.transform, false);
        luzObjeto.transform.localPosition = new Vector3(0f, -0.56f, 0f);
        Light luz = luzObjeto.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.48f, 0.16f);
        luz.intensity = 0.45f;
        luz.range = 2.2f;
        luz.shadows = LightShadows.None;

        GuardarPrefab(farol, FarolColgantePath);
        Debug.Log("Prefab FarolColgante creado o actualizado.");
    }

    private static void CrearFarolDeAceite()
    {
        Material metal = ObtenerMaterialMolinete();
        Material fuego = AssetDatabase.LoadAssetAtPath<Material>(FuegoPath);
        Material interior = AssetDatabase.LoadAssetAtPath<Material>(InteriorPath);
        Material laton = CargarOCrearMaterial(FarolLatonPath, new Color(0.38f, 0.22f, 0.08f), 0.72f, 0.4f);
        Material vidrio = ObtenerMaterialVidrioFarol();
        if (metal == null || fuego == null || interior == null || laton == null || vidrio == null)
        {
            Debug.LogError("Faltan materiales para construir el farol de aceite.");
            return;
        }

        GameObject farol = new GameObject("FarolAceite");

        CrearCilindro(farol.transform, "Deposito_de_Aceite", new Vector3(0f, 0.16f, 0f), new Vector3(0.34f, 0.14f, 0.34f), metal);
        CrearCilindro(farol.transform, "Base_del_Quemador", new Vector3(0f, 0.27f, 0f), new Vector3(0.25f, 0.07f, 0.25f), laton);
        CrearCilindro(farol.transform, "Vaso_de_Vidrio", new Vector3(0f, 0.52f, 0f), new Vector3(0.235f, 0.25f, 0.235f), vidrio);
        CrearAroHorizontal(farol.transform, "Aro_Inferior_del_Cristal", 0.31f, 0.13f, 16, 0.025f, metal);
        CrearAroHorizontal(farol.transform, "Aro_Superior_del_Cristal", 0.31f, 0.72f, 16, 0.025f, metal);

        for (int angulo = 0; angulo < 360; angulo += 90)
        {
            float radianes = angulo * Mathf.Deg2Rad;
            Vector3 puntoInferior = new Vector3(Mathf.Cos(radianes) * 0.145f, 0.3f, Mathf.Sin(radianes) * 0.145f);
            Vector3 puntoSuperior = new Vector3(Mathf.Cos(radianes) * 0.145f, 0.73f, Mathf.Sin(radianes) * 0.145f);
            CrearVigaEntre(farol.transform, "Protector_Vertical", puntoInferior, puntoSuperior, 0.035f, metal);
        }

        CrearCilindro(farol.transform, "Mecha", new Vector3(0f, 0.34f, 0f), new Vector3(0.045f, 0.08f, 0.045f), interior);
        CrearLlama(farol.transform, "Llama_del_Farol", new Vector3(0f, 0.39f, 0.115f), 0.09f, 0.17f, fuego);
        CrearCaja(farol.transform, "Regulador_de_Mecha", new Vector3(0f, 0.285f, 0.18f), new Vector3(0.07f, 0.045f, 0.035f), laton);

        CrearCilindro(farol.transform, "Capuchon", new Vector3(0f, 0.79f, 0f), new Vector3(0.32f, 0.07f, 0.32f), metal);
        CrearEsfera(farol.transform, "Techo_del_Capuchon", new Vector3(0f, 0.84f, 0f), new Vector3(0.31f, 0.13f, 0.31f), metal);
        CrearCilindro(farol.transform, "Sombrerete", new Vector3(0f, 0.91f, 0f), new Vector3(0.2f, 0.035f, 0.2f), metal);

        for (int lado = -1; lado <= 1; lado += 2)
        {
            Vector3 inicio = new Vector3(lado * 0.13f, 0.83f, 0f);
            Vector3 medio = new Vector3(lado * 0.17f, 1.08f, 0f);
            Vector3 fin = new Vector3(0f, 1.13f, 0f);
            CrearVigaEntre(farol.transform, "Asa_Arqueada", inicio, medio, 0.035f, metal);
            CrearVigaEntre(farol.transform, "Asa_Arqueada", medio, fin, 0.035f, metal);
        }

        GameObject luzObjeto = new GameObject("Resplandor_del_Aceite");
        luzObjeto.transform.SetParent(farol.transform, false);
        luzObjeto.transform.localPosition = new Vector3(0f, 0.48f, 0f);
        Light luz = luzObjeto.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.48f, 0.16f);
        luz.intensity = 0.8f;
        luz.range = 4f;
        luz.shadows = LightShadows.None;

        GuardarPrefab(farol, FarolPath);
        Debug.Log("Prefab FarolAceite creado o actualizado.");
    }

    private static Material ObtenerMaterialVidrioFarol()
    {
        Material vidrio = AssetDatabase.LoadAssetAtPath<Material>(FarolVidrioPath);
        if (vidrio == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;

            vidrio = new Material(shader);
            vidrio.name = "Mat_FarolVidrio";
            AssetDatabase.CreateAsset(vidrio, FarolVidrioPath);
        }

        Color tinte = new Color(1f, 0.63f, 0.28f, 0.16f);
        if (vidrio.HasProperty("_BaseColor")) vidrio.SetColor("_BaseColor", tinte);
        if (vidrio.HasProperty("_Color")) vidrio.SetColor("_Color", tinte);
        if (vidrio.HasProperty("_Surface")) vidrio.SetFloat("_Surface", 1f);
        if (vidrio.HasProperty("_Blend")) vidrio.SetFloat("_Blend", 0f);
        if (vidrio.HasProperty("_SrcBlend")) vidrio.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (vidrio.HasProperty("_DstBlend")) vidrio.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (vidrio.HasProperty("_ZWrite")) vidrio.SetFloat("_ZWrite", 0f);
        if (vidrio.HasProperty("_Cull")) vidrio.SetFloat("_Cull", 0f);
        vidrio.SetOverrideTag("RenderType", "Transparent");
        vidrio.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        vidrio.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(vidrio);
        return vidrio;
    }

    private static void CrearAroHorizontal(Transform padre, string nombre, float diametro, float altura, int segmentos, float grosor, Material material)
    {
        float radio = diametro * 0.5f;
        for (int segmento = 0; segmento < segmentos; segmento++)
        {
            float anguloA = segmento * Mathf.PI * 2f / segmentos;
            float anguloB = (segmento + 1) * Mathf.PI * 2f / segmentos;
            Vector3 inicio = new Vector3(Mathf.Cos(anguloA) * radio, altura, Mathf.Sin(anguloA) * radio);
            Vector3 fin = new Vector3(Mathf.Cos(anguloB) * radio, altura, Mathf.Sin(anguloB) * radio);
            CrearVigaEntre(padre, nombre, inicio, fin, grosor, material);
        }
    }

    private static void CrearEsfera(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material)
    {
        GameObject esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        esfera.name = nombre;
        esfera.transform.SetParent(padre, false);
        esfera.transform.localPosition = posicion;
        esfera.transform.localScale = escala;
        esfera.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Material ObtenerMaterialMolinete()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MolineteMetalPath);
        if (material != null) return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogError("No se encontro un shader para el metal del molinete.");
            return null;
        }

        material = new Material(shader);
        material.name = "Mat_MolineteMetal";
        Color aceroEnvejecido = new Color(0.18f, 0.19f, 0.18f);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", aceroEnvejecido);
        if (material.HasProperty("_Color")) material.SetColor("_Color", aceroEnvejecido);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.68f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.32f);
        AssetDatabase.CreateAsset(material, MolineteMetalPath);
        return material;
    }

    private static void CrearBanco(Material madera)
    {
        GameObject banco = new GameObject("BancoLargo");
        banco.AddComponent<SeatInteractable>();

        CrearCaja(banco.transform, "Asiento_Tablas", new Vector3(0f, 0.55f, 0f), new Vector3(2.1f, 0.1f, 0.46f), madera);

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                CrearCaja(banco.transform, "Pata", new Vector3(x * 0.88f, 0.25f, z * 0.15f), new Vector3(0.13f, 0.5f, 0.13f), madera);
            }

            CrearCaja(banco.transform, "Travesano_Lateral", new Vector3(0f, 0.34f, x * 0.15f), new Vector3(1.82f, 0.09f, 0.09f), madera);
        }

        GuardarPrefab(banco, BancoPath);
    }

    private static void CrearPortal(Material madera)
    {
        GameObject portal = new GameObject("PortalCartel");

        CrearCaja(portal.transform, "Poste_Izquierdo", new Vector3(-1.1f, 1.7f, 0f), new Vector3(0.18f, 3.4f, 0.2f), madera);
        CrearCaja(portal.transform, "Poste_Derecho", new Vector3(1.1f, 1.7f, 0f), new Vector3(0.18f, 3.4f, 0.2f), madera);
        CrearCaja(portal.transform, "Viga_Superior", new Vector3(0f, 3.34f, 0f), new Vector3(2.5f, 0.2f, 0.24f), madera);
        CrearCaja(portal.transform, "Colgante_Izquierdo", new Vector3(-0.55f, 3.02f, 0f), new Vector3(0.07f, 0.38f, 0.1f), madera);
        CrearCaja(portal.transform, "Colgante_Derecho", new Vector3(0.55f, 3.02f, 0f), new Vector3(0.07f, 0.38f, 0.1f), madera);
        CrearCaja(portal.transform, "Cartel_Madera", new Vector3(0f, 2.67f, 0f), new Vector3(1.85f, 0.58f, 0.14f), madera);

        GameObject objetoTexto = new GameObject("Texto_Editable");
        objetoTexto.transform.SetParent(portal.transform, false);
        objetoTexto.transform.localPosition = new Vector3(0f, 2.67f, 0.081f);

        TextMeshPro texto = objetoTexto.AddComponent<TextMeshPro>();
        texto.text = "PULPERIA";
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = new Color(0.96f, 0.9f, 0.75f);
        texto.fontSize = 0.28f;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 0.12f;
        texto.fontSizeMax = 0.28f;
        texto.rectTransform.sizeDelta = new Vector2(1.72f, 0.48f);
        if (TMP_Settings.defaultFontAsset != null)
            texto.font = TMP_Settings.defaultFontAsset;

        portal.AddComponent<CartelTexto>().Configurar(texto, "PULPERIA");
        GuardarPrefab(portal, PortalPath);
    }

    private static void CrearChimenea(Material piedra, Material interior)
    {
        GameObject chimenea = new GameObject("ChimeneaLenia");

        CrearCaja(chimenea.transform, "Base_Frontal", new Vector3(0f, 0.14f, -0.31f), new Vector3(0.78f, 0.28f, 0.16f), piedra);
        CrearCaja(chimenea.transform, "Base_Trasera", new Vector3(0f, 0.14f, 0.31f), new Vector3(0.78f, 0.28f, 0.16f), piedra);
        CrearCaja(chimenea.transform, "Base_Izquierda", new Vector3(-0.31f, 0.14f, 0f), new Vector3(0.16f, 0.28f, 0.46f), piedra);
        CrearCaja(chimenea.transform, "Base_Derecha", new Vector3(0.31f, 0.14f, 0f), new Vector3(0.16f, 0.28f, 0.46f), piedra);

        const float alturaCuerpo = 1.82f;
        const float centroY = 1.17f;
        CrearCaja(chimenea.transform, "Muro_Izquierdo", new Vector3(-0.23f, centroY, 0f), new Vector3(0.1f, alturaCuerpo, 0.56f), piedra);
        CrearCaja(chimenea.transform, "Muro_Derecho", new Vector3(0.23f, centroY, 0f), new Vector3(0.1f, alturaCuerpo, 0.56f), piedra);
        CrearCaja(chimenea.transform, "Muro_Frontal", new Vector3(0f, centroY, -0.23f), new Vector3(0.36f, alturaCuerpo, 0.1f), piedra);
        CrearCaja(chimenea.transform, "Muro_Trasero", new Vector3(0f, centroY, 0.23f), new Vector3(0.36f, alturaCuerpo, 0.1f), piedra);

        CrearCaja(chimenea.transform, "Interior_Ennegrecido", new Vector3(0f, 1.25f, 0.175f), new Vector3(0.35f, 1.2f, 0.015f), interior);

        float coronamientoY = 2.1f;
        CrearCaja(chimenea.transform, "Borde_Frontal", new Vector3(0f, coronamientoY, -0.27f), new Vector3(0.74f, 0.16f, 0.2f), piedra);
        CrearCaja(chimenea.transform, "Borde_Trasero", new Vector3(0f, coronamientoY, 0.27f), new Vector3(0.74f, 0.16f, 0.2f), piedra);
        CrearCaja(chimenea.transform, "Borde_Izquierdo", new Vector3(-0.27f, coronamientoY, 0f), new Vector3(0.2f, 0.16f, 0.34f), piedra);
        CrearCaja(chimenea.transform, "Borde_Derecho", new Vector3(0.27f, coronamientoY, 0f), new Vector3(0.2f, 0.16f, 0.34f), piedra);

        GuardarPrefab(chimenea, ChimeneaPath);
    }

    [MenuItem("Trucardo/Recrear estufa de epoca y chimenea")]
    public static void RecrearEstufaDeEpocaYChimenea()
    {
        Material madera = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Material piedra = AssetDatabase.LoadAssetAtPath<Material>(MamposteriaPath);
        Material hierro = CargarOCrearMaterial(HierroPath, new Color(0.12f, 0.13f, 0.14f), 0.72f, 0.38f);
        Material fuego = CargarOCrearMaterial(FuegoPath, new Color(0.95f, 0.18f, 0.025f), 0f, 0.2f, new Color(1f, 0.12f, 0.015f) * 2f);
        Material brasa = CargarOCrearMaterial(BrasaPath, new Color(1f, 0.56f, 0.08f), 0f, 0.2f, new Color(1f, 0.35f, 0.025f) * 2f);
        Material interior = CargarOCrearMaterial(InteriorPath, new Color(0.035f, 0.018f, 0.012f), 0f, 0.1f);
        if (madera == null || piedra == null || hierro == null || fuego == null || brasa == null || interior == null)
        {
            Debug.LogError("Faltan materiales para reconstruir la estufa y la chimenea.");
            return;
        }

        CrearEstufa(hierro, fuego, brasa, interior, madera, piedra);
        CrearChimenea(piedra, interior);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Estufa de epoca y chimenea creadas con la misma salida de humo.");
    }

    private static void CrearEstufa(Material hierro, Material fuego, Material brasa, Material interior, Material madera, Material piedra)
    {
        GameObject estufa = new GameObject("EstufaLenia");

        CrearCaja(estufa.transform, "Hogar_de_Piedra", new Vector3(0f, 0.12f, 0f), new Vector3(1.62f, 0.24f, 1.18f), piedra);
        CrearCaja(estufa.transform, "Muro_Trasero", new Vector3(0f, 0.82f, -0.43f), new Vector3(1.32f, 1.28f, 0.2f), piedra);
        CrearCaja(estufa.transform, "Pilar_Piedra_Izquierdo", new Vector3(-0.59f, 0.82f, 0.08f), new Vector3(0.25f, 1.28f, 1.02f), piedra);
        CrearCaja(estufa.transform, "Pilar_Piedra_Derecho", new Vector3(0.59f, 0.82f, 0.08f), new Vector3(0.25f, 1.28f, 1.02f), piedra);
        CrearCaja(estufa.transform, "Dintel_de_Piedra", new Vector3(0f, 1.42f, 0.08f), new Vector3(1.5f, 0.2f, 1.02f), piedra);

        CrearPanelTaper(estufa.transform, "Campana_Frontal_de_Piedra", new Vector3(-0.72f, 1.48f, 0.5f), new Vector3(0.72f, 1.48f, 0.5f), new Vector3(0.28f, 2.16f, 0.25f), new Vector3(-0.28f, 2.16f, 0.25f), piedra);
        CrearPanelTaper(estufa.transform, "Campana_Trasera_de_Piedra", new Vector3(0.72f, 1.48f, -0.5f), new Vector3(-0.72f, 1.48f, -0.5f), new Vector3(-0.28f, 2.16f, -0.25f), new Vector3(0.28f, 2.16f, -0.25f), piedra);
        CrearPanelTaper(estufa.transform, "Campana_Izquierda_de_Piedra", new Vector3(-0.72f, 1.48f, -0.5f), new Vector3(-0.72f, 1.48f, 0.5f), new Vector3(-0.28f, 2.16f, 0.25f), new Vector3(-0.28f, 2.16f, -0.25f), piedra);
        CrearPanelTaper(estufa.transform, "Campana_Derecha_de_Piedra", new Vector3(0.72f, 1.48f, 0.5f), new Vector3(0.72f, 1.48f, -0.5f), new Vector3(0.28f, 2.16f, -0.25f), new Vector3(0.28f, 2.16f, 0.25f), piedra);

        CrearCaja(estufa.transform, "Cuello_de_Humo_Izquierdo", new Vector3(-0.23f, 2.22f, 0f), new Vector3(0.1f, 0.24f, 0.56f), piedra);
        CrearCaja(estufa.transform, "Cuello_de_Humo_Derecho", new Vector3(0.23f, 2.22f, 0f), new Vector3(0.1f, 0.24f, 0.56f), piedra);
        CrearCaja(estufa.transform, "Cuello_de_Humo_Frontal", new Vector3(0f, 2.22f, -0.23f), new Vector3(0.36f, 0.24f, 0.1f), piedra);
        CrearCaja(estufa.transform, "Cuello_de_Humo_Trasero", new Vector3(0f, 2.22f, 0.23f), new Vector3(0.36f, 0.24f, 0.1f), piedra);

        CrearCaja(estufa.transform, "Fondo_Oscuro_del_Hogar", new Vector3(0f, 0.77f, 0.285f), new Vector3(0.78f, 0.78f, 0.04f), interior);
        CrearCaja(estufa.transform, "Marco_de_Hierro_Izquierdo", new Vector3(-0.44f, 0.77f, 0.33f), new Vector3(0.09f, 0.82f, 0.1f), hierro);
        CrearCaja(estufa.transform, "Marco_de_Hierro_Derecho", new Vector3(0.44f, 0.77f, 0.33f), new Vector3(0.09f, 0.82f, 0.1f), hierro);
        CrearCaja(estufa.transform, "Marco_de_Hierro_Superior", new Vector3(0f, 1.18f, 0.33f), new Vector3(0.95f, 0.09f, 0.1f), hierro);
        CrearCaja(estufa.transform, "Marco_de_Hierro_Inferior", new Vector3(0f, 0.36f, 0.33f), new Vector3(0.95f, 0.09f, 0.1f), hierro);

        CrearTronco(estufa.transform, "Lenio_en_el_Hogar", new Vector3(0f, 0.42f, 0.37f), new Vector3(0.48f, 0.085f, 0.07f), madera, -8f);
        CrearTronco(estufa.transform, "Lenio_cruzado", new Vector3(0f, 0.48f, 0.39f), new Vector3(0.42f, 0.07f, 0.06f), madera, 10f);
        CrearLlama(estufa.transform, "Llama_Interior", new Vector3(0f, 0.48f, 0.39f), 0.36f, 0.45f, fuego);
        CrearLlama(estufa.transform, "Centro_de_Brasas", new Vector3(0f, 0.4f, 0.4f), 0.26f, 0.25f, brasa);

        CrearCaja(estufa.transform, "Puerta_del_Cenicero", new Vector3(0f, 0.24f, 0.35f), new Vector3(0.42f, 0.13f, 0.09f), hierro);
        CrearCaja(estufa.transform, "Tiron_del_Cenicero", new Vector3(0f, 0.23f, 0.41f), new Vector3(0.12f, 0.035f, 0.04f), hierro);
        CrearCaja(estufa.transform, "Placa_de_Coccion_Izquierda", new Vector3(-0.43f, 1.55f, 0f), new Vector3(0.4f, 0.08f, 0.86f), hierro);
        CrearCaja(estufa.transform, "Placa_de_Coccion_Derecha", new Vector3(0.43f, 1.55f, 0f), new Vector3(0.4f, 0.08f, 0.86f), hierro);
        CrearCilindro(estufa.transform, "Aro_de_Coccion_Izquierdo", new Vector3(-0.43f, 1.62f, 0f), new Vector3(0.28f, 0.025f, 0.28f), hierro);
        CrearCilindro(estufa.transform, "Aro_de_Coccion_Derecho", new Vector3(0.43f, 1.62f, 0f), new Vector3(0.28f, 0.025f, 0.28f), hierro);

        GameObject puntoConexion = new GameObject("Punto_Conexion_Chimenea");
        puntoConexion.transform.SetParent(estufa.transform, false);
        puntoConexion.transform.localPosition = new Vector3(0f, 2.34f, 0f);

        GameObject luzObjeto = new GameObject("Resplandor_del_Fuego");
        luzObjeto.transform.SetParent(estufa.transform, false);
        luzObjeto.transform.localPosition = new Vector3(0f, 0.7f, 0.65f);
        Light luz = luzObjeto.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.27f, 0.06f);
        luz.intensity = 1.1f;
        luz.range = 3f;

        GuardarPrefab(estufa, EstufaPath);
    }

    private static void CrearValla(Material madera)
    {
        GameObject valla = new GameObject("VallaRural");

        CrearTronco(valla.transform, "Poste_Izquierdo", new Vector3(-1.16f, 0.62f, 0f), new Vector3(0.16f, 0.62f, 0.16f), madera);
        CrearTronco(valla.transform, "Poste_Derecho", new Vector3(1.16f, 0.62f, 0f), new Vector3(0.16f, 0.62f, 0.16f), madera);

        CrearTronco(valla.transform, "Larguero_Inferior", new Vector3(0f, 0.3f, 0f), new Vector3(2.38f, 0.11f, 0.12f), madera);
        CrearTronco(valla.transform, "Larguero_Central", new Vector3(0f, 0.66f, 0f), new Vector3(2.38f, 0.11f, 0.12f), madera);
        CrearTronco(valla.transform, "Larguero_Superior", new Vector3(0f, 1.02f, 0f), new Vector3(2.38f, 0.11f, 0.12f), madera);
        CrearTronco(valla.transform, "Diagonal_de_Apoyo", new Vector3(0f, 0.66f, -0.085f), new Vector3(2.5f, 0.075f, 0.075f), madera, -17f);

        GuardarPrefab(valla, VallaPath);
    }

    private static void CrearPorton(Material madera, Material herrajes)
    {
        GameObject porton = new GameObject("PortonRural");

        CrearTronco(porton.transform, "Marco_Izquierdo", new Vector3(-1.12f, 0.62f, 0f), new Vector3(0.13f, 0.62f, 0.13f), madera);
        CrearTronco(porton.transform, "Marco_Derecho", new Vector3(1.12f, 0.62f, 0f), new Vector3(0.13f, 0.62f, 0.13f), madera);
        CrearTronco(porton.transform, "Travesano_Inferior", new Vector3(0f, 0.18f, 0f), new Vector3(2.34f, 0.11f, 0.12f), madera);
        CrearTronco(porton.transform, "Travesano_Central", new Vector3(0f, 0.62f, 0f), new Vector3(2.34f, 0.11f, 0.12f), madera);
        CrearTronco(porton.transform, "Travesano_Superior", new Vector3(0f, 1.06f, 0f), new Vector3(2.34f, 0.11f, 0.12f), madera);
        CrearTronco(porton.transform, "Refuerzo_Diagonal_1", new Vector3(0f, 0.62f, 0.085f), new Vector3(2.3f, 0.075f, 0.075f), madera, -21f);
        CrearTronco(porton.transform, "Refuerzo_Diagonal_2", new Vector3(0f, 0.62f, 0.088f), new Vector3(2.3f, 0.075f, 0.075f), madera, 21f);

        CrearCaja(porton.transform, "Bisagra_Superior", new Vector3(-1.13f, 0.94f, 0.09f), new Vector3(0.34f, 0.055f, 0.025f), herrajes);
        CrearCaja(porton.transform, "Bisagra_Inferior", new Vector3(-1.13f, 0.28f, 0.09f), new Vector3(0.34f, 0.055f, 0.025f), herrajes);
        CrearCaja(porton.transform, "Traba", new Vector3(1.12f, 0.62f, 0.085f), new Vector3(0.18f, 0.055f, 0.035f), herrajes);

        GuardarPrefab(porton, PortonPath);
    }

    private static void CrearTronco(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material madera, float rotacionZ = 0f)
    {
        GameObject tronco = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tronco.name = nombre;
        tronco.transform.SetParent(padre, false);
        tronco.transform.localPosition = posicion;
        tronco.transform.localRotation = Quaternion.Euler(0f, 0f, rotacionZ);
        tronco.transform.localScale = escala;
        tronco.GetComponent<Renderer>().sharedMaterial = madera;
    }

    private static void CrearMolinete(Material metal, Material madera)
    {
        GameObject molinete = new GameObject("MolineteRural");
        const float alturaTorre = 4.9f;
        const float radioBase = 1.15f;
        const float radioCima = 0.38f;
        const float grosorTorre = 0.075f;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 basePata = new Vector3(x * radioBase, 0f, z * radioBase);
                Vector3 cimaPata = new Vector3(x * radioCima, alturaTorre, z * radioCima);
                CrearVigaEntre(molinete.transform, "Pata_Celosia", basePata, cimaPata, grosorTorre, metal);
                CrearCaja(molinete.transform, "Placa_de_Anclaje", new Vector3(x * radioBase, 0.08f, z * radioBase), new Vector3(0.2f, 0.16f, 0.2f), metal);
            }
        }

        float[] niveles = { 0.9f, 1.9f, 2.9f, 3.9f, alturaTorre };
        for (int nivel = 0; nivel < niveles.Length; nivel++)
        {
            float y = niveles[nivel];
            float radio = Mathf.Lerp(radioBase, radioCima, y / alturaTorre);
            CrearVigaEntre(molinete.transform, "Travesano_Frontal", new Vector3(-radio, y, -radio), new Vector3(radio, y, -radio), 0.055f, metal);
            CrearVigaEntre(molinete.transform, "Travesano_Trasero", new Vector3(-radio, y, radio), new Vector3(radio, y, radio), 0.055f, metal);
            CrearVigaEntre(molinete.transform, "Travesano_Lateral_Izquierdo", new Vector3(-radio, y, -radio), new Vector3(-radio, y, radio), 0.055f, metal);
            CrearVigaEntre(molinete.transform, "Travesano_Lateral_Derecho", new Vector3(radio, y, -radio), new Vector3(radio, y, radio), 0.055f, metal);
        }

        for (int panel = 0; panel < niveles.Length - 1; panel++)
        {
            float y0 = niveles[panel];
            float y1 = niveles[panel + 1];
            float r0 = Mathf.Lerp(radioBase, radioCima, y0 / alturaTorre);
            float r1 = Mathf.Lerp(radioBase, radioCima, y1 / alturaTorre);

            for (int lado = -1; lado <= 1; lado += 2)
            {
                CrearVigaEntre(molinete.transform, "Cruce_Frontal", new Vector3(-r0, y0, lado * r0), new Vector3(r1, y1, lado * r1), 0.045f, metal);
                CrearVigaEntre(molinete.transform, "Cruce_Frontal", new Vector3(r0, y0, lado * r0), new Vector3(-r1, y1, lado * r1), 0.045f, metal);
                CrearVigaEntre(molinete.transform, "Cruce_Lateral", new Vector3(lado * r0, y0, -r0), new Vector3(lado * r1, y1, r1), 0.045f, metal);
                CrearVigaEntre(molinete.transform, "Cruce_Lateral", new Vector3(lado * r0, y0, r0), new Vector3(lado * r1, y1, -r1), 0.045f, metal);
            }
        }

        CrearCaja(molinete.transform, "Plataforma", new Vector3(0f, 4.98f, 0f), new Vector3(1.18f, 0.12f, 1.18f), metal);
        CrearCaja(molinete.transform, "Piso_de_madera", new Vector3(0f, 5.055f, 0f), new Vector3(1.08f, 0.045f, 1.08f), madera);

        const float barandaY = 5.32f;
        CrearVigaEntre(molinete.transform, "Baranda_Frontal", new Vector3(-0.56f, barandaY, -0.56f), new Vector3(0.56f, barandaY, -0.56f), 0.035f, metal);
        CrearVigaEntre(molinete.transform, "Baranda_Trasera", new Vector3(-0.56f, barandaY, 0.56f), new Vector3(0.56f, barandaY, 0.56f), 0.035f, metal);
        CrearVigaEntre(molinete.transform, "Baranda_Izquierda", new Vector3(-0.56f, barandaY, -0.56f), new Vector3(-0.56f, barandaY, 0.56f), 0.035f, metal);
        CrearVigaEntre(molinete.transform, "Baranda_Derecha", new Vector3(0.56f, barandaY, -0.56f), new Vector3(0.56f, barandaY, 0.56f), 0.035f, metal);

        for (int peldaño = 0; peldaño < 14; peldaño++)
        {
            float y = 0.35f + peldaño * 0.34f;
            float radio = Mathf.Lerp(radioBase, radioCima, y / alturaTorre);
            CrearVigaEntre(molinete.transform, "Peldaño_Escalera", new Vector3(-radio - 0.11f, y, -radio - 0.06f), new Vector3(-radio - 0.11f, y, -radio + 0.31f), 0.035f, metal);
        }

        Vector3 centroRueda = new Vector3(0f, 5.92f, 0.45f);
        CrearVigaEntre(molinete.transform, "Soporte_del_Eje", new Vector3(0f, 5.03f, 0.25f), centroRueda, 0.09f, metal);
        CrearCilindroOrientado(molinete.transform, "Eje_de_la_Rueda", centroRueda, new Vector3(0.13f, 0.22f, 0.13f), metal);

        const int segmentosAro = 24;
        const float radioAro = 0.9f;
        float largoSegmento = 2f * radioAro * Mathf.Sin(Mathf.PI / segmentosAro) * 1.08f;
        for (int segmento = 0; segmento < segmentosAro; segmento++)
        {
            float angulo = segmento * 360f / segmentosAro;
            float radianes = angulo * Mathf.Deg2Rad;
            Vector3 posicion = centroRueda + new Vector3(Mathf.Cos(radianes) * radioAro, Mathf.Sin(radianes) * radioAro, 0f);
            CrearTronco(molinete.transform, "Segmento_del_Aro", posicion, new Vector3(largoSegmento, 0.045f, 0.045f), metal, angulo + 90f);
        }

        for (int radio = 0; radio < 8; radio++)
        {
            float angulo = radio * 45f;
            float radianes = angulo * Mathf.Deg2Rad;
            Vector3 extremo = centroRueda + new Vector3(Mathf.Cos(radianes) * 0.85f, Mathf.Sin(radianes) * 0.85f, 0f);
            CrearVigaEntre(molinete.transform, "Radio_de_la_Rueda", centroRueda, extremo, 0.035f, metal);
        }

        for (int aspa = 0; aspa < 16; aspa++)
        {
            float angulo = aspa * 22.5f;
            float radianes = angulo * Mathf.Deg2Rad;
            Vector3 posicion = centroRueda + new Vector3(Mathf.Cos(radianes) * 0.54f, Mathf.Sin(radianes) * 0.54f, -0.035f);
            CrearTronco(molinete.transform, "Aspa_de_Viento", posicion, new Vector3(0.67f, 0.055f, 0.035f), metal, angulo);
        }

        CrearCilindroOrientado(molinete.transform, "Cubo_del_Rotor", centroRueda + new Vector3(0f, 0f, -0.08f), new Vector3(0.23f, 0.12f, 0.23f), metal);
        CrearCilindroOrientado(molinete.transform, "Tapa_del_Cubo", centroRueda + new Vector3(0f, 0f, -0.16f), new Vector3(0.12f, 0.055f, 0.12f), metal);

        CrearVigaEntre(molinete.transform, "Brazo_de_la_Cola", centroRueda + new Vector3(0f, -0.08f, -0.12f), new Vector3(0f, 5.75f, -1.55f), 0.075f, metal);
        CrearCaja(molinete.transform, "Aleta_de_Orientacion", new Vector3(0f, 5.75f, -1.8f), new Vector3(0.82f, 0.58f, 0.045f), metal);
        CrearVigaEntre(molinete.transform, "Refuerzo_de_la_Cola_Izquierdo", new Vector3(0f, 5.75f, -1.8f), new Vector3(-0.39f, 5.47f, -1.8f), 0.035f, metal);
        CrearVigaEntre(molinete.transform, "Refuerzo_de_la_Cola_Derecho", new Vector3(0f, 5.75f, -1.8f), new Vector3(0.39f, 5.47f, -1.8f), 0.035f, metal);

        CrearVigaEntre(molinete.transform, "Varilla_de_Bombeo", new Vector3(0f, 0.3f, 0f), new Vector3(0f, 5.05f, 0f), 0.025f, metal);
        GuardarPrefab(molinete, MolinetePath);
    }

    private static void CrearVigaEntre(Transform padre, string nombre, Vector3 inicio, Vector3 fin, float grosor, Material material)
    {
        GameObject viga = GameObject.CreatePrimitive(PrimitiveType.Cube);
        viga.name = nombre;
        viga.transform.SetParent(padre, false);
        Vector3 vector = fin - inicio;
        viga.transform.localPosition = (inicio + fin) * 0.5f;
        viga.transform.localRotation = Quaternion.FromToRotation(Vector3.up, vector.normalized);
        viga.transform.localScale = new Vector3(grosor, vector.magnitude, grosor);
        viga.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CrearPanelTaper(Transform padre, string nombre, Vector3 inferiorIzquierdo, Vector3 inferiorDerecho, Vector3 superiorDerecho, Vector3 superiorIzquierdo, Material material)
    {
        GameObject panel = new GameObject(nombre);
        panel.transform.SetParent(padre, false);

        Mesh mesh = new Mesh { name = nombre + "Mesh" };
        mesh.vertices = new[] { inferiorIzquierdo, inferiorDerecho, superiorDerecho, superiorIzquierdo };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        panel.AddComponent<MeshFilter>().sharedMesh = mesh;
        panel.AddComponent<MeshRenderer>().sharedMaterial = material;
        panel.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    private static void CrearCilindroOrientado(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material)
    {
        GameObject cilindro = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cilindro.name = nombre;
        cilindro.transform.SetParent(padre, false);
        cilindro.transform.localPosition = posicion;
        cilindro.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        cilindro.transform.localScale = escala;
        cilindro.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CrearLlama(Transform padre, string nombre, Vector3 posicion, float ancho, float alto, Material material)
    {
        GameObject llama = new GameObject(nombre);
        llama.transform.SetParent(padre, false);
        llama.transform.localPosition = posicion;

        Mesh mesh = new Mesh { name = nombre + "Mesh" };
        mesh.vertices = new[]
        {
            new Vector3(-ancho * 0.5f, 0f, 0f),
            new Vector3(0f, alto, 0f),
            new Vector3(ancho * 0.5f, 0f, 0f)
        };
        mesh.triangles = new[] { 0, 2, 1 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        llama.AddComponent<MeshFilter>().sharedMesh = mesh;
        llama.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void CrearCilindro(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material)
    {
        GameObject cilindro = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cilindro.name = nombre;
        cilindro.transform.SetParent(padre, false);
        cilindro.transform.localPosition = posicion;
        cilindro.transform.localScale = escala;
        cilindro.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Material CargarOCrearMaterial(string ruta, Color color, float metalico, float suavidad, Color? emision = null)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (material != null) return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) return null;

        material = new Material(shader);
        material.name = System.IO.Path.GetFileNameWithoutExtension(ruta);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metalico);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", suavidad);
        if (emision.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emision.Value);
        }

        AssetDatabase.CreateAsset(material, ruta);
        return material;
    }

    private static void CrearCaja(Transform padre, string nombre, Vector3 posicion, Vector3 escala, Material material)
    {
        GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
        caja.name = nombre;
        caja.transform.SetParent(padre, false);
        caja.transform.localPosition = posicion;
        caja.transform.localScale = escala;
        caja.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void GuardarPrefab(GameObject objeto, string ruta)
    {
        PrefabUtility.SaveAsPrefabAsset(objeto, ruta);
        Object.DestroyImmediate(objeto);
    }
}