using UnityEngine;

/// <summary>
/// Интерфейс для всех собираемых ресурсов (бревно, камень, куст).
/// Позволяет PlayerToolController универсально вызвать метод обработки удара.
/// </summary>
public interface IHarvestable
{
    /// <summary>
    /// Обрабатывает удар, нанесенный инструментом.
    /// </summary>
    /// <param name="toolType">Тип инструмента, который нанес удар (например, "Axe").</param>
    void ProcessHit(string toolType);
}