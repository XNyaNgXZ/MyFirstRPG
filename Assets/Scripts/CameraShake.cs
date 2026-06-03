using UnityEngine;
using System.Collections;
public class CameraShake : MonoBehaviour // для MainCamera
{
    public static CameraShake Instance { get; private set; }
    void Awake() => Instance = this;

    public void Shake(float duration = 0.2f, float magnitude = 0.1f)
    {
        StartCoroutine(DoShake(duration, magnitude));
    }

    IEnumerator DoShake(float duration, float magnitude)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            transform.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
    }
}
