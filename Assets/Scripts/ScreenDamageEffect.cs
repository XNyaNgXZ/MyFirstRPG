using UnityEngine;
using UnityEngine.UI;
using System.Collections;


public class ScreenDamageEffect : MonoBehaviour // на Player 
{
    public static ScreenDamageEffect Instance {  get; private set; }

    [Range(0f, 1f)  ] public float flashAlpha = 0.4f;
    public float flashDuration = 0.3f;

    private Image flashImage;

    void Awake() => Instance = this;

    void Start()
    {
        // Создаём Canvas с красным оверлеем
        GameObject canvasGO = new GameObject("DamageCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        canvasGO.AddComponent<CanvasScaler>();

        GameObject imgGO = new GameObject("DamageFlash");
        imgGO.transform.SetParent(canvasGO.transform, false);
        RectTransform rt = imgGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        flashImage = imgGO.AddComponent<Image>();
        flashImage.color = new Color(1f, 0f, 0f, 0f); // прозрачный красный
        flashImage.raycastTarget = false;
    }

    // Вызывается из PlayerHealth при получении урона
    public void Flash()
    {
        StopAllCoroutines();
        StartCoroutine(DoFlash());
    }

    IEnumerator DoFlash()
    {
        // Появляемся
        float t = 0f;
        while (t < flashDuration * 0.3f)
        {
            flashImage.color = new Color(1f, 0f, 0f, Mathf.Lerp(0f, flashAlpha, t / (flashDuration * 0.3f)));
            t += Time.deltaTime;
            yield return null;
        }
        // Исчезаем
        t = 0f;
        while (t < flashDuration * 0.7f)
        {
            flashImage.color = new Color(1f, 0f, 0f, Mathf.Lerp(flashAlpha, 0f, t / (flashDuration * 0.7f)));
            t += Time.deltaTime;
            yield return null;
        }
        flashImage.color = new Color(1f, 0f, 0f, 0f);
    }

}
