using UnityEngine;
using UnityEditor;

public class NightSceneSetup : EditorWindow
{
    [MenuItem("Trucardo/Configurar Escena Nocturna")]
    public static void SetupNightScene()
    {
        // 1. Configurar la Luz de la Luna (Directional Light)
        Light[] lights = FindObjectsOfType<Light>();
        Light moonLight = null;
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                moonLight = light;
                break;
            }
        }

        if (moonLight == null)
        {
            GameObject moonObj = new GameObject("MoonLight");
            moonLight = moonObj.AddComponent<Light>();
            moonLight.type = LightType.Directional;
        }

        moonLight.name = "MoonLight";
        // Color azulado para simular luz de luna
        moonLight.color = new Color(0.5f, 0.6f, 0.8f); 
        moonLight.intensity = 0.5f; // Más tenue que el sol
        moonLight.shadows = LightShadows.Soft;
        moonLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // 2. Configurar la Iluminación Ambiental
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientSkyColor = new Color(0.05f, 0.05f, 0.1f);
        
        // 3. Crear o Cargar Material Skybox (Cielo nocturno) como un Asset real
        string materialPath = "Assets/NightSkybox.mat";
        Material nightSkyMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (nightSkyMaterial == null)
        {
            nightSkyMaterial = new Material(Shader.Find("Skybox/Procedural"));
            AssetDatabase.CreateAsset(nightSkyMaterial, materialPath);
        }
        
        nightSkyMaterial.name = "NightSkybox";
        nightSkyMaterial.SetFloat("_SunSize", 0.04f); // Tamaño pequeño para simular la luna en el shader
        nightSkyMaterial.SetFloat("_SunSizeConvergence", 5f);
        nightSkyMaterial.SetFloat("_AtmosphereThickness", 0.2f);
        nightSkyMaterial.SetColor("_SkyTint", new Color(0.05f, 0.05f, 0.1f));
        nightSkyMaterial.SetColor("_GroundColor", new Color(0.02f, 0.02f, 0.02f));
        nightSkyMaterial.SetFloat("_Exposure", 0.3f);
        
        EditorUtility.SetDirty(nightSkyMaterial);
        AssetDatabase.SaveAssets();
        
        RenderSettings.skybox = nightSkyMaterial;
        RenderSettings.sun = moonLight;

        // 4. Crear Estrellas (Particle System)
        GameObject starsObj = GameObject.Find("CieloEstrellado");
        if (starsObj != null) 
        {
            DestroyImmediate(starsObj);
        }
        
        starsObj = new GameObject("CieloEstrellado");
        ParticleSystem ps = starsObj.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = Mathf.Infinity;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startColor = new Color(1f, 1f, 1f, 0.8f);
        main.maxParticles = 3000;
        main.playOnAwake = true;
        main.prewarm = true;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 3000) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 300f; // Distancia de las estrellas
        shape.radiusThickness = 0f; // Emitir solo en la superficie de la esfera

        // Asignar el material integrado por defecto para las partículas
        ParticleSystemRenderer psr = starsObj.GetComponent<ParticleSystemRenderer>();
        psr.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
        
        // Evitar que el sistema de partículas se simule en tiempo de editor para no sobrecargar
        ps.Simulate(0f, true, true);
        ps.Play();

        Debug.Log("Escena nocturna configurada con éxito. Se ha ajustado la luna, la luz ambiental y se añadieron estrellas.");
    }
}
