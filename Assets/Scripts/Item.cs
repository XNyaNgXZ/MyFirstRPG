using UnityEngine;

[System.Serializable]
public class Item
{
    public string itemName;
    public string itemType;
    public string originalType;
    public int value;
    public int quantity = 1;
    public int maxQuantity = 1;
    public Color itemColor = Color.white;
    public Texture2D worldTexture;
    public Vector3 itemScale = Vector3.one * 0.4f;

    public Item(string name, string type, int val, Color color = default, Vector3 scale = default)
    {
        itemName = name;
        itemType = type;
        originalType = type;
        value = val;
        itemColor = color == default ? Color.white : color;
        itemScale = scale == default ? Vector3.one * 0.4f : scale;
        quantity = 1;
        maxQuantity = (type == "Arrow" || type == "Potion") ? 99 : 1;
    }
}