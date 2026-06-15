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

        Color col = data.Color;
        Vector3 scale = data.Scale;

        if (col == Color.white || col == default)
        {
            var rend = hit.collider.GetComponent<Renderer>();
            if (rend != null) col = rend.material.color;
        }

        Texture2D tex = data.definition != null ? data.definition.worldTexture : null;

        Item newItem = new Item(data.Name, data.Type, data.Value, col, scale);
        newItem.worldTexture = tex;

        // ✅ Для стакаемых предметов quantity берём из Value карточки
        if (newItem.maxQuantity > 1)
            newItem.quantity = Mathf.Max(1, data.Value);

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