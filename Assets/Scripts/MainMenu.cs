using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("Название игровой сцены")]
    public string gameSceneName = "SampleScene";

    [Header("Fade настройки")]
    public float fadeDuration = 1.5f;

    private AudioSource audioSource;
    private bool isLoading = false;

    void Start()
    {
        audioSource = FindAnyObjectByType<AudioSource>();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void PlayGame()
    {
        if (isLoading) return;
        isLoading = true;
        StartCoroutine(FadeAndLoad());
    }

    IEnumerator FadeAndLoad()
    {
        // ── Создаём чёрный оверлей ────────────────────────────────────
        var canvasGO = new GameObject("FadeOut");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGO.AddComponent<CanvasScaler>();

        var imgGO = new GameObject("Overlay");
        imgGO.transform.SetParent(canvasGO.transform, false);
        var rt = imgGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = imgGO.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);

        // ── Fade out — музыка тихнет, экран темнеет ───────────────────
        float elapsed = 0f;
        float startVolume = audioSource != null ? audioSource.volume : 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            img.color = new Color(0, 0, 0, t);
            if (audioSource != null)
                audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        img.color = Color.black;

        // ── Загружаем сцену ───────────────────────────────────────────
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}