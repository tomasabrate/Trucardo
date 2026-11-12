using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Trucardo.Core;

namespace Trucardo.EditorScripts
{
    [InitializeOnLoad]
    public static class SetupMusicianGaucho
    {
        static SetupMusicianGaucho()
        {
            EditorApplication.update += RunOnceWhenReady;
        }

        private static void RunOnceWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.update -= RunOnceWhenReady;

            FixFloorNormalMapsAndLanternLights();

            Scene scene = SceneManager.GetActiveScene();
            if (scene.isLoaded && scene.path == "Assets/Scenes/SampleScene.unity")
            {
                GameObject existing = GameObject.Find("Gaucho_Musico_Bar");
                Transform existingVisuals = existing != null ? existing.transform.Find("Visuals") : null;
                Transform existingGuitar = existing != null ? FindChildRecursive(existing.transform, "Guitarra") : null;
                bool needsSetup = existing == null
                    || existing.GetComponent<GauchoGuitarPlayer>() == null
                    || existingVisuals == null
                    || existingVisuals.localPosition.y < 0.25f
                    || existingGuitar == null
                    || Mathf.Abs(existingGuitar.localPosition.x - 0.075f) > 0.005f
                    || GameObject.Find("Gaucho_Musico_Bar_ManoFija") == null
                    || AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Core/Prefabs/GauchoMusicoManoFijaPrefab.prefab") == null;

                if (needsSetup)
                {
                    Setup();
                }
                else
                {
                    RefreshExistingMusicianPose(existing, true, "Assets/Core/Prefabs/GauchoMusicoPrefab.prefab");
                    GameObject existingFixed = GameObject.Find("Gaucho_Musico_Bar_ManoFija");
                    if (existingFixed != null)
                    {
                        RefreshExistingMusicianPose(existingFixed, false, "Assets/Core/Prefabs/GauchoMusicoManoFijaPrefab.prefab");
                    }
                }
            }
        }

        private static void RefreshExistingMusicianPose(GameObject rootObj, bool animateHands, string prefabPath)
        {
            if (rootObj == null) return;
            Transform visuals = rootObj.transform.Find("Visuals");
            Transform guitar = FindChildRecursive(rootObj.transform, "Guitarra");
            if (visuals == null || guitar == null) return;

            AnimationClip sittingClip = GetClip("Assets/Models/Personajes/Animaciones/Core/Sitting Idle.fbx");
            if (sittingClip != null)
            {
                sittingClip.SampleAnimation(visuals.gameObject, 0f);
            }

            GauchoGuitarPlayer guitarPlayer = rootObj.GetComponent<GauchoGuitarPlayer>();
            if (guitarPlayer != null)
            {
                guitarPlayer.animateHands = animateHands;
                guitarPlayer.CacheBones();
                guitarPlayer.ApplyGuitarAndArmPose(0f);
            }
            EditorUtility.SetDirty(rootObj);

            if (!string.IsNullOrEmpty(prefabPath))
            {
                bool wasActive = rootObj.activeSelf;
                if (!wasActive) rootObj.SetActive(true);
                PrefabUtility.SaveAsPrefabAssetAndConnect(rootObj, prefabPath, InteractionMode.AutomatedAction);
                if (!wasActive) rootObj.SetActive(false);
            }
        }

        private static void FixFloorNormalMapsAndLanternLights()
        {
            string[] normalMapPaths = new string[]
            {
                "Assets/Resources/Texturas/Maderas/Wood Shutter/wood_shutter_nor_gl_4k.jpg",
                "Assets/Resources/Texturas/Maderas/Dark Wooden Planks/dark_wooden_planks_nor_gl_4k.jpg",
                "Assets/Resources/Texturas/Maderas/Weathered Plank Siding/weathered_plank_siding_nor_gl_4k.jpg",
                "Assets/Resources/Texturas/Maderas/Weathered Planks/weathered_planks_nor_gl_4k.jpg",
                "Assets/Resources/Texturas/Maderas/Wood Table Large/wood_table_large_nor_gl_4k.jpg",
                "Assets/Resources/Texturas/Maderas/Wooden Barrel/wooden_barrel_nor_gl_4k.jpg",
                "Assets/Resources/Texturas/Piedra/Old Stone Wall/old_stone_wall_nor_gl_4k.jpg",
                "Assets/Resources/Texturas/Terreno/Brown Mud Leaves/brown_mud_leaves_01_nor_gl_4k.jpg"
            };

            foreach (string path in normalMapPaths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && (importer.textureType != TextureImporterType.NormalMap || importer.sRGBTexture))
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.sRGBTexture = false;
                    importer.SaveAndReimport();
                }
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            bool sceneModified = false;
            foreach (GameObject rootObj in scene.GetRootGameObjects())
            {
                if (rootObj == null) continue;
                foreach (Light light in rootObj.GetComponentsInChildren<Light>(true))
                {
                    if (light != null && light.gameObject.name == "Resplandor_del_Aceite")
                    {
                        if (light.type != LightType.Point
                            || Mathf.Abs(light.transform.localPosition.x) > 0.0001f
                            || Mathf.Abs(light.transform.localPosition.z) > 0.0001f)
                        {
                            light.type = LightType.Point;
                            light.transform.localPosition = new Vector3(0f, light.transform.localPosition.y, 0f);
                            light.transform.localRotation = Quaternion.identity;
                            EditorUtility.SetDirty(light);
                            EditorUtility.SetDirty(light.transform);
                            sceneModified = true;
                        }
                    }
                }
            }

