using UnityEngine;

/// <summary>
/// Универсальный скрипт для объектов, которые можно добывать/разрушать (деревья, камни и т.д.).
/// </summary>
public class ResourceProcessor : MonoBehaviour
{
    [Tooltip("Health of the object.")]
    public int health = 3;

    [Tooltip("The type of tool required to efficiently harvest this resource (e.g., Axe for wood, Pickaxe for rock).")]
    // NEW: Определяет, какой инструмент нужен
    public ItemType requiredToolType;

    [Tooltip("The Prefab to spawn when the object is destroyed (e.g., a stump, or resource items).")]
    public GameObject replacementPrefab;

    /// <summary>
    /// Called by PlayerToolController when the object is hit by a tool.
    /// </summary>
    /// <param name="toolType">The type of tool used (from ItemType enum).</param>
    public void ProcessHit(ItemType toolType)
    {
        // 1. Проверяем, был ли использован правильный инструмент
        if (toolType == requiredToolType)
        {
            health--;
            Debug.Log($"Объект {gameObject.name} (Требуется: {requiredToolType}) был ударен {toolType}. Осталось здоровья: {health}");

            if (health <= 0)
            {
                DestroyResource();
            }
        }
        else
        {
            // КОММЕНТАРИЙ: Можно добавить логику, что неправильный инструмент наносит меньше урона.
            Debug.LogWarning($"Объект {gameObject.name} ударен неправильным инструментом: {toolType}. Требуется: {requiredToolType}. Урон не нанесен или нанесен минимально.");
        }
    }
    
    private void DestroyResource()
    {
        Debug.Log($"Объект {gameObject.name} разрушен!");
        
        // --- LOGIC: SPAWN REPLACEMENT ---
        if (replacementPrefab != null)
        {
            // Spawn the replacement prefab at the current object's position and rotation
            Instantiate(replacementPrefab, transform.position, transform.rotation);
        }
        
        // Destroy the original object
        Destroy(gameObject);
    }
}