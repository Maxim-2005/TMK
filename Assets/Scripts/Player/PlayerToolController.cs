using UnityEngine;

public class PlayerToolController : MonoBehaviour
{
    // Component for managing held items
    PlayerPickup playerPickup;
    
    // Component for managing object highlights (from ObjectHighlighter.cs)
    private ObjectHighlighter objectHighlighter;
    
    // Tracks the object currently under the SphereCast to manage highlighting
    private GameObject currentTarget;
    // Tracks the last object that was highlighted to remove the highlight
    private GameObject lastHighlightedObject;
    
    [Header("Tool Hit Settings")]
    [Tooltip("Maximum distance for the SphereCast.")]
    public float maxHitDistance = 3f;

    [Tooltip("Radius of the SphereCast for hit detection (larger value means wider hit area).")]
    public float hitRadius = 0.5f;

    [Tooltip("Vertical offset from the player's center for the SphereCast origin.")]
    public float verticalOffset = 0.5f; 

    private float drawDuration = 0.1f; 

    void Start()
    {
        if (playerPickup == null) 
        {
            playerPickup = GetComponent<PlayerPickup>(); 
        }
        
        objectHighlighter = GetComponent<ObjectHighlighter>();
        if (objectHighlighter == null)
        {
            Debug.LogError("ObjectHighlighter component not found on the player! Highlighting will not work.");
        }
    }
    
    void Update()
    {
        // 1. Always check the area in front of the player and highlight potential targets.
        HandleHighlighting();
        
        // 2. Check for attack input (only proceed if holding a tool)
        if (Input.GetButtonDown("Fire1"))
        {
            HandleToolUse();
        }
    }
    
    /// <summary>
    /// Handles the continuous SphereCast to find and highlight potential targets.
    /// NEW LOGIC: Only highlights resources that match the currently held tool.
    /// </summary>
    private void HandleHighlighting()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * verticalOffset; 
        Vector3 rayDirection = transform.forward; 

        currentTarget = null;
        
        // --- 1. Определяем, какой инструмент держит игрок ---
        ItemType currentToolType = GetHeldToolType();
        
        // Execute SphereCast for continuous detection
        if (Physics.SphereCast(rayOrigin, hitRadius, rayDirection, out RaycastHit hit, maxHitDistance))
        {
            ResourceProcessor resourceProcessor = hit.collider.GetComponent<ResourceProcessor>();

            if (resourceProcessor != null)
            {
                // --- 2. Проверяем условие подсветки ---
                // Подсвечиваем только если игрок держит инструмент И этот инструмент подходит для ресурса.
                if (currentToolType != ItemType.Unknown && resourceProcessor.requiredToolType == currentToolType)
                {
                    currentTarget = hit.collider.gameObject;
                }
            }
            
            // Draw a yellow line to visualize the continuous SphereCast direction (only in Scene view)
            Debug.DrawRay(rayOrigin, rayDirection * hit.distance, Color.yellow); 
        }
        else
        {
            // Draw a white line to show the full range if nothing is hit
            Debug.DrawRay(rayOrigin, rayDirection * maxHitDistance, Color.white);
        }

        // --- Highlighting Logic (Remains the same) ---
        
        if (currentTarget != lastHighlightedObject)
        {
            if (lastHighlightedObject != null && objectHighlighter != null)
            {
                objectHighlighter.ResetHighlight(lastHighlightedObject);
            }

            if (currentTarget != null && objectHighlighter != null)
            {
                objectHighlighter.HighlightObject(currentTarget);
            }
            
            lastHighlightedObject = currentTarget;
        }
    }
    
    /// <summary>
    /// Handles the actual tool use (attack) upon input.
    /// </summary>
    private void HandleToolUse()
    {
        if (playerPickup == null || playerPickup.HeldObject == null) return;

        // Определяем тип инструмента (ItemType)
        ItemType toolType = GetHeldToolType();
        
        // Если игрок держит не инструмент, выходим.
        if (toolType == ItemType.Unknown) return; 

        // --- SphereCast Attack Logic ---
        
        Vector3 rayOrigin = transform.position + Vector3.up * verticalOffset; 
        Vector3 rayDirection = transform.forward; 

        if (Physics.SphereCast(rayOrigin, hitRadius, rayDirection, out RaycastHit hit, maxHitDistance))
        {
            // Проверяем, содержит ли пораженный объект ResourceProcessor
            ResourceProcessor resourceProcessor = hit.collider.GetComponent<ResourceProcessor>();

            if (resourceProcessor != null)
            {
                // Передаем определенный тип инструмента
                resourceProcessor.ProcessHit(toolType);

                Debug.Log($"Удар: {toolType} по объекту: {hit.collider.gameObject.name}");
                
                // Draw a green line to visualize a successful hit (brief moment)
                Debug.DrawLine(rayOrigin, hit.point, Color.green, drawDuration);
            }
        }
    }

    /// <summary>
    /// Determines the ItemType of the currently held tool (Axe or Pickaxe).
    /// </summary>
    private ItemType GetHeldToolType()
    {
        if (playerPickup == null || playerPickup.HeldObject == null)
        {
            return ItemType.Unknown;
        }

        // Пробуем получить Axe
        Axe axeTool = playerPickup.HeldObject.GetComponent<Axe>();
        if (axeTool != null)
        {
            return axeTool.toolType; // Возвращает ItemType.Axe
        }

        // Пробуем получить Pickaxe
        Pickaxe pickaxeTool = playerPickup.HeldObject.GetComponent<Pickaxe>();
        if (pickaxeTool != null)
        {
            return pickaxeTool.toolType; // Возвращает ItemType.Pickaxe
        }

        return ItemType.Unknown; // Ни топор, ни кирка
    }
}