            if (sceneModified)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        [MenuItem("Trucardo/Configurar Gaucho Musico")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            string scenePath = "Assets/Scenes/SampleScene.unity";
            if (SceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            FixFloorNormalMapsAndLanternLights();

            // 1. Crear / actualizar texturas y materiales del Gaucho Músico
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            string texPonchoMusicoPath = "Assets/Models/Personajes/Gaucho/Texturas/guacho_poncho_texture_musico.png";
            string texBodyMusicoPath = "Assets/Models/Personajes/Gaucho/Texturas/guacho_texture_musico.png";
            string matPonchoMusicoPath = "Assets/Models/Personajes/Gaucho/Texturas/Mat_Gaucho_Musico_Poncho.mat";
            string matBodyMusicoPath = "Assets/Models/Personajes/Gaucho/Texturas/Mat_Gaucho_Musico_Body.mat";

            Material matPonchoMusico = CreateOrUpdateMaterial(matPonchoMusicoPath, texPonchoMusicoPath, urpLit);
            Material matBodyMusico = CreateOrUpdateMaterial(matBodyMusicoPath, texBodyMusicoPath, urpLit);

            // Material de la guitarra
            Material matGuitarra = AssetDatabase.LoadAssetAtPath<Material>("Assets/Poderes/Materiales/Mat_Poder_Guitarra.mat");

            // 2. Crear / actualizar AnimatorController exclusivo para el Músico (default state SittingIdle)
            string animCtrlPath = "Assets/Core/Prefabs/GauchoMusicoAnimator.controller";
            RuntimeAnimatorController musicoController = SetupAnimatorController(animCtrlPath);

            // 3. Limpiar instancias previas en escena
            DestroyExistingByName("Gaucho_Musico_Bar");
            DestroyExistingByName("Gaucho_Musico_Bar_ManoFija");

            // Posición en la esquina donde está la música (MusicaBar x: -2.103, y: 0.849, z: 4.538)
            Vector3 cornerPos = new Vector3(-2.35f, 0.02f, 4.35f);
            Quaternion cornerRot = Quaternion.Euler(0f, 160f, 0f);

            // 4. Construir versión con manos fijas (respaldo) y guardar Prefab
            GameObject rootManoFija = BuildMusicianHierarchy(
                "Gaucho_Musico_Bar_ManoFija",
                cornerPos,
                cornerRot,
                matBodyMusico,
                matPonchoMusico,
                matGuitarra,
                musicoController,
                animateHands: false);

            string manoFijaPrefabPath = "Assets/Core/Prefabs/GauchoMusicoManoFijaPrefab.prefab";
            if (rootManoFija != null)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(rootManoFija, manoFijaPrefabPath, InteractionMode.AutomatedAction);
                // Dejar desactivado en la escena como respaldo listo para activar si se prefiere la mano fija
                rootManoFija.SetActive(false);
                EditorUtility.SetDirty(rootManoFija);
            }

            // 5. Construir versión principal con manos animadas tocando la guitarra y guardar Prefab
            GameObject rootAnimado = BuildMusicianHierarchy(
                "Gaucho_Musico_Bar",
                cornerPos,
                cornerRot,
                matBodyMusico,
                matPonchoMusico,
                matGuitarra,
                musicoController,
                animateHands: true);

            string musicoPrefabPath = "Assets/Core/Prefabs/GauchoMusicoPrefab.prefab";
            GameObject savedPrefab = null;
            if (rootAnimado != null)
            {
                savedPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(rootAnimado, musicoPrefabPath, InteractionMode.AutomatedAction);
                EditorUtility.SetDirty(rootAnimado);
            }

            // 6. Configurar EnsureMusicianGaucho en MusicaBar
            GameObject musicaBar = GameObject.Find("MusicaBar");
            if (musicaBar != null)
            {
                musicaBar.transform.position = cornerPos + new Vector3(0f, 0.85f, 0f);
                EnsureMusicianGaucho ensure = musicaBar.GetComponent<EnsureMusicianGaucho>();
                if (ensure == null)
                {
                    ensure = Undo.AddComponent<EnsureMusicianGaucho>(musicaBar);
                }

                SerializedObject so = new SerializedObject(ensure);
                SerializedProperty propPrefab = so.FindProperty("musicoPrefab");
                if (propPrefab != null && savedPrefab != null)
                {
                    propPrefab.objectReferenceValue = savedPrefab;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorUtility.SetDirty(musicaBar);
            }

            // 7. Guardar escena
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            Debug.Log("<color=#4CAF50><b>[SetupMusicianGaucho] Gaucho Músico configurado con banquito bajo de 4 patas, guitarra bien posicionada y dos variantes (manos animadas + respaldo de mano fija).</b></color>");
        }

        private static void DestroyExistingByName(string objName)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            foreach (GameObject rootObj in scene.GetRootGameObjects())
            {
                if (rootObj != null && rootObj.name == objName)
                {
                    Undo.DestroyObjectImmediate(rootObj);
                }
            }
        }

