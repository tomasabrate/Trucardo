using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using Trucardo.Core;

public class SetupGauchoPrefab : EditorWindow
{
    [MenuItem("Tools/Setup Gaucho Prefab")]
    public static void Setup()
    {
        string prefabPath = "Assets/Core/Prefabs/PlayerPrefab.prefab";
        string fbxPath = "Assets/Art/Personajes/Gaucho/guacho_base.fbx";

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

        if (prefab == null || fbx == null)
        {
            Debug.LogError("No se encontró el prefab o el fbx.");
            return;
        }

        // Instanciar el prefab para modificarlo
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        // Eliminar los placeholders viejos
        Transform cabeza = instance.transform.Find("Cabeza");
        Transform cuerpo = instance.transform.Find("Cuerpo");

        if (cabeza != null)
        {
            // En vez de borrar Cabeza, le sacamos los componentes visuales para que quede como pivote de la camara
            MeshRenderer mr = cabeza.GetComponent<MeshRenderer>();
            MeshFilter mf = cabeza.GetComponent<MeshFilter>();
            if (mr != null) DestroyImmediate(mr, true);
            if (mf != null) DestroyImmediate(mf, true);
        }

        // Eliminar 'Visuals' anterior si existe
        Transform oldVisuals = instance.transform.Find("Visuals");
        if (oldVisuals != null)
        {
            DestroyImmediate(oldVisuals.gameObject);
        }

        if (cuerpo != null)
        {
            DestroyImmediate(cuerpo.gameObject);
        }

        // Instanciar el FBX como un objeto visual
        GameObject visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        visualInstance.name = "Visuals";
        visualInstance.transform.SetParent(instance.transform);
        visualInstance.transform.localPosition = Vector3.zero;
        visualInstance.transform.localRotation = Quaternion.identity;
        // Agregar Animator si no tiene
        Animator anim = visualInstance.GetComponent<Animator>();
        if (anim == null)
        {
            anim = visualInstance.AddComponent<Animator>();
        }
        anim.applyRootMotion = false; // CRITICO: Evita que la animacion hunda al personaje en el piso!
        
        // Intentar reconectar el cerebro de animaciones si ya habia sido creado antes
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Core/Prefabs/GauchoAnimator.controller");
        if (controller != null)
        {
            anim.runtimeAnimatorController = controller;
        }

        // Reposicionar el pivote de la cámara a una altura estandar de ojos
        if (cabeza != null)
        {
            cabeza.localPosition = new Vector3(0, 1.6f, 0); 
            Transform cam = cabeza.Find("FirstPersonCamera");
            if (cam != null)
            {
                cam.localPosition = new Vector3(0, 0, 0.05f);
                Camera c = cam.GetComponent<Camera>();
                if (c != null) c.nearClipPlane = 0.01f;
            }
        }

        // Ajustar el CharacterController con valores humanos estandar fijos
        // Esto evita que si el hueso del modelo está mal configurado, el personaje se entierre en el piso.
        CharacterController cc = instance.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.height = 1.8f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.radius = 0.4f; 
        }

        // Aplicar cambios al prefab
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        DestroyImmediate(instance);

        Debug.Log("Prefab actualizado con éxito!");
    }
}
