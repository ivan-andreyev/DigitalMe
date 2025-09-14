using DigitalMe.Data.Entities;
using DigitalMe.Services.Strategies;

namespace DigitalMe.Services.PersonalityEngine;

/// <summary>
/// Интерфейс для анализа контекста и предоставления рекомендаций по адаптации поведения.
/// Отвечает только за анализ требований контекста.
/// </summary>
public interface IContextAnalyzer
{
    /// <summary>
    /// Анализирует контекст и предоставляет рекомендации по адаптации поведения.
    /// </summary>
    /// <param name="context">Ситуационный контекст</param>
    /// <returns>Рекомендации по адаптации</returns>
    ContextAnalysisResult AnalyzeContextRequirements(SituationalContext context);

    /// <summary>
    /// Определяет уровень сложности контекста.
    /// </summary>
    /// <param name="context">Ситуационный контекст</param>
    /// <returns>Уровень сложности (1-10)</returns>
    int DetermineContextComplexity(SituationalContext context);

    /// <summary>
    /// Анализирует временные аспекты контекста.
    /// </summary>
    /// <param name="context">Ситуационный контекст</param>
    /// <returns>Анализ временных требований</returns>
    TemporalContextAnalysis AnalyzeTemporalAspects(SituationalContext context);

    /// <summary>
    /// Определяет ключевые факторы контекста, влияющие на адаптацию.
    /// </summary>
    /// <param name="context">Ситуационный контекст</param>
    /// <returns>Список ключевых факторов</returns>
    List<ContextFactor> IdentifyKeyFactors(SituationalContext context);

    /// <summary>
    /// Прогнозирует потенциальные проблемы в данном контексте.
    /// </summary>
    /// <param name="context">Ситуационный контекст</param>
    /// <returns>Список потенциальных проблем</returns>
    List<string> PredictPotentialChallenges(SituationalContext context);
}

