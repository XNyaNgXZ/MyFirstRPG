using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemData : MonoBehaviour
{
    [Header("Карточка предмета (приоритет)")]
    public ItemDefinition definition; // ✅ если назначена — берём данные с неё

    [Header("Вручную (если нет карточки)")]
    public string itemName;
    public string itemType;
    public int value;
    public Color itemColor = Color.white;
    public Vector3 itemScale = Vector3.one * 0.4f;

    // Удобные свойства — читают карточку если есть, иначе вручную
    public string Name => definition != null ? definition.itemName : itemName;
    public string Type => definition != null ? definition.itemType : itemType;
    public int Value => definition != null ? definition.itemValue : value;
    public Color Color => definition != null ? definition.itemColor : itemColor;
    public Vector3 Scale => definition != null ? definition.itemScale : itemScale;
}