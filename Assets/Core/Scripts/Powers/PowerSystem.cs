using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Punto de entrada para validar y ejecutar poderes.
    /// La autoridad de red deberá invocar este flujo desde el servidor.
    /// </summary>
    public class PowerSystem : MonoBehaviour
    {
        [SerializeField] private PowerCatalog catalog;

        public PowerCatalog Catalog => catalog;

        [ContextMenu("Ejecutar primer poder del catálogo")]
        private void ExecuteFirstCatalogPower()
        {
            TryExecuteFromCatalog(0, 0, 0);
        }

        public bool TryExecute(
            PowerData power,
            ulong playerId,
            int team,
            GameObject source = null,
            GameObject target = null,
            GameModel gameModel = null)
        {
            if (power == null)
            {
                Debug.LogError("[Power] No se puede ejecutar un poder nulo.");
                return false;
            }

            if (!power.IsValid())
            {
                Debug.LogError($"[Power] El poder '{power.name}' no está configurado correctamente.");
                return false;
            }

            PowerContext context = new PowerContext(playerId, team, source, target, gameModel);
            power.Effect.Execute(context);
            return true;
        }

        public bool TryExecuteFromCatalog(
            int index,
            ulong playerId,
            int team,
            GameObject source = null,
            GameObject target = null,
            GameModel gameModel = null)
        {
            if (catalog == null)
            {
                Debug.LogError("[Power] PowerSystem no tiene un catálogo asignado.");
                return false;
            }

            if (index < 0 || index >= catalog.AvailablePowers.Count)
            {
                Debug.LogError($"[Power] Índice de poder fuera de rango: {index}.");
                return false;
            }

            return TryExecute(catalog.AvailablePowers[index], playerId, team, source, target, gameModel);
        }
    }
}
