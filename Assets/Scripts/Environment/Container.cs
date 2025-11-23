using UnityEngine;
using System.Collections.Generic;
using System.Linq; 
using TMPro; // Обязательно добавьте этот using для работы с текстом UI

public class Container : MonoBehaviour
{
    // Точка, куда будут спавниться или перемещаться предметы при взятии из ящика
    [Tooltip("Точка в ящике, куда перемещаются предметы при хранении.")]
    public Transform storageSpawnPoint; 
    
    [Header("Настройки Счетчик/Завершение")]
    [Tooltip("Необходимое количество предметов для завершения.")]
    public int requiredItems = 5; 
    [Tooltip("Компонент TextMeshProUGUI для отображения счетчика (0/5).")]
    public TextMeshProUGUI counterText;
    [Tooltip("Объект, на который заменится контейнер после завершения (например, открытый ящик).")]
    public GameObject replacementObjectPrefab; 

    [Header("Настройки подсветки")]
    public Color highlightColor = Color.cyan; // Цвет, которым будет подсвечиваться контейнер
    public float highlightIntensity = 1.5f; // Яркость свечения

    // Хранилище: список предметов, которые находятся внутри ящика
    private List<GameObject> storedItems = new List<GameObject>();

    // Используем ссылку на компонент PlayerPickup
    private PlayerPickup playerController; 
    
    // Флаг, который показывает, находится ли игрок сейчас в области триггера
    private bool playerIsInsideTrigger = false; 
    private bool isCompleted = false; // Новый флаг для состояния завершения

    // Словарь для хранения оригинального материала
    private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerPickup>();

        if (playerController == null)
        {
            Debug.LogError("КРИТИЧЕСКАЯ ОШИБКА КОНТЕЙНЕРА: Компонент PlayerPickup не найден в сцене!");
        }
        
        if (storageSpawnPoint == null)
        {
            storageSpawnPoint = transform.Find("SpawnPoint"); 

            if (storageSpawnPoint == null)
            {
                Debug.LogError("КРИТИЧЕСКАЯ ОШИБКА КОНТЕЙНЕРА: Не задана точка storageSpawnPoint! Назначьте Transform в инспекторе или создайте дочерний объект 'SpawnPoint'.");
            }
        }
        
        // Проверяем наличие коллайдера-триггера
        if (GetComponent<Collider>() == null || !GetComponent<Collider>().isTrigger)
        {
            Debug.LogWarning("ПРЕДУПРЕЖДЕНИЕ: На объекте-контейнере нет коллайдера с установленной галочкой 'Is Trigger'. Триггерная система не будет работать!");
        }
        
