// Как отдельный класс а не скрипт

[System.Serializable] // Для отображение в инспекторе (необязательная вещь)
public class Item
{
    public string itemName;
    public string itemType; // potion,sword e.t.c.
    public int value; // potion - hp / sword - damage e.t.c

    public Item(string name, string type, int val)
    {
        itemName = name;
        itemType = type;
        value = val;
    }
}