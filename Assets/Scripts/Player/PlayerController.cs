using UnityEngine;
using UnityEngine.InputSystem;

public class CameraXZMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private InputActionReference moveAction;

    private void OnEnable() => moveAction.action.Enable();
    private void OnDisable() => moveAction.action.Disable();

    void Update()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        transform.position += new Vector3(input.x, 0, input.y) * moveSpeed * Time.deltaTime;
    }
}