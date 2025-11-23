using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class PlayerPickup : MonoBehaviour
{
    public string targetTag = "Item";
    public Transform holdPosition;
    public float throwForce = 500f;
    public float pickupRange = 2f;
    public Color highlightColor = Color.yellow;
    public float highlightIntensity = 1f;

    // Делаем эти поля публичными для доступа из других скриптов
    public GameObject HeldObject { get; private set; }
    public Rigidbody HeldObjectRb { get; private set; }

    // Словарь для хранения оригинальных материалов подсвеченных предметов
    private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();

    private GameObject lastHighlightedItem;

    // НОВОЕ: Счетчик для блокировки взаимодействия с контейнером сразу после броска
    private int dropFrameCounter = 0; 
    public bool HasRecentlyDropped => dropFrameCounter > 0; // Публичное свойство для Container.cs
    
    void Update()
    {
        // Уменьшаем счетчик каждый кадр. Это гарантирует блокировку только на 1 кадр.
        if (dropFrameCounter > 0)
        {
            dropFrameCounter--;
        }

        HighlightNearestItem();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (HeldObject == null)
                TryPickupNearestItem();
            else
            {
                // Роняем предмет, чтобы он упал в контейнер, если игрок над ним
                DropItem(); 
                // НОВОЕ: Устанавливаем счетчик, чтобы заблокировать Container.Update()
                dropFrameCounter = 1; 
            }
        }

        if (HeldObject != null && Input.GetKeyDown(KeyCode.G))
            ThrowItem(); // Бросаем предмет, если нажат G

        if (HeldObject != null)
            HeldObject.transform.position = holdPosition.position;
    }
    
    /// <summary>
    /// Проверяет, находится ли предмет в данный момент внутри контейнера (является ли его дочерним элементом).
    /// </summary>
    private bool IsItemStored(GameObject item)
    {
        // Если у предмета есть родитель, и этот родитель (или его предок) 
        // имеет компонент Container, значит, предмет хранится и его нельзя подбирать напрямую.
        if (item.transform.parent != null)
        {
            // GetComponentInParent ищет компонент вверх по иерархии родителей
            if (item.transform.parent.GetComponentInParent<Container>() != null)
            {
                return true;
            }
        }
        return false;
    }

    void HighlightNearestItem()
    {
        if (HeldObject != null)
        {
            if (lastHighlightedItem != null)
                ResetHighlight(lastHighlightedItem);
            return;
        }

        // Находим все предметы, помеченные тегом Item
        GameObject[] items = GameObject.FindGameObjectsWithTag(targetTag)
            .Where(item =>
                // 1. Находится в зоне подбора
                Vector3.Distance(transform.position, item.transform.position) <= pickupRange &&
                // 2. *** КРИТИЧЕСКОЕ ИЗМЕНЕНИЕ: Не хранится в контейнере ***
                !IsItemStored(item))
            .ToArray();

        GameObject nearestItem = null;
        if (items.Length > 0)
        {
            // Выбираем ближайший предмет
            nearestItem = items
                .OrderBy(item => Vector3.Distance(transform.position, item.transform.position))
                .First();
        }

        // Обновляем подсветку
        if (lastHighlightedItem != nearestItem)
        {
            if (lastHighlightedItem != null)
                ResetHighlight(lastHighlightedItem);

            if (nearestItem != null)
                HighlightItem(nearestItem);
            
            lastHighlightedItem = nearestItem;
        }
    }
    
    void TryPickupNearestItem()
    {
        if (lastHighlightedItem == null) return;
        
        // Дополнительная проверка на случай, если предмет только что положили
        if (IsItemStored(lastHighlightedItem)) 
        {
             ResetHighlight(lastHighlightedItem);
             lastHighlightedItem = null;
             return;
        }

        // 1. Сбрасываем подсветку
        ResetHighlight(lastHighlightedItem); 

        // 2. Получаем ссылки на предмет
        HeldObject = lastHighlightedItem;
        HeldObjectRb = HeldObject.GetComponent<Rigidbody>();
        
        // 3. Настраиваем физику и коллайдер для держания
        if (HeldObjectRb != null)
        {
            HeldObjectRb.isKinematic = true;
        }
        
        Collider collider = HeldObject.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;

        // 4. Привязываем к руке игрока
        HeldObject.transform.SetParent(holdPosition);
        HeldObject.transform.localPosition = Vector3.zero;
        HeldObject.transform.localRotation = Quaternion.identity;
        
        lastHighlightedItem = null;
    }

    // Метод роняния (сброса) предмета
    public void DropItem()
    {
        if (HeldObject == null) return; 

        if (HeldObjectRb != null)
            HeldObjectRb.isKinematic = false; // Включаем физику, чтобы предмет упал

        Collider collider = HeldObject.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = true;

        HeldObject.transform.SetParent(null);
        HeldObject = null;
        HeldObjectRb = null;
    }

    void ThrowItem()
    {
        if (HeldObject == null) return; 
        
        if (HeldObjectRb != null)
            HeldObjectRb.isKinematic = false;

        Collider collider = HeldObject.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = true;

        HeldObject.transform.SetParent(null);
        
        if (HeldObjectRb != null)
            HeldObjectRb.AddForce(transform.forward * throwForce);
            
        HeldObject = null;
        HeldObjectRb = null;
    }

    // Используется только для ЗАБИРАНИЯ предмета из контейнера
    public bool TryGiveItem(GameObject item)
    {
        if (HeldObject != null)
        {
            return false;
        }

        HeldObject = item;
        HeldObjectRb = item.GetComponent<Rigidbody>();

        if (HeldObjectRb != null)
        {
            HeldObjectRb.isKinematic = true;
        }
        
        Collider collider = item.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;
            
        HeldObject.transform.SetParent(holdPosition);
        HeldObject.transform.localPosition = Vector3.zero;
        HeldObject.transform.localRotation = Quaternion.identity;
        
        return true;
    }
    
    // Этот метод больше не используется контейнером для кладения!
    public GameObject TakeHeldItem()
    {
        // Логика перенесена в DropItem() выше.
        if (HeldObject == null) return null;
        
        DropItem();
        return null; // Возвращаем null, так как предмет теперь просто брошен
    }

    // Методы подсветки
    public void HighlightItem(GameObject item)
    {
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (!originalMaterials.ContainsKey(item))
            {
                originalMaterials[item] = renderer.material;
            }

            Material highlightMat = new Material(originalMaterials[item]);
            highlightMat.EnableKeyword("_EMISSION");
            highlightMat.SetColor("_EmissionColor", highlightColor * highlightIntensity);
            renderer.material = highlightMat;
        }
    }

    public void ResetHighlight(GameObject item)
    {
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null && originalMaterials.ContainsKey(item))
        {
            renderer.material = originalMaterials[item];
            originalMaterials.Remove(item);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}