using UnityEngine;
using System.Collections;

public class HandController : MonoBehaviour
{
    public enum WeaponMode
    {
        Unarmed,
        OneHand,
        DualWield,
        TwoHand,
        Bow,
        Magic,
        SwordShield
    }

    [Header("Режим оружия")]
    public WeaponMode currentMode = WeaponMode.Unarmed;

    [Header("Аниматоры рук")]
    public Animator rightHandAnimator;
    public Animator leftHandAnimator;
    public Animator twoHandAnimator;

    [Header("Ссылки")]
    public Transform weaponHolder;
    public Inventory inventory;
    public Camera mainCamera;

    [Header("Хитбокс")]
    public float hitSphereRadius = 0.4f;

    [Header("Кулдаун атаки")]
    public float attackCooldown = 0.6f;
    private float lastAttackTime = -10f;

    [Header("Дальность атаки")]
    public float unarmedRange = 2f;
    public float weaponRange = 2.5f;
    public int unarmedDamage = 5;

    [Header("Заряженная атака")]
    private bool wasFullyCharged = false;
    public float chargeTime = 1.5f;
    public float chargeMinTime = 0.3f;
    public int chargeMultiplier = 3;
    public float chargeKnockback = 6f;
    public float fovKickAmount = 20f;
    [Range(0f, 1f)] public float chargeReadyVolume = 0.5f;

    [Header("Звуки оружия")]
    public AudioClip weaponHitSound;
    public AudioClip weaponMissSound;

    [Header("Звук заряда")]
    public AudioClip chargeReadySound;

    [Range(0f, 1f)] public float hitVolume = 0.9f;
    [Range(0f, 1f)] public float missVolume = 0.4f;

    [Header("Блок")]
    public AudioClip shieldRaiseSound;
    [Range(0f, 1f)] public float shieldVolume = 0.7f;

    public static bool IsBlocking { get; private set; } = false;

    public bool IsCharging { get; private set; } = false;
    public float ChargePercent { get; private set; } = 0f;
    public bool IsChargeVisible => IsCharging && chargeTimer >= chargeMinTime;

    private AudioSource audioSource;
    private GameObject currentWeaponModel;
    private bool isAttacking = false;
    private bool chargeReadyPlayed = false;
    private float chargeTimer = 0f;
    private EnemyNav pendingEnemy = null;
    private int pendingDamage = 0;
    private bool hasWeaponOnAttack = false;
    private Vector3 pendingHitPoint;

    public static HandController Instance { get; private set; }
    void Awake() => Instance = this;

    void Start()
    {
        if (inventory == null) inventory = GetComponent<Inventory>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (rightHandAnimator != null)
        {
            rightHandAnimator.ResetTrigger("attack");
            rightHandAnimator.ResetTrigger("pickup");
        }
    }

    void Update()
    {
        if (InventoryUICode.IsOpen || EquipmentUI.IsOpen) return;

        bool hasWeapon = inventory != null && inventory.GetEquippedItem("Weapon") != null;
        bool hasShield = inventory != null && inventory.GetEquippedItem("Shield") != null;

        // Обновляем параметры аниматоров
        UpdateAnimatorParams();

        // Блок
        if (hasShield)
        {
            if (Input.GetMouseButtonDown(1) && !IsBlocking)
            {
                IsBlocking = true;
                leftHandAnimator?.SetBool("isBlocking", true);
                if (shieldRaiseSound != null) audioSource.PlayOneShot(shieldRaiseSound, shieldVolume);
            }
            if (Input.GetMouseButtonUp(1))
            {
                IsBlocking = false;
                leftHandAnimator?.SetBool("isBlocking", false);
            }
        }
        else IsBlocking = false;

        if (IsBlocking) return;

        // Зарядка
        if (Input.GetMouseButton(0) && !isAttacking)
        {
            IsCharging = true;
            chargeTimer += Time.deltaTime;
            ChargePercent = Mathf.Clamp01(chargeTimer / chargeTime);

            if (ChargePercent >= 1f && !chargeReadyPlayed)
            {
                chargeReadyPlayed = true;
                if (chargeReadySound != null)
                    audioSource.PlayOneShot(chargeReadySound, chargeReadyVolume);
            }
        }

        // Отпустил — атака
        if (Input.GetMouseButtonUp(0) && !isAttacking)
        {
            if (Time.time < lastAttackTime + attackCooldown)
            {
                chargeTimer = 0f; ChargePercent = 0f;
                IsCharging = false; chargeReadyPlayed = false;
                return;
            }

            if (chargeTimer >= chargeMinTime)
            {
                wasFullyCharged = ChargePercent >= 1f;
                StartChargedAttack(hasWeapon);
            }
            else
                StartNormalAttack(hasWeapon);

            lastAttackTime = Time.time;
            chargeTimer = 0f;
            ChargePercent = 0f;
            IsCharging = false;
            chargeReadyPlayed = false;
        }

        if (!Input.GetMouseButton(0) && !isAttacking)
        {
            chargeTimer = 0f; ChargePercent = 0f; IsCharging = false;
        }

    }

