using UnityEngine;
using System.Collections;

public class HandController : MonoBehaviour
{
    public enum WeaponMode
    {
        Unarmed,
        OneHand,
        OneHandLeft,
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
    public Transform weaponHolderLeft;
    public Inventory inventory;
    public Camera mainCamera;

    [Header("Хитбокс")]
    public float hitSphereRadius = 0.4f;

    [Header("Кулдаун атаки")]
    public float attackCooldown = 0.6f;
    private float lastAttackTimeRight = -10f;
    private float lastAttackTimeLeft = -10f;

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

    // Отложенный блок
    private bool blockQueued = false;

    // Задержка блока после приземления
    private bool wasGroundedLastFrame = true;
    private float landingBlockDelay = 0.3f;
    private float landingBlockTimer = 0f;

    // Задержка атаки после приземления
    private float landingAttackDelay = 0.15f;
    private float landingAttackTimer = 0f;

    // Grace period для блока
    private float groundedGraceTimer = 0f;
    private float groundedGraceTime = 0.15f;

    // Правая рука
    public bool IsCharging { get; private set; } = false;
    public float ChargePercent { get; private set; } = 0f;
    public bool IsChargeVisible => IsCharging && chargeTimer >= chargeMinTime;
    private float chargeTimer = 0f;
    private bool chargeReadyPlayed = false;

    // Левая рука
    public bool IsChargingLeft { get; private set; } = false;
    public float ChargePercentLeft { get; private set; } = 0f;
    private float chargeTimerLeft = 0f;

    private AudioSource audioSource;
    private bool isAttackingRight = false;
    private bool isAttackingLeft = false;
    private EnemyNav pendingEnemy = null;
    private int pendingDamage = 0;
    private bool hasWeaponOnAttack = false;
    private Vector3 pendingHitPoint;

    // Защита от сброса блока во время BlockHit
    private bool isReceivingBlockHit = false;

    // ✅ Кулдаун атаки в воздухе — чтобы не спамил
    private float airAttackCooldown = 0.8f;
    private float lastAirAttackTimeRight = -10f;
    private float lastAirAttackTimeLeft = -10f;

    public static HandController Instance { get; private set; }
    void Awake() => Instance = this;

    void Start()
    {
        if (inventory == null) inventory = GetComponent<Inventory>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        rightHandAnimator?.ResetTrigger("attack");
        rightHandAnimator?.ResetTrigger("pickup");
    }

