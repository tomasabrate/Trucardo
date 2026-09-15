using Unity.Netcode.Components;

namespace Trucardo.Core
{
    /// <summary>
    /// Componente que reemplaza al NetworkTransform por defecto de Unity.
    /// Permite que cada jugador (Owner) tenga autoridad sobre su propia posición,
    /// evitando que el Servidor los teletransporte de vuelta al origen.
    /// </summary>
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
