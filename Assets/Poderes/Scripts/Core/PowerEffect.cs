using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Define la lógica que se ejecuta cuando se usa un poder.
    /// Retorna true si el poder se ejecutó con éxito y debe consumirse de la mesa,
    /// o false si las condiciones no se cumplieron (evitando que el jugador pierda el poder).
    /// </summary>
    public abstract class PowerEffect : ScriptableObject
    {
        public virtual bool CanExecute(PowerContext context, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public abstract bool Execute(PowerContext context);
    }
}
