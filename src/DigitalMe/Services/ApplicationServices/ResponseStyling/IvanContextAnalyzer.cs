using DigitalMe.Common;
using DigitalMe.Data.Entities;
using DigitalMe.Services.PersonalityEngine;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.ApplicationServices.ResponseStyling;

/// <summary>
/// Implementation of Ivan-specific context analysis.
/// Analyzes situational context to determine appropriate communication style.
/// </summary>
public class IvanContextAnalyzer : IIvanContextAnalyzer
{
    private readonly IPersonalityService _personalityService;
    private readonly ICommunicationStyleAnalyzer _communicationStyleAnalyzer;
    private readonly ILogger<IvanContextAnalyzer> _logger;

    public IvanContextAnalyzer(
        IPersonalityService personalityService,
        ICommunicationStyleAnalyzer communicationStyleAnalyzer,
        ILogger<IvanContextAnalyzer> logger)
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

            if (personalityResult.IsFailure)
                throw new InvalidOperationException($"Failed to load personality profile: {personalityResult.Error}");

            var personality = personalityResult.Value!;
            var style = _communicationStyleAnalyzer.DetermineOptimalCommunicationStyle(personality, context);

            // Apply Ivan-specific style adjustments
            ApplyIvanStyleAdjustments(style, context);

            return style;
        }, $"Error analyzing context for style determination: {context.ContextType}");
    }

    private static void ApplyIvanStyleAdjustments(ContextualCommunicationStyle style, SituationalContext context)
    {
        // Ivan's personality-specific adjustments
        switch (context.ContextType)
        {
            case ContextType.Technical:
                style.DirectnessLevel = Math.Max(style.DirectnessLevel, 0.8); // Ivan is direct in technical contexts
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

        // Ivan's characteristic high confidence and pragmatism
        style.ConfidenceLevel = Math.Max(style.ConfidenceLevel, 0.8);
        style.PragmatismLevel = Math.Max(style.PragmatismLevel, 0.8);
    }

    private static ContextualCommunicationStyle GetFallbackStyle(SituationalContext context)
    {
        return new ContextualCommunicationStyle
        {
            DirectnessLevel = 0.8, // Ivan's default directness
            ConfidenceLevel = 0.8, // Ivan's default confidence
            PragmatismLevel = 0.9, // Ivan's high pragmatism
            FormalityLevel = context.ContextType == ContextType.Professional ? 0.6 : 0.4,
            TechnicalDepth = context.ContextType == ContextType.Technical ? 0.9 : 0.5,
            StructuredApproach = 0.8, // Ivan's structured thinking
            EmotionalTone = context.ContextType == ContextType.Personal ? 0.6 : 0.4
        };
    }
}