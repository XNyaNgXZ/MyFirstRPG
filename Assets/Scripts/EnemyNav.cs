using UnityEngine.AI;
using UnityEngine;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyNav : MonoBehaviour
{
    [Header("Retro Material")]
    public Material retroMaterial; // PS1_Dynamic

    [Header("Sound")]
    public AudioClip attackSound;
    public float attackVolume = 0.5f;

    private NavMeshAgent agent;

    // Память
    private Vector3 lastKnownPosition;
    private bool hasLastKnownPosition = false;
    public float forgetTime = 5f;
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
    public GameObject dropItemPrefab;   // опционально: префаб предмета
    public string dropItemName = "Health Potion";
    public string dropItemType = "Potion";
    public int dropItemValue = 25;

    private Renderer rend;

    void Start()
    {
        if (retroMaterial != null && rend != null)
            rend.material = retroMaterial;

        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rend = GetComponent<Renderer>();
        if (rend == null)
            rend = GetComponentInChildren<Renderer>();
        UpdateColor();

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = attackRange - 0.5f;
        }
    }

    void Update()
    {
        if (player == null || agent == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool canSeePlayer = false;

        // Проверка видимости (простой луч из центра)
        if (distance <= detectionRange)
        {
            Vector3 startPoint = transform.position + Vector3.up * 0.5f;
            Vector3 direction = (player.position - startPoint).normalized;
            RaycastHit hit;
            if (Physics.Raycast(startPoint, direction, out hit, detectionRange))
            {
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
            timeSinceLastSeen += Time.deltaTime;
            if (timeSinceLastSeen < forgetTime)
            {
                agent.isStopped = false;
                agent.SetDestination(lastKnownPosition);
                if (Vector3.Distance(transform.position, lastKnownPosition) < 0.5f)
                    hasLastKnownPosition = false;
            }
            else
            {
                hasLastKnownPosition = false;
                agent.isStopped = true;
            }
        }
        else
        {
            agent.isStopped = true;
        }

        // Атака
        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
            agent.isStopped = true;
            Invoke(nameof(ResumeMovement), 0.3f);
        }
    }

    void ResumeMovement()
    {
        if (agent != null && agent.isActiveAndEnabled && !agent.isStopped)
            agent.isStopped = false;
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
                AudioSource.PlayClipAtPoint(attackSound, transform.position, attackVolume);
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
        GameObject drop;

        // Используем префаб, если он назначен, иначе создаём куб
        if (dropItemPrefab != null)
        {
            drop = Instantiate(dropItemPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            drop.name = dropItemName;
        }
        else
        {
            drop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            drop.transform.position = transform.position + Vector3.up * 0.5f;
            drop.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            drop.name = dropItemName;
        }

        drop.tag = "Item";
        Renderer dropRend = drop.GetComponent<Renderer>();

        // Назначение ретро-материала и цвета
        if (retroMaterial != null)
        {
            dropRend.material = retroMaterial;
            Color itemColor;
            switch (dropItemType)
            {
                case "Potion":
                    itemColor = Color.green;
                    break;
                case "Weapon":
                    itemColor = Color.red;
                    break;
                case "Helmet":
                case "Chest":
                case "Legs":
                case "Boots":
                    itemColor = Color.blue;
                    break;
                case "Ring":
                case "Amulet":
                    itemColor = new Color(75f / 255f, 0f, 130f / 255f);
                    break;
                default:
                    itemColor = Color.yellow;
                    break;
            }
            dropRend.material.color = itemColor;
        }
        else
        {
            // fallback: старый способ – цвет напрямую
            if (dropItemType == "Potion")
                dropRend.material.color = Color.green;
            else if (dropItemType == "Weapon")
                dropRend.material.color = Color.red;
            else
                dropRend.material.color = Color.blue;
        }

        // Добавляем ItemData
        ItemData data = drop.AddComponent<ItemData>();
        data.itemName = dropItemName;
        data.itemType = dropItemType;
        data.value = dropItemValue;

        // Физика
        Rigidbody rb = drop.AddComponent<Rigidbody>();
        rb.AddForce(Random.insideUnitSphere * 2f + Vector3.up * 2f, ForceMode.Impulse);

        // Коллайдер (у префаба может уже быть)
        if (drop.GetComponent<Collider>() == null)
            drop.AddComponent<BoxCollider>();
    }
}