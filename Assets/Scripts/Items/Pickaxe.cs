using UnityEngine;

/// <summary>
/// Marker component for an Axe item. Defines the tool type for resource interaction.
/// </summary>
public class Pickaxe : MonoBehaviour
{
    // MUST BE ItemType (ENUM) to match LogProcessor's method signature
    public ItemType toolType = ItemType.Pickaxe;
    
    // Future fields like damage, speed, etc. can be added here.
}