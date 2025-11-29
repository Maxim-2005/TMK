using UnityEngine;
using System.Linq;
using System.Collections.Generic;

// Скрипт, управляющий логикой подбора, удержания, бросания и роняния предметов игроком.
// Также содержит публичный метод для взаимодействия с Container.cs.
public class PlayerPickup : MonoBehaviour
{
    // === 1. НАСТРОЙКИ ===
    [Tooltip("Тег, который должен быть у подбираемых предметов.")]
    public string targetTag = "Item";
    [Tooltip("Точка в пространстве, где будет удерживаться предмет (должна быть дочерним объектом игрока).")]
    public Transform holdPosition;
    [Tooltip("Сила, с которой предмет будет брошен при нажатии G.")]
    public float throwForce = 500f;
    [Tooltip("Максимальное расстояние, на котором игрок может подобрать предмет.")]
    public float pickupRange = 2f;
    
    // === 2. СОСТОЯНИЕ И ССЫЛКИ ===
    
    // Ссылка на компонент ObjectHighlighter для управления подсветкой доступных предметов.
    private ObjectHighlighter highlighter;

    // Публичное свойство, возвращающее текущий удерживаемый объект.
    public GameObject HeldObject { get; private set; }
    // Последний предмет, который был подсвечен (кандидат на подбор).
    private GameObject lastHighlightedItem;
    
    // Счетчик, используемый для кратковременной блокировки взаимодействия с контейнером 
    // сразу после того, как игрок бросил или уронил предмет.
    private int dropFrameCounter = 0; 
    // Публичное свойство для проверки из Container.cs (true, если только что уронил).
    public bool HasRecentlyDropped => dropFrameCounter > 0; 

    void Start()
    {
        // Получаем ссылку на компонент подсветки
        highlighter = GetComponent<ObjectHighlighter>();
        if (highlighter == null)
        {
            Debug.LogError("PlayerPickup требует компонент ObjectHighlighter на том же GameObject!");
        }
    }

    // === 3. ОСНОВНОЙ ЦИКЛ ===

    void Update()
    {
        // Уменьшаем счетчик блокировки роняния на 1 кадр, пока он не достигнет 0.
        if (dropFrameCounter > 0)
        {
            dropFrameCounter--;
        }

        // Поиск и подсветка ближайшего подбираемого предмета.
        HighlightNearestItem();
        // Обработка пользовательского ввода (E и G).
        HandleInput();

        // Фиксация позиции предмета в руке: удерживаемый объект перемещается в holdPosition каждый кадр.
        if (HeldObject != null)
            HeldObject.transform.position = holdPosition.position;
    }
    
    // Вынесение обработки ввода в отдельный метод для чистоты Update
    private void HandleInput()
    {
        // Логика нажатия клавиши 'E' (Подобрать / Сбросить)
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (HeldObject == null)
                TryPickupNearestItem(); // Пытаемся подобрать, если руки пусты
            else
            {
                // Роняем предмет, если руки заняты
                DropItem(); 
                // Устанавливаем блокировку на 1 кадр, чтобы предотвратить немедленное взаимодействие с контейнерами.
                dropFrameCounter = 1; 
            }
        }

        // Логика нажатия клавиши 'G' (Бросить)
        if (HeldObject != null && Input.GetKeyDown(KeyCode.G))
            ThrowItem(); 
    }

    // === 4. ЛОГИКА ПОИСКА И ПОДСВЕТКИ ===

    void HighlightNearestItem()
    {
        // Если что-то держим, сбрасываем любую подсветку и выходим.
        if (HeldObject != null)
        {
            if (lastHighlightedItem != null)
                highlighter.ResetHighlight(lastHighlightedItem);
            return;
        }

        // Поиск ближайшего и доступного предмета в радиусе pickupRange
        GameObject nearestItem = FindNearestAvailableItem();

        // Обновляем подсветку, только если кандидат на подбор изменился
        if (lastHighlightedItem != nearestItem)
        {
            // Сброс подсветки старого предмета
            if (lastHighlightedItem != null)
                highlighter.ResetHighlight(lastHighlightedItem);

            // Подсветка нового предмета
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
                // 2. Не находится внутри контейнера (т.е. не привязан как дочерний элемент)
                !IsItemStored(item) &&
                // 3. Имеет необходимый компонент ItemPickup
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
        if (lastHighlightedItem == null) return;
        
        // Повторная проверка на случай, если предмет успел быть помещен в контейнер.
        if (IsItemStored(lastHighlightedItem)) 
        {
             highlighter.ResetHighlight(lastHighlightedItem);
             lastHighlightedItem = null;
             return;
        }

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
    /// Используется только для ЗАБИРАНИЯ предмета из контейнера (вызывается из Container.cs).
    /// </summary>
    /// <param name="item">Предмет, который нужно дать игроку.</param>
    /// <returns>True, если предмет был успешно принят (руки были пусты).</returns>
    public bool TryGiveItem(GameObject item)
    {
        // Не можем принять предмет, если что-то уже держим.
        if (HeldObject != null)
        {
            return false;
        }

        ItemPickup itemPickup = item.GetComponent<ItemPickup>();
        if (itemPickup == null) return false;

        // Делегируем привязку самому предмету
        itemPickup.PickupItem(holdPosition);

        HeldObject = item;
        
        return true;
    }
    
    // === 6. ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===

    /// <summary>
    /// Проверяет, находится ли предмет в данный момент внутри контейнера (является ли его дочерним элементом).
    /// Используется для исключения уже "хранящихся" предметов из подбора.
    /// </summary>
    private bool IsItemStored(GameObject item)
    {
        if (item.transform.parent != null)
        {
            // Проверяем, что родительский объект имеет компонент Container (или является его дочерним)
            // И исключаем нашу собственную holdPosition (чтобы не блокировать HeldObject)
            if (item.transform.parent.GetComponentInParent<Container>() != null && item.transform.parent != holdPosition)
            {
                return true;
            }
        }
        return false;
    }

    // Этот метод больше не используется контейнером для кладения! 
    // Оставлен для совместимости, но логика перенесена в DropItem.
    public GameObject TakeHeldItem()
    {
        if (HeldObject == null) return null;
        
        // Просто роняем предмет (поскольку Container.cs теперь сам уничтожает предмет при внесении)
        DropItem();
        return null; 
    }

    // Отображает радиус подбора в редакторе Unity (только при выбранном объекте).
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}