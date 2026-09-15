using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using Trucardo.Core;

namespace Trucardo.EditorScripts
{
    public class NetworkSetupAuto
    {
        [MenuItem("Trucardo/1. Configurar Escena de Red Automáticamente")]
        public static void SetupNetworkScene()
        {
            NetworkManager netManager = Object.FindAnyObjectByType<NetworkManager>();
            if (netManager == null)
            {
                GameObject netGO = new GameObject("NetworkManager");
                netManager = netGO.AddComponent<NetworkManager>();
                netGO.AddComponent<UnityTransport>();
                netGO.AddComponent<NetworkUI>();
                
                netManager.NetworkConfig.NetworkTransport = netGO.GetComponent<UnityTransport>();
            }

            GameObject serverGO = new GameObject("GameServer");
            GameServer serverScript = serverGO.AddComponent<GameServer>();
            serverGO.AddComponent<NetworkObject>();

            DeckData mainDeck = AssetDatabase.LoadAssetAtPath<DeckData>("Assets/Data/MainDeck.asset");
            if (mainDeck != null)
            {
                SerializedObject serializedServer = new SerializedObject(serverScript);
                serializedServer.FindProperty("masterDeck").objectReferenceValue = mainDeck;
                serializedServer.ApplyModifiedProperties();
            }

            if (!AssetDatabase.IsValidFolder("Assets/Core/Prefabs"))
                AssetDatabase.CreateFolder("Assets/Core", "Prefabs");
            
            string prefabPath = "Assets/Core/Prefabs/GameServer.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(serverGO, prefabPath);
            
            SerializedObject serializedNetManager = new SerializedObject(netManager);
            SerializedProperty prefabsList = serializedNetManager.FindProperty("NetworkConfig.Prefabs.m_Prefabs");
            
            if (prefabsList != null && savedPrefab != null)
            {
                int index = prefabsList.arraySize;
                prefabsList.InsertArrayElementAtIndex(index);
                SerializedProperty newElement = prefabsList.GetArrayElementAtIndex(index);
                newElement.FindPropertyRelative("Prefab").objectReferenceValue = savedPrefab;
                serializedNetManager.ApplyModifiedProperties();
            }

            Debug.Log("¡Escena configurada!");
        }

