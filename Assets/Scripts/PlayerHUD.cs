using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHUD : MonoBehaviour
{
    public static PlayerHUD Instance { get; private set; }

    [Header("HP Bar")]
    public Color hpColor = new Color(0.85f, 0.10f, 0.10f);
    public Color hpBgColor = new Color(0.08f, 0.02f, 0.02f);
    public float hpOffsetX = 20f;
    public float hpOffsetY = 20f;

    [Header("Stamina Bar")]
    public Color staminaColor = new Color(0.90f, 0.75f, 0.10f);
    public Color staminaBgColor = new Color(0.10f, 0.08f, 0.01f);
    public float staminaOffsetX = 0f;
    public float staminaOffsetY = 40f;

    [Header("Charge Bar — позиция")]
    public float chargeOffsetX = 0f;
    public float chargeOffsetY = 60f;

    [Header("Charge Bar — размер")]
    public float chargeBarWidth = 200f;
    public float chargeBarHeight = 14f;

    [Header("Charge Bar — цвета")]
    public Color chargeColor = new Color(0.3f, 0.7f, 1f);
    public Color chargeFullColor = new Color(1f, 0.85f, 0.1f);
    public Color chargeBgColor = new Color(0.02f, 0.05f, 0.10f);

    [Header("Charge Bar — скорость")]
    public float chargeFadeInSpeed = 8f;   // скорость появления
    public float chargeFadeOutSpeed = 4f;   // скорость исчезания

    [Header("Индикатор тревоги")]
    public Color alertNone = new Color(0.85f, 0.85f, 0.85f);
    public Color alertSearch = new Color(0.95f, 0.80f, 0.10f);
    public Color alertChase = new Color(0.90f, 0.10f, 0.10f);
    public float alertSize = 18f;
    public float alertOffsetX = 240f;
    public float alertOffsetY = 20f;

    [Header("Размеры HP/Stamina баров")]
    public float barWidth = 200f;
    public float barHeight = 14f;

    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private HandController handController;
    private GameObject hudCanvas;
    private RectTransform hpFill;
    private RectTransform staminaFill;
    private CanvasGroup staminaGroup;
    private RectTransform chargeFill;
    private Image chargeFillImage;
    private CanvasGroup chargeGroup;
    private Image alertIcon;

    private float hideTimer = 0f;
    private bool staminaWasFull = true;

    float StaminaHideDelay => playerMovement != null ? playerMovement.staminaHideDelay : 3f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerMovement = GetComponent<PlayerMovement>();
        handController = GetComponent<HandController>() ?? FindAnyObjectByType<HandController>();
        BuildHUD();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (hudCanvas != null) Destroy(hudCanvas);
        StartCoroutine(RebuildNextFrame());
    }

    System.Collections.IEnumerator RebuildNextFrame()
    {
        yield return null;
        yield return null;
        playerHealth = FindAnyObjectByType<PlayerHealth>();
        playerMovement = FindAnyObjectByType<PlayerMovement>();
        handController = FindAnyObjectByType<HandController>();
        hideTimer = 0f;
        staminaWasFull = true;
        BuildHUD();
    }

    void BuildHUD()
    {
        hudCanvas = new GameObject("PlayerHUDCanvas");
        var cv = hudCanvas.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 8;
        var sc = hudCanvas.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        hudCanvas.AddComponent<GraphicRaycaster>();

        // ── HP ────────────────────────────────────────────────────────────
        MakeRect("HPBg", hudCanvas.transform,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(barWidth, barHeight),
            new Vector2(hpOffsetX, -hpOffsetY), hpBgColor);

        var hpGO = MakeRect("HPFill", hudCanvas.transform,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(barWidth, barHeight),
            new Vector2(hpOffsetX, -hpOffsetY), hpColor);
        hpFill = hpGO.GetComponent<RectTransform>();

        // ── Alert ─────────────────────────────────────────────────────────
        var alertGO = new GameObject("AlertIcon");
        alertGO.transform.SetParent(hudCanvas.transform, false);
        var alertRT = alertGO.AddComponent<RectTransform>();
        alertRT.anchorMin = new Vector2(0, 1);
        alertRT.anchorMax = new Vector2(0, 1);
        alertRT.pivot = new Vector2(0, 1);
        alertRT.sizeDelta = new Vector2(alertSize, alertSize);
        alertRT.anchoredPosition = new Vector2(alertOffsetX, -alertOffsetY);
        alertIcon = alertGO.AddComponent<Image>();
        alertIcon.color = alertNone;

        // ── Stamina ───────────────────────────────────────────────────────
        var staminaRoot = new GameObject("StaminaRoot");
        staminaRoot.transform.SetParent(hudCanvas.transform, false);
        staminaGroup = staminaRoot.AddComponent<CanvasGroup>();
        staminaGroup.alpha = 0f;

        MakeRect("StaminaBg", staminaRoot.transform,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(barWidth, barHeight),
            new Vector2(staminaOffsetX, staminaOffsetY), staminaBgColor);

        var sf = MakeRect("StaminaFill", staminaRoot.transform,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 0),
            new Vector2(barWidth, barHeight),
            new Vector2(staminaOffsetX - barWidth * 0.5f, staminaOffsetY), staminaColor);
        staminaFill = sf.GetComponent<RectTransform>();

        // ── Charge Bar ────────────────────────────────────────────────────
        var chargeRoot = new GameObject("ChargeRoot");
        chargeRoot.transform.SetParent(hudCanvas.transform, false);
        chargeGroup = chargeRoot.AddComponent<CanvasGroup>();
        chargeGroup.alpha = 0f;

        MakeRect("ChargeBg", chargeRoot.transform,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(chargeBarWidth, chargeBarHeight),
            new Vector2(chargeOffsetX, chargeOffsetY), chargeBgColor);

        var cf = MakeRect("ChargeFill", chargeRoot.transform,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 0),
            new Vector2(chargeBarWidth, chargeBarHeight),
            new Vector2(chargeOffsetX - chargeBarWidth * 0.5f, chargeOffsetY), chargeColor);
        chargeFill = cf.GetComponent<RectTransform>();
        chargeFillImage = cf;
    }

    void Update()
    {
        UpdateHP();
        UpdateStamina();
        UpdateCharge();
        UpdateAlert();

        // Переназначаем ссылку если потерялась
        if (handController == null)
            handController = FindAnyObjectByType<HandController>();
    }

    void UpdateHP()
    {
        if (playerHealth == null || hpFill == null) return;
        float pct = playerHealth.maxHealth > 0
            ? Mathf.Clamp01((float)playerHealth.currentHealth / playerHealth.maxHealth) : 0f;
        hpFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barWidth * pct);
    }

    void UpdateStamina()
    {
        if (playerMovement == null || staminaFill == null || staminaGroup == null) return;
        float pct = playerMovement.maxStamina > 0
            ? Mathf.Clamp01(playerMovement.currentStamina / playerMovement.maxStamina) : 1f;

        staminaFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, barWidth * pct);

        bool isFull = pct >= 1f;
        if (isFull && !staminaWasFull) hideTimer = StaminaHideDelay;
        staminaWasFull = isFull;

        bool shouldShow = playerMovement.isSprinting || !isFull;
        if (hideTimer > 0f) { hideTimer -= Time.deltaTime; shouldShow = true; }
        staminaGroup.alpha = Mathf.MoveTowards(
            staminaGroup.alpha, shouldShow ? 1f : 0f, Time.deltaTime * 4f);
    }

    void UpdateCharge()
    {
        if (handController == null || chargeFill == null || chargeGroup == null) return;

        float pct = handController.ChargePercent;

        // ✅ Показываем бар только когда заряд уже начался (не сразу при клике)
        bool shouldShow = handController.IsChargeVisible;

        chargeFill.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal, chargeBarWidth * pct);

        // Цвет: синий → жёлтый при полном заряде
        if (chargeFillImage != null)
            chargeFillImage.color = Color.Lerp(chargeColor, chargeFullColor, pct);

        // Разная скорость появления и исчезания
        float speed = shouldShow ? chargeFadeInSpeed : chargeFadeOutSpeed;
        float targetAlpha = shouldShow ? 1f : 0f;
        chargeGroup.alpha = Mathf.MoveTowards(
            chargeGroup.alpha, targetAlpha, Time.deltaTime * speed);
    }

    void UpdateAlert()
    {
        if (alertIcon == null) return;
        bool anyChase = false, anySearch = false;
        foreach (var enemy in FindObjectsByType<EnemyNav>())
        {
            var s = enemy.GetCurrentState();
            if (s == EnemyNav.AlertLevel.Chase) { anyChase = true; break; }
            if (s == EnemyNav.AlertLevel.Search) anySearch = true;
        }
        Color target = anyChase ? alertChase : anySearch ? alertSearch : alertNone;
        alertIcon.color = Color.Lerp(alertIcon.color, target, Time.deltaTime * 6f);
    }

    Image MakeRect(string name, Transform parent,
                   Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                   Vector2 size, Vector2 pos, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }
}