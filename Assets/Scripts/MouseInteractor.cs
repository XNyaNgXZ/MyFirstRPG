using UnityEngine;
using UnityEngine.EventSystems;

public class MouseInteractor : MonoBehaviour
{
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
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Debug.Log("ЛКМ заблокирован, т.к. зажата ПКМ");
                return;
            }

            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, interactionRange))
            {
                GameObject target = hit.collider.gameObject;
                Debug.Log($"Кликнули по {target.name} (тег: {target.tag})");

                ItemData itemData = target.GetComponent<ItemData>();
                if (itemData != null)
                {
                    Debug.Log($"Подобран предмет: {itemData.itemName}");
                    if (inventory != null)
                    {
                        Item newItem = new Item(itemData.itemName, itemData.itemType, itemData.value);
                        inventory.AddItem(newItem);

                        if (pickupSound != null)
                        {
                            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
                        }
                        Destroy(target);
                    }
                    else
                    {
                        Debug.LogWarning("Инвентарь не найден, предмет не добавлен");
                    }
                }
                else if (target.CompareTag("NPC"))
                {
                    Debug.Log("Привет, путник! У нас завелись крысы в подвале, не смог бы ты нам помочь с этим?");
                }
                else if (target.CompareTag("Enemy"))
                {
                    EnemyNav enemy = target.GetComponent<EnemyNav>();
                    int damage = 1; // Базовый урон
                    if (enemy != null && inventory.equippedWeapon != null)
                    {
                        damage += inventory.equippedWeapon.value; // + урон от экип. оружия
                        enemy.TakeDamage(damage); // урон от 1 клика
                        Debug.Log($"Атака по {target.name}. Урон: {damage}");
                    }
                    else
                    {
                        Debug.LogWarning($"У объекта {target.name} есть тег Enemy, но нет компонента Enemy!");
                    }
                }
                else
                {
                    Debug.Log($"Кликнули по объекту: {target.name}");
                }
            }
            else
            {
                Debug.Log("Кликнули в пустоту");
            }
        }
    }
}