        [MenuItem("Trucardo/2. Crear Entorno y Personaje (Visual) Automáticamente")]
        public static void SetupEnvironmentAndPlayer()
        {
            // --- 1. Crear Suelo Verde ---
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(5, 1, 5); // 50x50 metros
            
            Material greenMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/GreenFloor.mat");
            if (greenMat == null)
            {
                greenMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (greenMat.shader == null) greenMat = new Material(Shader.Find("Standard"));
                greenMat.color = new Color(0.2f, 0.6f, 0.3f);
                if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
                AssetDatabase.CreateAsset(greenMat, "Assets/Art/GreenFloor.mat");
            }
            floor.GetComponent<MeshRenderer>().sharedMaterial = greenMat;

            // --- 2. Crear Material Marrón ---
            Material brownMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/BrownWood.mat");
            if (brownMat == null)
            {
                brownMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (brownMat.shader == null) brownMat = new Material(Shader.Find("Standard"));
                brownMat.color = new Color(0.4f, 0.2f, 0.0f);
                if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
                AssetDatabase.CreateAsset(brownMat, "Assets/Art/BrownWood.mat");
            }

            // --- 3. Crear Mesa (Superficie y Patas) ---
            GameObject table = new GameObject("Mesa");
            table.transform.position = Vector3.zero;

            GameObject tableTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableTop.transform.SetParent(table.transform);
            tableTop.transform.localPosition = new Vector3(0, 0.9f, 0); // Altura de la mesa a 0.9m
            tableTop.transform.localScale = new Vector3(2f, 0.1f, 2f);
            tableTop.GetComponent<MeshRenderer>().sharedMaterial = brownMat;

            // Función local para patas
            void CreateLeg(Transform parent, Vector3 pos, Vector3 scale) {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.transform.SetParent(parent);
                leg.transform.localPosition = pos;
                leg.transform.localScale = scale;
                leg.GetComponent<MeshRenderer>().sharedMaterial = brownMat;
            }

            CreateLeg(table.transform, new Vector3(0.9f, 0.45f, 0.9f), new Vector3(0.1f, 0.9f, 0.1f));
            CreateLeg(table.transform, new Vector3(-0.9f, 0.45f, 0.9f), new Vector3(0.1f, 0.9f, 0.1f));
            CreateLeg(table.transform, new Vector3(0.9f, 0.45f, -0.9f), new Vector3(0.1f, 0.9f, 0.1f));
            CreateLeg(table.transform, new Vector3(-0.9f, 0.45f, -0.9f), new Vector3(0.1f, 0.9f, 0.1f));

            // --- 4. Crear Sillas ---
            GameObject CreateChair(string name, Vector3 position, float rotationY)
            {
                GameObject chair = new GameObject(name);
                // chair.tag = "Silla"; // REMOVIDO para evitar error si el tag no existe en el proyecto
                chair.transform.position = position;
                chair.transform.eulerAngles = new Vector3(0, rotationY, 0);

                // Asiento (Altura 0.45m)
                GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seat.transform.SetParent(chair.transform);
                seat.transform.localPosition = new Vector3(0, 0.45f, 0);
                seat.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
                seat.GetComponent<MeshRenderer>().sharedMaterial = brownMat;

                // Respaldo
                GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
                back.transform.SetParent(chair.transform);
                back.transform.localPosition = new Vector3(0, 0.75f, -0.225f);
                back.transform.localScale = new Vector3(0.5f, 0.6f, 0.05f);
                back.GetComponent<MeshRenderer>().sharedMaterial = brownMat;

                // Patas silla
                CreateLeg(chair.transform, new Vector3(0.2f, 0.225f, 0.2f), new Vector3(0.05f, 0.45f, 0.05f));
                CreateLeg(chair.transform, new Vector3(-0.2f, 0.225f, 0.2f), new Vector3(0.05f, 0.45f, 0.05f));
                CreateLeg(chair.transform, new Vector3(0.2f, 0.225f, -0.2f), new Vector3(0.05f, 0.45f, 0.05f));
                CreateLeg(chair.transform, new Vector3(-0.2f, 0.225f, -0.2f), new Vector3(0.05f, 0.45f, 0.05f));

                return chair;
            }

            CreateChair("Silla1", new Vector3(0, 0, -1.6f), 0);
            CreateChair("Silla2", new Vector3(0, 0, 1.6f), 180);

            // --- Puntos de Aparición (Spawns) ---
            GameObject spawn1 = new GameObject("SpawnPoint1");
            spawn1.transform.position = new Vector3(-3, 0.5f, -3);

            GameObject spawn2 = new GameObject("SpawnPoint2");
            spawn2.transform.position = new Vector3(3, 0.5f, 3);

            // --- 4. Crear Jugador First Person Avanzado ---
            GameObject playerGO = new GameObject("PlayerPrefab");
            
            // Componentes Raíz
            playerGO.AddComponent<NetworkObject>();
            playerGO.AddComponent<ClientNetworkTransform>();
            
            CharacterController cc = playerGO.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 1f, 0); // Para que los pies queden en Y=0
            cc.height = 2f;
            
            PlayerController controller = playerGO.AddComponent<PlayerController>();

            // Cuerpo (Cubo)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Cuerpo";
            body.transform.SetParent(playerGO.transform);
            body.transform.localPosition = new Vector3(0, 0.5f, 0);
            body.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            Object.DestroyImmediate(body.GetComponent<BoxCollider>());

            // Cabeza (Esfera)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Cabeza";
            head.transform.SetParent(playerGO.transform);
            head.transform.localPosition = new Vector3(0, 1.25f, 0);
            head.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Object.DestroyImmediate(head.GetComponent<SphereCollider>());

