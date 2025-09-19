using System.ComponentModel.DataAnnotations;

namespace DigitalMe.Services.PersonalityEngine;

/// <summary>
/// Анализ временных аспектов контекста.
/// </summary>
public class TemporalContextAnalysis
{
    /// <summary>
    /// Время проведения анализа.
    /// </summary>
    public DateTime TimeOfDay { get; set; }

    /// <summary>
    /// Категория времени (Morning, Afternoon, Evening, Late Hours).
    /// </summary>
    public string TimeCategory { get; set; } = string.Empty;

    /// <summary>
    /// Указывает, является ли время рабочими часами.
    /// </summary>
    public bool WorkingHours { get; set; }

    /// <summary>
    /// Ожидаемый уровень энергии в данное время (0.0-1.0).
    /// </summary>
    public double EnergyLevel { get; set; }

    /// <summary>
    /// Ожидаемый уровень концентрации внимания (0.0-1.0).
    /// </summary>
    public double AttentionLevel { get; set; }

    /// <summary>
    /// Список рекомендуемых адаптаций для данного времени.
    /// </summary>
    public List<string> TemporalAdaptations { get; set; } = new();

    /// <summary>
    /// Потенциальные вызовы/трудности, связанные со временем.
    /// </summary>
    public List<string> TimeBasedChallenges { get; set; } = new();

    /// <summary>
    /// Рекомендуемый темп общения/работы.
    /// </summary>
    public string RecommendedPacing { get; set; } = string.Empty;
}