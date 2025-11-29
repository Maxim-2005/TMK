using UnityEngine;

// Скрипт для следования камеры за целью с плавной задержкой и фиксированным углом обзора.
public class FollowCamera : MonoBehaviour
{
    [Tooltip("Целевой объект (персонаж), за которым будет следовать камера.")]
    public Transform target;
    [Tooltip("Смещение камеры относительно цели.")]
    public Vector3 offset = new Vector3(-5, 10, -20);
    [Tooltip("Примерное время в секундах, за которое камера достигнет цели (чем меньше, тем быстрее).")]
    public float smoothTime = 0.5f;

    // Вспомогательная переменная, требуемая Vector3.SmoothDamp, для отслеживания текущей скорости камеры.
    private Vector3 velocity = Vector3.zero; 

    void Start()
    {
        // Устанавливаем фиксированную изометрическую ротацию один раз в начале.
        // Это предотвращает повторную установку одного и того же значения каждый кадр.
        transform.rotation = Quaternion.Euler(30, 30, 0);
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogError("Target not assigned! Пожалуйста, назначьте целевой объект в инспекторе.");
            return;
        }

        // 1. Вычисляем желаемую позицию, добавляя смещение к позиции цели.
        Vector3 desiredPosition = target.position + offset;
        
        // 2. Используем SmoothDamp для плавного перемещения. 
        // Это обеспечивает более естественное демпфированное движение, чем Lerp.
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            desiredPosition, 
            ref velocity, // Переменная, в которой SmoothDamp хранит скорость
            smoothTime    // Время достижения цели
        );

        // Примечание: Мы больше не устанавливаем transform.rotation здесь, 
        // так как он фиксирован и был установлен в Start().
    }
}