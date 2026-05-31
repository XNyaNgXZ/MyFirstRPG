using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    public List<Item> items = new List<Item>();
    public Item equippedWeapon = null;

    public Dictionary<string, Item> equippedItems = new Dictionary<string, Item>();

    public void EquipItem(Item item)
    {
        if (item == null) return;
        string type = item.itemType;

        if (!IsEquippableType(type))
        {
            Debug.Log($"Нельзя надеть предмет типа {type}");
            return;
        }

        if (equippedItems.ContainsKey(type))
            UnequipItem(type);

        equippedItems[type] = item;
        items.Remove(item);
        Debug.Log($"Надето: {item.itemName} (слот: {type})");

        if (type == "Weapon" && HandController.Instance != null)
            HandController.Instance.ShowWeaponModel();

        InventoryUICode.RefreshIfOpen();
        EquipmentUI.RefreshIfOpen();
    }

    public void UnequipItem(string type)
    {
        if (!equippedItems.ContainsKey(type)) return;

        Item item = equippedItems[type];
        equippedItems.Remove(type);
        items.Add(item);
        Debug.Log($"Снято: {item.itemName}");

        if (type == "Weapon" && HandController.Instance != null)
            HandController.Instance.HideWeaponModel();

        InventoryUICode.RefreshIfOpen();
        EquipmentUI.RefreshIfOpen();
    }

    public bool IsEquippableType(string type)
    {
        return type == "Weapon" || type == "Helmet" || type == "Chest" ||
               type == "Legs" || type == "Boots" || type == "Shield" ||
               type == "Ring" || type == "Amulet";
    }

    public Item GetEquippedItem(string type)
    {
        return equippedItems.ContainsKey(type) ? equippedItems[type] : null;
    }

    public int GetTotalDefense()
    {
        int total = 0;
        string[] armorTypes = { "Helmet", "Chest", "Legs", "Boots", "Shield", "Amulet", "Ring" };
        foreach (string type in armorTypes)
            if (equippedItems.ContainsKey(type))
                total += equippedItems[type].value;
        return total;
    }

    public void AddItem(Item newItem)
    {
        items.Add(newItem);
        Debug.Log($"Предмет '{newItem.itemName}' добавлен. Всего: {items.Count}");
    }

    public void RemoveItem(int index)
    {
        if (index >= 0 && index < items.Count)
            items.RemoveAt(index);
    }

    public Item TakeItem(int index)
    {
        if (index < 0 || index >= items.Count) return null;
        Item item = items[index];
        items.RemoveAt(index);
        return item;
    }

    public void UseItem(int index)
    {
        if (index < 0 || index >= items.Count) return;
        Item item = items[index];

        switch (item.itemType)
        {
            case "Potion":
                PlayerHealth ph = GetComponent<PlayerHealth>();
                if (ph != null) ph.Heal(item.value);
                RemoveItem(index);
                break;
            default:
                Debug.Log($"Неизвестный тип: {item.itemType}");
                break;
        }
    }
}