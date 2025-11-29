using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using TMPro;

public class Container : MonoBehaviour
{
    // Описание требуемого ресурса для рецепта.
    [System.Serializable]
    public class ResourceRequirement
    {
        [Tooltip("Название ресурса (должно совпадать с ItemTypeName в ItemPickup.cs)")]
        public string resourceName;
        [Tooltip("Требуемое количество для завершения.")]
        public int requiredAmount;
        [Tooltip("Префаб подбираемого ресурса (тот, что имеет компонент ItemPickup).")]
        public GameObject visualPrefab;
        
        public Vector3 defaultEulerRotation = Vector3.zero;
    }

    [Tooltip("Точка в ящике, куда перемещаются предметы при хранении. Не используется, но оставлена для удобства.")]
    public Transform storageSpawnPoint; 
    
    [Header("Настройки Рецепта/Завершения")]
    [Tooltip("Список требуемых ресурсов и их количество.")]
    public List<ResourceRequirement> requirements = new List<ResourceRequirement>
    {
        new ResourceRequirement { resourceName = "Stone", requiredAmount = 5 },
    };
    
    [Tooltip("Компонент TextMeshProUGUI для отображения счетчика.")]
    public TextMeshProUGUI counterText;
    [Tooltip("Объект, на который заменится контейнер после завершения.")]
    public GameObject replacementObjectPrefab; 

    // Хранилище: Dictionary<ТипРесурса, Количество>
    private Dictionary<string, int> storedResources = new Dictionary<string, int>();

    private PlayerPickup playerController; 
    private ObjectHighlighter highlighter;
    
    private bool playerIsInsideTrigger = false; 
    private bool isCompleted = false;

    void Start()
    {
        // Ищем контроллер игрока
        playerController = FindFirstObjectByType<PlayerPickup>();
        
        if (playerController != null)
        {
            // Берем компонент подсветки с игрока
            highlighter = playerController.GetComponent<ObjectHighlighter>();
        }

        if (playerController == null)
        {
            Debug.LogError("CRITICAL ERROR: PlayerPickup component not found in scene!");
        }
        
        if (highlighter == null)
        {
            Debug.LogWarning("ObjectHighlighter component not found on Player. Highlighting will not work.");
        }
        
        if (storageSpawnPoint == null)
        {
            storageSpawnPoint = transform.Find("SpawnPoint"); 

            if (storageSpawnPoint == null)
            {
                // Создаем точку по умолчанию
                GameObject defaultSpawn = new GameObject("SpawnPoint");
                defaultSpawn.transform.SetParent(transform);
                defaultSpawn.transform.localPosition = Vector3.up * 0.5f;
                storageSpawnPoint = defaultSpawn.transform;
            }
        }
        
        // Инициализируем хранилище
        foreach (var req in requirements)
        {
            storedResources[req.resourceName] = 0;
        }
        
        UpdateCounterDisplay();
    }

    void Update()
    {
        if (isCompleted)
        {
            highlighter?.ResetHighlight(gameObject);
            return;
        }

        if (playerController == null || storageSpawnPoint == null || !playerIsInsideTrigger)
        {
             return;
        }

        bool playerHasItem = playerController.HeldObject != null;
        bool containerHasAnyItem = storedResources.Values.Any(amount => amount > 0);
        
        // Логика подсветки
        if (highlighter != null)
        {
            // Подсвечиваем контейнер, если игрок в зоне и может взаимодействовать
            if (playerIsInsideTrigger && (playerHasItem || containerHasAnyItem))
            {
                highlighter.HighlightObject(gameObject);
            }
            else
            {
                highlighter.ResetHighlight(gameObject);
            }
        }

        // --- ЛОГИКА ИЗЪЯТИЯ (Кнопка E) ---
        // Берем предмет из ящика: Нажимаем E, если руки пусты и ящик не пуст.
        if (Input.GetKeyDown(KeyCode.E) && !playerHasItem && containerHasAnyItem && !playerController.HasRecentlyDropped)
        {
            WithdrawResource();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCompleted) return;

        if (other.GetComponent<PlayerPickup>() == playerController)
        {
            playerIsInsideTrigger = true;
        }
        
        ItemPickup itemComponent = other.GetComponent<ItemPickup>();
        
        if (other.CompareTag("Item") && itemComponent != null)
        {
            // Не кладем, если предмет в руке игрока (он должен сбросить его сам)
            if (playerController != null && other.gameObject == playerController.HeldObject) return;

            DepositByTrigger(other.gameObject, itemComponent.ItemTypeName);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerPickup>() == playerController)
        {
            playerIsInsideTrigger = false;
            
            // Сбрасываем подсветку контейнера при выходе
            highlighter?.ResetHighlight(gameObject);
        }
    }
    
