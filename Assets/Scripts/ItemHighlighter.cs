using UnityEngine;
using UnityEngine.UI;

// Повесь на Player
public class ItemHighlighter : MonoBehaviour
{
    [Header("Контур")]
    public float interactionRange = 3f;
    public Color outlineColor = new Color(1f, 0.92f, 0.3f, 1f);
    public float outlineWidth = 0.03f;

    [Header("Надпись над прицелом")]
    public Color labelColor = Color.white;
    public int labelFontSize = 14;
    public float labelOffsetY = 60f; // пикселей выше центра экрана

    private Camera playerCamera;
    private GameObject currentOutline;
    private Collider currentTarget;

    // UI
    private GameObject hudCanvas;
    private Text itemLabel;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        BuildLabel();
    }

    void BuildLabel()
    {
        hudCanvas = new GameObject("HighlightCanvas");
        DontDestroyOnLoad(hudCanvas);
        var cv = hudCanvas.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 9;
        var sc = hudCanvas.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);

        var go = new GameObject("ItemLabel");
        go.transform.SetParent(hudCanvas.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300f, 30f);
        rt.anchoredPosition = new Vector2(0f, labelOffsetY);

        itemLabel = go.AddComponent<Text>();
        itemLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        itemLabel.fontSize = labelFontSize;
        itemLabel.color = labelColor;
        itemLabel.alignment = TextAnchor.MiddleCenter;
        itemLabel.text = "";

        // Тень для читаемости
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.8f);
        shadow.effectDistance = new Vector2(1, -1);

        go.SetActive(false);
        itemLabel.gameObject.SetActive(false);
    }

    void Update()
    {
        Collider hitCollider = null;
        string itemName = "";

        if (playerCamera != null && !InventoryUICode.IsOpen && !EquipmentUI.IsOpen)
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange))
            {
                if (hit.collider.CompareTag("Item"))
                {
                    hitCollider = hit.collider;
                    var data = hit.collider.GetComponent<ItemData>();
                    itemName = data != null ? data.Name : hit.collider.name;
                }
            }
        }

        // Контур
        if (hitCollider != currentTarget)
        {
            RemoveOutline();
            currentTarget = hitCollider;
            if (hitCollider != null) CreateOutline(hitCollider.gameObject);
        }

        // Надпись
        if (itemLabel != null)
        {
            bool show = hitCollider != null && !string.IsNullOrEmpty(itemName);
            itemLabel.gameObject.SetActive(show);
            if (show) itemLabel.text = itemName;
        }
    }

    void CreateOutline(GameObject target)
    {
        var mf = target.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        currentOutline = new GameObject("_ItemOutline");
        currentOutline.transform.SetParent(target.transform, false);
        currentOutline.transform.localPosition = Vector3.zero;
        currentOutline.transform.localRotation = Quaternion.identity;
        currentOutline.transform.localScale = Vector3.one;

        currentOutline.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;

        var mr = currentOutline.AddComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Custom/ItemOutline"));
        if (mat != null)
        {
            mat.SetColor("_OutlineColor", outlineColor);
            mat.SetFloat("_OutlineWidth", outlineWidth);
        }
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    void RemoveOutline()
    {
        if (currentOutline != null) { Destroy(currentOutline); currentOutline = null; }
        currentTarget = null;
    }

    void OnDisable()
    {
        RemoveOutline();
        if (itemLabel != null) itemLabel.gameObject.SetActive(false);
    }
}