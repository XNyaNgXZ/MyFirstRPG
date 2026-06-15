using UnityEngine;

public class WeaponBob : MonoBehaviour
{
    [Header("Настройки покачивания")]
    public float walkBobSpeed = 8f;      // Скорость покачивания при ходьбе
    public float walkBobAmountX = 0.05f; // Амплитуда покачивания по горизонтали при ходьбе
    public float walkBobAmountY = 0.07f; // Амплитуда покачивания по вертикали при ходьбе

    public float sprintBobSpeed = 12f;    // Скорость покачивания при беге
    public float sprintBobAmountX = 0.08f;
    public float sprintBobAmountY = 0.12f;

    public float crouchBobSpeed = 5f;     // Скорость покачивания при приседании
    public float crouchBobAmountX = 0.03f;
    public float crouchBobAmountY = 0.04f;

    [Header("Плавность")]
    public float returnSpeed = 8f;        // Скорость возврата в исходное положение

    private Vector3 initialPosition;
    private float timer = 0f;
    private CharacterController controller;
    private PlayerMovement playerMovement; // Обратитесь к вашему скрипту PlayerMovement

    void Start()
    {
        initialPosition = transform.localPosition;
        controller = GetComponentInParent<CharacterController>();
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    void Update()
    {
        if (controller == null) return;

        bool isMoving = Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
        bool isGrounded = controller.isGrounded;

        // Прерываем эффект, если персонаж не на земле или не двигается
        if (!isGrounded || !isMoving)
        {
            timer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialPosition, returnSpeed * Time.deltaTime);
            return;
        }

        // Определяем параметры покачивания в зависимости от состояния (бег/присед/ходьба)
        float speed = walkBobSpeed;
        float amountX = walkBobAmountX;
        float amountY = walkBobAmountY;

        if (playerMovement != null)
        {
            if (playerMovement.isSprinting)
            {
                speed = sprintBobSpeed;
                amountX = sprintBobAmountX;
                amountY = sprintBobAmountY;
            }
            else if (playerMovement.IsCrouching)
            {
                speed = crouchBobSpeed;
                amountX = crouchBobAmountX;
                amountY = crouchBobAmountY;
            }
        }

        // Увеличиваем таймер, создавая волну
        timer += Time.deltaTime * speed;

        // Рассчитываем целевое смещение
        Vector3 targetPosition = initialPosition + new Vector3(
            Mathf.Sin(timer) * amountX,
            Mathf.Abs(Mathf.Sin(timer)) * amountY,
            0f
        );

        // Плавно перемещаем оружие к целевой позиции
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, returnSpeed * Time.deltaTime);
    }
}