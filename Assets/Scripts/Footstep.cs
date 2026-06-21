using UnityEngine;

public class Footstep : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip footstepSound;

    [Header("Громкость")]
    public float walkVolume = 0.3f;
    public float sprintVolume = 0.5f;
    public float crouchVolume = 0.1f;

    [Header("Интервал между шагами")]
    public float walkInterval = 0.5f;
    public float sprintInterval = 0.3f;
    public float crouchInterval = 0.8f;

    private float stepTimer = 0f;
    private CharacterController controller;
    private PlayerMovement playerMovement;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        bool isGrounded = controller.isGrounded;
        // ✅ Движение через velocity — стрелки не вызывают шаги
        Vector3 hVel = controller.velocity; hVel.y = 0;
        bool isMoving = hVel.magnitude > 0.1f;
        bool isNotJumping = controller.velocity.y <= 0.1f;

        if (isGrounded && isMoving && isNotJumping)
        {
            bool isSprinting = playerMovement != null && playerMovement.isSprinting;
            bool isCrouching = playerMovement != null && playerMovement.IsCrouching;

            float interval = isSprinting ? sprintInterval
                           : isCrouching ? crouchInterval
                           : walkInterval;
            float volume = isSprinting ? sprintVolume
                         : isCrouching ? crouchVolume
                         : walkVolume;

            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                audioSource.PlayOneShot(footstepSound, volume);
                stepTimer = interval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }
}