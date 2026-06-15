using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("Инерция от мыши")]
    public float swayAmount = 0.02f;
    public float maxSwayAmount = 0.06f;
    public float smoothAmount = 6f;

    [Header("Инерция от движения WASD")]
    public float moveSwayAmount = 0.04f;    // сила смещения от движения
    public float maxMoveSwayAmount = 0.08f; // максимальное смещение
    public float moveSmoothAmount = 5f;     // плавность

    private Vector3 initialPosition;
    private PlayerMovement playerMovement;

    void Start()
    {
        initialPosition = transform.localPosition;
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    void Update()
    {
        // ─── Инерция от мыши ─────────────────────────────────────────
        float mouseX = -Input.GetAxis("Mouse X") * swayAmount;
        float mouseY = -Input.GetAxis("Mouse Y") * swayAmount;
        mouseX = Mathf.Clamp(mouseX, -maxSwayAmount, maxSwayAmount);
        mouseY = Mathf.Clamp(mouseY, -maxSwayAmount, maxSwayAmount);

        // ─── Смещение от WASD ─────────────────────────────────────────
        float h = Input.GetAxisRaw("Horizontal"); // A/D → -1/1
        float v = Input.GetAxisRaw("Vertical");   // S/W → -1/1

        // Нормализуем диагональ
        Vector2 moveDir = new Vector2(h, v);
        if (moveDir.magnitude > 1f) moveDir.Normalize();

        // ✅ Рука смещается в сторону движения
        // Влево (A) → рука уходит влево по X
        // Вперёд (W) → рука уходит вперёд по Z (вниз по Y немного)
        // Назад (S) → рука уходит назад
        float moveSwayX = -moveDir.x * moveSwayAmount;
        float moveSwayZ = moveDir.y * moveSwayAmount;
        float moveSwayY = -Mathf.Abs(moveDir.magnitude) * moveSwayAmount * 0.3f;

        moveSwayX = Mathf.Clamp(moveSwayX, -maxMoveSwayAmount, maxMoveSwayAmount);
        moveSwayZ = Mathf.Clamp(moveSwayZ, -maxMoveSwayAmount, maxMoveSwayAmount);

        // ─── Итоговая позиция ────────────────────────────────────────
        Vector3 mouseSway = new Vector3(mouseX, mouseY, 0f);
        Vector3 moveSway = new Vector3(moveSwayX, moveSwayY, moveSwayZ);

        Vector3 targetPosition = initialPosition + mouseSway + moveSway;

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            Time.deltaTime * smoothAmount);
    }
}