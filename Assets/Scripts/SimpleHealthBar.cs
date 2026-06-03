using UnityEngine;
using UnityEngine.UI;
using System.Collections; // ← обязательно для IEnumerator

public class SimpleHealthBar : MonoBehaviour
{
    public Image healthFill;
    public PlayerHealth playerHealth;

    private float maxWidth;
    private RectTransform fillRect;

    IEnumerator Start()
    {
        yield return null; // ждём один кадр — UI успевает выстроиться

        if (healthFill == null)
            healthFill = GetComponentInChildren<Image>();

        if (healthFill == null)
        {
            Debug.LogError("SimpleHealthBar: не найден Image!");
            yield break;
        }

        fillRect = healthFill.GetComponent<RectTransform>();
        maxWidth = fillRect.rect.width;

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
        float newWidth = maxWidth * Mathf.Clamp01(percent);
        fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
    }
}