    void UpdateAnimatorParams()
    {
        CharacterController cc = GetComponent<CharacterController>();
        PlayerMovement pm = GetComponent<PlayerMovement>();

        bool grounded = cc != null && cc.isGrounded;
        bool crouching = pm != null && pm.IsCrouching;

        // Right Hand
        if (rightHandAnimator != null)
        {
            rightHandAnimator.SetBool("isGrounded", grounded);
            rightHandAnimator.SetBool("isCrouching", crouching);
        }

        // Left Hand
        if (leftHandAnimator != null)
        {
            leftHandAnimator.SetBool("isGrounded", grounded);
            leftHandAnimator.SetBool("isCrouching", crouching);
        }

        // Two Hand
        if (twoHandAnimator != null)
        {
            twoHandAnimator.SetBool("isGrounded", grounded);
            twoHandAnimator.SetBool("isCrouching", crouching);
        }
    }

    // ─── Режимы оружия ───────────────────────────────────────────────────

    public void SetWeaponMode(WeaponMode mode)
    {
        currentMode = mode;

        // Сбрасываем всё
        rightHandAnimator?.SetBool("hasWeapon", false);
        rightHandAnimator?.SetBool("hasMagic", false);
        leftHandAnimator?.SetBool("hasWeapon", false);
        leftHandAnimator?.SetBool("hasShield", false);
        leftHandAnimator?.SetBool("hasMagic", false);
        twoHandAnimator?.SetBool("hasWeapon", false);
        twoHandAnimator?.SetBool("hasBow", false);

        switch (mode)
        {
            case WeaponMode.OneHand:
                rightHandAnimator?.SetBool("hasWeapon", true);
                break;

            case WeaponMode.DualWield:
                rightHandAnimator?.SetBool("hasWeapon", true);
                leftHandAnimator?.SetBool("hasWeapon", true);
                break;

            case WeaponMode.TwoHand:
                twoHandAnimator?.SetBool("hasWeapon", true);
                break;

            case WeaponMode.Bow:
                twoHandAnimator?.SetBool("hasBow", true);
                break;

            case WeaponMode.Magic:
                rightHandAnimator?.SetBool("hasMagic", true);
                leftHandAnimator?.SetBool("hasMagic", true);
                break;

            case WeaponMode.SwordShield:
                rightHandAnimator?.SetBool("hasWeapon", true);
                leftHandAnimator?.SetBool("hasShield", true);
                break;
        }
    }

    // ─── Обычный удар ────────────────────────────────────────────────────

    void StartNormalAttack(bool hasWeapon)
    {
        isAttacking = true;
        hasWeaponOnAttack = hasWeapon;
        pendingEnemy = null;
        pendingDamage = 0;

        ScanHit(hasWeapon ? weaponRange : unarmedRange);

        if (hasWeapon)
        {
            rightHandAnimator?.SetTrigger("attack");
            // Для двуручного
            if (currentMode == WeaponMode.TwoHand || currentMode == WeaponMode.Bow)
                twoHandAnimator?.SetTrigger("attack");
        }

        Invoke(nameof(ApplyStoredHit), 0.2f);
        Invoke(nameof(ResetAttack), 0.6f);
    }

    // ─── Заряженный удар ─────────────────────────────────────────────────

