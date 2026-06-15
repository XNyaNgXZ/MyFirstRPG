using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Движение")]
    public float walkSpeed = 3f;
    public float mouseSensitivity = 2f;

    [Header("Спринт")]
    public float sprintMultiplier = 1.3f;
    public float maxStamina = 100f;
    public float staminaDrain = 20f;
    public float staminaRegen = 15f;
    public float staminaHideDelay = 3f;
    public float staminaRegenDelay = 2f;  // ✅ задержка перед реген
    private float lastStaminaUseTime = -10f;

    [HideInInspector] public float currentStamina;
    [HideInInspector] public bool isSprinting;

    [Header("Камера — плавность")]
    [SerializeField] private float verticalSensitivity = 2f;
    [SerializeField] private float maxVerticalAngle = 75f;
    [Tooltip("Плавность поворота камеры. Lunacid ~12")]
    public float cameraSmoothing = 12f;

    [Header("Наклон камеры")]
    public float tiltAmount = 1.5f;
    public float mouseTiltAmount = 2.5f;
    public float tiltSpeed = 8f;

    [Header("Jump and Gravity")]
    public float jumpHeight = 1.2f;
    public float gravity = -15f;
    public float jumpCooldown = 0.3f;
    private Vector3 verticalVelocity;
    private float lastJumpTime = -10f;

    [Header("Crouch Settings")]
    public float crouchSpeed = 1.5f;
    public float standHeight = 2f;
    public float crouchHeight = 1f;

    [Header("Sounds")]
    public AudioClip jumpSound;
    public AudioClip landSound;
    [Range(0f, 1f)] public float jumpVolume = 0.6f;
    [Range(0f, 1f)] public float landVolume = 0.7f;

    private CharacterController controller;
    public bool IsCrouching { get; private set; } = false;
    private float originalWalkSpeed;
    private Vector3 originalCenter;
    private Camera playerCamera;
    private AudioSource audioSource;
    private bool wasGrounded = true;

    // Плавное вращение
    private float targetYaw = 0f;
    private float targetPitch = 0f;
    private float currentYaw = 0f;
    private float currentPitch = 0f;

    // Наклон
    private float currentStrafeTilt = 0f;
    private float currentMouseTilt = 0f;
    private float lastMouseX = 0f;

    void Start()
    {
        IsCrouching = false;
        controller = GetComponent<CharacterController>();
        originalWalkSpeed = walkSpeed;
        originalCenter = controller.center;
        playerCamera = GetComponentInChildren<Camera>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        currentStamina = maxStamina;
        targetYaw = currentYaw = transform.eulerAngles.y;
        LockCursor();
    }

    void Update()
    {
        bool uiOpen = InventoryUICode.IsOpen || EquipmentUI.IsOpen;
        HandleCamera(uiOpen);
        HandleMovement(uiOpen);
        HandleCrouch(uiOpen);
    }

    void HandleCamera(bool uiOpen)
    {
        float mouseX = 0f, mouseY = 0f;

        if (!uiOpen)
        {
            mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            mouseY = Input.GetAxis("Mouse Y") * verticalSensitivity;
            targetYaw += mouseX;
            targetPitch -= mouseY;
            targetPitch = Mathf.Clamp(targetPitch, -maxVerticalAngle, maxVerticalAngle);

        }

        float smooth = cameraSmoothing * Time.deltaTime;
        currentYaw = Mathf.Lerp(currentYaw, targetYaw, smooth);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, smooth);
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

        float horizontal = Input.GetAxisRaw("Horizontal");
        float targetStrafeTilt = uiOpen ? 0f : -horizontal * tiltAmount;
        currentStrafeTilt = Mathf.Lerp(currentStrafeTilt, targetStrafeTilt, tiltSpeed * Time.deltaTime);

        lastMouseX = Mathf.Lerp(lastMouseX, mouseX, tiltSpeed * Time.deltaTime);
        float targetMouseTilt = uiOpen ? 0f : -lastMouseX * mouseTiltAmount;
        currentMouseTilt = Mathf.Lerp(currentMouseTilt, targetMouseTilt, tiltSpeed * Time.deltaTime);

        if (playerCamera != null)
            playerCamera.transform.localEulerAngles =
                new Vector3(currentPitch, 0f, currentStrafeTilt + currentMouseTilt);
    }

    void HandleMovement(bool uiOpen)
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 moveDir = transform.right * horizontal + transform.forward * vertical;
        if (moveDir.magnitude > 1f) moveDir.Normalize();

        bool isGrounded = controller.isGrounded;
        bool isMoving = moveDir.magnitude > 0.1f;

        if (!wasGrounded && isGrounded)
            if (landSound != null) audioSource.PlayOneShot(landSound, landVolume);
        wasGrounded = isGrounded;

        if (isGrounded && verticalVelocity.y < 0)
            verticalVelocity.y = -2f;

        // ✅ Спринт
        bool wantSprint = Input.GetKey(KeyCode.LeftShift) && isMoving &&
                          !IsCrouching && !uiOpen && isGrounded;

        // Спринт
        if (wantSprint && currentStamina > 0f)
        {
            isSprinting = true;
            currentStamina = Mathf.Max(0f, currentStamina - staminaDrain * Time.deltaTime);
            walkSpeed = originalWalkSpeed * sprintMultiplier;
            lastStaminaUseTime = Time.time; // ✅ фиксируем использование
        }
        else
        {
            isSprinting = false;
            walkSpeed = IsCrouching ? crouchSpeed : originalWalkSpeed;

            // ✅ Реген только если прошло 2 сек после использования И на земле
            if (isGrounded && Time.time >= lastStaminaUseTime + staminaRegenDelay)
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegen * Time.deltaTime);
        }

        // Прыжок
        if (Input.GetButtonDown("Jump") && isGrounded &&
            Time.time >= lastJumpTime + jumpCooldown && !uiOpen)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpTime = Time.time;
            lastStaminaUseTime = Time.time; // ✅ прыжок тоже останавливает реген
            if (jumpSound != null) audioSource.PlayOneShot(jumpSound, jumpVolume);
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        Vector3 finalMove = moveDir * walkSpeed * Time.deltaTime;
        finalMove.y = verticalVelocity.y * Time.deltaTime;
        controller.Move(finalMove);
    }

    void HandleCrouch(bool uiOpen)
    {
        if (!Input.GetKeyDown(KeyCode.LeftControl) || uiOpen || !controller.isGrounded) return;

        IsCrouching = !IsCrouching;

        if (IsCrouching)
        {
            controller.height = crouchHeight;
            walkSpeed = crouchSpeed;
            Vector3 c = originalCenter; c.y -= (standHeight - crouchHeight) / 2f;
            controller.center = c;
            if (playerCamera != null)
            {
                var cam = playerCamera.transform.localPosition; cam.y = 0.2f;
                playerCamera.transform.localPosition = cam;
            }
        }
        else
        {
            controller.height = standHeight;
            walkSpeed = originalWalkSpeed;
            controller.center = originalCenter;
            if (playerCamera != null)
            {
                var cam = playerCamera.transform.localPosition; cam.y = 0.5f;
                playerCamera.transform.localPosition = cam;
            }
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