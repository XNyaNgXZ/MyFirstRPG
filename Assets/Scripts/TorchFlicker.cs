using UnityEngine;

public class TorchFlicker : MonoBehaviour
{
    public float minIntensity = 0.8f;
    public float maxIntensity = 1.4f;
    public float flickerSpeed = 8f;

    private Light torchLight;
    private float seed;

    void Start()
    {
        torchLight = GetComponent<Light>();
        seed = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (torchLight == null) return;
        float noise = Mathf.PerlinNoise(seed + Time.time * flickerSpeed, 0f);
        torchLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
        // Ќебольшое смещение позиции дл€ живости
        transform.localPosition = new Vector3(
            Mathf.Sin(Time.time * 7f + seed) * 0.02f,
            0f,
            Mathf.Cos(Time.time * 5f + seed) * 0.02f
        );
    }
}