    void StartChargedAttack(bool hasWeapon)
    {
        isAttacking = true;
        hasWeaponOnAttack = hasWeapon;
        pendingEnemy = null;
        pendingDamage = 0;

        ScanHit((hasWeapon ? weaponRange : unarmedRange) * 1.3f);

        if (pendingDamage > 0)
        {
            float mult = Mathf.Lerp(1.5f, chargeMultiplier,
                         Mathf.Clamp01(chargeTimer / chargeTime));
            pendingDamage = Mathf.RoundToInt(pendingDamage * mult);
        }

        if (hasWeapon)
        {
            rightHandAnimator?.SetTrigger("attack");
            if (currentMode == WeaponMode.TwoHand || currentMode == WeaponMode.Bow)
                twoHandAnimator?.SetTrigger("attack");
        }

        Invoke(nameof(ApplyChargedHit), 0.25f);
        Invoke(nameof(ResetAttack), 0.7f);
    }

    // ─── Raycast ─────────────────────────────────────────────────────────

    void ScanHit(float range)
    {
        Camera cam = mainCamera ?? Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        int ignoreHandsLayer = ~LayerMask.GetMask("Hands");

        if (Physics.SphereCast(ray, hitSphereRadius, out RaycastHit hit, range, ignoreHandsLayer))
        {
            pendingHitPoint = hit.point;
            if (hit.collider.CompareTag("Enemy"))
            {
                EnemyNav enemy = hit.collider.GetComponent<EnemyNav>();
                if (enemy != null)
                {
                    pendingEnemy = enemy;
                    Item weapon = inventory?.GetEquippedItem("Weapon");
                    pendingDamage = weapon != null ? weapon.value : unarmedDamage;
                }
            }
        }
    }

    // ─── Применение ударов ───────────────────────────────────────────────

    void ApplyStoredHit()
    {
        if (pendingEnemy != null)
        {
            pendingEnemy.TakeDamage(pendingDamage);
            if (pendingHitPoint != Vector3.zero)
                HitSpark.Spawn(pendingHitPoint, hasWeaponOnAttack);
            AudioClip clip = hasWeaponOnAttack ? weaponHitSound : null;
            if (clip != null) audioSource.PlayOneShot(clip, hitVolume);
        }
        else
        {
            AudioClip clip = hasWeaponOnAttack ? weaponMissSound : null;
            if (clip != null) audioSource.PlayOneShot(clip, missVolume);
        }
        pendingEnemy = null; pendingDamage = 0;
    }

    void ApplyChargedHit()
    {
        if (pendingEnemy != null)
        {
            pendingEnemy.TakeDamage(pendingDamage);

            var agent = pendingEnemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                Vector3 dir = (pendingEnemy.transform.position - transform.position).normalized;
                agent.Move(dir * chargeKnockback * Time.deltaTime * 10f);
            }

            if (pendingHitPoint != Vector3.zero)
                HitSpark.Spawn(pendingHitPoint, hasWeaponOnAttack, true);

            if (wasFullyCharged)
            {
                CameraShake.Instance?.Shake(0.4f, 0.25f);
                StartCoroutine(FOVKick());
            }
            else
            {
                CameraShake.Instance?.Shake(0.15f, 0.07f);
            }

            AudioClip clip = hasWeaponOnAttack ? weaponHitSound : null;
            if (clip != null) audioSource.PlayOneShot(clip, hitVolume);
        }
        else
        {
            AudioClip clip = hasWeaponOnAttack ? weaponMissSound : null;
            if (clip != null) audioSource.PlayOneShot(clip, missVolume);
        }
        pendingEnemy = null; pendingDamage = 0;
    }

    IEnumerator FOVKick()
    {
        Camera cam = mainCamera
               ?? Camera.main
               ?? GameObject.Find("MainCamera")?.GetComponent<Camera>();

        if (cam == null) yield break;

        float original = cam.fieldOfView;
        float target = original + fovKickAmount;
        float elapsed = 0f;

        while (elapsed < 0.08f)
        {
            cam.fieldOfView = Mathf.Lerp(original, target, elapsed / 0.08f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            cam.fieldOfView = Mathf.Lerp(target, original, elapsed / 0.2f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cam.fieldOfView = original;
    }

    public void PlayPickup()
    {
        rightHandAnimator?.SetTrigger("pickup");
    }

    void ResetAttack() => isAttacking = false;

    public void ShowWeaponModel()
    {
        if (weaponHolder == null) return;
        Transform sword = weaponHolder.Find("WeaponSword");
        if (sword != null) sword.gameObject.SetActive(true);
    }

    public void HideWeaponModel()
    {
        if (weaponHolder == null) return;
        Transform sword = weaponHolder.Find("WeaponSword");
        if (sword != null) sword.gameObject.SetActive(false);
    }
}