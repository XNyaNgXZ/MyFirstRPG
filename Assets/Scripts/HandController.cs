using UnityEngine;

public class HandController : MonoBehaviour
{
    [Header("Ссылки")]
    public Animator handsAnimator;
    public Transform weaponHolder;
    public Inventory inventory;

    [Header("Звуки кулака")]
    public AudioClip punchSwingSound;    // punchsound.ogg
    public AudioClip punchHitSound;      // PunchHit.ogg
    public AudioClip punchMissSound;     // punchsoundmiss.ogg

    [Header("Звуки оружия")]
    public AudioClip weaponSwingSound;   // SwordSlash.ogg
    public AudioClip weaponHitSound;     // SwordSlash.ogg (или отдельный)
    public AudioClip weaponMissSound;    // WeaponMiss.ogg

    [Range(0f, 1f)] public float swingVolume = 0.6f;
    [Range(0f, 1f)] public float hitVolume = 0.9f;
    [Range(0f, 1f)] public float missVolume = 0.4f;

    private AudioSource audioSource;
    private GameObject currentWeaponModel;
    private bool isAttacking = false;
    private EnemyNav pendingEnemy = null;
    private int pendingDamage = 0;
    private bool hasWeaponOnAttack = false;

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
        if (handsAnimator == null) return;

        bool hasWeapon = inventory != null &&
                         inventory.GetEquippedItem("Weapon") != null;
        handsAnimator.SetBool("hasWeapon", hasWeapon);

        if (Input.GetMouseButtonDown(0) && !isAttacking)
            StartAttack(hasWeapon);
    }

    void StartAttack(bool hasWeapon)
    {
        isAttacking = true;
        hasWeaponOnAttack = hasWeapon;
        pendingEnemy = null;
        pendingDamage = 0;

        // Raycast из центра экрана
        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, 5f))
            {
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

        Invoke(nameof(ApplyStoredHit), 0.2f); // ← урон и звук попадания через 0.2 сек
        Invoke(nameof(ResetAttack), 0.6f);
    }

    // Вызывается Animation Event на кадре удара
public void ApplyStoredHit()
    {
        if (pendingEnemy != null)
        {
            pendingEnemy.TakeDamage(pendingDamage);
            Debug.Log($"Удар! Урон: {pendingDamage}");

            AudioClip hit = hasWeaponOnAttack ? weaponHitSound : punchHitSound;
            if (hit != null)
                audioSource.PlayOneShot(hit, hitVolume);
        }
        else
        {
            // Промах
            AudioClip miss = hasWeaponOnAttack ? weaponMissSound : punchMissSound;
            if (miss != null)
                audioSource.PlayOneShot(miss, missVolume);
        }

        pendingEnemy = null;
        pendingDamage = 0;
    }

    public void ApplyStoredPickup() { }

    public void PlayPickup()
    {
        if (handsAnimator != null)
            handsAnimator.SetTrigger("pickup");
    }

    void ResetAttack() => isAttacking = false;

    public void ShowWeaponModel()
    {
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