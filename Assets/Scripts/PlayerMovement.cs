using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float mouseSensitivity = 2f;

    [Header("Jump and Gravity")]
    public float jumpHeight = 1.2f;
    public float gravity = -15f;
    public float jumpCooldown = 0.3f;   // задержка между прыжками (сек)
    private Vector3 verticalVelocity;
    private float lastJumpTime = -10f;   // время последнего прыжка

    [Header("Camera")]
    [SerializeField] private float verticalSensitivity = 2f;
    [SerializeField] private float maxVerticalAngle = 80f;

    [Header("Crouch Settings")]
    public float crouchSpeed = 2.5f;
    public float standHeight = 2f;
    public float crouchHeight = 1f;

    private CharacterController controller;
    private bool isRotatingCamera = false;
    private bool isCrouching = false;
    private float originalWalkSpeed;
    private Vector3 originalCenter;
    private Camera playerCamera;
    private float verticalRotation = 0f;



    private void Start()
    {
        controller = GetComponent<CharacterController>();
        originalWalkSpeed = walkSpeed;
        originalCenter = controller.center;
        playerCamera = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;

        if (controller.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f; // небольшое прижатие к земле
        }
        if (Input.GetButtonDown("Jump") && controller.isGrounded && Time.time >= lastJumpTime + jumpCooldown)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpTime = Time.time;   // запоминаем момент прыжка
        }
        verticalVelocity.y += gravity * Time.deltaTime;

        // Вращение камеры только если курсор НЕ над UI
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButtonDown(1))
            {
                isRotatingCamera = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (isRotatingCamera)
        {
            // Прерываем вращение, если отпустили ПКМ или курсор наехал на UI
            if (Input.GetMouseButtonUp(1) || EventSystem.current.IsPointerOverGameObject())
            {
                isRotatingCamera = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * verticalSensitivity;
                transform.Rotate(0, mouseX, 0);
                verticalRotation -= mouseY;
                verticalRotation = Mathf.Clamp(verticalRotation, -maxVerticalAngle, maxVerticalAngle);
                playerCamera.transform.localEulerAngles = new Vector3(verticalRotation, 0, 0);
            }
        }

        if (moveDirection.magnitude > 1f)
            moveDirection.Normalize();

        Vector3 finalMove = moveDirection * walkSpeed * Time.deltaTime;
        finalMove.y = verticalVelocity.y * Time.deltaTime;
        controller.Move(finalMove);

        if (isRotatingCamera)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * verticalSensitivity;

            transform.Rotate(0, mouseX, 0); // Горизонтальный поворот

            verticalRotation -= mouseY; // Вертикальный поворот
            verticalRotation = Mathf.Clamp(verticalRotation, -maxVerticalAngle, maxVerticalAngle);
            playerCamera.transform.localEulerAngles = new Vector3(verticalRotation, 0, 0);
        }

        bool crouchInput = Input.GetKey(KeyCode.LeftControl);

        if (crouchInput && !isCrouching)
        {
            controller.height = crouchHeight;
            walkSpeed = crouchSpeed;
            Vector3 newCenter = originalCenter;
            newCenter.y -= (standHeight - crouchHeight) / 2f;
            controller.center = newCenter;

            Vector3 camPos = playerCamera.transform.localPosition;
            camPos.y = 0.2f;
            playerCamera.transform.localPosition = camPos;
            isCrouching = true;
        }
        else if (!crouchInput && isCrouching)
        {
            controller.height = standHeight;
            walkSpeed = originalWalkSpeed;
            controller.center = originalCenter;

            Vector3 camPos = playerCamera.transform.localPosition;
            camPos.y = 0.5f;
            playerCamera.transform.localPosition = camPos;
            isCrouching = false;
        }
    }
}