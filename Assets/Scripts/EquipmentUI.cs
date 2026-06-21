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
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
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
        tt.text = "СНАРЯЖЕНИЕ"; tt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        tooltipRect.sizeDelta = new Vector2(200, 80); tooltipRect.pivot = new Vector2(0, 1);
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

    // ✅ Блокировка слотов
    public bool IsSlotBlocked(string slotType)
    {
        if (inventory == null) return false;

        // Левая рука заблокирована если двуручное/лук в правой
        if (slotType == "WeaponLeft")
        {
            Item rightItem = inventory.GetEquippedItem("Weapon");
            return rightItem != null && (inventory.IsTwoHanded(rightItem) || inventory.IsBow(rightItem));
        }

        return false;
    }

    void CreateSlot(int idx, SlotDef slotDef)
    {
        bool isSpellSlot = slotDef.allowedType == "SpellRight" || slotDef.allowedType == "SpellLeft";
        Color normalColor = isSpellSlot
            ? new Color(0.15f, 0.12f, 0.22f, 1f)
            : new Color(0.20f, 0.20f, 0.24f, 1f);
        Color blockedColor = new Color(0.12f, 0.12f, 0.14f, 1f);

        var slotGO = new GameObject($"Slot_{slotDef.name}");
        slotGO.transform.SetParent(slotsContainer, false);
        slotObjects[slotDef.allowedType] = slotGO;
        Image bgImage = slotGO.AddComponent<Image>(); bgImage.color = normalColor;

        // Для слотов заклинаний — фиолетовый бордер
        if (isSpellSlot)
        {
            var border = new GameObject("SpellBorder"); border.transform.SetParent(slotGO.transform, false);
            var br = border.AddComponent<RectTransform>();
            br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one;
            br.offsetMin = new Vector2(-2, -2); br.offsetMax = new Vector2(2, 2);
            border.AddComponent<Image>().color = new Color(0.5f, 0.3f, 0.8f, 0.4f);
            border.transform.SetSiblingIndex(0);
        }

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
        labt.fontSize = 10;
        labt.color = isSpellSlot ? new Color(0.7f, 0.5f, 1f) : new Color(0.6f, 0.6f, 0.7f);
        labt.alignment = TextAnchor.MiddleCenter;

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
            if (IsSlotBlocked(slotDef.allowedType)) return;
            bgImage.color = new Color(0.30f, 0.30f, 0.36f, 1f);

            // ✅ Показываем заклинание или оружие
            SpellDefinition hoverSpell = inventory?.GetEquippedSpell(slotDef.allowedType);
            Item hoverItem = inventory?.GetEquippedItem(slotDef.allowedType);
            if (tooltip != null)
            {
                if (hoverSpell != null)
                    tooltipText.text = $"<b>{hoverSpell.spellName}</b>\nУрон: {hoverSpell.damage}  Мана: {hoverSpell.manaCost}\nЛКМ — снять";
                else if (hoverItem != null)
                    tooltipText.text = $"<b>{hoverItem.itemName}</b>\n{hoverItem.value}\nЛКМ — снять\nПКМ — выбросить";
                else
                    tooltipText.text = $"<b>{slotDef.name}</b>\n<color=#666666>Пусто</color>";
                tooltip.SetActive(true);
            }
        });
        et.triggers.Add(onEnter);

        var onExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        onExit.callback.AddListener(_ => {
            bgImage.color = IsSlotBlocked(slotDef.allowedType) ? blockedColor : normalColor;
            if (tooltip != null) tooltip.SetActive(false);
        });
        et.triggers.Add(onExit);

        btn.onClick.AddListener(() => {
            if (IsSlotBlocked(slotDef.allowedType)) return;
            if (inventory == null) return;

            // ✅ Сначала проверяем заклинание, потом оружие
            SpellDefinition equippedSpell = inventory.GetEquippedSpell(slotDef.allowedType);
            if (equippedSpell != null)
            {
                inventory.UnequipSpell(slotDef.allowedType);
                RefreshEquipmentUI();
                return;
            }

            Item equipped = inventory.GetEquippedItem(slotDef.allowedType);
            if (equipped != null) { inventory.UnequipItem(slotDef.allowedType); RefreshEquipmentUI(); }
        });

        var onRightClick = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        onRightClick.callback.AddListener(ev => {
            var ped = (PointerEventData)ev;
            if (ped.button != PointerEventData.InputButton.Right) return;
            if (IsSlotBlocked(slotDef.allowedType) || inventory == null) return;

            // ✅ Сначала проверяем заклинание
            SpellDefinition equippedSpell2 = inventory.GetEquippedSpell(slotDef.allowedType);
            if (equippedSpell2 != null)
            {
                inventory.UnequipSpell(slotDef.allowedType);
                RefreshEquipmentUI();
                return;
            }

            Item equipped = inventory.GetEquippedItem(slotDef.allowedType);
            if (equipped == null) return;

            inventory.equippedItems.Remove(slotDef.allowedType);

            if (HandController.Instance != null)
            {
                if (slotDef.allowedType == "Weapon")
                {
                    if (inventory.IsTwoHanded(equipped) || inventory.IsBow(equipped))
                        HandController.Instance.HideTwoHandModel();
                    else HandController.Instance.HideWeaponModel();
                    HandController.Instance.SetWeaponMode(HandController.WeaponMode.Unarmed);
                    HandController.Instance.ResetPickup();
                }
                else if (slotDef.allowedType == "WeaponLeft")
                {
                    if (inventory.IsShield(equipped)) HandController.Instance.HideShieldModel();
                    else HandController.Instance.HideWeaponModelLeft();
                    bool hasWeapon = inventory.equippedItems.ContainsKey("Weapon");
                    HandController.Instance.SetWeaponMode(
                        hasWeapon ? HandController.WeaponMode.OneHand : HandController.WeaponMode.Unarmed);
                }
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
        if (item.worldTexture != null) rend.material.mainTexture = item.worldTexture;
        else rend.material.color = GetItemColor(item);

        var data = drop.AddComponent<ItemData>();
        data.itemName = item.itemName; data.itemType = item.itemType;
        data.value = item.value; data.itemColor = item.itemColor;
        data.itemScale = item.itemScale;

        var col = drop.GetComponent<Collider>();
        var playerCol = player.GetComponent<Collider>();
        if (col != null && playerCol != null) Physics.IgnoreCollision(col, playerCol);
        foreach (var enemy in FindObjectsByType<EnemyNav>())
        {
            var ec = enemy.GetComponent<Collider>();
            if (ec != null && col != null) Physics.IgnoreCollision(col, ec);
        }

        // ✅ Lunacid стиль — бросок вперёд, отскок, потом парение
        ItemFloat.AddToDropped(drop);
        drop.AddComponent<BlobShadow>();
    }

    Color GetItemColor(Item item)
    {
        if (item == null) return Color.gray;
        if (item.itemColor != Color.white && item.itemColor != default(Color)) return item.itemColor;
        string type = item.originalType ?? item.itemType;
        return type switch
        {
            "Potion" => new Color(0.2f, 0.8f, 0.3f),
            "Weapon" or "WeaponLeft" or "Shield" or "TwoHand" or "Bow" => new Color(0.82f, 0.22f, 0.22f),
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

        foreach (var kvp in slotObjects)
        {
            string slotType = kvp.Key; GameObject slot = kvp.Value;
            bool blocked = IsSlotBlocked(slotType);

            Color normalColor = new Color(0.20f, 0.20f, 0.24f, 1f);
            Color blockedColor = new Color(0.12f, 0.12f, 0.14f, 1f);
            Color filledColor = new Color(0.28f, 0.28f, 0.33f, 1f);

            // ✅ Проверяем и оружие и заклинание в слоте
            bool hasWeapon = inventory.equippedItems.ContainsKey(slotType);
            SpellDefinition spell = (slotType == "Weapon" || slotType == "WeaponLeft")
                ? inventory.GetEquippedSpell(slotType) : null;
            bool hasSpell = spell != null;

            slot.GetComponent<Image>().color = blocked ? blockedColor
                : (hasWeapon || hasSpell) ? filledColor : normalColor;

            var iconImg = slot.transform.Find("Icon")?.GetComponent<Image>();
            var letterTxt = slot.transform.Find("Letter")?.GetComponent<Text>();
            if (iconImg == null) continue;

            if (blocked)
            {
                iconImg.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                if (letterTxt != null) { letterTxt.text = "✕"; letterTxt.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); }
            }
            else if (hasSpell)
            {
                // ✅ Заклинание — показываем цветом заклинания
                iconImg.color = spell.projectileColor;
                if (letterTxt != null)
                {
                    letterTxt.text = !string.IsNullOrEmpty(spell.spellName) ? spell.spellName[0].ToString() : "З";
                    letterTxt.color = Color.white;
                }
            }
            else if (hasWeapon)
            {
                Item item = inventory.equippedItems[slotType];
                iconImg.color = GetItemColor(item);
                if (letterTxt != null)
                {
                    letterTxt.text = !string.IsNullOrEmpty(item.itemName) ? item.itemName[0].ToString() : "?";
                    letterTxt.color = new Color(1, 1, 1, 0.9f);
                }
            }
            else
            {
                iconImg.color = new Color(0, 0, 0, 0);
                if (letterTxt != null)
                {
                    foreach (var s in slots) if (s.allowedType == slotType) { letterTxt.text = s.allowedType[0].ToString(); break; }
                    letterTxt.color = new Color(1, 1, 1, 0.18f);
                }
            }
        }
    }

    void Update()
    {
        // Escape обрабатывается в PauseMenu (закрывает всё сразу)
        // Открывается только через меню паузы
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
        if (open) RefreshEquipmentUI();
        else if (tooltip != null) tooltip.SetActive(false);
    }
}