using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    public const int SLOTS = 25;

    [System.NonSerialized]
    public Item[] items;

    public Dictionary<string, Item> equippedItems = new Dictionary<string, Item>();

    public Dictionary<string, SpellDefinition> equippedSpells = new Dictionary<string, SpellDefinition>();
    public List<SpellDefinition> knownSpells = new List<SpellDefinition>();

    [Header("Звуки надевания")]
    public AudioClip equipWeaponSound;
    public AudioClip equipArmorSound;
    public AudioClip equipAccessorySound;

    [Header("Звуки снятия")]
    public AudioClip unequipWeaponSound;
    public AudioClip unequipArmorSound;
    public AudioClip unequipAccessorySound;

    [Range(0f, 1f)] public float equipVolume = 0.7f;

    private AudioSource audioSource;

    void Awake()
    {
        if (items == null || items.Length != SLOTS)
            items = new Item[SLOTS];
        else
        {
            for (int i = 0; i < items.Length; i++)
                if (items[i] != null && string.IsNullOrEmpty(items[i].itemName))
                    items[i] = null;
        }
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public bool AddItem(Item newItem)
    {
        if (newItem.maxQuantity > 1)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null &&
                    items[i].itemName == newItem.itemName &&
                    items[i].quantity < items[i].maxQuantity)
                {
                    items[i].quantity = Mathf.Min(
                        items[i].quantity + newItem.quantity,
                        items[i].maxQuantity);
                    return true;
                }
            }
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                items[i] = newItem;
                return true;
            }
        }
        Debug.Log("Инвентарь полон!");
        return false;
    }

    public void RemoveItem(int index)
    {
        if (index >= 0 && index < items.Length)
            items[index] = null;
    }

    public Item TakeItem(int index)
    {
        if (index < 0 || index >= items.Length || items[index] == null)
            return null;
        Item item = items[index];
        items[index] = null;
        return item;
    }

    // ─── Заклинания ──────────────────────────────────────────────────

    public void AddKnownSpell(SpellDefinition spell)
    {
        if (spell == null) return;
        if (!knownSpells.Contains(spell))
            knownSpells.Add(spell);
        InventoryUICode.RefreshIfOpen();
    }

    public void RemoveKnownSpell(int index)
    {
        if (index >= 0 && index < knownSpells.Count)
            knownSpells.RemoveAt(index);
        InventoryUICode.RefreshIfOpen();
    }

    public void EquipSpell(SpellDefinition spell, string slot)
    {
        if (spell == null) return;

        if (IsTwoHandedOrBow(GetEquippedItem("Weapon")))
        {
            Debug.Log("Двуручное оружие блокирует магию!");
            return;
        }

        if (equippedSpells.ContainsKey(slot))
            UnequipSpell(slot);

        if (equippedItems.ContainsKey(slot))
            UnequipItem(slot);

        equippedSpells[slot] = spell;
        UpdateWeaponModeAfterSpell();

        InventoryUICode.RefreshIfOpen();
        EquipmentUI.RefreshIfOpen();
    }

    public void UnequipSpell(string slot)
    {
        if (!equippedSpells.ContainsKey(slot)) return;
        SpellDefinition spell = equippedSpells[slot];
        equippedSpells.Remove(slot);

        if (spell.isBook) AddKnownSpell(spell);

        UpdateWeaponModeAfterSpell();
        InventoryUICode.RefreshIfOpen();
        EquipmentUI.RefreshIfOpen();
    }

    void UpdateWeaponModeAfterSpell()
    {
        if (HandController.Instance == null) return;

        bool hasRightWeapon = equippedItems.ContainsKey("Weapon");
        bool hasLeftWeapon = equippedItems.ContainsKey("WeaponLeft");
        bool hasRightSpell = equippedSpells.ContainsKey("Weapon");
        bool hasLeftSpell = equippedSpells.ContainsKey("WeaponLeft");
        bool hasShield = hasLeftWeapon && IsShield(equippedItems["WeaponLeft"]);

        if (hasRightSpell && hasLeftSpell) HandController.Instance.SetWeaponMode(HandController.WeaponMode.Magic);
        else if (hasRightWeapon && hasLeftSpell) HandController.Instance.SetWeaponMode(HandController.WeaponMode.Magic);
        else if (hasRightSpell && hasShield) HandController.Instance.SetWeaponMode(HandController.WeaponMode.Magic);
        else if (hasRightSpell && hasLeftWeapon) HandController.Instance.SetWeaponMode(HandController.WeaponMode.Magic);
        else if (hasRightSpell) HandController.Instance.SetWeaponMode(HandController.WeaponMode.Magic);
        else if (hasLeftSpell) HandController.Instance.SetWeaponMode(HandController.WeaponMode.Magic);
    }

    public SpellDefinition GetEquippedSpell(string slot)
        => equippedSpells.TryGetValue(slot, out SpellDefinition s) ? s : null;

    // ─── Оружие ───────────────────────────────────────────────────────

    public bool IsShield(Item item)
    {
        if (item == null) return false;
        return item.itemType == "Shield" || item.originalType == "Shield";
    }

    public bool IsTwoHanded(Item item)
    {
        if (item == null) return false;
        return item.itemType == "TwoHand" || item.originalType == "TwoHand";
    }

    public bool IsBow(Item item)
    {
        if (item == null) return false;
        return item.itemType == "Bow" || item.originalType == "Bow";
    }

    bool IsTwoHandedOrBow(Item item) => IsTwoHanded(item) || IsBow(item);

    public void EquipItem(Item item)
    {
        if (item == null) return;
        string type = item.itemType;
        if (!IsEquippableType(type)) return;

        if (type == "Bow")
        {
            if (equippedItems.ContainsKey("Weapon")) ForceUnequip("Weapon");
            if (equippedItems.ContainsKey("WeaponLeft")) ForceUnequip("WeaponLeft");
            if (equippedSpells.ContainsKey("Weapon")) UnequipSpell("Weapon");
            if (equippedSpells.ContainsKey("WeaponLeft")) UnequipSpell("WeaponLeft");

            equippedItems["Weapon"] = item;
            for (int i = 0; i < items.Length; i++)
                if (items[i] == item) { items[i] = null; break; }

            PlayEquipSound("Weapon", true);
            if (HandController.Instance != null)
            {
                HandController.Instance.SetWeaponMode(HandController.WeaponMode.Bow);
                HandController.Instance.ShowTwoHandModel();
            }
            InventoryUICode.RefreshIfOpen();
            EquipmentUI.RefreshIfOpen();
            return;
        }

        if (type == "TwoHand")
        {
            if (equippedItems.ContainsKey("Weapon")) ForceUnequip("Weapon");
            if (equippedItems.ContainsKey("WeaponLeft")) ForceUnequip("WeaponLeft");
            if (equippedSpells.ContainsKey("Weapon")) UnequipSpell("Weapon");
            if (equippedSpells.ContainsKey("WeaponLeft")) UnequipSpell("WeaponLeft");

            equippedItems["Weapon"] = item;
            for (int i = 0; i < items.Length; i++)
                if (items[i] == item) { items[i] = null; break; }

            PlayEquipSound("Weapon", true);
            if (HandController.Instance != null)
            {
                HandController.Instance.SetWeaponMode(HandController.WeaponMode.TwoHand);
                HandController.Instance.ShowTwoHandModel();
            }
            InventoryUICode.RefreshIfOpen();
            EquipmentUI.RefreshIfOpen();
            return;
        }

        if ((type == "Weapon" || type == "WeaponLeft") &&
            equippedItems.ContainsKey("Weapon") &&
            IsTwoHandedOrBow(equippedItems["Weapon"]))
            ForceUnequip("Weapon");

        if (equippedItems.ContainsKey(type)) UnequipItem(type);

        if (equippedSpells.ContainsKey(type)) UnequipSpell(type);

        equippedItems[type] = item;
        for (int i = 0; i < items.Length; i++)
            if (items[i] == item) { items[i] = null; break; }

        PlayEquipSound(type, true);

        if (type == "Weapon" && HandController.Instance != null)
        {
            HandController.Instance.ShowWeaponModel();
            bool hasWeaponLeft = equippedItems.ContainsKey("WeaponLeft");
            bool hasShield = hasWeaponLeft && IsShield(equippedItems["WeaponLeft"]);
            bool hasLeftSpell = equippedSpells.ContainsKey("WeaponLeft");

            if (hasShield) HandController.Instance.SetWeaponMode(HandController.WeaponMode.SwordShield);
            else if (hasLeftSpell) HandController.Instance.SetWeaponMode(HandController.WeaponMode.Magic);
            else if (hasWeaponLeft) HandController.Instance.SetWeaponMode(HandController.WeaponMode.DualWield);
            else HandController.Instance.SetWeaponMode(HandController.WeaponMode.OneHand);

            HandController.Instance.RefreshLeftHandAnimator();
        }

        if (type == "WeaponLeft" && HandController.Instance != null)
        {
            bool hasWeapon = equippedItems.ContainsKey("Weapon");
            bool hasRightSpell = equippedSpells.ContainsKey("Weapon");

            if (IsShield(item))
            {
                HandController.Instance.ShowShieldModel();
                if (hasRightSpell) HandController.Instance.SetWeaponMode(HandController.WeaponMode.Magic);
                else HandController.Instance.SetWeaponMode(
                    hasWeapon ? HandController.WeaponMode.SwordShield : HandController.WeaponMode.Unarmed);
                HandController.Instance.RefreshLeftHandAnimator();
            }
            else
            {
                HandController.Instance.ShowWeaponModelLeft();
                HandController.Instance.SetWeaponMode(
                    hasWeapon ? HandController.WeaponMode.DualWield : HandController.WeaponMode.OneHandLeft);
            }
        }

        InventoryUICode.RefreshIfOpen();
        EquipmentUI.RefreshIfOpen();
    }

    void ForceUnequip(string type)
    {
        if (!equippedItems.ContainsKey(type)) return;
        Item item = equippedItems[type];
        equippedItems.Remove(type);
        AddItem(item);
        if (!string.IsNullOrEmpty(item.originalType)) item.itemType = item.originalType;
        PlayEquipSound(type, false);
        if (HandController.Instance != null)
        {
            if (type == "Weapon")
            {
                if (IsTwoHandedOrBow(item)) HandController.Instance.HideTwoHandModel();
                else HandController.Instance.HideWeaponModel();
            }
            else if (type == "WeaponLeft")
            {
                if (IsShield(item)) HandController.Instance.HideShieldModel();
                else HandController.Instance.HideWeaponModelLeft();
            }
        }
    }

    public void UnequipItem(string type)
    {
        if (!equippedItems.ContainsKey(type)) return;
        bool hasSpace = false;
        for (int i = 0; i < items.Length; i++)
            if (items[i] == null) { hasSpace = true; break; }
        if (!hasSpace) { Debug.Log("Инвентарь полон!"); return; }

        Item item = equippedItems[type];
        equippedItems.Remove(type);
        AddItem(item);
        if (!string.IsNullOrEmpty(item.originalType)) item.itemType = item.originalType;
        PlayEquipSound(type, false);

        if (type == "Weapon" && HandController.Instance != null)
        {
            if (IsTwoHandedOrBow(item)) HandController.Instance.HideTwoHandModel();
            else HandController.Instance.HideWeaponModel();
            bool hasWeaponLeft = equippedItems.ContainsKey("WeaponLeft");
            HandController.Instance.SetWeaponMode(
                hasWeaponLeft ? HandController.WeaponMode.OneHandLeft : HandController.WeaponMode.Unarmed);
            HandController.Instance.ResetPickup();
        }

        if (type == "WeaponLeft" && HandController.Instance != null)
        {
            if (IsShield(item)) HandController.Instance.HideShieldModel();
            else HandController.Instance.HideWeaponModelLeft();
            bool hasWeapon = equippedItems.ContainsKey("Weapon");
            HandController.Instance.SetWeaponMode(
                hasWeapon ? HandController.WeaponMode.OneHand : HandController.WeaponMode.Unarmed);
            HandController.Instance.RefreshLeftHandAnimator();
        }

        InventoryUICode.RefreshIfOpen();
        EquipmentUI.RefreshIfOpen();
    }

    void PlayEquipSound(string type, bool equip)
    {
        if (audioSource == null) return;
        AudioClip clip;
        if (equip)
            clip = type switch
            {
                "Weapon" or "WeaponLeft" or "TwoHand" or "Bow" => equipWeaponSound,
                "Helmet" or "Chest" or "Legs" or "Boots" => equipArmorSound,
                "Shield" or "Ring" or "Amulet" => equipAccessorySound,
                _ => null
            };
        else
            clip = type switch
            {
                "Weapon" or "WeaponLeft" or "TwoHand" or "Bow" => unequipWeaponSound,
                "Helmet" or "Chest" or "Legs" or "Boots" => unequipArmorSound,
                "Shield" or "Ring" or "Amulet" => unequipAccessorySound,
                _ => null
            };
        if (clip != null) audioSource.PlayOneShot(clip, equipVolume);
    }

    public bool IsEquippableType(string type)
        => type is "Weapon" or "WeaponLeft" or "Shield" or "Helmet" or "Chest"
                or "Legs" or "Boots" or "Ring" or "Amulet" or "TwoHand" or "Bow";

    public Item GetEquippedItem(string type)
        => equippedItems.TryGetValue(type, out Item i) ? i : null;

    public int GetTotalDefense()
    {
        int total = 0;
        foreach (string t in new[] { "Helmet", "Chest", "Legs", "Boots", "Amulet", "Ring" })
            if (equippedItems.ContainsKey(t)) total += equippedItems[t].value;
        return total;
    }

    public void UseItem(int index)
    {
        if (index < 0 || index >= items.Length || items[index] == null) return;
        Item item = items[index];

        if (item.itemType == "Potion")
        {
            PlayerHealth ph = GetComponent<PlayerHealth>();
            if (ph != null) ph.Heal(item.value);
            item.quantity = Mathf.Max(0, item.quantity - 1);
            if (item.quantity <= 0) RemoveItem(index);
            InventoryUICode.RefreshIfOpen();
        }
        else if (item.itemType == "ManaPotion")
        {
            PlayerMana pm = PlayerMana.Instance;
            if (pm != null) pm.RestoreMana(item.value);
            item.quantity = Mathf.Max(0, item.quantity - 1);
            if (item.quantity <= 0) RemoveItem(index);
            InventoryUICode.RefreshIfOpen();
        }
    }
}