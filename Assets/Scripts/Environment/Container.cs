using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using TMPro; 
using Game.Items;

/// <summary>
/// Контейнер, который принимает брошенные предметы и хранит их. 
/// Он может работать как сборочная станция (по достижении рецепта) или 
/// как разрушаемый ящик, выбрасывающий все накопленные ресурсы.
/// </summary>
public class Container : MonoBehaviour
{
    [System.Serializable]
    public class AcceptedResource
    {
        [Tooltip("Точное имя типа ресурса из ItemPickup.ItemTypeName (например, 'Stone').")] 
        public string itemTypeName;
        [Tooltip("Количество этого ресурса, требуемое для завершения рецепта (0, если рецепта нет).")]
        public int requiredAmount = 0; 
        [Tooltip("Префаб, который нужно использовать для создания визуального объекта при выбросе.")]
        public GameObject visualPrefab;
    }

    [Header("Настройки Ресурсов")]
    [Tooltip("Список типов ресурсов, которые этот контейнер принимает, и их префабы для выброса.")]
    public List<AcceptedResource> acceptedResourceTypes = new List<AcceptedResource>
    {
        new AcceptedResource { itemTypeName = "Stone", requiredAmount = 5, visualPrefab = null },
    };

    [Header("Настройки Рецепта")]
    [Tooltip("Префаб, который появится на месте контейнера после выполнения рецепта.")]
    public GameObject craftedPrefab;

    [Header("Настройки Взаимодействия")]
    [Tooltip("Сила, с которой предметы будут выбрасываться при опустошении контейнера.")]
    public float ejectionForce = 10f;
    [Tooltip("Максимальное расстояние, на котором можно инициировать выброс (для Trigger Collider).")]
    public float interactionDistance = 3f;

    // Параметры Suck-in (всасывания) удалены, так как функционал больше не используется.
    // [Header("Настройки Поглощения (Suck-in)")]
    // public float absorptionSpeed = 10f;
    // public float depositDistance = 0.5f; 

    private Dictionary<string, int> currentStorage = new Dictionary<string, int>();
    private Dictionary<string, GameObject> prefabLookup = new Dictionary<string, GameObject>();

    private PlayerPickup playerController; 
    private ObjectHighlighter highlighter; 
    
    private bool playerIsInsideTrigger = false; 

    void Start()
    {
        foreach (var res in acceptedResourceTypes)
        {
            if (!string.IsNullOrEmpty(res.itemTypeName) && res.visualPrefab != null)
            {
                currentStorage[res.itemTypeName] = 0; 
                prefabLookup[res.itemTypeName] = res.visualPrefab;
            }
            else
            {
                Debug.LogWarning($"Container '{gameObject.name}' has an unconfigured resource type or missing visual prefab in the Inspector. Check itemTypeName and visualPrefab.");
            }
        }

        playerController = FindFirstObjectByType<PlayerPickup>();
        if (playerController != null)
        {
            highlighter = playerController.GetComponent<ObjectHighlighter>();
        }

        if (playerController == null)
        {
            Debug.LogError("CRITICAL ERROR: PlayerPickup component not found in scene!");
        }
    }

