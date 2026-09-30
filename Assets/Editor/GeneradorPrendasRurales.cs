using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GeneradorPrendasRurales
{
    private const string Carpeta = "Assets/Models/BarRecursos/Prendas";
    private const string MallasCarpeta = Carpeta + "/Mallas";
    private const string MaterialesCarpeta = Carpeta + "/Materiales";

    private struct Prenda
    {
        public string Nombre;
        public bool Abrigo;
        public bool Capa;
        public float Ancho;
        public float Alto;
        public float Caida;
        public float Pliegues;
        public float Desgaste;
        public int Patron;
        public int Flecos;
        public Color Color;
    }

    static GeneradorPrendasRurales()
    {
        EditorApplication.delayCall += CrearPrendasFaltantes;
    }

    [MenuItem("Trucardo/Crear prendas gauchas faltantes")]
    public static void CrearPrendasFaltantes()
    {
        CrearPrendasFaltantes(false);
    }

    [MenuItem("Trucardo/Recrear prendas gauchas y ganchos")]
    public static void RecrearPrendasYGanchos()
    {
        CrearPrendasFaltantes(true);
    }

    private static void CrearPrendasFaltantes(bool recrear)
    {
        AsegurarCarpetas();
        Texture2D normalLana = CargarOCrearTextura("Normal_Lana.asset", 21, true);
        Texture2D suavidad = CargarOCrearTextura("Suavidad_Tejido.asset", 48, false);
        Texture2D texturaCuero = CargarOCrearTextura("Textura_Cuero.asset", 77, false);
        Texture2D normalCuero = CargarOCrearTextura("Normal_Cuero.asset", 93, true);
        Texture2D texturaMadera = CargarOCrearTextura("Textura_MaderaBoton.asset", 113, false);
        Texture2D normalHierro = CargarOCrearTextura("Normal_HierroGancho.asset", 137, true);
        if (normalLana == null || suavidad == null || texturaCuero == null || normalCuero == null || texturaMadera == null || normalHierro == null) return;

        Prenda[] prendas = ObtenerPrendas();
        for (int i = 0; i < prendas.Length; i++)
            CrearPrenda(prendas[i], normalLana, suavidad, texturaCuero, normalCuero, texturaMadera, recrear);

        Material madera = CrearMaterial("Mat_MaderaGanchos", new Color(0.34f, 0.22f, 0.13f), texturaMadera, normalLana, suavidad, 0f, 0.23f);
        Material hierro = CrearMaterial("Mat_HierroGanchos", new Color(0.16f, 0.14f, 0.12f), texturaCuero, normalHierro, suavidad, 0.58f, 0.3f);
        Material aceroOxidado = CrearMaterial("Mat_HierroOxidado", new Color(0.27f, 0.17f, 0.11f), texturaCuero, normalHierro, suavidad, 0.62f, 0.29f);
        if (madera != null && hierro != null && aceroOxidado != null)
        {
            CrearGancho("GanchoMadera_01", false, 0, madera, recrear);
            CrearGancho("GanchoMadera_02", false, 1, madera, recrear);
            CrearGancho("GanchoHierro_01", true, 0, hierro, recrear);
            CrearGancho("GanchoHierro_02", true, 1, aceroOxidado, recrear);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Prendas gauchas y ganchos individuales revisados en " + Carpeta);
    }

    private static Prenda[] ObtenerPrendas()
    {
        return new[]
        {
            new Prenda { Nombre = "Poncho_04", Ancho = 1.30f, Alto = 1.10f, Caida = 0.30f, Pliegues = 0.064f, Desgaste = 0.66f, Patron = 4, Flecos = 21, Color = new Color(0.20f, 0.23f, 0.23f) },
            new Prenda { Nombre = "Poncho_05", Ancho = 1.72f, Alto = 1.36f, Caida = 0.13f, Pliegues = 0.11f, Desgaste = 0.72f, Patron = 0, Flecos = 31, Color = new Color(0.39f, 0.32f, 0.23f) },
            new Prenda { Nombre = "Abrigo_03", Abrigo = true, Ancho = 0.66f, Alto = 0.96f, Caida = 0.11f, Pliegues = 0.039f, Desgaste = 0.39f, Patron = 0, Color = new Color(0.40f, 0.25f, 0.15f) },
            new Prenda { Nombre = "Abrigo_04", Abrigo = true, Ancho = 0.62f, Alto = 0.84f, Caida = 0.24f, Pliegues = 0.064f, Desgaste = 0.31f, Patron = 0, Color = new Color(0.55f, 0.45f, 0.33f) },
            new Prenda { Nombre = "Capa_01", Capa = true, Ancho = 1.52f, Alto = 1.34f, Caida = 0.28f, Pliegues = 0.095f, Desgaste = 0.48f, Patron = 5, Flecos = 27, Color = new Color(0.25f, 0.27f, 0.24f) }
        };
    }

    private static void CrearPrenda(Prenda prenda, Texture2D normalLana, Texture2D suavidad,
        Texture2D texturaCuero, Texture2D normalCuero, Texture2D texturaMadera, bool recrear = false)
    {
        string prefabPath = Carpeta + "/" + prenda.Nombre + ".prefab";
        Material tejido = CrearMaterial("Mat_Tejido_" + prenda.Nombre, prenda.Color,
            CargarOCrearTextura("Tejido_" + prenda.Nombre + ".asset", 30 + prenda.Patron * 17 + prenda.Nombre.GetHashCode() % 61, false, prenda.Patron),
            normalLana, suavidad, 0f, 0.18f);
        Material lana = CrearMaterial("Mat_LanaFlecos_" + prenda.Nombre, prenda.Color * 1.08f, texturaMadera, normalLana, suavidad, 0f, 0.14f);
        Material cuero = CrearMaterial("Mat_Cuero_" + prenda.Nombre, new Color(0.22f, 0.13f, 0.075f), texturaCuero, normalCuero, suavidad, 0f, 0.32f);
        Material botones = CrearMaterial("Mat_Botones_" + prenda.Nombre, new Color(0.37f, 0.25f, 0.14f), texturaMadera, normalLana, suavidad, 0f, 0.23f);
        if (tejido == null || lana == null || cuero == null || botones == null) return;
        if (!recrear && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        int semilla = Mathf.Abs(prenda.Nombre.GetHashCode());
        GameObject raiz = new GameObject(prenda.Nombre);
        if (prenda.Abrigo)
            CrearAbrigo(raiz.transform, prenda, tejido, lana, cuero, botones, semilla);
        else
            CrearPonchoOCapa(raiz.transform, prenda, tejido, lana, cuero, semilla);
        GuardarPrefab(raiz, prefabPath);
    }

    private static void CrearPonchoOCapa(Transform raiz, Prenda prenda, Material tejido, Material lana, Material cuero, int semilla)
    {
        GameObject prendaMesh = new GameObject("PrendaMesh");
        prendaMesh.transform.SetParent(raiz, false);
        Mesh mesh = CrearMallaTela(prenda, semilla, true, 0f, 1f, false);
        prendaMesh.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(mesh, prenda.Nombre + "_Prenda.asset");
        prendaMesh.AddComponent<MeshRenderer>().sharedMaterial = tejido;

        GameObject flecos = new GameObject("Flecos");
        flecos.transform.SetParent(raiz, false);
        if (prenda.Flecos > 0)
        {
            Mesh meshFlecos = CrearMallaFlecos(prenda, semilla);
            flecos.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(meshFlecos, prenda.Nombre + "_Flecos.asset");
            flecos.AddComponent<MeshRenderer>().sharedMaterial = lana;
        }

        CrearOjalSuperior(raiz, prenda.Nombre, lana);
        if (prenda.Capa)
        {
            CrearPanelLapel(raiz, "Capucha", -0.15f, -0.24f, 0.15f, -0.28f, 0.075f, cuero);
            CrearPanelLapel(raiz, "Capucha", 0.15f, -0.24f, -0.15f, -0.28f, 0.075f, cuero);
        }
    }

    private static void CrearAbrigo(Transform raiz, Prenda prenda, Material tejido, Material lana, Material cuero, Material botones, int semilla)
    {
        GameObject cuerpo = CrearGrupo(raiz, "Cuerpo");
        Mesh espalda = CrearMallaTela(prenda, semilla, false, 0f, 1f, true);
        GameObject espaldaObjeto = new GameObject("Espalda");
        espaldaObjeto.transform.SetParent(cuerpo.transform, false);
        espaldaObjeto.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(espalda, prenda.Nombre + "_Espalda.asset");
        espaldaObjeto.AddComponent<MeshRenderer>().sharedMaterial = tejido;

        for (int lado = 0; lado < 2; lado++)
        {
            float u0 = lado == 0 ? 0f : 0.54f;
            float u1 = lado == 0 ? 0.46f : 1f;
            Mesh panel = CrearMallaTela(prenda, semilla + lado * 13, false, u0, u1, false);
            GameObject delantero = new GameObject(lado == 0 ? "DelanteroIzquierdo" : "DelanteroDerecho");
            delantero.transform.SetParent(cuerpo.transform, false);
            delantero.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(panel, prenda.Nombre + "_Delantero_0" + (lado + 1) + ".asset");
            delantero.AddComponent<MeshRenderer>().sharedMaterial = tejido;
        }

        GameObject mangas = CrearGrupo(raiz, "Mangas");
        for (int lado = -1; lado <= 1; lado += 2)
        {
            Mesh manga = CrearMallaManga(prenda, lado, semilla + lado * 31);
            GameObject mangaObjeto = new GameObject(lado < 0 ? "MangaIzquierda" : "MangaDerecha");
            mangaObjeto.transform.SetParent(mangas.transform, false);
            mangaObjeto.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(manga, prenda.Nombre + "_Manga_" + (lado < 0 ? "I" : "D") + ".asset");
            mangaObjeto.AddComponent<MeshRenderer>().sharedMaterial = tejido;
        }

        GameObject cuello = CrearGrupo(raiz, "Cuello");
        CrearPanelLapel(cuello.transform, "SolapaIzquierda", -0.105f, -0.07f, -0.015f, -0.33f, -0.095f, cuero);
        CrearPanelLapel(cuello.transform, "SolapaDerecha", 0.105f, -0.07f, 0.015f, -0.33f, -0.095f, cuero);
        CrearLazoSuperior(raiz, prenda.Nombre, lana);

        GameObject bolsillos = CrearGrupo(raiz, "Bolsillos");
        CrearBolsillo(bolsillos.transform, "BolsilloIzquierdo", -0.19f, -0.49f, cuero);
        CrearBolsillo(bolsillos.transform, "BolsilloDerecho", 0.19f, -0.49f, cuero);

        GameObject botonRaiz = CrearGrupo(raiz, "Botones");
        for (int i = 0; i < 4; i++)
        {
            GameObject boton = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boton.name = "Boton_0" + (i + 1);
            Object.DestroyImmediate(boton.GetComponent<Collider>());
            boton.transform.SetParent(botonRaiz.transform, false);
            boton.transform.localPosition = new Vector3(0.026f, -0.24f - i * (prenda.Alto * 0.14f), -0.12f);
            boton.transform.localScale = new Vector3(0.025f, 0.025f, 0.012f);
            boton.GetComponent<Renderer>().sharedMaterial = botones;
        }
    }

    private static Mesh CrearMallaTela(Prenda prenda, int semilla, bool poncho, float uInicio, float uFin, bool espalda)
    {
        const int columnas = 28;
        const int filas = 26;
        List<Vector3> vertices = new List<Vector3>((columnas + 1) * (filas + 1) * 2);
        List<Vector2> uv = new List<Vector2>(vertices.Capacity);
        List<int> triangulos = new List<int>(columnas * filas * 12);
        int capa = (columnas + 1) * (filas + 1);

        for (int cara = 0; cara < 2; cara++)
        {
            for (int fila = 0; fila <= filas; fila++)
            {
                float v = fila / (float)filas;
                for (int columna = 0; columna <= columnas; columna++)
                {
                    float uLocal = columna / (float)columnas;
                    float u = Mathf.Lerp(uInicio, uFin, uLocal);
                    float ancho = AnchoTela(prenda, v, poncho);
                    float x = (u - 0.5f) * ancho;
                    float pliegue = Mathf.Sin(u * Mathf.PI * 2f * prenda.Caida + semilla * 0.01f) * prenda.Pliegues
                        + Mathf.Sin(u * Mathf.PI * 9f + semilla * 0.07f) * prenda.Pliegues * 0.19f;
                    float gravedad = Mathf.Sin(v * Mathf.PI * 0.84f);
                    float z = pliegue * gravedad + 0.017f * Mathf.Sin(v * Mathf.PI * 5f + u * 9f + semilla) * gravedad;
                    if (!poncho && !espalda)
                        z -= 0.025f;
                    else if (espalda)
                        z += 0.075f;
                    if (cara == 1) z += 0.025f;
                    float y = -0.04f - v * prenda.Alto + 0.015f * Mathf.Sin(u * 17f + semilla) * v;
                    vertices.Add(new Vector3(x, y, z));
                    uv.Add(new Vector2(u * 2.2f, v * 2.1f));
                }
            }
        }

        for (int cara = 0; cara < 2; cara++)
        {
            int baseCara = cara * capa;
            for (int fila = 0; fila < filas; fila++)
            {
                for (int columna = 0; columna < columnas; columna++)
                {
                    float uc = Mathf.Lerp(uInicio, uFin, (columna + 0.5f) / columnas);
                    float vc = (fila + 0.5f) / filas;
                    bool agujero = poncho && Mathf.Pow((uc - 0.5f) / 0.065f, 2f) + Mathf.Pow((vc - 0.13f) / 0.052f, 2f) < 1f;
                    if (agujero) continue;
                    int a = baseCara + fila * (columnas + 1) + columna;
                    int b = a + columnas + 1;
                    int c = a + 1;
                    int d = b + 1;
                    if (cara == 0)
                    {
                        triangulos.Add(a); triangulos.Add(b); triangulos.Add(c);
                        triangulos.Add(c); triangulos.Add(b); triangulos.Add(d);
                    }
                    else
                    {
                        triangulos.Add(a); triangulos.Add(c); triangulos.Add(b);
                        triangulos.Add(c); triangulos.Add(d); triangulos.Add(b);
                    }
                }
            }
        }

        Mesh mesh = new Mesh { name = prenda.Nombre + (espalda ? "_MallaEspalda" : "_MallaTela") };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static float AnchoTela(Prenda prenda, float v, bool poncho)
    {
        float progresion = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(v * 1.5f));
        if (prenda.Abrigo)
            return prenda.Ancho * Mathf.Lerp(0.72f, 1f, progresion) * (1f + 0.045f * Mathf.Sin(v * Mathf.PI * 2f));
        float hombros = Mathf.Lerp(0.24f, 1f, Mathf.SmoothStep(0.03f, 0.56f, v));
        float borde = Mathf.Lerp(1f, 0.92f, Mathf.SmoothStep(0.78f, 1f, v));
        return prenda.Ancho * hombros * borde * (1f + 0.025f * Mathf.Sin(v * 17f));
    }

    private static Mesh CrearMallaFlecos(Prenda prenda, int semilla)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> triangulos = new List<int>();
        float ancho = prenda.Ancho * 0.92f;
        for (int i = 0; i < prenda.Flecos; i++)
        {
            float x = Mathf.Lerp(-ancho * 0.5f, ancho * 0.5f, i / (float)(prenda.Flecos - 1));
            float largo = 0.045f + (Mathf.Abs(Mathf.Sin(i * 17.1f + semilla)) * 0.075f);
            float sway = Mathf.Sin(i * 4.7f + semilla) * 0.017f;
            Vector3[] puntos =
            {
                new Vector3(x, -prenda.Alto + 0.025f, 0.01f),
                new Vector3(x + sway * 0.4f, -prenda.Alto - largo * 0.45f, 0.015f),
                new Vector3(x + sway, -prenda.Alto - largo, 0.02f)
            };
            AgregarTubo(vertices, uv, triangulos, puntos, 0.0032f, i + semilla, 5);
        }

        Mesh mesh = new Mesh { name = prenda.Nombre + "_MallaFlecos" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CrearMallaManga(Prenda prenda, int lado, int semilla)
    {
        const int anillos = 14;
        const int lados = 12;
        List<Vector3> vertices = new List<Vector3>((anillos + 1) * lados);
        List<Vector2> uv = new List<Vector2>(vertices.Capacity);
        List<int> triangulos = new List<int>(anillos * lados * 6);
        Vector3 inicio = new Vector3(lado * prenda.Ancho * 0.31f, -0.14f, -0.005f);
        Vector3 fin = new Vector3(lado * prenda.Ancho * 0.49f, -prenda.Alto * 0.84f, 0.08f);
        Vector3 codo = Vector3.Lerp(inicio, fin, 0.57f) + new Vector3(lado * 0.045f, -0.02f, 0.025f);
        Vector3 eje = (fin - inicio).normalized;
        Vector3 ejeA = Vector3.Cross(eje, Vector3.forward).normalized;
        Vector3 ejeB = Vector3.Cross(eje, ejeA).normalized;
        for (int anillo = 0; anillo <= anillos; anillo++)
        {
            float t = anillo / (float)anillos;
            Vector3 centro = t < 0.57f ? Vector3.Lerp(inicio, codo, t / 0.57f) : Vector3.Lerp(codo, fin, (t - 0.57f) / 0.43f);
            float radio = Mathf.Lerp(0.105f, 0.065f, t) * (1f + 0.035f * Mathf.Sin(t * 15f + semilla));
            for (int ladoAnillo = 0; ladoAnillo < lados; ladoAnillo++)
            {
                float angulo = ladoAnillo * Mathf.PI * 2f / lados;
                vertices.Add(centro + (ejeA * Mathf.Cos(angulo) + ejeB * Mathf.Sin(angulo)) * radio);
                uv.Add(new Vector2(ladoAnillo / (float)lados, t * 1.8f));
            }
        }
        for (int anillo = 0; anillo < anillos; anillo++)
        {
            for (int ladoAnillo = 0; ladoAnillo < lados; ladoAnillo++)
            {
                int a = anillo * lados + ladoAnillo;
                int b = anillo * lados + (ladoAnillo + 1) % lados;
                AgregarCuadrilatero(triangulos, a, b, a + lados, b + lados);
            }
        }
        Mesh mesh = new Mesh { name = prenda.Nombre + (lado < 0 ? "_MangaIzquierda" : "_MangaDerecha") };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void CrearPanelLapel(Transform padre, string nombre, float x1, float y1, float x2, float y2, float z, Material material)
    {
        GameObject objeto = new GameObject(nombre);
        objeto.transform.SetParent(padre, false);
        Mesh mesh = new Mesh { name = nombre + "Mesh" };
        mesh.vertices = new[]
        {
            new Vector3(x1, y1, z), new Vector3(x2, y1, z), new Vector3((x1 + x2) * 0.5f, y2, z - 0.012f),
            new Vector3(x1, y1, z + 0.014f), new Vector3(x2, y1, z + 0.014f), new Vector3((x1 + x2) * 0.5f, y2, z + 0.002f)
        };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.zero, Vector2.right, Vector2.up };
        mesh.triangles = new[] { 0, 2, 1, 3, 4, 5, 0, 1, 3, 1, 4, 3, 1, 2, 4, 2, 5, 4, 2, 0, 5, 0, 3, 5 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        objeto.AddComponent<MeshFilter>().sharedMesh = mesh;
        objeto.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void CrearBolsillo(Transform padre, string nombre, float x, float y, Material material)
    {
        GameObject bolsillo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bolsillo.name = nombre;
        Object.DestroyImmediate(bolsillo.GetComponent<Collider>());
        bolsillo.transform.SetParent(padre, false);
        bolsillo.transform.localPosition = new Vector3(x, y, -0.12f);
        bolsillo.transform.localScale = new Vector3(0.13f, 0.16f, 0.025f);
        bolsillo.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CrearOjalSuperior(Transform raiz, string nombre, Material material)
    {
        CrearLazoSuperior(raiz, nombre, material);
    }

    private static void CrearLazoSuperior(Transform raiz, string nombre, Material material)
    {
        GameObject lazo = new GameObject("OjalSuperior");
        lazo.transform.SetParent(raiz, false);
        List<Vector3> puntos = new List<Vector3>();
        for (int i = 0; i <= 20; i++)
        {
            float t = i / 20f;
            float angulo = Mathf.Lerp(Mathf.PI, 0f, t);
            puntos.Add(new Vector3(Mathf.Cos(angulo) * 0.043f, -0.015f - Mathf.Sin(angulo) * 0.052f, -0.035f));
        }
        Mesh mesh = CrearMallaTuboSimple(nombre + "_Ojal", puntos, 0.006f, 11);
        lazo.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(mesh, nombre + "_Ojal.asset");
        lazo.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void CrearGancho(string nombre, bool hierro, int variante, Material material, bool recrear = false)
    {
        string prefabPath = Carpeta + "/" + nombre + ".prefab";
        if (!recrear && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        GameObject raiz = new GameObject(nombre);
        if (!hierro)
        {
            List<Vector3> camino = variante == 0
                ? new List<Vector3> { Vector3.zero, new Vector3(0f, 0f, 0.13f), new Vector3(0f, 0.012f, 0.23f) }
                : new List<Vector3> { Vector3.zero, new Vector3(0.012f, 0.01f, 0.08f), new Vector3(-0.01f, 0.04f, 0.17f), new Vector3(0.035f, 0.06f, 0.24f) };
            Mesh clavija = CrearMallaTuboSimple(nombre + "_Clavija", camino, variante == 0 ? 0.025f : 0.032f, 9);
            CrearObjetoMalla(raiz.transform, "Clavija", GuardarMalla(clavija, nombre + "_Clavija.asset"), material);
            if (variante == 1)
            {
                Mesh apoyo = CrearMallaTuboSimple(nombre + "_Apoyo", new List<Vector3> { new Vector3(0f, 0.035f, 0.16f), new Vector3(0f, 0.10f, 0.17f) }, 0.022f, 8);
                CrearObjetoMalla(raiz.transform, "ApoyoLateral", GuardarMalla(apoyo, nombre + "_Apoyo.asset"), material);
            }
        }
        else
        {
            CrearPlacaGancho(raiz.transform, nombre, material, variante);
            List<Vector3> camino = variante == 0
                ? new List<Vector3> { new Vector3(0f, 0.015f, 0.025f), new Vector3(0f, 0.01f, 0.13f), new Vector3(0f, -0.045f, 0.19f), new Vector3(0f, -0.10f, 0.16f), new Vector3(0f, -0.10f, 0.09f), new Vector3(0f, -0.065f, 0.075f) }
                : new List<Vector3> { new Vector3(0f, 0.065f, 0.02f), new Vector3(0f, 0.015f, 0.12f), new Vector3(0f, -0.045f, 0.17f), new Vector3(0f, -0.12f, 0.16f), new Vector3(0f, -0.15f, 0.10f), new Vector3(0f, -0.11f, 0.065f) };
            Mesh gancho = CrearMallaTuboSimple(nombre + "_Hierro", camino, variante == 0 ? 0.012f : 0.015f, 8);
            CrearObjetoMalla(raiz.transform, "GanchoForjado", GuardarMalla(gancho, nombre + "_Hierro.asset"), material);
        }
        GuardarPrefab(raiz, prefabPath);
    }

    private static void CrearPlacaGancho(Transform raiz, string nombre, Material material, int variante)
    {
        GameObject placa = GameObject.CreatePrimitive(PrimitiveType.Cube);
        placa.name = "PlacaArtesanal";
        Object.DestroyImmediate(placa.GetComponent<Collider>());
        placa.transform.SetParent(raiz.transform, false);
        placa.transform.localPosition = new Vector3(0f, 0f, 0.018f);
        placa.transform.localScale = variante == 0 ? new Vector3(0.095f, 0.12f, 0.025f) : new Vector3(0.12f, 0.16f, 0.03f);
        placa.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CrearObjetoMalla(Transform padre, string nombre, Mesh malla, Material material)
    {
        GameObject objeto = new GameObject(nombre);
        objeto.transform.SetParent(padre, false);
        objeto.AddComponent<MeshFilter>().sharedMesh = malla;
        objeto.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Mesh CrearMallaTuboSimple(string nombre, IList<Vector3> puntos, float radio, int lados)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> triangulos = new List<int>();
        AgregarTubo(vertices, uv, triangulos, puntos, radio, nombre.GetHashCode(), lados);
        Mesh mesh = new Mesh { name = nombre };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AgregarTubo(List<Vector3> vertices, List<Vector2> uv, List<int> triangulos,
        IList<Vector3> puntos, float radio, int semilla, int lados)
    {
        if (puntos.Count < 2) return;
        int inicio = vertices.Count;
        float longitud = 0f;
        for (int i = 1; i < puntos.Count; i++) longitud += Vector3.Distance(puntos[i - 1], puntos[i]);
        float recorrido = 0f;
        for (int anillo = 0; anillo < puntos.Count; anillo++)
        {
            Vector3 tangente = (puntos[Mathf.Min(anillo + 1, puntos.Count - 1)] - puntos[Mathf.Max(anillo - 1, 0)]).normalized;
            Vector3 referencia = Mathf.Abs(Vector3.Dot(tangente, Vector3.up)) > 0.91f ? Vector3.forward : Vector3.up;
            Vector3 ejeA = Vector3.Cross(tangente, referencia).normalized;
            Vector3 ejeB = Vector3.Cross(tangente, ejeA).normalized;
            if (anillo > 0) recorrido += Vector3.Distance(puntos[anillo - 1], puntos[anillo]);
            for (int lado = 0; lado < lados; lado++)
            {
                float angulo = lado * Mathf.PI * 2f / lados;
                float fibra = 1f + 0.055f * Mathf.Sin(angulo * 3f + anillo * 0.7f + semilla);
                vertices.Add(puntos[anillo] + (ejeA * Mathf.Cos(angulo) + ejeB * Mathf.Sin(angulo)) * radio * fibra);
                uv.Add(new Vector2(lado / (float)lados, longitud > 0f ? recorrido / longitud : 0f));
            }
        }
        for (int anillo = 0; anillo < puntos.Count - 1; anillo++)
        {
            for (int lado = 0; lado < lados; lado++)
            {
                int a = inicio + anillo * lados + lado;
                int b = inicio + anillo * lados + (lado + 1) % lados;
                AgregarCuadrilatero(triangulos, a, b, a + lados, b + lados);
            }
        }
    }

    private static Texture2D CargarOCrearTextura(string nombre, int semilla, bool normal, int patron = 0)
    {
        string ruta = MaterialesCarpeta + "/" + nombre;
        Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        bool crear = textura == null;
        const int resolucion = 128;
        if (crear)
        {
            textura = new Texture2D(resolucion, resolucion, TextureFormat.RGBA32, true, normal)
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
                float fibra = Mathf.PerlinNoise((x + semilla) * 0.07f, (y + semilla) * 0.18f);
                float trama = Mathf.PerlinNoise((x + semilla * 2) * 0.17f, (y + semilla) * 0.09f);
                if (normal)
                {
                    Vector3 n = new Vector3((fibra - 0.5f) * 0.48f, (trama - 0.5f) * 0.48f, 1f).normalized;
                    pixeles[y * resolucion + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                    continue;
                }

                float tono = 0.83f + (fibra - 0.5f) * 0.18f + (trama - 0.5f) * 0.12f;
                float borde = Mathf.Min(y, resolucion - 1 - y) / (float)resolucion;
                bool franja = patron > 0 && (borde < 0.075f || (patron % 2 == 0 && y % (20 + patron) < 2));
                if (franja) tono *= 0.63f + (trama - 0.5f) * 0.1f;
                if (patron >= 3 && x % (19 + patron) < 2) tono *= 0.78f;
                if (fibra < 0.23f) tono *= 0.9f;
                pixeles[y * resolucion + x] = new Color(tono, tono, tono, 1f);
            }
        }
        textura.SetPixels(pixeles);
        textura.Apply(true, false);
        if (crear) AssetDatabase.CreateAsset(textura, ruta);
        else EditorUtility.SetDirty(textura);
        return textura;
    }

    private static Material CrearMaterial(string nombre, Color color, Texture2D albedo, Texture2D normal,
        Texture2D suavidad, float metalico, float smoothness)
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
        if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", 0.35f);
        if (material.HasProperty("_MetallicGlossMap")) material.SetTexture("_MetallicGlossMap", suavidad);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metalico);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        material.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Mesh GuardarMalla(Mesh mesh, string nombre)
    {
        string ruta = MallasCarpeta + "/" + nombre;
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

    private static GameObject CrearGrupo(Transform padre, string nombre)
    {
        GameObject grupo = new GameObject(nombre);
        grupo.transform.SetParent(padre, false);
        return grupo;
    }

    private static void AgregarCuadrilatero(List<int> triangulos, int a, int b, int c, int d)
    {
        triangulos.Add(a); triangulos.Add(b); triangulos.Add(c);
        triangulos.Add(b); triangulos.Add(d); triangulos.Add(c);
    }

    private static void GuardarPrefab(GameObject objeto, string ruta)
    {
        PrefabUtility.SaveAsPrefabAsset(objeto, ruta);
        Object.DestroyImmediate(objeto);
    }
}