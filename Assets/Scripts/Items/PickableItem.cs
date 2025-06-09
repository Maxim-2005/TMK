using UnityEngine;

public class PickableItem : MonoBehaviour
{
    private Rigidbody rb;
    private Material originalMaterial;
    private Collider objectCollider;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        originalMaterial = GetComponent<Renderer>().material;
        objectCollider = GetComponent<Collider>();
    }

    // Можно вызвать при подборе (например, для дополнительных эффектов)
    public void OnPickedUp()
    {
        rb.isKinematic = true;
        objectCollider.enabled = false;
    }

    // Можно вызвать при отпускании
    public void OnDropped()
    {
        objectCollider.enabled = true;
        rb.isKinematic = false;
    }

    // Можно вызвать при броске
    public void OnThrown(Vector3 force)
    {
        objectCollider.enabled = true;
        rb.isKinematic = false;
        rb.AddForce(force);
    }

    // Возвращает исходный материал (для сброса подсветки)
    public Material GetOriginalMaterial()
    {
        return originalMaterial;
    }
}