    void Update()
    {
        if (playerController == null || !playerIsInsideTrigger)
        {
             return;
        }

        bool containerHasResources = currentStorage.Any(kvp => kvp.Value > 0);
        bool playerHasEmptyHands = playerController.HeldObject == null;
        bool recipeComplete = CheckRecipeCompletion();
        bool playerCanDeposit = IsPlayerHoldingAcceptedItem();

        if (highlighter != null)
        {
            bool shouldHighlight = recipeComplete || playerCanDeposit || (containerHasResources && playerHasEmptyHands);

            if (shouldHighlight)
            {
                highlighter.HighlightObject(gameObject);
            }
            else
            {
                highlighter.ResetHighlight(gameObject);
            }
        }

        if (Input.GetKeyDown(KeyCode.E) && playerIsInsideTrigger)
        {
            if (recipeComplete)
            {
                HandleCraftingCompletion();
            }
            else if (containerHasResources && playerHasEmptyHands)
            {
                EjectAllStoredResources();
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Проверяет, держит ли игрок предмет, который контейнер принимает для депозита.
    /// </summary>
    private bool IsPlayerHoldingAcceptedItem()
    {
        if (playerController == null || playerController.HeldObject == null)
        {
            return false;
        }

        if (playerController.HeldObject.TryGetComponent(out ItemPickup heldItem))
        {
            return currentStorage.ContainsKey(heldItem.ItemTypeName);
        }

        return false;
    }
    
    /// <summary>
    /// Попытка поместить предмет в хранилище контейнера.
    /// </summary>
    private bool TryDepositItem(ItemPickup itemPickup)
    {
        string itemType = itemPickup.ItemTypeName;
        
        if (currentStorage.ContainsKey(itemType))
        {
            currentStorage[itemType]++;
            Destroy(itemPickup.gameObject);
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Проверяет, достигнуто ли требуемое количество каждого ресурса.
    /// </summary>
    bool CheckRecipeCompletion()
    {
        if (craftedPrefab == null) return false;

        foreach (var required in acceptedResourceTypes)
        {
            if (required.requiredAmount <= 0) continue; 

            if (!currentStorage.ContainsKey(required.itemTypeName) || 
                currentStorage[required.itemTypeName] < required.requiredAmount)
            {
                return false;
            }
        }
        return true; 
    }

    /// <summary>
    /// Уничтожает контейнер и создает готовый предмет (крафтинг).
    /// </summary>
    void HandleCraftingCompletion()
    {
        if (highlighter != null)
        {
            highlighter.ResetHighlight(gameObject);
        }
        
        if (craftedPrefab == null)
        {
            Debug.LogError($"[CONTAINER CRAFT] Cannot craft: 'Crafted Prefab' is not set on container '{gameObject.name}'.");
            EjectAllStoredResources(); 
            Destroy(gameObject);
            return;
        }

        Instantiate(craftedPrefab, transform.position, transform.rotation);
        Destroy(gameObject);
    }

    /// <summary>
    /// Вызывается, когда объект с коллайдером входит в триггер.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerPickup>() == playerController)
        {
            playerIsInsideTrigger = true;
        }
    }

    /// <summary>
    /// Вызывается каждый кадр, пока объект находится в триггере. Используется для поглощения предметов.
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        // 1. Проверяем, что это подбираемый предмет и он нам нужен
        if (other.TryGetComponent(out ItemPickup itemPickup) && currentStorage.ContainsKey(itemPickup.ItemTypeName))
        {
            // 2. Если игрок держит этот предмет в руках, мы не должны его забирать
            if (playerController != null && playerController.HeldObject == itemPickup.gameObject) return;
            
            // ПРЕДМЕТ БРОШЕН И НАХОДИТСЯ ВНУТРИ ТРИГГЕРА. НЕМЕДЛЕННЫЙ ДЕПОЗИТ.
            TryDepositItem(itemPickup); 
        }
    }
    
    /// <summary>
    /// Вызывается, когда объект с коллайдером выходит из триггера.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerPickup>() == playerController)
        {
            playerIsInsideTrigger = false;
            highlighter?.ResetHighlight(gameObject); 
        }
    }
    
    /// <summary>
    /// Создает все предметы из хранилища, придает им силу и очищает хранилище. 
    /// </summary>
    void EjectAllStoredResources()
    {
        if (highlighter != null)
        {
            highlighter.ResetHighlight(gameObject);
        }
        
        Vector3 containerPosition = transform.position;
        Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        
        foreach (var storageEntry in currentStorage.ToList()) 
        {
            string itemType = storageEntry.Key;
            int amount = storageEntry.Value;
            
            if (amount <= 0 || !prefabLookup.ContainsKey(itemType)) continue;

            GameObject prefabToSpawn = prefabLookup[itemType];

            for (int i = 0; i < amount; i++)
            {
                GameObject spawnedItem = Instantiate(prefabToSpawn, containerPosition + Vector3.up * 0.5f, randomRotation);
                
                Rigidbody rb = spawnedItem.GetComponent<Rigidbody>();
                ItemPickup itemPickup = spawnedItem.GetComponent<ItemPickup>();

                if (rb != null)
                {
                    Vector3 randomDirection = Random.insideUnitCircle.normalized;
                    Vector3 forceDirection = (Vector3.up * 0.5f + new Vector3(randomDirection.x, 0, randomDirection.y) * 0.2f).normalized;
                    rb.AddForce(forceDirection * ejectionForce * Random.Range(0.8f, 1.2f), ForceMode.Impulse);
                }
                
                if (itemPickup != null)
                {
                    itemPickup.CanBePickedUp = false;
                    itemPickup.Invoke("EnablePickup", 0.5f); 
                }
            }
            
            currentStorage[itemType] = 0;
        }
    }
}