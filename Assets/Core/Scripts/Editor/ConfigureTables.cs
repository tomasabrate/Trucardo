using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Trucardo.EditorScripts
{
    public static class ConfigureTables
    {
        [MenuItem("Trucardo/Configurar mesas y bancos")]
        public static void ConfigureSampleScene()
        {
            if (EditorSceneManager.GetActiveScene().path != "Assets/Scenes/SampleScene.unity")
            {
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            }

            GameObject group = GameObject.Find("Mesas_y_Sillas");
            if (group == null)
                throw new InvalidOperationException("No se encontró Mesas_y_Sillas en la escena abierta.");

            List<Transform> tables = group.GetComponentsInChildren<Transform>(true)
                .Where(t => t != group.transform && IsTableName(t.name))
                .ToList();

            if (tables.Count == 0)
                throw new InvalidOperationException("No se encontraron mesas dentro de Mesas_y_Sillas.");

            List<Trucardo.Core.SeatInteractable> remainingSeats = group.GetComponentsInChildren<Transform>(true)
                .Where(t => t.GetComponent<Trucardo.Core.SeatInteractable>() != null)
                .Select(t => t.GetComponent<Trucardo.Core.SeatInteractable>())
                .ToList();

            for (int i = 0; i < tables.Count; i++)
            {
                Transform table = tables[i];
                string id = $"Mesa_{i + 1}";
                table.name = id;

                Trucardo.Core.TableDefinition definition = table.GetComponent<Trucardo.Core.TableDefinition>();
                if (definition == null)
                    definition = Undo.AddComponent<Trucardo.Core.TableDefinition>(table.gameObject);

                List<Trucardo.Core.SeatInteractable> nearestSeats = remainingSeats
                    .Where(s => s != null)
                    .OrderBy(s => HorizontalDistance(s.transform.position, table.position))
                    .Take(2)
                    .ToList();

                if (nearestSeats.Count != 2)
                    throw new InvalidOperationException($"{id} no tiene dos bancos con SeatInteractable.");

                nearestSeats[0].name = $"{id}_Banco_A";
                nearestSeats[1].name = $"{id}_Banco_B";
                definition.Configure(id, nearestSeats[0], nearestSeats[1]);
                remainingSeats.Remove(nearestSeats[0]);
                remainingSeats.Remove(nearestSeats[1]);

                EditorUtility.SetDirty(table.gameObject);
                EditorUtility.SetDirty(nearestSeats[0].gameObject);
                EditorUtility.SetDirty(nearestSeats[1].gameObject);
            }

            EditorSceneManager.MarkSceneDirty(group.scene);
            EditorSceneManager.SaveScene(group.scene);
            Debug.Log($"Configuradas {tables.Count} mesas en {group.scene.name}.");
        }

        public static void ConfigureSampleSceneBatch()
        {
            ConfigureSampleScene();
        }

        private static bool IsTableName(string name)
        {
            string lowerName = name.ToLowerInvariant();
            return lowerName.Contains("mesa") && !lowerName.Contains("mesas_y_sillas");
        }

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            first.y = 0f;
            second.y = 0f;
            return Vector3.Distance(first, second);
        }
    }
}
