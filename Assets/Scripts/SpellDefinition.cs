using UnityEngine;

[CreateAssetMenu(fileName = "NewSpell", menuName = "RPG/Spell")]
public class SpellDefinition : ScriptableObject
{
    [Header("Основное")]
    public string spellName = "Новое заклинание";
    public string spellType = "Fireball"; // Fireball, IceArrow, Heal

    [Header("Характеристики")]
    public int damage = 20;
    public float manaCost = 15f;
    public float manaCostPerSecond = 10f;
    public float healPerSecond = 15f;
    public float projectileSpeed = 18f;
    public float lifetime = 5f;

    [Header("Внешний вид")]
    public Color projectileColor = new Color(1f, 0.4f, 0.1f);
    public float projectileSize = 0.18f;
    public Sprite icon;

    [Header("Партикли")]
    public GameObject projectileParticles; // партикли на снаряде (крутятся вместе с ним)
    public GameObject impactParticles;     // партикли при попадании

    [Header("Звуки")]
    public AudioClip castSound;
    public AudioClip impactSound;
    public AudioClip chargeSound;

    [Header("Книга или свиток")]
    public bool isBook = false;
}