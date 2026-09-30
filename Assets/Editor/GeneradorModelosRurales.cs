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