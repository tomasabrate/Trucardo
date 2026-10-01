using UnityEngine;
using UnityEditor;

public class FireCreator
{
    [MenuItem("Tools/Agregar Fuego Basico a la Escena")]
    public static void CreateFireInScene()
    {
        // Crear contenedor
        GameObject fireObj = new GameObject("FuegoBasico (Chimenea)");

        // Fuego principal
        ParticleSystem ps = fireObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = fireObj.GetComponent<ParticleSystemRenderer>();
        
        Material particleMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
        if (particleMat == null) {
            particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        }
        psr.material = particleMat;

        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 2.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startColor = new Color(1f, 0.4f, 0f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.gravityModifier = -0.1f;

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.2f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.yellow, 0.0f), new GradientColorKey(new Color(1f, 0.3f, 0f), 0.5f), new GradientColorKey(Color.red, 0.8f), new GradientColorKey(Color.black, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 0.6f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.5f);
        curve.AddKey(0.2f, 1.0f);
        curve.AddKey(1.0f, 0.0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        // Centro mas claro
        GameObject coreObj = new GameObject("CentroDelFuego");
        coreObj.transform.parent = fireObj.transform;
        coreObj.transform.localPosition = Vector3.zero;
        ParticleSystem cps = coreObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer cpsr = coreObj.GetComponent<ParticleSystemRenderer>();
        cpsr.material = particleMat;

        var cmain = cps.main;
        cmain.duration = 1.0f;
        cmain.loop = true;
        cmain.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
        cmain.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 1.5f);
        cmain.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        cmain.startColor = new Color(1f, 0.8f, 0.2f, 1f);
        cmain.simulationSpace = ParticleSystemSimulationSpace.Local;
        
        var cemission = cps.emission;
        cemission.rateOverTime = 15f;

        var cshape = cps.shape;
        cshape.shapeType = ParticleSystemShapeType.Cone;
        cshape.angle = 8f;
        cshape.radius = 0.1f;

        var ccolorOverLifetime = cps.colorOverLifetime;
        ccolorOverLifetime.enabled = true;
        Gradient cgrad = new Gradient();
        cgrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.yellow, 0.5f), new GradientColorKey(new Color(1f, 0.5f, 0f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 0.5f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        ccolorOverLifetime.color = cgrad;

        var csizeOverLifetime = cps.sizeOverLifetime;
        csizeOverLifetime.enabled = true;
        csizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        // Chispas
        GameObject sparksObj = new GameObject("Chispas");
        sparksObj.transform.parent = fireObj.transform;
        sparksObj.transform.localPosition = Vector3.zero;
        ParticleSystem sps = sparksObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer spsr = sparksObj.GetComponent<ParticleSystemRenderer>();
        spsr.material = particleMat;

        var smain = sps.main;
        smain.duration = 1.0f;
        smain.loop = true;
        smain.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 2.0f);
        smain.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.0f);
        smain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        smain.startColor = new Color(1f, 0.7f, 0.2f, 1f); 
        smain.simulationSpace = ParticleSystemSimulationSpace.Local;
        
        var semission = sps.emission;
        semission.rateOverTime = 8f;

        var sshape = sps.shape;
        sshape.shapeType = ParticleSystemShapeType.Cone;
        sshape.angle = 20f;
        sshape.radius = 0.3f;

        var scolorOverLifetime = sps.colorOverLifetime;
        scolorOverLifetime.enabled = true;
        Gradient sgrad = new Gradient();
        sgrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.yellow, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        scolorOverLifetime.color = sgrad;

        var snoise = sps.noise;
        snoise.enabled = true;
        snoise.strength = 0.5f;
        snoise.frequency = 1f;

        Undo.RegisterCreatedObjectUndo(fireObj, "Crear Fuego Basico");
        Selection.activeGameObject = fireObj;

