using UnityEngine;

public class Footstep : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip footstepSound;

    [Header("Громкость")]
    public float walkVolume = 0.3f;
    public float sprintVolume = 0.5f;  // громче при беге
    public float crouchVolume = 0.1f;  // тише при приседании

    [Header("Интервал между шагами")]
    public float walkInterval = 0.5f;
    public float sprintInterval = 0.3f; // быстрее при беге
    public float crouchInterval = 0.8f; // медленнее при приседании

    private float stepTimer = 0f;
    private CharacterController controller;
    private PlayerMovement playerMovement;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        bool isGrounded = controller.isGrounded;
        bool isMoving = Input.GetAxisRaw("Horizontal") != 0 ||
                            Input.GetAxisRaw("Vertical") != 0;
        bool isNotJumping = controller.velocity.y <= 0.1f;

        if (isGrounded && isMoving && isNotJumping)
        {
            bool isSprinting = playerMovement != null && playerMovement.isSprinting;
            bool isCrouching = playerMovement != null && playerMovement.IsCrouching;

            // ✅ Выбираем интервал и громкость по состоянию
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