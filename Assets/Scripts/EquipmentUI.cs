using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentUI : MonoBehaviour
{
    [Header("Материал для выброшенных предметов")]
    public Material retroMaterial;

    private GameObject tooltip;
    private Text tooltipText;
    private RectTransform tooltipRect;
    public static bool IsOpen { get; private set; } = false;
    private GameObject equipmentCanvas;
    private GameObject equipmentPanel;
    private Transform slotsContainer;
    private Inventory inventory;

    private Dictionary<string, GameObject> slotObjects = new Dictionary<string, GameObject>();
    private List<(RectTransform rect, string type)> slotRectList
        = new List<(RectTransform, string)>();

    private struct SlotDef
    {
        public string name;
        public string allowedType;
    }

    private SlotDef[] slots = new SlotDef[]
    {
        new SlotDef { name = "Оружие",    allowedType = "Weapon"  },
        new SlotDef { name = "Шлем",      allowedType = "Helmet"  },
        new SlotDef { name = "Нагрудник", allowedType = "Chest"   },
        new SlotDef { name = "Поножи",    allowedType = "Legs"    },
        new SlotDef { name = "Ботинки",   allowedType = "Boots"   },
        new SlotDef { name = "Щит",       allowedType = "Shield"  },
        new SlotDef { name = "Кольцо 1",  allowedType = "Ring"    },
        new SlotDef { name = "Кольцо 2",  allowedType = "Ring"    },
        new SlotDef { name = "Кольцо 3",  allowedType = "Ring"    },
        new SlotDef { name = "Кольцо 4",  allowedType = "Ring"    },
        new SlotDef { name = "Амулет",    allowedType = "Amulet"  },
    };

    private int gridColumns = 4;
    private float cellSize = 85f;
    private float cellSpacing = 8f;
    private float pad = 14f;
    private float titleH = 36f;
    private float inventoryPanelH = 420f;
    private float edgeOffset = 20f;

    public static EquipmentUI Instance { get; private set; }
    void Awake() => Instance = this;

    void Start()
    {
        inventory = GetComponent<Inventory>() ?? FindAnyObjectByType<Inventory>();

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        CreateEquipmentUI();
        equipmentPanel.SetActive(false);
    }

    void CreateEquipmentUI()
    {
        equipmentCanvas = new GameObject("EquipmentCanvas");
        Canvas canvas = equipmentCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = equipmentCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        equipmentCanvas.AddComponent<GraphicRaycaster>();

        int rows = Mathf.CeilToInt((float)slots.Length / gridColumns);
        float gridW = gridColumns * cellSize + (gridColumns - 1) * cellSpacing;
        float gridH = rows * cellSize + (rows - 1) * cellSpacing;
        float panelW = gridW + pad * 2;
        float panelH = gridH + pad * 2 + titleH;

        equipmentPanel = new GameObject("EquipmentPanel");
        equipmentPanel.transform.SetParent(equipmentCanvas.transform, false);
        var panelRect = equipmentPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.sizeDelta = new Vector2(panelW, panelH);
        panelRect.anchoredPosition = new Vector2(-edgeOffset, edgeOffset + inventoryPanelH + 10f);
        equipmentPanel.AddComponent<Image>().color = new Color(0.13f, 0.13f, 0.16f, 1f);

        // Заголовок
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(equipmentPanel.transform, false);
        var tr = titleGO.AddComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 1); tr.anchorMax = new Vector2(1, 1);
        tr.pivot = new Vector2(0.5f, 1);
        tr.sizeDelta = new Vector2(0, titleH); tr.anchoredPosition = Vector2.zero;
        var tt = titleGO.AddComponent<Text>();
        tt.text = "СНАРЯЖЕНИЕ  [I]";
        tt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tt.fontSize = 18; tt.color = new Color(0.65f, 0.65f, 0.75f);
        tt.alignment = TextAnchor.MiddleCenter;

        // Разделитель
        var lineGO = new GameObject("Line");
        lineGO.transform.SetParent(equipmentPanel.transform, false);
        var lr = lineGO.AddComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 1); lr.anchorMax = new Vector2(1, 1);
        lr.pivot = new Vector2(0.5f, 1);
        lr.sizeDelta = new Vector2(-pad * 2, 1);
        lr.anchoredPosition = new Vector2(0, -titleH);
        lineGO.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.38f, 1f);

        // Контейнер слотов
        var content = new GameObject("Content");
        content.transform.SetParent(equipmentPanel.transform, false);
        var cr = content.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.5f, 0.5f); cr.anchorMax = new Vector2(0.5f, 0.5f);
        cr.pivot = new Vector2(0.5f, 0.5f);
        cr.sizeDelta = new Vector2(gridW, gridH);
        cr.anchoredPosition = new Vector2(0, -(titleH / 2f));

        var grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(cellSize, cellSize);
        grid.spacing = new Vector2(cellSpacing, cellSpacing);
        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = gridColumns;
        slotsContainer = content.transform;

        for (int i = 0; i < slots.Length; i++)
            CreateSlot(i, slots[i]);

        // Тултип
        tooltip = new GameObject("EqTooltip");
        tooltip.transform.SetParent(equipmentCanvas.transform, false);
        tooltipRect = tooltip.AddComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(190, 70);
        tooltipRect.pivot = new Vector2(0, 1);
        tooltip.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.1f, 1f);

        var ttGO = new GameObject("TooltipText");
        ttGO.transform.SetParent(tooltip.transform, false);
        var ttr = ttGO.AddComponent<RectTransform>();
        ttr.anchorMin = Vector2.zero; ttr.anchorMax = Vector2.one;
        ttr.offsetMin = new Vector2(8, 6); ttr.offsetMax = new Vector2(-8, -6);
        tooltipText = ttGO.AddComponent<Text>();
        tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tooltipText.color = Color.white; tooltipText.fontSize = 12;
        tooltipText.supportRichText = true;
        tooltipText.alignment = TextAnchor.UpperLeft;
        tooltip.SetActive(false);
    }

    void CreateSlot(int idx, SlotDef slotDef)
    {
        Color normalColor = new Color(0.20f, 0.20f, 0.24f, 1f);

        var slotGO = new GameObject($"Slot_{slotDef.name}");
        slotGO.transform.SetParent(slotsContainer, false);
        slotObjects[slotDef.allowedType] = slotGO;

        Image bgImage = slotGO.AddComponent<Image>();
        bgImage.color = normalColor;

        // Иконка (за текстом)
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(slotGO.transform, false);
        iconGO.transform.SetSiblingIndex(0);
        var ir = iconGO.AddComponent<RectTransform>();
        ir.anchorMin = new Vector2(0.05f, 0.05f);
        ir.anchorMax = new Vector2(0.95f, 0.95f);
        ir.offsetMin = ir.offsetMax = Vector2.zero;
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.color = new Color(0, 0, 0, 0);

        // Подпись снизу
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(slotGO.transform, false);
        var labr = labelGO.AddComponent<RectTransform>();
        labr.anchorMin = new Vector2(0, 0); labr.anchorMax = new Vector2(1, 0);
        labr.pivot = new Vector2(0.5f, 0);
        labr.sizeDelta = new Vector2(0, 20); labr.anchoredPosition = Vector2.zero;
        var labt = labelGO.AddComponent<Text>();
        labt.text = slotDef.name;
        labt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labt.fontSize = 10; labt.color = new Color(0.6f, 0.6f, 0.7f);
        labt.alignment = TextAnchor.MiddleCenter;

        // Буква типа
        var letterGO = new GameObject("Letter");
        letterGO.transform.SetParent(slotGO.transform, false);
        var letr = letterGO.AddComponent<RectTransform>();
        letr.anchorMin = new Vector2(0, 0.2f); letr.anchorMax = Vector2.one;
        letr.offsetMin = letr.offsetMax = Vector2.zero;
        var lett = letterGO.AddComponent<Text>();
        lett.text = slotDef.allowedType.Length > 0
                    ? slotDef.allowedType[0].ToString() : "?";
        lett.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lett.fontSize = 26; lett.fontStyle = FontStyle.Bold;
        lett.color = new Color(1f, 1f, 1f, 0.18f);
        lett.alignment = TextAnchor.MiddleCenter;

        Button btn = slotGO.AddComponent<Button>();
        btn.targetGraphic = bgImage;
        btn.transition = Selectable.Transition.None;

        var et = slotGO.AddComponent<EventTrigger>();

        // ── Наведение ────────────────────────────────────────────────────
        var onEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        onEnter.callback.AddListener((_) =>
        {
            bgImage.color = new Color(0.30f, 0.30f, 0.36f, 1f);
            Item equipped = inventory?.GetEquippedItem(slotDef.allowedType);
            if (tooltip != null)
            {
                tooltipText.text = equipped != null
                    ? $"<b>{equipped.itemName}</b>\nЗащита: {equipped.value}\n" +
                      "ЛКМ — снять (если есть место)\nПКМ — выбросить"
                    : $"<b>{slotDef.name}</b>\n<color=#666666>Пусто</color>";
                tooltip.SetActive(true);
            }
        });
        et.triggers.Add(onEnter);

        // ── Уход мыши ────────────────────────────────────────────────────
        var onExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        onExit.callback.AddListener((_) =>
        {
            bgImage.color = normalColor;
            if (tooltip != null) tooltip.SetActive(false);
        });
        et.triggers.Add(onExit);

        // ── ЛКМ — снять (если есть место в инвентаре) ───────────────────
        btn.onClick.AddListener(() =>
        {
            if (inventory == null) return;
            Item equipped = inventory.GetEquippedItem(slotDef.allowedType);
            if (equipped != null)
            {
                inventory.UnequipItem(slotDef.allowedType);
                RefreshEquipmentUI();
            }
        });

        // ── ПКМ — выбросить в мир даже если инвентарь полон ─────────────
        var onRightClick = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        onRightClick.callback.AddListener((ev) =>
        {
            var ped = (PointerEventData)ev;
            if (ped.button != PointerEventData.InputButton.Right) return;
            if (inventory == null) return;

            Item equipped = inventory.GetEquippedItem(slotDef.allowedType);
            if (equipped == null) return;

            // ✅ Снимаем напрямую, минуя проверку инвентаря
            inventory.equippedItems.Remove(slotDef.allowedType);

            if (slotDef.allowedType == "Weapon" && HandController.Instance != null)
                HandController.Instance.HideWeaponModel();

            // Выбрасываем в мир
            Transform player = inventory.transform;
            Vector3 pos = player.position + player.forward * 1.5f + Vector3.up * 0.5f;
            var drop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            drop.transform.position = pos;
            drop.transform.localScale = Vector3.one * 0.4f;
            drop.name = equipped.itemName;
            drop.tag = "Item";

            var rend = drop.GetComponent<Renderer>();
            if (retroMaterial != null) rend.material = retroMaterial;
            rend.material.color = equipped.itemType switch
            {
                "Potion" => new Color(0.2f, 0.8f, 0.3f),
                "Weapon" or "Shield" => new Color(0.82f, 0.22f, 0.22f),
                "Helmet" or "Chest" or "Legs" or "Boots" => new Color(0.22f, 0.48f, 0.85f),
                "Ring" or "Amulet" => new Color(0.45f, 0f, 0.7f),
                _ => new Color(0.6f, 0.6f, 0.6f)
            };

            var data = drop.AddComponent<ItemData>();
            data.itemName = equipped.itemName;
            data.itemType = equipped.itemType;
            data.value = equipped.value;

            drop.AddComponent<Rigidbody>()
                .AddForce(player.forward * 3f + Vector3.up * 2f, ForceMode.Impulse);

            if (tooltip != null) tooltip.SetActive(false);
            RefreshEquipmentUI();
            InventoryUICode.RefreshIfOpen();
        });
        et.triggers.Add(onRightClick);

        // Сохраняем rect для drag & drop из инвентаря
        slotRectList.Add((slotGO.GetComponent<RectTransform>(), slotDef.allowedType));
    }

    // ✅ Возвращает тип слота под курсором (для drag из инвентаря)
    public string GetSlotTypeUnderMouse()
    {
        if (!IsOpen) return null;
        foreach (var (rect, type) in slotRectList)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                    rect, Input.mousePosition, null))
                return type;
        }
        return null;
    }

    public static void RefreshIfOpen()
    {
        if (IsOpen && Instance != null)
            Instance.RefreshEquipmentUI();
    }

    void RefreshEquipmentUI()
    {
        if (inventory == null) inventory = GetComponent<Inventory>();
        if (inventory == null) return;

        var equipped = inventory.equippedItems;

        foreach (var kvp in slotObjects)
        {
            string slotType = kvp.Key;
            GameObject slot = kvp.Value;
            Item item = equipped.ContainsKey(slotType) ? equipped[slotType] : null;

            slot.GetComponent<Image>().color = item != null
                ? new Color(0.28f, 0.28f, 0.33f)
                : new Color(0.20f, 0.20f, 0.24f);

            var iconImg = slot.transform.Find("Icon")?.GetComponent<Image>();
            var letterTxt = slot.transform.Find("Letter")?.GetComponent<Text>();

            if (iconImg != null)
            {
                if (item != null)
                {
                    iconImg.color = item.itemType switch
                    {
                        "Weapon" or "Shield" => new Color(0.82f, 0.22f, 0.22f),
                        "Helmet" or "Chest" or "Legs" or "Boots" => new Color(0.22f, 0.48f, 0.85f),
                        "Ring" or "Amulet" => new Color(75f / 255f, 0f, 130f / 255f),
                        _ => Color.white
                    };
                    if (letterTxt != null)
                    {
                        letterTxt.text = item.itemName.Length > 0
                                          ? item.itemName[0].ToString() : "?";
                        letterTxt.color = new Color(1f, 1f, 1f, 0.9f);
                    }
                }
                else
                {
                    iconImg.color = new Color(0, 0, 0, 0);
                    if (letterTxt != null)
                    {
                        foreach (var s in slots)
                            if (s.allowedType == slotType)
                            {
                                letterTxt.text = s.allowedType.Length > 0
                                                 ? s.allowedType[0].ToString() : "?";
                                break;
                            }
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
            IsOpen = !IsOpen;
            equipmentPanel.SetActive(IsOpen);
            if (IsOpen)
            {
                RefreshEquipmentUI();
                PlayerMovement.UnlockCursor();
            }
            else
            {
                PlayerMovement.LockCursor();
                if (tooltip != null) tooltip.SetActive(false);
            }
        }

        if (tooltip != null && tooltip.activeSelf)
        {
            Vector2 mp = Input.mousePosition;
            float ox = 15f, oy = -15f;
            if (mp.x + tooltipRect.sizeDelta.x + ox > Screen.width)
                ox = -tooltipRect.sizeDelta.x - 5f;
            if (mp.y + oy - tooltipRect.sizeDelta.y < 0)
                oy = tooltipRect.sizeDelta.y + 5f;
            tooltipRect.position = new Vector3(mp.x + ox, mp.y + oy, 0);
        }
    }

    public void SetOpen(bool open)
    {
        if (equipmentPanel == null) return;
        IsOpen = open;
        equipmentPanel.SetActive(open);
        if (open) RefreshEquipmentUI();
        else if (tooltip != null) tooltip.SetActive(false);
    }
}