        if (SceneView.lastActiveSceneView != null)
        {
            Camera sceneCam = SceneView.lastActiveSceneView.camera;
            if (sceneCam != null)
            {
                fireObj.transform.position = sceneCam.transform.position + sceneCam.transform.forward * 3f;
            }
        }
    }

    [MenuItem("Tools/Agregar Fuego Low Poly a la Escena")]
    public static void CreateLowPolyFireInScene()
    {
        // Crear contenedor
        GameObject fireObj = new GameObject("FuegoLowPoly (Chimenea)");

        // Fuego principal
        ParticleSystem ps = fireObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = fireObj.GetComponent<ParticleSystemRenderer>();
        
        // Intentamos usar el shader de partículas Unlit de URP para que no se vea rosa
        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Material unlitMat;
        if (urpUnlit != null)
        {
            unlitMat = new Material(urpUnlit);
        }
        else
        {
            // Fallback si no encuentra el shader URP, usar el que funciono en el fuego basico
            unlitMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
        }
        psr.material = unlitMat;

        // Modo Malla (Cubo)
        psr.renderMode = ParticleSystemRenderMode.Mesh;
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        psr.mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        GameObject.DestroyImmediate(tempCube);

        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        // Rotacion inicial 3D
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new Color(1f, 0.5f, 0f, 1f); 
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.25f;

        // Como usamos cubo opaco, el Alpha no importara mucho a menos que el shader sea transparente,
        // asi que nos enfocamos en cambiar el color base de amarillo a rojo.
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.yellow, 0.0f), new GradientColorKey(new Color(1f, 0.3f, 0f), 0.5f), new GradientColorKey(Color.red, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
        );
        colorOverLifetime.color = grad;

        // Escalar de grande a 0
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 1.0f);
        curve.AddKey(1.0f, 0.0f); // Se encoge a 0 al morir, da efecto toon/lowpoly
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        // Rotar durante la vida
        var rotOverLifetime = ps.rotationOverLifetime;
        rotOverLifetime.enabled = true;
        rotOverLifetime.separateAxes = true;
        rotOverLifetime.x = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        rotOverLifetime.y = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);

        // Chispas
        GameObject sparksObj = new GameObject("ChispasLowPoly");
        sparksObj.transform.parent = fireObj.transform;
        sparksObj.transform.localPosition = Vector3.zero;
        ParticleSystem sps = sparksObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer spsr = sparksObj.GetComponent<ParticleSystemRenderer>();
        spsr.material = unlitMat;
        spsr.renderMode = ParticleSystemRenderMode.Mesh;
        spsr.mesh = psr.mesh; // el mismo cubo

        var smain = sps.main;
        smain.duration = 1.0f;
        smain.loop = true;
        smain.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
        smain.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4.0f);
        smain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        smain.startRotation3D = true;
        smain.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        smain.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        smain.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        smain.startColor = new Color(1f, 0.8f, 0.2f, 1f); 
        smain.simulationSpace = ParticleSystemSimulationSpace.Local;
        
        var semission = sps.emission;
        semission.rateOverTime = 10f;

        var sshape = sps.shape;
        sshape.shapeType = ParticleSystemShapeType.Cone;
        sshape.angle = 20f;
        sshape.radius = 0.3f;

        var scolorOverLifetime = sps.colorOverLifetime;
        scolorOverLifetime.enabled = true;
        Gradient sgrad = new Gradient();
        sgrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.yellow, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
        );
        scolorOverLifetime.color = sgrad;

        var ssizeOverLifetime = sps.sizeOverLifetime;
        ssizeOverLifetime.enabled = true;
        ssizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        var srotOverLifetime = sps.rotationOverLifetime;
        srotOverLifetime.enabled = true;
        srotOverLifetime.separateAxes = true;
        srotOverLifetime.x = new ParticleSystem.MinMaxCurve(-Mathf.PI * 2f, Mathf.PI * 2f);
        srotOverLifetime.y = new ParticleSystem.MinMaxCurve(-Mathf.PI * 2f, Mathf.PI * 2f);
        srotOverLifetime.z = new ParticleSystem.MinMaxCurve(-Mathf.PI * 2f, Mathf.PI * 2f);

        Undo.RegisterCreatedObjectUndo(fireObj, "Crear Fuego Low Poly");
        Selection.activeGameObject = fireObj;

        if (SceneView.lastActiveSceneView != null)
        {
            Camera sceneCam = SceneView.lastActiveSceneView.camera;
            if (sceneCam != null)
            {
                // Lo ponemos un poco al lado del original (derecha) para poder comparar
                fireObj.transform.position = sceneCam.transform.position + sceneCam.transform.forward * 3f + sceneCam.transform.right * 1.5f;
            }
        }
    }
}
