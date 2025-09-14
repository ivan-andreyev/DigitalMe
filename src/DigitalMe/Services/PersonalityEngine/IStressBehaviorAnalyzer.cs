using DigitalMe.Data.Entities;
using DigitalMe.Services.Strategies;

namespace DigitalMe.Services.PersonalityEngine;

/// <summary>
/// Интерфейс для анализа стрессовых модификаций поведения.
/// Отвечает только за расчет изменений поведения под стрессом и временным давлением.
/// </summary>
public interface IStressBehaviorAnalyzer
{
    /// <summary>
    /// Модифицирует поведенческие паттерны на основе уровня стресса и временного давления.
    /// </summary>
    /// <param name="personality">Профиль личности для анализа</param>
    /// <param name="stressLevel">Уровень стресса (0.0-1.0)</param>
    /// <param name="timePressure">Уровень временного давления (0.0-1.0)</param>
    /// <returns>Модифицированные паттерны поведения</returns>
    StressBehaviorModifications AnalyzeStressModifications(PersonalityProfile personality, double stressLevel, double timePressure);

    /// <summary>
    /// Валидирует входные параметры стресса и временного давления.
    /// </summary>
    /// <param name="stressLevel">Уровень стресса для валидации</param>
    /// <param name="timePressure">Уровень временного давления для валидации</param>
    /// <returns>Tuple с валидированными значениями</returns>
    (double ValidatedStress, double ValidatedTimePressure) ValidateStressParameters(double stressLevel, double timePressure);
}