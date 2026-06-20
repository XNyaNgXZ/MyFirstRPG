using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryUICode : MonoBehaviour
{
    public static bool IsOpen { get; private set; } = false;
    public static InventoryUICode Instance { get; private set; }

    [Header("Материал для выброшенных предметов")]
    public Material retroMaterial;

    [Header("Звук выброса")]
    public AudioClip dropSound;
    [Range(0f, 1f)] public float dropVolume = 0.4f;

    [Header("Физика дропа")]
    public float dropFreezeDelay = 1.5f;
    public float throwForce = 5f;

    private Inventory inventory;
    private AudioSource audioSource;
    private GameObject inventoryCanvas;
    private GameObject inventoryPanel;
    private GameObject tabsContainer;
    private GameObject tooltip;
    private Text tooltipText;
    private RectTransform tooltipRect;
    private Text titleText;

    private const int COLS = 5;
    private const int ROWS = 5;
    private const int SLOTS = COLS * ROWS;
    private const float CELL = 68f;
    private const float SPACING = 6f;
    private const float PAD = 12f;
    private const float TITLE_H = 36f;
    private const float DRAG_THRESHOLD = 8f;
    private const float TAB_SIZE = 36f;
    private const float TAB_W = 36f;

    private GameObject[] slotGOs = new GameObject[SLOTS];
    private Image[] slotIcons = new Image[SLOTS];
    private Text[] slotLetters = new Text[SLOTS];
    private RectTransform[] slotRects = new RectTransform[SLOTS];

    private int currentTab = 0;
    private GameObject tabInventory;
    private GameObject tabSpells;
    private GameObject inventoryGrid;
    private GameObject spellsGrid;
    private System.Collections.Generic.List<GameObject> spellSlotGOs = new();

    private int pendingIndex = -1;
    private GameObject splitPanel = null;
    private int splitFromIndex = -1;
    private bool dragJustStarted = false; // ✅ защита от немедленного дропа
    private Vector2 mouseDownPos;
    private bool isDragging = false;
    private int dragFromIndex = -1;
    private Item draggedItem = null;
    private SpellDefinition draggedSpell = null;
    private GameObject dragGhost = null;
    private RectTransform ghostRect = null;

    void Awake() => Instance = this;

    void Start()
    {
        inventory = GetComponent<Inventory>() ?? FindAnyObjectByType<Inventory>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        BuildUI();
        inventoryPanel.SetActive(false);
        tabsContainer.SetActive(false);
    }

    void BuildUI()
    {
        if (inventoryCanvas != null) Destroy(inventoryCanvas);

        float gridW = COLS * CELL + (COLS - 1) * SPACING;
        float gridH = ROWS * CELL + (ROWS - 1) * SPACING;
        float panelW = gridW + PAD * 2;
        float panelH = gridH + PAD * 2 + TITLE_H;

        inventoryCanvas = new GameObject("InventoryCanvas");
        DontDestroyOnLoad(inventoryCanvas);
        var cv = inventoryCanvas.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 10;
        var sc = inventoryCanvas.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        sc.matchWidthOrHeight = 0.5f;
        inventoryCanvas.AddComponent<GraphicRaycaster>();

        // Вкладки
        tabsContainer = new GameObject("Tabs");
        tabsContainer.transform.SetParent(inventoryCanvas.transform, false);
        var tabsRT = tabsContainer.AddComponent<RectTransform>();
        tabsRT.anchorMin = new Vector2(1, 0); tabsRT.anchorMax = new Vector2(1, 0);
        tabsRT.pivot = new Vector2(1, 0);
        tabsRT.sizeDelta = new Vector2(TAB_W, TAB_SIZE * 2 + 4f);
        tabsRT.anchoredPosition = new Vector2(-(20 + panelW), 20 + panelH - TAB_SIZE * 2 - 4f);
        tabsContainer.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.11f, 0.9f);
        tabInventory = MakeTab(tabsContainer.transform, "И", 0);
        tabSpells = MakeTab(tabsContainer.transform, "З", 1);

        // Основная панель
        inventoryPanel = new GameObject("InventoryPanel");
        inventoryPanel.transform.SetParent(inventoryCanvas.transform, false);
        var pr = inventoryPanel.AddComponent<RectTransform>();
        pr.anchorMin = new Vector2(1, 0); pr.anchorMax = new Vector2(1, 0);
        pr.pivot = new Vector2(1, 0); pr.sizeDelta = new Vector2(panelW, panelH);
        pr.anchoredPosition = new Vector2(-20, 20);
        inventoryPanel.AddComponent<Image>().color = new Color(0.13f, 0.13f, 0.16f, 1f);

        var titleGO = new GameObject("Label"); titleGO.transform.SetParent(inventoryPanel.transform, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.sizeDelta = new Vector2(0, TITLE_H); titleRT.anchoredPosition = Vector2.zero;
        titleText = titleGO.AddComponent<Text>();
        titleText.text = "ИНВЕНТАРЬ  [TAB]";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 18; titleText.color = new Color(0.65f, 0.65f, 0.75f);
        titleText.alignment = TextAnchor.MiddleCenter;

        var ln = new GameObject("Line"); ln.transform.SetParent(inventoryPanel.transform, false);
        var lr = ln.AddComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 1); lr.anchorMax = new Vector2(1, 1);
        lr.pivot = new Vector2(0.5f, 1); lr.sizeDelta = new Vector2(-PAD * 2, 1);
        lr.anchoredPosition = new Vector2(0, -TITLE_H);
        ln.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.38f, 1f);

        // Грид инвентаря
        inventoryGrid = new GameObject("Grid");
        inventoryGrid.transform.SetParent(inventoryPanel.transform, false);
        var gr = inventoryGrid.AddComponent<RectTransform>();
        gr.anchorMin = new Vector2(0.5f, 0.5f); gr.anchorMax = new Vector2(0.5f, 0.5f);
        gr.pivot = new Vector2(0.5f, 0.5f); gr.sizeDelta = new Vector2(gridW, gridH);
        gr.anchoredPosition = new Vector2(0, -(TITLE_H / 2));
        for (int i = 0; i < SLOTS; i++) CreateSlot(i, inventoryGrid.transform);

        // Грид заклинаний
        spellsGrid = new GameObject("SpellsGrid");
        spellsGrid.transform.SetParent(inventoryPanel.transform, false);
        var sg = spellsGrid.AddComponent<RectTransform>();
        sg.anchorMin = new Vector2(0.5f, 0.5f); sg.anchorMax = new Vector2(0.5f, 0.5f);
        sg.pivot = new Vector2(0.5f, 0.5f); sg.sizeDelta = new Vector2(gridW, gridH);
        sg.anchoredPosition = new Vector2(0, -(TITLE_H / 2));
        spellsGrid.SetActive(false);

        BuildTooltip();
        RefreshTabVisuals();
    }

    GameObject MakeTab(Transform parent, string label, int tabIndex)
    {
        var go = new GameObject($"Tab_{label}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(0, TAB_SIZE);
        rt.anchoredPosition = new Vector2(0, -tabIndex * (TAB_SIZE + 2f));
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.18f, 0.22f, 1f);
        var textGO = new GameObject("Label"); textGO.transform.SetParent(go.transform, false);
        var tr = textGO.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        var t = textGO.AddComponent<Text>();
        t.text = label; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 14; t.fontStyle = FontStyle.Bold;
        t.color = new Color(0.6f, 0.6f, 0.7f); t.alignment = TextAnchor.MiddleCenter;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg; btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => SwitchTab(tabIndex));
        return go;
    }

    void SwitchTab(int tab)
    {
        currentTab = tab;
        inventoryGrid.SetActive(tab == 0);
        spellsGrid.SetActive(tab == 1);
        if (titleText != null)
            titleText.text = tab == 0 ? "ИНВЕНТАРЬ  [TAB]" : "ЗАКЛИНАНИЯ  [TAB]";
        if (tab == 1) BuildSpellSlots();
        RefreshTabVisuals();
    }

    void RefreshTabVisuals()
    {
        Color active = new Color(0.28f, 0.28f, 0.36f, 1f);
        Color inactive = new Color(0.18f, 0.18f, 0.22f, 1f);
        if (tabInventory != null) tabInventory.GetComponent<Image>().color = currentTab == 0 ? active : inactive;
        if (tabSpells != null) tabSpells.GetComponent<Image>().color = currentTab == 1 ? active : inactive;
    }

    public void RefreshSpellsIfOpen()
    {
        if (IsOpen && currentTab == 1) BuildSpellSlots();
    }

    void BuildSpellSlots()
    {
        foreach (var s in spellSlotGOs) if (s != null) Destroy(s);
        spellSlotGOs.Clear();
        if (inventory == null || inventory.knownSpells == null) return;
        var known = inventory.knownSpells;

        for (int i = 0; i < known.Count; i++)
        {
            int col = i % COLS, row = i / COLS;
            float x = col * (CELL + SPACING), y = -row * (CELL + SPACING);
            var spell = known[i];
            int ci = i;

            var go = new GameObject($"SpellSlot_{i}");
            go.transform.SetParent(spellsGrid.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(CELL, CELL);
            rt.anchoredPosition = new Vector2(x, y);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.15f, 0.22f, 1f);

            var bar = new GameObject("Bar"); bar.transform.SetParent(go.transform, false);
            var barRT = bar.AddComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0, 1); barRT.anchorMax = new Vector2(1, 1);
            barRT.pivot = new Vector2(0.5f, 1);
            barRT.offsetMin = new Vector2(0, -3); barRT.offsetMax = Vector2.zero;
            barRT.sizeDelta = new Vector2(0, 3);
            bar.AddComponent<Image>().color = spell.projectileColor;

            var nameGO = new GameObject("Name"); nameGO.transform.SetParent(go.transform, false);
            var nr = nameGO.AddComponent<RectTransform>();
            nr.anchorMin = new Vector2(0, 0.35f); nr.anchorMax = Vector2.one;
            nr.offsetMin = new Vector2(3, 0); nr.offsetMax = new Vector2(-3, -4);
            var nt = nameGO.AddComponent<Text>();
            nt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nt.fontSize = 9; nt.color = Color.white;
            nt.alignment = TextAnchor.MiddleCenter;
            nt.text = spell.spellName;

            var typeGO = new GameObject("Type"); typeGO.transform.SetParent(go.transform, false);
            var tyr = typeGO.AddComponent<RectTransform>();
            tyr.anchorMin = Vector2.zero; tyr.anchorMax = new Vector2(1, 0.38f);
            tyr.offsetMin = new Vector2(3, 3); tyr.offsetMax = new Vector2(-3, 0);
            var tyt = typeGO.AddComponent<Text>();
            tyt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tyt.fontSize = 8; tyt.color = spell.projectileColor;
            tyt.alignment = TextAnchor.MiddleCenter;
            tyt.text = spell.isBook ? "B" : "S";

            spellSlotGOs.Add(go);
            var et = go.AddComponent<EventTrigger>();

            var onEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            onEnter.callback.AddListener(_ => {
                bg.color = new Color(0.28f, 0.22f, 0.36f, 1f);
                if (tooltip != null)
                {
                    bool eqRight = inventory?.GetEquippedSpell("Weapon") == spell;
                    bool eqLeft = inventory?.GetEquippedSpell("WeaponLeft") == spell;
                    string status = eqRight ? "  [ПР рука]" : eqLeft ? "  [ЛЕВ рука]" : "";
                    tooltipText.text = $"<b>{spell.spellName}</b>{status}  {(spell.isBook ? "Книга" : "Свиток")}\n" +
                        $"Урон: {spell.damage}   Мана: {spell.manaCost}\n" +
                        (eqRight ? "ЛКМ — снять с правой\n" : "ЛКМ — правая рука\n") +
                        (eqLeft ? "ПКМ — снять с левой" : "ПКМ — левая рука");
                    tooltip.SetActive(true);
                }
            });
            et.triggers.Add(onEnter);

            var onExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            onExit.callback.AddListener(_ => {
                bg.color = new Color(0.18f, 0.15f, 0.22f, 1f);
                if (tooltip != null) tooltip.SetActive(false);
            });
            et.triggers.Add(onExit);

            var onDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            onDown.callback.AddListener(ev => {
                var ped = (PointerEventData)ev;
                if (ped.button == PointerEventData.InputButton.Left)
                {
                    if (inventory?.GetEquippedSpell("Weapon") == spell)
                    {
                        inventory?.UnequipSpell("Weapon");
                        BuildSpellSlots(); EquipmentUI.RefreshIfOpen();
                        if (tooltip != null) tooltip.SetActive(false);
                        return;
                    }
                    inventory?.EquipSpell(spell, "Weapon");
                    if (!spell.isBook) inventory?.RemoveKnownSpell(ci);
                    BuildSpellSlots(); EquipmentUI.RefreshIfOpen();
                    if (tooltip != null) tooltip.SetActive(false);
                }
                else if (ped.button == PointerEventData.InputButton.Right)
                {
                    if (inventory?.GetEquippedSpell("WeaponLeft") == spell)
                    {
                        inventory?.UnequipSpell("WeaponLeft");
                        BuildSpellSlots(); EquipmentUI.RefreshIfOpen();
                        if (tooltip != null) tooltip.SetActive(false);
                        return;
                    }
                    inventory?.EquipSpell(spell, "WeaponLeft");
                    if (!spell.isBook) inventory?.RemoveKnownSpell(ci);
                    BuildSpellSlots(); EquipmentUI.RefreshIfOpen();
                    if (tooltip != null) tooltip.SetActive(false);
                }
            });
            et.triggers.Add(onDown);

            var onBeginDrag = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            onBeginDrag.callback.AddListener(_ => StartSpellDrag(spell, ci));
            et.triggers.Add(onBeginDrag);

            var onDrag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            onDrag.callback.AddListener(_ => { if (ghostRect != null) ghostRect.position = Input.mousePosition; });
            et.triggers.Add(onDrag);

            var onEndDrag = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            onEndDrag.callback.AddListener(_ => {
                if (isDragging && draggedSpell != null)
                {
                    string slotType = EquipmentUI.Instance?.GetSlotTypeUnderMouse();
                    if (slotType == "Weapon" || slotType == "WeaponLeft")
                    {
                        inventory?.EquipSpell(draggedSpell, slotType);
                        if (!draggedSpell.isBook) inventory?.RemoveKnownSpell(ci);
                        BuildSpellSlots(); EquipmentUI.RefreshIfOpen();
                    }
                    FinishDrag();
                }
            });
            et.triggers.Add(onEndDrag);
        }
    }

    void StartSpellDrag(SpellDefinition spell, int fromIndex)
    {
        draggedSpell = spell; draggedItem = null; isDragging = true; dragFromIndex = fromIndex;
        dragGhost = new GameObject("SpellDragGhost");
        dragGhost.transform.SetParent(inventoryCanvas.transform, false);
        ghostRect = dragGhost.AddComponent<RectTransform>();
        ghostRect.sizeDelta = new Vector2(CELL * 0.85f, CELL * 0.85f);
        ghostRect.pivot = new Vector2(0.5f, 0.5f);
        ghostRect.position = Input.mousePosition;
        var gi = dragGhost.AddComponent<Image>();
        gi.color = spell.projectileColor; gi.raycastTarget = false;
        var gt = new GameObject("GT"); gt.transform.SetParent(dragGhost.transform, false);
        var gtr = gt.AddComponent<RectTransform>();
        gtr.anchorMin = Vector2.zero; gtr.anchorMax = Vector2.one;
        gtr.offsetMin = gtr.offsetMax = Vector2.zero;
        var gtt = gt.AddComponent<Text>();
        gtt.text = spell.spellName.Length > 0 ? spell.spellName[0].ToString() : "?";
        gtt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        gtt.fontSize = 20; gtt.fontStyle = FontStyle.Bold;
        gtt.color = Color.white; gtt.alignment = TextAnchor.MiddleCenter;
        gtt.raycastTarget = false;
    }

    void CreateSlot(int index, Transform parent)
    {
        int col = index % COLS, row = index / COLS;
        float x = col * (CELL + SPACING), y = -row * (CELL + SPACING);

        var go = new GameObject($"Slot_{index}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1); rt.sizeDelta = new Vector2(CELL, CELL);
        rt.anchoredPosition = new Vector2(x, y);
        go.AddComponent<Image>().color = new Color(0.20f, 0.20f, 0.24f, 1f);

        var iconGO = new GameObject("Icon"); iconGO.transform.SetParent(go.transform, false);
        iconGO.transform.SetSiblingIndex(0);
        var ir = iconGO.AddComponent<RectTransform>();
        ir.anchorMin = new Vector2(0.05f, 0.05f); ir.anchorMax = new Vector2(0.95f, 0.95f);
        ir.offsetMin = ir.offsetMax = Vector2.zero;
        var iconImg = iconGO.AddComponent<Image>(); iconImg.color = new Color(0, 0, 0, 0);

        var lGO = new GameObject("Letter"); lGO.transform.SetParent(go.transform, false);
        var llr = lGO.AddComponent<RectTransform>();
        llr.anchorMin = new Vector2(0, 0.2f); llr.anchorMax = Vector2.one;
        llr.offsetMin = llr.offsetMax = Vector2.zero;
        var lt = lGO.AddComponent<Text>();
        lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lt.fontSize = 24; lt.fontStyle = FontStyle.Bold;
        lt.color = new Color(1, 1, 1, 0.7f); lt.alignment = TextAnchor.MiddleCenter;

        var qGO = new GameObject("Quantity"); qGO.transform.SetParent(go.transform, false);
        var qr = qGO.AddComponent<RectTransform>();
        qr.anchorMin = Vector2.zero; qr.anchorMax = new Vector2(1, 0.35f);
        qr.offsetMin = new Vector2(3, 3); qr.offsetMax = new Vector2(-3, 0);
        var qt = qGO.AddComponent<Text>();
        qt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        qt.fontSize = 16; qt.fontStyle = FontStyle.Bold;
        qt.color = new Color(1, 1, 0.5f, 1f);
        qt.alignment = TextAnchor.LowerRight;

        slotGOs[index] = go; slotIcons[index] = iconImg;
        slotLetters[index] = lt; slotRects[index] = rt;

        int ci = index;
        var et = go.AddComponent<EventTrigger>();

        AddEvent(et, EventTriggerType.PointerEnter, _ => {
            if (isDragging || !Safe(ci)) return;
            var item = inventory.items[ci];
            if (item != null && tooltip != null)
            {
                string displayType = !string.IsNullOrEmpty(item.originalType) ? item.originalType : item.itemType;
                string splitHint = item.maxQuantity > 1 && item.quantity > 1 ? "\nShift+ЛКМ — разделить" : "";
                tooltipText.text = $"<b>{item.itemName}</b>\n{displayType}  |  {item.value}\n" +
                    $"ЛКМ — надеть/использовать\nЛКМ+drag — переместить\nПКМ — выбросить{splitHint}";
                tooltip.SetActive(true);
            }
        });
        AddEvent(et, EventTriggerType.PointerExit, _ => { if (tooltip != null) tooltip.SetActive(false); });
        AddEvent(et, EventTriggerType.PointerDown, ev => {
            var ped = (PointerEventData)ev;
            if (ped.button == PointerEventData.InputButton.Left)
            {
                if (!Safe(ci) || inventory.items[ci] == null) return;
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    SplitStack(ci); return;
                }
                pendingIndex = ci; mouseDownPos = Input.mousePosition;
                if (tooltip != null) tooltip.SetActive(false);
            }
            else if (ped.button == PointerEventData.InputButton.Right)
                DropItemToWorld(ci);
        });
    }

    void Update()
    {
        HandleOpenClose();
        HandleDragLogic();
        UpdateTooltipPos();
    }

    void HandleOpenClose()
    {
        if (!Input.GetKeyDown(KeyCode.Tab)) return;
        IsOpen = !IsOpen;
        inventoryPanel.SetActive(IsOpen);
        tabsContainer.SetActive(IsOpen);

        if (IsOpen)
        {
            RefreshSlots();
            if (currentTab == 1) BuildSpellSlots();
            PlayerMovement.UnlockCursor();
        }
        else
        {
            PlayerMovement.LockCursor();
            if (tooltip != null) tooltip.SetActive(false);
            if (isDragging) CancelDrag();
            if (splitPanel != null) CloseSplitPanel();
            pendingIndex = -1;
        }
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        inventoryPanel?.SetActive(open);
        tabsContainer?.SetActive(open);
        if (!open)
        {
            if (tooltip != null) tooltip.SetActive(false);
            if (isDragging) CancelDrag();
            if (splitPanel != null) CloseSplitPanel();
            pendingIndex = -1;
        }
        else
        {
            RefreshSlots();
            if (currentTab == 1) BuildSpellSlots();
        }
    }

    void HandleDragLogic()
    {
        if (isDragging && ghostRect != null)
            ghostRect.position = Input.mousePosition;

        bool dragStartedThisFrame = false;

        if (pendingIndex >= 0 && !isDragging)
        {
            float dist = Vector2.Distance(Input.mousePosition, mouseDownPos);
            if (dist > DRAG_THRESHOLD)
            {
                StartDrag(pendingIndex); pendingIndex = -1; dragStartedThisFrame = true;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                int idx = pendingIndex; pendingIndex = -1; ClickSlot(idx);
            }
        }

        // ✅ Сбрасываем флаг через кадр после split
        if (dragJustStarted)
        {
            dragJustStarted = false;
            return;
        }

        if (isDragging && draggedItem != null && !dragStartedThisFrame)
        {
            if (Input.GetMouseButtonUp(0))
            {
                int invTarget = GetInvSlotUnderMouse();
                if (invTarget >= 0) EndDragOnInvSlot(invTarget);
                else
                {
                    string equipType = EquipmentUI.Instance?.GetSlotTypeUnderMouse();
                    if (equipType != null && draggedItem != null)
                    {
                        if (EquipmentUI.Instance.IsSlotBlocked(equipType)) EndDragOutside();
                        else
                        {
                            string origType = draggedItem.originalType ?? draggedItem.itemType;
                            bool canEquip = origType == equipType ||
                                (equipType == "WeaponLeft" && (origType == "Weapon" || origType == "Shield")) ||
                                (equipType == "Weapon" && (origType == "TwoHand" || origType == "Bow"));
                            if (canEquip)
                            {
                                if (equipType == "WeaponLeft") draggedItem.itemType = "WeaponLeft";
                                inventory.EquipItem(draggedItem);
                                draggedItem = null;
                                FinishDrag();
                            }
                            else EndDragOutside();
                        }
                    }
                    else EndDragOutside();
                }
            }
        }
    }

    void ClickSlot(int index)
    {
        if (!Safe(index)) return;
        Item item = inventory.items[index];
        if (item == null) return;

        string origType = item.originalType ?? item.itemType;
        Item rightItem = inventory.GetEquippedItem("Weapon");
        bool rightHasTwoHandOrBow = rightItem != null && (inventory.IsTwoHanded(rightItem) || inventory.IsBow(rightItem));
        bool hasWeaponLeft = inventory.GetEquippedItem("WeaponLeft") != null;

        if (origType == "TwoHand") { item.itemType = "TwoHand"; inventory.EquipItem(item); }
        else if (origType == "Bow") { item.itemType = "Bow"; inventory.EquipItem(item); }
        else if (origType == "Shield")
        {
            if (rightHasTwoHandOrBow) inventory.UnequipItem("Weapon");
            item.itemType = "WeaponLeft"; inventory.EquipItem(item);
        }
        else if (origType == "Weapon")
        {
            if (rightHasTwoHandOrBow) inventory.EquipItem(item);
            else if (rightItem != null && !hasWeaponLeft) { item.itemType = "WeaponLeft"; inventory.EquipItem(item); }
            else inventory.EquipItem(item);
        }
        else if (inventory.IsEquippableType(origType)) inventory.EquipItem(item);
        else if (origType == "Potion" || origType == "ManaPotion") inventory.UseItem(index);

        RefreshSlots();
    }

    void StartDrag(int fromIndex)
    {
        if (!Safe(fromIndex) || inventory.items[fromIndex] == null) return;
        dragFromIndex = fromIndex; draggedItem = inventory.TakeItem(fromIndex);
        draggedSpell = null; isDragging = true;

        dragGhost = new GameObject("DragGhost");
        dragGhost.transform.SetParent(inventoryCanvas.transform, false);
        ghostRect = dragGhost.AddComponent<RectTransform>();
        ghostRect.sizeDelta = new Vector2(CELL * 0.85f, CELL * 0.85f);
        ghostRect.pivot = new Vector2(0.5f, 0.5f);
        ghostRect.position = Input.mousePosition;
        var gi = dragGhost.AddComponent<Image>();
        gi.color = GetItemColor(draggedItem); gi.raycastTarget = false;

        var glGO = new GameObject("GL"); glGO.transform.SetParent(dragGhost.transform, false);
        var glr = glGO.AddComponent<RectTransform>();
        glr.anchorMin = Vector2.zero; glr.anchorMax = Vector2.one;
        glr.offsetMin = glr.offsetMax = Vector2.zero;
        var glt = glGO.AddComponent<Text>();
        glt.text = !string.IsNullOrEmpty(draggedItem.itemName) ? draggedItem.itemName[0].ToString() : "?";
        glt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        glt.fontSize = 24; glt.fontStyle = FontStyle.Bold;
        glt.color = Color.white; glt.alignment = TextAnchor.MiddleCenter;
        glt.raycastTarget = false;
        RefreshSlots();
    }

    void EndDragOnInvSlot(int targetIndex)
    {
        if (!isDragging || draggedItem == null) return;
        if (!Safe(targetIndex)) { EndDragOutside(); return; }
        if (!string.IsNullOrEmpty(draggedItem.originalType)) draggedItem.itemType = draggedItem.originalType;

        Item existing = inventory.items[targetIndex];

        // Слияние стаков
        if (existing != null && existing.itemName == draggedItem.itemName
            && existing.maxQuantity > 1 && existing.quantity < existing.maxQuantity)
        {
            int canAdd = existing.maxQuantity - existing.quantity;
            int add = Mathf.Min(canAdd, draggedItem.quantity);
            existing.quantity += add;
            draggedItem.quantity -= add;
            if (draggedItem.quantity <= 0) { draggedItem = null; FinishDrag(); return; }
            if (Safe(dragFromIndex) && inventory.items[dragFromIndex] == null)
                inventory.items[dragFromIndex] = draggedItem;
            else inventory.AddItem(draggedItem);
            draggedItem = null; FinishDrag(); return;
        }

        inventory.items[targetIndex] = draggedItem;
        if (existing != null && Safe(dragFromIndex)) inventory.items[dragFromIndex] = existing;
        FinishDrag();
    }

    void EndDragOutside()
    {
        if (!isDragging || draggedItem == null) return;
        if (!string.IsNullOrEmpty(draggedItem.originalType)) draggedItem.itemType = draggedItem.originalType;
        SpawnItemInWorld(draggedItem); FinishDrag();
    }

    void CancelDrag()
    {
        if (!isDragging) return;
        if (draggedItem != null)
        {
            if (!string.IsNullOrEmpty(draggedItem.originalType)) draggedItem.itemType = draggedItem.originalType;
            if (Safe(dragFromIndex) && inventory.items[dragFromIndex] == null) inventory.items[dragFromIndex] = draggedItem;
            else inventory.AddItem(draggedItem);
        }
        FinishDrag();
    }

    void FinishDrag()
    {
        isDragging = false; draggedItem = null; draggedSpell = null; dragFromIndex = -1; pendingIndex = -1;
        if (dragGhost != null) { Destroy(dragGhost); dragGhost = null; ghostRect = null; }
        RefreshSlots();
    }

    void DropItemToWorld(int index)
    {
        if (!Safe(index)) return;
        Item item = inventory.TakeItem(index);
        if (item == null) return;
        if (tooltip != null) tooltip.SetActive(false);
        if (!string.IsNullOrEmpty(item.originalType)) item.itemType = item.originalType;
        SpawnItemInWorld(item); RefreshSlots();
    }

    void SpawnItemInWorld(Item item)
    {
        if (item == null) return;
        if (dropSound != null && audioSource != null) audioSource.PlayOneShot(dropSound, dropVolume);

        Camera cam = GetComponentInChildren<Camera>();
        Vector3 throwDir = cam != null ? cam.transform.forward : transform.forward;
        Vector3 spawnPos = cam != null
            ? cam.transform.position + throwDir * 0.5f
            : transform.position + Vector3.up * 0.5f + throwDir * 0.5f;

        var drop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        drop.transform.position = spawnPos;
        drop.transform.localScale = item.itemScale;
        drop.name = item.itemName; drop.tag = "Item";

        var rend = drop.GetComponent<Renderer>();
        if (retroMaterial != null) rend.material = new Material(retroMaterial);
        if (item.worldTexture != null) rend.material.mainTexture = item.worldTexture;
        else rend.material.color = GetItemColor(item);

        var data = drop.AddComponent<ItemData>();
        data.itemName = item.itemName; data.itemType = item.itemType;
        data.value = item.maxQuantity > 1 ? item.quantity : item.value;
        data.itemColor = item.itemColor;
        data.itemScale = item.itemScale;

        var col = drop.GetComponent<Collider>();
        var playerCol = GetComponent<Collider>();
        if (col != null && playerCol != null) Physics.IgnoreCollision(col, playerCol);
        foreach (var enemy in FindObjectsByType<EnemyNav>())
        {
            var ec = enemy.GetComponent<Collider>();
            if (ec != null && col != null) Physics.IgnoreCollision(col, ec);
        }

        // ✅ Парящий предмет как в Lunacid
        // ✅ Lunacid стиль — бросок вперёд, отскок, потом парение
        ItemFloat.AddToDropped(drop);
        drop.AddComponent<BlobShadow>();
    }

    public void RefreshSlots()
    {
        if (inventory == null || inventory.items == null) return;
        for (int i = 0; i < SLOTS; i++)
        {
            if (!Safe(i)) break;
            var go = slotGOs[i]; var ico = slotIcons[i]; var let = slotLetters[i];
            if (go == null || !go) { RebuildUIIfNeeded(); return; }
            if (ico == null || !ico || let == null || !let) continue;

            bool ghost = isDragging && i == dragFromIndex;
            Item item = ghost ? null : inventory.items[i];
            if (item != null && !string.IsNullOrEmpty(item.itemName))
            {
                ico.color = GetItemColor(item);
                let.text = item.itemName[0].ToString();
                let.color = Color.white;
            }
            else { ico.color = new Color(0, 0, 0, 0); let.text = ""; }

            var qText = go.transform.Find("Quantity")?.GetComponent<Text>();
            if (qText != null)
                qText.text = (item != null && item.maxQuantity > 1) ? item.quantity.ToString() : "";
        }
    }

    void SplitStack(int index)
    {
        if (!Safe(index)) return;
        Item item = inventory.items[index];
        if (item == null || item.maxQuantity <= 1 || item.quantity <= 1) return;
        if (tooltip != null) tooltip.SetActive(false);
        ShowSplitPanel(index, item);
    }

    void ShowSplitPanel(int fromIndex, Item item)
    {
        if (splitPanel != null) Destroy(splitPanel);
        splitFromIndex = fromIndex;

        splitPanel = new GameObject("SplitPanel");
        splitPanel.transform.SetParent(inventoryCanvas.transform, false);
        var pr = splitPanel.AddComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.pivot = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(220, 130);
        pr.anchoredPosition = Vector2.zero;
        splitPanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.13f, 1f);

        var titleGO = new GameObject("Title"); titleGO.transform.SetParent(splitPanel.transform, false);
        var tr = titleGO.AddComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 1); tr.anchorMax = new Vector2(1, 1);
        tr.pivot = new Vector2(0.5f, 1); tr.sizeDelta = new Vector2(0, 30); tr.anchoredPosition = Vector2.zero;
        var tt = titleGO.AddComponent<Text>();
        tt.text = $"Разделить: {item.itemName} (макс {item.quantity})";
        tt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tt.fontSize = 11; tt.color = new Color(0.7f, 0.7f, 0.8f);
        tt.alignment = TextAnchor.MiddleCenter;

        var inputGO = new GameObject("Input"); inputGO.transform.SetParent(splitPanel.transform, false);
        var ir = inputGO.AddComponent<RectTransform>();
        ir.anchorMin = new Vector2(0.1f, 0.5f); ir.anchorMax = new Vector2(0.9f, 0.8f);
        ir.offsetMin = ir.offsetMax = Vector2.zero;
        inputGO.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1f);
        var inputField = inputGO.AddComponent<InputField>();

        var inputTextGO = new GameObject("Text"); inputTextGO.transform.SetParent(inputGO.transform, false);
        var itr = inputTextGO.AddComponent<RectTransform>();
        itr.anchorMin = Vector2.zero; itr.anchorMax = Vector2.one;
        itr.offsetMin = new Vector2(5, 2); itr.offsetMax = new Vector2(-5, -2);
        var inputText = inputTextGO.AddComponent<Text>();
        inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        inputText.fontSize = 18; inputText.color = Color.white;
        inputText.alignment = TextAnchor.MiddleCenter;
        inputField.textComponent = inputText;
        inputField.contentType = InputField.ContentType.IntegerNumber;
        inputField.text = Mathf.Max(1, item.quantity / 2).ToString();
        inputField.characterLimit = 3;
        inputField.Select(); inputField.ActivateInputField();

        MakeSplitBtn(splitPanel.transform, "OK", new Vector2(0.1f, 0.1f), new Vector2(0.45f, 0.42f),
            new Color(0.2f, 0.4f, 0.2f, 1f), () => {
                if (int.TryParse(inputField.text, out int amount)) DoSplit(splitFromIndex, amount);
                CloseSplitPanel();
            });

        MakeSplitBtn(splitPanel.transform, "Отмена", new Vector2(0.55f, 0.1f), new Vector2(0.9f, 0.42f),
            new Color(0.4f, 0.2f, 0.2f, 1f), CloseSplitPanel);
    }

    void MakeSplitBtn(Transform parent, string label, Vector2 amin, Vector2 amax, Color col, System.Action onClick)
    {
        var go = new GameObject(label); go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = amin; r.anchorMax = amax; r.offsetMin = r.offsetMax = Vector2.zero;
        var bg = go.AddComponent<Image>(); bg.color = col;
        var btn = go.AddComponent<Button>(); btn.targetGraphic = bg;
        btn.onClick.AddListener(() => onClick());
        var tGO = new GameObject("T"); tGO.transform.SetParent(go.transform, false);
        var tr = tGO.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        var t = tGO.AddComponent<Text>();
        t.text = label; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 14; t.color = Color.white; t.alignment = TextAnchor.MiddleCenter;
    }

    void DoSplit(int index, int amount)
    {
        if (!Safe(index)) return;
        Item item = inventory.items[index];
        if (item == null || amount <= 0) return;

        // ✅ Если ввели больше чем есть — берём всё
        amount = Mathf.Min(amount, item.quantity);

        Item split = new Item(item.itemName, item.itemType, item.value, item.itemColor, item.itemScale);
        split.worldTexture = item.worldTexture;
        split.quantity = amount;
        item.quantity -= amount;

        // ✅ Если забрали всё — очищаем исходный слот
        if (item.quantity <= 0) inventory.items[index] = null;

        // ✅ Вешаем на мышь как drag
        // dragFromIndex = -1 чтобы исходный слот не скрывался (там остаток)
        dragFromIndex = -1;
        draggedItem = split;
        draggedSpell = null;
        isDragging = true;
        dragJustStarted = true; // ✅ защита от немедленного дропа

        dragGhost = new GameObject("DragGhost");
        dragGhost.transform.SetParent(inventoryCanvas.transform, false);
        ghostRect = dragGhost.AddComponent<RectTransform>();
        ghostRect.sizeDelta = new Vector2(CELL * 0.85f, CELL * 0.85f);
        ghostRect.pivot = new Vector2(0.5f, 0.5f);
        ghostRect.position = Input.mousePosition;
        var gi = dragGhost.AddComponent<Image>();
        gi.color = GetItemColor(split); gi.raycastTarget = false;

        var glGO = new GameObject("GL"); glGO.transform.SetParent(dragGhost.transform, false);
        var glr = glGO.AddComponent<RectTransform>();
        glr.anchorMin = Vector2.zero; glr.anchorMax = Vector2.one;
        glr.offsetMin = glr.offsetMax = Vector2.zero;
        var glt = glGO.AddComponent<Text>();
        glt.text = !string.IsNullOrEmpty(split.itemName) ? split.itemName[0].ToString() : "?";
        glt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        glt.fontSize = 24; glt.fontStyle = FontStyle.Bold;
        glt.color = Color.white; glt.alignment = TextAnchor.MiddleCenter;
        glt.raycastTarget = false;

        RefreshSlots();
    }

    void CloseSplitPanel()
    {
        if (splitPanel != null) { Destroy(splitPanel); splitPanel = null; }
        splitFromIndex = -1;
    }

    void RebuildUIIfNeeded() { BuildUI(); inventoryPanel?.SetActive(IsOpen); tabsContainer?.SetActive(IsOpen); }
    bool Safe(int i) => inventory != null && inventory.items != null && i >= 0 && i < inventory.items.Length;

    int GetInvSlotUnderMouse()
    {
        if (currentTab != 0) return -1;
        for (int i = 0; i < SLOTS; i++)
        {
            if (slotRects[i] == null) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(slotRects[i], Input.mousePosition, null)) return i;
        }
        return -1;
    }

    Color GetItemColor(Item item)
    {
        if (item == null) return Color.gray;
        if (item.itemColor != Color.white && item.itemColor != default(Color)) return item.itemColor;
        string type = item.originalType ?? item.itemType;
        return type switch
        {
            "Potion" => new Color(0.2f, 0.8f, 0.3f),
            "ManaPotion" => new Color(0.2f, 0.4f, 1.0f),
            "Weapon" or "WeaponLeft" or "TwoHand" or "Bow" => new Color(0.82f, 0.22f, 0.22f),
            "Shield" => new Color(0.22f, 0.48f, 0.85f),
            "Helmet" or "Chest" or "Legs" or "Boots" => new Color(0.22f, 0.48f, 0.85f),
            "Ring" or "Amulet" => new Color(0.45f, 0f, 0.7f),
            _ => new Color(0.6f, 0.6f, 0.6f)
        };
    }

    void UpdateTooltipPos()
    {
        if (tooltip == null || !tooltip.activeSelf) return;
        Vector2 mp = Input.mousePosition; float ox = 15f, oy = -15f;
        if (mp.x + tooltipRect.sizeDelta.x + ox > Screen.width) ox = -tooltipRect.sizeDelta.x - 5f;
        if (mp.y + oy - tooltipRect.sizeDelta.y < 0) oy = tooltipRect.sizeDelta.y + 5f;
        tooltipRect.position = new Vector3(mp.x + ox, mp.y + oy, 0);
    }

    void BuildTooltip()
    {
        tooltip = new GameObject("InvTooltip");
        tooltip.transform.SetParent(inventoryCanvas.transform, false);
        tooltipRect = tooltip.AddComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(210, 90); tooltipRect.pivot = new Vector2(0, 1);
        tooltip.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.1f, 1f);
        var tGO = new GameObject("Text"); tGO.transform.SetParent(tooltip.transform, false);
        var tr = tGO.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(8, 6); tr.offsetMax = new Vector2(-8, -6);
        tooltipText = tGO.AddComponent<Text>();
        tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tooltipText.color = Color.white; tooltipText.fontSize = 11;
        tooltipText.supportRichText = true; tooltipText.alignment = TextAnchor.UpperLeft;
        tooltip.SetActive(false);
    }

    void MakeLabel(Transform parent, string text, Vector2 amin, Vector2 amax, Vector2 piv, Vector2 sz, Vector2 ap, int fs, Color col)
    {
        var go = new GameObject("Label"); go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = amin; r.anchorMax = amax; r.pivot = piv; r.sizeDelta = sz; r.anchoredPosition = ap;
        var t = go.AddComponent<Text>();
        t.text = text; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fs; t.color = col; t.alignment = TextAnchor.MiddleCenter;
    }

    void AddEvent(EventTrigger et, EventTriggerType type, System.Action<BaseEventData> action)
    {
        var e = new EventTrigger.Entry { eventID = type };
        e.callback.AddListener(ev => action(ev)); et.triggers.Add(e);
    }

    public static void RefreshIfOpen()
    {
        if (IsOpen && Instance != null)
        {
            Instance.RefreshSlots();
            if (Instance.currentTab == 1) Instance.BuildSpellSlots();
        }
    }
}