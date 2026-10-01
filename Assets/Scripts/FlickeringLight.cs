using UnityEngine;

[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour
{
    [Tooltip("Intensidad mas baja que puede alcanzar el fuego")]
    public float minIntensity = 0.5f;
    [Tooltip("Intensidad mas alta (el brillo maximo)")]
    public float maxIntensity = 1.5f;
    [Tooltip("Que tan rapido titila la luz")]
    public float flickerSpeed = 3.0f;

    private Light fireLight;
    private float randomOffset;

    void Start()
    {
        // Obtenemos el componente Light del objeto
        fireLight = GetComponent<Light>();
        
        // Un numero aleatorio para que, si tienes varias luces, no titilen todas al mismo ritmo exacto
        randomOffset = Random.Range(0.0f, 100.0f);
    }

    void Update()
    {
        if (fireLight != null)
        {
            // PerlinNoise genera variaciones suaves (como el titileo natural) en lugar de saltos bruscos
            float noise = Mathf.PerlinNoise(randomOffset, Time.time * flickerSpeed);
            
            // Ajustamos la intensidad de la luz basándonos en ese ruido
            fireLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
        }
    }
}
