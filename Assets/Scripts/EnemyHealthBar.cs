using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnemyHealthBar : MonoBehaviour
{
    public float heightOffset = 0.8f;
    public float hideDelay = 3f;

    private GameObject barRoot;
    private Image fillImage;
    private Coroutine hideCoroutine;
    private int cachedMax;

    // Ищем камеру надёжнее чем Camera.main
    private Camera GetCam() =>
        Camera.main ?? FindAnyObjectByType<Camera>();

    void Awake()
    {
        // Создаём сразу в Awake — не ждём Start
        CreateBar();
        barRoot.SetActive(false);
    }

    void CreateBar()
    {
        barRoot = new GameObject("EnemyHealthBar");
        barRoot.transform.SetParent(transform, false);
        barRoot.transform.localPosition = Vector3.up * heightOffset;
        barRoot.transform.localScale = Vector3.one * 0.008f;

        Canvas canvas = barRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = GetCam();
        canvas.sortingOrder = 20;

        RectTransform cr = barRoot.GetComponent<RectTransform>();
        cr.sizeDelta = new Vector2(150f, 11f);

        // Рамка
        GameObject border = new GameObject("Border");
        border.transform.SetParent(barRoot.transform, false);
        RectTransform br = border.AddComponent<RectTransform>();
        br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one;
        br.offsetMin = br.offsetMax = Vector2.zero;
        border.AddComponent<Image>().color = new Color(0f, 0f, 0f, 1f);

        // Фон
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(barRoot.transform, false);
        RectTransform bgr = bg.AddComponent<RectTransform>();
        bgr.anchorMin = Vector2.zero; bgr.anchorMax = Vector2.one;
        bgr.offsetMin = new Vector2(2f, 2f);
        bgr.offsetMax = new Vector2(-2f, -2f);
        bg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // Заливка
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(barRoot.transform, false);
        RectTransform fr = fill.AddComponent<RectTransform>();
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
        fr.offsetMin = new Vector2(2f, 2f);
        fr.offsetMax = new Vector2(-2f, -2f);

        fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.15f, 0.85f, 0.2f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 1f;

        Debug.Log($"[EnemyHealthBar] Создан на {gameObject.name}");
    }

    void LateUpdate()
    {
        if (barRoot == null || !barRoot.activeSelf) return;
        Camera cam = GetCam();
        if (cam != null)
            barRoot.transform.LookAt(barRoot.transform.position + cam.transform.forward);
    }

    // Показать бар (при преследовании)
    public void ShowBar(int current, int max)
    {
        if (barRoot == null) return;
        cachedMax = max;
        barRoot.SetActive(true);
        SetFill(current, max);

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    // Обновить при уроне
    public void UpdateBar(int current, int max)
    {
        if (barRoot == null) return;
        cachedMax = max;
        barRoot.SetActive(true);
        SetFill(current, max);

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    void SetFill(int current, int max)
    {
        if (fillImage == null) return;
        float pct = Mathf.Clamp01((float)current / max);
        fillImage.fillAmount = pct;
        fillImage.color = pct > 0.5f
            ? Color.Lerp(new Color(1f, 0.85f, 0f),
                         new Color(0.15f, 0.85f, 0.2f), (pct - 0.5f) * 2f)
            : Color.Lerp(new Color(0.85f, 0.1f, 0.1f),
                         new Color(1f, 0.85f, 0f), pct * 2f);
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);
        if (barRoot != null) barRoot.SetActive(false);
    }
}