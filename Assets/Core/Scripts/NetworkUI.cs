using Unity.Netcode;
using UnityEngine;

namespace Trucardo.Core
{
    /// <summary>
    /// UI básica temporal para poder iniciar sesión como Host o Cliente en el juego compilado (Build).
    /// </summary>
    public class NetworkUI : MonoBehaviour
    {
        void OnGUI()
        {
            if (NetworkManager.Singleton == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            
            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                if (GUILayout.Button("Start Host", GUILayout.Width(200), GUILayout.Height(50)))
                {
                    NetworkManager.Singleton.StartHost();
                }
                if (GUILayout.Button("Start Client", GUILayout.Width(200), GUILayout.Height(50)))
                {
                    NetworkManager.Singleton.StartClient();
                }
            }
            else
            {
                GUILayout.Label($"Estado: {(NetworkManager.Singleton.IsServer ? "Servidor/Host" : "Cliente Conectado")}");
            }

            GUILayout.EndArea();
        }
    }
}
