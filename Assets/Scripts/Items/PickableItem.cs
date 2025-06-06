using UnityEngine;

public class PickableItem : MonoBehaviour
{
    private Rigidbody rb;
    private Material originalMaterial; // Сохраняем исходный материал

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        originalMaterial = GetComponent<Renderer>().material;
    }

    // Можно вызвать при подборе (например, для дополнительных эффектов)
    public void OnPickedUp()
    {
        rb.isKinematic = true;
    }

    // Можно вызвать при отпускании
    public void OnDropped()
    {
        rb.isKinematic = false;
    }

    // Можно вызвать при броске
    public void OnThrown(Vector3 force)
    {
        rb.isKinematic = false;
        rb.AddForce(force);
    }

    // Возвращает исходный материал (для сброса подсветки)
    public Material GetOriginalMaterial()
    {
        return originalMaterial;
    }
}