using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class IsometricCharacterController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("Components")]
    private CharacterController characterController;
    private Camera mainCamera;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Получаем ввод
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (horizontal == 0 && vertical == 0)
        {
            return; // Полная остановка при отсутствии ввода
        }

        // Рассчитываем направление движения относительно камеры
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;
        cameraForward.y = 0; // Игнорируем наклон камеры по Y
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Создаем вектор движения
        Vector3 moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;

        // Двигаем персонажа
        if (moveDirection.magnitude > 0.1f)
        {
            // Перемещение
            characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

            // Плавный поворот в сторону движения
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}