    void Update()
    {
        if (InventoryUICode.IsOpen || EquipmentUI.IsOpen) return;

        Item leftSlotItem = inventory?.GetEquippedItem("WeaponLeft");
        bool hasShield = leftSlotItem != null &&
                         (leftSlotItem.itemName == "Shield" ||
                          leftSlotItem.itemType == "Shield" ||
                          leftSlotItem.originalType == "Shield");

        UpdateAnimatorParams();

        CharacterController cc = GetComponent<CharacterController>();
        bool grounded = cc != null && cc.isGrounded;

        // Отслеживаем приземление
        bool justLanded = !wasGroundedLastFrame && grounded;
        if (justLanded)
        {
            landingBlockTimer = landingBlockDelay;
            landingAttackTimer = landingAttackDelay;
        }

        if (landingBlockTimer > 0f) landingBlockTimer -= Time.deltaTime;
        if (landingAttackTimer > 0f) landingAttackTimer -= Time.deltaTime;

        wasGroundedLastFrame = grounded;

        // ─── Блок (ПКМ) ──────────────────────────────────────────────
        if (hasShield)
        {
            if (Input.GetMouseButtonDown(1))
                blockQueued = true;

            if (Input.GetMouseButtonUp(1))
            {
                blockQueued = false;
                if (IsBlocking)
                {
                    IsBlocking = false;
                    leftHandAnimator?.SetBool("isBlocking", false);
                    leftHandAnimator?.ResetTrigger("BlockHit");
                }
            }

            if (grounded)
                groundedGraceTimer = groundedGraceTime;
            else
                groundedGraceTimer -= Time.deltaTime;

            bool effectivelyGrounded = grounded || groundedGraceTimer > 0f;

            bool canBlock = blockQueued &&
                            effectivelyGrounded &&
                            !isAttackingRight &&
                            !isAttackingLeft &&
                            landingBlockTimer <= 0f;

            if (canBlock && !IsBlocking)
            {
                IsBlocking = true;
                leftHandAnimator?.SetBool("isBlocking", true);
                if (shieldRaiseSound != null)
                    audioSource.PlayOneShot(shieldRaiseSound, shieldVolume);
            }

            if (IsBlocking && (!effectivelyGrounded || isAttackingRight || isAttackingLeft)
                && !isReceivingBlockHit)
            {
                IsBlocking = false;
                leftHandAnimator?.SetBool("isBlocking", false);
            }
        }
        else
        {
            IsBlocking = false;
            blockQueued = false;
            leftHandAnimator?.SetBool("isBlocking", false);
        }

        if (IsBlocking) return;

        // ─── Правая рука (ЛКМ) ───────────────────────────────────────
        bool hasRightWeapon = false;
        Item rightWeapon = inventory?.GetEquippedItem("Weapon");
        if (rightWeapon != null && rightWeapon.itemType == "Weapon")
            hasRightWeapon = true;

        if (currentMode == WeaponMode.SwordShield && hasRightWeapon)
            hasRightWeapon = true;

        if (currentMode == WeaponMode.OneHand || currentMode == WeaponMode.DualWield)
            hasRightWeapon = (rightWeapon != null && rightWeapon.itemType == "Weapon");

        if (!grounded && hasRightWeapon)
        {
            // ✅ Атака в воздухе — только по клику, без заряда
            if (Input.GetMouseButtonDown(0) && !isAttackingRight && !isAttackingLeft
                && Time.time >= lastAirAttackTimeRight + airAttackCooldown)
            {
                StartAirAttack(hasRightWeapon, false);
                lastAirAttackTimeRight = Time.time;
            }
        }
        else
        {
            // Обычная атака на земле с зарядом
            if (Input.GetMouseButton(0) && !isAttackingRight && !isAttackingLeft && hasRightWeapon)
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

            if (Input.GetMouseButtonUp(0) && !isAttackingRight && !isAttackingLeft && hasRightWeapon)
            {
                if (Time.time >= lastAttackTimeRight + attackCooldown
                    && landingAttackTimer <= 0f)
                {
                    if (chargeTimer >= chargeMinTime)
                    {
                        wasFullyCharged = ChargePercent >= 1f;
                        StartChargedAttack(hasRightWeapon, false);
                    }
                    else
                        StartNormalAttack(hasRightWeapon, false);

                    lastAttackTimeRight = Time.time;
                    chargeTimer = 0f; ChargePercent = 0f;
                    IsCharging = false; chargeReadyPlayed = false;
                }
            }

            if ((!Input.GetMouseButton(0) || !hasRightWeapon) && !isAttackingRight && !isAttackingLeft)
            {
                chargeTimer = 0f; ChargePercent = 0f; IsCharging = false;
            }
        }

        // ─── Левая рука (ПКМ) ────────────────────────────────────────
        bool hasLeftWeapon = false;
        if (leftSlotItem != null)
        {
            bool isShieldItem = leftSlotItem.itemName == "Shield" ||
                                leftSlotItem.itemType == "Shield" ||
                                leftSlotItem.originalType == "Shield";
            if (!isShieldItem && (leftSlotItem.itemType == "Weapon" ||
                                  leftSlotItem.itemType == "WeaponLeft"))
                hasLeftWeapon = true;
        }

        if (!IsBlocking && !isAttackingLeft && !isAttackingRight && hasLeftWeapon)
        {
            if (!grounded)
            {
                // ✅ Атака левой в воздухе
                if (Input.GetMouseButtonDown(1) && !isAttackingRight && !isAttackingLeft
                    && Time.time >= lastAirAttackTimeLeft + airAttackCooldown)
                {
                    StartAirAttack(false, true);
                    lastAirAttackTimeLeft = Time.time;
                }
            }
            else
            {
                if (Input.GetMouseButton(1))
                {
                    IsChargingLeft = true;
                    chargeTimerLeft += Time.deltaTime;
                    ChargePercentLeft = Mathf.Clamp01(chargeTimerLeft / chargeTime);
                }

                if (Input.GetMouseButtonUp(1))
                {
                    if (Time.time >= lastAttackTimeLeft + attackCooldown
                        && landingAttackTimer <= 0f)
                    {
                        if (chargeTimerLeft >= chargeMinTime)
                        {
                            wasFullyCharged = ChargePercentLeft >= 1f;
                            StartChargedAttack(false, true);
                        }
                        else
                            StartNormalAttack(false, true);

                        lastAttackTimeLeft = Time.time;
                        chargeTimerLeft = 0f; ChargePercentLeft = 0f; IsChargingLeft = false;
                    }
                }

                if (!Input.GetMouseButton(1))
                {
                    chargeTimerLeft = 0f; ChargePercentLeft = 0f; IsChargingLeft = false;
                }
            }
        }
        else
        {
            chargeTimerLeft = 0f; ChargePercentLeft = 0f; IsChargingLeft = false;
        }
    }

