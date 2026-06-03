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
        if (playerCamera == null)
            Debug.LogError("MouseInteractor: Камера не найдена!");

        inventory = GetComponent<Inventory>();
        if (inventory == null)
            Debug.LogError("MouseInteractor: На Player нет компонента Inventory!");
    }

    void Update()
    {
        if (InventoryUICode.IsOpen || EquipmentUI.IsOpen) return;

        // E — подбор предмета
        if (Input.GetKeyDown(KeyCode.E))
        {
            // ✅ Кулдаун подбора
            if (Time.time < lastPickupTime + pickupCooldown) return;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange))
            {
                ItemData itemData = hit.collider.GetComponent<ItemData>();
                if (itemData != null && inventory != null)
                {
                    Item newItem = new Item(itemData.itemName, itemData.itemType, itemData.value);

                    // ✅ Если инвентарь полон — предмет не берём
                    bool added = inventory.AddItem(newItem);
                    if (!added) return;

                    lastPickupTime = Time.time; // ✅ обновляем время подбора
                    InventoryUICode.RefreshIfOpen();

                    if (pickupSound != null)
                        AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);

                    Destroy(hit.collider.gameObject);

                    if (HandController.Instance != null)
                        HandController.Instance.PlayPickup();
                }
            }
        }
    }
}