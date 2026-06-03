using UnityEngine.AI;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyNav : MonoBehaviour
{
    enum State { Idle, Chasing, Strafing, Telegraph, Attacking, Charging, Retreating }
    private State state = State.Idle;

    [Header("Material / Drop")]
    public Material dropMaterial;

    [Header("Sound")]
    public AudioClip attackHitSound;  // попадание по игроку (без блока)
    public AudioClip attackMissSound; // промах
    public AudioClip chargeSound;     // дэш
    public float attackVolume = 0.5f;

    [Header("Отталкивание")]
    public float knockbackForce = 3f;
    public float knockbackDuration = 0.15f;

    private NavMeshAgent agent;
    private EnemyHealthBar healthBar;

    private Vector3 lastKnownPosition;
    private bool hasLastKnownPosition = false;
    public float forgetTime = 5f;
    private float timeSinceLastSeen = 0f;
    private bool canSeePlayerNow = false;

    [Header("Stats")]
    public int maxHealth = 50;
    private int currentHealth;

    [Header("Обычная атака")]
    public float attackRange = 2f;
    public int attackDamage = 10;
    public float attackCooldown = 2.5f;
    private float lastAttackTime = -10f;

    [Header("Заряд-атака")]
    public int chargeDamage = 20;
    public float chargeSpeed = 8f;
    public float chargeCooldown = 6f;
    public float chargeChance = 0.1f;
    private float lastChargeTime = -10f;

    [Header("Страфинг")]
    public float strafeRadius = 2.5f;
    public float strafeSpeed = 1.5f;
    private float strafeDir = 1f;
    private float strafeTimer = 0f;

    [Header("Отступление")]
    public float retreatHpPercent = 0.3f;
    public float retreatDistance = 4f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float detectionRange = 5f;
    private Transform player;

    [Header("Drop")]
    public GameObject dropItemPrefab;
    public string dropItemName = "Health Potion";
    public string dropItemType = "Potion";
    public int dropItemValue = 25;

    private Renderer rend;
    private Color baseColor;
    private bool isKnockedBack = false;
    private Vector3 originalScale;
    private Coroutine telegraphFlash;

    void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rend = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();

        if (rend != null)
            baseColor = rend.material.HasProperty("_Color")
                ? rend.material.GetColor("_Color")
                : Color.white;

        originalScale = transform.localScale;
        healthBar = gameObject.AddComponent<EnemyHealthBar>();

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = 0f;
            agent.updateRotation = true;
        }

        strafeDir = Random.value > 0.5f ? 1f : -1f;
    }

    void Update()
    {
        if (player == null || agent == null || isKnockedBack) return;
        UpdateVision();
        UpdateState();
        ExecuteState();
    }

    // ─── Визуал состояний ────────────────────────────────────────────────

    void SetStateVisual(State newState)
    {
        transform.localScale = originalScale;

        if (telegraphFlash != null)
        {
            StopCoroutine(telegraphFlash);
            telegraphFlash = null;
        }

        if (rend != null) rend.material.SetColor("_Color", baseColor);

        switch (newState)
        {
            case State.Telegraph:
                telegraphFlash = StartCoroutine(FlashColor(Color.yellow, 0.15f));
                break;

            case State.Charging:
                if (rend != null) rend.material.SetColor("_Color", Color.red);
                transform.localScale = new Vector3(
                    originalScale.x * 0.8f,
                    originalScale.y * 0.8f,
                    originalScale.z * 1.4f);
                break;

            case State.Retreating:
                if (rend != null) rend.material.SetColor("_Color",
                    new Color(0.2f, 0.3f, 0.8f));
                break;
        }
    }

    IEnumerator FlashColor(Color flash, float interval)
    {
        while (true)
        {
            if (rend != null) rend.material.SetColor("_Color", flash);
            yield return new WaitForSeconds(interval);
            if (rend != null) rend.material.SetColor("_Color", baseColor);
            yield return new WaitForSeconds(interval);
        }
    }

    // ─── Зрение ──────────────────────────────────────────────────────────

    void UpdateVision()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        canSeePlayerNow = false;

        if (distance <= detectionRange)
        {
            Vector3 startPoint = transform.position + Vector3.up * 0.5f;
            CharacterController playerCC = player.GetComponent<CharacterController>();
            float centerY = playerCC != null ? playerCC.center.y : 0.8f;
            Vector3 playerCenter = player.position + Vector3.up * centerY;
            Vector3 dir = (playerCenter - startPoint).normalized;

            if (Physics.Raycast(startPoint, dir, out RaycastHit hit, detectionRange))
                if (hit.collider.CompareTag("Player"))
                {
                    canSeePlayerNow = true;
                    lastKnownPosition = player.position;
                    hasLastKnownPosition = true;
                    timeSinceLastSeen = 0f;
                }
        }

        if (!canSeePlayerNow && hasLastKnownPosition)
        {
            timeSinceLastSeen += Time.deltaTime;
            if (timeSinceLastSeen >= forgetTime)
            {
                hasLastKnownPosition = false;
                state = State.Idle;
                SetStateVisual(State.Idle);
            }
        }
    }

    // ─── Переходы состояний ──────────────────────────────────────────────

    void UpdateState()
    {
        if (state == State.Telegraph || state == State.Charging) return;

        float distance = Vector3.Distance(transform.position, player.position);
        float hpPct = (float)currentHealth / maxHealth;

        if (!canSeePlayerNow && !hasLastKnownPosition)
        {
            if (state != State.Idle)
            {
                state = State.Idle;
                SetStateVisual(State.Idle);
            }
            return;
        }

        if (hpPct <= retreatHpPercent && canSeePlayerNow)
        {
            if (state != State.Retreating)
            {
                state = State.Retreating;
                SetStateVisual(State.Retreating);
            }
            return;
        }

        if (canSeePlayerNow)
        {
            healthBar?.ShowBar(currentHealth, maxHealth);

            if (distance <= strafeRadius)
            {
                bool canAttack = Time.time >= lastAttackTime + attackCooldown;
                bool canCharge = Time.time >= lastChargeTime + chargeCooldown;
                bool willCharge = canCharge && Random.value < chargeChance;

                if ((canAttack || willCharge) && state != State.Telegraph)
                {
                    StartCoroutine(TelegraphThenAttack(willCharge));
                    state = State.Telegraph;
                    SetStateVisual(State.Telegraph);
                }
                else if (state != State.Telegraph && state != State.Strafing)
                {
                    state = State.Strafing;
                    SetStateVisual(State.Strafing);
                }
            }
            else if (state != State.Chasing)
            {
                state = State.Chasing;
                SetStateVisual(State.Chasing);
            }
        }
        else if (hasLastKnownPosition && state != State.Chasing)
        {
            state = State.Chasing;
            SetStateVisual(State.Chasing);
        }
    }

    // ─── Выполнение состояний ────────────────────────────────────────────

    void ExecuteState()
    {
        if (state != State.Strafing)
            agent.updateRotation = true;

        switch (state)
        {
            case State.Idle:
                agent.isStopped = true;
                break;

            case State.Chasing:
                agent.speed = moveSpeed;
                agent.isStopped = false;
                agent.SetDestination(hasLastKnownPosition
                    ? lastKnownPosition : player.position);
                break;

            case State.Strafing:
                DoStrafe();
                break;

            case State.Retreating:
                DoRetreat();
                break;

            case State.Telegraph:
            case State.Charging:
            case State.Attacking:
                break;
        }
    }

    // ─── Страфинг ────────────────────────────────────────────────────────

    void DoStrafe()
    {
        agent.updateRotation = false;

        strafeTimer -= Time.deltaTime;
        if (strafeTimer <= 0f)
        {
            strafeDir = -strafeDir;
            strafeTimer = Random.Range(1.5f, 3f);
        }

        Vector3 toEnemy = (transform.position - player.position).normalized;
        Vector3 strafeVec = Vector3.Cross(toEnemy, Vector3.up) * strafeDir;
        Vector3 target = player.position + toEnemy * strafeRadius + strafeVec * 0.5f;

        agent.speed = strafeSpeed;
        agent.isStopped = false;
        agent.SetDestination(target);

        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookDir),
                10f * Time.deltaTime);
    }

    // ─── Отступление ─────────────────────────────────────────────────────

    void DoRetreat()
    {
        Vector3 away = (transform.position - player.position).normalized;
        agent.speed = moveSpeed * 0.8f;
        agent.isStopped = false;
        agent.SetDestination(transform.position + away * retreatDistance);
    }

    // ─── Телеграф + выбор атаки ──────────────────────────────────────────

    IEnumerator TelegraphThenAttack(bool doCharge)
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        if (doCharge)
        {
            transform.localScale = new Vector3(
                originalScale.x * 0.85f,
                originalScale.y * 0.85f,
                originalScale.z * 1.3f);

            // ✅ Отключаем автоповорот — сами смотрим на игрока
            agent.updateRotation = false;

            Vector3 backDir = (transform.position - player.position).normalized;
            Vector3 backTarget = transform.position + backDir * 1.8f;
            agent.speed = moveSpeed * 1.5f;
            agent.isStopped = false;
            agent.SetDestination(backTarget);

            float backElapsed = 0f;
            while (backElapsed < 0.35f)
            {
                // ✅ Постоянно смотрим на игрока пока отходим
                Vector3 lookDir = player.position - transform.position;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(lookDir);

                backElapsed += Time.deltaTime;
                yield return null;
            }

            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            // Возвращаем автоповорот для заряда
            agent.updateRotation = true;

            yield return new WaitForSeconds(0.25f);
        }
        else
        {
            yield return new WaitForSeconds(0.8f); // обычный телеграф
        }

        if (doCharge)
            yield return StartCoroutine(DoChargeAttack());
        else
            DoNormalAttack();

        transform.localScale = originalScale;
        state = State.Strafing;
        SetStateVisual(State.Strafing);
    }

    // ─── Обычная атака ───────────────────────────────────────────────────

    void DoNormalAttack()
    {
        state = State.Attacking;
        lastAttackTime = Time.time;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                // ✅ Звук только если НЕ блокирует —
                // при блоке PlayerHealth сам играет свой звук
                if (!HandController.IsBlocking && attackHitSound != null)
                    AudioSource.PlayClipAtPoint(attackHitSound,
                        transform.position, attackVolume);

                ph.TakeDamage(attackDamage, transform);
            }
        }
        else
        {
            // Промах
            if (attackMissSound != null)
                AudioSource.PlayClipAtPoint(attackMissSound,
                    transform.position, attackVolume);
        }
    }

    // ─── Заряд-атака ─────────────────────────────────────────────────────

    IEnumerator DoChargeAttack()
    {
        state = State.Charging;
        lastChargeTime = Time.time;
        lastAttackTime = Time.time;
        SetStateVisual(State.Charging);

        // ✅ Звук дэша в начале
        if (chargeSound != null)
            AudioSource.PlayClipAtPoint(chargeSound, transform.position, attackVolume);

        agent.speed = chargeSpeed;
        agent.isStopped = false;
        agent.SetDestination(player.position);

        float elapsed = 0f;
        bool hit = false;

        while (elapsed < 0.8f)
        {
            elapsed += Time.deltaTime;
            float dist = Vector3.Distance(transform.position, player.position);
            if (!hit && dist <= attackRange)
            {
                hit = true;
                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    // ✅ Звук только если НЕ блокирует
                    if (!HandController.IsBlocking && attackHitSound != null)
                        AudioSource.PlayClipAtPoint(attackHitSound,
                            transform.position, attackVolume);

                    ph.TakeDamage(chargeDamage, transform);
                    CameraShake.Instance?.Shake(0.3f, 0.15f);
                }
                break;
            }
            yield return null;
        }

        // Промах дэшем
        if (!hit && attackMissSound != null)
            AudioSource.PlayClipAtPoint(attackMissSound,
                transform.position, attackVolume);

        agent.speed = moveSpeed;
        agent.isStopped = true;
    }

    // ─── Получение урона ─────────────────────────────────────────────────

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (player != null)
        {
            lastKnownPosition = player.position;
            hasLastKnownPosition = true;
            timeSinceLastSeen = 0f;
        }

        if (state == State.Retreating)
        {
            state = State.Chasing;
            SetStateVisual(State.Chasing);
        }

        CameraShake.Instance?.Shake(0.12f, 0.06f);
        StartCoroutine(HitFlash());
        FloatingDamageNumber.Spawn(transform.position, damage);
        healthBar?.UpdateBar(currentHealth, maxHealth);
        if (player != null) StartCoroutine(Knockback());

        if (currentHealth <= 0) Die();
    }

    IEnumerator HitFlash()
    {
        if (rend == null) yield break;
        if (telegraphFlash != null) StopCoroutine(telegraphFlash);
        rend.material.SetColor("_Color", Color.white);
        yield return new WaitForSeconds(0.1f);
        if (rend != null) rend.material.SetColor("_Color", baseColor);
        if (state == State.Telegraph)
            telegraphFlash = StartCoroutine(FlashColor(Color.yellow, 0.15f));
    }

    IEnumerator Knockback()
    {
        if (agent == null) yield break;
        isKnockedBack = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        Vector3 dir = (transform.position - player.position).normalized;
        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            float t = 1f - elapsed / knockbackDuration;
            agent.Move(dir * knockbackForce * t * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isKnockedBack = false;
        agent.isStopped = false;
    }

    void Die()
    {
        DropItem();
        Destroy(gameObject);
    }

    void DropItem()
    {
        GameObject drop = dropItemPrefab != null
            ? Instantiate(dropItemPrefab,
                transform.position + Vector3.up * 0.5f, Quaternion.identity)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        if (dropItemPrefab == null)
        {
            drop.transform.position = transform.position + Vector3.up * 0.5f;
            drop.transform.localScale = Vector3.one * 0.5f;
        }

        drop.name = dropItemName;
        drop.tag = "Item";

        Renderer dr = drop.GetComponent<Renderer>();
        if (dr != null)
        {
            if (dropMaterial != null) dr.material = dropMaterial;
            dr.material.color = dropItemType switch
            {
                "Potion" => Color.green,
                "Weapon" => Color.red,
                "Helmet" or "Chest" or "Legs" or "Boots" => Color.blue,
                "Ring" or "Amulet" => new Color(0.5f, 0f, 0.8f),
                _ => Color.yellow
            };
        }

        ItemData data = drop.AddComponent<ItemData>();
        data.itemName = dropItemName;
        data.itemType = dropItemType;
        data.value = dropItemValue;

        drop.AddComponent<Rigidbody>()
            .AddForce(Random.insideUnitSphere * 2f + Vector3.up * 2f, ForceMode.Impulse);

        if (drop.GetComponent<Collider>() == null)
            drop.AddComponent<BoxCollider>();
    }
}