    // ✅ Атака в воздухе — без заряда, мгновенный удар
    void StartAirAttack(bool hasWeapon, bool isLeftHand = false)
    {
        if (isLeftHand) isAttackingLeft = true;
        else isAttackingRight = true;

        hasWeaponOnAttack = hasWeapon;
        pendingEnemy = null;
        pendingDamage = 0;

        ScanHit(hasWeapon ? weaponRange : unarmedRange, !isLeftHand);

        if (isLeftHand)
            leftHandAnimator?.SetTrigger("attack");
        else
            rightHandAnimator?.SetTrigger("attack");

        Invoke(nameof(ApplyStoredHit), 0.2f);
        if (isLeftHand) Invoke(nameof(ResetAttackLeft), 0.5f);
        else Invoke(nameof(ResetAttackRight), 0.5f);
    }

    void UpdateAnimatorParams()
    {
        CharacterController cc = GetComponent<CharacterController>();
        PlayerMovement pm = GetComponent<PlayerMovement>();

        bool grounded = cc != null && cc.isGrounded;
        bool crouching = pm != null && pm.IsCrouching;

        if (rightHandAnimator != null)
        {
            rightHandAnimator.SetBool("isGrounded", grounded);
            rightHandAnimator.SetBool("isCrouching", crouching);
        }

        if (leftHandAnimator != null)
        {
            leftHandAnimator.SetBool("isGrounded", grounded);
            leftHandAnimator.SetBool("isCrouching", crouching);
        }

        if (twoHandAnimator != null)
        {
            twoHandAnimator.SetBool("isGrounded", grounded);
            twoHandAnimator.SetBool("isCrouching", crouching);
        }
    }

