using DigitalMe.Data.Entities;
using DigitalMe.Services.Strategies;

namespace DigitalMe.Services.PersonalityEngine;

/// <summary>
/// Интерфейс для анализа уверенности на основе экспертизы в предметной области.
/// Отвечает только за расчет настроек уверенности.
/// </summary>
public interface IExpertiseConfidenceAnalyzer
{
    /// <summary>
    /// Настраивает уверенность в ответах на основе экспертизы в предметной области.
    /// </summary>
    /// <param name="personality">Профиль личности</param>
    /// <param name="domainType">Тип предметной области</param>
    /// <param name="taskComplexity">Сложность задачи (1-10)</param>
    /// <returns>Настройки уверенности для данной области</returns>
    ExpertiseConfidenceAdjustment AnalyzeExpertiseConfidence(PersonalityProfile personality, DomainType domainType, int taskComplexity);

    /// <summary>
    /// Валидирует параметры сложности задачи.
    /// </summary>
    /// <param name="taskComplexity">Сложность задачи для валидации</param>
    /// <returns>Валидированное значение сложности</returns>
    int ValidateTaskComplexity(int taskComplexity);

    /// <summary>
    /// Проверяет, является ли домен основной областью экспертизы для данной персоналии.
    /// </summary>
    /// <param name="personality">Профиль личности</param>
    /// <param name="domainType">Тип предметной области</param>
    /// <returns>True, если это основная область экспертизы</returns>
    bool IsCoreDomain(PersonalityProfile personality, DomainType domainType);

    /// <summary>
    /// Проверяет, является ли домен слабой стороной для данной персоналии.
    /// </summary>
    /// <param name="personality">Профиль личности</param>
    /// <param name="domainType">Тип предметной области</param>
    /// <returns>True, если это слабая сторона</returns>
    bool IsWeaknessDomain(PersonalityProfile personality, DomainType domainType);
}