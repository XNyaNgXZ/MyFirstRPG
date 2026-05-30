using UnityEngine;

public class Footstep : MonoBehaviour
{

    public AudioSource audioSource;
    public AudioClip footstepSound;
    public float footstepVolume = 0.3f;
    public float stepInterval = 0.5f; // Интервал между шагами

    private float stepTimer = 0f;
    private CharacterController controller;
    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void Update()
    {
        bool isGrounded = controller.isGrounded;
        bool isMoving = (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0);
        // Дополнительное условие: вертикальная скорость не должна быть положительной (исключаем прыжок)

        bool isNotJumping = controller.velocity.y <= 0.1f;

        if (isGrounded && isMoving && isNotJumping)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                audioSource.PlayOneShot(footstepSound, footstepVolume);
                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }
}
