using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "RPG/Item")]
public class ItemDefinition : ScriptableObject
{
    [Header("Основное")]
    public string itemName = "Новый предмет";
    public string itemType = "Weapon"; // Weapon / Shield / Helmet / Chest / Legs / Boots / Ring / Amulet / Potion
    public int itemValue = 1;

    [Header("Внешний вид")]
    public Color itemColor = Color.white;
    public Vector3 itemScale = Vector3.one * 0.4f;

    [Header("Визуал")]
    public GameObject worldPrefab;
    public Sprite icon;
    public Texture2D worldTexture; // ✅ текстура на объект в мире
}