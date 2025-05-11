using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference zoomAction;

    void OnEnable()
    {
        moveAction.action.Enable();
        zoomAction.action.Enable();
    }

    void OnDisable()
    {
        moveAction.action.Disable();
        zoomAction.action.Disable();
    }

    void Update()
    {
        // Чтение ввода
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        float zoomInput = zoomAction.action.ReadValue<float>();

        // Движение и зум
        transform.Translate(new Vector3(moveInput.x, 0, moveInput.y) * moveSpeed * Time.deltaTime);
        Camera.main.orthographicSize -= zoomInput * 0.1f;
    }
}