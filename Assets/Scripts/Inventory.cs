using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    public const int SLOTS = 25;

    [System.NonSerialized]
    public Item[] items;

    public Dictionary<string, Item> equippedItems = new Dictionary<string, Item>();

    [Header("Звуки надевания")]
    public AudioClip equipWeaponSound;
    public AudioClip equipArmorSound;
    public AudioClip equipAccessorySound;

    [Header("Звуки снятия")]
    public AudioClip unequipWeaponSound;
    public AudioClip unequipArmorSound;
    public AudioClip unequipAccessorySound;

    [Range(0f, 1f)] public float equipVolume = 0.7f;

    private AudioSource audioSource;

    void Awake()
    {
        if (items == null || items.Length != SLOTS)
            items = new Item[SLOTS];
        else
        {
            for (int i = 0; i < items.Length; i++)
                if (items[i] != null && string.IsNullOrEmpty(items[i].itemName))
                    items[i] = null;
        }
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public bool AddItem(Item newItem)
    {
        // ✅ Стакаемые предметы — ищем существующий стак
        if (newItem.maxQuantity > 1)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null &&
                    items[i].itemName == newItem.itemName &&
                    items[i].quantity < items[i].maxQuantity)
                {
                    items[i].quantity = Mathf.Min(
                        items[i].quantity + newItem.quantity,
                        items[i].maxQuantity);
                    Debug.Log($"'{newItem.itemName}' добавлен в стак, итого: {items[i].quantity}");
                    return true;
                }
            }
        }

        // Обычное добавление в пустой слот
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                items[i] = newItem;
                Debug.Log($"'{newItem.itemName}' добавлен в слот {i}");
                return true;
            }
        }
        Debug.Log("Инвентарь полон!");
        return false;
    }

    public void RemoveItem(int index)
    {
        if (index >= 0 && index < items.Length)
            items[index] = null;
    }

    public Item TakeItem(int index)
    {
        if (index < 0 || index >= items.Length || items[index] == null)
            return null;
        Item item = items[index];
        items[index] = null;
        return item;
    }

    public bool IsShield(Item item)
    {
        if (item == null) return false;
        return item.itemType == "Shield" || item.originalType == "Shield";
    }

    public bool IsTwoHanded(Item item)
    {
        if (item == null) return false;
        return item.itemType == "TwoHand" || item.originalType == "TwoHand";
    }

    public bool IsBow(Item item)
    {
        if (item == null) return false;
        return item.itemType == "Bow" || item.originalType == "Bow";
    }

    bool IsTwoHandedOrBow(Item item)
    {
        return IsTwoHanded(item) || IsBow(item);
    }

    public void EquipItem(Item item)
    {
        if (item == null) return;
        string type = item.itemType;
        if (!IsEquippableType(type)) return;

        if (type == "Bow")
        {
            if (equippedItems.ContainsKey("Weapon"))
                ForceUnequip("Weapon");
            if (equippedItems.ContainsKey("WeaponLeft"))
                ForceUnequip("WeaponLeft");

            equippedItems["Weapon"] = item;

            for (int i = 0; i < items.Length; i++)
                if (items[i] == item) { items[i] = null; break; }

            PlayEquipSound("Weapon", true);

            if (HandController.Instance != null)
            {
                HandController.Instance.SetWeaponMode(HandController.WeaponMode.Bow);
                HandController.Instance.ShowTwoHandModel();
            }

            InventoryUICode.RefreshIfOpen();
            EquipmentUI.RefreshIfOpen();
            return;
        }

        if (type == "TwoHand")
        {
            if (equippedItems.ContainsKey("Weapon"))
                ForceUnequip("Weapon");
            if (equippedItems.ContainsKey("WeaponLeft"))
                ForceUnequip("WeaponLeft");

            equippedItems["Weapon"] = item;

            for (int i = 0; i < items.Length; i++)
                if (items[i] == item) { items[i] = null; break; }

            PlayEquipSound("Weapon", true);

            if (HandController.Instance != null)
            {
                HandController.Instance.SetWeaponMode(HandController.WeaponMode.TwoHand);
                HandController.Instance.ShowTwoHandModel();
            }

            InventoryUICode.RefreshIfOpen();
            EquipmentUI.RefreshIfOpen();
            return;
        }

        if ((type == "Weapon" || type == "WeaponLeft") &&
            equippedItems.ContainsKey("Weapon") &&
            IsTwoHandedOrBow(equippedItems["Weapon"]))
        {
            ForceUnequip("Weapon");
        }

        if (equippedItems.ContainsKey(type))
            UnequipItem(type);

        equippedItems[type] = item;

        for (int i = 0; i < items.Length; i++)
            if (items[i] == item) { items[i] = null; break; }

        PlayEquipSound(type, true);

        if (type == "Weapon" && HandController.Instance != null)
        {
            HandController.Instance.ShowWeaponModel();

            bool hasWeaponLeft = equippedItems.ContainsKey("WeaponLeft");
            bool hasShield = hasWeaponLeft && IsShield(equippedItems["WeaponLeft"]);

            if (hasShield)
            {
                HandController.Instance.SetWeaponMode(HandController.WeaponMode.SwordShield);
                HandController.Instance.RefreshLeftHandAnimator();
            }
            else if (hasWeaponLeft)
                HandController.Instance.SetWeaponMode(HandController.WeaponMode.DualWield);
            else
                HandController.Instance.SetWeaponMode(HandController.WeaponMode.OneHand);
        }

        if (type == "WeaponLeft" && HandController.Instance != null)
        {
            bool hasWeapon = equippedItems.ContainsKey("Weapon");

            if (IsShield(item))
            {
                HandController.Instance.ShowShieldModel();
                HandController.Instance.SetWeaponMode(
                    hasWeapon ? HandController.WeaponMode.SwordShield
                              : HandController.WeaponMode.Unarmed);
                HandController.Instance.RefreshLeftHandAnimator();
            }
            else
            {
                HandController.Instance.ShowWeaponModelLeft();
                HandController.Instance.SetWeaponMode(
                    hasWeapon ? HandController.WeaponMode.DualWield
                              : HandController.WeaponMode.OneHandLeft);
            }
        }

        InventoryUICode.RefreshIfOpen();
        EquipmentUI.RefreshIfOpen();
    }

    void ForceUnequip(string type)
    {
        if (!equippedItems.ContainsKey(type)) return;
        Item item = equippedItems[type];
        equippedItems.Remove(type);
        AddItem(item);

        if (!string.IsNullOrEmpty(item.originalType))
            item.itemType = item.originalType;

        PlayEquipSound(type, false);

        if (HandController.Instance != null)
        {
            if (type == "Weapon")
            {
                if (IsTwoHandedOrBow(item))
                    HandController.Instance.HideTwoHandModel();
                else
                    HandController.Instance.HideWeaponModel();
            }
            else if (type == "WeaponLeft")
            {
                if (IsShield(item))
                    HandController.Instance.HideShieldModel();
                else
                    HandController.Instance.HideWeaponModelLeft();
            }
        }
    }

    public void UnequipItem(string type)
    {
        if (!equippedItems.ContainsKey(type)) return;

        bool hasSpace = false;
        for (int i = 0; i < items.Length; i++)
            if (items[i] == null) { hasSpace = true; break; }

        if (!hasSpace)
        {
            Debug.Log("Инвентарь полон — нельзя снять предмет!");
            return;
        }

        Item item = equippedItems[type];
        equippedItems.Remove(type);
        AddItem(item);

        if (!string.IsNullOrEmpty(item.originalType))
            item.itemType = item.originalType;

        PlayEquipSound(type, false);

        if (type == "Weapon" && HandController.Instance != null)
        {
            if (IsTwoHandedOrBow(item))
                HandController.Instance.HideTwoHandModel();
            else
                HandController.Instance.HideWeaponModel();

            bool hasWeaponLeft = equippedItems.ContainsKey("WeaponLeft");
            HandController.Instance.SetWeaponMode(
                hasWeaponLeft ? HandController.WeaponMode.OneHandLeft
                              : HandController.WeaponMode.Unarmed);
            HandController.Instance.ResetPickup();
        }

        if (type == "WeaponLeft" && HandController.Instance != null)
        {
            if (IsShield(item))
                HandController.Instance.HideShieldModel();
            else
                HandController.Instance.HideWeaponModelLeft();

            bool hasWeapon = equippedItems.ContainsKey("Weapon");
            HandController.Instance.SetWeaponMode(
                hasWeapon ? HandController.WeaponMode.OneHand
                          : HandController.WeaponMode.Unarmed);

            HandController.Instance.RefreshLeftHandAnimator();
        }

        InventoryUICode.RefreshIfOpen();
        EquipmentUI.RefreshIfOpen();
    }

    void PlayEquipSound(string type, bool equip)
    {
        if (audioSource == null) return;
        AudioClip clip;
        if (equip)
            clip = type switch
            {
                "Weapon" or "WeaponLeft" or "TwoHand" or "Bow" => equipWeaponSound,
                "Helmet" or "Chest" or "Legs" or "Boots" => equipArmorSound,
                "Shield" or "Ring" or "Amulet" => equipAccessorySound,
                _ => null
            };
        else
            clip = type switch
            {
                "Weapon" or "WeaponLeft" or "TwoHand" or "Bow" => unequipWeaponSound,
                "Helmet" or "Chest" or "Legs" or "Boots" => unequipArmorSound,
                "Shield" or "Ring" or "Amulet" => unequipAccessorySound,
                _ => null
            };
        if (clip != null) audioSource.PlayOneShot(clip, equipVolume);
    }

    public bool IsEquippableType(string type)
        => type is "Weapon" or "WeaponLeft" or "Shield" or "Helmet" or "Chest"
                or "Legs" or "Boots" or "Ring" or "Amulet" or "TwoHand" or "Bow";

    public Item GetEquippedItem(string type)
        => equippedItems.TryGetValue(type, out Item i) ? i : null;

    public int GetTotalDefense()
    {
        int total = 0;
        foreach (string t in new[] { "Helmet", "Chest", "Legs", "Boots", "Amulet", "Ring" })
            if (equippedItems.ContainsKey(t)) total += equippedItems[t].value;
        return total;
    }

    public void UseItem(int index)
    {
        if (index < 0 || index >= items.Length || items[index] == null) return;
        Item item = items[index];
        if (item.itemType == "Potion")
        {
            PlayerHealth ph = GetComponent<PlayerHealth>();
            if (ph != null) ph.Heal(item.value);
            item.quantity--;
            if (item.quantity <= 0)
                RemoveItem(index);
        }
    }
}