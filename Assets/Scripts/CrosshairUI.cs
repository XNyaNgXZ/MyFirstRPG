using UnityEngine;
using UnityEngine.UI;

// Повесь этот скрипт на Player.
// Автоматически создаёт прицел по центру экрана.
public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair Settings")]
    public Color crosshairColor = new Color(1f, 1f, 1f, 0.85f);
    public float size = 12f;        // размер перекрестья
    public float thickness = 2f;    // толщина линий
    public float gap = 4f;          // зазор в центре

    private GameObject canvasGO;
    private GameObject crosshairRoot;

    void Start()
    {
        CreateCrosshair();
    }

    void Update()
    {
        // Скрываем прицел когда открыт UI
        bool uiOpen = InventoryUICode.IsOpen || EquipmentUI.IsOpen;
        if (crosshairRoot != null)
            crosshairRoot.SetActive(!uiOpen);
    }

    void CreateCrosshair()
    {
        canvasGO = new GameObject("CrosshairCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        canvasGO.AddComponent<CanvasScaler>();

        crosshairRoot = new GameObject("Crosshair");
        crosshairRoot.transform.SetParent(canvasGO.transform, false);

        // Горизонтальная левая линия
        MakeLine(crosshairRoot.transform, new Vector2(-(gap + size / 2f), 0),
                 new Vector2(size, thickness));
        // Горизонтальная правая линия
        MakeLine(crosshairRoot.transform, new Vector2(gap + size / 2f, 0),
                 new Vector2(size, thickness));
        // Вертикальная верхняя линия
        MakeLine(crosshairRoot.transform, new Vector2(0, gap + size / 2f),
                 new Vector2(thickness, size));
        // Вертикальная нижняя линия
        MakeLine(crosshairRoot.transform, new Vector2(0, -(gap + size / 2f)),
                 new Vector2(thickness, size));

        // Точка по центру
        MakeLine(crosshairRoot.transform, Vector2.zero,
                 new Vector2(thickness, thickness));
    }

    void MakeLine(Transform parent, Vector2 pos, Vector2 sz)
    {
        GameObject go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sz;
        rt.anchoredPosition = pos;
        Image img = go.AddComponent<Image>();
        img.color = crosshairColor;
    }
}