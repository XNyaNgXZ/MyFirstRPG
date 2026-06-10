using UnityEngine;

[System.Serializable]
public class Item
{
    public string itemName;
    public string itemType;
    public string originalType; // оригинальный тип до надевания в левую руку
    public int value;
    public Color itemColor = Color.white;
    public Texture2D worldTexture;
    public Vector3 itemScale = Vector3.one * 0.4f;

    public Item(string name, string type, int val, Color color = default, Vector3 scale = default)
    {
        itemName = name;
        itemType = type;
        originalType = type; // сохраняем оригинальный тип
        value = val;
        itemColor = color == default ? Color.white : color;
        itemScale = scale == default ? Vector3.one * 0.4f : scale;
    }
}