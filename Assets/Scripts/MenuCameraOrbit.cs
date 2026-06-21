using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MenuCameraOrbit : MonoBehaviour
{
    [Header("Точка вращения")]
    public Transform target;
    public Vector3 targetOffset = Vector3.zero;

    [Header("Параметры орбиты")]
    public float orbitSpeed = 4f;
    public float orbitRadius = 0.1f;   // ✅ вплотную к костру
    public float orbitHeight = 0.4f;   // ✅ низко у костра

    [Header("Покачивание камеры")]
    public float bobAmount = 0.15f;
    public float bobSpeed = 0.4f;

    [Header("FOV пульсация")]
    public float fovBase = 60f;
    public float fovAmount = 1.5f;
    public float fovSpeed = 0.3f;

    [Header("Fade In")]
    public float fadeDuration = 3f;

    private float angle = 0f;
    private Camera cam;
    private GameObject fadeOverlay;
    private Image fadeImage;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null) cam.fieldOfView = fovBase;
        CreateFadeOverlay();
        StartCoroutine(FadeIn());
    }

    void CreateFadeOverlay()
    {
        // Canvas поверх всего
        var canvasGO = new GameObject("FadeCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>();

        fadeOverlay = new GameObject("FadeOverlay");
        fadeOverlay.transform.SetParent(canvasGO.transform, false);
        var rt = fadeOverlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        fadeImage = fadeOverlay.AddComponent<Image>();
        fadeImage.color = Color.black;
    }

    IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            if (fadeImage != null)
                fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        if (fadeImage != null)
            fadeImage.color = new Color(0, 0, 0, 0);
    }

    void Update()
    {
        angle += orbitSpeed * Time.deltaTime;

        Vector3 center = target != null
            ? target.position + targetOffset
            : targetOffset;

        float rad = angle * Mathf.Deg2Rad;
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;

        Vector3 pos = center + new Vector3(
            Mathf.Sin(rad) * orbitRadius,
            orbitHeight + bob,
            Mathf.Cos(rad) * orbitRadius
        );

        transform.position = pos;
        transform.LookAt(center + Vector3.up * 0.5f);

        if (cam != null)
            cam.fieldOfView = fovBase + Mathf.Sin(Time.time * fovSpeed) * fovAmount;
    }
}