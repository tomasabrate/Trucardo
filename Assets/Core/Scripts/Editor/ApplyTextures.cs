using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;

public class RearrangeTables
{
    [MenuItem("Tools/Acomodar Mesas")]
    public static void Apply()
    {
        GameObject parent = GameObject.Find("Mesas_y_Sillas");
        GameObject barra = GameObject.Find("Barra_Bar");

        if (parent == null || barra == null)
        {
            Debug.LogError("No se encontró Mesas_y_Sillas o Barra_Bar.");
            return;
        }

        // Buscar todos los hijos que sean "Mesa" (o que contengan Mesa en el nombre)
        List<Transform> mesas = new List<Transform>();
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            Transform child = parent.transform.GetChild(i);
            // Cada hijo de Mesas_y_Sillas en tu escena es una Mesa.prefab
            mesas.Add(child);
        }

        if (mesas.Count == 0) return;

        // Las 3 mesas cerca de la barra son las que tienen la distancia más corta a la barra en Z o X.
        // Asumiendo que la barra está en el fondo, podemos ordenarlas por distancia a la barra.
        var mesasOrdenadas = mesas.OrderBy(m => Vector3.Distance(m.position, barra.transform.position)).ToList();

        // Tomar las 3 más cercanas
        var mesasCercaBarra = mesasOrdenadas.Take(3).ToList();
        
        // Ahora, de esas 3, ordenarlas por el eje X para saber cuál es izquierda, centro y derecha.
        // Si la barra está en Z, el eje a lo largo de la barra es X. (o viceversa)
        // Tomaremos la mayor varianza de ejes para ordenarlas.
        float varX = mesasCercaBarra.Max(m => m.position.x) - mesasCercaBarra.Min(m => m.position.x);
        float varZ = mesasCercaBarra.Max(m => m.position.z) - mesasCercaBarra.Min(m => m.position.z);
        
        if (varX > varZ)
            mesasCercaBarra = mesasCercaBarra.OrderBy(m => m.position.x).ToList();
        else
            mesasCercaBarra = mesasCercaBarra.OrderBy(m => m.position.z).ToList();

        if (mesasCercaBarra.Count == 3)
        {
            Transform mesaIzq = mesasCercaBarra[0];
            Transform mesaCentro = mesasCercaBarra[1];
            Transform mesaDer = mesasCercaBarra[2];

            // 1. Eliminar la del medio
            Undo.DestroyObjectImmediate(mesaCentro.gameObject);

            // 2. Mover las otras 2 más al centro de la habitación (lejos de la barra).
            // Para saber hacia dónde es el "centro", miramos las otras mesas de la habitación.
            var otrasMesas = mesasOrdenadas.Skip(3).ToList();
            Vector3 centroOtrasMesas = Vector3.zero;
            foreach (var m in otrasMesas) centroOtrasMesas += m.position;
            if (otrasMesas.Count > 0) centroOtrasMesas /= otrasMesas.Count;

            // Mover la mesaIzq y mesaDer hacia el centro de las otras mesas en un 30% o unos 2 metros.
            Vector3 dirIzq = (centroOtrasMesas - mesaIzq.position).normalized;
            Vector3 dirDer = (centroOtrasMesas - mesaDer.position).normalized;

            // Ignoramos el cambio en Y
            dirIzq.y = 0; dirDer.y = 0;

            // Las movemos unos 1.5 metros hacia el centro de la sala
            Undo.RecordObject(mesaIzq, "Mover mesa");
            Undo.RecordObject(mesaDer, "Mover mesa");
            mesaIzq.position += dirIzq * 1.5f;
            mesaDer.position += dirDer * 1.5f;

            Debug.Log("Mesas acomodadas correctamente. Se eliminó la del medio y se acercaron las de los costados.");
        }

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }
}
