using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair Settings")]
    public Color crosshairColor = new Color(1f, 1f, 1f, 0.85f);
    public float size = 12f;
    public float thickness = 2f;
    public float gap = 4f;

    private GameObject canvasGO;
    private GameObject crosshairRoot;

    void Start()
    {
        CreateCrosshair();
    }

    void Update()
    {
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

        MakeLine(crosshairRoot.transform, new Vector2(-(gap + size / 2f), 0), new Vector2(size, thickness));
        MakeLine(crosshairRoot.transform, new Vector2(gap + size / 2f, 0), new Vector2(size, thickness));
        MakeLine(crosshairRoot.transform, new Vector2(0, gap + size / 2f), new Vector2(thickness, size));
        MakeLine(crosshairRoot.transform, new Vector2(0, -(gap + size / 2f)), new Vector2(thickness, size));
        MakeLine(crosshairRoot.transform, Vector2.zero, new Vector2(thickness, thickness));
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