using UnityEngine;

public class WeaponBob : MonoBehaviour
{
    [Header("Настройки покачивания")]
    public float walkBobSpeed = 8f;
    public float walkBobAmountX = 0.05f;
    public float walkBobAmountY = 0.07f;
    public float sprintBobSpeed = 12f;
    public float sprintBobAmountX = 0.08f;
    public float sprintBobAmountY = 0.12f;
    public float crouchBobSpeed = 5f;
    public float crouchBobAmountX = 0.03f;
    public float crouchBobAmountY = 0.04f;

    [Header("Плавность")]
    public float returnSpeed = 8f;

    private Vector3 initialPosition;
    private float timer = 0f;
    private CharacterController controller;
    private PlayerMovement playerMovement;

    void Start()
    {
        initialPosition = transform.localPosition;
        controller = GetComponentInParent<CharacterController>();
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    void Update()
    {
        if (controller == null) return;

        // ✅ velocity — работает всегда, независимо от меню/инвентаря
        Vector3 hVel = controller.velocity; hVel.y = 0;
        bool isMoving = hVel.magnitude > 0.1f;
        bool isGrounded = controller.isGrounded;

        if (!isGrounded || !isMoving)
        {
            timer = 0f;
            transform.localPosition = Vector3.Lerp(
                transform.localPosition, initialPosition, returnSpeed * Time.deltaTime);
            return;
        }

        float speed = walkBobSpeed, amountX = walkBobAmountX, amountY = walkBobAmountY;
        if (playerMovement != null)
        {
            if (playerMovement.isSprinting)
            { speed = sprintBobSpeed; amountX = sprintBobAmountX; amountY = sprintBobAmountY; }
            else if (playerMovement.IsCrouching)
            { speed = crouchBobSpeed; amountX = crouchBobAmountX; amountY = crouchBobAmountY; }
        }

        timer += Time.deltaTime * speed;
        Vector3 target = initialPosition + new Vector3(
            Mathf.Sin(timer) * amountX,
            Mathf.Abs(Mathf.Sin(timer)) * amountY, 0f);
        transform.localPosition = Vector3.Lerp(transform.localPosition, target, returnSpeed * Time.deltaTime);
    }
}