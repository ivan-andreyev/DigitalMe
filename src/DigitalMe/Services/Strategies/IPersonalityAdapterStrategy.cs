using DigitalMe.Data.Entities;

namespace DigitalMe.Services.Strategies;

/// <summary>
/// Стратегия адаптации персоналии под различные контексты.
/// Реализует Strategy Pattern для поддержки множественных персоналий.
/// </summary>
public interface IPersonalityAdapterStrategy
{
    /// <summary>
    /// Проверяет, может ли данная стратегия обработать указанную персоналию.
    /// </summary>
    /// <param name="personality">Профиль личности для проверки</param>
    /// <returns>True, если стратегия может обработать эту персоналию</returns>
    bool CanHandle(PersonalityProfile personality);

    /// <summary>
    /// Адаптирует профиль личности под ситуационный контекст.
    /// </summary>
    /// <param name="personality">Базовый профиль личности</param>
    /// <param name="context">Ситуационный контекст</param>
    /// <returns>Адаптированный профиль личности</returns>
    Task<PersonalityProfile> AdaptToContextAsync(PersonalityProfile personality, SituationalContext context);

    /// <summary>
    /// Вычисляет стрессовые модификации поведения.
    /// </summary>
    /// <param name="personality">Профиль личности</param>
    /// <param name="stressLevel">Уровень стресса (0.0-1.0)</param>
    /// <param name="timePressure">Уровень временного давления (0.0-1.0)</param>
    /// <returns>Модификации поведения под стрессом</returns>
    StressBehaviorModifications CalculateStressModifications(PersonalityProfile personality, double stressLevel, double timePressure);

    /// <summary>
    /// Вычисляет настройки уверенности на основе экспертизы.
    /// </summary>
    /// <param name="personality">Профиль личности</param>
    /// <param name="domainType">Тип предметной области</param>
    /// <param name="taskComplexity">Сложность задачи (1-10)</param>
    /// <returns>Настройки уверенности</returns>
    ExpertiseConfidenceAdjustment CalculateExpertiseConfidence(PersonalityProfile personality, DomainType domainType, int taskComplexity);

    /// <summary>
    /// Определяет оптимальный стиль коммуникации для контекста.
    /// </summary>
    /// <param name="personality">Профиль личности</param>
    /// <param name="context">Ситуационный контекст</param>
    /// <returns>Рекомендуемый стиль коммуникации</returns>
    ContextualCommunicationStyle DetermineCommunicationStyle(PersonalityProfile personality, SituationalContext context);

    /// <summary>
    /// Анализирует требования контекста для адаптации.
    /// </summary>
    /// <param name="context">Ситуационный контекст</param>
    /// <returns>Результат анализа контекста</returns>
    ContextAnalysisResult AnalyzeContext(SituationalContext context);

    /// <summary>
    /// Получает приоритет стратегии (для разрешения конфликтов).
    /// Более высокие значения имеют больший приоритет.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Получает имя стратегии для логирования и отладки.
    /// </summary>
    string StrategyName { get; }
}

/// <summary>
/// Фабрика для создания и выбора подходящих стратегий адаптации персоналий.
/// </summary>
public interface IPersonalityStrategyFactory
{
    /// <summary>
    /// Получает подходящую стратегию для указанной персоналии.
    /// </summary>
    /// <param name="personality">Профиль личности</param>
    /// <returns>Подходящая стратегия или null, если стратегия не найдена</returns>
    IPersonalityAdapterStrategy? GetStrategy(PersonalityProfile personality);

    /// <summary>
    /// Получает все доступные стратегии, отсортированные по приоритету.
    /// </summary>
    /// <returns>Список всех стратегий в порядке убывания приоритета</returns>
    IEnumerable<IPersonalityAdapterStrategy> GetAllStrategies();

    /// <summary>
    /// Регистрирует новую стратегию в фабрике.
    /// </summary>
    /// <param name="strategy">Стратегия для регистрации</param>
    void RegisterStrategy(IPersonalityAdapterStrategy strategy);
}

/// <summary>
/// Реализация фабрики стратегий адаптации персоналий.
/// </summary>
public class PersonalityStrategyFactory : IPersonalityStrategyFactory
{
    private readonly List<IPersonalityAdapterStrategy> _strategies;
    private readonly ILogger<PersonalityStrategyFactory> _logger;

    public PersonalityStrategyFactory(
        IEnumerable<IPersonalityAdapterStrategy> strategies,
        ILogger<PersonalityStrategyFactory> logger)
    {
        _strategies = strategies.OrderByDescending(s => s.Priority).ToList();
        _logger = logger;

        _logger.LogInformation("Initialized PersonalityStrategyFactory with {StrategyCount} strategies: {StrategyNames}",
            _strategies.Count, string.Join(", ", _strategies.Select(s => s.StrategyName)));
    }

    public IPersonalityAdapterStrategy? GetStrategy(PersonalityProfile personality)
    {
        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(personality));

        if (strategy != null)
        {
            _logger.LogDebug("Selected strategy {StrategyName} for personality {PersonalityName}",
                strategy.StrategyName, personality.Name);
        }
        else
        {
            _logger.LogWarning("No strategy found for personality {PersonalityName}", personality.Name);
        }

        return strategy;
    }

    public IEnumerable<IPersonalityAdapterStrategy> GetAllStrategies()
    {
        return _strategies.AsReadOnly();
    }

    public void RegisterStrategy(IPersonalityAdapterStrategy strategy)
    {
        if (strategy == null)
        {
            throw new ArgumentNullException(nameof(strategy));
        }

        _strategies.Add(strategy);
        _strategies.Sort((a, b) => b.Priority.CompareTo(a.Priority)); // Re-sort by priority

        _logger.LogInformation("Registered new strategy {StrategyName} with priority {Priority}",
            strategy.StrategyName, strategy.Priority);
    }
}