    // Кладет предмет в контейнер, когда он попадает в триггер.
    void DepositByTrigger(GameObject itemToStore, string resourceTypeName)
    {
        string type = resourceTypeName;
        ResourceRequirement requirement = requirements.FirstOrDefault(req => req.resourceName == type);

        if (requirement == null)
        {
            Debug.LogWarning($"CONTAINER: Ресурс '{type}' не требуется.");
            return;
        }
        
        if (!storedResources.ContainsKey(type))
        {
             storedResources[type] = 0;
        }
        
        // Проверка лимита
        if (storedResources[type] >= requirement.requiredAmount)
        {
             Debug.Log($"CONTAINER: Ресурс '{type}' уже полон.");
             return;
        }
        
        storedResources[type]++;
        Destroy(itemToStore); 
        
        Debug.Log($"ПРЕДМЕТ ПОМЕЩЕН АВТОМАТИЧЕСКИ: '{type}'. Текущее количество: {storedResources[type]}");
        
        UpdateCounterDisplay();
        CheckCompletion();
    }
    
    // Извлекает один предмет из контейнера и дает его игроку.
    void WithdrawResource()
    {
        // 1. Ищем ресурс, который можно изъять
        ResourceRequirement resourceToWithdraw = requirements.FirstOrDefault(req => 
            storedResources.ContainsKey(req.resourceName) && storedResources[req.resourceName] > 0
        );

        if (resourceToWithdraw == null)
        {
            Debug.Log("CONTAINER: Нечего изымать.");
            return;
        }
        
        string type = resourceToWithdraw.resourceName;
        
        // 2. Уменьшаем счетчик
        storedResources[type]--;

        // 3. Создаем физический предмет
        Vector3 spawnPosition = storageSpawnPoint.position;
        GameObject spawnedItem = Instantiate(resourceToWithdraw.visualPrefab, spawnPosition, Quaternion.identity);

        // 4. Пытаемся дать предмет игроку
        if (playerController.TryGiveItem(spawnedItem))
        {
            Debug.Log($"ПРЕДМЕТ ИЗЪЯТ: '{type}'. Передан игроку.");
        }
        else
        {
             // Если руки игрока неожиданно полны
             Debug.LogError("CONTAINER: Не удалось передать предмет игроку (Руки неожиданно полны).");
             
             // Откат
             storedResources[type]++; 
             Destroy(spawnedItem);
        }
        
        // 5. Обновляем счетчик
        UpdateCounterDisplay();
    }
    
    // Обновляет отображение TextMeshProUGUI.
    void UpdateCounterDisplay()
    {
        if (counterText != null)
        {
            string display = "";
            foreach (var req in requirements)
            {
                int currentAmount = storedResources.ContainsKey(req.resourceName) ? storedResources[req.resourceName] : 0;
                display += $"{req.resourceName}: {currentAmount}/{req.requiredAmount}\n";
            }
            counterText.text = display.TrimEnd('\n'); 
        }
        else
        {
            Debug.LogWarning("Счетчик TextMeshProUGUI не назначен! Обновление дисплея не удалось.");
        }
    }

    // Проверяет, выполнены ли все требования рецепта.
    void CheckCompletion()
    {
        bool allRequirementsMet = true;
        foreach (var req in requirements)
        {
            int current = storedResources.ContainsKey(req.resourceName) ? storedResources[req.resourceName] : 0;
            
            if (current < req.requiredAmount)
            {
                allRequirementsMet = false;
                break;
            }
        }

        if (allRequirementsMet)
        {
            CompleteContainer();
        }
    }
    
    // Завершает контейнер: удаляет подсветку и заменяет объект.
    void CompleteContainer()
    {
        isCompleted = true;
        Debug.Log("КОНТЕЙНЕР ЗАПОЛНЕН! Выполнение завершения...");
        
        highlighter?.ResetHighlight(gameObject);

        // Обновляем счетчик в последний раз
        if (counterText != null)
        {
             counterText.text = "ЗАВЕРШЕНО!";
        }
        
        // Заменяем текущий объект
        if (replacementObjectPrefab != null)
        {
            Instantiate(replacementObjectPrefab, transform.position, transform.rotation);
        }
        
        // Уничтожаем текущий объект контейнера
        Destroy(gameObject);
    }
}