        private static GameObject BuildMusicianHierarchy(
            string objectName,
            Vector3 cornerPos,
            Quaternion cornerRot,
            Material matBodyMusico,
            Material matPonchoMusico,
            Material matGuitarra,
            RuntimeAnimatorController musicoController,
            bool animateHands)
        {
            GameObject root = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(root, "Crear " + objectName);
            root.transform.position = cornerPos;
            root.transform.rotation = cornerRot;

            // A. Instanciar el banquito bajo de 4 patas de las mesas (Silla.prefab -> BanquitoLowPoly)
            string sillaPrefabPath = "Assets/Models/BarRecursos/Silla.prefab";
            GameObject sillaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sillaPrefabPath);
            if (sillaPrefab != null)
            {
                GameObject bancoInstance = (GameObject)PrefabUtility.InstantiatePrefab(sillaPrefab, root.transform);
                bancoInstance.name = "Banco";
                bancoInstance.transform.localPosition = Vector3.zero;
                bancoInstance.transform.localRotation = Quaternion.identity;
                bancoInstance.transform.localScale = Vector3.one;

                // Silla.prefab tiene un offset z=-1.1 en su raíz; al poner localPosition = zero queda perfectamente centrado
                for (int i = 0; i < bancoInstance.transform.childCount; i++)
                {
                    Transform child = bancoInstance.transform.GetChild(i);
                    child.localPosition = Vector3.zero;
                    child.localRotation = Quaternion.identity;
                }

                // Desactivar SeatInteractable y Collider para que ni jugadores ni bots intenten usar el banquito del músico
                SeatInteractable seat = bancoInstance.GetComponent<SeatInteractable>();
                if (seat != null)
                {
                    seat.enabled = false;
                }
                foreach (Collider col in bancoInstance.GetComponentsInChildren<Collider>(true))
                {
                    col.enabled = false;
                }
            }

