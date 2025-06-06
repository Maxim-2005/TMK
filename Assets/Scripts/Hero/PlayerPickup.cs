using UnityEngine;
using System.Linq;

public class PlayerPickup : MonoBehaviour
{
    public string targetTag = "Item";
    public Transform holdPosition;
    public float throwForce = 500f;
    public float pickupRange = 2f;
    public Color highlightColor = Color.yellow;
    public float highlightIntensity = 1f;

    private GameObject heldObject;
    private Rigidbody heldObjectRb;
    private GameObject lastHighlightedItem;
    private Material originalMaterial;

    void Update()
    {
        HighlightNearestItem();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldObject == null)
                TryPickupNearestItem();
            else
                DropItem();
        }

        if (heldObject != null && Input.GetKeyDown(KeyCode.G))
            ThrowItem();

        if (heldObject != null)
            heldObject.transform.position = holdPosition.position;
    }

    void HighlightNearestItem()
    {
        if (heldObject != null) 
        {
            if (lastHighlightedItem != null)
                ResetHighlight(lastHighlightedItem);
            return;
        }

        GameObject[] items = GameObject.FindGameObjectsWithTag(targetTag)
            .Where(item => 
                Vector3.Distance(transform.position, item.transform.position) <= pickupRange)
            .ToArray();

        if (items.Length == 0)
        {
            if (lastHighlightedItem != null)
                ResetHighlight(lastHighlightedItem);
            return;
        }

        GameObject nearestItem = items
            .OrderBy(item => Vector3.Distance(transform.position, item.transform.position))
            .First();

        // Всегда сбрасываем предыдущую подсветку, даже если предмет тот же
        if (lastHighlightedItem != null)
            ResetHighlight(lastHighlightedItem);

        // Подсвечиваем текущий предмет
        HighlightItem(nearestItem);
        lastHighlightedItem = nearestItem;
    }

    void HighlightItem(GameObject item)
    {
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Сохраняем оригинальный материал только если он еще не сохранен
            if (originalMaterial == null || item != lastHighlightedItem)
                originalMaterial = renderer.material;
            
            Material highlightMat = new Material(originalMaterial);
            highlightMat.EnableKeyword("_EMISSION");
            highlightMat.SetColor("_EmissionColor", highlightColor * highlightIntensity);
            renderer.material = highlightMat;
        }
    }

    void ResetHighlight(GameObject item)
    {
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null && originalMaterial != null)
        {
            renderer.material = originalMaterial;
            // Сбрасываем сохраненный материал, чтобы при следующем выделении взять актуальный
            originalMaterial = null;
        }
    }

    void TryPickupNearestItem()
    {
        GameObject[] items = GameObject.FindGameObjectsWithTag(targetTag)
            .Where(item => 
                Vector3.Distance(transform.position, item.transform.position) <= pickupRange)
            .ToArray();

        if (items.Length == 0) return;

        GameObject nearestItem = items
            .OrderBy(item => Vector3.Distance(transform.position, item.transform.position))
            .First();

        heldObject = nearestItem;
        heldObjectRb = nearestItem.GetComponent<Rigidbody>();
        heldObjectRb.isKinematic = true;
    }

    void DropItem()
    {
        heldObjectRb.isKinematic = false;
        heldObject = null;
    }

    void ThrowItem()
    {
        heldObjectRb.isKinematic = false;
        heldObjectRb.AddForce(transform.forward * throwForce);
        heldObject = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}