using UnityEngine.AI;
using UnityEngine;

[RequireComponent(typeof(NavMeshAgent))]

public class EnemyNav : MonoBehaviour
{
    [Header("Sound")]
    public AudioClip attackSound;
    public float attackVolume = 0.5f;

    private NavMeshAgent agent;
    private LayerMask obstacleMask; // чтобы Layer считать препятствиями

    //Память
    private Vector3 lastKnownPosition; // последняя известная позиция игрока
    private bool hasLastKnownPosition = false;
    public float forgetTime = 5f; // через какое время забудет позицию
    private float timeSinceLastSeen = 0f;

    [Header("Stats")]
    public int maxHealth = 50;
    private int currentHealth;

    [Header("Attack")]
    public float attackRange = 2f;
    public int attackDamage = 15;
    public float attackCooldown = 3.5f;
    private float lastAttackTime;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float detectionRange = 5f;
    private Transform player;

    [Header("Drop")]
    public GameObject dropItemPrefab;
    public string dropItemName = "Health Potion";
    public string dropItemType = "Potion";
    public int dropItemValue = 5;

    private Renderer rend;

    void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rend = GetComponent<Renderer>();
        if (rend == null)
            rend = GetComponentInChildren<Renderer>();
        UpdateColor();

        // Мои объекты у которых Layer - Default (Можно и поменять отдельно на Wall для стен)
        obstacleMask = LayerMask.GetMask("Default"); 
        
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed; // на агента наша переменная скорости moveSpeed
            agent.stoppingDistance = attackRange - 0.5f; // чтобы подходил ближе
        }

        if (dropItemPrefab == null)
            CreateDropPrefab();

        //enemyController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (player == null || agent == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        // система обнаружения + препятствия (Layer - Default)
        bool canSeePlayer = false;
        if (distance <= detectionRange) // движение к игроку, если в зоне обнаружения
        {
            Vector3 startPoint = transform.position + Vector3.up * 0.5f; // луч из центра тела
            Vector3 direction = (player.position - startPoint).normalized;
            RaycastHit hit;

            if (Physics.Raycast(startPoint, direction, out hit, detectionRange))
            {
                // Если луч попал в игрока (или в объект с тегом "Player") - видит
                if (hit.collider.CompareTag("Player"))
                {
                    canSeePlayer = true;
                    lastKnownPosition = player.position;
                    hasLastKnownPosition = true;
                    timeSinceLastSeen = 0f;
                }
            }
        }
        if (canSeePlayer)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            timeSinceLastSeen = 0f;
        }
        else if (hasLastKnownPosition)
        {
            // не вижу, но помню, где игрок был
            timeSinceLastSeen += Time.deltaTime;
            if (timeSinceLastSeen < forgetTime)
            {
                // Иду к последней известной позиции
                agent.isStopped = false;
                agent.SetDestination(lastKnownPosition);
                // если почти пришел - то останавливаюсь и забываю, чтобы не топтаться на месте
                if (Vector3.Distance(transform.position, lastKnownPosition) < 0.5f)
                {
                    hasLastKnownPosition = false;
                }
            }
            else
            {
                // Время уже истекло и я забыл / стало неинтересно
                hasLastKnownPosition = false;
                agent.isStopped = true;
            }
        }
        else // нет цели - стоим
        {
            agent.isStopped = true;
        }

        // Атака, если в радиусе атаки и прошло достаточно времени
        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
            // + небольшая пауза перед возобновлением движения
            agent.isStopped = true;
            Invoke(nameof(ResumeMovement), 0.3f);
        }
    }
    void ResumeMovement()
    {
        if (agent != null && agent.isActiveAndEnabled && !agent.isStopped) 
        {
            agent.isStopped = false;
        }
    }

    void Attack()
    {
        lastAttackTime = Time.time;
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log($"Враг атаковал! Нанесено {attackDamage} урона.");
        
            if (attackSound != null)
            {
                AudioSource.PlayClipAtPoint(attackSound, transform.position, attackVolume);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Враг получил {damage} урона. Осталось здоровья: {currentHealth}");
        UpdateColor();

        if (currentHealth <= 0)
            Die();
    }

    void UpdateColor()
    {
        if (rend != null)
        {
            float healthPercent = (float)currentHealth / maxHealth;
            rend.material.color = Color.Lerp(Color.red, Color.green, healthPercent);
        }
    }

    void Die()
    {
        Debug.Log("Враг повержен!");
        DropItem();
        Destroy(gameObject);
    }

    void DropItem()
    {
        GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        drop.transform.position = transform.position + Vector3.up * 0.5f;
        drop.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        drop.name = dropItemName;

        ItemData data = drop.AddComponent<ItemData>();
        data.itemName = dropItemName;
        data.itemType = dropItemType;
        data.value = dropItemValue;

        Rigidbody rb = drop.AddComponent<Rigidbody>();
        rb.AddForce(Random.insideUnitSphere * 2f + Vector3.up * 2f, ForceMode.Impulse);

        Renderer dropRend = drop.GetComponent<Renderer>();
        if (dropRend != null)
        {
            if (dropItemType == "Potion")
                dropRend.material.color = Color.green;
            else if (dropItemType == "Weapon")
                dropRend.material.color = Color.red;
            else
                dropRend.material.color = Color.blue;
        }

        if (drop.GetComponent<Collider>() == null)
            drop.AddComponent<BoxCollider>();
    }

    void CreateDropPrefab()
    {
        dropItemPrefab = new GameObject("DropPrefab");
        dropItemPrefab.SetActive(false);
    }
}