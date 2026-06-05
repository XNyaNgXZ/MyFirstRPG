using UnityEngine;

// Вешается автоматически на задропанный предмет
public class ItemFreezer : MonoBehaviour
{
    public float delay = 1.5f;

    void Start() => Invoke(nameof(Freeze), delay);

    void Freeze()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb == null) return;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }
}