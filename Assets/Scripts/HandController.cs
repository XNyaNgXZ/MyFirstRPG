using UnityEngine;

public class HandController : MonoBehaviour
{
    public enum WeaponMode
    {
        Unarmed, OneHand, OneHandLeft, DualWield, TwoHand, Bow, Magic, SwordShield
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

    [Header("Замах двуручного")]
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

    [Header("Магия — точка спавна снарядов")]
    public Transform spellSpawnPointRight;
    public Transform spellSpawnPointLeft;

    [Header("Звуки оружия")]
    public AudioClip weaponHitSound;
    public AudioClip weaponMissSound;
    public AudioClip weaponWindUpSound;
    public AudioClip bowDrawSound;
    public AudioClip bowShootSound;

    [Header("Звуки магии (запасные)")]
    public AudioClip spellCastSound;
    public AudioClip spellHealSound;
    [Range(0f, 1f)] public float spellVolume = 0.8f;
    [Range(0f, 1f)] public float hitVolume = 0.9f;
    [Range(0f, 1f)] public float missVolume = 0.4f;
    [Range(0f, 1f)] public float windUpVolume = 0.5f;
    [Range(0f, 1f)] public float bowVolume = 1.0f;

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
    private EnemyNav pendingEnemy = null;
    private int pendingDamage = 0;
    private bool hasWeaponOnAttack = false;
    private Vector3 pendingHitPoint;
    private bool isReceivingBlockHit = false;

    // Магия
    private bool isHealingRight = false;
    private bool isHealingLeft = false;
    private float healAccumulatorRight = 0f;
    private float healAccumulatorLeft = 0f;
    private GameObject healParticlesRight = null;
    private GameObject healParticlesLeft = null;

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
            isWindingUpRight = false; windUpTimerRight = 0f;
            isWindingUpLeft = false; windUpTimerLeft = 0f;
            rightHandAnimator?.ResetTrigger("windUp");
            rightHandAnimator?.SetTrigger("cancelWindUp");
            leftHandAnimator?.ResetTrigger("windUp");
            leftHandAnimator?.SetTrigger("cancelWindUp");
        }
        if (landingBlockTimer > 0f) landingBlockTimer -= Time.deltaTime;
        if (landingAttackTimer > 0f) landingAttackTimer -= Time.deltaTime;
        wasGroundedLastFrame = grounded;

        // ─── Блок ────────────────────────────────────────────────────
        if (hasShield)
        {
            if (Input.GetMouseButtonDown(1)) blockQueued = true;
            if (Input.GetMouseButtonUp(1))
            {
                blockQueued = false;
                if (IsBlocking) { IsBlocking = false; leftHandAnimator?.SetBool("isBlocking", false); }
            }

            if (grounded) groundedGraceTimer = groundedGraceTime;
            else groundedGraceTimer -= Time.deltaTime;
            bool effectivelyGrounded = grounded || groundedGraceTimer > 0f;

            bool canBlock = blockQueued && effectivelyGrounded && !isWindingUpRight && landingBlockTimer <= 0f;
            if (canBlock && !IsBlocking)
            {
                IsBlocking = true;
                leftHandAnimator?.SetBool("isBlocking", true);
                if (shieldRaiseSound != null) audioSource.PlayOneShot(shieldRaiseSound, shieldVolume);
            }
            if (IsBlocking && (!effectivelyGrounded || isWindingUpRight) && !isReceivingBlockHit)
            { IsBlocking = false; leftHandAnimator?.SetBool("isBlocking", false); }
            if (IsBlocking) return;
        }
        else
        {
            IsBlocking = false; blockQueued = false;
            leftHandAnimator?.SetBool("isBlocking", false);
        }

