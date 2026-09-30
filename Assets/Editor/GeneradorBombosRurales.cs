using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GeneradorBombosRurales
{
    private const string Carpeta = "Assets/Models/BarRecursos/Bombos";
    private const string MallasCarpeta = Carpeta + "/Mallas";
    private const string MaterialesCarpeta = Carpeta + "/Materiales";
    private const string VersionEditorPrefs = "Trucardo.GeneradorBombosRurales.Version";

    private struct PerfilBombo
    {
        public string Nombre;
        public float Alto;
        public float Radio;
        public int Duelas;
        public int Tensores;
        public float Abarrilado;
        public float Deformacion;
        public float Desgaste;
        public float GrosorAro;
        public float CurvaturaParche;
        public float DesfaseTensores;
        public float Inclinacion;
        public Color Madera;
        public Color Cuero;
    }

    static GeneradorBombosRurales()
    {
        EditorApplication.delayCall += CrearBombosFaltantes;
    }

    [MenuItem("Trucardo/Crear bombos gauchos faltantes")]
    public static void CrearBombosFaltantes()
    {
        bool actualizarGeometria = EditorPrefs.GetInt(VersionEditorPrefs, 0) < 3;
        CrearBombos(actualizarGeometria);
        EditorPrefs.SetInt(VersionEditorPrefs, 3);
    }

    [MenuItem("Trucardo/Recrear bombos gauchos")]
    public static void RecrearBombos()
    {
        CrearBombos(true);
        EditorPrefs.SetInt(VersionEditorPrefs, 3);
    }

    private static void CrearBombos(bool recrear)
    {
        AsegurarCarpeta(Carpeta);
        AsegurarCarpeta(MallasCarpeta);
        AsegurarCarpeta(MaterialesCarpeta);

        Texture2D texturaMadera = CargarOCrearTextura("Albedo_Quebracho.asset", 17, 0);
        Texture2D normalMadera = CargarOCrearTextura("Normal_MaderaBombo.asset", 29, 1);
        Texture2D roughnessMadera = CargarOCrearTextura("Roughness_MaderaBombo.asset", 43, 2);
        Texture2D texturaCuero = CargarOCrearTextura("Albedo_CueroClaro.asset", 61, 3);
        Texture2D normalCuero = CargarOCrearTextura("Normal_CueroBombo.asset", 79, 1);
        Texture2D roughnessCuero = CargarOCrearTextura("Roughness_CueroBombo.asset", 97, 2);
        if (texturaMadera == null || normalMadera == null || roughnessMadera == null || texturaCuero == null ||
            normalCuero == null || roughnessCuero == null)
        {
            Debug.LogError("No se pudieron crear las texturas de los bombos.");
            return;
        }

        PerfilBombo[] perfiles =
        {
            new PerfilBombo { Nombre = "Bombo_03", Alto = 0.56f, Radio = 0.205f, Duelas = 10, Tensores = 6, Abarrilado = 0.018f, Deformacion = 0.011f, Desgaste = 0.78f, GrosorAro = 0.018f, CurvaturaParche = 0.006f, DesfaseTensores = 1.12f, Madera = new Color(0.32f, 0.23f, 0.16f), Cuero = new Color(0.74f, 0.64f, 0.48f) },
            new PerfilBombo { Nombre = "Bombo_05", Alto = 0.55f, Radio = 0.22f, Duelas = 10, Tensores = 10, Abarrilado = 0.03f, Deformacion = 0.029f, Desgaste = 0.62f, GrosorAro = 0.021f, CurvaturaParche = 0.007f, DesfaseTensores = 1.18f, Inclinacion = 0.035f, Madera = new Color(0.34f, 0.21f, 0.14f), Cuero = new Color(0.79f, 0.68f, 0.51f) },
            new PerfilBombo { Nombre = "Bombo_05", Alto = 0.55f, Radio = 0.22f, Duelas = 10, Tensores = 10, Abarrilado = 0.03f, Deformacion = 0.029f, Desgaste = 0.62f, GrosorAro = 0.021f, CurvaturaParche = 0.007f, DesfaseTensores = 1.18f, Inclinacion = 0.035f, Madera = new Color(0.34f, 0.21f, 0.14f), Cuero = new Color(0.79f, 0.68f, 0.51f) }
        };

        for (int i = 0; i < perfiles.Length; i++)
            CrearBombo(perfiles[i], texturaMadera, normalMadera, roughnessMadera, texturaCuero, normalCuero, roughnessCuero, recrear);

        CrearBaqueta("Baqueta_01", 0, texturaMadera, normalMadera, roughnessMadera, recrear);
        CrearBaqueta("Baqueta_02", 1, texturaMadera, normalMadera, roughnessMadera, recrear);

        LimpiarAssetsObsoletos();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Bombos gauchos individuales revisados en " + Carpeta);
    }

    private static void CrearBombo(PerfilBombo perfil, Texture2D texturaMadera, Texture2D normalMadera,
        Texture2D roughnessMadera, Texture2D texturaCuero, Texture2D normalCuero, Texture2D roughnessCuero,
        bool recrear)
    {
        string prefabPath = Carpeta + "/" + perfil.Nombre + ".prefab";
        if (!recrear && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        Material[] tablas = new Material[3];
        float[] variacionTonos = { 0.82f, 1f, 1.13f };
        for (int i = 0; i < tablas.Length; i++)
        {
            tablas[i] = CrearMaterial("Mat_Madera_" + perfil.Nombre + "_0" + (i + 1), perfil.Madera * variacionTonos[i],
                texturaMadera, normalMadera, roughnessMadera, 0f, 0.17f);
            if (tablas[i] == null) return;
        }

        Material cuero = CrearMaterial("Mat_Cuero_" + perfil.Nombre, perfil.Cuero,
            texturaCuero, normalCuero, roughnessCuero, 0f, 0.2f);
        Material cueroTensor = CrearMaterial("Mat_TensorCuero_" + perfil.Nombre, new Color(0.58f, 0.39f, 0.24f),
            texturaCuero, normalCuero, roughnessCuero, 0f, 0.2f);
        Material aro = CrearMaterial("Mat_AroMadera_" + perfil.Nombre, perfil.Madera * 0.79f,
            texturaMadera, normalMadera, roughnessMadera, 0f, 0.19f);
        if (cuero == null || cueroTensor == null || aro == null) return;

        GameObject raiz = new GameObject(perfil.Nombre);
        GameObject cuerpo = CrearGrupo(raiz.transform, "Cuerpo");
        Mesh mallaBase = TransformarMalla(perfil, CrearMallaCuerpoContinuo(perfil));
        CrearObjetoMalla(cuerpo.transform, "NucleoMadera", GuardarMalla(mallaBase, perfil.Nombre + "_Nucleo.asset"), tablas[1]);
        for (int i = 0; i < perfil.Duelas; i++)
        {
            Mesh tablaMesh = TransformarMalla(perfil, CrearMallaDuela(perfil, i));
            string nombreTabla = "Tabla_" + (i + 1).ToString("00");
            CrearObjetoMalla(cuerpo.transform, nombreTabla, GuardarMalla(tablaMesh, perfil.Nombre + "_" + nombreTabla + ".asset"), tablas[i % tablas.Length]);
        }

        float radioExtremo = PerfilRadio(perfil, 0.04f, perfil.Radio);
        Mesh meshAroSuperior = TransformarMalla(perfil, CrearMallaAro(perfil, perfil.Alto - 0.035f, radioExtremo, 0.045f, perfil.GrosorAro));
        CrearObjetoMalla(raiz.transform, "AroSuperior", GuardarMalla(meshAroSuperior, perfil.Nombre + "_AroSuperior.asset"), aro);
        Mesh meshAroInferior = TransformarMalla(perfil, CrearMallaAro(perfil, 0.035f, radioExtremo, 0.045f, perfil.GrosorAro));
        CrearObjetoMalla(raiz.transform, "AroInferior", GuardarMalla(meshAroInferior, perfil.Nombre + "_AroInferior.asset"), aro);

        GameObject parcheSuperior = CrearGrupo(raiz.transform, "ParcheSuperior");
        Mesh superiorMesh = TransformarMalla(perfil, CrearMallaParche(perfil, true));
        CrearObjetoMalla(parcheSuperior.transform, "CueroSuperior", GuardarMalla(superiorMesh, perfil.Nombre + "_ParcheSuperior.asset"), cuero);
        GameObject parcheInferior = CrearGrupo(raiz.transform, "ParcheInferior");
        Mesh inferiorMesh = TransformarMalla(perfil, CrearMallaParche(perfil, false));
        CrearObjetoMalla(parcheInferior.transform, "CueroInferior", GuardarMalla(inferiorMesh, perfil.Nombre + "_ParcheInferior.asset"), cuero);

        GameObject tensores = CrearGrupo(raiz.transform, "Tensores");
        CrearTensores(tensores.transform, perfil, cueroTensor);

        GuardarPrefab(raiz, prefabPath);
    }

    private static Mesh CrearMallaDuela(PerfilBombo perfil, int indice)
    {
        const int filas = 10;
        const int divisionesAncho = 2;
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> triangulos = new List<int>();
        float paso = Mathf.PI * 2f / perfil.Duelas;
        float anchoAngular = paso * 0.955f;
        float inicio = indice * paso - anchoAngular * 0.5f;
        float grosor = Mathf.Clamp(perfil.Radio * 0.052f, 0.012f, 0.018f);
        int columnas = divisionesAncho + 1;

        for (int cara = 0; cara < 2; cara++)
        {
            for (int fila = 0; fila <= filas; fila++)
            {
                float t = fila / (float)filas;
                for (int segmento = 0; segmento <= divisionesAncho; segmento++)
                {
                    float u = segmento / (float)divisionesAncho;
                    float a = inicio + u * anchoAngular;
                    float radio = PerfilRadio(perfil, t, perfil.Radio);
                    float ondulacion = perfil.Deformacion * 0.11f * Mathf.Sin(indice * 1.9f + t * 9f + u * 4f);
                    float variacion = 1f + ondulacion + perfil.Desgaste * 0.004f * Mathf.Sin(t * 39f + u * 8f + indice);
                    float r = (radio - (cara == 0 ? 0f : grosor)) * variacion;
                    float y = t * perfil.Alto + perfil.Deformacion * 0.12f * Mathf.Sin(a * 3f + t * 6f) * t;
                    vertices.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                    uv.Add(new Vector2(u, t));
                }
            }
        }

        int superficie = (filas + 1) * columnas;
        for (int cara = 0; cara < 2; cara++)
        {
            int baseCara = cara * superficie;
            for (int fila = 0; fila < filas; fila++)
            {
                for (int segmento = 0; segmento < divisionesAncho; segmento++)
                {
                    int a = baseCara + fila * columnas + segmento;
                    int b = a + columnas;
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

        for (int fila = 0; fila < filas; fila++)
        {
            int exterior = fila * columnas;
            int interior = superficie + exterior;
            AgregarCuadrilatero(triangulos, exterior, interior, exterior + columnas, interior + columnas);
            int exteriorDerecho = exterior + divisionesAncho;
            int interiorDerecho = interior + divisionesAncho;
            AgregarCuadrilatero(triangulos, exteriorDerecho, exteriorDerecho + columnas, interiorDerecho, interiorDerecho + columnas);
        }
        for (int segmento = 0; segmento < divisionesAncho; segmento++)
        {
            AgregarCuadrilatero(triangulos, segmento, segmento + 1, superficie + segmento, superficie + segmento + 1);
            int arriba = filas * columnas + segmento;
            AgregarCuadrilatero(triangulos, arriba, superficie + arriba, arriba + 1, superficie + arriba + 1);
        }

        Mesh mesh = new Mesh { name = perfil.Nombre + "_Duela_" + (indice + 1).ToString("00") };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void ValidarNormalExteriorParche(Mesh mesh, bool superior)
    {
        Vector3 normalEsperada = superior ? Vector3.up : Vector3.down;
        int triangulosSuperficie = 6 * 48 * 6;
        Vector3[] vertices = mesh.vertices;
        int[] triangulos = mesh.triangles;
        for (int i = 0; i < triangulosSuperficie; i += 3)
        {
            Vector3 a = vertices[triangulos[i]];
            Vector3 b = vertices[triangulos[i + 1]];
            Vector3 c = vertices[triangulos[i + 2]];
            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < 0.0000001f) continue;
            if (Vector3.Dot(normal.normalized, normalEsperada) < 0.5f)
                throw new System.InvalidOperationException("Normal exterior invertida en " + mesh.name);
            return;
        }
        throw new System.InvalidOperationException("No se encontro una cara valida en " + mesh.name);
    }

    private static Mesh CrearMallaCuerpoContinuo(PerfilBombo perfil)
    {
        const int segmentos = 48;
        const int anillos = 14;
        List<Vector3> vertices = new List<Vector3>((anillos + 1) * (segmentos + 1));
        List<Vector2> uv = new List<Vector2>(vertices.Capacity);
        List<int> triangulos = new List<int>(anillos * segmentos * 6);
        for (int anillo = 0; anillo <= anillos; anillo++)
        {
            float t = anillo / (float)anillos;
            float radio = PerfilRadio(perfil, t, perfil.Radio) - 0.009f;
            for (int segmento = 0; segmento <= segmentos; segmento++)
            {
                float a = segmento * Mathf.PI * 2f / segmentos;
                vertices.Add(new Vector3(Mathf.Cos(a) * radio, t * perfil.Alto, Mathf.Sin(a) * radio));
                uv.Add(new Vector2(segmento / (float)segmentos, t));
            }
        }
        for (int anillo = 0; anillo < anillos; anillo++)
        {
            for (int segmento = 0; segmento < segmentos; segmento++)
            {
                int a = anillo * (segmentos + 1) + segmento;
                int b = a + segmentos + 1;
                triangulos.Add(a); triangulos.Add(b); triangulos.Add(a + 1);
                triangulos.Add(a + 1); triangulos.Add(b); triangulos.Add(b + 1);
            }
        }
        Mesh mesh = new Mesh { name = perfil.Nombre + "_CuerpoContinuo" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CrearMallaParche(PerfilBombo perfil, bool superior)
    {
        const int segmentos = 48;
        const int anillos = 6;
        List<Vector3> vertices = new List<Vector3>((anillos + 1) * (segmentos + 1));
        List<Vector2> uv = new List<Vector2>(vertices.Capacity);
        List<int> triangulos = new List<int>(anillos * segmentos * 6);
        float radio = PerfilRadio(perfil, superior ? 0.99f : 0.01f, perfil.Radio) - 0.018f;
        float centroY = superior ? perfil.Alto - 0.046f : 0.046f;
        float sag = perfil.CurvaturaParche;

        for (int anillo = 0; anillo <= anillos; anillo++)
        {
            float r = radio * anillo / anillos;
            for (int i = 0; i <= segmentos; i++)
            {
                float a = i * Mathf.PI * 2f / segmentos;
                float ondulacion = perfil.Deformacion * 0.06f * Mathf.Sin(a * 5f + anillo * 0.8f);
                float y = centroY + (superior ? 1f : -1f) * sag * (1f - Mathf.Pow(r / radio, 2f)) + ondulacion;
                vertices.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                uv.Add(new Vector2(0.5f + Mathf.Cos(a) * r / (radio * 2f), 0.5f + Mathf.Sin(a) * r / (radio * 2f)));
            }
        }

        for (int anillo = 0; anillo < anillos; anillo++)
        {
            for (int i = 0; i < segmentos; i++)
            {
                int a = anillo * (segmentos + 1) + i;
                int b = a + segmentos + 1;
                if (superior)
                {
                    triangulos.Add(a); triangulos.Add(a + 1); triangulos.Add(b);
                    triangulos.Add(a + 1); triangulos.Add(b + 1); triangulos.Add(b);
                }
                else
                {
                    triangulos.Add(a); triangulos.Add(b); triangulos.Add(a + 1);
                    triangulos.Add(a + 1); triangulos.Add(b); triangulos.Add(b + 1);
                }
            }
        }

        int superficie = (anillos + 1) * (segmentos + 1);
        for (int i = 0; i < segmentos; i++)
        {
            int top = anillos * (segmentos + 1) + i;
            int topSiguiente = top + 1;
            int bottom = superficie + top;
            int bottomSiguiente = bottom + 1;
            triangulos.Add(top); triangulos.Add(topSiguiente); triangulos.Add(bottom);
            triangulos.Add(topSiguiente); triangulos.Add(bottomSiguiente); triangulos.Add(bottom);
        }

        Mesh mesh = new Mesh { name = perfil.Nombre + (superior ? "_CueroSuperior" : "_CueroInferior") };
        List<Vector3> verticesInferiores = new List<Vector3>(vertices.Count);
        List<Vector2> uvInferiores = new List<Vector2>(uv.Count);
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 punto = vertices[i];
            punto.y += superior ? -0.006f : 0.006f;
            verticesInferiores.Add(punto);
            uvInferiores.Add(uv[i]);
        }
        vertices.AddRange(verticesInferiores);
        uv.AddRange(uvInferiores);
        int offsetInterior = superficie;
        for (int anillo = 0; anillo < anillos; anillo++)
        {
            for (int i = 0; i < segmentos; i++)
            {
                int a = offsetInterior + anillo * (segmentos + 1) + i;
                int b = a + segmentos + 1;
                if (superior)
                {
                    triangulos.Add(a); triangulos.Add(b); triangulos.Add(a + 1);
                    triangulos.Add(a + 1); triangulos.Add(b); triangulos.Add(b + 1);
                }
                else
                {
                    triangulos.Add(a); triangulos.Add(a + 1); triangulos.Add(b);
                    triangulos.Add(a + 1); triangulos.Add(b + 1); triangulos.Add(b);
                }
            }
        }
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        ValidarNormalExteriorParche(mesh, superior);
        return mesh;
    }

    private static Mesh CrearMallaAro(PerfilBombo perfil, float centroY, float radio, float alto, float grosor)
    {
        const int segmentos = 48;
        List<Vector3> vertices = new List<Vector3>(segmentos * 8);
        List<Vector2> uv = new List<Vector2>(segmentos * 8);
        List<int> triangulos = new List<int>(segmentos * 24);
        for (int i = 0; i < segmentos; i++)
        {
            float a = i * Mathf.PI * 2f / segmentos;
            float irregularidad = perfil.Deformacion * 0.12f * Mathf.Sin(a * 5f + i * 0.1f);
            float rOuter = radio + grosor * 0.5f + irregularidad;
            float rInner = radio - grosor * 0.5f + irregularidad;
            float y0 = centroY - alto * 0.5f + perfil.Deformacion * 0.025f * Mathf.Sin(a * 3f);
            float y1 = centroY + alto * 0.5f + perfil.Deformacion * 0.025f * Mathf.Sin(a * 3f);
            vertices.Add(new Vector3(Mathf.Cos(a) * rOuter, y0, Mathf.Sin(a) * rOuter));
            vertices.Add(new Vector3(Mathf.Cos(a) * rOuter, y1, Mathf.Sin(a) * rOuter));
            vertices.Add(new Vector3(Mathf.Cos(a) * rInner, y0, Mathf.Sin(a) * rInner));
            vertices.Add(new Vector3(Mathf.Cos(a) * rInner, y1, Mathf.Sin(a) * rInner));
            for (int j = 0; j < 4; j++) uv.Add(new Vector2(i / (float)segmentos, j % 2));
        }
        for (int i = 0; i < segmentos; i++)
        {
            int a = i * 4;
            int b = ((i + 1) % segmentos) * 4;
            AgregarCuadrilatero(triangulos, a, b, a + 1, b + 1);
            AgregarCuadrilatero(triangulos, a + 2, a + 3, b + 2, b + 3);
            AgregarCuadrilatero(triangulos, a + 1, b + 1, a + 3, b + 3);
            AgregarCuadrilatero(triangulos, a, a + 2, b, b + 2);
        }
        Mesh mesh = new Mesh { name = perfil.Nombre + "_Aro" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void CrearTensores(Transform padre, PerfilBombo perfil, Material material)
    {
        float paso = Mathf.PI * 2f / perfil.Tensores;
        for (int i = 0; i < perfil.Tensores; i++)
        {
            float angulo = i * paso;
            int desplazamiento = (i % 2 == 0) ? 1 : -1;
            float anguloDestino = angulo + desplazamiento * paso * perfil.DesfaseTensores;
            List<Vector3> puntos = new List<Vector3>();
            const int divisiones = 8;
            for (int punto = 0; punto <= divisiones; punto++)
            {
                float t = punto / (float)divisiones;
                float a = Mathf.Lerp(angulo, anguloDestino, t);
                float r = PerfilRadio(perfil, t, perfil.Radio) + 0.015f;
                float y = Mathf.Lerp(perfil.Alto - 0.03f, 0.03f, t);
                float nudo = 0.003f * Mathf.Sin(t * Mathf.PI * 6f + i);
                puntos.Add(new Vector3(Mathf.Cos(a) * r, y + nudo, Mathf.Sin(a) * r));
            }
            string nombreTensor = "TensorCuero_" + (i + 1).ToString("00");
            Mesh mesh = TransformarMalla(perfil, CrearMallaTiraCuero(perfil.Nombre + "_" + nombreTensor, puntos, 0.021f, 0.006f));
            CrearObjetoMalla(padre, nombreTensor, GuardarMalla(mesh, perfil.Nombre + "_" + nombreTensor + ".asset"), material);
            CrearFijacionTensor(padre, perfil, i, angulo, true, material);
            CrearFijacionTensor(padre, perfil, i, anguloDestino, false, material);
        }
    }

    private static Mesh CrearMallaTiraCuero(string nombre, IList<Vector3> puntos, float ancho, float grosor)
    {
        List<Vector3> vertices = new List<Vector3>(puntos.Count * 4);
        List<Vector2> uv = new List<Vector2>(puntos.Count * 4);
        List<int> triangulos = new List<int>((puntos.Count - 1) * 24);
        for (int i = 0; i < puntos.Count; i++)
        {
            Vector3 tangente = (puntos[Mathf.Min(i + 1, puntos.Count - 1)] - puntos[Mathf.Max(i - 1, 0)]).normalized;
            Vector3 radial = new Vector3(puntos[i].x, 0f, puntos[i].z).normalized;
            Vector3 ejeAncho = Vector3.Cross(radial, tangente).normalized;
            if (ejeAncho.sqrMagnitude < 0.01f) ejeAncho = Vector3.forward;
            float ondulacion = 1f + 0.04f * Mathf.Sin(i * 1.3f + nombre.GetHashCode() * 0.001f);
            Vector3 centro = puntos[i] + radial * (grosor * 0.5f);
            Vector3 lateral = ejeAncho * ancho * 0.5f * ondulacion;
            Vector3 espesor = radial * grosor * 0.5f;
            vertices.Add(centro - lateral - espesor);
            vertices.Add(centro + lateral - espesor);
            vertices.Add(centro + lateral + espesor);
            vertices.Add(centro - lateral + espesor);
            uv.Add(new Vector2(0f, i / (float)(puntos.Count - 1)));
            uv.Add(new Vector2(1f, i / (float)(puntos.Count - 1)));
            uv.Add(new Vector2(1f, i / (float)(puntos.Count - 1)));
            uv.Add(new Vector2(0f, i / (float)(puntos.Count - 1)));
        }
        for (int i = 0; i < puntos.Count - 1; i++)
        {
            int a = i * 4;
            int b = a + 4;
            AgregarCuadrilatero(triangulos, a, b, a + 1, b + 1);
            AgregarCuadrilatero(triangulos, a + 1, b + 1, a + 2, b + 2);
            AgregarCuadrilatero(triangulos, a + 2, b + 2, a + 3, b + 3);
            AgregarCuadrilatero(triangulos, a + 3, b + 3, a, b);
        }
        Mesh mesh = new Mesh { name = nombre };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void CrearFijacionTensor(Transform padre, PerfilBombo perfil, int indice, float angulo, bool superior, Material material)
    {
        float t = superior ? 0.94f : 0.06f;
        float radio = PerfilRadio(perfil, t, perfil.Radio) + 0.018f;
        GameObject fijacion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fijacion.name = "Nudo_" + (indice + 1).ToString("00") + (superior ? "_Superior" : "_Inferior");
        Object.DestroyImmediate(fijacion.GetComponent<Collider>());
        fijacion.transform.SetParent(padre, false);
        Vector3 posicion = new Vector3(Mathf.Cos(angulo) * radio, t * perfil.Alto, Mathf.Sin(angulo) * radio);
        fijacion.transform.localPosition = TransformarPunto(perfil, posicion);
        Quaternion orientacion = Quaternion.FromToRotation(Vector3.up, new Vector3(Mathf.Cos(angulo), 0.25f, Mathf.Sin(angulo)));
        fijacion.transform.localRotation = orientacion;
        fijacion.transform.localScale = new Vector3(0.026f, 0.014f, 0.02f);
        fijacion.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Mesh TransformarMalla(PerfilBombo perfil, Mesh mesh)
    {
        if (Mathf.Abs(perfil.Inclinacion) < 0.0001f) return mesh;
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = TransformarPunto(perfil, vertices[i]);
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Vector3 TransformarPunto(PerfilBombo perfil, Vector3 punto)
    {
        punto.x += perfil.Inclinacion * Mathf.Clamp01(punto.y / perfil.Alto);
        return punto;
    }

    private static void LimpiarAssetsObsoletos()
    {
        string[] bombosEliminados = { "Bombo_01", "Bombo_02", "Bombo_04", "Bombo_06", "Bombo_07", "Bombo_08" };
        for (int i = 0; i < bombosEliminados.Length; i++)
        {
            string nombre = bombosEliminados[i];
            AssetDatabase.DeleteAsset(Carpeta + "/" + nombre + ".prefab");
            LimpiarCarpetaPorNombre(MallasCarpeta, nombre + "_");
            LimpiarCarpetaPorNombre(MaterialesCarpeta, "_" + nombre);
        }
        LimpiarCarpetaPorNombre(MaterialesCarpeta, "Mat_Soga_Bombo_");
        AssetDatabase.DeleteAsset(MaterialesCarpeta + "/Albedo_SogaBombo.asset");
    }

    private static void LimpiarCarpetaPorNombre(string carpeta, string fragmento)
    {
        string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { carpeta });
        for (int i = 0; i < guids.Length; i++)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guids[i]);
            string nombre = System.IO.Path.GetFileNameWithoutExtension(ruta);
            if (nombre.Contains(fragmento)) AssetDatabase.DeleteAsset(ruta);
        }
    }

    private static void CrearBaqueta(string nombre, int variante, Texture2D albedo, Texture2D normal, Texture2D roughness, bool recrear)
    {
        string prefabPath = Carpeta + "/" + nombre + ".prefab";
        if (!recrear && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;
        Material madera = CrearMaterial("Mat_" + nombre, variante == 0 ? new Color(0.57f, 0.37f, 0.22f) : new Color(0.43f, 0.28f, 0.18f),
            albedo, normal, roughness, 0f, 0.18f);
        if (madera == null) return;

        float largo = variante == 0 ? 0.38f : 0.34f;
        float radio = variante == 0 ? 0.014f : 0.017f;
        GameObject raiz = new GameObject(nombre);
        List<Vector3> puntos = new List<Vector3>
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, largo * 0.7f, 0.003f),
            new Vector3(variante == 0 ? 0.004f : -0.003f, largo, 0f)
        };
        Mesh mesh = CrearMallaCordel(nombre + "Mesh", puntos, radio, 9);
        CrearObjetoMalla(raiz.transform, "Madera", GuardarMalla(mesh, nombre + ".asset"), madera);
        GuardarPrefab(raiz, prefabPath);
    }

    private static float PerfilRadio(PerfilBombo perfil, float t, float radio)
    {
        return radio * (1f - perfil.Abarrilado + perfil.Abarrilado * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI));
    }

    private static Mesh CrearMallaCordel(string nombre, IList<Vector3> puntos, float radio, int lados)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> triangulos = new List<int>();
        for (int i = 0; i < puntos.Count; i++)
        {
            Vector3 tangente = (puntos[Mathf.Min(i + 1, puntos.Count - 1)] - puntos[Mathf.Max(i - 1, 0)]).normalized;
            Vector3 referencia = Mathf.Abs(Vector3.Dot(tangente, Vector3.up)) > 0.92f ? Vector3.forward : Vector3.up;
            Vector3 ejeA = Vector3.Cross(tangente, referencia).normalized;
            Vector3 ejeB = Vector3.Cross(tangente, ejeA).normalized;
            for (int lado = 0; lado < lados; lado++)
            {
                float angulo = lado * Mathf.PI * 2f / lados;
                float fibra = 1f + 0.06f * Mathf.Sin(angulo * 3f + i * 0.8f);
                vertices.Add(puntos[i] + (ejeA * Mathf.Cos(angulo) + ejeB * Mathf.Sin(angulo)) * radio * fibra);
                uv.Add(new Vector2(lado / (float)lados, i / (float)(puntos.Count - 1)));
            }
        }
        for (int i = 0; i < puntos.Count - 1; i++)
        {
            for (int lado = 0; lado < lados; lado++)
            {
                int a = i * lados + lado;
                int b = i * lados + (lado + 1) % lados;
                triangulos.Add(a); triangulos.Add(b); triangulos.Add(a + lados);
                triangulos.Add(b); triangulos.Add(b + lados); triangulos.Add(a + lados);
            }
        }
        Mesh mesh = new Mesh { name = nombre };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
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
                float fibra = Mathf.PerlinNoise((x + semilla) * 0.08f, (y + semilla) * 0.14f);
                float veta = Mathf.PerlinNoise((x + semilla * 2) * 0.035f, (y + semilla) * 0.19f);
                if (tipo == 1)
                {
                    Vector3 n = new Vector3((fibra - 0.5f) * 0.42f, (veta - 0.5f) * 0.4f, 1f).normalized;
                    pixeles[y * resolucion + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
                else if (tipo == 2)
                {
                    float roughness = Mathf.Clamp01(0.76f + fibra * 0.1f + veta * 0.08f);
                    pixeles[y * resolucion + x] = new Color(0.72f, 0f, 0f, roughness);
                }
                else if (tipo == 3)
                {
                    float mancha = Mathf.PerlinNoise((x + semilla) * 0.025f, (y - semilla) * 0.03f);
                    float cuero = Mathf.Clamp(0.88f + (fibra - 0.5f) * 0.12f + (veta - 0.5f) * 0.08f, 0.72f, 1f);
                    cuero *= mancha < 0.24f ? 0.91f : 1f;
                    pixeles[y * resolucion + x] = new Color(cuero, cuero * 0.97f, cuero * 0.9f, 1f);
                }
                else
                {
                    float hilo = Mathf.Clamp(0.88f + (fibra - 0.5f) * 0.13f + (veta - 0.5f) * 0.1f, 0.68f, 1f);
                    if (x % 9 < 2) hilo *= 0.9f;
                    pixeles[y * resolucion + x] = new Color(hilo, hilo * 0.89f, hilo * 0.72f, 1f);
                }
            }
        }
        textura.SetPixels(pixeles);
        textura.Apply(true, false);
        if (crear) AssetDatabase.CreateAsset(textura, ruta);
        else EditorUtility.SetDirty(textura);
        return textura;
    }

    private static Material CrearMaterial(string nombre, Color color, Texture2D albedo, Texture2D normal,
        Texture2D roughness, float metalico, float smoothness)
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
        if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", 0.32f);
        if (material.HasProperty("_MetallicGlossMap")) material.SetTexture("_MetallicGlossMap", roughness);
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

    private static GameObject CrearGrupo(Transform padre, string nombre)
    {
        GameObject grupo = new GameObject(nombre);
        grupo.transform.SetParent(padre, false);
        return grupo;
    }

    private static void CrearObjetoMalla(Transform padre, string nombre, Mesh mesh, Material material)
    {
        GameObject objeto = new GameObject(nombre);
        objeto.transform.SetParent(padre, false);
        objeto.AddComponent<MeshFilter>().sharedMesh = mesh;
        objeto.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void AgregarCuadrilatero(List<int> triangulos, int a, int b, int c, int d)
    {
        triangulos.Add(a); triangulos.Add(b); triangulos.Add(c);
        triangulos.Add(b); triangulos.Add(d); triangulos.Add(c);
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