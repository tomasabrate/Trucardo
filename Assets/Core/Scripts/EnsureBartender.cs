using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Garantiza que siempre exista una instancia del Bartender en la barra,
    /// ya sea al probar directamente en el Editor o en cualquier modo de juego.
    /// </summary>
    public class EnsureBartender : MonoBehaviour
    {
        [SerializeField] private GameObject bartenderPrefab;

        private void Awake()
        {
            Ensure();
        }

        public GameObject Ensure()
        {
            GameObject existing = GameObject.Find("Bartender");
            if (existing != null) return existing;

            GameObject prefab = bartenderPrefab;
#if UNITY_EDITOR
            if (prefab == null)
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Core/Prefabs/BartenderPrefab.prefab");
            }
#endif
            if (prefab != null)
            {
                GameObject inst = Instantiate(prefab, transform.position, transform.rotation);
                inst.name = "Bartender";
                inst.transform.SetParent(transform.parent);

                var ctrl = inst.GetComponent<BartenderController>();
                if (ctrl != null)
                {
                    ctrl.Initialize(transform.position, transform.rotation);
                }
                return inst;
            }
            return null;
        }
    }
}