            // Cámara
            GameObject camObj = new GameObject("FirstPersonCamera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.transform.SetParent(head.transform);
            camObj.transform.localPosition = new Vector3(0, 0, 0.2f);

            // Asignar referencias al PlayerController
            controller.cameraTransform = camObj.transform;
            controller.localMeshesToHide = new GameObject[] { body, head };

            // Intentar recuperar el CardPrefab si ya fue creado
            GameObject existingCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Core/Prefabs/CardPrefab.prefab");
            if (existingCardPrefab != null)
            {
                controller.cardPrefab = existingCardPrefab;

                GameServer serverInstance = Object.FindAnyObjectByType<GameServer>();
                if (serverInstance != null)
                {
                    SerializedObject serializedServer = new SerializedObject(serverInstance);
                    serializedServer.FindProperty("cardPrefab").objectReferenceValue = existingCardPrefab;
                    serializedServer.ApplyModifiedProperties();
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(serverInstance.gameObject.scene);
                }
            }

            // Guardar Prefab
            string prefabPath = "Assets/Core/Prefabs/PlayerPrefab.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(playerGO, prefabPath);
            Object.DestroyImmediate(playerGO); // Borrar de escena

            // Asignar al NetworkManager
            NetworkManager netManager = Object.FindAnyObjectByType<NetworkManager>();
            if (netManager != null && savedPrefab != null)
            {
                SerializedObject serializedNetManager = new SerializedObject(netManager);
                serializedNetManager.FindProperty("NetworkConfig.PlayerPrefab").objectReferenceValue = savedPrefab;
                serializedNetManager.ApplyModifiedProperties();
                Debug.Log("¡Entorno creado y PlayerPrefab configurado con éxito!");
            }
        }

