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

        // ✅ Убираем Rigidbody — ItemFloat сам найдёт пол
        var rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        // Игнорируем коллизии с игроком
        var col = GetComponent<Collider>();
        var playerObj = GameObject.FindWithTag("Player");
        if (col != null && playerObj != null)
        {
            foreach (Collider c in playerObj.GetComponentsInChildren<Collider>(true))
                Physics.IgnoreCollision(col, c);
        }

        // ✅ ItemFloat находит пол и парит
        if (GetComponent<ItemFloat>() == null)
        {
            var f = gameObject.AddComponent<ItemFloat>();
            f.applyThrow = false;
        }

        if (GetComponent<BlobShadow>() == null)
            gameObject.AddComponent<BlobShadow>();
    }

    public void Pickup(Inventory inventory)
    {
        if (spell == null || inventory == null) return;
        inventory.AddKnownSpell(spell);
        InventoryUICode.RefreshIfOpen();
        Destroy(gameObject);
    }
}