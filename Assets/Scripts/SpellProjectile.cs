using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    public int damage;
    public float speed;
    public float lifetime;
    public string spellType;
    public AudioClip impactSound; // ✅

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

        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = spell.spellType == "Fireball";
        rb.linearVelocity = direction.normalized * spell.projectileSpeed;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var proj = go.AddComponent<SpellProjectile>();
        proj.damage = spell.damage;
        proj.speed = spell.projectileSpeed;
        proj.lifetime = spell.lifetime;
        proj.spellType = spell.spellType;
        proj.impactSound = spell.impactSound; // ✅
        proj.rb = rb;

        // Игнорируем коллизии с игроком
        Collider spellCol = go.GetComponent<Collider>();
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && spellCol != null)
            foreach (Collider col in player.GetComponentsInChildren<Collider>(true))
                Physics.IgnoreCollision(spellCol, col);

        Destroy(go, spell.lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        if (collision.collider.CompareTag("Enemy"))
        {
            EnemyNav enemy = collision.collider.GetComponent<EnemyNav>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                HitSpark.Spawn(collision.contacts[0].point, false, spellType == "Fireball");
                // ✅ Звук попадания
                if (impactSound != null)
                    AudioSource.PlayClipAtPoint(impactSound, collision.contacts[0].point);
            }
        }
        else
        {
            // ✅ Звук попадания в стену/пол тоже
            if (impactSound != null)
                AudioSource.PlayClipAtPoint(impactSound, collision.contacts[0].point);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Destroy(gameObject, 0.05f);
    }
}