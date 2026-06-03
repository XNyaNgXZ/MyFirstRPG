using UnityEngine;

// Повесь на MainCamera
public class HeadBob : MonoBehaviour
{
    [Header("Ходьба")]
    public float walkBobSpeed = 9f;
    public float walkBobAmountX = 0.07f;
    public float walkBobAmountY = 0.12f;

    [Header("Приседание")]
    public float crouchBobSpeed = 6f;
    public float crouchBobAmountX = 0.05f;
    public float crouchBobAmountY = 0.08f;

    [Header("Позиции камеры")]
    public float standCamY = 0.5f;   // должно совпадать с PlayerMovement
    public float crouchCamY = 0.2f;   // должно совпадать с PlayerMovement

    [Header("Плавность")]
    public float returnSpeed = 8f;

    private CharacterController controller;
    private PlayerMovement playerMovement;
    private float timer = 0f;
    private Vector3 defaultLocalPos;

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
        float baseY = crouching ? crouchCamY : standCamY;
        Vector3 basePos = new Vector3(defaultLocalPos.x, baseY, defaultLocalPos.z);

        if (!isGrounded || uiOpen)
        {
            timer = 0f;
            transform.localPosition = Vector3.Lerp(
                transform.localPosition, basePos, returnSpeed * Time.deltaTime);
            return;
        }

        if (isMoving)
        {
            float speed = crouching ? crouchBobSpeed : walkBobSpeed;
            float amountX = crouching ? crouchBobAmountX : walkBobAmountX;
            float amountY = crouching ? crouchBobAmountY : walkBobAmountY;

            timer += Time.deltaTime * speed;

            Vector3 target = basePos + new Vector3(
                Mathf.Sin(timer) * amountX,
                Mathf.Abs(Mathf.Sin(timer)) * amountY,
                0f);

            transform.localPosition = Vector3.Lerp(
                transform.localPosition, target, returnSpeed * Time.deltaTime);
        }
        else
        {
            timer = 0f;
            transform.localPosition = Vector3.Lerp(
                transform.localPosition, basePos, returnSpeed * Time.deltaTime);
        }
    }
}