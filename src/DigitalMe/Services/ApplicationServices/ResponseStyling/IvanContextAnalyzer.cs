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
    private readonly IIvanPersonalityService _ivanPersonalityService;
    private readonly ICommunicationStyleAnalyzer _communicationStyleAnalyzer;
    private readonly ILogger<IvanContextAnalyzer> _logger;

    public IvanContextAnalyzer(
        IIvanPersonalityService ivanPersonalityService,
        ICommunicationStyleAnalyzer communicationStyleAnalyzer,
        ILogger<IvanContextAnalyzer> logger)
    {
        _ivanPersonalityService = ivanPersonalityService;
        _communicationStyleAnalyzer = communicationStyleAnalyzer;
        _logger = logger;
    }

    public async Task<ContextualCommunicationStyle> GetContextualStyleAsync(SituationalContext context)
    {
        try
        {
            var personality = await _ivanPersonalityService.GetIvanPersonalityAsync();
            var style = _communicationStyleAnalyzer.DetermineOptimalCommunicationStyle(personality, context);

            // Apply Ivan-specific style adjustments
            ApplyIvanStyleAdjustments(style, context);

            return style;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing context for style determination");
            return GetFallbackStyle(context);
        }
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