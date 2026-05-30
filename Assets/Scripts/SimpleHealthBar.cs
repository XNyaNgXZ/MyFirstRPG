using UnityEngine;
using UnityEngine.UI;

public class SimpleHealthBar : MonoBehaviour
{
    public Image healthFill;        // красная полоска (HealthBarFill)
    public PlayerHealth playerHealth;

    private float maxWidth;
    private RectTransform fillRect;

    void Start()
    {
        if (healthFill == null)
            healthFill = GetComponentInChildren<Image>(); // ищем дочерний Image

        fillRect = healthFill.GetComponent<RectTransform>();
        maxWidth = fillRect.rect.width; // начальная ширина (при полном здоровье)

        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        UpdateHealthBar();
    }

    void Update()
    {
        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (playerHealth == null || fillRect == null) return;

        float percent = (float)playerHealth.currentHealth / playerHealth.maxHealth;
        float newWidth = maxWidth * percent;
        fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
    }
}