using UnityEngine;

namespace Trucardo.Core
{
    public enum PowerRarity
    {
        Comun,
        Legendario
    }

    [CreateAssetMenu(fileName = "NewPower", menuName = "Trucardo/Power Data")]
    public class PowerData : ScriptableObject
    {
        [Header("Identificación")]
        [Tooltip("Identificador estable para red, guardado y estadísticas.")]
        public string PowerId;
        public string DisplayName;
        public PowerRarity Rarity = PowerRarity.Comun;

        [Header("Representación Tradicional")]
        [Tooltip("Objeto criollo/tradicional argentino que representa el poder.")]
        public string ObjectConcept;
        [Tooltip("Estado del modelo 3D (ej. Provisorio, En desarrollo, Final)")]
        public string ModelStatus = "Provisorio";

        [Header("Descripciones")]
        [TextArea(1, 3)]
        [Tooltip("Descripción corta para HUD, avisos y tooltips.")]
        public string ShortDescription;

        [TextArea(3, 8)]
        [Tooltip("Descripción detallada de la mecánica y aplicación técnica en el Truco.")]
        public string DetailedDescription;

        [TextArea(2, 5)]
        public string Description;

        public string GetEffectiveShortDescription()
        {
            return !string.IsNullOrWhiteSpace(ShortDescription) ? ShortDescription : Description;
        }

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
                && PowerId != "test_power"
                && !string.IsNullOrWhiteSpace(DisplayName)
                && Effect != null;
        }
    }
}
