using UnityEngine;

public class MouseInteractor : MonoBehaviour
{
    private float lastPickupTime = -10f;
    public float pickupCooldown = 0.8f; // секунд между подборами

    public float interactionRange = 5f;
    public AudioClip pickupSound;
    public float pickupVolume = 0.4f;

    private Camera playerCamera;
    private Inventory inventory;

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

        // ЛКМ — атака / подбор (анимацию запускает HandController отдельно)
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, interactionRange))
            {
                GameObject target = hit.collider.gameObject;
                Debug.Log($"Попали в: {target.name} (тег: {target.tag})");

                if (target.CompareTag("Enemy"))
                {
                    EnemyNav enemy = target.GetComponent<EnemyNav>();
                    if (enemy != null)
                    {
                        int damage = 1;
                        Item weapon = inventory?.GetEquippedItem("Weapon");
                        if (weapon != null) damage += weapon.value;
                        enemy.TakeDamage(damage);
                        Debug.Log($"Атака по {target.name}. Урон: {damage}");
                    }
                }
                else if (target.CompareTag("NPC"))
                {
                    Debug.Log("Привет, путник!");
                }
                // Если ни враг, ни NPC — просто ничего (анимацию атаки запустит HandController)
            }
            // else — промах, анимация атаки всё равно будет из HandController
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (Time.time < lastPickupTime + pickupCooldown) return;

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange))
            {
                ItemData itemData = hit.collider.GetComponent<ItemData>();
                if (itemData != null && inventory != null)
                {
                    lastPickupTime = Time.time; 

                    Item newItem = new Item(itemData.itemName, itemData.itemType, itemData.value);
                    inventory.AddItem(newItem);
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