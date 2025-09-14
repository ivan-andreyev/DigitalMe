namespace DigitalMe.Services.PersonalityEngine;

/// <summary>
/// Уровень влияния фактора контекста.
/// </summary>
public enum FactorImpact
{
    /// <summary>
    /// Низкое влияние на адаптацию поведения.
    /// </summary>
    Low,

    /// <summary>
    /// Умеренное влияние на адаптацию поведения.
    /// </summary>
    Medium,

    /// <summary>
    /// Высокое влияние на адаптацию поведения.
    /// </summary>
    High,

    /// <summary>
    /// Критическое влияние, требующее значительной адаптации поведения.
    /// </summary>
    Critical
}