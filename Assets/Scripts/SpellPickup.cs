using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpellPickup : MonoBehaviour
{
    [Header("Заклинание")]
    public SpellDefinition spell;

    void Start()
    {
        gameObject.tag = "Item";

        if (spell != null)
        {
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                rend.material.color = spell.projectileColor;
            }
        }

        // ✅ Физика — падает на пол
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = false;

        // Игрок не врезается
        var col = GetComponent<Collider>();
        var playerObj = GameObject.FindWithTag("Player");
        if (col != null && playerObj != null)
        {
            var playerCol = playerObj.GetComponent<Collider>();
            if (playerCol != null) Physics.IgnoreCollision(col, playerCol);
        }

        // Замираем через 1.5 сек
        Invoke(nameof(Freeze), 3.5f);
    }

    void Freeze()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb == null) return;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    public void Pickup(Inventory inventory)
    {
        if (spell == null || inventory == null) return;

        inventory.AddKnownSpell(spell);

        string bookOrScroll = spell.isBook ? "Книга" : "Свиток";
        Debug.Log($"Подобран {bookOrScroll}: {spell.spellName}");

        InventoryUICode.RefreshIfOpen();
        Destroy(gameObject);
    }
}