        // Инициализируем отображение счетчика при старте
        UpdateCounterDisplay();
    }

    void Update()
    {
        // Прерываем работу, если контейнер уже завершен
        if (isCompleted)
        {
            // Сбрасываем подсветку, если вдруг она осталась
            ResetHighlight(this.gameObject);
            return;
        }

        // Прерываем работу, если нет критических ссылок или игрок не в зоне
        if (playerController == null || storageSpawnPoint == null || !playerIsInsideTrigger)
        {
             return;
        }

        // 1. Проверяем состояние предметов
        bool playerHasItem = playerController.HeldObject != null;
        bool containerHasItem = storedItems.Count > 0;
        
        // Подсветка активна, если игрок в зоне И (несет предмет ИЛИ ящик не пуст)
        if (playerHasItem || containerHasItem)
        {
            HighlightObject(this.gameObject);
        }
        else
        {
            ResetHighlight(this.gameObject);
        }

        // *** ЛОГИКА ИЗВЛЕЧЕНИЯ (Нажатие клавиши E) ***

        // Берем предмет из ящика: Нажимаем E, если руки пусты и ящик не пуст.
        // НОВАЯ ПРОВЕРКА: Игрок НЕ должен был только что бросить предмет (HasRecentlyDropped == false).
        if (Input.GetKeyDown(KeyCode.E) && !playerHasItem && containerHasItem && !playerController.HasRecentlyDropped)
        {
            WithdrawItem(); // Берем предмет
        }
    }

    // *** МЕТОДЫ ТРИГГЕРОВ ***

    private void OnTriggerEnter(Collider other)
    {
        // Если контейнер завершен, игнорируем любые входы
        if (isCompleted) return;

        // 1. Проверяем, вошел ли в триггер игрок (для подсветки)
        if (other.GetComponent<PlayerPickup>() == playerController)
        {
            playerIsInsideTrigger = true;
        }
        
        // 2. Проверяем, попал ли в триггер СБРОШЕННЫЙ ПРЕДМЕТ (для кладения)
        if (other.CompareTag("Item"))
        {
            DepositByTrigger(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Проверяем, вышел ли из триггера игрок
        if (other.GetComponent<PlayerPickup>() == playerController)
        {
            playerIsInsideTrigger = false;
            ResetHighlight(this.gameObject); // Сбрасываем подсветку
        }
    }
    
    /// <summary>
    /// Кладет предмет, который попал в триггер, в контейнер, и проверяет условие завершения.
    /// </summary>
    void DepositByTrigger(GameObject itemToStore)
    {
        if (itemToStore == null || isCompleted) return;
        
        // Проверка: предмет уже внутри? (Нужна для предотвращения многократного срабатывания)
        if (storedItems.Contains(itemToStore)) return;

        // Отключаем физику и коллайдер у захваченного предмета
        Rigidbody rb = itemToStore.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; 
        }
        
        Collider collider = itemToStore.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;
        
        // Размещаем предмет в точке хранения
        itemToStore.transform.SetParent(storageSpawnPoint);
        itemToStore.transform.localPosition = Vector3.zero;
        itemToStore.transform.localRotation = Quaternion.identity; 
        
        storedItems.Add(itemToStore);
        Debug.Log($"ПРЕДМЕТ ПОЛОЖЕН АВТОМАТИЧЕСКИ: '{itemToStore.name}' попал в триггер. Всего предметов: {storedItems.Count}");
        
        // Обновляем счетчик
        UpdateCounterDisplay();

        // *** ПРОВЕРКА УСЛОВИЯ ЗАВЕРШЕНИЯ ***
        if (storedItems.Count >= requiredItems)
        {
            CompleteContainer();
        }
    }

    /// <summary>
    /// Извлекает последний предмет из контейнера и передает его игроку.
    /// </summary>
    void WithdrawItem()
    {
        if (storedItems.Count == 0 || isCompleted) return; // Нельзя брать, если завершено
        
        int lastIndex = storedItems.Count - 1;
        GameObject itemToGive = storedItems[lastIndex];
        
        // Передаем предмет игроку
        if (playerController.TryGiveItem(itemToGive))
        {
            // Если игрок смог взять предмет (т.е. руки были пусты), удаляем из списка
            storedItems.RemoveAt(lastIndex); 
            Debug.Log($"ПРЕДМЕТ ВЗЯТ: '{itemToGive.name}' взят из ящика. Осталось предметов: {storedItems.Count}");
            
            // Обновляем счетчик
            UpdateCounterDisplay();
        }
        // else: Если руки заняты, предмет остается.
    }
    
    /// <summary>
    /// Обновляет текстовое отображение счетчика.
    /// </summary>
    void UpdateCounterDisplay()
    {
        if (counterText != null)
        {
            counterText.text = $"{storedItems.Count}/{requiredItems}";
        }
        else
        {
            // Предупреждение, чтобы напомнить пользователю, если он не назначил компонент
            Debug.LogWarning("Счетчик TextMeshProUGUI не назначен в Инспекторе! Пожалуйста, добавьте UI Text (TMP) и перетащите его в поле Counter Text.");
        }
    }
    
    /// <summary>
    /// Завершает работу контейнера: удаляет предметы и заменяет объект.
    /// </summary>
    void CompleteContainer()
    {
        isCompleted = true;
        Debug.Log("КОНТЕЙНЕР ЗАПОЛНЕН! Выполняю завершение...");
        
        // Сбрасываем подсветку ящика перед удалением
        ResetHighlight(this.gameObject);

        // 1. Удаляем все хранящиеся предметы
        foreach (GameObject item in storedItems)
        {
            // Удаляем сам игровой объект предмета
            Destroy(item);
        }
        storedItems.Clear(); // Очищаем список после уничтожения

        // Обновляем счетчик в последний раз
        if (counterText != null)
        {
             // Здесь можно написать что-то вроде "ГОТОВО"
             counterText.text = "ЗАВЕРШЕНО";
        }
        
        // 2. Заменяем текущий объект на другой
        if (replacementObjectPrefab != null)
        {
            // Создаем новый объект на месте старого контейнера
            Instantiate(replacementObjectPrefab, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogWarning("Replacement Object Prefab не назначен. Замена не выполнена. Контейнер будет просто удален.");
        }
        
        // 3. Удаляем текущий объект контейнера
        Destroy(gameObject);
    }

    // Методы подсветки
    // (Остаются без изменений)
    
    /// <summary>
    /// Подсвечивает указанный объект, используя эмиссию материала.
    /// </summary>
    void HighlightObject(GameObject item)
    {
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (!originalMaterials.ContainsKey(item))
            {
                // Клонируем материал, чтобы не менять оригинальный ассет
                originalMaterials[item] = renderer.material;
            }

            // Применяем подсвечивающий материал
            Material highlightMat = new Material(originalMaterials[item]);
            highlightMat.EnableKeyword("_EMISSION");
            highlightMat.SetColor("_EmissionColor", highlightColor * highlightIntensity);
            renderer.material = highlightMat;
        }
    }

    /// <summary>
    /// Сбрасывает подсветку объекта, возвращая ему оригинальный материал.
    /// </summary>
    void ResetHighlight(GameObject item)
    {
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null && originalMaterials.ContainsKey(item))
        {
            renderer.material = originalMaterials[item];
            originalMaterials.Remove(item);
        }
    }
}