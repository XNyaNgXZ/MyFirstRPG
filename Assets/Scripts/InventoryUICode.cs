using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryUICode : MonoBehaviour
{
    public static bool IsOpen = false;
    public static InventoryUICode Instance { get; private set; }

    [Header("Drop Sound")]
    public AudioClip dropSound;
    public float dropVolume = 0.4f;

    private Inventory inventory;
    private GameObject inventoryPanel;
    private Transform contentPanel;
    private GameObject tooltip;
    private Text tooltipText;
    private RectTransform tooltipRect;

    private int gridColumns = 5;
    private int gridRows = 5;
    private float cellSize = 68f;
    private float cellSpacing = 5f;

    void Start()
    {
        inventory = GetComponent<Inventory>();
        Instance = this;
        if (inventory == null)
        {
            Debug.LogError("InventoryUICode: на Player нет компонента Inventory!");
            return;
        }

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        CreateInventoryUI();
        inventoryPanel.SetActive(false);
    }

    void CreateInventoryUI()
    {
        GameObject canvasGO = new GameObject("InventoryCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Точный размер панели под сетку
        float gridW = gridColumns * cellSize + (gridColumns - 1) * cellSpacing;
        float gridH = gridRows * cellSize + (gridRows - 1) * cellSpacing;
        float pad = 12f;
        float titleH = 36f;
        float panelW = gridW + pad * 2;
        float panelH = gridH + pad * 2 + titleH;

        inventoryPanel = new GameObject("InventoryPanel");
        inventoryPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRect = inventoryPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.sizeDelta = new Vector2(panelW, panelH);
        panelRect.anchoredPosition = new Vector2(-20f, 20f);
        inventoryPanel.AddComponent<Image>().color = new Color(0.13f, 0.13f, 0.16f, 1f);

        // Заголовок
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(inventoryPanel.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, titleH);
        titleRect.anchoredPosition = Vector2.zero;
        Text titleTxt = titleGO.AddComponent<Text>();
        titleTxt.text = "ИНВЕНТАРЬ  [TAB]";
        titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 14;
        titleTxt.color = new Color(0.65f, 0.65f, 0.75f, 1f);
        titleTxt.alignment = TextAnchor.MiddleCenter;

        // Разделитель
        GameObject lineGO = new GameObject("Line");
        lineGO.transform.SetParent(inventoryPanel.transform, false);
        RectTransform lineRect = lineGO.AddComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0, 1);
        lineRect.anchorMax = new Vector2(1, 1);
        lineRect.pivot = new Vector2(0.5f, 1);
        lineRect.sizeDelta = new Vector2(-pad * 2, 1);
        lineRect.anchoredPosition = new Vector2(0, -titleH);
        lineGO.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.38f, 1f);

        // Контейнер сетки — строго по размеру ячеек, по центру
        GameObject content = new GameObject("Content");
        content.transform.SetParent(inventoryPanel.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(gridW, gridH);
        // Смещаем вниз на половину заголовка чтобы сетка была по центру оставшегося места
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

        contentPanel = content.transform;

        // Тултип
        tooltip = new GameObject("Tooltip");
        tooltip.transform.SetParent(canvasGO.transform, false);
        tooltipRect = tooltip.AddComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(190, 90);
        tooltipRect.pivot = new Vector2(0, 1);
        tooltip.AddComponent<Image>().color = new Color(0.07f, 0.07f, 0.1f, 1f);

        GameObject ttTextGO = new GameObject("TooltipText");
        ttTextGO.transform.SetParent(tooltip.transform, false);
        RectTransform ttRect = ttTextGO.AddComponent<RectTransform>();
        ttRect.anchorMin = Vector2.zero;
        ttRect.anchorMax = Vector2.one;
        ttRect.offsetMin = new Vector2(10, 8);
        ttRect.offsetMax = new Vector2(-10, -8);
        tooltipText = ttTextGO.AddComponent<Text>();
        tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tooltipText.color = Color.white;
        tooltipText.fontSize = 13;
        tooltipText.alignment = TextAnchor.UpperLeft;
        tooltipText.supportRichText = true;

        tooltip.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            IsOpen = !IsOpen;
            inventoryPanel.SetActive(IsOpen);

            if (IsOpen)
            {
                RefreshInventoryUI();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                HideTooltip();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        // Тултип следует за курсором
        if (tooltip != null && tooltip.activeSelf)
        {
            Vector2 mp = Input.mousePosition;
            float ox = 15f, oy = -15f;
            if (mp.x + tooltipRect.sizeDelta.x + ox > Screen.width) ox = -tooltipRect.sizeDelta.x - 5f;
            if (mp.y + oy - tooltipRect.sizeDelta.y < 0) oy = tooltipRect.sizeDelta.y + 5f;
            tooltipRect.position = new Vector3(mp.x + ox, mp.y + oy, 0);
        }
    }

    void RefreshInventoryUI()
    {
        foreach (Transform child in contentPanel)
            Destroy(child.gameObject);

        int total = gridColumns * gridRows;
        for (int i = 0; i < total; i++)
        {
            Item item = i < inventory.items.Count ? inventory.items[i] : null;
            CreateSlot(item, i);
        }
    }

    void CreateSlot(Item item, int slotIndex)
    {
        GameObject slotGO = new GameObject(item != null ? item.itemName : $"Slot_{slotIndex}");
        slotGO.transform.SetParent(contentPanel, false);

        Image slotBg = slotGO.AddComponent<Image>();
        slotBg.color = item != null
            ? new Color(0.28f, 0.28f, 0.33f, 1f)
            : new Color(0.20f, 0.20f, 0.24f, 1f);

        if (item != null)
        {
            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(slotGO.transform, false);
            RectTransform iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            Image iconImg = iconGO.AddComponent<Image>();
            iconImg.color = item.itemType switch
            {
                "Potion" => new Color(0.15f, 0.72f, 0.33f, 1f),
                "Weapon" => new Color(0.82f, 0.22f, 0.22f, 1f),
                "Armour" => new Color(0.22f, 0.48f, 0.85f, 1f),
                _ => new Color(0.75f, 0.65f, 0.18f, 1f),
            };

            GameObject letterGO = new GameObject("Letter");
            letterGO.transform.SetParent(iconGO.transform, false);
            RectTransform lRect = letterGO.AddComponent<RectTransform>();
            lRect.anchorMin = Vector2.zero;
            lRect.anchorMax = Vector2.one;
            lRect.offsetMin = Vector2.zero;
            lRect.offsetMax = Vector2.zero;
            Text lTxt = letterGO.AddComponent<Text>();
            lTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lTxt.fontSize = 24;
            lTxt.fontStyle = FontStyle.Bold;
            lTxt.color = new Color(1f, 1f, 1f, 0.45f);
            lTxt.alignment = TextAnchor.MiddleCenter;
            lTxt.text = item.itemType.Length > 0 ? item.itemType[0].ToString() : "?";

            EventTrigger trigger = slotGO.AddComponent<EventTrigger>();
            Item capturedItem = item;
            int capturedIndex = slotIndex;

            var onEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            onEnter.callback.AddListener((_) => ShowTooltip(capturedItem));
            trigger.triggers.Add(onEnter);

            var onExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            onExit.callback.AddListener((_) => HideTooltip());
            trigger.triggers.Add(onExit);

            var onClick = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            onClick.callback.AddListener((data) =>
            {
                var ped = (PointerEventData)data;
                if (ped.button == PointerEventData.InputButton.Left)
                {
                    HideTooltip();
                    if (item.itemType == "Potion")
                    {
                        inventory.UseItem(capturedIndex);
                        RefreshInventoryUI();
                    }
                    else if (item.itemType == "Weapon")
                    {
                        inventory.EquipWeapon(item);
                        RefreshInventoryUI(); // Обновим инвентарь, то есть оружие исчезнет из списка
                    }
                    else
                    {
                        Debug.Log($"Нельзя использовать предмет типа {item.itemType}");
                    }
                }
                else if (ped.button == PointerEventData.InputButton.Right)
                {
                    HideTooltip();
                    // Оставим пока просто выбрасывание также и оружия (как и зелья) ПОКА ЧТО
                    DropItemToWorld(capturedIndex);
                    RefreshInventoryUI();
                }
            });
            trigger.triggers.Add(onClick);

            Button btn = slotGO.AddComponent<Button>();
            btn.targetGraphic = slotBg;
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(0.28f, 0.28f, 0.33f);
            cb.highlightedColor = new Color(0.38f, 0.38f, 0.45f);
            cb.pressedColor = new Color(0.18f, 0.18f, 0.22f);
            btn.colors = cb;
        }
    }

    void DropItemToWorld(int index)
    {
        Item item = inventory.TakeItem(index);
        if (item == null) return;

        Vector3 dropPos = transform.position + transform.forward * 1.2f + Vector3.up * 0.5f;

        GameObject dropped = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dropped.name = item.itemName;
        dropped.transform.position = dropPos;
        dropped.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        dropped.tag = "Item";

        ItemData data = dropped.AddComponent<ItemData>();
        data.itemName = item.itemName;
        data.itemType = item.itemType;
        data.value = item.value;

        Rigidbody rb = dropped.AddComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * 2f + Vector3.up * 1.5f;

        Renderer rend = dropped.GetComponent<Renderer>();
        if (rend != null)
        {
            //rend.material = new Material(Shader.Find("Standard"));
            rend.material.color = item.itemType 
            switch
            {
                "Potion" => new Color(0.15f, 0.72f, 0.33f),
                "Weapon" => new Color(0.82f, 0.22f, 0.22f),
                "Armour" => new Color(0.22f, 0.48f, 0.85f),
                _ => new Color(0.75f, 0.65f, 0.18f),
            };
        }

        Debug.Log($"Выброшен предмет: {item.itemName}");

        if (dropSound != null)
        {
            AudioSource.PlayClipAtPoint(dropSound, transform.position, dropVolume);
        }
    }

    void ShowTooltip(Item item)
    {
        tooltip.SetActive(true);
        string typeLabel = item.itemType switch
        {
            "Potion" => "<color=#3dcc66>Зелье</color>",
            "Weapon" => "<color=#e03535>Оружие</color>",
            "Armour" => "<color=#3579e0>Броня</color>",
            _ => item.itemType
        };
        string valueLabel = item.itemType switch
        {
            "Potion" => $"Лечение: <color=#3dcc66>+{item.value} HP</color>",
            "Weapon" => $"Урон: <color=#e03535>{item.value}</color>",
            "Armour" => $"Защита: <color=#3579e0>{item.value}</color>",
            _ => $"Значение: {item.value}"
        };
        tooltipText.text = $"<b>{item.itemName}</b>\n{typeLabel}  {valueLabel}\n" +
                           $"<color=#666666>ЛКМ — использовать  |  ПКМ — выбросить</color>";
    }

    void HideTooltip()
    {
        if (tooltip != null) tooltip.SetActive(false);
    }
    public static void RefreshIfOpen()
    {
        if (IsOpen && Instance != null)
        {
            Instance.RefreshInventoryUI();
        }
    }
}