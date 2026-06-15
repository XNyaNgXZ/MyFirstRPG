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

        // Свиток/книга заклинания
        SpellPickup spellPickup = hit.collider.GetComponent<SpellPickup>();
        if (spellPickup != null)
        {
            spellPickup.Pickup(inventory);
            lastPickupTime = Time.time;
            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
            if (HandController.Instance != null) HandController.Instance.PlayPickup();
            return;
        }

        // Обычный предмет
        ItemData data = hit.collider.GetComponent<ItemData>();
        if (data == null || inventory == null) return;

        // ✅ Всё берём из карточки через свойства ItemData (они уже приоритизируют definition)
        string itemName = data.Name;
        string itemType = data.Type;
        int itemValue = data.Value;
        Color itemColor = data.Color;
        Vector3 itemScale = data.Scale;
        Texture2D tex = data.definition != null ? data.definition.worldTexture : null;

        // Цвет — если белый/дефолт берём с рендерера
        if (itemColor == Color.white || itemColor == default)
        {
            var rend = hit.collider.GetComponent<Renderer>();
            if (rend != null) itemColor = rend.material.color;
        }

        Item newItem = new Item(itemName, itemType, itemValue, itemColor, itemScale);
        newItem.worldTexture = tex;

        // ✅ quantity = 1 для всего, кроме стрел где берём itemValue как количество пачки
        newItem.quantity = itemType == "Arrow" ? Mathf.Max(1, itemValue) : 1;

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