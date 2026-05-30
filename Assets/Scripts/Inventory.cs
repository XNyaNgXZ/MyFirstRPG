using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    public List<Item> items = new List<Item>();
    public Item equippedWeapon = null; 

    public void AddItem(Item newItem)
    {
        items.Add(newItem);
        Debug.Log($"Предмет '{newItem.itemName}' добавлен в инвентарь. Всего предметов: {items.Count}");
    }

    public void EquipWeapon(Item weapon)
    {
        if (weapon == null) return;
        if (weapon.itemType != "Weapon")
        {
            Debug.Log("Это не оружие!");
            return;
        }
        // Если уже экипировано другое оружие, снимаем его обратно в инвентарь
        if (equippedWeapon == null)
        {
            UnequipWeapon();
        }
        equippedWeapon = weapon;
        // Удаляем оружие из списка инвентаря (оно теперь экипировано)
        items.Remove(weapon);
        Debug.Log($"Экипировано: {weapon.itemName}. Урон +{weapon.value}");
    }
    public void UnequipWeapon()
    {
        if (equippedWeapon != null)
        {
            items.Add(equippedWeapon);
            Debug.Log($"Снято: {equippedWeapon.itemName}");
            equippedWeapon = null;
        }
    }

    public void RemoveItem(int index)
    {
        if (index >= 0 && index < items.Count)
        {
            Debug.Log($"Удален предмет: {items[index].itemName}");
            items.RemoveAt(index);
        }
    }

    // Возвращает предмет и удаляет из инвентаря — для выброса в мир
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
        Debug.Log($"Используем предмет {item.itemName}");

        switch (item.itemType)
        {
            case "Potion":
                PlayerHealth playerHealth = GetComponent<PlayerHealth>();
                if (playerHealth != null)
                    playerHealth.Heal(item.value);
                else
                    Debug.Log("Нет компонента PlayerHealth!");
                RemoveItem(index);
                break;
            default:
                Debug.Log($"Неизвестный тип предмета: {item.itemType}");
                break;
        }
    }
}