using UnityEngine;
using TMPro; 

public class PotionCounter : MonoBehaviour
{
    private TMP_Text counterText;  // вместо Text
    private Inventory inventory;

    void Start()
    {
        counterText = GetComponentInChildren<TMP_Text>();
        if (counterText == null)
            Debug.LogError("PotionCounter: Не найден TMP_Text на дочерних объектах!");

        inventory = FindAnyObjectByType<Inventory>();
        if (inventory == null)
            Debug.LogError("PotionCounter: Не найден Inventory!");

        UpdateCount();
    }

    void Update()
    {
        UpdateCount();
    }

    void UpdateCount()
    {
        if (inventory == null || counterText == null) return;
        int count = 0;
        foreach (Item item in inventory.items)
            if (item.itemType == "Potion") count++;
        counterText.text = $"Зелий: {count}";
    }
}