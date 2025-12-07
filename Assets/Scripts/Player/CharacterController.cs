using UnityEngine;

// Требует наличия компонента CharacterController на том же объекте.
// Это гарантирует, что скрипт не будет работать без необходимого компонента для физики и движения.
[RequireComponent(typeof(CharacterController))]
public class IsometricCharacterController : MonoBehaviour
{
    [Header("Movement")]
    // Клавиша для ускорения (спринта).
    public KeyCode sprintKey = KeyCode.LeftShift;
    // Базовая скорость перемещения персонажа.
    public float walkSpeed = 3f;
    // Увеличенная скорость перемещения при зажатой клавише спринта.
    public float sprintSpeed = 7f;
    // Скорость, с которой персонаж плавно поворачивается в направлении движения.
    public float rotationSpeed = 10f;

    [Header("Dash")]
    // Клавиша для рывка
    public KeyCode dashKey = KeyCode.Space;
    //Время перезарядки рывка
    public float dashReloadTime = 2f;
    //Сила рывка
    public float dashPower = 15f;
    //Длительность рывка
    public float dashDuration = 0.2f;

    [Header("Gravity")]
    // Сила гравитации, которая будет применяться к персонажу. Отрицательное значение.
    public float gravity = -30f; 
    // Небольшая сила, которая прижимает персонажа к земле, когда он стоит на ней.
    public float groundVelocity = -2f; 

    [Header("Components")]
    // Ссылка на компонент CharacterController для обработки физического движения.
    private CharacterController characterController;
    // Ссылка на главную камеру сцены. Используется для расчета направления движения относительно обзора.
    private Camera mainCamera;
    
    // Текущая активная скорость (либо walkSpeed, либо sprintSpeed).
    private float _currentSpeed;
    // Вектор вертикальной скорости (включает гравитацию и потенциальный прыжок).
    private Vector3 _velocity; 

    // --- ДОБАВЛЕННЫЕ ПЕРЕМЕННЫЕ ДЛЯ РЫВКА ---
    private bool _isDashing = false;
    private bool _canDash = true;
    private float _dashTimer = 0f;
    private Vector3 _dashDirection;
    private float _dashCooldownTimer = 0f;

    void Start()
    {
        // Получаем обязательный компонент CharacterController
        characterController = GetComponent<CharacterController>();
        // Получаем ссылку на главную камеру для использования в расчетах
        mainCamera = Camera.main;

        // Устанавливаем начальную скорость персонажа
        _currentSpeed = walkSpeed;
        
        // Проверка на критические компоненты для повышения стабильности:
        if (characterController == null)
        {
            Debug.LogError("CharacterController не найден! Прикрепите CharacterController к этому объекту.");
        }
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera не найдена! Убедитесь, что на сцене есть активная камера с тегом 'MainCamera'.");
        }
    }

    void Update()
    {
        if (mainCamera == null || characterController == null) return;
        
        // --- 1. ЛОГИКА ГРАВИТАЦИИ ---
        ApplyGravity();

        // --- ОБРАБОТКА ПЕРЕЗАРЯДКИ РЫВКА ---
        HandleDashCooldown();

        // --- ЛОГИКА РЫВКА ---
        HandleDash();

        // Если в процессе рывка, не обрабатываем обычное движение
        if (_isDashing)
        {
            characterController.Move(_dashDirection * dashPower * Time.deltaTime);
            return;
        }

        // Логика спринта: если клавиша зажата, используем sprintSpeed, иначе walkSpeed.
        _currentSpeed = Input.GetKey(sprintKey) ? sprintSpeed : walkSpeed;

        // Получаем необработанный ввод с осей (Horizontal и Vertical).
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // --- 2. РАСЧЕТ ГОРИЗОНТАЛЬНОГО ДВИЖЕНИЯ ---

        // Если нет ввода, пропускаем расчет направления и поворота.
        if (horizontal == 0 && vertical == 0)
        {
            // Если персонаж не двигается, но не находится на земле, нужно применить только гравитацию.
            // Двигаем персонажа с текущей _velocity (гравитация)
            characterController.Move(_velocity * Time.deltaTime);
            return; 
        }

        // --- Расчет направления движения относительно камеры (ИЗОМЕТРИЧЕСКИЙ ВИД) ---
        
        // Получаем вектор "вперед" и "вправо" от текущего поворота камеры.
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;
        
        // Игнорируем наклон камеры по вертикали (Y), чтобы движение было строго горизонтальным (XZ плоскость).
        cameraForward.y = 0; 
        cameraRight.y = 0;
        
        // Нормализуем векторы, чтобы иметь чистое направление.
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Создаем итоговый вектор горизонтального движения.
        // .normalized гарантирует, что диагональное движение не будет быстрее прямого.
        Vector3 moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;

        // --- 3. ПРИМЕНЕНИЕ ДВИЖЕНИЯ И ПОВОРОТА ---
        
        if (moveDirection.magnitude > 0.1f)
        {
            // Общий вектор движения = Горизонтальный Вектор * Скорость + Вертикальный Вектор (гравитация)
            Vector3 finalMoveVector = moveDirection * _currentSpeed + _velocity;

            // characterController.Move(ОбщийВектор * ВремяКадра)
            characterController.Move(finalMoveVector * Time.deltaTime);
            
            // --- Логика Поворота ---
            
            // Вычисляем целевой поворот (Quaternion), в который должен смотреть персонаж (вдоль moveDirection).
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            
            // Плавно поворачиваем персонажа, используя Slerp для мягкого вращения.
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
        }
    }
    
    // --- ДОБАВЛЕННЫЕ МЕТОДЫ ДЛЯ РЫВКА ---

    private void HandleDashCooldown()
    {
        if (!_canDash)
        {
            _dashCooldownTimer -= Time.deltaTime;
            if (_dashCooldownTimer <= 0f)
            {
                _canDash = true;
            }
        }
    }

    private void HandleDash()
    {
        // Если рывок активен, обрабатываем таймер
        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0f)
            {
                _isDashing = false;
            }
            return;
        }

        // Проверяем возможность и нажатие для рывка
        if (_canDash && Input.GetKeyDown(dashKey))
        {
            Dash();
        }
    }

    private void Dash()
    {
        // Получаем направление взгляда персонажа
        _dashDirection = transform.forward;
        
        // Запускаем рывок
        _isDashing = true;
        _canDash = false;
        _dashTimer = dashDuration;
        _dashCooldownTimer = dashReloadTime;

        // Можно добавить визуальные/звуковые эффекты здесь
        Debug.Log("Dash activated!");
    }

    // Отдельный метод для обработки гравитации и сброса вертикальной скорости при касании земли.
    private void ApplyGravity()
    {
        // Проверяем, находится ли контроллер на земле
        if (characterController.isGrounded)
        {
            // Если да, сбрасываем вертикальную скорость до небольшой отрицательной силы 
            // для надежного "прижатия" к земле.
            _velocity.y = groundVelocity;
        }
        else
        {
            // Если нет, применяем гравитацию, увеличивая отрицательную вертикальную скорость.
            // Time.deltaTime используется для обеспечения кадронезависимости.
            _velocity.y += gravity * Time.deltaTime;
        }
    }
}