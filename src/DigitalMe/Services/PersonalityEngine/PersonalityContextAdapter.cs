using DigitalMe.Data.Entities;
using DigitalMe.Services.Strategies;
using DigitalMe.Services.Utils;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.PersonalityEngine;

/// <summary>
/// Реализация адаптера контекста личности.
/// Использует Strategy Pattern для делегирования работы подходящим стратегиям.
/// </summary>
public class PersonalityContextAdapter : IPersonalityContextAdapter
{
    private readonly ILogger<PersonalityContextAdapter> _logger;
    private readonly IPersonalityStrategyFactory _strategyFactory;

    public PersonalityContextAdapter(
        ILogger<PersonalityContextAdapter> logger,
        IPersonalityStrategyFactory strategyFactory)
    {
        _logger = logger;
        _strategyFactory = strategyFactory;
    }

    public async Task<PersonalityProfile> AdaptToContextAsync(PersonalityProfile basePersonality, SituationalContext context)
    {
        _logger.LogDebug("Adapting personality {PersonalityName} to context: {ContextType}, urgency: {Urgency}, time: {TimeOfDay}",
            basePersonality.Name, context.ContextType, context.UrgencyLevel, context.TimeOfDay);

        // Get appropriate strategy for this personality
        var strategy = _strategyFactory.GetStrategy(basePersonality);
        if (strategy == null)
        {
            _logger.LogWarning("No strategy found for personality {PersonalityName}, using basic cloning",
                basePersonality.Name);
            return ClonePersonalityProfile(basePersonality);
        }

        _logger.LogDebug("Using strategy {StrategyName} for personality adaptation",
            strategy.StrategyName);

        // Delegate to the strategy
        var adaptedPersonality = await strategy.AdaptToContextAsync(basePersonality, context);

        _logger.LogDebug("Successfully adapted personality {PersonalityName} using {StrategyName}",
            basePersonality.Name, strategy.StrategyName);

        return adaptedPersonality;
    }

    public PersonalityProfile ClonePersonalityProfile(PersonalityProfile original)
    {
        _logger.LogTrace("Cloning personality profile {PersonalityName}", original.Name);

        var cloned = PersonalityProfileCloner.Clone(original);

        _logger.LogTrace("Cloned personality profile with {TraitCount} traits and {PatternCount} temporal patterns",
            cloned.Traits?.Count ?? 0, cloned.TemporalPatterns?.Count ?? 0);

        return cloned;
    }
}