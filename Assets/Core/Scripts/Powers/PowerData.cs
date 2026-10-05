using UnityEngine;

namespace Trucardo.Core
{
    [CreateAssetMenu(fileName = "NewPower", menuName = "Trucardo/Power Data")]
    public class PowerData : ScriptableObject
    {
        [Header("Identificación")]
        [Tooltip("Identificador estable para red, guardado y estadísticas.")]
        public string PowerId;
        public string DisplayName;

        [TextArea(2, 5)]
        public string Description;

        [Header("Presentación")]
        [Tooltip("Imagen que se muestra al elegir entre poderes.")]
        public Sprite SelectionImage;
        [Tooltip("Prefab 3D que se instancia al presentar o ejecutar el poder.")]
        public GameObject WorldPrefab;

        [Header("Reglas")]
        public PowerEffect Effect;
        [Min(0)]
        public float Cooldown;
        [Min(0)]
        public int RequiredCost;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(PowerId)
                && !string.IsNullOrWhiteSpace(DisplayName)
                && Effect != null;
        }
    }
}
