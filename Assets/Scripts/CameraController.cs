using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target;  // Персонаж
    public Vector3 offset = new Vector3(-5, 10, -20);  // Смещение камеры
    public float smoothSpeed = 0.5f;  // Плавность слежения

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogError("Target not assigned!");
            return;
        }

        // Вычисляем желаемую позицию
        Vector3 desiredPosition = target.position + offset;
        
        // Плавное перемещение
        transform.position = Vector3.Lerp(
            transform.position, 
            desiredPosition, 
            smoothSpeed * Time.deltaTime * 10  // Ускорение для компенсации Lerp
        );

        // Фиксированный изометрический угол (45°)
        transform.rotation = Quaternion.Euler(30, 30, 0);
    }
}