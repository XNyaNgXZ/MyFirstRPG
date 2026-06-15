using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHUD : MonoBehaviour
{
    public static PlayerHUD Instance { get; private set; }

    [Header("HP Bar")]
    public Color hpColor = new Color(0.85f, 0.10f, 0.10f);
    public Color hpBgColor = new Color(0.08f, 0.02f, 0.02f);
    public float hpOffsetX = 20f;
    public float hpOffsetY = 20f;
    public float hpBarWidth = 200f;
    public float hpBarHeight = 14f;

    [Header("Stamina Bar")]
    public Color staminaColor = new Color(0.90f, 0.75f, 0.10f);
    public Color staminaBgColor = new Color(0.10f, 0.08f, 0.01f);
    public float staminaOffsetX = 0f;
    public float staminaOffsetY = 40f;
    public float staminaBarWidth = 200f;
    public float staminaBarHeight = 14f;

    [Header("Иконки оружия")]
    public float weaponIconSize = 64f;
    public Color weaponIconBgColor = new Color(0.1f, 0.1f, 0.12f, 0.9f);
    public Color weaponIconBorderColor = new Color(0.4f, 0.4f, 0.5f, 0.8f);
    public Color weaponIconEmptyColor = new Color(0.15f, 0.15f, 0.18f, 0.0f);
    public Color weaponIconFilledColor = new Color(0.82f, 0.22f, 0.22f, 1f);
    public Color weaponIconShieldColor = new Color(0.22f, 0.48f, 0.85f, 1f);
    public int weaponLetterFontSize = 22;
    public int weaponLabelFontSize = 10;
    public float rightIconOffsetX = 30f;
    public float rightIconOffsetY = 30f;
    public float leftIconOffsetX = 30f;
    public float leftIconOffsetY = 30f;

    [Header("Индикатор тревоги")]
    public Color alertNone = new Color(0.85f, 0.85f, 0.85f);
    public Color alertSearch = new Color(0.95f, 0.80f, 0.10f);
    public Color alertChase = new Color(0.90f, 0.10f, 0.10f);
    public float alertSize = 18f;
    public float alertOffsetX = 240f;
    public float alertOffsetY = 20f;

    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private HandController handController;
    private Inventory inventory;
    private GameObject hudCanvas;

    private RectTransform hpFill;
    private RectTransform staminaFill;
    private CanvasGroup staminaGroup;

    private Image weaponIconRight;
    private Image weaponIconLeft;
    private Text weaponLetterRight;
    private Text weaponLetterLeft;

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
        inventory = GetComponent<Inventory>() ?? FindAnyObjectByType<Inventory>();
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

    IEnumerator RebuildNextFrame()
    {
        yield return null; yield return null;
        playerHealth = FindAnyObjectByType<PlayerHealth>();
        playerMovement = FindAnyObjectByType<PlayerMovement>();
        handController = FindAnyObjectByType<HandController>();
        inventory = FindAnyObjectByType<Inventory>();
        hideTimer = 0f; staminaWasFull = true;
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
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        sc.matchWidthOrHeight = 0.5f;

        hudCanvas.AddComponent<GraphicRaycaster>();

        // HP
        MakeBar("HP", hudCanvas.transform,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(hpOffsetX, -hpOffsetY),
            hpBarWidth, hpBarHeight, hpBgColor, hpColor,
            out hpFill, out _);

        // Alert
        var alertGO = new GameObject("AlertIcon");
        alertGO.transform.SetParent(hudCanvas.transform, false);
        var alertRT = alertGO.AddComponent<RectTransform>();
        alertRT.anchorMin = alertRT.anchorMax = new Vector2(0, 1);
        alertRT.pivot = new Vector2(0, 1);
        alertRT.sizeDelta = new Vector2(alertSize, alertSize);
        alertRT.anchoredPosition = new Vector2(alertOffsetX, -alertOffsetY);
        alertIcon = alertGO.AddComponent<Image>();
        alertIcon.color = alertNone;

        // Stamina
        var staminaRoot = new GameObject("StaminaRoot");
        staminaRoot.transform.SetParent(hudCanvas.transform, false);
        staminaGroup = staminaRoot.AddComponent<CanvasGroup>();
        staminaGroup.alpha = 0f;

        MakeBar("Stamina", staminaRoot.transform,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(staminaOffsetX, staminaOffsetY),
            staminaBarWidth, staminaBarHeight, staminaBgColor, staminaColor,
            out staminaFill, out _);

        // Weapon icons
        BuildWeaponIcons();
    }

    void BuildWeaponIcons()
    {
        // Правая рука
        GameObject rightRoot = new GameObject("WeaponIconRight");
        rightRoot.transform.SetParent(hudCanvas.transform, false);
        RectTransform rightRT = rightRoot.AddComponent<RectTransform>();
        rightRT.anchorMin = rightRT.anchorMax = new Vector2(1f, 0f);
        rightRT.pivot = new Vector2(1f, 0f);
        rightRT.sizeDelta = new Vector2(weaponIconSize, weaponIconSize);
        rightRT.anchoredPosition = new Vector2(-rightIconOffsetX, rightIconOffsetY);
        rightRoot.AddComponent<Image>().color = weaponIconBgColor;

        GameObject rBorder = new GameObject("Border");
        rBorder.transform.SetParent(rightRoot.transform, false);
        var rBorderRT = rBorder.AddComponent<RectTransform>();
        rBorderRT.anchorMin = Vector2.zero; rBorderRT.anchorMax = Vector2.one;
        rBorderRT.offsetMin = rBorderRT.offsetMax = Vector2.zero;
        rBorder.AddComponent<Image>().color = weaponIconBorderColor;

        GameObject rIcon = new GameObject("Icon");
        rIcon.transform.SetParent(rightRoot.transform, false);
        var rIconRT = rIcon.AddComponent<RectTransform>();
        rIconRT.anchorMin = new Vector2(0.08f, 0.08f);
        rIconRT.anchorMax = new Vector2(0.92f, 0.92f);
        rIconRT.offsetMin = rIconRT.offsetMax = Vector2.zero;
        weaponIconRight = rIcon.AddComponent<Image>();
        weaponIconRight.color = weaponIconEmptyColor;

        GameObject rLetterGO = new GameObject("Letter");
        rLetterGO.transform.SetParent(rightRoot.transform, false);
        var rLetterRT = rLetterGO.AddComponent<RectTransform>();
        rLetterRT.anchorMin = Vector2.zero; rLetterRT.anchorMax = Vector2.one;
        rLetterRT.offsetMin = rLetterRT.offsetMax = Vector2.zero;
        weaponLetterRight = rLetterGO.AddComponent<Text>();
        weaponLetterRight.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        weaponLetterRight.fontSize = weaponLetterFontSize;
        weaponLetterRight.fontStyle = FontStyle.Bold;
        weaponLetterRight.color = Color.white;
        weaponLetterRight.alignment = TextAnchor.MiddleCenter;

        AddText(rightRoot.transform, "ПР", weaponLabelFontSize,
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, -weaponLabelFontSize - 2),
            new Vector2(0, weaponLabelFontSize + 2));

        // Левая рука
        GameObject leftRoot = new GameObject("WeaponIconLeft");
        leftRoot.transform.SetParent(hudCanvas.transform, false);
        RectTransform leftRT = leftRoot.AddComponent<RectTransform>();
        leftRT.anchorMin = leftRT.anchorMax = new Vector2(0f, 0f);
        leftRT.pivot = new Vector2(0f, 0f);
        leftRT.sizeDelta = new Vector2(weaponIconSize, weaponIconSize);
        leftRT.anchoredPosition = new Vector2(leftIconOffsetX, leftIconOffsetY);
        leftRoot.AddComponent<Image>().color = weaponIconBgColor;

        GameObject lBorder = new GameObject("Border");
        lBorder.transform.SetParent(leftRoot.transform, false);
        var lBorderRT = lBorder.AddComponent<RectTransform>();
        lBorderRT.anchorMin = Vector2.zero; lBorderRT.anchorMax = Vector2.one;
        lBorderRT.offsetMin = lBorderRT.offsetMax = Vector2.zero;
        lBorder.AddComponent<Image>().color = weaponIconBorderColor;

        GameObject lIcon = new GameObject("Icon");
        lIcon.transform.SetParent(leftRoot.transform, false);
        var lIconRT = lIcon.AddComponent<RectTransform>();
        lIconRT.anchorMin = new Vector2(0.08f, 0.08f);
        lIconRT.anchorMax = new Vector2(0.92f, 0.92f);
        lIconRT.offsetMin = lIconRT.offsetMax = Vector2.zero;
        weaponIconLeft = lIcon.AddComponent<Image>();
        weaponIconLeft.color = weaponIconEmptyColor;

        GameObject lLetterGO = new GameObject("Letter");
        lLetterGO.transform.SetParent(leftRoot.transform, false);
        var lLetterRT = lLetterGO.AddComponent<RectTransform>();
        lLetterRT.anchorMin = Vector2.zero; lLetterRT.anchorMax = Vector2.one;
        lLetterRT.offsetMin = lLetterRT.offsetMax = Vector2.zero;
        weaponLetterLeft = lLetterGO.AddComponent<Text>();
        weaponLetterLeft.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        weaponLetterLeft.fontSize = weaponLetterFontSize;
        weaponLetterLeft.fontStyle = FontStyle.Bold;
        weaponLetterLeft.color = Color.white;
        weaponLetterLeft.alignment = TextAnchor.MiddleCenter;

        AddText(leftRoot.transform, "ЛЕВ", weaponLabelFontSize,
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, -weaponLabelFontSize - 2),
            new Vector2(0, weaponLabelFontSize + 2));
    }

    void Update()
    {
        UpdateHP();
        UpdateStamina();
        UpdateWeaponIcons();
        UpdateAlert();

        if (handController == null) handController = FindAnyObjectByType<HandController>();
        if (inventory == null) inventory = FindAnyObjectByType<Inventory>();
    }

    void UpdateHP()
    {
        if (playerHealth == null || hpFill == null) return;
        float pct = Mathf.Clamp01((float)playerHealth.currentHealth / playerHealth.maxHealth);
        hpFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, hpBarWidth * pct);
    }

    void UpdateStamina()
    {
        if (playerMovement == null || staminaFill == null || staminaGroup == null) return;
        float pct = Mathf.Clamp01(playerMovement.currentStamina / playerMovement.maxStamina);
        staminaFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, staminaBarWidth * pct);

        bool isFull = pct >= 1f;
        if (isFull && !staminaWasFull) hideTimer = StaminaHideDelay;
        staminaWasFull = isFull;

        bool shouldShow = playerMovement.isSprinting || !isFull;
        if (hideTimer > 0f) { hideTimer -= Time.deltaTime; shouldShow = true; }
        staminaGroup.alpha = Mathf.MoveTowards(staminaGroup.alpha, shouldShow ? 1f : 0f, Time.deltaTime * 4f);
    }

    void UpdateWeaponIcons()
    {
        if (inventory == null) return;

        Item rightWeapon = inventory.GetEquippedItem("Weapon");
        if (weaponIconRight != null)
        {
            weaponIconRight.color = rightWeapon != null ? weaponIconFilledColor : weaponIconEmptyColor;
            if (weaponLetterRight != null)
                weaponLetterRight.text = rightWeapon != null && !string.IsNullOrEmpty(rightWeapon.itemName)
                    ? rightWeapon.itemName[0].ToString() : "";
        }

        Item leftItem = inventory.GetEquippedItem("WeaponLeft");
        if (weaponIconLeft != null)
        {
            if (leftItem != null)
            {
                weaponIconLeft.color = leftItem.itemType == "Shield"
                    ? weaponIconShieldColor : weaponIconFilledColor;
                if (weaponLetterLeft != null)
                    weaponLetterLeft.text = !string.IsNullOrEmpty(leftItem.itemName)
                        ? leftItem.itemName[0].ToString() : "";
            }
            else
            {
                weaponIconLeft.color = weaponIconEmptyColor;
                if (weaponLetterLeft != null) weaponLetterLeft.text = "";
            }
        }
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
        alertIcon.color = Color.Lerp(alertIcon.color,
            anyChase ? alertChase : anySearch ? alertSearch : alertNone,
            Time.deltaTime * 6f);
    }

    void MakeBar(string name, Transform parent,
                 Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos,
                 float width, float height, Color bgColor, Color fillColor,
                 out RectTransform fillRT, out CanvasGroup group)
    {
        group = null;

        var bg = new GameObject(name + "Bg");
        bg.transform.SetParent(parent, false);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = anchorMin; bgRT.anchorMax = anchorMax; bgRT.pivot = pivot;
        bgRT.sizeDelta = new Vector2(width, height); bgRT.anchoredPosition = pos;
        bg.AddComponent<Image>().color = bgColor;

        var fill = new GameObject(name + "Fill");
        fill.transform.SetParent(parent, false);
        var fRT = fill.AddComponent<RectTransform>();
        fRT.anchorMin = anchorMin; fRT.anchorMax = anchorMax;
        fRT.pivot = new Vector2(anchorMin.x == 0.5f ? 0 : pivot.x, pivot.y);
        fRT.sizeDelta = new Vector2(width, height);
        fRT.anchoredPosition = anchorMin.x == 0.5f
            ? new Vector2(pos.x - width * 0.5f, pos.y)
            : pos;
        fill.AddComponent<Image>().color = fillColor;
        fillRT = fRT;
    }

    void AddText(Transform parent, string text, int fontSize,
                 Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Label_" + text);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.color = new Color(0.6f, 0.6f, 0.7f);
        t.alignment = TextAnchor.MiddleCenter;
    }
}