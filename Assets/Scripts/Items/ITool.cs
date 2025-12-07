using UnityEngine;

/// <summary>
/// Интерфейс для всех инструментов (топор, кирка, лопата и т.д.).
/// Позволяет PlayerToolController универсально определить тип инструмента.
/// </summary>
public interface ITool
{
    // Свойство для получения типа инструмента (например, "Axe", "Pickaxe").
    string ToolType { get; }
}