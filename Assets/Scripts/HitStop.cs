using UnityEngine;
using System.Collections;

public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }
    void Awake() => Instance = this;

    // ✅ Основной метод — вызывай при попадании по врагу
    public void Stop(float duration = 0.05f, float timeScale = 0.05f)
    {
        StopAllCoroutines();
        StartCoroutine(DoHitStop(duration, timeScale));
    }

    IEnumerator DoHitStop(float duration, float timeScale)
    {
        Time.timeScale = timeScale;
        Time.fixedDeltaTime = 0.02f * timeScale;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}