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
    private GameObject tooltip;
    private Text tooltipText;
    private RectTransform tooltipRect;

    private const int COLS = 5;
    private const int ROWS = 5;
    private const int SLOTS = COLS * ROWS;
    private const float CELL = 68f;
    private const float SPACING = 6f;
    private const float PAD = 12f;
    private const float TITLE_H = 36f;
    private const float DRAG_THRESHOLD = 8f;

    private GameObject[] slotGOs = new GameObject[SLOTS];
    private Image[] slotIcons = new Image[SLOTS];
    private Text[] slotLetters = new Text[SLOTS];
    private Text[] slotQuantities = new Text[SLOTS]; // ✅ тексты количества
    private RectTransform[] slotRects = new RectTransform[SLOTS];

    private int pendingIndex = -1;
    private Vector2 mouseDownPos;
    private bool isDragging = false;
    private int dragFromIndex = -1;
    private Item draggedItem = null;
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
        inventoryCanvas.AddComponent<GraphicRaycaster>();

        inventoryPanel = new GameObject("InventoryPanel");
        inventoryPanel.transform.SetParent(inventoryCanvas.transform, false);
        var pr = inventoryPanel.AddComponent<RectTransform>();
        pr.anchorMin = new Vector2(1, 0); pr.anchorMax = new Vector2(1, 0);
        pr.pivot = new Vector2(1, 0); pr.sizeDelta = new Vector2(panelW, panelH);
        pr.anchoredPosition = new Vector2(-20, 20);
        inventoryPanel.AddComponent<Image>().color = new Color(0.13f, 0.13f, 0.16f, 1f);

        MakeLabel(inventoryPanel.transform, "ИНВЕНТАРЬ [ТАБ]",
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
            new Vector2(0, TITLE_H), Vector2.zero, 18, new Color(0.65f, 0.65f, 0.75f));

        var ln = new GameObject("Line"); ln.transform.SetParent(inventoryPanel.transform, false);
        var lr = ln.AddComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 1); lr.anchorMax = new Vector2(1, 1);
        lr.pivot = new Vector2(0.5f, 1); lr.sizeDelta = new Vector2(-PAD * 2, 1);
        lr.anchoredPosition = new Vector2(0, -TITLE_H);
        ln.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.38f, 1f);

        var grid = new GameObject("Grid"); grid.transform.SetParent(inventoryPanel.transform, false);
        var gr = grid.AddComponent<RectTransform>();
        gr.anchorMin = new Vector2(0.5f, 0.5f); gr.anchorMax = new Vector2(0.5f, 0.5f);
        gr.pivot = new Vector2(0.5f, 0.5f); gr.sizeDelta = new Vector2(gridW, gridH);
        gr.anchoredPosition = new Vector2(0, -(TITLE_H / 2));

        for (int i = 0; i < SLOTS; i++) CreateSlot(i, grid.transform);
        BuildTooltip();
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

        // Иконка
        var iconGO = new GameObject("Icon"); iconGO.transform.SetParent(go.transform, false);
        iconGO.transform.SetSiblingIndex(0);
        var ir = iconGO.AddComponent<RectTransform>();
        ir.anchorMin = new Vector2(0.05f, 0.05f); ir.anchorMax = new Vector2(0.95f, 0.95f);
        ir.offsetMin = ir.offsetMax = Vector2.zero;
        var iconImg = iconGO.AddComponent<Image>(); iconImg.color = new Color(0, 0, 0, 0);

        // Буква предмета (центр)
        var lGO = new GameObject("Letter"); lGO.transform.SetParent(go.transform, false);
        var llr = lGO.AddComponent<RectTransform>();
        llr.anchorMin = new Vector2(0, 0.2f); llr.anchorMax = Vector2.one;
        llr.offsetMin = llr.offsetMax = Vector2.zero;
        var lt = lGO.AddComponent<Text>();
        lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lt.fontSize = 24; lt.fontStyle = FontStyle.Bold;
        lt.color = new Color(1, 1, 1, 0.7f); lt.alignment = TextAnchor.MiddleCenter;

        // ✅ Количество (правый нижний угол)
        var qGO = new GameObject("Quantity"); qGO.transform.SetParent(go.transform, false);
        var qr = qGO.AddComponent<RectTransform>();
        qr.anchorMin = Vector2.zero; qr.anchorMax = new Vector2(1f, 0.35f);
        qr.offsetMin = new Vector2(2f, 2f); qr.offsetMax = new Vector2(-2f, 0f);
        var qt = qGO.AddComponent<Text>();
        qt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        qt.fontSize = 13; qt.fontStyle = FontStyle.Bold;
        qt.color = new Color(1f, 0.9f, 0.4f); qt.alignment = TextAnchor.LowerRight;
        qt.text = "";

        slotGOs[index] = go; slotIcons[index] = iconImg;
        slotLetters[index] = lt; slotRects[index] = rt;
        slotQuantities[index] = qt;

        int ci = index;
        var et = go.AddComponent<EventTrigger>();

        AddEvent(et, EventTriggerType.PointerEnter, _ => {
            if (isDragging || !Safe(ci)) return;
            var item = inventory.items[ci];
            if (item != null && tooltip != null)
            {
                string displayType = !string.IsNullOrEmpty(item.originalType) ? item.originalType : item.itemType;
                string quantityLine = item.maxQuantity > 1 ? $"Количество: {item.quantity}/{item.maxQuantity}\n" : "";
                tooltipText.text = $"<b>{item.itemName}</b>\n{displayType}  |  {item.value}\n" +
                    quantityLine +
                    "ЛКМ — надеть/использовать\nЛКМ+drag — переместить\nПКМ — выбросить";
                tooltip.SetActive(true);
            }
        });

        AddEvent(et, EventTriggerType.PointerExit, _ => { if (tooltip != null) tooltip.SetActive(false); });

        AddEvent(et, EventTriggerType.PointerDown, ev => {
            var ped = (PointerEventData)ev;
            if (ped.button == PointerEventData.InputButton.Left)
            {
                if (!Safe(ci) || inventory.items[ci] == null) return;
                pendingIndex = ci; mouseDownPos = Input.mousePosition;
                if (tooltip != null) tooltip.SetActive(false);
            }
            else if (ped.button == PointerEventData.InputButton.Right)
            {
                DropItemToWorld(ci);
            }
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
        if (IsOpen) { RefreshSlots(); PlayerMovement.UnlockCursor(); }
        else
        {
            PlayerMovement.LockCursor();
            if (tooltip != null) tooltip.SetActive(false);
            if (isDragging) CancelDrag();
            pendingIndex = -1;
        }
    }

    void HandleDragLogic()
    {
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

        if (isDragging && !dragStartedThisFrame)
        {
            if (ghostRect != null) ghostRect.position = Input.mousePosition;
            if (Input.GetMouseButtonUp(0))
            {
                int invTarget = GetInvSlotUnderMouse();
                if (invTarget >= 0)
                {
                    EndDragOnInvSlot(invTarget);
                }
                else
                {
                    string equipType = EquipmentUI.Instance?.GetSlotTypeUnderMouse();
                    if (equipType != null && draggedItem != null)
                    {
                        if (EquipmentUI.Instance.IsSlotBlocked(equipType))
                        {
                            EndDragOutside();
                        }
                        else
                        {
                            string origType = draggedItem.originalType ?? draggedItem.itemType;
                            bool canEquip = origType == equipType ||
                                           (equipType == "WeaponLeft" && (origType == "Weapon" || origType == "Shield")) ||
                                           (equipType == "Weapon" && (origType == "TwoHand" || origType == "Bow"));

                            if (canEquip)
                            {
                                if (equipType == "WeaponLeft")
                                    draggedItem.itemType = "WeaponLeft";

                                inventory.EquipItem(draggedItem);
                                draggedItem = null;
                                FinishDrag();
                            }
                            else
                                EndDragOutside();
                        }
                    }
                    else EndDragOutside();
                }
            }
        }
        else if (isDragging && dragStartedThisFrame && ghostRect != null)
        {
            ghostRect.position = Input.mousePosition;
        }
    }

    void ClickSlot(int index)
    {
        if (!Safe(index)) return;
        Item item = inventory.items[index];
        if (item == null) return;

        string origType = item.originalType ?? item.itemType;

        Item rightItem = inventory.GetEquippedItem("Weapon");
        bool rightHasTwoHandOrBow = rightItem != null &&
            (inventory.IsTwoHanded(rightItem) || inventory.IsBow(rightItem));
        bool hasWeaponLeft = inventory.GetEquippedItem("WeaponLeft") != null;

        if (origType == "TwoHand")
        {
            item.itemType = "TwoHand";
            inventory.EquipItem(item);
        }
        else if (origType == "Bow")
        {
            item.itemType = "Bow";
            inventory.EquipItem(item);
        }
        else if (origType == "Shield")
        {
            if (rightHasTwoHandOrBow)
                inventory.UnequipItem("Weapon");

            item.itemType = "WeaponLeft";
            inventory.EquipItem(item);
        }
        else if (origType == "Weapon")
        {
            if (rightHasTwoHandOrBow)
                inventory.EquipItem(item);
            else if (rightItem != null && !hasWeaponLeft)
            {
                item.itemType = "WeaponLeft";
                inventory.EquipItem(item);
            }
            else
                inventory.EquipItem(item);
        }
        else if (inventory.IsEquippableType(origType))
            inventory.EquipItem(item);
        else if (origType == "Potion")
            inventory.UseItem(index);

        RefreshSlots();
    }

    void StartDrag(int fromIndex)
    {
        if (!Safe(fromIndex) || inventory.items[fromIndex] == null) return;
        if (inventoryCanvas == null) return;

        dragFromIndex = fromIndex;
        draggedItem = inventory.TakeItem(fromIndex);
        isDragging = true;

        dragGhost = new GameObject("DragGhost");
        dragGhost.transform.SetParent(inventoryCanvas.transform, false);
        ghostRect = dragGhost.AddComponent<RectTransform>();
        ghostRect.sizeDelta = new Vector2(CELL * 0.85f, CELL * 0.85f);
        ghostRect.pivot = new Vector2(0.5f, 0.5f);
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
        if (!string.IsNullOrEmpty(draggedItem.originalType))
            draggedItem.itemType = draggedItem.originalType;
        Item existing = inventory.items[targetIndex];
        inventory.items[targetIndex] = draggedItem;
        if (existing != null && Safe(dragFromIndex)) inventory.items[dragFromIndex] = existing;
        FinishDrag();
    }

    void EndDragOutside()
    {
        if (!isDragging || draggedItem == null) return;
        if (!string.IsNullOrEmpty(draggedItem.originalType))
            draggedItem.itemType = draggedItem.originalType;
        SpawnItemInWorld(draggedItem); FinishDrag();
    }

    void CancelDrag()
    {
        if (!isDragging || draggedItem == null) return;
        if (!string.IsNullOrEmpty(draggedItem.originalType))
            draggedItem.itemType = draggedItem.originalType;
        if (Safe(dragFromIndex) && inventory.items[dragFromIndex] == null)
            inventory.items[dragFromIndex] = draggedItem;
        else inventory.AddItem(draggedItem);
        FinishDrag();
    }

    void FinishDrag()
    {
        isDragging = false; draggedItem = null; dragFromIndex = -1; pendingIndex = -1;
        if (dragGhost != null) { Destroy(dragGhost); dragGhost = null; ghostRect = null; }
        RefreshSlots();
    }

    void DropItemToWorld(int index)
    {
        if (!Safe(index)) return;
        Item item = inventory.TakeItem(index);
        if (item == null) return;
        if (tooltip != null) tooltip.SetActive(false);
        if (!string.IsNullOrEmpty(item.originalType))
            item.itemType = item.originalType;
        SpawnItemInWorld(item);
        RefreshSlots();
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
        if (item.worldTexture != null)
            rend.material.mainTexture = item.worldTexture;
        else
            rend.material.color = GetItemColor(item);

        var data = drop.AddComponent<ItemData>();
        data.itemName = item.itemName; data.itemType = item.itemType;
        data.value = item.quantity > 1 ? item.quantity : item.value; // ✅ выбрасываем всё количество
        data.itemColor = item.itemColor;
        data.itemScale = item.itemScale;

        var col = drop.GetComponent<Collider>();
        var playerCol = GetComponent<Collider>();
        if (col != null && playerCol != null) Physics.IgnoreCollision(col, playerCol);

        foreach (var enemy in FindObjectsByType<EnemyNav>())
        {
            var enemyCol = enemy.GetComponent<Collider>();
            if (enemyCol != null && col != null)
                Physics.IgnoreCollision(col, enemyCol);
        }

        var rb = drop.AddComponent<Rigidbody>();
        rb.AddForce(throwDir * throwForce, ForceMode.Impulse);

        var freezer = drop.AddComponent<ItemFreezer>();
        freezer.delay = dropFreezeDelay;
    }

    void RefreshSlots()
    {
        if (inventory == null || inventory.items == null) return;
        for (int i = 0; i < SLOTS; i++)
        {
            if (!Safe(i)) break;
            var go = slotGOs[i]; var ico = slotIcons[i]; var let = slotLetters[i];
            var qty = slotQuantities[i];
            if (go == null || !go) { RebuildUIIfNeeded(); return; }
            if (ico == null || !ico || let == null || !let) continue;

            bool ghost = isDragging && i == dragFromIndex;
            Item item = ghost ? null : inventory.items[i];
            if (item != null && !string.IsNullOrEmpty(item.itemName))
            {
                ico.color = GetItemColor(item);
                let.text = item.itemName[0].ToString();
                let.color = Color.white;
                // ✅ Показываем количество только для стакаемых
                if (qty != null)
                    qty.text = item.maxQuantity > 1 ? item.quantity.ToString() : "";
            }
            else
            {
                ico.color = new Color(0, 0, 0, 0);
                let.text = "";
                if (qty != null) qty.text = "";
            }
        }
    }

    void RebuildUIIfNeeded() { BuildUI(); inventoryPanel?.SetActive(IsOpen); }
    bool Safe(int i) => inventory != null && inventory.items != null && i >= 0 && i < inventory.items.Length;

    int GetInvSlotUnderMouse()
    {
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
            "Weapon" or "WeaponLeft" or "TwoHand" or "Bow" => new Color(0.82f, 0.22f, 0.22f),
            "Shield" => new Color(0.22f, 0.48f, 0.85f),
            "Helmet" or "Chest" or "Legs" or "Boots" => new Color(0.22f, 0.48f, 0.85f),
            "Ring" or "Amulet" => new Color(0.45f, 0f, 0.7f),
            "Arrow" => new Color(0.6f, 0.5f, 0.2f),
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
        tooltipRect.sizeDelta = new Vector2(210, 85); tooltipRect.pivot = new Vector2(0, 1);
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

    public static void RefreshIfOpen() { if (IsOpen && Instance != null) Instance.RefreshSlots(); }
}