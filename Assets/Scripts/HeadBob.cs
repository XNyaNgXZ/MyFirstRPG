using UnityEngine;

// Повесь на MainCamera
public class HeadBob : MonoBehaviour
{
    [Header("Ходьба")]
    public float walkBobSpeed = 9f;
    public float walkBobAmountX = 0.07f;
    public float walkBobAmountY = 0.12f;

    [Header("Бег")]
    public float sprintBobSpeed = 13f;
    public float sprintBobAmountX = 0.10f;
    public float sprintBobAmountY = 0.18f;

    [Header("Приседание")]
    public float crouchBobSpeed = 5f;
    public float crouchBobAmountX = 0.03f;
    public float crouchBobAmountY = 0.05f;

    [Header("Приземление")]
    public float landingDipAmount = 0.18f;  // насколько камера падает вниз
    public float landingDipSpeed = 12f;    // скорость падения
    public float landingReturnSpeed = 6f;   // скорость возврата

    [Header("Шаговый импакт")]
    public float stepImpactAmount = 0.06f;  // резкий толчок вниз при каждом шаге
    public float stepImpactSpeed = 20f;    // скорость затухания импакта

    [Header("Позиции камеры")]
    public float standCamY = 0.5f;
    public float crouchCamY = 0.2f;

    [Header("Плавность")]
    public float returnSpeed = 8f;

    private CharacterController controller;
    private PlayerMovement playerMovement;
    private float timer = 0f;
    private float lastTimerSin = 0f;  // для определения шага
    private Vector3 defaultLocalPos;

    // Приземление
    private float landingOffset = 0f;
    private bool wasGrounded = true;
    private float landingVelocity = 0f;

    // Импакт шага
    private float stepImpactOffset = 0f;

    void Start()
    {
        defaultLocalPos = transform.localPosition;
        controller = GetComponentInParent<CharacterController>();
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    void Update()
    {
        if (controller == null) return;

        bool uiOpen = InventoryUICode.IsOpen || EquipmentUI.IsOpen;
        bool isGrounded = controller.isGrounded;
        bool isMoving = Input.GetAxisRaw("Horizontal") != 0 ||
                          Input.GetAxisRaw("Vertical") != 0;
        bool crouching = playerMovement != null && playerMovement.IsCrouching;
        bool sprinting = playerMovement != null && playerMovement.isSprinting;

        // ✅ Импакт приземления
        if (!wasGrounded && isGrounded)
        {
            // ✅ Фиксируем скорость падения до приземления
            landingVelocity = -landingDipAmount;
        }
        wasGrounded = isGrounded;

        // Анимируем приземление
        if (Mathf.Abs(landingOffset) > 0.001f || Mathf.Abs(landingVelocity) > 0.001f)
        {
            landingOffset += landingVelocity * Time.deltaTime * landingDipSpeed;
            landingVelocity = Mathf.Lerp(landingVelocity, 0f, landingReturnSpeed * Time.deltaTime);
            landingOffset = Mathf.Lerp(landingOffset, 0f, landingReturnSpeed * Time.deltaTime);
        }

        // ✅ Импакт шага — резкий толчок при каждом шаге
        stepImpactOffset = Mathf.Lerp(stepImpactOffset, 0f, stepImpactSpeed * Time.deltaTime);

        float baseY = crouching ? crouchCamY : standCamY;
        Vector3 basePos = new Vector3(defaultLocalPos.x, baseY, defaultLocalPos.z);

        if (!isGrounded || uiOpen)
        {
            timer = 0f;
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                basePos + Vector3.up * landingOffset,
                returnSpeed * Time.deltaTime);
            return;
        }

        if (isMoving)
        {
            float speed = sprinting ? sprintBobSpeed : crouching ? crouchBobSpeed : walkBobSpeed;
            float amountX = sprinting ? sprintBobAmountX : crouching ? crouchBobAmountX : walkBobAmountX;
            float amountY = sprinting ? sprintBobAmountY : crouching ? crouchBobAmountY : walkBobAmountY;

            timer += Time.deltaTime * speed;

            float currentSin = Mathf.Sin(timer);

            // ✅ Определяем момент шага — когда синус переходит через ноль вниз
            if (lastTimerSin >= 0f && currentSin < 0f)
                stepImpactOffset = -stepImpactAmount;

            lastTimerSin = currentSin;

            Vector3 target = basePos + new Vector3(
                Mathf.Sin(timer) * amountX,
                Mathf.Abs(currentSin) * amountY + stepImpactOffset + landingOffset,
                0f);

            transform.localPosition = Vector3.Lerp(
                transform.localPosition, target, returnSpeed * Time.deltaTime);
        }
        else
        {
            timer = 0f;
            lastTimerSin = 0f;
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                basePos + Vector3.up * landingOffset,
                returnSpeed * Time.deltaTime);
        }
    }
}