using UnityEngine;

[RequireComponent (typeof(Collider))]
public class ItemData : MonoBehaviour
{
    public string itemName;
    public string itemType; // potion, weapon, key, armour e.t.c.
    public int value;
}
