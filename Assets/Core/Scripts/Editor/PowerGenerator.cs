using System.IO;
using UnityEditor;
using UnityEngine;
using Trucardo.Core;

namespace Trucardo.EditorScripts
{
    public static class PowerGenerator
    {
        [MenuItem("Trucardo/Generar Poder de Prueba")]
        public static void GenerateTestPower()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder("Assets/Data/Powers");
            EnsureFolder("Assets/Data/Powers/Test");

            const string effectPath = "Assets/Data/Powers/Test/LogPowerEffect.asset";
            const string powerPath = "Assets/Data/Powers/Test/PoderDePrueba.asset";
            const string catalogPath = "Assets/Data/Powers/PowerCatalog.asset";

            LogPowerEffect effect = AssetDatabase.LoadAssetAtPath<LogPowerEffect>(effectPath);
            if (effect == null)
            {
                effect = ScriptableObject.CreateInstance<LogPowerEffect>();
                effect.Message = "El poder de prueba funciona correctamente.";
                AssetDatabase.CreateAsset(effect, effectPath);
            }

            PowerData power = AssetDatabase.LoadAssetAtPath<PowerData>(powerPath);
            if (power == null)
            {
                power = ScriptableObject.CreateInstance<PowerData>();
                power.PowerId = "test_power";
                power.DisplayName = "Poder de prueba";
                power.Description = "Poder temporal para validar selección y ejecución.";
                power.Effect = effect;
                AssetDatabase.CreateAsset(power, powerPath);
            }

            PowerCatalog catalog = AssetDatabase.LoadAssetAtPath<PowerCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PowerCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            if (!catalog.AvailablePowers.Contains(power))
            {
                catalog.AvailablePowers.Add(power);
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = power;
            Debug.Log("Poder de prueba generado en Assets/Data/Powers/Test.");
        }

        private static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
