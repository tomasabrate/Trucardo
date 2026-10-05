using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Datos disponibles para un efecto al momento de ejecutarse.
    /// </summary>
    public sealed class PowerContext
    {
        public ulong PlayerId { get; }
        public int Team { get; }
        public GameObject Source { get; }
        public GameObject Target { get; }
        public GameModel GameModel { get; }

        public PowerContext(
            ulong playerId,
            int team,
            GameObject source = null,
            GameObject target = null,
            GameModel gameModel = null)
        {
            PlayerId = playerId;
            Team = team;
            Source = source;
            Target = target;
            GameModel = gameModel;
        }
    }
}
