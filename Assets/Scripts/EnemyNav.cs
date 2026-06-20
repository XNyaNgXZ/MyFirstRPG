using UnityEngine.AI;
using UnityEngine;
using System.Collections;

[System.Serializable]
public class DropEntry
{
    public ItemDefinition item;
    [Range(0f, 100f)]
    public float weight = 50f;
}

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyNav : MonoBehaviour
{
    enum State { Idle, Chasing, Strafing, Telegraph, Attacking, Charging, Searching, Retreating }
    private State state = State.Idle;

    [Header("Material / Drop")]
    public Material dropMaterial;

    [Header("Sound")]
    public AudioClip attackSound;
    public AudioClip chargeSound;
    public float attackVolume = 0.5f;

    [Header("Отталкивание")]
    public float knockbackForce = 3f;
    public float knockbackDuration = 0.15f;

    private NavMeshAgent agent;
    private EnemyHealthBar healthBar;

    private Vector3 lastKnownPosition;
    private bool hasLastKnownPosition = false;
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

    [Header("Отступление после атаки")]
    public float retreatDistance = 2.5f;
    public float retreatSpeed = 3f;
    public float retreatDuration = 1.2f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float detectionRange = 5f;
    private Transform player;

    [Header("Зрение")]
    public float fieldOfView = 110f;
    public float peripheralRange = 2f;

    [Header("Слух")]
    public float hearingRange = 3f;
    public float hearingRangeSprint = 6f;
    private PlayerMovement playerMovement;

    [Header("Обнаружение")]
    public float detectionDelay = 1f;
    private float detectionTimer = 0f;

    [Header("Поиск после потери игрока")]
    public float searchDuration = 3f;
    public float searchTurnSpeed = 60f;
    private float searchTimer = 0f;

    [Header("Патруль")]
    public float patrolRadius = 4f;
    public float patrolWaitMin = 2f;
    public float patrolWaitMax = 4f;
    private Vector3 homePosition;
    private float patrolTimer = 0f;
    private bool patrolWaiting = false;

    [Header("Drop")]
    public DropEntry[] possibleDrops;
    [Range(0f, 100f)]
    public float dropChance = 80f;

    private Renderer rend;
    private Color baseColor;
    private bool isKnockedBack = false;
    private Vector3 originalScale;
    private Coroutine telegraphFlash;

    private CharacterController playerCC;

    private float sightGraceTimer = 0f;
    private float sightGraceTime = 2f;

    void Start()
    {
        currentHealth = maxHealth;
        homePosition = transform.position;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        playerMovement = player?.GetComponent<PlayerMovement>();
        playerCC = player?.GetComponent<CharacterController>();
        rend = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();

        if (rend != null)
            baseColor = rend.material.HasProperty("_Color")
                ? rend.material.GetColor("_Color") : Color.white;

        originalScale = transform.localScale;
        healthBar = gameObject.AddComponent<EnemyHealthBar>();

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = strafeRadius * 0.8f;
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

    float DistanceToPlayer()
    {
        Vector3 a = transform.position;
        Vector3 b = player.position;
        a.y = 0; b.y = 0;
        return Vector3.Distance(a, b);
    }

    void SetStateVisual(State newState)
    {
        transform.localScale = originalScale;
        if (telegraphFlash != null) { StopCoroutine(telegraphFlash); telegraphFlash = null; }
        if (rend != null) rend.material.SetColor("_Color", baseColor);

        switch (newState)
        {
            case State.Telegraph:
                telegraphFlash = StartCoroutine(FlashColor(Color.yellow, 0.15f));
                break;
            case State.Charging:
                if (rend != null) rend.material.SetColor("_Color", Color.red);
                transform.localScale = new Vector3(
                    originalScale.x * 0.8f, originalScale.y * 0.8f, originalScale.z * 1.4f);
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

    void UpdateVision()
    {
        float distance = DistanceToPlayer();
        canSeePlayerNow = false;

        bool isCrouching = playerMovement != null && playerMovement.IsCrouching;
        bool isSprinting = playerMovement != null && playerMovement.isSprinting;
        float activeHearingRange = isSprinting ? hearingRangeSprint : hearingRange;

        bool canHear = false;
        if (!isCrouching && distance <= activeHearingRange)
        {
            Vector3 sp = transform.position + Vector3.up * 0.5f;
            Vector3 pp = player.position + Vector3.up * 0.5f;
            Vector3 dir = (pp - sp).normalized;
            if (Physics.Raycast(sp, dir, out RaycastHit hearHit, activeHearingRange))
                canHear = hearHit.collider.CompareTag("Player");
        }

        bool canSee = false;
        if (distance <= detectionRange)
        {
            bool inPeripheral = distance <= peripheralRange;
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToPlayer);
            bool inFOV = angle <= fieldOfView * 0.5f;

            if (inPeripheral || inFOV)
            {
                Vector3 sp = transform.position + Vector3.up * 0.5f;
                Vector3 ccCenter = playerCC != null
                    ? player.TransformPoint(playerCC.center)
                    : player.position + Vector3.up * 0.9f;

                Vector3 dir = (ccCenter - sp).normalized;
                if (Physics.Raycast(sp, dir, out RaycastHit hit, detectionRange))
                    if (hit.collider.CompareTag("Player")) canSee = true;

                if (!canSee)
                {
                    Vector3 lowTarget = player.position + Vector3.up * 0.1f;
                    Vector3 dir2 = (lowTarget - sp).normalized;
                    if (Physics.Raycast(sp, dir2, out RaycastHit hit2, detectionRange))
                        if (hit2.collider.CompareTag("Player")) canSee = true;
                }
            }
        }

        bool detected = canHear || canSee;

        if (detected)
        {
            sightGraceTimer = sightGraceTime;
            detectionTimer += Time.deltaTime;
            lastKnownPosition = player.position;
            hasLastKnownPosition = true;
            timeSinceLastSeen = 0f;

            if (detectionTimer >= detectionDelay)
            {
                canSeePlayerNow = true;
                if (state == State.Idle || state == State.Searching)
                {
                    state = State.Chasing;
                    SetStateVisual(State.Chasing);
                }
            }
        }
        else
        {
            if (DistanceToPlayer() <= strafeRadius * 1.5f && hasLastKnownPosition)
            {
                canSeePlayerNow = true;
                lastKnownPosition = player.position;
                sightGraceTimer = sightGraceTime;
            }
            else if (sightGraceTimer > 0f)
            {
                sightGraceTimer -= Time.deltaTime;
                canSeePlayerNow = true;
                lastKnownPosition = player.position;
            }
            else
            {
                detectionTimer = Mathf.Max(0f, detectionTimer - Time.deltaTime * 2f);
            }
        }

        if (!canSeePlayerNow && hasLastKnownPosition)
        {
            timeSinceLastSeen += Time.deltaTime;

            if (timeSinceLastSeen < 5f)
            {
                lastKnownPosition = player.position;
                if (state != State.Chasing &&
                    state != State.Telegraph &&
                    state != State.Charging &&
                    state != State.Retreating)
                {
                    state = State.Chasing;
                    SetStateVisual(State.Chasing);
                }
            }
            else if (state != State.Searching)
            {
                state = State.Searching;
                searchTimer = 0f;
                agent.isStopped = true;
                SetStateVisual(State.Idle);
            }

            if (state == State.Searching)
            {
                searchTimer += Time.deltaTime;
                if (searchTimer >= searchDuration)
                {
                    hasLastKnownPosition = false;
                    detectionTimer = 0f;
                    state = State.Idle;
                    SetStateVisual(State.Idle);
                }
            }
        }
    }

    void UpdateState()
    {
        if (state == State.Telegraph ||
            state == State.Charging ||
            state == State.Searching ||
            state == State.Retreating) return;

        float distance = DistanceToPlayer();

        if (!canSeePlayerNow && !hasLastKnownPosition)
        {
            if (state != State.Idle) { state = State.Idle; SetStateVisual(State.Idle); }
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

    void ExecuteState()
    {
        switch (state)
        {
            case State.Idle:
                DoPatrol();
                break;
            case State.Chasing:
                agent.updateRotation = true;
                agent.speed = moveSpeed;
                agent.isStopped = false;
                agent.stoppingDistance = strafeRadius * 0.85f;
                agent.SetDestination(canSeePlayerNow ? player.position : lastKnownPosition);
                break;
            case State.Strafing:
                DoStrafe();
                break;
            case State.Searching:
                float distToLast = Vector3.Distance(transform.position, lastKnownPosition);
                if (distToLast > 1.2f)
                {
                    agent.isStopped = false;
                    agent.speed = moveSpeed * 0.8f;
                    agent.SetDestination(lastKnownPosition);
                }
                else
                {
                    agent.isStopped = true;
                    Vector3 lookDir = player.position - transform.position;
                    lookDir.y = 0;
                    if (lookDir != Vector3.zero)
                        transform.rotation = Quaternion.RotateTowards(
                            transform.rotation,
                            Quaternion.LookRotation(lookDir),
                            searchTurnSpeed * Time.deltaTime);
                }
                break;
            case State.Telegraph:
            case State.Charging:
            case State.Attacking:
            case State.Retreating:
                break;
        }
    }

    void DoPatrol()
    {
        patrolTimer -= Time.deltaTime;
        if (patrolTimer > 0f) return;

        if (patrolWaiting)
        {
            patrolWaiting = false;
            Vector3 randDir = Random.insideUnitSphere * patrolRadius;
            randDir.y = 0f;
            Vector3 target = homePosition + randDir;

            if (NavMesh.SamplePosition(target, out NavMeshHit hit, patrolRadius, -1))
            {
                agent.isStopped = false;
                agent.speed = moveSpeed * 0.5f;
                agent.SetDestination(hit.position);
            }
            patrolTimer = Random.Range(3f, 6f);
        }
        else
        {
            agent.isStopped = true;
            patrolWaiting = true;
            patrolTimer = Random.Range(patrolWaitMin, patrolWaitMax);
            StartCoroutine(PatrolLook());
        }
    }

    IEnumerator PatrolLook()
    {
        float dir = Random.value > 0.5f ? 1f : -1f;
        float elapsed = 0f;
        float time = Random.Range(1f, 2.5f);
        while (elapsed < time)
        {
            transform.Rotate(0f, dir * searchTurnSpeed * 0.5f * Time.deltaTime, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void DoStrafe()
    {
        agent.updateRotation = false;
        agent.stoppingDistance = 0f;
        strafeTimer -= Time.deltaTime;
        if (strafeTimer <= 0f) { strafeDir = -strafeDir; strafeTimer = Random.Range(1.5f, 3f); }

        Vector3 toEnemy = (transform.position - player.position).normalized;
        Vector3 strafeVec = Vector3.Cross(toEnemy, Vector3.up) * strafeDir;
        Vector3 target2 = player.position + toEnemy * strafeRadius + strafeVec * 0.5f;

        agent.speed = strafeSpeed;
        agent.isStopped = false;
        agent.SetDestination(target2);

        Vector3 lookDir2 = player.position - transform.position;
        lookDir2.y = 0;
        if (lookDir2 != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(lookDir2), 10f * Time.deltaTime);
    }

    IEnumerator TelegraphThenAttack(bool doCharge)
    {
        if (!doCharge) lastAttackTime = Time.time;
        else lastChargeTime = Time.time;

        agent.isStopped = true; agent.velocity = Vector3.zero;
        Vector3 dir = player.position - transform.position; dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);

        if (doCharge)
            transform.localScale = new Vector3(
                originalScale.x * 0.85f, originalScale.y * 0.85f, originalScale.z * 1.3f);

        yield return new WaitForSeconds(0.8f);

        if (doCharge) yield return StartCoroutine(DoChargeAttack());
        else DoNormalAttack();

        transform.localScale = originalScale;
        yield return StartCoroutine(DoRetreat());
    }

    void DoNormalAttack()
    {
        state = State.Attacking;
        if (DistanceToPlayer() <= attackRange + 0.5f)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(attackDamage, transform);
                if (attackSound != null)
                    AudioSource.PlayClipAtPoint(attackSound, transform.position, attackVolume);
            }
        }
    }

    IEnumerator DoChargeAttack()
    {
        state = State.Charging;
        SetStateVisual(State.Charging);
        if (chargeSound != null)
            AudioSource.PlayClipAtPoint(chargeSound, transform.position, attackVolume);

        agent.speed = chargeSpeed;
        agent.isStopped = false;
        agent.stoppingDistance = 0f;
        agent.SetDestination(player.position);

        float elapsed = 0f; bool hit = false;
        while (elapsed < 0.8f)
        {
            elapsed += Time.deltaTime;
            if (!hit && DistanceToPlayer() <= attackRange)
            {
                hit = true;
                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(chargeDamage, transform);
                    CameraShake.Instance?.Shake(0.3f, 0.15f);
                }
                break;
            }
            yield return null;
        }
        agent.speed = moveSpeed;
        agent.isStopped = true;
    }

    IEnumerator DoRetreat()
    {
        state = State.Retreating;
        SetStateVisual(State.Idle);

        agent.isStopped = false;
        agent.speed = retreatSpeed;
        agent.stoppingDistance = 0f;
        agent.updateRotation = false;

        float elapsed = 0f;
        while (elapsed < retreatDuration)
        {
            elapsed += Time.deltaTime;

            if (player != null)
            {
                Vector3 retreatDir = (transform.position - player.position).normalized;
                retreatDir.y = 0f;
                Vector3 retreatTarget = transform.position + retreatDir * retreatDistance;

                if (NavMesh.SamplePosition(retreatTarget, out NavMeshHit hit, retreatDistance, -1))
                    agent.SetDestination(hit.position);

                Vector3 lookDir = player.position - transform.position;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(lookDir), 15f * Time.deltaTime);
            }

            yield return null;
        }

        agent.speed = moveSpeed;
        agent.stoppingDistance = strafeRadius * 0.85f;
        agent.updateRotation = true;
        state = State.Strafing;
        SetStateVisual(State.Strafing);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (player != null)
        {
            lastKnownPosition = player.position;
            hasLastKnownPosition = true;
            timeSinceLastSeen = 0f;
            detectionTimer = detectionDelay;
            sightGraceTimer = sightGraceTime;
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
        isKnockedBack = true; agent.isStopped = true; agent.velocity = Vector3.zero;
        Vector3 dir = (transform.position - player.position).normalized;
        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            float t = 1f - elapsed / knockbackDuration;
            agent.Move(dir * knockbackForce * t * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        isKnockedBack = false; agent.isStopped = false;
    }

    void Die() { DropItem(); Destroy(gameObject); }

    void DropItem()
    {
        if (possibleDrops == null || possibleDrops.Length == 0) return;
        if (Random.Range(0f, 100f) > dropChance) return;

        float totalWeight = 0f;
        foreach (var d in possibleDrops)
            if (d.item != null) totalWeight += d.weight;
        if (totalWeight <= 0f) return;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        DropEntry chosen = null;

        foreach (var d in possibleDrops)
        {
            if (d.item == null) continue;
            cumulative += d.weight;
            if (roll <= cumulative) { chosen = d; break; }
        }
        if (chosen == null || chosen.item == null) return;

        Vector3 pos = transform.position + Vector3.up * 0.5f;
        var drop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        drop.transform.position = pos;
        drop.transform.localScale = chosen.item.itemScale;
        drop.name = chosen.item.itemName;
        drop.tag = "Item";

        Color finalColor = chosen.item.itemColor != Color.white && chosen.item.itemColor != default
            ? chosen.item.itemColor
            : chosen.item.itemType switch
            {
                "Potion" => new Color(0.2f, 0.8f, 0.3f),
                "Weapon" or "Shield" => new Color(0.82f, 0.22f, 0.22f),
                "Helmet" or "Chest" or "Legs" or "Boots" => new Color(0.22f, 0.48f, 0.85f),
                "Ring" or "Amulet" => new Color(0.45f, 0f, 0.7f),
                _ => new Color(0.6f, 0.6f, 0.6f)
            };

        var r = drop.GetComponent<Renderer>();
        if (r != null)
        {
            if (dropMaterial != null) r.material = new Material(dropMaterial);
            if (chosen.item.worldTexture != null)
                r.material.mainTexture = chosen.item.worldTexture;
            else
                r.material.color = finalColor;
        }

        var data = drop.AddComponent<ItemData>();
        data.itemName = chosen.item.itemName;
        data.itemType = chosen.item.itemType;
        data.value = chosen.item.itemValue;
        data.itemColor = finalColor;
        data.itemScale = chosen.item.itemScale;

        var col = drop.GetComponent<Collider>();
        if (col == null) col = drop.AddComponent<BoxCollider>();

        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            var pc = playerObj.GetComponent<Collider>();
            if (pc != null) Physics.IgnoreCollision(col, pc);
        }
        foreach (var enemy in FindObjectsByType<EnemyNav>())
        {
            var ec = enemy.GetComponent<Collider>();
            if (ec != null) Physics.IgnoreCollision(col, ec);
        }

        // ✅ Случайное направление броска 360° — передаём в ItemFloat
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 throwDir = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)).normalized;

        drop.AddComponent<BlobShadow>();

        // ✅ ItemFloat добавляем первым, потом хелпер задаёт направление через Awake
        var itemFloat = drop.AddComponent<ItemFloat>();
        itemFloat.applyThrow = true;
        itemFloat.throwForce = 3.5f;
        itemFloat.customThrowDir = throwDir; // сразу задаём направление напрямую
    }

    public enum AlertLevel { None, Search, Chase }

    public AlertLevel GetCurrentState()
    {
        switch (state)
        {
            case State.Chasing:
            case State.Strafing:
            case State.Telegraph:
            case State.Attacking:
            case State.Charging:
            case State.Retreating:
                return AlertLevel.Chase;
            case State.Searching:
                return AlertLevel.Search;
            default:
                return AlertLevel.None;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 leftDir = Quaternion.Euler(0, -fieldOfView * 0.5f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, fieldOfView * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftDir * detectionRange);
        Gizmos.DrawRay(transform.position, rightDir * detectionRange);
        Gizmos.DrawWireSphere(transform.position, peripheralRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}