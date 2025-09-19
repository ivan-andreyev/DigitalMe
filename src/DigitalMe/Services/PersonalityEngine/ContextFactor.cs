namespace DigitalMe.Services.PersonalityEngine;

/// <summary>
/// Фактор контекста с его влиянием.
/// </summary>
public class ContextFactor
{
    /// <summary>
    /// Название фактора контекста.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Значение фактора контекста.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Уровень влияния фактора на поведение.
    /// </summary>
    public FactorImpact Impact { get; set; }

    /// <summary>
    /// Подробное описание фактора и его влияния.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}