    public void SetWeaponMode(WeaponMode mode)
    {
        currentMode = mode;

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
            case WeaponMode.OneHandLeft:
                leftHandAnimator?.SetBool("hasWeapon", true);
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

    public void RefreshLeftHandAnimator()
    {
        Item leftItem = inventory?.GetEquippedItem("WeaponLeft");
        bool isShield = leftItem != null &&
                        (leftItem.itemType == "Shield" || leftItem.originalType == "Shield");

        leftHandAnimator?.SetBool("hasShield", isShield);
        leftHandAnimator?.SetBool("hasWeapon", !isShield && leftItem != null);
    }

    public void TriggerBlockHit()
    {
        isReceivingBlockHit = true;
        leftHandAnimator?.SetTrigger("BlockHit");
        Invoke(nameof(ResetBlockHit), 0.4f);
    }

    void ResetBlockHit() => isReceivingBlockHit = false;

    public void ShowShieldModel()
    {
        Transform shield = weaponHolderLeft?.Find("Shield");
        if (shield != null) shield.gameObject.SetActive(true);
        Transform sword = weaponHolderLeft?.Find("OneHandWeaponLeft");
        if (sword != null) sword.gameObject.SetActive(false);
    }

    public void HideShieldModel()
    {
        Transform shield = weaponHolderLeft?.Find("Shield");
        if (shield != null) shield.gameObject.SetActive(false);
    }

    void StartNormalAttack(bool hasWeapon, bool isLeftHand = false)
    {
        if (isLeftHand) isAttackingLeft = true;
        else isAttackingRight = true;

        hasWeaponOnAttack = hasWeapon;
        pendingEnemy = null;
        pendingDamage = 0;

        ScanHit(hasWeapon ? weaponRange : unarmedRange, !isLeftHand);

        if (isLeftHand)
            leftHandAnimator?.SetTrigger("attack");
        else
        {
            if (hasWeapon) rightHandAnimator?.SetTrigger("attack");
            if (currentMode == WeaponMode.TwoHand || currentMode == WeaponMode.Bow)
                twoHandAnimator?.SetTrigger("attack");
        }

        Invoke(nameof(ApplyStoredHit), 0.2f);
        if (isLeftHand) Invoke(nameof(ResetAttackLeft), 0.6f);
        else Invoke(nameof(ResetAttackRight), 0.6f);
    }

    void StartChargedAttack(bool hasWeapon, bool isLeftHand = false)
    {
        if (isLeftHand) isAttackingLeft = true;
        else isAttackingRight = true;

        hasWeaponOnAttack = hasWeapon;
        pendingEnemy = null;
        pendingDamage = 0;

        float range = (hasWeapon ? weaponRange : unarmedRange) * 1.3f;
        ScanHit(range, !isLeftHand);

        if (pendingDamage > 0)
        {
            float timerToUse = isLeftHand ? chargeTimerLeft : chargeTimer;
            float mult = Mathf.Lerp(1.5f, chargeMultiplier,
                         Mathf.Clamp01(timerToUse / chargeTime));
            pendingDamage = Mathf.RoundToInt(pendingDamage * mult);
        }

        if (isLeftHand)
            leftHandAnimator?.SetTrigger("attack");
        else
        {
            if (hasWeapon) rightHandAnimator?.SetTrigger("attack");
            if (currentMode == WeaponMode.TwoHand || currentMode == WeaponMode.Bow)
                twoHandAnimator?.SetTrigger("attack");
        }

        Invoke(nameof(ApplyChargedHit), 0.25f);
        if (isLeftHand) Invoke(nameof(ResetAttackLeft), 0.7f);
        else Invoke(nameof(ResetAttackRight), 0.7f);
    }

    void ScanHit(float range, bool forRightHand = true)
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
                    Item weapon = null;

                    if (forRightHand)
                        weapon = inventory?.GetEquippedItem("Weapon");
                    else
                    {
                        weapon = inventory?.GetEquippedItem("WeaponLeft");
                        if (weapon != null && (weapon.itemName == "Shield" ||
                            weapon.itemType == "Shield" || weapon.originalType == "Shield"))
                            weapon = null;
                    }

                    pendingDamage = weapon != null ? weapon.value : unarmedDamage;
                }
            }
        }
    }

    void ApplyStoredHit()
    {
        if (pendingEnemy != null)
        {
            pendingEnemy.TakeDamage(pendingDamage);
            if (pendingHitPoint != Vector3.zero)
                HitSpark.Spawn(pendingHitPoint, hasWeaponOnAttack);
            if (weaponHitSound != null) audioSource.PlayOneShot(weaponHitSound, hitVolume);
        }
        else
        {
            if (weaponMissSound != null) audioSource.PlayOneShot(weaponMissSound, missVolume);
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
                CameraShake.Instance?.Shake(0.15f, 0.07f);

            if (weaponHitSound != null) audioSource.PlayOneShot(weaponHitSound, hitVolume);
        }
        else
        {
            if (weaponMissSound != null) audioSource.PlayOneShot(weaponMissSound, missVolume);
        }
        pendingEnemy = null; pendingDamage = 0;
    }

    IEnumerator FOVKick()
    {
        Camera cam = mainCamera ?? Camera.main;
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

    public void PlayPickup() => rightHandAnimator?.SetTrigger("pickup");
    public void ResetPickup() => rightHandAnimator?.ResetTrigger("pickup");
    void ResetAttackRight() => isAttackingRight = false;
    void ResetAttackLeft() => isAttackingLeft = false;

    public void ShowWeaponModel()
    {
        Transform sword = weaponHolder?.Find("OneHandWeaponRight");
        if (sword != null) sword.gameObject.SetActive(true);
    }

    public void ShowWeaponModelLeft()
    {
        Transform sword = weaponHolderLeft?.Find("OneHandWeaponLeft");
        if (sword != null) sword.gameObject.SetActive(true);
    }

    public void HideWeaponModel()
    {
        Transform sword = weaponHolder?.Find("OneHandWeaponRight");
        if (sword != null) sword.gameObject.SetActive(false);
    }

    public void HideWeaponModelLeft()
    {
        Transform sword = weaponHolderLeft?.Find("OneHandWeaponLeft");
        if (sword != null) sword.gameObject.SetActive(false);
    }
}