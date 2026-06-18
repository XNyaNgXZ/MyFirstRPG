using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("Инерция от мыши (Lunacid стиль)")]
    public float swaySpeed = 2f;
    public float swayReturnSpeed = 0.5f;
    public float maxSwayX = 0.12f;
    public float maxSwayY = 0.08f;
    public float mouseSensitivity = 0.003f;

    private Vector3 initialPosition;
    private Vector2 currentSway;
    private Vector2 targetSway;

    void Start()
    {
        initialPosition = transform.localPosition;
    }

    void Update()
    {
        if (InventoryUICode.IsOpen || EquipmentUI.IsOpen)
        {
            targetSway = Vector2.Lerp(targetSway, Vector2.zero, Time.deltaTime * swayReturnSpeed);
            currentSway = Vector2.Lerp(currentSway, targetSway, Time.deltaTime * swaySpeed * 10f);
            transform.localPosition = Vector3.Lerp(
                transform.localPosition, initialPosition, Time.deltaTime * swayReturnSpeed);
            return;
        }

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        bool mouseMoving = Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f;

        if (mouseMoving)
        {
            targetSway.x -= mouseX * mouseSensitivity * swaySpeed * 50f;
            targetSway.y -= mouseY * mouseSensitivity * swaySpeed * 50f;
            targetSway.x = Mathf.Clamp(targetSway.x, -maxSwayX, maxSwayX);
            targetSway.y = Mathf.Clamp(targetSway.y, -maxSwayY, maxSwayY);
        }
        else
        {
            targetSway = Vector2.Lerp(targetSway, Vector2.zero, Time.deltaTime * swayReturnSpeed);
        }

        currentSway = Vector2.Lerp(currentSway, targetSway, Time.deltaTime * swaySpeed * 10f);

        Vector3 targetPosition = initialPosition + new Vector3(currentSway.x, currentSway.y, 0f);
        transform.localPosition = Vector3.Lerp(
            transform.localPosition, targetPosition, Time.deltaTime * swaySpeed * 10f);
    }
}