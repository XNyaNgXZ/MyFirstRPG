using UnityEngine;

public class PlayerMana : MonoBehaviour
{
    public static PlayerMana Instance { get; private set; }

    public float maxMana = 100f;
    public float currentMana;

    void Awake()
    {
        Instance = this;
        currentMana = maxMana;
    }

    public bool HasMana(float amount) => currentMana >= amount;

    public bool UseMana(float amount)
    {
        if (currentMana < amount) return false;
        currentMana -= amount;
        return true;
    }

    public void UseManaUnchecked(float amount)
    {
        currentMana = Mathf.Max(0f, currentMana - amount);
    }

    // ✅ Восстановление только через зелья
    public void RestoreMana(float amount)
    {
        currentMana = Mathf.Min(maxMana, currentMana + amount);
    }
}