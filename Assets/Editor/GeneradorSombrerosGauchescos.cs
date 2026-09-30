using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GeneradorSombrerosGauchescos
{
    private const string Carpeta = "Assets/Models/BarRecursos/SombrerosGauchos";
    private const string MallasCarpeta = Carpeta + "/Mallas";
    private const string MaterialesCarpeta = Carpeta + "/Materiales";

    private struct PerfilSombrero
    {
        public string Nombre;
        public float RadioAla;
        public float RadioCopa;
        public float AltoCopa;
        public float CurvaAla;
        public float Deformacion;
        public float Hundimiento;
        public float Desgaste;
        public bool Cinta;
        public bool Lazo;
        public Color Paja;
        public Color Banda;
    }

    static GeneradorSombrerosGauchescos()
    {
        EditorApplication.delayCall += CrearSombrerosFaltantes;
    }

    [MenuItem("Trucardo/Crear sombreros gauchescos faltantes")]
    public static void CrearSombrerosFaltantes()
    {
        CrearSombreros(false);
    }

    [MenuItem("Trucardo/Recrear sombreros gauchescos")]
    public static void RecrearSombreros()
    {
        CrearSombreros(true);
    }

    private static void CrearSombreros(bool recrear)
    {
        AsegurarCarpetas();
        Texture2D texturaPaja = CargarOCrearTextura("Albedo_PajaClara.asset", 31, 0);
        Texture2D normalPaja = CargarOCrearTextura("Normal_Paja.asset", 53, 1);
        Texture2D suavidadPaja = CargarOCrearTextura("Suavidad_Paja.asset", 79, 2);
        Texture2D texturaCinta = CargarOCrearTextura("Albedo_CintaTejida.asset", 97, 3);
        if (texturaPaja == null || normalPaja == null || suavidadPaja == null || texturaCinta == null)
        {
            Debug.LogError("No se pudieron crear las texturas claras de los sombreros gauchescos.");
            return;
        }

        PerfilSombrero[] perfiles =
        {
            new PerfilSombrero { Nombre = "SombreroGaucho_02", RadioAla = 0.235f, RadioCopa = 0.105f, AltoCopa = 0.145f, CurvaAla = 0.014f, Deformacion = 0.012f, Hundimiento = 0.016f, Desgaste = 0.48f, Cinta = true, Lazo = true, Paja = new Color(0.68f, 0.59f, 0.44f), Banda = new Color(0.47f, 0.36f, 0.24f) },
            new PerfilSombrero { Nombre = "SombreroGaucho_03", RadioAla = 0.25f, RadioCopa = 0.115f, AltoCopa = 0.17f, CurvaAla = -0.012f, Deformacion = 0.006f, Hundimiento = 0.005f, Desgaste = 0.19f, Cinta = true, Lazo = true, Paja = new Color(0.84f, 0.73f, 0.55f), Banda = new Color(0.68f, 0.52f, 0.32f) },
            new PerfilSombrero { Nombre = "SombreroGaucho_04", RadioAla = 0.24f, RadioCopa = 0.11f, AltoCopa = 0.16f, CurvaAla = 0.021f, Deformacion = 0.024f, Hundimiento = 0.012f, Desgaste = 0.61f, Cinta = true, Lazo = false, Paja = new Color(0.66f, 0.61f, 0.49f), Banda = new Color(0.51f, 0.43f, 0.31f) },
            new PerfilSombrero { Nombre = "SombreroGaucho_05", RadioAla = 0.255f, RadioCopa = 0.12f, AltoCopa = 0.18f, CurvaAla = 0.017f, Deformacion = 0.013f, Hundimiento = 0.015f, Desgaste = 0.38f, Cinta = true, Lazo = true, Paja = new Color(0.78f, 0.66f, 0.48f), Banda = new Color(0.59f, 0.40f, 0.25f) }
        };

        bool hayVariantesAntiguas = false;
        for (int i = 6; i <= 10; i++)
            hayVariantesAntiguas |= AssetDatabase.LoadAssetAtPath<GameObject>(Carpeta + "/SombreroGaucho_" + i.ToString("00") + ".prefab") != null;
        recrear |= hayVariantesAntiguas;

        for (int i = 0; i < perfiles.Length; i++)
            CrearSombrero(perfiles[i], texturaPaja, normalPaja, suavidadPaja, texturaCinta, recrear);

        for (int i = 6; i <= 10; i++)
            AssetDatabase.DeleteAsset(Carpeta + "/SombreroGaucho_" + i.ToString("00") + ".prefab");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Sombreros gauchescos individuales revisados en " + Carpeta);
    }

    private static void CrearSombrero(PerfilSombrero perfil, Texture2D texturaPaja, Texture2D normalPaja,
        Texture2D suavidadPaja, Texture2D texturaCinta, bool recrear)
    {
        string prefabPath = Carpeta + "/" + perfil.Nombre + ".prefab";
        Material paja = CrearMaterial("Mat_Paja_" + perfil.Nombre, perfil.Paja, texturaPaja, normalPaja, suavidadPaja, 0.17f);
        Material interior = CrearMaterial("Mat_InteriorPaja_" + perfil.Nombre, perfil.Paja * 0.84f, texturaPaja, normalPaja, suavidadPaja, 0.2f);
        Material banda = CrearMaterial("Mat_Cinta_" + perfil.Nombre, perfil.Banda, texturaCinta, normalPaja, suavidadPaja, 0.22f);
        Material costura = CrearMaterial("Mat_Costura_" + perfil.Nombre, Color.Lerp(perfil.Paja, perfil.Banda, 0.38f), texturaPaja, normalPaja, suavidadPaja, 0.16f);
        if (paja == null || interior == null || banda == null || costura == null) return;
        if (!recrear && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        int semilla = Mathf.Abs(perfil.Nombre.GetHashCode());
        GameObject raiz = new GameObject(perfil.Nombre);
        GameObject sombrero = new GameObject("SombreroMesh");
        sombrero.transform.SetParent(raiz.transform, false);
        sombrero.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(CrearMallaCuerpo(perfil, semilla), perfil.Nombre + "_Cuerpo.asset");
        sombrero.AddComponent<MeshRenderer>().sharedMaterial = paja;

        GameObject forro = new GameObject("BandaInterior");
        forro.transform.SetParent(raiz.transform, false);
        forro.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(CrearMallaInterior(perfil, semilla), perfil.Nombre + "_Interior.asset");
        forro.AddComponent<MeshRenderer>().sharedMaterial = interior;

        if (perfil.Cinta)
        {
            GameObject cinta = new GameObject("Cinta");
            cinta.transform.SetParent(raiz.transform, false);
            cinta.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(CrearMallaCinta(perfil, semilla), perfil.Nombre + "_Cinta.asset");
            cinta.AddComponent<MeshRenderer>().sharedMaterial = banda;
            GameObject pespunte = new GameObject("CosturaCinta");
            pespunte.transform.SetParent(raiz.transform, false);
            pespunte.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(CrearMallaCostura(perfil, semilla), perfil.Nombre + "_Costura.asset");
            pespunte.AddComponent<MeshRenderer>().sharedMaterial = costura;
        }

        if (perfil.Lazo)
        {
            GameObject lazo = new GameObject("LazoColgado");
            lazo.transform.SetParent(raiz.transform, false);
            lazo.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(CrearMallaLazo(perfil, semilla), perfil.Nombre + "_Lazo.asset");
            lazo.AddComponent<MeshRenderer>().sharedMaterial = banda;
        }

        GuardarPrefab(raiz, prefabPath);
    }

    private static Mesh CrearMallaCuerpo(PerfilSombrero perfil, int semilla)
    {
        const int segmentos = 48;
        const int filasAla = 8;
        const int filasCopa = 18;
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> triangulos = new List<int>();
        float baseY = -perfil.AltoCopa * 0.78f;
        const float grosorAla = 0.004f;

        for (int fila = 0; fila <= filasCopa; fila++)
        {
            float t = fila / (float)filasCopa;
            float y;
            float radio;
            if (t < 0.78f)
            {
                float tCopa = t / 0.78f;
                y = baseY + perfil.AltoCopa * 0.82f * tCopa;
                radio = perfil.RadioCopa * (1f + 0.06f * Mathf.Sin(tCopa * Mathf.PI * 0.72f) - 0.26f * tCopa);
            }
            else
            {
                float domo = (t - 0.78f) / 0.22f;
                float anguloDomo = domo * Mathf.PI * 0.5f;
                float radioCima = perfil.RadioCopa * 0.8f;
                y = baseY + perfil.AltoCopa * (0.82f + 0.18f * Mathf.Sin(anguloDomo));
                radio = radioCima * Mathf.Cos(anguloDomo);
            }
            for (int i = 0; i <= segmentos; i++)
            {
                float angulo = i * Mathf.PI * 2f / segmentos;
                float deformacion = 1f + perfil.Deformacion * (Mathf.Sin(angulo * 3f + semilla) * 0.48f + Mathf.Sin(angulo * 5f + semilla * 0.3f) * 0.25f);
                float hundimiento = -perfil.Hundimiento * Mathf.Pow(Mathf.Clamp01((t - 0.72f) / 0.28f), 2f) * Mathf.Pow(Mathf.Max(0f, Mathf.Cos(angulo + 0.55f)), 4f);
                float x = Mathf.Cos(angulo) * radio * deformacion;
                float z = Mathf.Sin(angulo) * radio * deformacion;
                vertices.Add(new Vector3(x, y + hundimiento, z));
                uv.Add(new Vector2(i / (float)segmentos, t));
            }
        }

        for (int fila = 0; fila < filasCopa; fila++)
        {
            for (int i = 0; i < segmentos; i++)
            {
                int a = fila * (segmentos + 1) + i;
                int b = a + segmentos + 1;
                triangulos.Add(a); triangulos.Add(b); triangulos.Add(a + 1);
                triangulos.Add(a + 1); triangulos.Add(b); triangulos.Add(b + 1);
            }
        }

        int baseAla = vertices.Count;
        for (int fila = 1; fila <= filasAla; fila++)
        {
            float tAla = fila / (float)filasAla;
            float radio = Mathf.Lerp(perfil.RadioCopa, perfil.RadioAla, tAla);
            for (int i = 0; i <= segmentos; i++)
            {
                float angulo = i * Mathf.PI * 2f / segmentos;
                float frontal = Mathf.Sin(angulo);
                float lateral = Mathf.Cos(angulo);
                float ondulacion = perfil.Deformacion * tAla * (Mathf.Sin(angulo * 3f + semilla) * 0.45f + Mathf.Sin(angulo * 7f + semilla * 0.21f) * 0.2f);
                float desgaste = perfil.Desgaste * 0.002f * tAla * Mathf.Sin(angulo * 13f + semilla * 0.1f);
                float y = baseY + tAla * (perfil.CurvaAla * (frontal * frontal - 0.25f) + perfil.CurvaAla * 0.35f * lateral)
                    + perfil.Deformacion * 0.025f * Mathf.Sin(angulo * 2f + semilla) * tAla;
                float x = Mathf.Cos(angulo) * radio * (1f + ondulacion + desgaste);
                float z = Mathf.Sin(angulo) * radio * (1f + ondulacion * 0.7f + desgaste);
                vertices.Add(new Vector3(x, y, z));
                uv.Add(new Vector2(0.5f + x / (perfil.RadioAla * 2f), 0.5f + z / (perfil.RadioAla * 2f)));
            }
        }
        for (int fila = 0; fila < filasAla; fila++)
        {
            int filaInterior = fila == 0 ? 0 : baseAla + (fila - 1) * (segmentos + 1);
            int filaExterior = baseAla + fila * (segmentos + 1);
            for (int i = 0; i < segmentos; i++)
            {
                int a = filaInterior + i;
                int b = filaExterior + i;
                triangulos.Add(a); triangulos.Add(a + 1); triangulos.Add(b);
                triangulos.Add(a + 1); triangulos.Add(b + 1); triangulos.Add(b);
            }
        }

        int baseAlaInferior = vertices.Count;
        for (int fila = 0; fila <= filasAla; fila++)
        {
            float tAla = fila / (float)filasAla;
            float radio = Mathf.Lerp(perfil.RadioCopa * 0.9f, perfil.RadioAla, tAla);
            for (int i = 0; i <= segmentos; i++)
            {
                float angulo = i * Mathf.PI * 2f / segmentos;
                float frontal = Mathf.Sin(angulo);
                float lateral = Mathf.Cos(angulo);
                float ondulacion = perfil.Deformacion * tAla * (Mathf.Sin(angulo * 3f + semilla) * 0.45f + Mathf.Sin(angulo * 7f + semilla * 0.21f) * 0.2f);
                float desgaste = perfil.Desgaste * 0.002f * tAla * Mathf.Sin(angulo * 13f + semilla * 0.1f);
                float y = baseY + tAla * (perfil.CurvaAla * (frontal * frontal - 0.25f) + perfil.CurvaAla * 0.35f * lateral)
                    + perfil.Deformacion * 0.025f * Mathf.Sin(angulo * 2f + semilla) * tAla - grosorAla;
                float x = Mathf.Cos(angulo) * radio * (1f + ondulacion + desgaste);
                float z = Mathf.Sin(angulo) * radio * (1f + ondulacion * 0.7f + desgaste);
                vertices.Add(new Vector3(x, y, z));
                uv.Add(new Vector2(0.5f + x / (perfil.RadioAla * 2f), 0.5f + z / (perfil.RadioAla * 2f)));
            }
        }
        for (int fila = 0; fila < filasAla; fila++)
        {
            int aFila = baseAlaInferior + fila * (segmentos + 1);
            int bFila = aFila + segmentos + 1;
            for (int i = 0; i < segmentos; i++)
            {
                int a = aFila + i;
                int b = bFila + i;
                triangulos.Add(a); triangulos.Add(b); triangulos.Add(a + 1);
                triangulos.Add(a + 1); triangulos.Add(b); triangulos.Add(b + 1);
            }
        }

        int anilloSuperior = filasCopa * (segmentos + 1);
        int tapa = vertices.Count;
        vertices.Add(new Vector3(0f, baseY + perfil.AltoCopa - perfil.Hundimiento, 0f));
        uv.Add(new Vector2(0.5f, 1f));
        for (int i = 0; i < segmentos; i++)
        {
            triangulos.Add(tapa); triangulos.Add(anilloSuperior + i + 1); triangulos.Add(anilloSuperior + i);
        }
        for (int i = 0; i < segmentos; i++)
        {
            int siguiente = (i + 1) % segmentos;
            int superiorActual = anilloSuperior + i;
            int superiorSiguiente = anilloSuperior + siguiente;
            int inferiorActual = baseAlaInferior + filasAla * (segmentos + 1) + i;
            int inferiorSiguiente = baseAlaInferior + filasAla * (segmentos + 1) + siguiente;
            triangulos.Add(superiorActual); triangulos.Add(superiorSiguiente); triangulos.Add(inferiorActual);
            triangulos.Add(superiorSiguiente); triangulos.Add(inferiorSiguiente); triangulos.Add(inferiorActual);
        }

        Mesh mesh = new Mesh { name = perfil.Nombre + "_MallaCuerpo" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CrearMallaInterior(PerfilSombrero perfil, int semilla)
    {
        const int segmentos = 40;
        const int filas = 3;
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> triangulos = new List<int>();
        float baseY = -perfil.AltoCopa * 0.78f + 0.004f;
        float radioExterior = perfil.RadioCopa * 0.83f;
        float radioInterior = perfil.RadioCopa * 0.63f;
        for (int fila = 0; fila <= filas; fila++)
        {
            float r = Mathf.Lerp(radioExterior, radioInterior, fila / (float)filas);
            for (int i = 0; i <= segmentos; i++)
            {
                float a = i * Mathf.PI * 2f / segmentos;
                float irregularidad = perfil.Deformacion * 0.18f * Mathf.Sin(a * 4f + semilla);
                vertices.Add(new Vector3(Mathf.Cos(a) * r * (1f + irregularidad), baseY, Mathf.Sin(a) * r * (1f + irregularidad)));
                uv.Add(new Vector2(i / (float)segmentos, fila / (float)filas));
            }
        }
        int inicioInterior = vertices.Count;
        const int filasCopa = 12;
        for (int fila = 0; fila <= filasCopa; fila++)
        {
            float t = fila / (float)filasCopa;
            float y = baseY + t * perfil.AltoCopa;
            float radio = perfil.RadioCopa * (0.96f + 0.13f * Mathf.Sin(t * Mathf.PI * 0.78f) - 0.09f * t) - 0.008f;
            for (int i = 0; i <= segmentos; i++)
            {
                float a = i * Mathf.PI * 2f / segmentos;
                float hundimiento = -perfil.Hundimiento * t * t * Mathf.Pow(Mathf.Max(0f, Mathf.Cos(a + 0.55f)), 4f);
                float variacion = 1f + perfil.Deformacion * 0.16f * Mathf.Sin(a * 4f + semilla);
                vertices.Add(new Vector3(Mathf.Cos(a) * radio * variacion, y + hundimiento - 0.006f, Mathf.Sin(a) * radio * variacion));
                uv.Add(new Vector2(i / (float)segmentos, t));
            }
        }
        for (int fila = 0; fila < filasCopa; fila++)
        {
            for (int i = 0; i < segmentos; i++)
            {
                int a = inicioInterior + fila * (segmentos + 1) + i;
                int b = a + segmentos + 1;
                triangulos.Add(a); triangulos.Add(a + 1); triangulos.Add(b);
                triangulos.Add(a + 1); triangulos.Add(b + 1); triangulos.Add(b);
            }
        }

        int tapaInterior = vertices.Count;
        vertices.Add(new Vector3(0f, baseY + perfil.AltoCopa - perfil.Hundimiento - 0.007f, 0f));
        uv.Add(new Vector2(0.5f, 1f));
        int ultimaFilaInterior = inicioInterior + filasCopa * (segmentos + 1);
        for (int i = 0; i < segmentos; i++)
        {
            triangulos.Add(tapaInterior); triangulos.Add(ultimaFilaInterior + i); triangulos.Add(ultimaFilaInterior + i + 1);
        }

        for (int fila = 0; fila < filas; fila++)
        {
            for (int i = 0; i < segmentos; i++)
            {
                int a = fila * (segmentos + 1) + i;
                int b = a + segmentos + 1;
                triangulos.Add(a); triangulos.Add(a + 1); triangulos.Add(b);
                triangulos.Add(a + 1); triangulos.Add(b + 1); triangulos.Add(b);
            }
        }
        Mesh mesh = new Mesh { name = perfil.Nombre + "_BandaInteriorMesh" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CrearMallaCinta(PerfilSombrero perfil, int semilla)
    {
        const int segmentos = 48;
        List<Vector3> vertices = new List<Vector3>(segmentos * 4);
        List<Vector2> uv = new List<Vector2>(segmentos * 4);
        List<int> triangulos = new List<int>(segmentos * 12);
        float baseY = -perfil.AltoCopa * 0.78f + 0.025f;
        float alto = Mathf.Clamp(perfil.AltoCopa * 0.13f, 0.018f, 0.025f);
        for (int i = 0; i < segmentos; i++)
        {
            float a = i * Mathf.PI * 2f / segmentos;
            float r = perfil.RadioCopa * (0.97f + 0.1f * Mathf.Sin(a * 3f + semilla));
            float irregularidad = perfil.Deformacion * 0.2f * Mathf.Sin(a * 5f + semilla);
            Vector3 abajo = new Vector3(Mathf.Cos(a) * r, baseY + irregularidad, Mathf.Sin(a) * r);
            Vector3 arriba = new Vector3(Mathf.Cos(a) * r, baseY + alto + irregularidad, Mathf.Sin(a) * r);
            vertices.Add(abajo); vertices.Add(arriba);
            uv.Add(new Vector2(i / (float)segmentos, 0f)); uv.Add(new Vector2(i / (float)segmentos, 1f));
        }
        for (int i = 0; i < segmentos; i++)
        {
            int a = i * 2;
            int b = ((i + 1) % segmentos) * 2;
            triangulos.Add(a); triangulos.Add(b); triangulos.Add(a + 1);
            triangulos.Add(a + 1); triangulos.Add(b); triangulos.Add(b + 1);
            triangulos.Add(a); triangulos.Add(a + 1); triangulos.Add(b);
            triangulos.Add(a + 1); triangulos.Add(b + 1); triangulos.Add(b);
        }
        Mesh mesh = new Mesh { name = perfil.Nombre + "_CintaMesh" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CrearMallaCostura(PerfilSombrero perfil, int semilla)
    {
        List<Vector3> puntos = new List<Vector3>();
        float y = -perfil.AltoCopa * 0.78f + 0.029f;
        for (int i = 0; i <= 96; i++)
        {
            float a = i * Mathf.PI * 2f / 96f;
            float r = perfil.RadioCopa * (0.975f + 0.1f * Mathf.Sin(a * 3f + semilla));
            puntos.Add(new Vector3(Mathf.Cos(a) * r, y + 0.0015f * Mathf.Sin(i * 0.37f), Mathf.Sin(a) * r));
        }
        return CrearMallaCordel(perfil.Nombre + "_CosturaMesh", puntos, 0.0018f, 5);
    }

    private static Mesh CrearMallaLazo(PerfilSombrero perfil, int semilla)
    {
        float y = -perfil.AltoCopa * 0.78f + perfil.AltoCopa * 0.93f;
        float z = -perfil.RadioCopa * 0.74f;
        List<Vector3> puntos = new List<Vector3>();
        for (int i = 0; i <= 22; i++)
        {
            float t = i / 22f;
            float a = Mathf.Lerp(Mathf.PI, 0f, t);
            float amplitud = 0.018f + 0.002f * Mathf.Sin(semilla);
            puntos.Add(new Vector3(Mathf.Cos(a) * amplitud, y + Mathf.Sin(a) * 0.03f, z - 0.025f * Mathf.Sin(a)));
        }
        return CrearMallaCordel(perfil.Nombre + "_LazoMesh", puntos, 0.0038f, 6);
    }

    private static Mesh CrearMallaCordel(string nombre, IList<Vector3> puntos, float radio, int lados)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> triangulos = new List<int>();
        for (int i = 0; i < puntos.Count; i++)
        {
            Vector3 tangente = (puntos[Mathf.Min(i + 1, puntos.Count - 1)] - puntos[Mathf.Max(i - 1, 0)]).normalized;
            Vector3 ejeA = Vector3.Cross(tangente, Vector3.forward).normalized;
            Vector3 ejeB = Vector3.Cross(tangente, ejeA).normalized;
            for (int j = 0; j < lados; j++)
            {
                float a = j * Mathf.PI * 2f / lados;
                vertices.Add(puntos[i] + (ejeA * Mathf.Cos(a) + ejeB * Mathf.Sin(a)) * radio);
                uv.Add(new Vector2(j / (float)lados, i / (float)(puntos.Count - 1)));
            }
        }
        for (int i = 0; i < puntos.Count - 1; i++)
        {
            for (int j = 0; j < lados; j++)
            {
                int a = i * lados + j;
                int b = i * lados + (j + 1) % lados;
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
                float hilo = Mathf.PerlinNoise((x + semilla) * 0.11f, (y + semilla) * 0.035f);
                float trama = Mathf.PerlinNoise((x + semilla * 2) * 0.16f, (y + semilla * 3) * 0.12f);
                if (tipo == 1)
                {
                    Vector3 n = new Vector3((hilo - 0.5f) * 0.34f, (trama - 0.5f) * 0.34f, 1f).normalized;
                    pixeles[y * resolucion + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
                else if (tipo == 2)
                {
                    float rough = Mathf.Clamp01(0.72f + hilo * 0.12f + trama * 0.08f);
                    pixeles[y * resolucion + x] = new Color(0.7f, 0f, 0f, rough);
                }
                else
                {
                    float fibra = Mathf.Clamp(0.91f + (hilo - 0.5f) * 0.12f + (trama - 0.5f) * 0.09f, 0.72f, 1f);
                    float paja = tipo == 3 ? (x % 13 < 2 ? 0.82f : 1f) : 1f;
                    pixeles[y * resolucion + x] = new Color(fibra * paja, fibra * paja * 0.97f, fibra * paja * 0.91f, 1f);
                }
            }
        }
        textura.SetPixels(pixeles);
        textura.Apply(true, false);
        if (crear) AssetDatabase.CreateAsset(textura, ruta);
        else EditorUtility.SetDirty(textura);
        return textura;
    }

    private static Material CrearMaterial(string nombre, Color color, Texture2D albedo, Texture2D normal, Texture2D suavidad, float smoothness)
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
        if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", 0.28f);
        if (material.HasProperty("_MetallicGlossMap")) material.SetTexture("_MetallicGlossMap", suavidad);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
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

    private static void GuardarPrefab(GameObject objeto, string ruta)
    {
        PrefabUtility.SaveAsPrefabAsset(objeto, ruta);
        Object.DestroyImmediate(objeto);
    }
}