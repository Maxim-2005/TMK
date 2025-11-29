using UnityEngine;

/// <summary>
/// Component added to all pickable items (resources, weapons, etc.) to define their type
/// and handle the physics/hierarchy changes when picked up or dropped/thrown by the player.
/// </summary>
public class ItemPickup : MonoBehaviour
{
    [Tooltip("The unique type name of this item (e.g., 'Stone', 'Wood'). Used by Container.cs.")]
    public string ItemTypeName = "Stone";
    
    [Tooltip("Can this item be picked up by the player?")]
    public bool isPickable = true; 

    private Rigidbody rb;
    private Collider col;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        if (rb == null || col == null)
        {
            Debug.LogError($"ItemPickup on {gameObject.name} requires both Rigidbody and Collider components.");
        }
    }

    /// <summary>
    /// Handles the item being picked up, attaching it to the player's hand position.
    /// </summary>
    /// <param name="holdPosition">The Transform in the player's hand to parent to.</param>
    public void PickupItem(Transform holdPosition)
    {
        // 1. Physics/Colliders setup for holding
        if (rb != null) rb.isKinematic = true;  // Disable physics simulation
        if (col != null) col.enabled = false;  // Disable collider to prevent self-collision

        // 2. Hierarchy/Position setup
        transform.SetParent(holdPosition);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Handles the item being released or thrown, restoring physics and detaching.
    /// </summary>
    /// <param name="releaseForce">The force vector to apply (Vector3.zero for a drop).</param>
    public void ReleaseItem(Vector3 releaseForce)
    {
        // 1. Hierarchy detachment
        transform.SetParent(null);

        // 2. Physics/Colliders setup for dropping
        if (col != null) col.enabled = true; 
        
        if (rb != null) 
        {
            rb.isKinematic = false; // Restore physics
            
            // Apply force if throwing
            if (releaseForce != Vector3.zero)
            {
                rb.AddForce(releaseForce, ForceMode.Impulse);
            }
            else
            {
                 // Add a small upward impulse for a clean drop
                 rb.AddForce(Vector3.up * 1f, ForceMode.Impulse);
            }
        }
    }
}