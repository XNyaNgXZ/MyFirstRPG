using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentUI : MonoBehaviour
{
    private GameObject tooltip;
    private Text tooltipText;
    private RectTransform tooltipRect;
    public static bool IsOpen { get; private set; } = false;
    private GameObject equipmentCanvas;
    private GameObject equipmentPanel;
    private Transform slotsContainer;
    private Inventory inventory;

    [Header("Материал для дропа")]
    public Material retroMaterial;

    [Header("Физика дропа")]
    public float dropFreezeDelay = 1.5f;
    public float throwForce = 5f;

    private Dictionary<string, GameObject> slotObjects = new Dictionary<string, GameObject>();
    private List<(RectTransform rect, string type)> slotRectList = new List<(RectTransform, string)>();

    private struct SlotDef { public string name, allowedType; }

    private SlotDef[] slots = {
        new SlotDef{name="Оружие",      allowedType="Weapon"     },
        new SlotDef{name="Шлем",        allowedType="Helmet"     },
        new SlotDef{name="Нагрудник",   allowedType="Chest"      },
        new SlotDef{name="Поножи",      allowedType="Legs"       },
        new SlotDef{name="Ботинки",     allowedType="Boots"      },
        new SlotDef{name="Лев.рука",    allowedType="WeaponLeft" },
        new SlotDef{name="Кольцо 1",    allowedType="Ring"       },
        new SlotDef{name="Кольцо 2",    allowedType="Ring"       },
        new SlotDef{name="Кольцо 3",    allowedType="Ring"       },
        new SlotDef{name="Кольцо 4",    allowedType="Ring"       },
        new SlotDef{name="Амулет",      allowedType="Amulet"     },
    };

    private int gridColumns = 4;
    private float cellSize = 85f, cellSpacing = 8f, pad = 14f, titleH = 36f;
    private float inventoryPanelH = 420f, edgeOffset = 20f;

    public static EquipmentUI Instance { get; private set; }
    void Awake() => Instance = this;

    void Start()
    {
        inventory = GetComponent<Inventory>() ?? FindAnyObjectByType<Inventory>();
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>();
        }
        CreateEquipmentUI(); equipmentPanel.SetActive(false);
    }

    void CreateEquipmentUI()
    {
        equipmentCanvas = new GameObject("EquipmentCanvas");
        var canvas = equipmentCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
        var scaler = equipmentCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        equipmentCanvas.AddComponent<GraphicRaycaster>();

        int rows = Mathf.CeilToInt((float)slots.Length / gridColumns);
        float gridW = gridColumns * cellSize + (gridColumns - 1) * cellSpacing;
        float gridH = rows * cellSize + (rows - 1) * cellSpacing;
        float panelW = gridW + pad * 2, panelH = gridH + pad * 2 + titleH;

        equipmentPanel = new GameObject("EquipmentPanel");
        equipmentPanel.transform.SetParent(equipmentCanvas.transform, false);
        var panelRect = equipmentPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f); panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f); panelRect.sizeDelta = new Vector2(panelW, panelH);
        panelRect.anchoredPosition = new Vector2(-edgeOffset, edgeOffset + inventoryPanelH + 10f);
        equipmentPanel.AddComponent<Image>().color = new Color(0.13f, 0.13f, 0.16f, 1f);

        var titleGO = new GameObject("Title"); titleGO.transform.SetParent(equipmentPanel.transform, false);
        var tr = titleGO.AddComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 1); tr.anchorMax = new Vector2(1, 1);
        tr.pivot = new Vector2(0.5f, 1); tr.sizeDelta = new Vector2(0, titleH); tr.anchoredPosition = Vector2.zero;
        var tt = titleGO.AddComponent<Text>();
        tt.text = "СНАРЯЖЕНИЕ  [I]"; tt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tt.fontSize = 18; tt.color = new Color(0.65f, 0.65f, 0.75f); tt.alignment = TextAnchor.MiddleCenter;

        var lineGO = new GameObject("Line"); lineGO.transform.SetParent(equipmentPanel.transform, false);
        var lr = lineGO.AddComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 1); lr.anchorMax = new Vector2(1, 1);
        lr.pivot = new Vector2(0.5f, 1); lr.sizeDelta = new Vector2(-pad * 2, 1);
        lr.anchoredPosition = new Vector2(0, -titleH);
        lineGO.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.38f, 1f);

        var content = new GameObject("Content"); content.transform.SetParent(equipmentPanel.transform, false);
        var cr = content.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.5f, 0.5f); cr.anchorMax = new Vector2(0.5f, 0.5f);
        cr.pivot = new Vector2(0.5f, 0.5f); cr.sizeDelta = new Vector2(gridW, gridH);
        cr.anchoredPosition = new Vector2(0, -(titleH / 2f));
        var grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(cellSize, cellSize); grid.spacing = new Vector2(cellSpacing, cellSpacing);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft; grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = gridColumns;
        slotsContainer = content.transform;

        for (int i = 0; i < slots.Length; i++) CreateSlot(i, slots[i]);

        tooltip = new GameObject("EqTooltip"); tooltip.transform.SetParent(equipmentCanvas.transform, false);
        tooltipRect = tooltip.AddComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(200, 70); tooltipRect.pivot = new Vector2(0, 1);
        tooltip.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.1f, 1f);
        var ttGO = new GameObject("TooltipText"); ttGO.transform.SetParent(tooltip.transform, false);
        var ttr = ttGO.AddComponent<RectTransform>();
        ttr.anchorMin = Vector2.zero; ttr.anchorMax = Vector2.one;
        ttr.offsetMin = new Vector2(8, 6); ttr.offsetMax = new Vector2(-8, -6);
        tooltipText = ttGO.AddComponent<Text>();
        tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tooltipText.color = Color.white; tooltipText.fontSize = 12;
        tooltipText.supportRichText = true; tooltipText.alignment = TextAnchor.UpperLeft;
        tooltip.SetActive(false);
    }

    void CreateSlot(int idx, SlotDef slotDef)
    {
        Color normalColor = new Color(0.20f, 0.20f, 0.24f, 1f);
        var slotGO = new GameObject($"Slot_{slotDef.name}");
        slotGO.transform.SetParent(slotsContainer, false);
        slotObjects[slotDef.allowedType] = slotGO;
        Image bgImage = slotGO.AddComponent<Image>(); bgImage.color = normalColor;

        var iconGO = new GameObject("Icon"); iconGO.transform.SetParent(slotGO.transform, false);
        iconGO.transform.SetSiblingIndex(0);
        var ir = iconGO.AddComponent<RectTransform>();
        ir.anchorMin = new Vector2(0.05f, 0.05f); ir.anchorMax = new Vector2(0.95f, 0.95f);
        ir.offsetMin = ir.offsetMax = Vector2.zero;
        var iconImg = iconGO.AddComponent<Image>(); iconImg.color = new Color(0, 0, 0, 0);

        var labelGO = new GameObject("Label"); labelGO.transform.SetParent(slotGO.transform, false);
        var labr = labelGO.AddComponent<RectTransform>();
        labr.anchorMin = new Vector2(0, 0); labr.anchorMax = new Vector2(1, 0);
        labr.pivot = new Vector2(0.5f, 0); labr.sizeDelta = new Vector2(0, 20); labr.anchoredPosition = Vector2.zero;
        var labt = labelGO.AddComponent<Text>();
        labt.text = slotDef.name; labt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labt.fontSize = 10; labt.color = new Color(0.6f, 0.6f, 0.7f); labt.alignment = TextAnchor.MiddleCenter;

        var letterGO = new GameObject("Letter"); letterGO.transform.SetParent(slotGO.transform, false);
        var letr = letterGO.AddComponent<RectTransform>();
        letr.anchorMin = new Vector2(0, 0.2f); letr.anchorMax = Vector2.one;
        letr.offsetMin = letr.offsetMax = Vector2.zero;
        var lett = letterGO.AddComponent<Text>();
        lett.text = slotDef.allowedType.Length > 0 ? slotDef.allowedType[0].ToString() : "?";
        lett.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lett.fontSize = 26; lett.fontStyle = FontStyle.Bold;
        lett.color = new Color(1f, 1f, 1f, 0.18f); lett.alignment = TextAnchor.MiddleCenter;

        Button btn = slotGO.AddComponent<Button>();
        btn.targetGraphic = bgImage; btn.transition = Selectable.Transition.None;
        var et = slotGO.AddComponent<EventTrigger>();

        var onEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        onEnter.callback.AddListener(_ => {
            bgImage.color = new Color(0.30f, 0.30f, 0.36f, 1f);
            Item equipped = inventory?.GetEquippedItem(slotDef.allowedType);
            if (tooltip != null)
            {
                tooltipText.text = equipped != null
                    ? $"<b>{equipped.itemName}</b>\n{equipped.value}\nЛКМ — снять\nПКМ — выбросить"
                    : $"<b>{slotDef.name}</b>\n<color=#666666>Пусто</color>";
                tooltip.SetActive(true);
            }
        });
        et.triggers.Add(onEnter);

        var onExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        onExit.callback.AddListener(_ => { bgImage.color = normalColor; if (tooltip != null) tooltip.SetActive(false); });
        et.triggers.Add(onExit);

        btn.onClick.AddListener(() => {
            if (inventory == null) return;
            Item equipped = inventory.GetEquippedItem(slotDef.allowedType);
            if (equipped != null) { inventory.UnequipItem(slotDef.allowedType); RefreshEquipmentUI(); }
        });

        var onRightClick = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        onRightClick.callback.AddListener(ev => {
            var ped = (PointerEventData)ev;
            if (ped.button != PointerEventData.InputButton.Right) return;
            if (inventory == null) return;
            Item equipped = inventory.GetEquippedItem(slotDef.allowedType);
            if (equipped == null) return;

            inventory.equippedItems.Remove(slotDef.allowedType);

            if (slotDef.allowedType == "Weapon" && HandController.Instance != null)
            {
                HandController.Instance.HideWeaponModel();
                HandController.Instance.SetWeaponMode(HandController.WeaponMode.Unarmed);
            }
            else if (slotDef.allowedType == "WeaponLeft" && HandController.Instance != null)
            {
                if (equipped.itemType != "Shield")
                    HandController.Instance.HideWeaponModelLeft();

                bool hasWeapon = inventory.equippedItems.ContainsKey("Weapon");
                HandController.Instance.SetWeaponMode(
                    hasWeapon ? HandController.WeaponMode.OneHand
                              : HandController.WeaponMode.Unarmed);
            }

            SpawnInWorld(equipped, inventory.transform);

            if (tooltip != null) tooltip.SetActive(false);
            RefreshEquipmentUI(); InventoryUICode.RefreshIfOpen();
        });
        et.triggers.Add(onRightClick);

        slotRectList.Add((slotGO.GetComponent<RectTransform>(), slotDef.allowedType));
    }

    void SpawnInWorld(Item item, Transform player)
    {
        Camera cam = player.GetComponentInChildren<Camera>();
        Vector3 throwDir = cam != null ? cam.transform.forward : player.forward;
        Vector3 spawnPos = cam != null
            ? cam.transform.position + throwDir * 0.5f
            : player.position + Vector3.up * 0.5f + throwDir * 0.5f;

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
        data.value = item.value; data.itemColor = item.itemColor;
        data.itemScale = item.itemScale;

        var col = drop.GetComponent<Collider>();
        var playerCol = player.GetComponent<Collider>();
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

    Color GetItemColor(Item item)
    {
        if (item == null) return Color.gray;
        if (item.itemColor != Color.white && item.itemColor != default(Color)) return item.itemColor;
        return item.itemType switch
        {
            "Potion" => new Color(0.2f, 0.8f, 0.3f),
            "Weapon" or "WeaponLeft" or "Shield" => new Color(0.82f, 0.22f, 0.22f),
            "Helmet" or "Chest" or "Legs" or "Boots" => new Color(0.22f, 0.48f, 0.85f),
            "Ring" or "Amulet" => new Color(0.45f, 0f, 0.7f),
            _ => new Color(0.6f, 0.6f, 0.6f)
        };
    }

    public string GetSlotTypeUnderMouse()
    {
        if (!IsOpen) return null;
        foreach (var (rect, type) in slotRectList)
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null)) return type;
        return null;
    }

    public static void RefreshIfOpen() { if (IsOpen && Instance != null) Instance.RefreshEquipmentUI(); }

    void RefreshEquipmentUI()
    {
        if (inventory == null) inventory = GetComponent<Inventory>();
        if (inventory == null) return;
        var equipped = inventory.equippedItems;

        foreach (var kvp in slotObjects)
        {
            string slotType = kvp.Key; GameObject slot = kvp.Value;
            Item item = equipped.ContainsKey(slotType) ? equipped[slotType] : null;
            slot.GetComponent<Image>().color = item != null ? new Color(0.28f, 0.28f, 0.33f) : new Color(0.20f, 0.20f, 0.24f);

            var iconImg = slot.transform.Find("Icon")?.GetComponent<Image>();
            var letterTxt = slot.transform.Find("Letter")?.GetComponent<Text>();

            if (iconImg != null)
            {
                if (item != null)
                {
                    iconImg.color = GetItemColor(item);
                    if (letterTxt != null)
                    {
                        letterTxt.text = !string.IsNullOrEmpty(item.itemName) ? item.itemName[0].ToString() : "?";
                        letterTxt.color = new Color(1f, 1f, 1f, 0.9f);
                    }
                }
                else
                {
                    iconImg.color = new Color(0, 0, 0, 0);
                    if (letterTxt != null)
                    {
                        foreach (var s in slots) if (s.allowedType == slotType) { letterTxt.text = s.allowedType[0].ToString(); break; }
                        letterTxt.color = new Color(1f, 1f, 1f, 0.18f);
                    }
                }
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            IsOpen = !IsOpen; equipmentPanel.SetActive(IsOpen);
            if (IsOpen) { RefreshEquipmentUI(); PlayerMovement.UnlockCursor(); }
            else { PlayerMovement.LockCursor(); if (tooltip != null) tooltip.SetActive(false); }
        }
        if (tooltip != null && tooltip.activeSelf)
        {
            Vector2 mp = Input.mousePosition; float ox = 15f, oy = -15f;
            if (mp.x + tooltipRect.sizeDelta.x + ox > Screen.width) ox = -tooltipRect.sizeDelta.x - 5f;
            if (mp.y + oy - tooltipRect.sizeDelta.y < 0) oy = tooltipRect.sizeDelta.y + 5f;
            tooltipRect.position = new Vector3(mp.x + ox, mp.y + oy, 0);
        }
    }

    public void SetOpen(bool open)
    {
        if (equipmentPanel == null) return;
        IsOpen = open; equipmentPanel.SetActive(open);
        if (open) RefreshEquipmentUI(); else if (tooltip != null) tooltip.SetActive(false);
    }
}