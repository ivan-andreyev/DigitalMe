using DigitalMe.Data.Entities;
using DigitalMe.Services.Strategies;

namespace DigitalMe.Services.PersonalityEngine;

/// <summary>
/// Интерфейс для анализа оптимального стиля коммуникации.
/// Отвечает только за определение стиля коммуникации для контекста.
/// </summary>
public interface ICommunicationStyleAnalyzer
{
    /// <summary>
    /// Определяет оптимальный стиль коммуникации для текущего контекста.
    /// </summary>
    /// <param name="personality">Профиль личности</param>
    /// <param name="context">Контекст взаимодействия</param>
    /// <returns>Рекомендуемый стиль коммуникации</returns>
    ContextualCommunicationStyle DetermineOptimalCommunicationStyle(PersonalityProfile personality, SituationalContext context);

    /// <summary>
    /// Анализирует требования контекста для выбора стиля коммуникации.
    /// </summary>
    /// <param name="context">Ситуационный контекст</param>
    /// <returns>Анализ требований контекста</returns>
    CommunicationContextAnalysis AnalyzeCommunicationContext(SituationalContext context);

    /// <summary>
    /// Определяет уровень формальности для данного контекста.
    /// </summary>
    /// <param name="personality">Профиль личности</param>
    /// <param name="context">Ситуационный контекст</param>
    /// <returns>Рекомендуемый уровень формальности (0.0-1.0)</returns>
    double DetermineFormalityLevel(PersonalityProfile personality, SituationalContext context);

    /// <summary>
    /// Определяет рекомендуемый тон общения для контекста.
    /// </summary>
    /// <param name="personality">Профиль личности</param>
    /// <param name="context">Ситуационный контекст</param>
    /// <returns>Описание рекомендуемого тона</returns>
    string DetermineRecommendedTone(PersonalityProfile personality, SituationalContext context);
}