        [MenuItem("Trucardo/3. Crear Prefab de Carta 3D Automáticamente")]
        public static void CreateCardPrefab()
        {
            // Raíz de la carta (Escala 1,1,1 para evitar distorsión)
            GameObject cardRoot = new GameObject("CardPrefab");

            // Malla visual (Cubo aplastado más chico)
            GameObject meshGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            meshGO.name = "Mesh";
            meshGO.transform.SetParent(cardRoot.transform);
            meshGO.transform.localPosition = Vector3.zero;
            meshGO.transform.localScale = new Vector3(0.07f, 0.002f, 0.125f); // 7cm x 12.5cm

            // Material Blanco
            Material whiteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/WhiteCard.mat");
            if (whiteMat == null)
            {
                whiteMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (whiteMat.shader == null) whiteMat = new Material(Shader.Find("Standard"));
                whiteMat.color = Color.white;
                whiteMat.SetFloat("_Smoothness", 0.1f);
                if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
                AssetDatabase.CreateAsset(whiteMat, "Assets/Art/WhiteCard.mat");
            }
            meshGO.GetComponent<MeshRenderer>().sharedMaterial = whiteMat;

            // Agregar NetworkObject al Root
            cardRoot.AddComponent<NetworkObject>();
            cardRoot.AddComponent<NetworkTransform>();

            // Script visual
            CardVisual visual = cardRoot.AddComponent<CardVisual>();

            // TEXTO DEL PALO (Izquierda)
            GameObject textSuitGO = new GameObject("TextSuit");
            textSuitGO.transform.SetParent(cardRoot.transform);
            textSuitGO.transform.localPosition = new Vector3(-0.02f, 0.002f, 0); // Izquierda
            textSuitGO.transform.localEulerAngles = new Vector3(90, 0, 0);
            textSuitGO.transform.localScale = Vector3.one; 

            TextMesh tmSuit = textSuitGO.AddComponent<TextMesh>();
            tmSuit.text = "Espada";
            tmSuit.fontSize = 40;
            tmSuit.characterSize = 0.005f; 
            tmSuit.anchor = TextAnchor.MiddleRight; // Alineado a la derecha para que termine cerca del centro
            tmSuit.alignment = TextAlignment.Right;
            tmSuit.color = Color.black;

            // TEXTO DEL NUMERO (Derecha)
            GameObject textNumGO = new GameObject("TextNumber");
            textNumGO.transform.SetParent(cardRoot.transform);
            textNumGO.transform.localPosition = new Vector3(0.02f, 0.002f, 0); // Derecha
            textNumGO.transform.localEulerAngles = new Vector3(90, 0, 0);
            textNumGO.transform.localScale = Vector3.one; 

            TextMesh tmNum = textNumGO.AddComponent<TextMesh>();
            tmNum.text = "1";
            tmNum.fontSize = 60; // Número más grande
            tmNum.characterSize = 0.005f; 
            tmNum.anchor = TextAnchor.MiddleLeft; // Alineado a la izquierda para que empiece cerca del centro
            tmNum.alignment = TextAlignment.Left;
            tmNum.color = Color.black;

            // visual.textSuit = tmSuit;
            // visual.textNumber = tmNum;

            // Guardar Prefab
            string prefabPath = "Assets/Core/Prefabs/CardPrefab.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(cardRoot, prefabPath);
            Object.DestroyImmediate(cardRoot);

            // Agregar al NetworkManager
            NetworkManager netManager = Object.FindAnyObjectByType<NetworkManager>();
            if (netManager != null && savedPrefab != null)
            {
                SerializedObject serializedNetManager = new SerializedObject(netManager);
                SerializedProperty prefabsList = serializedNetManager.FindProperty("NetworkConfig.Prefabs.m_Prefabs");
                
                if (prefabsList != null)
                {
                    // Verificar si ya existe
                    bool exists = false;
                    for (int i = 0; i < prefabsList.arraySize; i++)
                    {
                        if (prefabsList.GetArrayElementAtIndex(i).FindPropertyRelative("Prefab").objectReferenceValue == savedPrefab)
                            exists = true;
                    }

                    if (!exists)
                    {
                        int index = prefabsList.arraySize;
                        prefabsList.InsertArrayElementAtIndex(index);
                        prefabsList.GetArrayElementAtIndex(index).FindPropertyRelative("Prefab").objectReferenceValue = savedPrefab;
                        serializedNetManager.ApplyModifiedProperties();
                    }
                }
                else
                {
                    Debug.LogWarning("No se pudo añadir el CardPrefab a la lista del NetworkManager automáticamente. Por favor añádelo manualmente en el Inspector.");
                }
                
                Debug.Log("¡Prefab de Carta 3D creado y añadido al NetworkManager!");

                // Añadir referencia al PlayerPrefab
                GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Core/Prefabs/PlayerPrefab.prefab");
                if (playerPrefab != null)
                {
                    PlayerController pc = playerPrefab.GetComponent<PlayerController>();
                    if (pc != null)
                    {
                        SerializedObject serializedPC = new SerializedObject(pc);
                        serializedPC.FindProperty("cardPrefab").objectReferenceValue = savedPrefab;
                        serializedPC.ApplyModifiedProperties();
                    }
                }

                // Añadir referencia al GameServer
                GameServer serverInstance = Object.FindAnyObjectByType<GameServer>();
                if (serverInstance != null)
                {
                    SerializedObject serializedServer = new SerializedObject(serverInstance);
                    serializedServer.FindProperty("cardPrefab").objectReferenceValue = savedPrefab;
                    serializedServer.ApplyModifiedProperties();
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(serverInstance.gameObject.scene);
                }
                
                // Llamar automáticamente al configurador minimalista
                CardBuilderEditor.ConfigurarCartas();
            }
        }
        [MenuItem("Trucardo/4. Ajustar Tamaño de Mesa y Sillas")]
        public static void AdjustTableLayout()
        {
            GameObject table = GameObject.Find("Mesa");
            if (table != null)
            {
                Transform tableTop = table.transform.GetChild(0); // El cubo de arriba
                if (tableTop != null) tableTop.localScale = new Vector3(1.4f, 0.1f, 1.4f); // Mesa más chica
                
                // Mover las patas un poco más al centro
                if (table.transform.childCount >= 5)
                {
                    table.transform.GetChild(1).localPosition = new Vector3(0.6f, 0.45f, 0.6f);
                    table.transform.GetChild(2).localPosition = new Vector3(-0.6f, 0.45f, 0.6f);
                    table.transform.GetChild(3).localPosition = new Vector3(0.6f, 0.45f, -0.6f);
                    table.transform.GetChild(4).localPosition = new Vector3(-0.6f, 0.45f, -0.6f);
                }
            }

            // Alejar las sillas para que el jugador tenga mejor perspectiva de la mesa
            GameObject chair1 = GameObject.Find("Silla1");
            if (chair1 != null) chair1.transform.position = new Vector3(0, 0, -1.35f);

            GameObject chair2 = GameObject.Find("Silla2");
            if (chair2 != null) chair2.transform.position = new Vector3(0, 0, 1.35f);

            Debug.Log("¡Mesa achicada y sillas acercadas con éxito!");
        }
    }
}