        // ─── Лук ─────────────────────────────────────────────────────
        if (currentMode == WeaponMode.Bow)
        {
            if (!grounded) { if (isDrawingBow) { isDrawingBow = false; bowDrawTimer = 0f; } }
            else
            {
                if (Input.GetMouseButtonDown(0) && !isDrawingBow && landingAttackTimer <= 0f)
                {
                    if (HasArrows())
                    {
                        isDrawingBow = true; bowDrawTimer = 0f;
                        twoHandAnimator?.SetTrigger("draw");
                        if (bowDrawSound != null)
                        {
                            audioSource.clip = bowDrawSound;
                            audioSource.loop = true;
                            audioSource.volume = bowVolume;
                            audioSource.Play();
                        }
                    }
                }
                if (Input.GetMouseButton(0) && isDrawingBow)
                    bowDrawTimer = Mathf.Min(bowDrawTimer + Time.deltaTime, bowMaxDrawTime);
                if (Input.GetMouseButtonUp(0) && isDrawingBow)
                {
                    audioSource.loop = false; audioSource.Stop();
                    if (bowDrawTimer >= bowMinDrawTime && Time.time >= lastAttackTimeRight + attackCooldown)
                    {
                        ShootArrow(); twoHandAnimator?.SetTrigger("shoot");
                        lastAttackTimeRight = Time.time;
                    }
                    else twoHandAnimator?.ResetTrigger("draw");
                    isDrawingBow = false; bowDrawTimer = 0f;
                }
            }
            return;
        }

        // ─── Двуручное ───────────────────────────────────────────────
        if (currentMode == WeaponMode.TwoHand)
        {
            HandleTwoHand(grounded);
            return;
        }

        // ─── Магия + оружие ──────────────────────────────────────────
        SpellDefinition rightSpell = inventory?.GetEquippedSpell("Weapon");
        SpellDefinition leftSpell = inventory?.GetEquippedSpell("WeaponLeft");
        Item rightWeapon = inventory?.GetEquippedItem("Weapon");

        if (rightSpell != null)
            HandleSpellInput(true, rightSpell, grounded, ref isHealingRight, ref lastAttackTimeRight);
        else
            HandleWeaponInput(true, rightWeapon, grounded);

