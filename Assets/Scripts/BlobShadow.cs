using UnityEngine;

public class BlobShadow : MonoBehaviour
{
    [Header("Настройки тени")]
    public float shadowAlpha = 0.6f;
    public float shadowScale = 0.8f;
    public float heightOffset = 0.02f;
    public LayerMask groundLayer = ~0;

    private GameObject shadowQuad;
    private Renderer shadowRenderer;
    private static Texture2D circleTexture;

    void Start() => CreateShadow();

    static Texture2D GetCircleTexture()
    {
        if (circleTexture != null) return circleTexture;

        int size = 64;
        circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - dist / radius);
                // Мягкие края
                alpha = Mathf.Pow(alpha, 1.5f);
                circleTexture.SetPixel(x, y, new Color(0, 0, 0, alpha));
            }
        circleTexture.Apply();
        return circleTexture;
    }

    void CreateShadow()
    {
        shadowQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        shadowQuad.name = "_BlobShadow";
        Destroy(shadowQuad.GetComponent<Collider>());

        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.mainTexture = GetCircleTexture();
        mat.color = new Color(0, 0, 0, shadowAlpha);
        mat.SetFloat("_Surface", 1);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        shadowRenderer = shadowQuad.GetComponent<Renderer>();
        shadowRenderer.material = mat;
        shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        shadowRenderer.receiveShadows = false;
    }

    void Update()
    {
        if (shadowQuad == null) return;

        Ray ray = new Ray(transform.position, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 10f, groundLayer))
        {
            shadowQuad.SetActive(true);
            shadowQuad.transform.position = hit.point + Vector3.up * heightOffset;
            shadowQuad.transform.rotation = Quaternion.Euler(90, 0, 0);

            Vector3 size = GetObjectSize();
            float scale = Mathf.Max(size.x, size.z) * shadowScale;
            float height = hit.distance;
            float fade = Mathf.Clamp01(1f - height / 5f);
            scale *= Mathf.Lerp(1.4f, 0.7f, fade);

            shadowQuad.transform.localScale = new Vector3(scale, scale, 1f);

            var col = shadowRenderer.material.color;
            col.a = shadowAlpha * fade;
            shadowRenderer.material.color = col;
        }
        else
        {
            shadowQuad.SetActive(false);
        }
    }

    Vector3 GetObjectSize()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) return col.bounds.size;
        Renderer rend = GetComponent<Renderer>();
        if (rend != null) return rend.bounds.size;
        return Vector3.one * 0.5f;
    }

    void OnDestroy()
    {
        if (shadowQuad != null) Destroy(shadowQuad);
    }
}