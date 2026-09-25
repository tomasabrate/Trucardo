using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SetupGauchoAnimator : EditorWindow
{
    [MenuItem("Tools/Setup Gaucho Animator")]
    public static void CreateController()
    {
        string path = "Assets/Core/Prefabs/GauchoAnimator.controller";
        
        // Crear un Animator Controller vacío
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        // Añadir parámetros
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsSeated", AnimatorControllerParameterType.Bool);

        // Obtener la capa base
        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // Buscar las animaciones en la carpeta
        AnimationClip idleClip = GetClip("Assets/Art/Personajes/Animaciones/Core/Breathing Idle.fbx");
        if (idleClip == null) idleClip = GetClip("Assets/Art/Personajes/Animaciones/Core/Standing Idle.fbx");
        if (idleClip == null) idleClip = GetClip("Assets/Art/Personajes/Animaciones/Core/Idle.fbx");

        AnimationClip walkClip = GetClip("Assets/Art/Personajes/Animaciones/Core/Walking.fbx");
        if (walkClip == null) walkClip = GetClip("Assets/Art/Personajes/Animaciones/Core/Standard Walk.fbx");

        AnimationClip seatedClip = GetClip("Assets/Art/Personajes/Animaciones/Core/Sitting Idle.fbx");
        if (seatedClip == null) seatedClip = GetClip("Assets/Art/Personajes/Animaciones/Core/Seated Idle.fbx");

        AnimationClip standToSitClip = GetClip("Assets/Art/Personajes/Animaciones/Core/Stand To Sit.fbx");
        AnimationClip sitToStandClip = GetClip("Assets/Art/Personajes/Animaciones/Core/Sit To Stand.fbx");

        if (idleClip == null || walkClip == null || seatedClip == null)
        {
            Debug.LogError("Faltan animaciones base (Idle, Walk, o Seated Idle).");
            return;
        }

        // Crear Estados
        AnimatorState stateIdle = rootStateMachine.AddState("Idle");
        stateIdle.motion = idleClip;

        AnimatorState stateWalk = rootStateMachine.AddState("Walk");
        stateWalk.motion = walkClip;

        AnimatorState stateSeated = rootStateMachine.AddState("Seated");
        stateSeated.motion = seatedClip;

        AnimatorState stateStandToSit = null;
        AnimatorState stateSitToStand = null;

        if (standToSitClip != null) 
        {
            stateStandToSit = rootStateMachine.AddState("StandToSit");
            stateStandToSit.motion = standToSitClip;
        }

        if (sitToStandClip != null)
        {
            stateSitToStand = rootStateMachine.AddState("SitToStand");
            stateSitToStand.motion = sitToStandClip;
        }

        // Estado por defecto
        rootStateMachine.defaultState = stateIdle;

        // Transiciones AnyState -> Seated (o StandToSit)
        if (stateStandToSit != null)
        {
            // Idle -> StandToSit
            AnimatorStateTransition idleToSit = stateIdle.AddTransition(stateStandToSit);
            idleToSit.AddCondition(AnimatorConditionMode.If, 0, "IsSeated");
            idleToSit.duration = 0.25f; // Transición fluida de 0.25 seg
            
            // StandToSit -> Seated (automático al terminar la animacion)
            AnimatorStateTransition toSeated = stateStandToSit.AddTransition(stateSeated);
            toSeated.hasExitTime = true;
            toSeated.exitTime = 0.85f; // Pasa a estar sentado casi al final de la anim
            toSeated.duration = 0.25f;
        }
        else
        {
            AnimatorStateTransition toSeated = rootStateMachine.AddAnyStateTransition(stateSeated);
            toSeated.AddCondition(AnimatorConditionMode.If, 0, "IsSeated");
            toSeated.duration = 0.25f;
        }

        // Seated -> SitToStand (o Idle)
        if (stateSitToStand != null)
        {
            AnimatorStateTransition seatedToStand = stateSeated.AddTransition(stateSitToStand);
            seatedToStand.AddCondition(AnimatorConditionMode.IfNot, 0, "IsSeated");
            seatedToStand.duration = 0.25f;

            AnimatorStateTransition standToIdle = stateSitToStand.AddTransition(stateIdle);
            standToIdle.hasExitTime = true;
            standToIdle.exitTime = 0.85f;
            standToIdle.duration = 0.25f;
        }
        else
        {
            AnimatorStateTransition seatedToIdle = stateSeated.AddTransition(stateIdle);
            seatedToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsSeated");
            seatedToIdle.duration = 0.25f;
        }

        // Idle -> Walk
        AnimatorStateTransition idleToWalk = stateIdle.AddTransition(stateWalk);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToWalk.duration = 0.15f;

        // Walk -> Idle
        AnimatorStateTransition walkToIdle = stateWalk.AddTransition(stateIdle);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        walkToIdle.duration = 0.15f;

        // Asignar al prefab
        string prefabPath = "Assets/Core/Prefabs/PlayerPrefab.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Animator anim = instance.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.runtimeAnimatorController = controller;
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            }
            DestroyImmediate(instance);
            Debug.Log("Animator Controller creado y asignado con éxito!");
        }
    }

    private static AnimationClip GetClip(string path)
    {
        Object[] objs = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var o in objs)
        {
            if (o is AnimationClip && !o.name.StartsWith("__preview__"))
            {
                return (AnimationClip)o;
            }
        }
        return null;
    }
}
