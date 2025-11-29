using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Attaches to the Player and manages the visual highlight effect 
/// on other objects (Items, Containers) passed to its methods.
/// </summary>
public class ObjectHighlighter : MonoBehaviour
{
    [Header("Highlight Settings")]
    [Tooltip("Emission color for the highlight.")]
    public Color highlightColor = Color.cyan;
    [Tooltip("Intensity of the emission color.")]
    public float highlightIntensity = 1.5f;

    // Dictionary to store the original material for each Renderer we modify.
    // Key: Renderer of the highlighted object. Value: Original Material.
    private Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();

    /// <summary>
    /// Applies the highlight effect to the specified target object.
    /// </summary>
    /// <param name="target">The GameObject to highlight.</param>
    public void HighlightObject(GameObject target)
    {
        if (target == null) return;

        // Находим все Renderer на объекте и его дочерних элементах
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            // Пропускаем, если уже подсвечен
            if (originalMaterials.ContainsKey(renderer)) continue;

            // 1. Сохраняем оригинальный материал
            // Клонируем материал, чтобы избежать изменения оригинального ассета
            Material originalMat = renderer.material;
            originalMaterials[renderer] = originalMat;

            // 2. Создаем и применяем материал подсветки
            Material highlightMat = new Material(originalMat);
            
            // Включаем эмиссию
            highlightMat.EnableKeyword("_EMISSION");
            highlightMat.SetColor("_EmissionColor", highlightColor * highlightIntensity);
            
            renderer.material = highlightMat;
        }
    }

    /// <summary>
    /// Resets the highlight effect, returning the original material.
    /// </summary>
    /// <param name="target">The GameObject whose highlight should be reset.</param>
    public void ResetHighlight(GameObject target)
    {
        if (target == null) return;
        
        // Находим все Renderer, которые могут быть в словаре
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            if (originalMaterials.ContainsKey(renderer))
            {
                // Возвращаем оригинальный материал
                renderer.material = originalMaterials[renderer];
                originalMaterials.Remove(renderer);
            }
        }
    }

    void OnDestroy()
    {
        // Очистка всех материалов при уничтожении
        foreach (var pair in originalMaterials)
        {
            // Мы должны восстановить материал, прежде чем уничтожить его
            if (pair.Key != null && pair.Value != null)
            {
                pair.Key.material = pair.Value;
            }
        }
        originalMaterials.Clear();
    }
}