        if (!hasShield)
        {
            if (leftSpell != null)
                HandleSpellInput(false, leftSpell, grounded, ref isHealingLeft, ref lastAttackTimeLeft);
            else
                HandleWeaponInput(false, inventory?.GetEquippedItem("WeaponLeft"), grounded);
        }
    }

    // ─── Магия ───────────────────────────────────────────────────────

    void HandleSpellInput(bool isRight, SpellDefinition spell, bool grounded,
                          ref bool isHealing, ref float lastAttackTime)
    {
        int mouseBtn = isRight ? 0 : 1;
        PlayerMana mana = PlayerMana.Instance;

        if (spell.spellType == "Heal")
        {
            if (Input.GetMouseButtonDown(mouseBtn))
            {
                isHealing = mana != null && mana.HasMana(0.1f);
                if (isHealing)
                {
                    AudioClip healClip = spell.chargeSound != null ? spell.chargeSound : spellHealSound;
                    if (healClip != null)
                    {
                        audioSource.clip = healClip;
                        audioSource.loop = true;
                        audioSource.volume = spellVolume * 0.5f;
                        audioSource.Play();
                    }
                    if (spell.projectileParticles != null)
                        SpawnHealParticles(spell.projectileParticles, isRight);
                }
            }

            if (Input.GetMouseButton(mouseBtn) && isHealing)
            {
                float cost = spell.manaCostPerSecond * Time.deltaTime;
                if (mana != null && mana.HasMana(cost))
                {
                    mana.UseManaUnchecked(cost);
                    float acc = isRight ? healAccumulatorRight : healAccumulatorLeft;
                    acc += spell.healPerSecond * Time.deltaTime;
                    int healAmount = Mathf.FloorToInt(acc);
                    if (healAmount > 0)
                    {
                        acc -= healAmount;
                        PlayerHealth ph = GetComponent<PlayerHealth>();
                        if (ph != null) ph.Heal(healAmount);
                    }
                    if (isRight) healAccumulatorRight = acc;
                    else healAccumulatorLeft = acc;
                }
                else
                {
                    isHealing = false;
                    audioSource.loop = false; audioSource.Stop();
                    DestroyHealParticles(isRight);
                }
            }

            if (Input.GetMouseButtonUp(mouseBtn))
            {
                isHealing = false;
                audioSource.loop = false; audioSource.Stop();
                if (isRight) healAccumulatorRight = 0f;
                else healAccumulatorLeft = 0f;
                DestroyHealParticles(isRight);
            }
            return;
        }

        // Снаряды
        if (Input.GetMouseButtonDown(mouseBtn) && grounded && Time.time >= lastAttackTime + attackCooldown)
        {
            if (mana != null && mana.UseMana(spell.manaCost))
            {
                CastSpell(spell, isRight);
                lastAttackTime = Time.time;
                if (isRight) rightHandAnimator?.SetTrigger("attack");
                else leftHandAnimator?.SetTrigger("attack");
            }
        }
    }

    void CastSpell(SpellDefinition spell, bool isRight)
    {
        Camera cam = mainCamera ?? Camera.main;
        if (cam == null) return;
        Transform spawnPoint = isRight ? spellSpawnPointRight : spellSpawnPointLeft;
        Vector3 spawnPos = spawnPoint != null
            ? spawnPoint.position
            : cam.transform.position + cam.transform.forward * 0.5f;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        SpellProjectile.Spawn(spell, spawnPos, ray.direction);
        AudioClip castClip = spell.castSound != null ? spell.castSound : spellCastSound;
        if (castClip != null) audioSource.PlayOneShot(castClip, spellVolume);
    }

    void SpawnHealParticles(GameObject prefab, bool isRight)
    {
        if (isRight)
        {
            if (healParticlesRight != null) Destroy(healParticlesRight);
            healParticlesRight = Instantiate(prefab, transform.position, Quaternion.identity);
            healParticlesRight.transform.SetParent(transform);
            healParticlesRight.transform.localPosition = Vector3.up * 0.5f;
        }
        else
        {
            if (healParticlesLeft != null) Destroy(healParticlesLeft);
            healParticlesLeft = Instantiate(prefab, transform.position, Quaternion.identity);
            healParticlesLeft.transform.SetParent(transform);
            healParticlesLeft.transform.localPosition = Vector3.up * 0.5f;
        }
    }

    void DestroyHealParticles(bool isRight)
    {
        if (isRight)
        {
            if (healParticlesRight != null)
            {
                // ✅ Останавливаем эмиссию но даём частицам доиграть
                var ps = healParticlesRight.GetComponent<ParticleSystem>();
                if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(healParticlesRight, ps != null ? ps.main.startLifetime.constantMax : 2f);
                healParticlesRight = null;
            }
        }
        else
        {
            if (healParticlesLeft != null)
            {
                var ps = healParticlesLeft.GetComponent<ParticleSystem>();
                if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(healParticlesLeft, ps != null ? ps.main.startLifetime.constantMax : 2f);
                healParticlesLeft = null;
            }
        }
    }

    // ─── Оружие (Lunacid стиль) ───────────────────────────────────────

    void HandleWeaponInput(bool isRight, Item weapon, bool grounded)
    {
        if (weapon == null) return;
        bool isWeapon = weapon.itemType == "Weapon" || weapon.itemType == "WeaponLeft";
        if (!isWeapon) return;

        int mouseBtn = isRight ? 0 : 1;
        ref float lastTime = ref lastAttackTimeRight;
        ref bool isWindingUp = ref isWindingUpRight;
        ref float windUpTimer = ref windUpTimerRight;
        if (!isRight)
        {
            lastTime = ref lastAttackTimeLeft;
            isWindingUp = ref isWindingUpLeft;
            windUpTimer = ref windUpTimerLeft;
        }

        if (!grounded)
        {
            if (isWindingUp)
            {
                isWindingUp = false; windUpTimer = 0f;
                if (isRight) { rightHandAnimator?.ResetTrigger("windUp"); rightHandAnimator?.SetTrigger("cancelWindUp"); }
                else { leftHandAnimator?.ResetTrigger("windUp"); leftHandAnimator?.SetTrigger("cancelWindUp"); }
            }
            return;
        }

        if (Input.GetMouseButtonDown(mouseBtn) && landingAttackTimer <= 0f && Time.time >= lastTime + 0.15f)
        {
            isWindingUp = true; windUpTimer = 0f;
            if (isRight)
            {
                rightHandAnimator?.ResetTrigger("strike");
                rightHandAnimator?.ResetTrigger("cancelWindUp");
                rightHandAnimator?.SetTrigger("windUp");
            }
            else
            {
                leftHandAnimator?.ResetTrigger("strike");
                leftHandAnimator?.ResetTrigger("cancelWindUp");
                leftHandAnimator?.SetTrigger("windUp");
            }
            if (weaponWindUpSound != null) audioSource.PlayOneShot(weaponWindUpSound, windUpVolume);
        }

        if (Input.GetMouseButton(mouseBtn) && isWindingUp)
            windUpTimer = Mathf.Min(windUpTimer + Time.deltaTime, maxWindUpTime);

        if (Input.GetMouseButtonUp(mouseBtn) && isWindingUp)
        {
            if (isRight) rightHandAnimator?.ResetTrigger("windUp");
            else leftHandAnimator?.ResetTrigger("windUp");
            DoStrike(isRight, windUpTimer);
            lastTime = Time.time;
            isWindingUp = false; windUpTimer = 0f;
        }
    }

    void DoStrike(bool isRight, float windUp = 0f)
    {
        hasWeaponOnAttack = isRight;
        pendingEnemy = null;

        float chargePercent = Mathf.Clamp01(windUp / maxWindUpTime);
        float damageMultiplier = Mathf.Lerp(0.5f, 2f, chargePercent);

        Item weapon = isRight
            ? inventory?.GetEquippedItem("Weapon")
            : inventory?.GetEquippedItem("WeaponLeft");

        pendingDamage = weapon != null
            ? Mathf.RoundToInt(weapon.value * damageMultiplier)
            : Mathf.RoundToInt(unarmedDamage * damageMultiplier);

        ScanHit(weaponRange * 1.2f, isRight);

        if (isRight) { rightHandAnimator?.ResetTrigger("strike"); rightHandAnimator?.SetTrigger("strike"); }
        else { leftHandAnimator?.ResetTrigger("strike"); leftHandAnimator?.SetTrigger("strike"); }

        Invoke(nameof(ApplyStoredHit), 0.15f);
    }

    void HandleTwoHand(bool grounded)
    {
        if (!grounded)
        {
            if (isWindingUpRight) { isWindingUpRight = false; windUpTimerRight = 0f; }
            return;
        }

        if (Input.GetMouseButtonDown(0) && landingAttackTimer <= 0f)
        {
            isWindingUpRight = true; windUpTimerRight = 0f;
            twoHandAnimator?.ResetTrigger("strike");
            twoHandAnimator?.ResetTrigger("cancelWindUp");
            twoHandAnimator?.SetTrigger("windUp");
            if (weaponWindUpSound != null) audioSource.PlayOneShot(weaponWindUpSound, windUpVolume);
        }
        if (Input.GetMouseButton(0) && isWindingUpRight)
            windUpTimerRight = Mathf.Min(windUpTimerRight + Time.deltaTime, twoHandMaxWindUpTime);
        if (Input.GetMouseButtonUp(0) && isWindingUpRight)
        {
            if (windUpTimerRight >= twoHandMinWindUpTime && Time.time >= lastAttackTimeRight + attackCooldown)
            {
                hasWeaponOnAttack = true; pendingEnemy = null; pendingDamage = 0;
                ScanHit(weaponRange * 1.3f, true);
                twoHandAnimator?.ResetTrigger("windUp");
                twoHandAnimator?.ResetTrigger("strike");
                twoHandAnimator?.SetTrigger("strike");
                Invoke(nameof(ApplyStoredHit), 0.2f);
                lastAttackTimeRight = Time.time;
            }
            else { twoHandAnimator?.ResetTrigger("windUp"); twoHandAnimator?.SetTrigger("cancelWindUp"); }
            isWindingUpRight = false; windUpTimerRight = 0f;
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
                if (inventory.items[i].quantity <= 0) inventory.RemoveItem(i);
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
        GameObject arrowGO = Instantiate(arrowPrefab, spawnPos, Quaternion.LookRotation(ray.direction));
        Arrow arrow = arrowGO.GetComponent<Arrow>();
        if (arrow != null)
        {
            float drawPercent = Mathf.Clamp01(bowDrawTimer / bowMaxDrawTime);
            arrow.damage = Mathf.RoundToInt(Mathf.Lerp(5f, arrow.damage, drawPercent));
        }
        ConsumeArrow();
        AudioClip shootClip = bowShootSound != null ? bowShootSound : weaponHitSound;
        if (shootClip != null) audioSource.PlayOneShot(shootClip, bowVolume);
    }

    void UpdateAnimatorParams()
    {
        CharacterController cc = GetComponent<CharacterController>();
        PlayerMovement pm = GetComponent<PlayerMovement>();
        bool grounded = cc != null && cc.isGrounded;
        bool crouching = pm != null && pm.IsCrouching;
        rightHandAnimator?.SetBool("isGrounded", grounded);
        rightHandAnimator?.SetBool("isCrouching", crouching);
        leftHandAnimator?.SetBool("isGrounded", grounded);
        leftHandAnimator?.SetBool("isCrouching", crouching);
        twoHandAnimator?.SetBool("isGrounded", grounded);
        twoHandAnimator?.SetBool("isCrouching", crouching);
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
            case WeaponMode.OneHand: rightHandAnimator?.SetBool("hasWeapon", true); break;
            case WeaponMode.OneHandLeft: leftHandAnimator?.SetBool("hasWeapon", true); break;
            case WeaponMode.DualWield:
                rightHandAnimator?.SetBool("hasWeapon", true);
                leftHandAnimator?.SetBool("hasWeapon", true); break;
            case WeaponMode.TwoHand: twoHandAnimator?.SetBool("hasWeapon", true); break;
            case WeaponMode.Bow: twoHandAnimator?.SetBool("hasBow", true); break;
            case WeaponMode.SwordShield:
                rightHandAnimator?.SetBool("hasWeapon", true);
                leftHandAnimator?.SetBool("hasShield", true); break;
            case WeaponMode.Magic:
                bool hasRightSpell = inventory?.GetEquippedSpell("Weapon") != null;
                bool hasLeftSpell = inventory?.GetEquippedSpell("WeaponLeft") != null;
                bool hasRightWeapon = inventory?.GetEquippedItem("Weapon") != null;
                bool hasLeftWeapon = inventory?.GetEquippedItem("WeaponLeft") != null;
                if (hasRightSpell) rightHandAnimator?.SetBool("hasMagic", true);
                else if (hasRightWeapon) rightHandAnimator?.SetBool("hasWeapon", true);
                if (hasLeftSpell) leftHandAnimator?.SetBool("hasMagic", true);
                else if (hasLeftWeapon) leftHandAnimator?.SetBool("hasWeapon", true);
                break;
        }
    }

    public void RefreshLeftHandAnimator()
    {
        Item leftItem = inventory?.GetEquippedItem("WeaponLeft");
        bool isShield = leftItem != null && (leftItem.itemType == "Shield" || leftItem.originalType == "Shield");
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
                    Item weapon = forRightHand
                        ? inventory?.GetEquippedItem("Weapon")
                        : inventory?.GetEquippedItem("WeaponLeft");
                    if (weapon != null && (weapon.itemName == "Shield" ||
                        weapon.itemType == "Shield" || weapon.originalType == "Shield"))
                        weapon = null;
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
            if (pendingHitPoint != Vector3.zero) HitSpark.Spawn(pendingHitPoint, hasWeaponOnAttack);
            if (weaponHitSound != null) audioSource.PlayOneShot(weaponHitSound, hitVolume);
        }
        else { if (weaponMissSound != null) audioSource.PlayOneShot(weaponMissSound, missVolume); }
        pendingEnemy = null; pendingDamage = 0;
    }

    public void PlayPickup() => rightHandAnimator?.SetTrigger("pickup");
    public void ResetPickup() => rightHandAnimator?.ResetTrigger("pickup");

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