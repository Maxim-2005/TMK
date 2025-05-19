using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Gravity : MonoBehaviour 
{
    public float gravity = -30f; // Сила гравитации (можно регулировать)
    private CharacterController _controller;
    private Vector3 _velocity;

    void Start() 
    {
        _controller = GetComponent<CharacterController>();
    }

    void Update() 
    {
        // Применяем гравитацию, если персонаж не на земле
        if (!_controller.isGrounded)
        {
            _velocity.y += gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
        }
        else 
        {
            _velocity.y = -2f; // Небольшая сила "прижатия" к земле
        }
    }
}