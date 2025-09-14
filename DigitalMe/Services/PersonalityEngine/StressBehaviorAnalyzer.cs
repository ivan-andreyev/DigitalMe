using DigitalMe.Data.Entities;
using DigitalMe.Services.Strategies;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.PersonalityEngine;

/// <summary>
/// Реализация анализатора стрессового поведения.
/// Использует Strategy Pattern для делегирования анализа подходящим стратегиям.
/// </summary>
public class StressBehaviorAnalyzer : IStressBehaviorAnalyzer
{
    private readonly ILogger<StressBehaviorAnalyzer> _logger;
    private readonly IPersonalityStrategyFactory _strategyFactory;

    /// <summary>
    /// Инициализирует новый экземпляр анализатора стрессового поведения.
    /// </summary>
    /// <param name="logger">Логгер для записи диагностической информации</param>
    /// <param name="strategyFactory">Фабрика стратегий для получения подходящих стратегий анализа</param>
    public StressBehaviorAnalyzer(
        ILogger<StressBehaviorAnalyzer> logger,
        IPersonalityStrategyFactory strategyFactory)
    {
        _logger = logger;
        _strategyFactory = strategyFactory;
    }

    public StressBehaviorModifications AnalyzeStressModifications(PersonalityProfile personality, double stressLevel, double timePressure)
    {
        _logger.LogDebug("Analyzing stress modifications for {PersonalityName}: stress={StressLevel}, timePressure={TimePressure}",
            personality.Name, stressLevel, timePressure);

        // Validate input parameters
        var (validatedStress, validatedTimePressure) = ValidateStressParameters(stressLevel, timePressure);

        // Get appropriate strategy for this personality
        var strategy = _strategyFactory.GetStrategy(personality);
        if (strategy == null)
        {
            _logger.LogWarning("No strategy found for personality {PersonalityName}, using generic stress patterns",
                personality.Name);
            return CreateGenericStressModifications(validatedStress, validatedTimePressure);
        }

        _logger.LogDebug("Using strategy {StrategyName} for stress behavior analysis",
            strategy.StrategyName);

        // Delegate to the strategy
        var modifications = strategy.CalculateStressModifications(personality, validatedStress, validatedTimePressure);

        // Apply additional validation and normalization
        NormalizeModifications(modifications);

        _logger.LogDebug("Stress modifications calculated for {PersonalityName}: directness={Directness}, warmth_reduction={WarmthReduction}",
            personality.Name, modifications.DirectnessIncrease, modifications.WarmthReduction);

        return modifications;
    }

    public (double ValidatedStress, double ValidatedTimePressure) ValidateStressParameters(double stressLevel, double timePressure)
    {
        var validatedStress = Math.Clamp(stressLevel, PersonalityConstants.MinimumStressLevel, PersonalityConstants.MaximumStressLevel);
        var validatedTimePressure = Math.Clamp(timePressure, PersonalityConstants.MinimumStressLevel, PersonalityConstants.MaximumStressLevel);

        if (validatedStress != stressLevel || validatedTimePressure != timePressure)
        {
            _logger.LogDebug("Stress parameters adjusted: stress {OriginalStress} → {ValidatedStress}, timePressure {OriginalTimePressure} → {ValidatedTimePressure}",
                stressLevel, validatedStress, timePressure, validatedTimePressure);
        }

        return (validatedStress, validatedTimePressure);
    }

    #region Private Helper Methods

    private StressBehaviorModifications CreateGenericStressModifications(double stressLevel, double timePressure)
    {
        _logger.LogDebug("Creating generic stress modifications: stress={StressLevel}, timePressure={TimePressure}",
            stressLevel, timePressure);

        return new StressBehaviorModifications
        {
            // Generic stress behavior patterns
            DirectnessIncrease = stressLevel * PersonalityConstants.GenericStressDirectnessFactor,
            TechnicalDetailReduction = timePressure * PersonalityConstants.GenericTimePressureDetailReduction,
            WarmthReduction = stressLevel * PersonalityConstants.GenericStressWarmthReduction,

            // Basic stress responses
            StructuredThinkingBoost = stressLevel * PersonalityConstants.GenericStructuredThinkingBoostFactor,
            SolutionFocusBoost = timePressure * PersonalityConstants.GenericSolutionFocusBoostFactor,
            SelfReflectionReduction = stressLevel * PersonalityConstants.GenericSelfReflectionReductionFactor,

            // Confidence typically decreases under stress for generic case
            ConfidenceBoost = -stressLevel * PersonalityConstants.GenericConfidenceReductionFactor, // Slight confidence reduction
            PragmatismIncrease = timePressure * PersonalityConstants.GenericPragmatismIncreaseFactor,
            ResultsOrientationIncrease = timePressure * PersonalityConstants.GenericResultsOrientationIncreaseFactor
        };
    }

    private void NormalizeModifications(StressBehaviorModifications modifications)
    {
        // Ensure all values are within reasonable bounds
        modifications.DirectnessIncrease = Math.Clamp(modifications.DirectnessIncrease, 0.0, PersonalityConstants.MaxDirectnessIncrease);
        modifications.StructuredThinkingBoost = Math.Clamp(modifications.StructuredThinkingBoost, 0.0, PersonalityConstants.MaxStructuredThinkingBoost);
        modifications.TechnicalDetailReduction = Math.Clamp(modifications.TechnicalDetailReduction, 0.0, PersonalityConstants.MaxTechnicalDetailReduction);
        modifications.WarmthReduction = Math.Clamp(modifications.WarmthReduction, 0.0, PersonalityConstants.MaxWarmthReduction);
        modifications.SolutionFocusBoost = Math.Clamp(modifications.SolutionFocusBoost, 0.0, PersonalityConstants.MaxSolutionFocusBoost);
        modifications.SelfReflectionReduction = Math.Clamp(modifications.SelfReflectionReduction, 0.0, PersonalityConstants.MaxSelfReflectionReduction);
        modifications.ConfidenceBoost = Math.Clamp(modifications.ConfidenceBoost, PersonalityConstants.MaxConfidenceBoostNegative, PersonalityConstants.MaxConfidenceBoostPositive);
        modifications.PragmatismIncrease = Math.Clamp(modifications.PragmatismIncrease, 0.0, PersonalityConstants.MaxPragmatismIncrease);
        modifications.ResultsOrientationIncrease = Math.Clamp(modifications.ResultsOrientationIncrease, 0.0, PersonalityConstants.MaxResultsOrientationIncrease);
    }

    #endregion
}