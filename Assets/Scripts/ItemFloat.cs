using UnityEngine;

public class ItemFloat : MonoBehaviour
{
    [Header("Вращение")]
    public float rotateSpeed = 90f;
    public float bobHeight = 0.08f;
    public float bobSpeed = 2f;

    [Header("Наклон")]
    public float tiltAngle = 25f;

    [Header("Бросок при дропе")]
    public bool applyThrow = false;
    public float throwForce = 1.8f;
    public float bounceTime = 1.4f;
    public Vector3 customThrowDir = Vector3.zero;

    [Header("Высота парения")]
    public float floatHeight = 0.1f;
    public LayerMask groundLayer = ~0;

    private float startY;
    private float timeOffset;
    public bool isSettled = false;
    private bool isThrown = false;
    private Vector3 throwVelocity;
    private float throwTimer = 0f;
    private float gravityVel = 0f;

    void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);
        transform.rotation = Quaternion.Euler(tiltAngle, transform.eulerAngles.y, 0f);
    }

    void Start()
    {
        timeOffset = Random.Range(0f, Mathf.PI * 2f);

        Collider myCol = GetComponent<Collider>();
        if (myCol != null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                foreach (Collider c in player.GetComponentsInChildren<Collider>(true))
                    Physics.IgnoreCollision(myCol, c);

            foreach (var enemy in Object.FindObjectsByType<EnemyNav>(FindObjectsInactive.Exclude))
            {
                Collider ec = enemy.GetComponent<Collider>();
                if (ec != null) Physics.IgnoreCollision(myCol, ec);
            }

            foreach (var other in Object.FindObjectsByType<ItemFloat>(FindObjectsInactive.Exclude))
            {
                if (other == this) continue;
                Collider otherCol = other.GetComponent<Collider>();
                if (otherCol != null) Physics.IgnoreCollision(myCol, otherCol);
            }
        }

        if (applyThrow)
        {
            isThrown = true;
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Vector3 dir;
            if (customThrowDir != Vector3.zero)
            {
                dir = customThrowDir;
                dir.y = 0.15f;
            }
            else
            {
                Camera cam = Camera.main;
                dir = cam != null ? cam.transform.forward : Vector3.forward;
                dir.y = 0.15f;
            }
            throwVelocity = dir.normalized * throwForce;
            gravityVel = 1.5f;
        }
        else
        {
            FindGroundHeight();
        }
    }

    float GetHalfScale() => transform.localScale.y * 0.5f;

    void FindGroundHeight()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 5f, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(ray, 50f);
        float groundY = float.MinValue;

        foreach (var hit in hits)
        {
            if (hit.collider.GetComponent<ItemFloat>() != null) continue;
            if (hit.collider.GetComponent<ItemData>() != null) continue;
            if (hit.collider.GetComponent<SpellPickup>() != null) continue;
            if (hit.collider == GetComponent<Collider>()) continue;
            if (hit.point.y > groundY) groundY = hit.point.y;
        }

        float halfScale = GetHalfScale();
        startY = groundY > float.MinValue
            ? groundY + floatHeight + halfScale
            : transform.position.y;

        transform.position = new Vector3(transform.position.x, startY, transform.position.z);
        isSettled = true;
    }

    void Update()
    {
        // Вращение с фиксированным наклоном
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
        Vector3 e = transform.eulerAngles;
        e.x = tiltAngle;
        e.z = 0f;
        transform.eulerAngles = e;

        if (isThrown)
        {
            throwTimer += Time.deltaTime;
            gravityVel -= 9f * Time.deltaTime;

            Vector3 pos = transform.position;
            pos += throwVelocity * Time.deltaTime;
            pos.y += gravityVel * Time.deltaTime;
            throwVelocity = Vector3.Lerp(throwVelocity, Vector3.zero, Time.deltaTime * 3f);

            float halfScale = GetHalfScale();
            Ray ray = new Ray(pos + Vector3.up * 0.5f, Vector3.down);

            // ✅ Фильтруем — только пол/стены, не другие предметы
            RaycastHit[] bounceHits = Physics.RaycastAll(ray, halfScale + 0.4f);
            RaycastHit bestHit = default;
            bool foundGround = false;
            foreach (var h in bounceHits)
            {
                if (h.collider.GetComponent<ItemFloat>() != null) continue;
                if (h.collider.GetComponent<ItemData>() != null) continue;
                if (h.collider.GetComponent<SpellPickup>() != null) continue;
                if (h.collider == GetComponent<Collider>()) continue;
                if (!foundGround || h.point.y > bestHit.point.y)
                { bestHit = h; foundGround = true; }
            }

            if (foundGround)
            {
                if (throwTimer > 0.15f && gravityVel < -0.3f)
                {
                    gravityVel = Mathf.Abs(gravityVel) * 0.4f;
                    throwVelocity *= 0.4f;
                    pos.y = bestHit.point.y + halfScale + 0.02f;
                }

                if (throwTimer >= bounceTime)
                {
                    isThrown = false;
                    isSettled = true;
                    startY = bestHit.point.y + floatHeight + halfScale;
                    pos.y = startY;
                    Collider col = GetComponent<Collider>();
                    if (col != null) col.enabled = true;
                    if (GetComponent<BlobShadow>() == null)
                        gameObject.AddComponent<BlobShadow>();
                    // ✅ Найти свободное место если пересекаемся с другим предметом
                    pos = FindFreePosition(pos);
                    startY = pos.y;
                }
            }

            transform.position = pos;
        }
        else if (isSettled)
        {
            float newY = startY + Mathf.Sin(Time.time * bobSpeed + timeOffset) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    bool IsPositionFree(Vector3 pos, float minDist)
    {
        foreach (var other in Object.FindObjectsByType<ItemFloat>(FindObjectsInactive.Exclude))
        {
            if (other == this) continue;
            if (!other.isSettled) continue;
            float dist = Vector2.Distance(
                new Vector2(pos.x, pos.z),
                new Vector2(other.transform.position.x, other.transform.position.z));
            if (dist < minDist) return false;
        }
        return true;
    }

    Vector3 FindFreePosition(Vector3 pos)
    {
        float radius = Mathf.Max(transform.localScale.x, transform.localScale.z) * 0.5f;
        float minDist = radius * 3f;

        // ✅ Текущая позиция свободна — остаёмся
        if (IsPositionFree(pos, minDist)) return pos;

        float searchRadius = radius * 4f;
        for (int i = 0; i < 12; i++)
        {
            float angle = i * (360f / 12) * Mathf.Deg2Rad;
            Vector3 candidate = pos + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * searchRadius;

            if (!IsPositionFree(candidate, minDist)) continue;

            // ✅ Проверяем нет ли стены между исходной позицией и кандидатом
            Vector3 checkFrom = pos + Vector3.up * 0.3f;
            Vector3 checkTo = candidate + Vector3.up * 0.3f;
            Vector3 checkDir = (checkTo - checkFrom).normalized;
            float checkDist = Vector3.Distance(checkFrom, checkTo);
            if (Physics.Raycast(checkFrom, checkDir, checkDist)) continue;

            Ray ray = new Ray(candidate + Vector3.up * 5f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 50f))
            {
                if (hit.collider.GetComponent<ItemFloat>() == null &&
                    hit.collider.GetComponent<ItemData>() == null &&
                    hit.collider.GetComponent<SpellPickup>() == null)
                {
                    candidate.y = hit.point.y + floatHeight + GetHalfScale();
                    return candidate;
                }
            }
        }
        // ✅ Не нашли свободное место — принудительно опускаем на пол под текущей позицией
        Ray fallbackRay = new Ray(pos + Vector3.up * 5f, Vector3.down);
        if (Physics.Raycast(fallbackRay, out RaycastHit fallbackHit, 50f))
        {
            if (fallbackHit.collider.GetComponent<ItemFloat>() == null &&
                fallbackHit.collider.GetComponent<ItemData>() == null &&
                fallbackHit.collider.GetComponent<SpellPickup>() == null)
            {
                pos.y = fallbackHit.point.y + floatHeight + GetHalfScale();
            }
        }
        return pos;
    }

    public static ItemFloat AddToDropped(GameObject go)
    {
        Collider myCol = go.GetComponent<Collider>();
        if (myCol != null)
        {
            foreach (var other in Object.FindObjectsByType<ItemFloat>(FindObjectsInactive.Exclude))
            {
                Collider otherCol = other.GetComponent<Collider>();
                if (otherCol != null) Physics.IgnoreCollision(myCol, otherCol);
            }
            foreach (var sp in Object.FindObjectsByType<SpellPickup>(FindObjectsInactive.Exclude))
            {
                Collider spCol = sp.GetComponent<Collider>();
                if (spCol != null) Physics.IgnoreCollision(myCol, spCol);
            }
        }

        var f = go.AddComponent<ItemFloat>();
        f.applyThrow = true;
        return f;
    }
}