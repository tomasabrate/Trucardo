using Unity.Netcode;
using Unity.Netcode.Transports.UTP; // Necesario para configurar la IP
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trucardo.Core
{
    public enum GameMode
    {
        None,
        Singleplayer,
        Host,
        Client
    }

    /// <summary>
    /// UI básica temporal para poder iniciar sesión como Host o Cliente en el juego compilado (Build).
    /// </summary>
    public class NetworkUI : MonoBehaviour
    {
        public static GameMode selectedMode = GameMode.None;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            selectedMode = GameMode.None;
        }
        
        // IP por defecto (localhost)
        private string ipAddress = "127.0.0.1";

        void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartSelectedMode();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "SampleScene") StartSelectedMode();
        }

        private void StartSelectedMode()
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsListening) return;

            if (selectedMode == GameMode.Singleplayer || selectedMode == GameMode.Host)
            {
                NetworkManager.Singleton.StartHost();
            }
        }

        void OnGUI()
        {
            if (SceneManager.GetActiveScene().name != "SampleScene") return;
            if (NetworkManager.Singleton == null) return;

            // Si ya estamos conectados, mostramos solo el estado y salimos
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            {
                GUILayout.BeginArea(new Rect(10, 10, 300, 300));
                if (selectedMode == GameMode.Singleplayer)
                    GUILayout.Label("Estado: Modo Individual (Solo)");
                else
                    GUILayout.Label($"Estado: {(NetworkManager.Singleton.IsServer ? "Servidor/Host" : "Cliente Conectado")}");
                GUILayout.EndArea();
                return;
            }

            // Si seleccionó Cliente en el menú principal, le mostramos el cuadro para ingresar la IP
            if (selectedMode == GameMode.Client)
            {
                // Centramos el cuadro en la pantalla
                GUILayout.BeginArea(new Rect(Screen.width / 2f - 150, Screen.height / 2f - 100, 300, 200));

                // Fondo oscuro
                GUI.Box(new Rect(0, 0, 300, 200), "Conectarse al Host");
                
                GUILayout.Space(40);
                GUILayout.Label("Ingresa la IP local del Host:");
                
                // Campo de texto para la IP
                ipAddress = GUILayout.TextField(ipAddress, GUILayout.Height(30));
                
                GUILayout.Space(20);
                if (GUILayout.Button("Conectar", GUILayout.Height(50)))
                {
                    // Configuramos la IP antes de iniciar el cliente
                    var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                    if (transport != null)
                    {
                        transport.SetConnectionData(ipAddress, (ushort)7777);
                    }
                    NetworkManager.Singleton.StartClient();
                }
                
                GUILayout.EndArea();
                return;
            }

            // Si seleccionó Singleplayer o Host, está cargando, no mostramos nada más
            if (selectedMode != GameMode.None) return;

            // --- FALLBACK UI (por si corren la escena directamente sin pasar por el menú) ---
            GUILayout.BeginArea(new Rect(10, 10, 300, 350));
            
            if (GUILayout.Button("Individual (Solo)", GUILayout.Width(200), GUILayout.Height(50)))
            {
                selectedMode = GameMode.Singleplayer;
                NetworkManager.Singleton.StartHost();
            }
            
            GUILayout.Space(10);
            if (GUILayout.Button("Start Host", GUILayout.Width(200), GUILayout.Height(50)))
            {
                selectedMode = GameMode.Host;
                NetworkManager.Singleton.StartHost();
            }
            
            GUILayout.Space(10);
            ipAddress = GUILayout.TextField(ipAddress, GUILayout.Width(200), GUILayout.Height(30));
            
            if (GUILayout.Button("Start Client", GUILayout.Width(200), GUILayout.Height(50)))
            {
                selectedMode = GameMode.Client;
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    transport.SetConnectionData(ipAddress, (ushort)7777);
                }
                NetworkManager.Singleton.StartClient();
            }

            GUILayout.EndArea();
        }
    }
}
