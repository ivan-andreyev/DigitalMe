using DigitalMe.Common;
using DigitalMe.Data.Entities;
using DigitalMe.Services.PersonalityEngine;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.ApplicationServices.ResponseStyling;

/// <summary>
/// Implementation of personal context analysis.
/// Analyzes situational context to determine appropriate communication style based on personality.
/// </summary>
public class PersonalContextAnalyzer : IPersonalContextAnalyzer
{
    private readonly IPersonalityService _personalityService;
    private readonly ICommunicationStyleAnalyzer _communicationStyleAnalyzer;
    private readonly ILogger<PersonalContextAnalyzer> _logger;

    public PersonalContextAnalyzer(
        IPersonalityService personalityService,
        ICommunicationStyleAnalyzer communicationStyleAnalyzer,
        ILogger<PersonalContextAnalyzer> logger)
    {
        _personalityService = personalityService;
        _communicationStyleAnalyzer = communicationStyleAnalyzer;
        _logger = logger;
    }

    public async Task<Result<ContextualCommunicationStyle>> GetContextualStyleAsync(SituationalContext context)
    {
        return await ResultExtensions.TryAsync(async () =>
        {
            var personalityResult = await _personalityService.GetPersonalityAsync();

            if (!personalityResult.IsSuccess)
                throw new InvalidOperationException($"Failed to load personality profile: {personalityResult.Error}");

            var personality = personalityResult.Value!;
            var style = _communicationStyleAnalyzer.DetermineOptimalCommunicationStyle(personality, context);

            // Apply personality-specific style adjustments
            ApplyPersonalityStyleAdjustments(style, context);

            return style;
        }, $"Error analyzing context {context.ContextType} for style determination");
    }

    private static void ApplyPersonalityStyleAdjustments(ContextualCommunicationStyle style, SituationalContext context)
    {
        // Personality-specific adjustments based on context
        switch (context.ContextType)
        {
            case ContextType.Technical:
                style.DirectnessLevel = Math.Max(style.DirectnessLevel, 0.8); // Direct in technical contexts
                style.TechnicalDepth = Math.Max(style.TechnicalDepth, 0.9); // High technical detail
                style.StructuredApproach = Math.Max(style.StructuredApproach, 0.8); // Structured thinking
                break;

            case ContextType.Professional:
                style.FormalityLevel = Math.Min(style.FormalityLevel, 0.6); // Moderate formality
                style.DirectnessLevel = Math.Max(style.DirectnessLevel, 0.7); // Still direct
                style.PragmatismLevel = Math.Max(style.PragmatismLevel, 0.8); // Highly pragmatic
                break;

            case ContextType.Personal:
                style.FormalityLevel = Math.Min(style.FormalityLevel, 0.3); // Informal
                style.DirectnessLevel = Math.Max(style.DirectnessLevel, 0.9); // Very direct personally
                style.EmotionalTone = Math.Max(style.EmotionalTone, 0.6); // More emotional
                break;
        }

        // Characteristic high confidence and pragmatism
        style.ConfidenceLevel = Math.Max(style.ConfidenceLevel, 0.8);
        style.PragmatismLevel = Math.Max(style.PragmatismLevel, 0.8);
    }

    private static ContextualCommunicationStyle GetFallbackStyle(SituationalContext context)
    {
        return new ContextualCommunicationStyle
        {
            DirectnessLevel = 0.8, // Default directness
            ConfidenceLevel = 0.8, // Default confidence
            PragmatismLevel = 0.9, // High pragmatism
            FormalityLevel = context.ContextType == ContextType.Professional ? 0.6 : 0.4,
            TechnicalDepth = context.ContextType == ContextType.Technical ? 0.9 : 0.5,
            StructuredApproach = 0.8, // Structured thinking
            EmotionalTone = context.ContextType == ContextType.Personal ? 0.6 : 0.4
        };
    }
}