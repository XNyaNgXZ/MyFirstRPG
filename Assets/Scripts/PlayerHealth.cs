using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    private Inventory inventory;

    [Header("Звуки получения урона — будет случайный из списка")]
    public AudioClip[] hurtSounds;       // Hurt1, Hurt2, Hurt3, Hurt4
    [Range(0f, 1f)] public float hurtVolume = 0.8f;

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
        Debug.Log($"Здоровье: {currentHealth}/{maxHealth}");
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"Вылечился на {amount}. Здоровье: {currentHealth}/{maxHealth}");
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        int defense = inventory != null ? inventory.GetTotalDefense() : 0;
        int finalDamage = Mathf.Max(0, damage - defense);
        currentHealth -= finalDamage;
        Debug.Log($"Получено {finalDamage} урона (защита {defense})");

        if (hurtSounds != null && hurtSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = hurtSounds[Random.Range(0, hurtSounds.Length)];
            if (clip != null)
                audioSource.PlayOneShot(clip, hurtVolume);
        }

        if (currentHealth <= 0)
        {
            isDead = true;
            Debug.Log("Вы погибли, сцена перезапустится через 2 секунды");
            Invoke(nameof(RestartGame), 2f);
        }
    }

    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}