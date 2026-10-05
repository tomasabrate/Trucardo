using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Define la lógica que se ejecuta cuando se usa un poder.
    /// </summary>
    public abstract class PowerEffect : ScriptableObject
    {
        public abstract void Execute(PowerContext context);
    }
}
