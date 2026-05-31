using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    private Inventory inventory;

    [Header("Sound")]
    public AudioClip hurtSound;
    public float hurtVolume = 0.5f;

    private bool isDead = false;
    public int maxHealth = 100;
    public int currentHealth;
    void Start()
    {
        inventory = GetComponent<Inventory>();
        currentHealth = maxHealth;
        Debug.Log($"Здоровье: {currentHealth}/{maxHealth}");
    }
    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        Debug.Log($"Вылечился на {amount}. Здоровье: {currentHealth}/{maxHealth}");
    }
    public void TakeDamage(int damage)
    {
        if (isDead)
        {
            return;
        }

        int defense = (inventory != null) ? inventory.GetTotalDefense() : 0;
        int finalDamage = damage - defense;
        if (finalDamage < 0) finalDamage = 0;
        currentHealth -= finalDamage;
        Debug.Log($"Получено {finalDamage} урона (защита {defense})");

        if (hurtSound != null)
        {
            AudioSource.PlayClipAtPoint(hurtSound, transform.position, hurtVolume);
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
