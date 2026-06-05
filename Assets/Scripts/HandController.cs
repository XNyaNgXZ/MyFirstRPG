using UnityEngine;

public class HandController : MonoBehaviour
{
    [Header("Ссылки")]
    public Animator handsAnimator;
    public Transform weaponHolder;
    public Inventory inventory;

    [Header("Дальность атаки")]
    public float unarmedRange = 2f;
    public float weaponRange = 2.5f;

    [Header("Звуки кулака")]
    public AudioClip punchSwingSound;
    public AudioClip punchHitSound;
    public AudioClip punchMissSound;

    [Header("Звуки оружия")]
    public AudioClip weaponSwingSound;
    public AudioClip weaponHitSound;
    public AudioClip weaponMissSound;

    [Range(0f, 1f)] public float swingVolume = 0.6f;
    [Range(0f, 1f)] public float hitVolume = 0.9f;
    [Range(0f, 1f)] public float missVolume = 0.4f;

    [Header("Блок")]
    public AudioClip shieldRaiseSound;
    [Range(0f, 1f)] public float shieldVolume = 0.7f;

    public static bool IsBlocking { get; private set; } = false;

    private AudioSource audioSource;
    private GameObject currentWeaponModel;
    private bool isAttacking = false;
    private EnemyNav pendingEnemy = null;
    private int pendingDamage = 0;
    private bool hasWeaponOnAttack = false;
    private Vector3 pendingHitPoint;

    public static HandController Instance { get; private set; }

    void Awake() => Instance = this;

    void Start()
    {
        if (inventory == null)
            inventory = GetComponent<Inventory>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (handsAnimator == null)
            Debug.LogError("HandController: не назначен Animator!");
        else
        {
            handsAnimator.ResetTrigger("punch");
            handsAnimator.ResetTrigger("attack");
            handsAnimator.ResetTrigger("pickup");
        }
    }

    void Update()
    {
        if (InventoryUICode.IsOpen || EquipmentUI.IsOpen) return;

        bool hasWeapon = inventory != null &&
                         inventory.GetEquippedItem("Weapon") != null;
        bool hasShield = inventory != null &&
                         inventory.GetEquippedItem("Shield") != null;

        handsAnimator.SetBool("hasWeapon", hasWeapon);

        if (hasShield)
        {
            if (Input.GetMouseButtonDown(1) && !IsBlocking)
            {
                IsBlocking = true;
                if (shieldRaiseSound != null)
                    audioSource.PlayOneShot(shieldRaiseSound, shieldVolume);
            }
            if (Input.GetMouseButtonUp(1))
                IsBlocking = false;
        }
        else
        {
            IsBlocking = false;
        }

        if (IsBlocking) return;

        if (Input.GetMouseButtonDown(0) && !isAttacking)
            StartAttack(hasWeapon);
    }

    void StartAttack(bool hasWeapon)
    {
        isAttacking = true;
        hasWeaponOnAttack = hasWeapon;
        pendingEnemy = null;
        pendingDamage = 0;
        pendingHitPoint = Vector3.zero;

        // ✅ Дальность зависит от того есть оружие или нет
        float range = hasWeapon ? weaponRange : unarmedRange;

        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, range))
            {
                pendingHitPoint = hit.point;
                if (hit.collider.CompareTag("Enemy"))
                {
                    EnemyNav enemy = hit.collider.GetComponent<EnemyNav>();
                    if (enemy != null)
                    {
                        pendingEnemy = enemy;
                        pendingDamage = 1;
                        Item weapon = inventory?.GetEquippedItem("Weapon");
                        if (weapon != null) pendingDamage += weapon.value;
                    }
                }
            }
        }

        handsAnimator.SetTrigger(hasWeapon ? "attack" : "punch");
        Invoke(nameof(ApplyStoredHit), 0.2f);
        Invoke(nameof(ResetAttack), 0.6f);
    }

    void ApplyStoredHit()
    {
        if (pendingEnemy != null)
        {
            pendingEnemy.TakeDamage(pendingDamage);
            if (pendingHitPoint != Vector3.zero)
                HitSpark.Spawn(pendingHitPoint, hasWeaponOnAttack);
            AudioClip hit = hasWeaponOnAttack ? weaponHitSound : punchHitSound;
            if (hit != null) audioSource.PlayOneShot(hit, hitVolume);
        }
        else
        {
            AudioClip miss = hasWeaponOnAttack ? weaponMissSound : punchMissSound;
            if (miss != null) audioSource.PlayOneShot(miss, missVolume);
        }
        pendingEnemy = null;
        pendingDamage = 0;
    }

    public void PlayPickup()
    {
        if (handsAnimator != null) handsAnimator.SetTrigger("pickup");
    }

    public void ApplyStoredPickup() { }

    void ResetAttack() => isAttacking = false;

    public void ShowWeaponModel()
    {
        if (weaponHolder == null) return;
        if (currentWeaponModel != null) Destroy(currentWeaponModel);

        currentWeaponModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        currentWeaponModel.transform.SetParent(weaponHolder, false);
        currentWeaponModel.transform.localPosition = Vector3.zero;
        currentWeaponModel.transform.localScale = new Vector3(0.04f, 0.04f, 0.45f);
        currentWeaponModel.layer = LayerMask.NameToLayer("Hands");
        Destroy(currentWeaponModel.GetComponent<Collider>());
    }

    public void HideWeaponModel()
    {
        if (currentWeaponModel != null) Destroy(currentWeaponModel);
        currentWeaponModel = null;
    }
}