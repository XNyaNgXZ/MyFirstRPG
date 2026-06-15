using UnityEngine;

// Повесь на любой предмет в сцене — он упадёт на пол и замрёт
[RequireComponent(typeof(ItemData))]
public class ItemSettle : MonoBehaviour
{
    [Tooltip("Через сколько секунд предмет замрёт после падения")]
    public float settleDelay = 1.5f;

    void Start()
    {
        // Добавляем Rigidbody если нет — предмет упадёт на пол
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = false;

        // Игрок не врезается в предмет
        var col = GetComponent<Collider>();
        var playerObj = GameObject.FindWithTag("Player");
        if (col != null && playerObj != null)
        {
            var playerCol = playerObj.GetComponent<Collider>();
            if (playerCol != null)
                Physics.IgnoreCollision(col, playerCol);
        }

        // Через settleDelay замораживаем
        Invoke(nameof(Freeze), settleDelay);
    }

    void Freeze()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb == null) return;
        rb.linearVelocity = Vector3.zero;      // ← сначала обнуляем
        rb.angularVelocity = Vector3.zero;     // ← потом
        rb.isKinematic = true;                 // ← потом кинематик
    }
}