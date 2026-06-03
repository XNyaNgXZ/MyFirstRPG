using UnityEngine;
using TMPro;

public class PotionCounter : MonoBehaviour
{
    private TMP_Text counterText;
    private Inventory inventory;

    void Start()
    {
        counterText = GetComponentInChildren<TMP_Text>();
        inventory = FindAnyObjectByType<Inventory>();
        UpdateCount();
    }

    void Update() => UpdateCount();

    void UpdateCount()
    {
        if (inventory == null || counterText == null) return;
        int count = 0;
        foreach (Item item in inventory.items)
            if (item != null && item.itemType == "Potion") count++;
        counterText.text = $"Potions: {count}";
    }
}