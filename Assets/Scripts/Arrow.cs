using UnityEngine;

public class Arrow : MonoBehaviour
{
    [Header("Урон")]
    public int damage = 15;

    [Header("Настройки полёта")]
    public float speed = 30f;
    public float lifetime = 5f; // через сколько секунд исчезает если не попал

    private Rigidbody rb;
    private bool hasHit = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
            rb.useGravity = true;
        }
        transform.rotation = transform.rotation * Quaternion.Euler(90f, 0f, 0f);
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (hasHit || rb == null) return;

        if (rb.linearVelocity.sqrMagnitude > 0.1f)
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity)
                               * Quaternion.Euler(90f, 0f, 0f);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        // ✅ Попали по врагу
        if (collision.collider.CompareTag("Enemy"))
        {
            EnemyNav enemy = collision.collider.GetComponent<EnemyNav>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                HitSpark.Spawn(collision.contacts[0].point, true);
            }
        }

        // ✅ Останавливаем стрелу и прикрепляем к объекту
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // Прикрепляем стрелу к тому во что попали
        transform.SetParent(collision.transform);

        // Исчезает через 3 секунды после попадания
        Destroy(gameObject, 3f);
    }
}