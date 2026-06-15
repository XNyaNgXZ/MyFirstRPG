using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("Инерция от мыши")]
    public float swayAmount = 0.02f;
    public float maxSwayAmount = 0.06f;
    public float smoothAmount = 6f;

    [Header("Инерция от движения WASD")]
    public float moveSwayAmount = 0.04f;
    public float maxMoveSwayAmount = 0.08f;
    public float moveSmoothAmount = 5f;

    private Vector3 initialPosition;
    private PlayerMovement playerMovement;

    void Start()
    {
        initialPosition = transform.localPosition;
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    void Update()
    {
        if (InventoryUICode.IsOpen || EquipmentUI.IsOpen)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition, initialPosition, Time.deltaTime * smoothAmount);
            return;
        }

        float mouseX = -Input.GetAxis("Mouse X") * swayAmount;
        float mouseY = -Input.GetAxis("Mouse Y") * swayAmount;
        mouseX = Mathf.Clamp(mouseX, -maxSwayAmount, maxSwayAmount);
        mouseY = Mathf.Clamp(mouseY, -maxSwayAmount, maxSwayAmount);

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 moveDir = new Vector2(h, v);
        if (moveDir.magnitude > 1f) moveDir.Normalize();

        float moveSwayX = -moveDir.x * moveSwayAmount;
        float moveSwayZ = moveDir.y * moveSwayAmount;
        float moveSwayY = -Mathf.Abs(moveDir.magnitude) * moveSwayAmount * 0.3f;
        moveSwayX = Mathf.Clamp(moveSwayX, -maxMoveSwayAmount, maxMoveSwayAmount);
        moveSwayZ = Mathf.Clamp(moveSwayZ, -maxMoveSwayAmount, maxMoveSwayAmount);

        Vector3 mouseSway = new Vector3(mouseX, mouseY, 0f);
        Vector3 moveSway = new Vector3(moveSwayX, moveSwayY, moveSwayZ);
        Vector3 targetPosition = initialPosition + mouseSway + moveSway;

        transform.localPosition = Vector3.Lerp(
            transform.localPosition, targetPosition, Time.deltaTime * smoothAmount);
    }
}