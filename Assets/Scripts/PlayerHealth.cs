using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    private Inventory inventory;

    [Header("Звуки урона")]
    public AudioClip[] hurtSounds;
    [Range(0f, 1f)] public float hurtVolume = 0.8f;

    [Header("Звуки блока")]
    public AudioClip[] blockHitSounds;
    [Range(0f, 1f)] public float blockHitVolume = 0.8f;

    private AudioSource audioSource;
    private bool isDead = false;
    public int maxHealth = 100;
    public int currentHealth;

    void Start()
    {
        inventory = GetComponent<Inventory>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        currentHealth = maxHealth;

        // ✅ Подписываемся на загрузку сцены
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ✅ Вызывается при каждой загрузке сцены — сбрасываем состояние
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentHealth = maxHealth;
        isDead = false;
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    public void TakeDamage(int damage, Transform attacker = null)
    {
        if (isDead) return;

        if (HandController.IsBlocking)
        {
            Item shield = inventory?.GetEquippedItem("Shield");
            int blocked = shield != null ? shield.value : 0;
            int remaining = Mathf.Max(0, damage - blocked);

            if (blockHitSounds != null && blockHitSounds.Length > 0)
            {
                AudioClip clip = blockHitSounds[Random.Range(0, blockHitSounds.Length)];
                if (clip != null) audioSource.PlayOneShot(clip, blockHitVolume);
            }

            CameraShake.Instance?.Shake(0.12f, 0.04f);

            if (remaining <= 0) return;
            damage = remaining;
        }

        int defense = inventory != null ? inventory.GetTotalDefense() : 0;
        int finalDamage = Mathf.Max(0, damage - defense);
        currentHealth -= finalDamage;

        if (hurtSounds != null && hurtSounds.Length > 0)
        {
            AudioClip clip = hurtSounds[Random.Range(0, hurtSounds.Length)];
            if (clip != null) audioSource.PlayOneShot(clip, hurtVolume);
        }

        ScreenDamageEffect.Instance?.Flash();
        CameraShake.Instance?.Shake(0.2f, 0.08f);

        CharacterController cc = GetComponent<CharacterController>();
        if (attacker != null && cc != null)
            StartCoroutine(ApplyKnockback(cc, attacker));

        if (currentHealth <= 0)
        {
            isDead = true;
            currentHealth = 0;
            Invoke(nameof(RestartGame), 2f);
        }
    }

    IEnumerator ApplyKnockback(CharacterController cc, Transform attacker)
    {
        Vector3 dir = (transform.position - attacker.position).normalized;
        dir.y = 0.3f;
        float force = 4f;
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            cc.Move(dir * force * (1f - elapsed / duration) * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}