using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Sound")]
    public AudioClip hurtSound;
    public float hurtVolume = 0.5f;

    private bool isDead = false;
    public int maxHealth = 10;
    public int currentHealth;
    void Start()
    {
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
        
        currentHealth -= damage;
        
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
