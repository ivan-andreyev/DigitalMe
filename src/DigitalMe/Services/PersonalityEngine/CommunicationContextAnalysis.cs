using DigitalMe.Data.Entities;

namespace DigitalMe.Services.PersonalityEngine;

/// <summary>
/// Результат анализа коммуникационных требований контекста.
/// Содержит рекомендации по адаптации стиля общения.
/// </summary>
public class CommunicationContextAnalysis
{
    /// <summary>
    /// Анализируемый контекст.
    /// </summary>
    public SituationalContext Context { get; set; } = null!;

    /// <summary>
    /// Рекомендуемый уровень формальности (0.0-1.0).
    /// </summary>
    public double RecommendedFormalityLevel { get; set; } = 0.5;

    /// <summary>
    /// Рекомендуемый уровень прямоты общения (0.0-1.0).
    /// </summary>
    public double RecommendedDirectnessLevel { get; set; } = 0.5;

    /// <summary>
    /// Рекомендуемая глубина технических деталей (0.0-1.0).
    /// </summary>
    public double RecommendedTechnicalDepth { get; set; } = 0.5;

    /// <summary>
    /// Рекомендуемый уровень эмоциональной открытости (0.0-1.0).
    /// </summary>
    public double RecommendedEmotionalOpenness { get; set; } = 0.5;

    /// <summary>
    /// Рекомендуемый уровень теплоты общения (0.0-1.0).
    /// </summary>
    public double RecommendedWarmthLevel { get; set; } = 0.5;

    /// <summary>
    /// Коммуникационные требования для данного контекста.
    /// </summary>
    public List<string> CommunicationRequirements { get; set; } = new();

    /// <summary>
    /// Потенциальные коммуникационные ловушки и вызовы.
    /// </summary>
    public List<string> CommunicationChallenges { get; set; } = new();

    /// <summary>
    /// Рекомендуемый тон общения.
    /// </summary>
    public string RecommendedTone { get; set; } = "";

    /// <summary>
    /// Рекомендации по стилю общения для данного контекста.
    /// </summary>
    public List<string> StyleRecommendations { get; set; } = new();

    /// <summary>
    /// Приоритетные аспекты коммуникации для данного контекста.
    /// </summary>
    public List<string> PriorityCommunicationAspects { get; set; } = new();

    /// <summary>
    /// Временная метка анализа.
    /// </summary>
    public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;
}