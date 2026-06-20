using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    public int damage;
    public float speed;
    public float lifetime;
    public string spellType;
    public AudioClip impactSound;
    public GameObject impactParticles; // ✅

    private Rigidbody rb;
    private bool hasHit = false;

    public static void Spawn(SpellDefinition spell, Vector3 position, Vector3 direction)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * spell.projectileSize;
        go.name = spell.spellName + "_Projectile";

        var rend = go.GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        rend.material.color = spell.projectileColor;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Свечение
        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.transform.SetParent(go.transform, false);
        glow.transform.localScale = Vector3.one * 1.6f;
        Destroy(glow.GetComponent<Collider>());
        var glowRend = glow.GetComponent<Renderer>();
        glowRend.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        Color glowColor = spell.projectileColor;
        glowColor.a = 0.25f;
        glowRend.material.color = glowColor;
        glowRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // ✅ Партикли на снаряде
        if (spell.projectileParticles != null)
        {
            GameObject ps = Object.Instantiate(spell.projectileParticles, go.transform);
            ps.transform.localPosition = Vector3.zero;
        }

        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = spell.spellType == "Fireball";
        rb.linearVelocity = direction.normalized * spell.projectileSpeed;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var proj = go.AddComponent<SpellProjectile>();
        proj.damage = spell.damage;
        proj.speed = spell.projectileSpeed;
        proj.lifetime = spell.lifetime;
        proj.spellType = spell.spellType;
        proj.impactSound = spell.impactSound;
        proj.impactParticles = spell.impactParticles; // ✅
        proj.rb = rb;

        // Игнорируем коллизии с игроком
        Collider spellCol = go.GetComponent<Collider>();
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && spellCol != null)
            foreach (Collider col in player.GetComponentsInChildren<Collider>(true))
                Physics.IgnoreCollision(spellCol, col);

        // ✅ Игнорируем коллизии с другими снарядами
        if (spellCol != null)
            foreach (var other in Object.FindObjectsByType<SpellProjectile>(FindObjectsInactive.Exclude))
            {
                Collider otherCol = other.GetComponent<Collider>();
                if (otherCol != null) Physics.IgnoreCollision(spellCol, otherCol);
            }

        Destroy(go, spell.lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        Vector3 hitPoint = collision.contacts[0].point;
        Vector3 hitNormal = collision.contacts[0].normal;

        if (collision.collider.CompareTag("Enemy"))
        {
            EnemyNav enemy = collision.collider.GetComponent<EnemyNav>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                HitSpark.Spawn(hitPoint, false, spellType == "Fireball");
            }
        }

        // ✅ Партикли при попадании
        if (impactParticles != null)
        {
            GameObject ps = Instantiate(impactParticles, hitPoint,
                Quaternion.LookRotation(hitNormal));
            Destroy(ps, 3f);
        }

        // Звук попадания
        if (impactSound != null)
            AudioSource.PlayClipAtPoint(impactSound, hitPoint);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Destroy(gameObject, 0.05f);
    }
}