using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("Настройки инерции")]
    public float swayAmount = 0.02f;     // Множитель силы инерции
    public float maxSwayAmount = 0.06f;  // Максимальное смещение
    public float smoothAmount = 6f;      // Плавность возврата в исходное положение

    private Vector3 initialPosition;

    void Start()
    {
        // Запоминаем исходную локальную позицию оружия (относительно камеры/рук)
        initialPosition = transform.localPosition;
    }

    void Update()
    {
        // Получаем движения мыши и умножаем на силу инерции
        float moveX = -Input.GetAxis("Mouse X") * swayAmount;
        float moveY = -Input.GetAxis("Mouse Y") * swayAmount;

        // Ограничиваем максимальное смещение, чтобы оружие не "улетало"
        moveX = Mathf.Clamp(moveX, -maxSwayAmount, maxSwayAmount);
        moveY = Mathf.Clamp(moveY, -maxSwayAmount, maxSwayAmount);

        // Рассчитываем целевую позицию
        Vector3 targetPosition = new Vector3(moveX, moveY, 0f);
        // Плавно перемещаем оружие к целевой позиции (с учётом изначального положения)
        transform.localPosition = Vector3.Lerp(transform.localPosition, initialPosition + targetPosition, Time.deltaTime * smoothAmount);
    }
}