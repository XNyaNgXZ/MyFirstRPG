using UnityEngine;

public class MouseInteractor : MonoBehaviour
{
    public float interactionRange = 5f;
    public float pickupCooldown = 0.8f;
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float pickupVolume = 0.4f;

    private Camera playerCamera;
    private Inventory inventory;
    private float lastPickupTime = -10f;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        inventory = GetComponent<Inventory>();
    }

    void Update()
    {
        if (InventoryUICode.IsOpen || EquipmentUI.IsOpen) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (Time.time < lastPickupTime + pickupCooldown) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange)) return;

        ItemData data = hit.collider.GetComponent<ItemData>();
        if (data == null || inventory == null) return;

        // ✅ Читаем данные через свойства — они сами выберут карточку или ручные данные
        Color col = data.Color;
        Vector3 scale = data.Scale;

        // Если цвет не задан — берём с рендерера объекта
        if (col == Color.white || col == default)
        {
            var rend = hit.collider.GetComponent<Renderer>();
            if (rend != null) col = rend.material.color;
        }

        // Читаем текстуру с карточки если есть
        Texture2D tex = data.definition != null ? data.definition.worldTexture : null;

        Item newItem = new Item(data.Name, data.Type, data.Value, col, scale);
        newItem.worldTexture = tex; // ✅

        if (!inventory.AddItem(newItem)) return;

        lastPickupTime = Time.time;
        InventoryUICode.RefreshIfOpen();

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);

        Destroy(hit.collider.gameObject);

        if (HandController.Instance != null)
            HandController.Instance.PlayPickup();
    }
}