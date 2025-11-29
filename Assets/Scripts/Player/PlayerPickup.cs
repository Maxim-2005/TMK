using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Game.Items;

// Интерфейс для гибкого приема предметов (от контейнеров или других источников).
public interface IItemTaker
{
    bool TryGiveItem(GameObject item);
}

// Скрипт, управляющий логикой подбора, удержания, бросания и роняния предметов игроком.
public class PlayerPickup : MonoBehaviour, IItemTaker 
{
    // === 1. НАСТРОЙКИ ===
    [Tooltip("Тег, который должен быть у подбираемых предметов.")]
    public string targetTag = "Item";
    [Tooltip("Точка в пространстве, где будет удерживаться предмет (должна быть дочерним объектом игрока).")]
    public Transform holdPosition;
    
    [SerializeField]
    [Tooltip("Сила, с которой предмет будет брошен при нажатии G.")]
    private float throwForce = 10f; 
    
    [Tooltip("Максимальное расстояние, на котором игрок может подобрать предмет.")]
    public float pickupRange = 2f;
    
    // === 2. СОСТОЯНИЕ И ССЫЛКИ ===
    
    private ObjectHighlighter highlighter;

    // Публичное свойство, возвращающее текущий удерживаемый объект.
    public GameObject HeldObject { get; private set; }
    // Последний предмет, который был подсвечен (кандидат на подбор).
    private GameObject lastHighlightedItem;
    
    private int dropFrameCounter = 0; 
    // Публичное свойство для проверки из Container.cs (true, если только что уронил).
    public bool HasRecentlyDropped => dropFrameCounter > 0; 

    void Start()
    {
        // Получаем ссылку на компонент подсветки
        highlighter = GetComponent<ObjectHighlighter>();
        if (highlighter == null)
        {
            Debug.LogError("PlayerPickup requires ObjectHighlighter component on the same GameObject!");
        }
    }

    // === 3. ОСНОВНОЙ ЦИКЛ ===

    void Update()
    {
        // Счетчик кадров для предотвращения двойного действия 'E' (Drop Item + Withdraw from Container)
        if (dropFrameCounter > 0)
        {
            dropFrameCounter--;
        }

        HighlightNearestItem();
        HandleInput();

        // Удерживаемый предмет всегда следует за точкой удержания (Hold Position)
        if (HeldObject != null)
            HeldObject.transform.position = holdPosition.position;
    }
    
    private void HandleInput()
    {
        // Логика нажатия клавиши 'E' (Подобрать / Сбросить)
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (HeldObject == null)
            {
                // Если ничего не держим, пытаемся подобрать ближайший предмет
                TryPickupNearestItem(); 
            }
            else
            {
                // Если держим, сбрасываем предмет.
                DropItem(); 
                // Активируем счетчик, чтобы Container.cs мог игнорировать этот кадр
                dropFrameCounter = 2; // Увеличено до 2 кадров для надежности
            }
        }

        // Логика нажатия клавиши 'G' (Бросить)
        if (HeldObject != null && Input.GetKeyDown(KeyCode.G))
            ThrowItem(); 
    }

    // === 4. ЛОГИКА ПОИСКА И ПОДСВЕТКИ ===

    void HighlightNearestItem()
    {
        // Не подсвечиваем предметы, если игрок уже что-то держит
        if (HeldObject != null)
        {
            if (lastHighlightedItem != null)
                highlighter.ResetHighlight(lastHighlightedItem);
            return;
        }

        GameObject nearestItem = FindNearestAvailableItem();

        if (lastHighlightedItem != nearestItem)
        {
            // Сбрасываем старую подсветку
            if (lastHighlightedItem != null)
                highlighter.ResetHighlight(lastHighlightedItem);

            // Активируем новую подсветку
            if (nearestItem != null)
                highlighter.HighlightObject(nearestItem); 
            
            lastHighlightedItem = nearestItem;
        }
    }
    
    private GameObject FindNearestAvailableItem()
    {
        // Находим все объекты с целевым тегом.
        GameObject[] items = GameObject.FindGameObjectsWithTag(targetTag)
            .Where(item =>
                // 1. В пределах радиуса подбора
                Vector3.Distance(transform.position, item.transform.position) <= pickupRange &&
                // 2. Имеет необходимый компонент ItemPickup (это гарантирует, что это подбираемый предмет)
                item.GetComponent<ItemPickup>() != null) 
            .ToArray();

        if (items.Length > 0)
        {
            // Выбираем ближайший предмет, сортируя по расстоянию.
            return items
                .OrderBy(item => Vector3.Distance(transform.position, item.transform.position))
                .First();
        }
        return null;
    }

    // === 5. ЛОГИКА ДЕЙСТВИЙ (PUBLIC & PRIVATE) ===

    void TryPickupNearestItem()
    {
        // Пытаемся подобрать предмет, который находится в поле зрения и подсвечен
        if (lastHighlightedItem == null) return;
        
        ItemPickup item = lastHighlightedItem.GetComponent<ItemPickup>();
        if (item == null) return; 

        // 1. Сбрасываем подсветку
        highlighter.ResetHighlight(lastHighlightedItem); 

        // 2. Передаем управление самому предмету, чтобы он привязался к holdPosition.
        item.PickupItem(holdPosition);

        // 3. Обновляем состояние игрока
        HeldObject = lastHighlightedItem;
        lastHighlightedItem = null;
    }

    // Метод роняния (сброса) предмета, отключает привязку и возвращает физику.
    public void DropItem()
    {
        if (HeldObject == null) return; 

        ItemPickup item = HeldObject.GetComponent<ItemPickup>();
        if (item == null) return;

        // Делегируем сброс предмету, передавая нулевую силу (просто роняем вниз)
        item.ReleaseItem(Vector3.zero);

        // Обновляем состояние игрока
        HeldObject = null;
    }

    // Метод бросания предмета (с силой)
    void ThrowItem()
    {
        if (HeldObject == null) return; 
        
        ItemPickup item = HeldObject.GetComponent<ItemPickup>();
        if (item == null) return;

        // Рассчитываем силу броска в направлении, куда смотрит игрок.
        Vector3 force = transform.forward * throwForce;
        // Делегируем бросок предмету
        item.ReleaseItem(force);
            
        // Обновляем состояние игрока
        HeldObject = null;
    }

    /// <summary>
    /// Реализация IItemTaker. Используется для ЗАБИРАНИЯ предмета из контейнера (вызывается из Container.cs).
    /// </summary>
    public bool TryGiveItem(GameObject item)
    {
        // 1. Не можем принять предмет, если что-то уже держим.
        if (HeldObject != null)
        {
            return false;
        }

        ItemPickup itemPickup = item.GetComponent<ItemPickup>();
        if (itemPickup == null) 
        {
            Debug.LogError($"Item '{item.name}' does not have ItemPickup component, transfer is impossible.");
            return false;
        }

        // 2. Делегируем привязку самому предмету
        itemPickup.PickupItem(holdPosition);

        // 3. Обновляем состояние игрока
        HeldObject = item;
        
        return true;
    }
    
    // === 6. ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===

    // Отображает радиус подбора в редакторе Unity.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}