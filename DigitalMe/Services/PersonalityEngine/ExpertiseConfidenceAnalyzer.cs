using DigitalMe.Data.Entities;
using DigitalMe.Services.Strategies;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.PersonalityEngine;

/// <summary>
/// Реализация анализатора уверенности на основе экспертизы.
/// Использует Strategy Pattern для делегирования анализа подходящим стратегиям.
/// </summary>
public class ExpertiseConfidenceAnalyzer : IExpertiseConfidenceAnalyzer
{
    private readonly ILogger<ExpertiseConfidenceAnalyzer> _logger;
    private readonly IPersonalityStrategyFactory _strategyFactory;
    private readonly IPersonalityConfigurationService _configurationService;

    /// <summary>
    /// Инициализирует новый экземпляр анализатора уверенности на основе экспертизы.
    /// </summary>
    /// <param name="logger">Логгер для записи диагностической информации</param>
    /// <param name="strategyFactory">Фабрика стратегий для получения подходящих стратегий анализа</param>
    /// <param name="configurationService">Сервис конфигурации персоналий</param>
    public ExpertiseConfidenceAnalyzer(
        ILogger<ExpertiseConfidenceAnalyzer> logger,
        IPersonalityStrategyFactory strategyFactory,
        IPersonalityConfigurationService configurationService)
    {
        _logger = logger;
        _strategyFactory = strategyFactory;
        _configurationService = configurationService;
    }

    public ExpertiseConfidenceAdjustment AnalyzeExpertiseConfidence(PersonalityProfile personality, DomainType domainType, int taskComplexity)
    {
        _logger.LogDebug("Analyzing expertise confidence for {PersonalityName} in domain {Domain}, complexity {Complexity}",
            personality.Name, domainType, taskComplexity);

        // Validate task complexity
        var validatedComplexity = ValidateTaskComplexity(taskComplexity);

        // Get appropriate strategy for this personality
        var strategy = _strategyFactory.GetStrategy(personality);
        if (strategy == null)
        {
            _logger.LogWarning("No strategy found for personality {PersonalityName}, using generic confidence calculation",
                personality.Name);
            return CreateGenericConfidenceAdjustment(personality, domainType, validatedComplexity);
        }

        _logger.LogDebug("Using strategy {StrategyName} for expertise confidence analysis",
            strategy.StrategyName);

        // Delegate to the strategy
        var adjustment = strategy.CalculateExpertiseConfidence(personality, domainType, validatedComplexity);

        // Apply additional validation
        ValidateConfidenceAdjustment(adjustment);

        _logger.LogDebug("Expertise confidence calculated for {PersonalityName}: base={BaseConfidence}, adjusted={AdjustedConfidence}",
            personality.Name, adjustment.BaseConfidence, adjustment.AdjustedConfidence);

        return adjustment;
    }

    public int ValidateTaskComplexity(int taskComplexity)
    {
        var validated = Math.Clamp(taskComplexity, PersonalityConstants.MinimumTaskComplexity, PersonalityConstants.MaximumTaskComplexity);

        if (validated != taskComplexity)
        {
            _logger.LogDebug("Task complexity adjusted: {OriginalComplexity} → {ValidatedComplexity}",
                taskComplexity, validated);
        }

        return validated;
    }

    public bool IsCoreDomain(PersonalityProfile personality, DomainType domainType)
    {
        // Try to get personality-specific expertise levels
        if (_configurationService.IsPersonalitySupported(personality.Name))
        {
            var expertiseLevels = _configurationService.GetExpertiseLevels(personality.Name);
            if (expertiseLevels.TryGetValue(domainType, out var expertise))
            {
                var isCore = expertise >= PersonalityConstants.ExpertLevelThreshold;
                _logger.LogTrace("Domain {Domain} is core domain for {PersonalityName}: {IsCore} (expertise: {Expertise})",
                    domainType, personality.Name, isCore, expertise);
                return isCore;
            }
        }

        // Fallback: assume no core domains for unknown personalities
        return false;
    }

    public bool IsWeaknessDomain(PersonalityProfile personality, DomainType domainType)
    {
        // Try to get personality-specific expertise levels
        if (_configurationService.IsPersonalitySupported(personality.Name))
        {
            var expertiseLevels = _configurationService.GetExpertiseLevels(personality.Name);
            if (expertiseLevels.TryGetValue(domainType, out var expertise))
            {
                var isWeakness = expertise <= PersonalityConstants.WeaknessLevelThreshold;
                _logger.LogTrace("Domain {Domain} is weakness domain for {PersonalityName}: {IsWeakness} (expertise: {Expertise})",
                    domainType, personality.Name, isWeakness, expertise);
                return isWeakness;
            }
        }

        // Fallback: assume no specific weaknesses for unknown personalities
        return false;
    }

    #region Private Helper Methods

    private ExpertiseConfidenceAdjustment CreateGenericConfidenceAdjustment(PersonalityProfile personality, DomainType domainType, int taskComplexity)
    {
        _logger.LogDebug("Creating generic confidence adjustment for domain {Domain}, complexity {Complexity}",
            domainType, taskComplexity);

        var baseConfidence = PersonalityConstants.GenericBaseConfidence;
        var complexityFactor = 1.0 - (taskComplexity - 1) * PersonalityConstants.GenericComplexityReductionRate;

        // Try to get personality-specific expertise if available in configuration
        if (_configurationService.IsPersonalitySupported(personality.Name))
        {
            var expertiseLevels = _configurationService.GetExpertiseLevels(personality.Name);
            if (expertiseLevels.TryGetValue(domainType, out var specificExpertise))
            {
                baseConfidence = specificExpertise;
                _logger.LogDebug("Used personality-specific expertise for {PersonalityName} in {Domain}: {Expertise}",
                    personality.Name, domainType, specificExpertise);
            }
        }

        return new ExpertiseConfidenceAdjustment
        {
            Domain = domainType,
            TaskComplexity = taskComplexity,
            BaseConfidence = baseConfidence,
            ComplexityAdjustment = complexityFactor,
            AdjustedConfidence = Math.Clamp(baseConfidence * complexityFactor,
                PersonalityConstants.MinimumConfidenceLevel,
                PersonalityConstants.MaximumConfidenceLevel)
        };
    }

    private void ValidateConfidenceAdjustment(ExpertiseConfidenceAdjustment adjustment)
    {
        // Ensure all confidence values are within valid bounds
        adjustment.BaseConfidence = Math.Clamp(adjustment.BaseConfidence,
            PersonalityConstants.MinimumConfidenceLevel,
            PersonalityConstants.MaximumConfidenceLevel);

        adjustment.AdjustedConfidence = Math.Clamp(adjustment.AdjustedConfidence,
            PersonalityConstants.MinimumConfidenceLevel,
            PersonalityConstants.MaximumConfidenceLevel);

        adjustment.DomainExpertiseBonus = Math.Clamp(adjustment.DomainExpertiseBonus, 0.0, 0.3);
        adjustment.KnownWeaknessReduction = Math.Clamp(adjustment.KnownWeaknessReduction, 0.0, 0.5);

        // Ensure complexity adjustment is reasonable
        adjustment.ComplexityAdjustment = Math.Clamp(adjustment.ComplexityAdjustment, 0.1, 1.0);
    }

    #endregion
}