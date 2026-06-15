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

    [Header("Объекты рук")]
    public GameObject twoHandObject;

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

    [Header("Замах")]
    public float minWindUpTime = 0.2f;
    public float maxWindUpTime = 1.5f;

    [Header("Замах двуручного (медленнее)")]
    public float twoHandMinWindUpTime = 0.4f;
    public float twoHandMaxWindUpTime = 2f;

    private float windUpTimerRight = 0f;
    private bool isWindingUpRight = false;
    private float windUpTimerLeft = 0f;
    private bool isWindingUpLeft = false;

    [Header("Лук")]
    public GameObject arrowPrefab;
    public Transform arrowSpawnPoint;
    public float bowMinDrawTime = 0.3f;
    public float bowMaxDrawTime = 1.5f;
    private float bowDrawTimer = 0f;
    private bool isDrawingBow = false;

    [Header("Звуки оружия")]
    public AudioClip weaponHitSound;
    public AudioClip weaponMissSound;
    public AudioClip weaponWindUpSound;
    public AudioClip bowDrawSound;   // натяжение тетивы
    public AudioClip bowShootSound;  // выстрел из лука

    [Range(0f, 1f)] public float hitVolume = 0.9f;
    [Range(0f, 1f)] public float missVolume = 0.4f;
    [Range(0f, 1f)] public float windUpVolume = 0.5f;

    [Header("Блок")]
    public AudioClip shieldRaiseSound;
    [Range(0f, 1f)] public float shieldVolume = 0.7f;

    public static bool IsBlocking { get; private set; } = false;

    private bool blockQueued = false;
    private bool wasGroundedLastFrame = true;
    private float landingBlockDelay = 0.3f;
    private float landingBlockTimer = 0f;
    private float landingAttackDelay = 0.15f;
    private float landingAttackTimer = 0f;
    private float groundedGraceTimer = 0f;
    private float groundedGraceTime = 0.15f;

    private AudioSource audioSource;
    private bool isAttackingRight = false;
    private bool isAttackingLeft = false;
    private EnemyNav pendingEnemy = null;
    private int pendingDamage = 0;
    private bool hasWeaponOnAttack = false;
    private Vector3 pendingHitPoint;
    private bool isReceivingBlockHit = false;

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

        bool justLanded = !wasGroundedLastFrame && grounded;
        if (justLanded)
        {
            landingBlockTimer = landingBlockDelay;
            landingAttackTimer = landingAttackDelay;
        }

        if (landingBlockTimer > 0f) landingBlockTimer -= Time.deltaTime;
        if (landingAttackTimer > 0f) landingAttackTimer -= Time.deltaTime;
        wasGroundedLastFrame = grounded;

        // ─── Блок (ПКМ) со щитом ─────────────────────────────────────
        if (hasShield)
        {
            if (Input.GetMouseButtonDown(1)) blockQueued = true;

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

            if (grounded) groundedGraceTimer = groundedGraceTime;
            else groundedGraceTimer -= Time.deltaTime;

            bool effectivelyGrounded = grounded || groundedGraceTimer > 0f;

            bool canBlock = blockQueued && effectivelyGrounded &&
                            !isAttackingRight && !isAttackingLeft &&
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

            if (IsBlocking) return;
        }
        else
        {
            IsBlocking = false;
            blockQueued = false;
            leftHandAnimator?.SetBool("isBlocking", false);
        }

        // ─── Лук ─────────────────────────────────────────────────────
        if (currentMode == WeaponMode.Bow)
        {
            if (!grounded)
            {
                if (isDrawingBow)
                {
                    isDrawingBow = false;
                    bowDrawTimer = 0f;
                    twoHandAnimator?.ResetTrigger("draw");
                }
            }
            else
            {
                // Начало натяжения
                if (Input.GetMouseButtonDown(0) && !isAttackingRight && !isDrawingBow
                    && landingAttackTimer <= 0f)
                {
                    if (HasArrows())
                    {
                        isDrawingBow = true;
                        bowDrawTimer = 0f;
                        twoHandAnimator?.SetTrigger("draw");
                        if (bowDrawSound != null)
                            audioSource.PlayOneShot(bowDrawSound, windUpVolume);
                    }
                    else
                    {
                        Debug.Log("Нет стрел!");
                    }
                }

                // Держим натяжение
                if (Input.GetMouseButton(0) && isDrawingBow)
                    bowDrawTimer = Mathf.Min(bowDrawTimer + Time.deltaTime, bowMaxDrawTime);

                // Отпустили — выстрел
                if (Input.GetMouseButtonUp(0) && isDrawingBow)
                {
                    if (bowDrawTimer >= bowMinDrawTime
                        && Time.time >= lastAttackTimeRight + attackCooldown)
                    {
                        ShootArrow();
                        twoHandAnimator?.SetTrigger("shoot");
                        lastAttackTimeRight = Time.time;
                    }
                    else
                    {
                        twoHandAnimator?.ResetTrigger("draw");
                    }

                    isDrawingBow = false;
                    bowDrawTimer = 0f;
                }
            }
            return;
        }

        // ─── Двуручное (ЛКМ) ─────────────────────────────────────────
        if (currentMode == WeaponMode.TwoHand)
        {
            if (!grounded)
            {
                if (Input.GetMouseButtonDown(0) && !isAttackingRight
                    && Time.time >= lastAirAttackTimeRight + airAttackCooldown)
                {
                    isAttackingRight = true;
                    hasWeaponOnAttack = true;
                    pendingEnemy = null; pendingDamage = 0;
                    ScanHit(weaponRange, true);
                    twoHandAnimator?.SetTrigger("attack");
                    Invoke(nameof(ApplyStoredHit), 0.2f);
                    Invoke(nameof(ResetAttackRight), 0.5f);
                    lastAirAttackTimeRight = Time.time;
                }

                if (isWindingUpRight)
                {
                    isWindingUpRight = false;
                    windUpTimerRight = 0f;
                    twoHandAnimator?.ResetTrigger("windUp");
                    twoHandAnimator?.SetTrigger("cancelWindUp");
                }
            }
            else
            {
                float minTime = twoHandMinWindUpTime;
                float maxTime = twoHandMaxWindUpTime;

                if (Input.GetMouseButtonDown(0) && !isAttackingRight && !isWindingUpRight
                    && landingAttackTimer <= 0f)
                {
                    isWindingUpRight = true;
                    windUpTimerRight = 0f;
                    twoHandAnimator?.SetTrigger("windUp");
                    if (weaponWindUpSound != null)
                        audioSource.PlayOneShot(weaponWindUpSound, windUpVolume);
                }

                if (Input.GetMouseButton(0) && isWindingUpRight)
                    windUpTimerRight = Mathf.Min(windUpTimerRight + Time.deltaTime, maxTime);

                if (Input.GetMouseButtonUp(0) && isWindingUpRight)
                {
                    if (windUpTimerRight >= minTime
                        && Time.time >= lastAttackTimeRight + attackCooldown)
                    {
                        isAttackingRight = true;
                        hasWeaponOnAttack = true;
                        pendingEnemy = null; pendingDamage = 0;
                        ScanHit(weaponRange * 1.3f, true);
                        twoHandAnimator?.ResetTrigger("strike");
                        twoHandAnimator?.SetTrigger("strike");
                        Invoke(nameof(ApplyStoredHit), 0.2f);
                        Invoke(nameof(ResetAttackRight), 0.7f);
                        lastAttackTimeRight = Time.time;
                    }
                    else
                    {
                        twoHandAnimator?.ResetTrigger("windUp");
                        twoHandAnimator?.SetTrigger("cancelWindUp");
                    }

                    isWindingUpRight = false;
                    windUpTimerRight = 0f;
                }
            }
            return;
        }

        // ─── Правая рука (ЛКМ) ───────────────────────────────────────
        bool hasRightWeapon = false;
        Item rightWeapon = inventory?.GetEquippedItem("Weapon");
        if (rightWeapon != null && rightWeapon.itemType == "Weapon")
            hasRightWeapon = true;
        if (currentMode == WeaponMode.SwordShield && hasRightWeapon)
            hasRightWeapon = true;
        if (currentMode == WeaponMode.OneHand || currentMode == WeaponMode.DualWield)
            hasRightWeapon = (rightWeapon != null && rightWeapon.itemType == "Weapon");

        if (hasRightWeapon)
        {
            if (!grounded)
            {
                if (Input.GetMouseButtonDown(0) && !isAttackingRight && !isAttackingLeft
                    && Time.time >= lastAirAttackTimeRight + airAttackCooldown)
                {
                    StartAirAttack(true, false);
                    lastAirAttackTimeRight = Time.time;
                }

                if (isWindingUpRight)
                {
                    isWindingUpRight = false;
                    windUpTimerRight = 0f;
                    rightHandAnimator?.ResetTrigger("windUp");
                    rightHandAnimator?.SetTrigger("cancelWindUp");
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0) && !isAttackingRight && !isAttackingLeft
                    && !isWindingUpRight && !isWindingUpLeft && landingAttackTimer <= 0f)
                {
                    isWindingUpRight = true;
                    windUpTimerRight = 0f;
                    rightHandAnimator?.SetTrigger("windUp");
                    if (weaponWindUpSound != null)
                        audioSource.PlayOneShot(weaponWindUpSound, windUpVolume);
                }

                if (Input.GetMouseButton(0) && isWindingUpRight)
                    windUpTimerRight = Mathf.Min(windUpTimerRight + Time.deltaTime, maxWindUpTime);

                if (Input.GetMouseButtonUp(0) && isWindingUpRight)
                {
                    if (windUpTimerRight >= minWindUpTime
                        && Time.time >= lastAttackTimeRight + attackCooldown)
                    {
                        StartWindUpAttack(true);
                        lastAttackTimeRight = Time.time;
                    }
                    else
                    {
                        rightHandAnimator?.ResetTrigger("windUp");
                        rightHandAnimator?.SetTrigger("cancelWindUp");
                    }

                    isWindingUpRight = false;
                    windUpTimerRight = 0f;
                }
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

        if (hasLeftWeapon)
        {
            if (!grounded)
            {
                if (Input.GetMouseButtonDown(1) && !isAttackingRight && !isAttackingLeft
                    && Time.time >= lastAirAttackTimeLeft + airAttackCooldown)
                {
                    StartAirAttack(false, true);
                    lastAirAttackTimeLeft = Time.time;
                }

                if (isWindingUpLeft)
                {
                    isWindingUpLeft = false;
                    windUpTimerLeft = 0f;
                    leftHandAnimator?.ResetTrigger("windUp");
                    leftHandAnimator?.SetTrigger("cancelWindUp");
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(1) && !isAttackingRight && !isAttackingLeft
                    && !isWindingUpLeft && !isWindingUpRight && landingAttackTimer <= 0f)
                {
                    isWindingUpLeft = true;
                    windUpTimerLeft = 0f;
                    leftHandAnimator?.SetTrigger("windUp");
                    if (weaponWindUpSound != null)
                        audioSource.PlayOneShot(weaponWindUpSound, windUpVolume);
                }

                if (Input.GetMouseButton(1) && isWindingUpLeft)
                    windUpTimerLeft = Mathf.Min(windUpTimerLeft + Time.deltaTime, maxWindUpTime);

                if (Input.GetMouseButtonUp(1) && isWindingUpLeft)
                {
                    if (windUpTimerLeft >= minWindUpTime
                        && Time.time >= lastAttackTimeLeft + attackCooldown)
                    {
                        StartWindUpAttack(false);
                        lastAttackTimeLeft = Time.time;
                    }
                    else
                    {
                        leftHandAnimator?.ResetTrigger("windUp");
                        leftHandAnimator?.SetTrigger("cancelWindUp");
                    }

                    isWindingUpLeft = false;
                    windUpTimerLeft = 0f;
                }
            }
        }
    }

    bool HasArrows()
    {
        if (inventory == null) return false;
        foreach (var item in inventory.items)
            if (item != null && item.itemType == "Arrow") return true;
        return false;
    }

    void ConsumeArrow()
    {
        if (inventory == null) return;
        for (int i = 0; i < inventory.items.Length; i++)
        {
            if (inventory.items[i] != null && inventory.items[i].itemType == "Arrow")
            {
                inventory.items[i].quantity--;
                if (inventory.items[i].quantity <= 0)
                    inventory.RemoveItem(i);
                InventoryUICode.RefreshIfOpen();
                return;
            }
        }
    }

    void ShootArrow()
    {
        if (arrowPrefab == null) return;

        Camera cam = mainCamera ?? Camera.main;
        if (cam == null) return;

        Vector3 spawnPos = arrowSpawnPoint != null
            ? arrowSpawnPoint.position
            : cam.transform.position + cam.transform.forward;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 direction = ray.direction;

        GameObject arrowGO = Instantiate(arrowPrefab, spawnPos, Quaternion.LookRotation(direction));

        Arrow arrow = arrowGO.GetComponent<Arrow>();
        if (arrow != null)
        {
            float drawPercent = Mathf.Clamp01(bowDrawTimer / bowMaxDrawTime);
            arrow.damage = Mathf.RoundToInt(Mathf.Lerp(5f, arrow.damage, drawPercent));
        }

        ConsumeArrow();

        AudioClip shootClip = bowShootSound != null ? bowShootSound : weaponHitSound;
        if (shootClip != null) audioSource.PlayOneShot(shootClip, hitVolume);
    }

    void StartWindUpAttack(bool isRightHand)
    {
        if (isRightHand) isAttackingRight = true;
        else isAttackingLeft = true;

        hasWeaponOnAttack = isRightHand;
        pendingEnemy = null;
        pendingDamage = 0;

        ScanHit(weaponRange * 1.2f, isRightHand);

        if (isRightHand)
        {
            rightHandAnimator?.ResetTrigger("strike");
            rightHandAnimator?.SetTrigger("strike");
            Invoke(nameof(ApplyStoredHit), 0.15f);
            Invoke(nameof(ResetAttackRight), 0.6f);
        }
        else
        {
            leftHandAnimator?.ResetTrigger("strike");
            leftHandAnimator?.SetTrigger("strike");
            Invoke(nameof(ApplyStoredHit), 0.15f);
            Invoke(nameof(ResetAttackLeft), 0.6f);
        }
    }

    void StartAirAttack(bool isRightHand, bool isLeftHand)
    {
        if (isLeftHand) isAttackingLeft = true;
        else isAttackingRight = true;

        hasWeaponOnAttack = isRightHand;
        pendingEnemy = null;
        pendingDamage = 0;

        ScanHit(weaponRange, isRightHand);

        if (isLeftHand) leftHandAnimator?.SetTrigger("attack");
        else rightHandAnimator?.SetTrigger("attack");

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
                rightHandAnimator?.SetBool("hasWeapon", true); break;
            case WeaponMode.OneHandLeft:
                leftHandAnimator?.SetBool("hasWeapon", true); break;
            case WeaponMode.DualWield:
                rightHandAnimator?.SetBool("hasWeapon", true);
                leftHandAnimator?.SetBool("hasWeapon", true); break;
            case WeaponMode.TwoHand:
                twoHandAnimator?.SetBool("hasWeapon", true); break;
            case WeaponMode.Bow:
                twoHandAnimator?.SetBool("hasBow", true); break;
            case WeaponMode.Magic:
                rightHandAnimator?.SetBool("hasMagic", true);
                leftHandAnimator?.SetBool("hasMagic", true); break;
            case WeaponMode.SwordShield:
                rightHandAnimator?.SetBool("hasWeapon", true);
                leftHandAnimator?.SetBool("hasShield", true); break;
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

    public void ShowTwoHandModel()
    {
        if (twoHandObject == null) return;
        Transform weapon = FindDeep(twoHandObject.transform, "TwoHandWeapon");
        Transform bow = FindDeep(twoHandObject.transform, "TwoHandBow");

        if (currentMode == WeaponMode.Bow)
        {
            if (bow != null) bow.gameObject.SetActive(true);
            if (weapon != null) weapon.gameObject.SetActive(false);
        }
        else
        {
            if (weapon != null) weapon.gameObject.SetActive(true);
            if (bow != null) bow.gameObject.SetActive(false);
        }
    }

    public void HideTwoHandModel()
    {
        if (twoHandObject == null) return;
        Transform weapon = FindDeep(twoHandObject.transform, "TwoHandWeapon");
        Transform bow = FindDeep(twoHandObject.transform, "TwoHandBow");
        if (weapon != null) weapon.gameObject.SetActive(false);
        if (bow != null) bow.gameObject.SetActive(false);
    }

    Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }

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

                    if (currentMode == WeaponMode.TwoHand)
                        weapon = inventory?.GetEquippedItem("Weapon");
                    else if (forRightHand)
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