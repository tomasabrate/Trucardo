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
                .Where(t => t.gameObject.activeInHierarchy && Trucardo.Core.TableDefinition.IsSeatName(t.name))
                .Select(t => t.GetComponent<Trucardo.Core.SeatInteractable>() ?? Undo.AddComponent<Trucardo.Core.SeatInteractable>(t.gameObject))
                .ToList();

            for (int i = 0; i < tables.Count; i++)
            {
                Transform table = tables[i];
                string id = string.IsNullOrEmpty(table.name) || table.name.StartsWith("Mesa_Individual") ? $"Mesa_{i + 1}" : table.name;
                table.name = id;

                Trucardo.Core.TableDefinition definition = table.GetComponent<Trucardo.Core.TableDefinition>();
                if (definition == null)
                    definition = Undo.AddComponent<Trucardo.Core.TableDefinition>(table.gameObject);

                List<Trucardo.Core.SeatInteractable> mySeats = remainingSeats
                    .Where(s => s.transform.IsChildOf(table))
                    .OrderBy(s => s.transform.localPosition.x)
                    .Take(2)
                    .ToList();

                if (mySeats.Count < 2)
                {
                    int needed = 2 - mySeats.Count;
                    var nearest = remainingSeats
                        .Where(s => !s.transform.IsChildOf(table))
                        .OrderBy(s => HorizontalDistance(s.transform.position, table.position))
                        .Take(needed);
                    mySeats.AddRange(nearest);
                }

                if (mySeats.Count != 2)
                    throw new InvalidOperationException($"{id} no tiene dos bancos con SeatInteractable.");

                mySeats = mySeats.OrderByDescending(s => table.InverseTransformPoint(s.transform.position).x).ToList();

                mySeats[0].name = $"{id}_Banco_A";
                mySeats[1].name = $"{id}_Banco_B";
                definition.Configure(id, mySeats[0], mySeats[1]);
                remainingSeats.Remove(mySeats[0]);
                remainingSeats.Remove(mySeats[1]);

                EditorUtility.SetDirty(table.gameObject);
                EditorUtility.SetDirty(mySeats[0].gameObject);
                EditorUtility.SetDirty(mySeats[1].gameObject);
            }

            EditorSceneManager.MarkSceneDirty(group.scene);
            EditorSceneManager.SaveScene(group.scene);
            Debug.Log($"Configuradas {tables.Count} mesas en {group.scene.name}.");
        }

        public static void ConfigureSampleSceneBatch()
        {
            ConfigureSampleScene();
        }

        private static bool IsTableName(string name) => Trucardo.Core.TableDefinition.IsTableName(name);

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            first.y = 0f;
            second.y = 0f;
            return Vector3.Distance(first, second);
        }
    }
}
