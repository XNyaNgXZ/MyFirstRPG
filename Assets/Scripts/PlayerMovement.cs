using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float mouseSensitivity = 2f;

    [Header("Jump and Gravity")]
    public float jumpHeight = 1.2f;
    public float gravity = -15f;
    public float jumpCooldown = 0.3f;
    private Vector3 verticalVelocity;
    private float lastJumpTime = -10f;

    [Header("Camera")]
    [SerializeField] private float verticalSensitivity = 2f;
    [SerializeField] private float maxVerticalAngle = 80f;

    [Header("Crouch Settings")]
    public float crouchSpeed = 2.5f;
    public float standHeight = 2f;
    public float crouchHeight = 1f;

    private CharacterController controller;
    private bool isCrouching = false;
    private float originalWalkSpeed;
    private Vector3 originalCenter;
    private Camera playerCamera;
    private float verticalRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        originalWalkSpeed = walkSpeed;
        originalCenter = controller.center;
        playerCamera = GetComponentInChildren<Camera>();

        // ✅ Сразу блокируем курсор — FPS режим
        LockCursor();
    }

    void Update()
    {
        bool uiOpen = InventoryUICode.IsOpen || EquipmentUI.IsOpen;

        // ✅ Мышь вращает камеру только когда UI закрыт
        if (!uiOpen)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * verticalSensitivity;

            transform.Rotate(0, mouseX, 0);
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -maxVerticalAngle, maxVerticalAngle);
            playerCamera.transform.localEulerAngles = new Vector3(verticalRotation, 0, 0);

            // Держим курсор заблокированным
            if (Cursor.lockState != CursorLockMode.Locked)
                LockCursor();
        }

        // Движение
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        if (moveDirection.magnitude > 1f) moveDirection.Normalize();

        // Гравитация и прыжок
        if (controller.isGrounded && verticalVelocity.y < 0)
            verticalVelocity.y = -2f;

        if (Input.GetButtonDown("Jump") && controller.isGrounded &&
            Time.time >= lastJumpTime + jumpCooldown && !uiOpen)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpTime = Time.time;
        }
        verticalVelocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = moveDirection * walkSpeed * Time.deltaTime;
        finalMove.y = verticalVelocity.y * Time.deltaTime;
        controller.Move(finalMove);

        // Приседание
        bool crouchInput = Input.GetKey(KeyCode.LeftControl);
        if (crouchInput && !isCrouching)
        {
            controller.height = crouchHeight;
            walkSpeed = crouchSpeed;
            Vector3 c = originalCenter; c.y -= (standHeight - crouchHeight) / 2f;
            controller.center = c;
            Vector3 cam = playerCamera.transform.localPosition; cam.y = 0.2f;
            playerCamera.transform.localPosition = cam;
            isCrouching = true;
        }
        else if (!crouchInput && isCrouching)
        {
            controller.height = standHeight;
            walkSpeed = originalWalkSpeed;
            controller.center = originalCenter;
            Vector3 cam = playerCamera.transform.localPosition; cam.y = 0.5f;
            playerCamera.transform.localPosition = cam;
            isCrouching = false;
        }
    }

    public static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}