            // B. Instanciar Gaucho con Poncho y Boina/Sombrero
            string fbxGauchoPath = "Assets/Models/Personajes/Gaucho/guacho_boina_poncho.fbx";
            GameObject gauchoFbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxGauchoPath);
            if (gauchoFbx == null)
            {
                Debug.LogError("No se encontró el FBX del gaucho en: " + fbxGauchoPath);
                return root;
            }

            GameObject visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(gauchoFbx, root.transform);
            visualInstance.name = "Visuals";
            // Mismo offset exacto que usan GameServer y BartenderController sobre Silla.prefab (y = +0.35, z = +0.15)
            // para que la cadera apoye arriba de la tabla del banquito (y=0.48m) y las piernas caigan por delante sin atravesarlo
            visualInstance.transform.localPosition = new Vector3(0f, 0.35f, 0.15f);
            visualInstance.transform.localRotation = Quaternion.identity;
            visualInstance.transform.localScale = Vector3.one;

            // Configurar Animator
            Animator anim = visualInstance.GetComponent<Animator>();
            if (anim == null) anim = visualInstance.AddComponent<Animator>();
            anim.applyRootMotion = false;
            anim.runtimeAnimatorController = musicoController;

            Avatar avatar = LoadAvatarFromFbx(fbxGauchoPath);
            if (avatar != null) anim.avatar = avatar;

            // Muestrear pose de sentarse en el Editor
            AnimationClip sittingClip = GetClip("Assets/Models/Personajes/Animaciones/Core/Sitting Idle.fbx");
            if (sittingClip != null)
            {
                sittingClip.SampleAnimation(visualInstance, 0f);
            }

            // Aplicar materiales recoloreados a cuerpo, poncho y sombrero/boina (todos los submeshes)
            Renderer[] allRenderers = visualInstance.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in allRenderers)
            {
                string rName = r.gameObject.name.ToLowerInvariant();
                int slots = r.sharedMaterials != null ? Mathf.Max(1, r.sharedMaterials.Length) : 1;
                Material[] mats = new Material[slots];

                if (rName.Contains("body"))
                {
                    mats[0] = matBodyMusico;
                    for (int i = 1; i < slots; i++) mats[i] = matPonchoMusico;
                }
                else
                {
                    for (int i = 0; i < slots; i++) mats[i] = matBodyMusico;
                }

                r.sharedMaterials = mats;
            }

            // C. Instanciar la Guitarra sobre el torso (mixamorig:Spine1)
            string guitarraFbxPath = "Assets/Poderes/Modelos/Modelo FBX/Guitarra.fbx";
            GameObject guitarraFbx = AssetDatabase.LoadAssetAtPath<GameObject>(guitarraFbxPath);
            Transform chestBone = (anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Chest) : null)
                ?? FindChildRecursive(visualInstance.transform, "mixamorig:Spine1")
                ?? FindChildRecursive(visualInstance.transform, "Spine1")
                ?? visualInstance.transform;

            if (guitarraFbx != null)
            {
                GameObject guitarraInstance = (GameObject)PrefabUtility.InstantiatePrefab(guitarraFbx, chestBone);
                guitarraInstance.name = "Guitarra";

                // Preservar los 6 materiales embebidos de Guitarra.fbx (Cuerpo_madera, Mastil_paleta, Diapason, Puente_boca, Crema, Roseta)
                // y solo usar matGuitarra como respaldo si algún slot estuviera vacío
                if (matGuitarra != null)
                {
                    foreach (Renderer gr in guitarraInstance.GetComponentsInChildren<Renderer>(true))
                    {
                        Material[] currentMats = gr.sharedMaterials;
                        if (currentMats == null || currentMats.Length == 0)
                        {
                            gr.sharedMaterial = matGuitarra;
                        }
                        else
                        {
                            bool changed = false;
                            for (int i = 0; i < currentMats.Length; i++)
                            {
                                if (currentMats[i] == null)
                                {
                                    currentMats[i] = matGuitarra;
                                    changed = true;
                                }
                            }
                            if (changed) gr.sharedMaterials = currentMats;
                        }
                    }
                }
            }

            // D. Añadir GauchoGuitarPlayer y aplicar la pose de guitarra + manos inmediatamente (para que quede serializada en escena y prefab)
            GauchoGuitarPlayer guitarPlayer = root.AddComponent<GauchoGuitarPlayer>();
            guitarPlayer.animateHands = animateHands;
            guitarPlayer.CacheBones();
            guitarPlayer.ApplyGuitarAndArmPose(0f);

            return root;
        }

        private static Material CreateOrUpdateMaterial(string matPath, string texPath, Shader shader)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null)
            {
                AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader != null ? shader : Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else if (shader != null && mat.shader != shader)
            {
                mat.shader = shader;
            }

            if (tex != null)
            {
                mat.mainTexture = tex;
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static RuntimeAnimatorController SetupAnimatorController(string controllerPath)
        {
            AnimationClip sittingClip = GetClip("Assets/Models/Personajes/Animaciones/Core/Sitting Idle.fbx");
            if (sittingClip != null)
            {
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(sittingClip);
                if (!settings.loopTime)
                {
                    settings.loopTime = true;
                    AnimationUtility.SetAnimationClipSettings(sittingClip, settings);
                    EditorUtility.SetDirty(sittingClip);
                }
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState sittingState = null;
            foreach (var childState in sm.states)
            {
                if (childState.state.name == "SittingIdle")
                {
                    sittingState = childState.state;
                    break;
                }
            }

            if (sittingState == null)
            {
                sittingState = sm.AddState("SittingIdle");
            }

            sittingState.motion = sittingClip;
            sm.defaultState = sittingState;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static Avatar LoadAvatarFromFbx(string fbxPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (Object asset in assets)
            {
                if (asset is Avatar avatar && avatar.isValid)
                {
                    return avatar;
                }
            }
            return null;
        }

        private static AnimationClip GetClip(string fbxPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string nameContains)
        {
            if (parent == null) return null;
            if (parent.name.IndexOf(nameContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildRecursive(parent.GetChild(i), nameContains);
                if (found != null) return found;
            }
            return null;
        }
    }
}
