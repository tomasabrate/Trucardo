using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Trucardo.Core;

[InitializeOnLoad]
public class SetupGauchoMaterials
{
    static SetupGauchoMaterials()
    {
        EditorApplication.delayCall += Setup;
    }

    [MenuItem("Tools/Setup Gaucho Materials")]
    public static void Setup()
    {
        string texPlayerPath = "Assets/Models/Personajes/Gaucho/Texturas/guacho_texture_1.png";
        string texBotPath = "Assets/Models/Personajes/Gaucho/Texturas/guacho_texture_2.png";
        string matPlayerPath = "Assets/Models/Personajes/Gaucho/Texturas/Mat_Gaucho_Player.mat";
        string matBotPath = "Assets/Models/Personajes/Gaucho/Texturas/Mat_Gaucho_Bot.mat";
        string playerPrefabPath = "Assets/Core/Prefabs/PlayerPrefab.prefab";
        string botPrefabPath = "Assets/Core/Prefabs/BotPrefab.prefab";

        AssetDatabase.ImportAsset(texPlayerPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(texBotPath, ImportAssetOptions.ForceUpdate);

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        // 1. Material del Jugador (Textura 1)
        Material matPlayer = AssetDatabase.LoadAssetAtPath<Material>(matPlayerPath);
        if (matPlayer == null)
        {
            matPlayer = new Material(urpLit != null ? urpLit : Shader.Find("Standard"));
            AssetDatabase.CreateAsset(matPlayer, matPlayerPath);
        }
        else if (urpLit != null)
        {
            matPlayer.shader = urpLit;
        }

        Texture2D texPlayer = AssetDatabase.LoadAssetAtPath<Texture2D>(texPlayerPath);
        if (texPlayer != null)
        {
            matPlayer.SetTexture("_BaseMap", texPlayer);
            matPlayer.SetTexture("_MainTex", texPlayer);
        }
        matPlayer.SetFloat("_Smoothness", 0.1f);
        EditorUtility.SetDirty(matPlayer);

        // 2. Material de los Bots (Textura 2)
        Material matBot = AssetDatabase.LoadAssetAtPath<Material>(matBotPath);
        if (matBot == null)
        {
            matBot = new Material(urpLit != null ? urpLit : Shader.Find("Standard"));
            AssetDatabase.CreateAsset(matBot, matBotPath);
        }
        else if (urpLit != null)
        {
            matBot.shader = urpLit;
        }

        Texture2D texBot = AssetDatabase.LoadAssetAtPath<Texture2D>(texBotPath);
        if (texBot != null)
        {
            matBot.SetTexture("_BaseMap", texBot);
            matBot.SetTexture("_MainTex", texBot);
        }
        matBot.SetFloat("_Smoothness", 0.1f);
        EditorUtility.SetDirty(matBot);

        AssetDatabase.SaveAssets();

        // 3. Asignar Mat_Gaucho_Player al PlayerPrefab
        GameObject playerPrefabObj = PrefabUtility.LoadPrefabContents(playerPrefabPath);
        if (playerPrefabObj != null)
        {
            Transform visuals = playerPrefabObj.transform.Find("Visuals");
            if (visuals != null)
            {
                Renderer[] rends = visuals.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    r.sharedMaterial = matPlayer;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(playerPrefabObj, playerPrefabPath);
            PrefabUtility.UnloadPrefabContents(playerPrefabObj);
            Debug.Log("[SetupGauchoMaterials] PlayerPrefab configurado con Textura 1 (Mat_Gaucho_Player).");
        }

        // 4. Crear/Configurar BotPrefab con Mat_Gaucho_Bot
        GameObject botPrefabObj = PrefabUtility.LoadPrefabContents(playerPrefabPath);
        if (botPrefabObj != null)
        {
            Transform visuals = botPrefabObj.transform.Find("Visuals");
            if (visuals != null)
            {
                Renderer[] rends = visuals.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    r.sharedMaterial = matBot;
                }
            }
            GameObject savedBotPrefab = PrefabUtility.SaveAsPrefabAsset(botPrefabObj, botPrefabPath);
            PrefabUtility.UnloadPrefabContents(botPrefabObj);
            Debug.Log("[SetupGauchoMaterials] BotPrefab creado/configurado con Textura 2 (Mat_Gaucho_Bot).");

            // 5. Asignar botPrefab en GameServer si existe en la escena
            GameServer server = Object.FindFirstObjectByType<GameServer>();
            if (server != null && savedBotPrefab != null)
            {
                server.botPrefab = savedBotPrefab;
                EditorUtility.SetDirty(server);
                Debug.Log("[SetupGauchoMaterials] botPrefab (BotPrefab con Textura 2) asignado al GameServer de la escena.");

                Scene activeScene = SceneManager.GetActiveScene();
                if (activeScene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(activeScene);
                    EditorSceneManager.SaveScene(activeScene);
                }
            }
        }
    }
}
