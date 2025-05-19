using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class IsometricCharacterController : MonoBehaviour
{
    [Header("Movement")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public float walkSpeed = 3f;
    public float sprintSpeed = 7f;
    public float rotationSpeed = 10f;

    [Header("Components")]
    private CharacterController characterController;
    private Camera mainCamera;
    private CharacterController _controller;
    private float _currentSpeed;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        mainCamera = Camera.main;

        _controller = GetComponent<CharacterController>();
        _currentSpeed = walkSpeed;
    }

    void Update()
    {
         // Спринт при зажатой клавише
        _currentSpeed = Input.GetKey(sprintKey) ? sprintSpeed : walkSpeed;

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

        // Создаем вектор движения и НОРМАЛИЗУЕМ его
        Vector3 moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;

        // Двигаем персонажа
        if (moveDirection.magnitude > 0.1f)
        {
            // Перемещение с гарантированно одинаковой скоростью
            characterController.Move(moveDirection * _currentSpeed * Time.deltaTime);
            
            // Поворот (оставляем без изменений)
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}