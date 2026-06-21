using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    public static bool IsOpen { get; private set; } = false;
    public static bool IsPaused { get; private set; } = false;
    public static float LastClosedByClickTime = -10f; // ✅ время последнего закрытия кликом

    private GameObject pauseCanvasGO;
    private GameObject tabBarGO;
    private GameObject[] panels = new GameObject[3];
    private Image[] tabBgs = new Image[3];
    private TextMeshProUGUI[] tabTxts = new TextMeshProUGUI[3];
    private int currentTab = -1;
    private int rememberedTab = -1; // ✅ запоминаем вкладку при закрытии

    private TextMeshProUGUI statHP, statMana, statStam, statDmg, statDef;
    private TextMeshProUGUI slotRight, slotLeft, slotHelmet, slotChest, slotLegs, slotBoots;
    private GameObject confirmGO;

    // Цвета — точно как в инвентаре
    static readonly Color BG = new Color(0.13f, 0.13f, 0.16f, 1f);
    static readonly Color SLOT = new Color(0.20f, 0.20f, 0.24f, 1f);
    static readonly Color ACTIVE = new Color(0.28f, 0.28f, 0.36f, 1f);
    static readonly Color LINE = new Color(0.30f, 0.30f, 0.38f, 1f);
    static readonly Color TXT = new Color(0.65f, 0.65f, 0.75f, 1f);
    static readonly Color TXT_VAL = new Color(0.90f, 0.90f, 0.95f, 1f);

    void Start()
    {
        IsOpen = IsPaused = false;
        BuildUI();
        tabBarGO.SetActive(false);
        foreach (var p in panels) if (p) p.SetActive(false);
    }

    void Update()
    {
        // ✅ Курсор управляется только здесь
        bool shouldShowCursor = IsOpen || InventoryUICode.IsOpen || EquipmentUI.IsOpen;
        if (shouldShowCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // ✅ Единая обработка Escape — закрывает ВСЁ сразу
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsOpen || InventoryUICode.IsOpen || EquipmentUI.IsOpen) Close();
            else Open();
        }

        // ✅ Клик ЛКМ/ПКМ по пустому месту (не по UI) — закрыть меню и вернуться в игру
        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) &&
            (IsOpen || InventoryUICode.IsOpen || EquipmentUI.IsOpen))
        {
            bool overUI = EventSystem.current != null &&
                          EventSystem.current.IsPointerOverGameObject();
            if (!overUI) { Close(); LastClosedByClickTime = Time.time; }
        }

        // ✅ Навигация по вкладкам ТОЛЬКО стрелками (не WASD)
        if (IsOpen)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                int t = currentTab < 0 ? 0 : Mathf.Min(currentTab + 1, 2);
                if (t != currentTab) SwitchTab(t);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                int t = currentTab < 0 ? 0 : Mathf.Max(currentTab - 1, 0);
                if (t != currentTab) SwitchTab(t);
            }
        }

        if (IsOpen && currentTab == 1) RefreshStats();
    }

    void OnDestroy()
    {
        if (pauseCanvasGO) Destroy(pauseCanvasGO);
        IsOpen = IsPaused = false;
    }

    void Open()
    {
        IsOpen = IsPaused = true;
        tabBarGO.SetActive(true);
        foreach (var p in panels) if (p) p.SetActive(false);
        currentTab = -1;
        UpdateTabVisuals();
        // ✅ Восстанавливаем последнюю открытую вкладку
        if (rememberedTab >= 0) SwitchTab(rememberedTab);
    }

    void Close()
    {
        IsOpen = IsPaused = false;
        rememberedTab = currentTab; // ✅ запоминаем что было открыто
        tabBarGO.SetActive(false);
        foreach (var p in panels) if (p) p.SetActive(false);
        if (confirmGO) confirmGO.SetActive(false);
        currentTab = -1;
        var invUI = FindAnyObjectByType<InventoryUICode>();
        var eqUI = FindAnyObjectByType<EquipmentUI>();
        if (invUI != null) invUI.SetOpen(false);
        if (eqUI != null) eqUI.SetOpen(false);
        // Курсором управляет только Update
    }

    void SwitchTab(int idx)
    {
        // Закрываем инвентарь/снаряжение при смене вкладки
        if (idx != 0)
        {
            var invUI = FindAnyObjectByType<InventoryUICode>();
            var eqUI = FindAnyObjectByType<EquipmentUI>();
            if (invUI != null) invUI.SetOpen(false);
            if (eqUI != null) eqUI.SetOpen(false);
        }

        if (currentTab == idx)
        {
            panels[idx].SetActive(false);
            currentTab = -1;
            // При закрытии вкладки 0 — закрыть окна, курсор оставить
            if (idx == 0)
            {
                var invUI = FindAnyObjectByType<InventoryUICode>();
                var eqUI = FindAnyObjectByType<EquipmentUI>();
                if (invUI != null) invUI.SetOpen(false);
                if (eqUI != null) eqUI.SetOpen(false);
                // Курсором управляет Update
            }
        }
        else
        {
            currentTab = idx;
            for (int i = 0; i < 3; i++) panels[i].SetActive(i == idx);
            if (confirmGO) confirmGO.SetActive(false);
            if (idx == 0)
            {
                // ✅ Сразу открываем оба окна
                var invUI = FindAnyObjectByType<InventoryUICode>();
                var eqUI = FindAnyObjectByType<EquipmentUI>();
                if (invUI != null) invUI.SetOpen(true);
                if (eqUI != null) eqUI.SetOpen(true);
            }
            if (idx == 1) RefreshStats();
        }
        UpdateTabVisuals();
    }

    void UpdateTabVisuals()
    {
        for (int i = 0; i < 3; i++)
        {
            bool active = i == currentTab;
            if (tabBgs[i]) tabBgs[i].color = active ? ACTIVE : BG;
            if (tabTxts[i]) tabTxts[i].color = TXT;
        }
    }

    // ─── Live data ────────────────────────────────────────────────────
    void RefreshStats()
    {
        var ph = FindAnyObjectByType<PlayerHealth>();
        var pm = FindAnyObjectByType<PlayerMana>();
        var pmo = FindAnyObjectByType<PlayerMovement>();
        var inv = FindAnyObjectByType<Inventory>();
        var hc = FindAnyObjectByType<HandController>();

        if (statHP != null && ph != null) statHP.text = $"{ph.currentHealth} / {ph.maxHealth}";
        if (statMana != null && pm != null) statMana.text = $"{Mathf.RoundToInt(pm.currentMana)} / {Mathf.RoundToInt(pm.maxMana)}";
        if (statStam != null && pmo != null) statStam.text = $"{Mathf.RoundToInt(pmo.currentStamina)} / {Mathf.RoundToInt(pmo.maxStamina)}";

        int dmg = 0;
        if (inv != null) { var w = inv.GetEquippedItem("Weapon"); dmg = w != null ? w.value : (hc != null ? hc.unarmedDamage : 0); }
        if (statDmg != null) statDmg.text = dmg.ToString();
        if (statDef != null) statDef.text = (inv != null ? inv.GetTotalDefense() : 0).ToString();
    }

    void RefreshEquipSlots()
    {
        var inv = FindAnyObjectByType<Inventory>();
        if (inv == null) return;
        if (slotRight != null) { var x = inv.GetEquippedItem("Weapon"); slotRight.text = x != null ? x.itemName : "—"; }
        if (slotLeft != null) { var x = inv.GetEquippedItem("WeaponLeft"); slotLeft.text = x != null ? x.itemName : "—"; }
        if (slotHelmet != null) { var x = inv.GetEquippedItem("Helmet"); slotHelmet.text = x != null ? x.itemName : "—"; }
        if (slotChest != null) { var x = inv.GetEquippedItem("Chest"); slotChest.text = x != null ? x.itemName : "—"; }
        if (slotLegs != null) { var x = inv.GetEquippedItem("Legs"); slotLegs.text = x != null ? x.itemName : "—"; }
        if (slotBoots != null) { var x = inv.GetEquippedItem("Boots"); slotBoots.text = x != null ? x.itemName : "—"; }
    }

    // ─── Build UI ─────────────────────────────────────────────────────
    void BuildUI()
    {
        pauseCanvasGO = new GameObject("PauseCanvas");
        var cv = pauseCanvasGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 50;
        var cs = pauseCanvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.5f;
        pauseCanvasGO.AddComponent<GraphicRaycaster>();

        BuildTabBar();
        BuildPanel0();
        BuildPanel1();
        BuildPanel2();
    }

    void BuildTabBar()
    {
        tabBarGO = new GameObject("TabBar");
        tabBarGO.transform.SetParent(pauseCanvasGO.transform, false);
        var r = tabBarGO.AddComponent<RectTransform>();
        // ✅ Правый верхний угол
        r.anchorMin = new Vector2(1, 1); r.anchorMax = new Vector2(1, 1);
        r.pivot = new Vector2(1, 1);
        r.sizeDelta = new Vector2(380, 64);
        r.anchoredPosition = new Vector2(-20, -20);
        tabBarGO.AddComponent<Image>().color = BG;

        // Нижняя линия — серая как в инвентаре
        MakeRect(tabBarGO.transform, new Vector2(0, 0), new Vector2(1, 0),
                 new Vector2(0, 0), new Vector2(0, 1), LINE);

        string[] labels = { "СНАРЯЖЕНИЕ", "ПЕРСОНАЖ", "ВЫХОД" };

        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            var tab = new GameObject($"T{i}");
            tab.transform.SetParent(tabBarGO.transform, false);
            var tr = tab.AddComponent<RectTransform>();
            tr.anchorMin = new Vector2(i / 3f, 0); tr.anchorMax = new Vector2((i + 1) / 3f, 1);
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            var bg = tab.AddComponent<Image>(); bg.color = BG;
            tabBgs[i] = bg;

            var lbl = MakeTMP($"Lb{i}", tab.transform, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f));
            lbl.text = labels[i]; lbl.fontSize = 11; lbl.fontStyle = FontStyles.Bold;
            lbl.characterSpacing = 1.5f; lbl.color = TXT;
            tabTxts[i] = lbl;

            if (i < 2) // разделитель
                MakeRect(tabBarGO.transform,
                    new Vector2((i + 1) / 3f, 0.15f), new Vector2((i + 1) / 3f, 0.85f),
                    Vector2.zero, new Vector2(1, 0), LINE);

            var btn = tab.AddComponent<Button>(); btn.targetGraphic = bg;
            var c = btn.colors;
            c.normalColor = BG; c.highlightedColor = SLOT; c.pressedColor = ACTIVE;
            btn.colors = c;
            // ✅ Отключаем авто-навигацию — WASD больше не выбирает вкладки
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.onClick.AddListener(() => SwitchTab(idx));
        }
    }

    void BuildPanel0()
    {
        // Нет панели — окна открываются сразу при клике на вкладку
        panels[0] = new GameObject("P0_Empty");
        panels[0].transform.SetParent(pauseCanvasGO.transform, false);
        panels[0].AddComponent<RectTransform>();
    }

    void BuildPanel1()
    {
        var p = MakePanel(1, 220);
        SectionLabel(p, "ПЕРСОНАЖ", 0.93f);

        string[] names = { "Здоровье", "Мана", "Выносливость", "Урон", "Защита" };
        float[] ys = { 0.78f, 0.63f, 0.48f, 0.33f, 0.18f };
        var vals = new TextMeshProUGUI[5];

        for (int i = 0; i < 5; i++)
        {
            if (i > 0)
                MakeRect(p, new Vector2(0.04f, ys[i] + 0.12f), new Vector2(0.96f, ys[i] + 0.122f),
                         Vector2.zero, Vector2.zero, LINE);

            var row = new GameObject($"R{i}"); row.transform.SetParent(p, false);
            var rr = row.AddComponent<RectTransform>();
            rr.anchorMin = new Vector2(0.04f, ys[i] - 0.05f);
            rr.anchorMax = new Vector2(0.96f, ys[i] + 0.1f);
            rr.offsetMin = rr.offsetMax = Vector2.zero;

            var lbl = MakeTMP($"N{i}", row.transform, new Vector2(0, 0), new Vector2(0.6f, 1));
            lbl.text = names[i]; lbl.fontSize = 11;
            lbl.alignment = TextAlignmentOptions.MidlineLeft; lbl.color = TXT;

            var val = MakeTMP($"V{i}", row.transform, new Vector2(0.6f, 0), new Vector2(1, 1));
            val.text = "—"; val.fontSize = 11; val.fontStyle = FontStyles.Bold;
            val.alignment = TextAlignmentOptions.MidlineRight; val.color = TXT_VAL;
            vals[i] = val;
        }

        statHP = vals[0]; statMana = vals[1]; statStam = vals[2];
        statDmg = vals[3]; statDef = vals[4];
    }

    void BuildPanel2()
    {
        var p = MakePanel(2, 160);
        SectionLabel(p, "МЕНЮ", 0.93f);

        MakeBtn(p, "Продолжить", new Vector2(0.03f, 0.6f), new Vector2(0.97f, 0.82f), Close);
        MakeBtn(p, "Главное меню", new Vector2(0.03f, 0.34f), new Vector2(0.97f, 0.56f),
            () => { if (confirmGO) confirmGO.SetActive(true); },
            new Color(0.22f, 0.07f, 0.1f, 1f));

        confirmGO = new GameObject("Confirm");
        confirmGO.transform.SetParent(panels[2].transform, false);
        var cr = confirmGO.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.03f, 0.03f); cr.anchorMax = new Vector2(0.97f, 0.3f);
        cr.offsetMin = cr.offsetMax = Vector2.zero;
        confirmGO.AddComponent<Image>().color = BG;
        confirmGO.SetActive(false);

        var ct = MakeTMP("CT", confirmGO.transform, new Vector2(0, 0.55f), new Vector2(1, 1));
        ct.text = "Выйти в главное меню?"; ct.fontSize = 11; ct.color = TXT;

        MakeBtn(confirmGO.transform, "Да",
            new Vector2(0.03f, 0.05f), new Vector2(0.47f, 0.5f), GoToMainMenu,
            new Color(0.25f, 0.07f, 0.1f, 1f));
        MakeBtn(confirmGO.transform, "Отмена",
            new Vector2(0.53f, 0.05f), new Vector2(0.97f, 0.5f),
            () => confirmGO.SetActive(false));
    }

    // ─── Helpers ──────────────────────────────────────────────────────
    Transform MakePanel(int idx, float height)
    {
        var go = new GameObject($"P{idx}");
        go.transform.SetParent(pauseCanvasGO.transform, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(1, 1); r.anchorMax = new Vector2(1, 1);
        r.pivot = new Vector2(1, 1);
        r.sizeDelta = new Vector2(380, height);
        r.anchoredPosition = new Vector2(-20, -78);
        go.AddComponent<Image>().color = BG;

        // Нижняя линия как разделитель
        MakeRect(go.transform, new Vector2(0.04f, 0), new Vector2(0.96f, 0),
                 new Vector2(0, 0), new Vector2(0, 1), LINE);

        panels[idx] = go;
        return go.transform;
    }

    void SectionLabel(Transform parent, string text, float anchorY)
    {
        var t = MakeTMP("SL", parent,
            new Vector2(0.04f, anchorY - 0.04f), new Vector2(0.96f, anchorY + 0.04f));
        t.text = text; t.fontSize = 12; t.fontStyle = FontStyles.Bold;
        t.characterSpacing = 3f; t.color = TXT;
    }

    void MakeBtn(Transform parent, string label,
        Vector2 amin, Vector2 amax, System.Action onClick, Color? bgCol = null)
    {
        var go = new GameObject(label); go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = amin; r.anchorMax = amax; r.offsetMin = r.offsetMax = Vector2.zero;
        var bg = go.AddComponent<Image>(); bg.color = bgCol ?? SLOT;

        var t = MakeTMP("T", go.transform, new Vector2(0.04f, 0), new Vector2(1, 1));
        t.text = label; t.fontSize = 11; t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.MidlineLeft; t.color = TXT;

        var btn = go.AddComponent<Button>(); btn.targetGraphic = bg;
        var c = btn.colors;
        c.normalColor = bgCol ?? SLOT; c.highlightedColor = ACTIVE; c.pressedColor = ACTIVE;
        btn.colors = c;
        btn.onClick.AddListener(() => onClick());
    }

    TextMeshProUGUI MakeTMP(string name, Transform parent, Vector2 amin, Vector2 amax)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = amin; r.anchorMax = amax; r.offsetMin = r.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.alignment = TextAlignmentOptions.Center; t.color = TXT; t.fontSize = 11;
        return t;
    }

    void MakeRect(Transform parent, Vector2 amin, Vector2 amax, Vector2 omin, Vector2 omax, Color color)
    {
        var go = new GameObject("R"); go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = amin; r.anchorMax = amax; r.offsetMin = omin; r.offsetMax = omax;
        go.AddComponent<Image>().color = color;
    }

    public void SetPaused(bool paused) { if (paused) Open(); else Close(); }
    public void Resume() => Close();
    public void GoToMainMenu()
    {
        IsOpen = IsPaused = false;
        Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene("MainMenu");
    }
}