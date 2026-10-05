using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// Poder de prueba para validar el flujo antes de implementar efectos de juego.
    /// </summary>
    [CreateAssetMenu(fileName = "LogPowerEffect", menuName = "Trucardo/Power Effects/Log Test Effect")]
    public class LogPowerEffect : PowerEffect
    {
        [TextArea(2, 4)]
        public string Message = "El poder de prueba fue ejecutado.";

        public override void Execute(PowerContext context)
        {
            if (context == null)
            {
                Debug.LogError("[Power] No se puede ejecutar el poder sin contexto.");
                return;
            }

            Debug.Log($"[Power] {Message} Jugador: {context.PlayerId}, equipo: {context.Team}.");
        }
    }
}
