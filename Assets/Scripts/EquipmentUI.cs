using System.Collections;
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

    private Dictionary<string, GameObject> slotObjects = new Dictionary<string, GameObject>();
    private Dictionary<string, Item> currentEquipment; // будем брать из inventory

    private struct SlotDef
    {
        public string name; // имя слота (в тултипе)
        public string allowedType; // тип предмета, который надевается (Weapon, Helmet e.t.c.)
    }

    private SlotDef[] slots = new SlotDef[]
    {
        new SlotDef { name = "Оружие", allowedType = "Weapon"},
        new SlotDef { name = "Шлем", allowedType = "Helmet"},
        new SlotDef { name = "Нагруник", allowedType = "Chest"},
        new SlotDef { name = "Поножи", allowedType = "Legs"},
        new SlotDef { name = "Ботинки", allowedType = "Boots"},
        new SlotDef { name = "Щит", allowedType = "Shield"},
        new SlotDef { name = "Кольцо 1", allowedType = "Ring"},
        new SlotDef { name = "Кольцо 2", allowedType = "Ring"},
        new SlotDef { name = "Кольцо 3", allowedType = "Ring"},
        new SlotDef { name = "Кольцо 4", allowedType = "Ring"},
        new SlotDef { name = "Амулет", allowedType = "Amulet"},
    };

    private int gridColumns = 4; // сколько слотов в строке
    private float cellSize = 85f;
    private float cellSpacing = 8f;
    private float pad = 14f;
    private float titleH = 36f;

    private float inventoryPanelH = 420f;
    private float edgeOffset = 20f;

    void Start()
    {
        inventory = GetComponent<Inventory>();
        if( inventory == null)
        {
            inventory = FindAnyObjectByType<Inventory>();
        }
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
        CreateEquipmentUI();
        equipmentPanel.SetActive(false);
    }

    void CreateEquipmentUI()
    {
        //Canvas
        equipmentCanvas = new GameObject("EquipmentCanvas");
        Canvas canvas = equipmentCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = equipmentCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        equipmentCanvas.AddComponent<GraphicRaycaster>();

        int rows = Mathf.CeilToInt((float)slots.Length / gridColumns);
        float gridW = gridColumns * cellSize + (gridColumns - 1) * cellSpacing;
        float gridH = rows * cellSize + (rows - 1) * cellSpacing;
        float panelW = gridW + pad * 2;
        float panelH = gridH + pad * 2 + titleH;

        //Панель (фон)
        equipmentPanel = new GameObject("EquipmentPanel");
        equipmentPanel.transform.SetParent(equipmentCanvas.transform, false);
        RectTransform panelRect = equipmentPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.sizeDelta= new Vector2(panelW, panelH);
        panelRect.anchoredPosition = new Vector2(
            -edgeOffset, edgeOffset + inventoryPanelH + 10f  // 10px зазор между панелями
        );
        equipmentPanel.AddComponent<Image>().color = new Color(0.13f, 0.13f, 0.16f, 1f);

        // заголовок
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(equipmentPanel.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, titleH);
        titleRect.anchoredPosition = Vector2.zero;
        Text titleTxt = titleGO.AddComponent<Text>();
        titleTxt.text = "СНАРЯЖЕНИЕ  [I]";
        titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 18;
        titleTxt.color = new Color(0.65f, 0.65f, 0.75f, 1f);
        titleTxt.alignment = TextAnchor.MiddleCenter;

        // Разделитель
        GameObject lineGO = new GameObject("Line");
        lineGO.transform.SetParent(equipmentPanel.transform, false);
        RectTransform lineRect = lineGO.AddComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0, 1);
        lineRect.anchorMax = new Vector2(1, 1);
        lineRect.pivot = new Vector2(0.5f, 1);
        lineRect.sizeDelta = new Vector2(-pad * 2, 1);
        lineRect.anchoredPosition = new Vector2(0, -titleH);
        lineGO.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.38f, 1f);

        //контейнер для слотов
        GameObject content = new GameObject("Content");
        content.transform.SetParent(equipmentPanel.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(gridW, gridH);
        contentRect.anchoredPosition = new Vector2(0, -(titleH / 2f));

        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(cellSize, cellSize);
        grid.spacing = new Vector2(cellSpacing, cellSpacing);
        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = gridColumns;

        slotsContainer = content.transform;

        // создание слотов
        for (int i = 0; i < slots.Length; i++)
        {
            CreateSlot(i, slots[i]);
        }

        // Тултип
        tooltip = new GameObject("Tooltip");
        tooltip.transform.SetParent(equipmentCanvas.transform, false);
        tooltipRect = tooltip.AddComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(180, 60);
        tooltipRect.pivot = new Vector2(0, 1);
        tooltip.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.1f, 1f);

        GameObject ttTextGO = new GameObject("TooltipText");
        ttTextGO.transform.SetParent(tooltip.transform, false);
        RectTransform ttRect = ttTextGO.AddComponent<RectTransform>();
        ttRect.anchorMin = Vector2.zero; ttRect.anchorMax = Vector2.one;
        ttRect.offsetMin = new Vector2(8, 6); ttRect.offsetMax = new Vector2(-8, -6);
        tooltipText = ttTextGO.AddComponent<Text>();
        tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tooltipText.color = Color.white;
        tooltipText.fontSize = 12;
        tooltipText.supportRichText = true;
        tooltipText.alignment = TextAnchor.UpperLeft;
        tooltip.SetActive(false);
    }
    void CreateSlot(int idx, SlotDef slotDef)
    {
        Color normalColor = new Color(0.20f, 0.20f, 0.24f, 1f);
        Color highlightColor = new Color(0.30f, 0.30f, 0.36f, 1f);
        Color pressColor = new Color(0.40f, 0.35f, 0.20f, 1f);

        GameObject slotGO = new GameObject($"Slot_{slotDef.name}");
        slotGO.transform.SetParent(slotsContainer, false);
        slotObjects[slotDef.allowedType] = slotGO;

        //Фон Слота
        Image bgImage = slotGO.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.24f, 1f);

        // Подпись слота снизу
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(slotGO.transform, false);
        RectTransform labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 0);
        labelRect.pivot = new Vector2(0.5f, 0);
        labelRect.sizeDelta = new Vector2(0, 20);
        labelRect.anchoredPosition = Vector2.zero;
        Text labelTxt = labelGO.AddComponent<Text>();
        labelTxt.text = slotDef.name;
        labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelTxt.fontSize = 10;
        labelTxt.color = new Color(0.6f, 0.6f, 0.7f, 1f);
        labelTxt.alignment = TextAnchor.MiddleCenter;

        // Буква типа по центру
        GameObject letterGO = new GameObject("Letter");
        letterGO.transform.SetParent(slotGO.transform, false);
        RectTransform letterRect = letterGO.AddComponent<RectTransform>();
        letterRect.anchorMin = new Vector2(0, 0.2f);
        letterRect.anchorMax = new Vector2(1, 1);
        letterRect.offsetMin = Vector2.zero;
        letterRect.offsetMax = Vector2.zero;
        Text letterTxt = letterGO.AddComponent<Text>();
        letterTxt.text = slotDef.allowedType.Length > 0
                                ? slotDef.allowedType[0].ToString() : "?";
        letterTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        letterTxt.fontSize = 26;
        letterTxt.fontStyle = FontStyle.Bold;
        letterTxt.color = new Color(1f, 1f, 1f, 0.18f);
        letterTxt.alignment = TextAnchor.MiddleCenter;

        //добавление кнопки для возможности взаимодействия
        Button btn = slotGO.AddComponent<Button>();
        btn.targetGraphic = bgImage;
        btn.transition = Selectable.Transition.None;

        EventTrigger et = slotGO.AddComponent<EventTrigger>();

        var onEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        onEnter.callback.AddListener((_) => {
            bgImage.color = highlightColor;
            // Показываем тултип
            Item equipped = inventory?.GetEquippedItem(slotDef.allowedType);
            if (tooltip != null)
            {
                tooltipText.text = equipped != null
                    ? $"<b>{equipped.itemName}</b>\nЗащита: {equipped.value}\nЛКМ — снять"
                    : $"<b>{slotDef.name}</b>\n<color=#666666>Пусто</color>";
                tooltip.SetActive(true);
            }
        });
        et.triggers.Add(onEnter);

        var onExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        onExit.callback.AddListener((_) => {
            bgImage.color = normalColor;
            if (tooltip != null) tooltip.SetActive(false);
        });
        et.triggers.Add(onExit);

        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.2f, 0.2f, 0.24f);
        cb.highlightedColor = new Color(0.3f, 0.3f, 0.36f);
        cb.pressedColor = new Color(0.15f, 0.15f, 0.18f);
        btn.colors = cb;

        btn.onClick.AddListener(() =>
        {
            if (inventory != null)
            {
                Item equipped = inventory.GetEquippedItem(slotDef.allowedType);
                if (equipped != null)
                {
                    inventory.UnequipItem(slotDef.allowedType);
                    RefreshEquipmentUI();
                }
                else
                {
                    Debug.Log($"Слот {slotDef.name} пуст");
                }
            }
        });

    }
    // Кратковспышка цвета при клике, затем возврат
    IEnumerator FlashSlot(Image img, Color flash, Color normal)
    {
        img.color = flash;
        yield return new WaitForSeconds(0.10f);
        img.color = normal;
    }

    public static EquipmentUI Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }
    public static void RefreshIfOpen()
    {
        if (IsOpen && Instance != null)
        {
            Instance.RefreshEquipmentUI();
        }
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

            Image bg = slot.GetComponent<Image>();
            bg.color = item != null ? new Color(0.28f, 0.28f, 0.33f) : new Color(0.20f, 0.20f, 0.24f);

            Transform iconTransform = slot.transform.Find("Icon");
            if (iconTransform == null)
            {
                GameObject iconGO = new GameObject("Icon");
                iconGO.transform.SetParent(slot.transform, false);
                RectTransform iconRect = iconGO.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.1f, 0.1f);
                iconRect.anchorMax = new Vector2(0.9f, 0.9f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                Image iconImg = iconGO.AddComponent<Image>();
                iconTransform = iconGO.transform;
            }

            Image iconImage = iconTransform.GetComponent<Image>();
            Text letterText = iconTransform.Find("Letter")?.GetComponent<Text>();
            if (iconImage != null)
            {
                if (item != null)
                {
                    // --- Цвет в зависимости от типа ---
                    Color equipColor;
                    switch (item.itemType)
                    {
                        case "Weapon":
                        case "Shield":
                            equipColor = new Color(0.82f, 0.22f, 0.22f);
                            break;
                        case "Helmet":
                        case "Chest":
                        case "Legs":
                        case "Boots":
                            equipColor = new Color(0.22f, 0.48f, 0.85f);
                            break;
                        case "Ring":
                        case "Amulet":
                            equipColor = new Color(75f / 255f, 0f, 130f / 255f);
                            break;
                        default:
                            equipColor = Color.white;
                            break;
                    }
                    iconImage.color = equipColor;

                    if (letterText != null)
                    {
                        letterText.text = item.itemName.Length > 0 ? item.itemName[0].ToString() : "?";
                        letterText.gameObject.SetActive(true);
                    }
                }
                else
                {
                    iconImage.color = new Color(0.2f, 0.2f, 0.24f, 0.5f);
                    if (letterText != null) letterText.gameObject.SetActive(false);
                }
            }
        }
    }

    void Update()
    {
        // EquipmentUI Update():
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
            }
        }

        if (tooltip != null && tooltip.activeSelf)
        {
            Vector2 mp = Input.mousePosition;
            float ox = 15f, oy = -15f;
            if (mp.x + tooltipRect.sizeDelta.x + ox > Screen.width) ox = -tooltipRect.sizeDelta.x - 5f;
            if (mp.y + oy - tooltipRect.sizeDelta.y < 0) oy = tooltipRect.sizeDelta.y + 5f;
            tooltipRect.position = new Vector3(mp.x + ox, mp.y + oy, 0);
        }
    }
    public void SetOpen(bool open)
    {
        if (equipmentPanel == null) return;
        IsOpen = open;
        equipmentPanel.SetActive(open);
        if (open)
        {
            RefreshEquipmentUI(); // при открытии обновляем содержимое
        }
    }
}
