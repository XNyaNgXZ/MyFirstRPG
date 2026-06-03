using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    public const int SLOTS = 25; // 5x5

    // ✅ Фиксированный массив — пустая ячейка = null, индексы не смещаются
    public Item[] items = new Item[SLOTS];

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
        {
            items = new Item[SLOTS];
        }
        else
        {
            // ✅ Очищаем дефолтные пустые объекты от сериализации Unity
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && string.IsNullOrEmpty(items[i].itemName))
                    items[i] = null;
            }
        }
    }
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    // ✅ Добавляет в первую свободную ячейку
    public bool AddItem(Item newItem)
    {
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

    // ✅ Убирает предмет из слота (не смещает остальные)
    public void RemoveItem(int index)
    {
        if (index >= 0 && index < items.Length)
            items[index] = null;
    }

    // ✅ Берёт предмет из слота и возвращает его
    public Item TakeItem(int index)
    {
        if (index < 0 || index >= items.Length || items[index] == null)
            return null;
        Item item = items[index];
        items[index] = null;
        return item;
    }

    public void EquipItem(Item item)
    {
        if (item == null) return;
        string type = item.itemType;
        if (!IsEquippableType(type)) return;

        if (equippedItems.ContainsKey(type))
            UnequipItem(type);

        equippedItems[type] = item;

        // Убираем из инвентаря
        for (int i = 0; i < items.Length; i++)
            if (items[i] == item) { items[i] = null; break; }

        PlayEquipSound(type, true);

        if (type == "Weapon" && HandController.Instance != null)
            HandController.Instance.ShowWeaponModel();

        InventoryUICode.RefreshIfOpen();
        EquipmentUI.RefreshIfOpen();
    }

    public void UnequipItem(string type)
    {
        if (!equippedItems.ContainsKey(type)) return;

        // ✅ Проверяем есть ли свободный слот в инвентаре
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

        PlayEquipSound(type, false);

        if (type == "Weapon" && HandController.Instance != null)
            HandController.Instance.HideWeaponModel();

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
                "Weapon" => equipWeaponSound,
                "Helmet" or "Chest" or "Legs" or "Boots" => equipArmorSound,
                "Shield" or "Ring" or "Amulet" => equipAccessorySound,
                _ => null
            };
        else
            clip = type switch
            {
                "Weapon" => unequipWeaponSound,
                "Helmet" or "Chest" or "Legs" or "Boots" => unequipArmorSound,
                "Shield" or "Ring" or "Amulet" => unequipAccessorySound,
                _ => null
            };
        if (clip != null) audioSource.PlayOneShot(clip, equipVolume);
    }

    public bool IsEquippableType(string type)
        => type is "Weapon" or "Helmet" or "Chest" or "Legs"
                or "Boots" or "Shield" or "Ring" or "Amulet";

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
            RemoveItem(index);
        }
    }
}