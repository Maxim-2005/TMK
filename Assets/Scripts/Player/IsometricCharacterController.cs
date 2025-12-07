using UnityEngine;
// using UnityEngine.InputSystem; // Удаляем, так как пока не используем InputSystem для мыши напрямую

// Требует наличия компонента CharacterController на том же объекте.
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
    
    // МЫ БОЛЬШЕ НЕ ИСПОЛЬЗУЕМ rotationSpeed ДЛЯ ПОВОРОТА, но оставим для плавности
    [Tooltip("Скорость, с которой персонаж плавно поворачивается к курсору.")]
    public float rotationSpeed = 15f; 

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
    
    // Слой, по которому будет выполняться Raycast от камеры для определения точки прицеливания (например, "Ground").
    [Header("Targeting")]
    [Tooltip("Слой, который Raycast будет использовать для определения точки прицеливания.")]
    public LayerMask groundLayer; 

    // Компоненты
    private CharacterController characterController;
    private Camera mainCamera;

    // Внутреннее состояние движения
    private Vector3 _velocity;
    private float _currentSpeed;
    private Vector3 _moveDirection;

    // Внутреннее состояние рывка
    private bool _isDashing = false;
    private bool _canDash = true;
    private Vector3 _dashDirection;
    private float _dashTimer;
    private float _dashCooldownTimer;

    void Awake()
    {
        // Получаем CharacterController
        characterController = GetComponent<CharacterController>();
        // Находим главную камеру, которая нужна для Raycasting
        mainCamera = Camera.main; 

        if (mainCamera == null)
        {
            Debug.LogError("Main Camera is missing the 'MainCamera' tag. Rotation by mouse will not work.");
        }
    }

    void Update()
    {
        HandleDashTimers();
        
        // Предотвращаем движение, пока идет рывок
        if (_isDashing)
        {
            // Движение во время рывка
            characterController.Move(_dashDirection * dashPower * Time.deltaTime);
            // Гравитация не применяется, пока идет рывок
            return;
        }

        HandleMovement();
        HandleRotation(); // Новый метод для поворота по курсору
        ApplyGravity();
        
        // Применяем финальное движение
        characterController.Move(_velocity * Time.deltaTime);
    }

    /// <summary>
    /// Обрабатывает ввод для движения (WASD/стрелки) и рассчитывает _moveDirection.
    /// </summary>
    private void HandleMovement()
    {
        // Получаем ввод по горизонтали и вертикали
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        // Определяем направление движения относительно глобальных осей (или относительно изометрической камеры)
        // Если камера фиксирована, можно использовать мировые оси:
        _moveDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;
        
        // Определяем текущую скорость (спринт или ходьба)
        _currentSpeed = Input.GetKey(sprintKey) ? sprintSpeed : walkSpeed;
        
        // Рассчитываем вектор движения в плоскости
        Vector3 planarVelocity = _moveDirection * _currentSpeed;
        
        // Сохраняем вертикальную скорость
        _velocity.x = planarVelocity.x;
        _velocity.z = planarVelocity.z;
        // _velocity.y уже обработан в ApplyGravity
    }
    
    /// <summary>
    /// Выполняет Raycast от курсора мыши до GroundLayer и поворачивает персонажа к этой точке.
    /// </summary>
    private void HandleRotation()
    {
        if (mainCamera == null) return;
        
        // 1. Создаем Ray из позиции курсора мыши
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // 2. Выполняем Raycast только по указанному слою земли (GroundLayer)
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            // 3. Получаем точку пересечения луча с плоскостью (целевая точка)
            Vector3 targetPoint = hit.point;
            
            // 4. Игнорируем высоту Y, чтобы персонаж не наклонялся вверх/вниз
            targetPoint.y = transform.position.y;

            // 5. Вычисляем вектор направления от персонажа к целевой точке
            Vector3 direction = targetPoint - transform.position;
            
            // 6. Если направление не нулевое, поворачиваем персонажа
            if (direction.magnitude > 0.1f)
            {
                // Вычисляем желаемый поворот
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                
                // Плавно поворачиваем персонажа
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, 
                    targetRotation, 
                    rotationSpeed * Time.deltaTime
                );
            }
        }
    }

    private void HandleDashTimers()
    {
        if (!_canDash)
        {
            _dashCooldownTimer -= Time.deltaTime;
            if (_dashCooldownTimer <= 0f)
            {
                _canDash = true;
            }
        }
        
        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0f)
            {
                _isDashing = false;
            }
            // Рывок активен, выходим, чтобы не применять обычное движение/гравитацию
            // Движение во время рывка обрабатывается прямо в Update()
        }

        // Проверяем возможность и нажатие для рывка
        if (_canDash && Input.GetKeyDown(dashKey))
        {
            Dash();
        }
    }

    private void Dash()
    {
        // Получаем направление взгляда персонажа (теперь это направление к курсору!)
        _dashDirection = transform.forward;
        
        // Запускаем рывок
        _isDashing = true;
        _canDash = false;
        _dashTimer = dashDuration;
        _dashCooldownTimer = dashReloadTime;

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
            // Time.deltaTime * Time.deltaTime используется для правильной физической формулы (v = at)
            _velocity.y += gravity * Time.deltaTime; 
        }
    }
}