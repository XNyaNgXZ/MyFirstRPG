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

    private float startY;
    private float timeOffset;
    public bool isSettled = false;
    private bool isThrown = false;
    private Vector3 throwVelocity;
    private float throwTimer = 0f;
    private float gravityVel = 0f;

    // ✅ Окружение = нет игровых скриптов
    static bool IsEnv(Collider col)
    {
        if (col == null) return false;
        if (col.GetComponent<ItemFloat>() != null) return false;
        if (col.GetComponent<ItemData>() != null) return false;
        if (col.GetComponent<SpellPickup>() != null) return false;
        if (col.GetComponent<EnemyNav>() != null) return false;
        if (col.GetComponent<PlayerMovement>() != null) return false;
        if (col.GetComponent<Arrow>() != null) return false;
        return true;
    }

    // ✅ Для FindGroundHeight — исключаем себя но не по IsEnv
    bool IsGroundForMe(Collider col)
    {
        if (col == null) return false;
        if (col == GetComponent<Collider>()) return false; // себя исключаем
        // Чужие предметы не считаем полом
        if (col.GetComponent<ItemFloat>() != null) return false;
        if (col.GetComponent<EnemyNav>() != null) return false;
        if (col.GetComponent<PlayerMovement>() != null) return false;
        if (col.GetComponent<Arrow>() != null) return false;
        // ItemData и SpellPickup на ДРУГИХ объектах — не пол
        // но свой коллайдер уже исключён выше
        if (col.GetComponent<ItemData>() != null) return false;
        if (col.GetComponent<SpellPickup>() != null) return false;
        return true;
    }

    void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);
        transform.rotation = Quaternion.Euler(tiltAngle, transform.eulerAngles.y, 0f);

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

            if (applyThrow) myCol.enabled = false;
        }
    }

    void Start()
    {
        timeOffset = Random.Range(0f, Mathf.PI * 2f);

        if (applyThrow)
        {
            isThrown = true;
            Vector3 dir = customThrowDir != Vector3.zero
                ? customThrowDir
                : (Camera.main != null ? Camera.main.transform.forward : Vector3.forward);
            dir.y = 0.15f;
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
        float groundY = float.MinValue;
        foreach (var hit in Physics.RaycastAll(ray, 50f))
        {
            if (!IsGroundForMe(hit.collider)) continue;
            if (hit.point.y > groundY) groundY = hit.point.y;
        }
        // ✅ halfScale из bounds — точный размер независимо от scale
        Collider selfCol = GetComponent<Collider>();
        float halfScale = selfCol != null ? selfCol.bounds.extents.y : transform.localScale.y * 0.5f;
        startY = groundY > float.MinValue
            ? groundY + floatHeight + halfScale
            : transform.position.y;
        transform.position = new Vector3(transform.position.x, startY, transform.position.z);
        isSettled = true;
    }

    void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
        Vector3 e = transform.eulerAngles;
        e.x = tiltAngle; e.z = 0f;
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
            RaycastHit bestHit = default;
            bool foundGround = false;
            foreach (var h in Physics.RaycastAll(new Ray(pos + Vector3.up * 0.5f, Vector3.down), halfScale + 0.4f))
            {
                if (h.collider == GetComponent<Collider>()) continue;
                if (!IsEnv(h.collider)) continue;
                if (!foundGround || h.point.y > bestHit.point.y) { bestHit = h; foundGround = true; }
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
                    // Проверяем что под нами реальный пол
                    bool hasGround = false;
                    foreach (var gh in Physics.RaycastAll(new Ray(pos + Vector3.up * 0.5f, Vector3.down), 50f))
                        if (IsEnv(gh.collider)) { hasGround = true; break; }

                    if (!hasGround)
                    {
                        throwTimer = bounceTime * 0.5f;
                        gravityVel = -2f;
                    }
                    else
                    {
                        isThrown = false;
                        isSettled = true;
                        // ✅ bounds.extents.y — реальная высота с учётом scale
                        Collider selfCol2 = GetComponent<Collider>();
                        float realHalf = selfCol2 != null ? selfCol2.bounds.extents.y : halfScale;
                        startY = bestHit.point.y + floatHeight + realHalf + 0.08f;
                        pos.y = startY;
                        Collider col = GetComponent<Collider>();
                        if (col != null) col.enabled = true;
                        if (GetComponent<BlobShadow>() == null)
                            gameObject.AddComponent<BlobShadow>();
                        pos = FindFreePosition(pos);
                        startY = pos.y;
                    }
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
            if (other == this || !other.isSettled) continue;
            float dist = Vector2.Distance(new Vector2(pos.x, pos.z),
                new Vector2(other.transform.position.x, other.transform.position.z));
            if (dist < minDist) return false;
        }
        return true;
    }

    Vector3 FindFreePosition(Vector3 pos)
    {
        float radius = Mathf.Max(transform.localScale.x, transform.localScale.z) * 0.5f;
        float minDist = Mathf.Max(radius * 3f, 0.3f);
        if (IsPositionFree(pos, minDist)) return pos;

        float searchRadius = radius * 4f;
        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f * Mathf.Deg2Rad;
            Vector3 candidate = pos + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * searchRadius;
            if (!IsPositionFree(candidate, minDist)) continue;

            Vector3 from = pos + Vector3.up * 0.3f;
            Vector3 to = candidate + Vector3.up * 0.3f;
            if (Physics.Raycast(from, (to - from).normalized, Vector3.Distance(from, to))) continue;

            if (Physics.Raycast(new Ray(candidate + Vector3.up * 5f, Vector3.down), out RaycastHit hit, 50f))
                if (IsEnv(hit.collider))
                {
                    candidate.y = hit.point.y + floatHeight + GetHalfScale();
                    return candidate;
                }
        }

        if (Physics.Raycast(new Ray(pos + Vector3.up * 5f, Vector3.down), out RaycastHit fh, 50f))
            if (IsEnv(fh.collider))
                pos.y = fh.point.y + floatHeight + GetHalfScale();
        return pos;
    }

    public static ItemFloat AddToDropped(GameObject go)
    {
        Collider myCol = go.GetComponent<Collider>();
        if (myCol != null)
        {
            foreach (var other in Object.FindObjectsByType<ItemFloat>(FindObjectsInactive.Exclude))
            {
                Collider oc = other.GetComponent<Collider>();
                if (oc != null) Physics.IgnoreCollision(myCol, oc);
            }
            foreach (var sp in Object.FindObjectsByType<SpellPickup>(FindObjectsInactive.Exclude))
            {
                Collider sc = sp.GetComponent<Collider>();
                if (sc != null) Physics.IgnoreCollision(myCol, sc);
            }
        }
        var f = go.AddComponent<ItemFloat>();
        f.applyThrow = true;
        return f;
    }
}