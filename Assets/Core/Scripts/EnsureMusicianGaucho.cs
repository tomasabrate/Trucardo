using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Garantiza que siempre exista una instancia del Gaucho Músico en el bar,
    /// sentado en el banquito de 4 patas con guitarra, poncho y sombrero recoloreados,
    /// ejecutando su animación de sentado y rasgueo de guitarra en la esquina de la música.
    /// </summary>
    public class EnsureMusicianGaucho : MonoBehaviour
    {
        [SerializeField] private GameObject musicoPrefab;

        private void Awake()
        {
            EnsureMusician();
        }

        public GameObject EnsureMusician()
        {
            GameObject existing = GameObject.Find("Gaucho_Musico_Bar");
            if (existing != null)
            {
                Transform vis = existing.transform.Find("Visuals");
                if (vis != null && vis.localPosition.y < 0.25f)
                {
                    vis.localPosition = new Vector3(0f, 0.35f, 0.15f);
                }

                if (existing.GetComponent<GauchoGuitarPlayer>() == null)
                {
                    GauchoGuitarPlayer gp = existing.AddComponent<GauchoGuitarPlayer>();
                    gp.animateHands = true;
                }
                return existing;
            }

            // Si el usuario activó manualmente la versión de respaldo con mano fija, respetarla
            GameObject existingFixed = GameObject.Find("Gaucho_Musico_Bar_ManoFija");
            if (existingFixed != null && existingFixed.activeInHierarchy)
            {
                Transform visFixed = existingFixed.transform.Find("Visuals");
                if (visFixed != null && visFixed.localPosition.y < 0.25f)
                {
                    visFixed.localPosition = new Vector3(0f, 0.35f, 0.15f);
                }

                if (existingFixed.GetComponent<GauchoGuitarPlayer>() == null)
                {
                    GauchoGuitarPlayer gp = existingFixed.AddComponent<GauchoGuitarPlayer>();
                    gp.animateHands = false;
                }
                return existingFixed;
            }

            Vector3 cornerPos = new Vector3(-2.35f, 0.02f, 4.35f);
            Quaternion cornerRot = Quaternion.Euler(0f, 160f, 0f);

            GameObject prefab = musicoPrefab;
#if UNITY_EDITOR
            if (prefab == null)
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Core/Prefabs/GauchoMusicoPrefab.prefab");
            }
#endif
            if (prefab != null)
            {
                GameObject inst = Instantiate(prefab, cornerPos, cornerRot);
                inst.name = "Gaucho_Musico_Bar";
                if (inst.GetComponent<GauchoGuitarPlayer>() == null)
                {
                    GauchoGuitarPlayer gp = inst.AddComponent<GauchoGuitarPlayer>();
                    gp.animateHands = true;
                }
                return inst;
            }

#if UNITY_EDITOR
            return BuildRuntimeMusician(cornerPos, cornerRot);
#else
            return null;
#endif
        }

#if UNITY_EDITOR
        private GameObject BuildRuntimeMusician(Vector3 cornerPos, Quaternion cornerRot)
        {
            GameObject root = new GameObject("Gaucho_Musico_Bar");
            root.transform.position = cornerPos;
            root.transform.rotation = cornerRot;

            // Banquito bajo de 4 patas (Silla.prefab)
            GameObject sillaPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/BarRecursos/Silla.prefab");
            if (sillaPrefab != null)
            {
                GameObject banco = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(sillaPrefab, root.transform);
                banco.name = "Banco";
                banco.transform.localPosition = Vector3.zero;
                banco.transform.localRotation = Quaternion.identity;
                for (int i = 0; i < banco.transform.childCount; i++)
                {
                    Transform child = banco.transform.GetChild(i);
                    child.localPosition = Vector3.zero;
                    child.localRotation = Quaternion.identity;
                }
                SeatInteractable seat = banco.GetComponent<SeatInteractable>();
                if (seat != null) seat.enabled = false;
                foreach (Collider col in banco.GetComponentsInChildren<Collider>(true))
                    col.enabled = false;
            }

            // Gaucho
            GameObject fbxGaucho = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Personajes/Gaucho/guacho_boina_poncho.fbx");
            RuntimeAnimatorController animCtrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Core/Prefabs/GauchoMusicoAnimator.controller");
            Material matPoncho = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Personajes/Gaucho/Texturas/Mat_Gaucho_Musico_Poncho.mat");
            Material matBody = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Personajes/Gaucho/Texturas/Mat_Gaucho_Musico_Body.mat");

            if (fbxGaucho != null)
            {
                GameObject visuals = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(fbxGaucho, root.transform);
                visuals.name = "Visuals";
                visuals.transform.localPosition = new Vector3(0f, 0.35f, 0.15f);
                visuals.transform.localRotation = Quaternion.identity;

                Animator anim = visuals.GetComponent<Animator>();
                if (anim == null) anim = visuals.AddComponent<Animator>();
                anim.applyRootMotion = false;
                if (animCtrl != null) anim.runtimeAnimatorController = animCtrl;

                Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Models/Personajes/Gaucho/guacho_boina_poncho.fbx");
                foreach (var a in assets)
                {
                    if (a is Avatar av && av.isValid) { anim.avatar = av; break; }
                }

                Renderer[] rends = visuals.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in rends)
                {
                    string rn = r.gameObject.name.ToLowerInvariant();
                    int slots = r.sharedMaterials != null ? Mathf.Max(1, r.sharedMaterials.Length) : 1;
                    Material[] mats = new Material[slots];
                    if (rn.Contains("body"))
                    {
                        mats[0] = matBody;
                        for (int i = 1; i < slots; i++) mats[i] = matPoncho;
                    }
                    else
                    {
                        for (int i = 0; i < slots; i++) mats[i] = matBody;
                    }
                    r.sharedMaterials = mats;
                }

                // Guitarra
                GameObject guitarraFbx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Poderes/Modelos/Modelo FBX/Guitarra.fbx");
                Material matGuitarra = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Poderes/Materiales/Mat_Poder_Guitarra.mat");
                Transform chest = anim.GetBoneTransform(HumanBodyBones.Chest) ?? visuals.transform;

                if (guitarraFbx != null)
                {
                    GameObject guitarra = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(guitarraFbx, chest);
                    guitarra.name = "Guitarra";
                    if (matGuitarra != null)
                    {
                        foreach (Renderer gr in guitarra.GetComponentsInChildren<Renderer>(true))
                        {
                            Material[] currentMats = gr.sharedMaterials;
                            if (currentMats == null || currentMats.Length == 0)
                                gr.sharedMaterial = matGuitarra;
                        }
                    }
                }
            }

            GauchoGuitarPlayer gp = root.AddComponent<GauchoGuitarPlayer>();
            gp.animateHands = true;
            gp.CacheBones();
            gp.ApplyGuitarAndArmPose(0f);

            return root;
        }
#endif
    }
}
