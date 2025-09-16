using DigitalMe.Common;
using DigitalMe.Data.Entities;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DigitalMe.Services.ApplicationServices.ResponseStyling;

/// <summary>
/// Implementation of personal linguistic pattern application.
/// Applies personality-specific communication patterns to text.
/// </summary>
public class PersonalLinguisticPatternService : IPersonalLinguisticPatternService
{
    private readonly ILogger<PersonalLinguisticPatternService> _logger;

    public PersonalLinguisticPatternService(ILogger<PersonalLinguisticPatternService> logger)
    {
        _logger = logger;
    }

    public Result<string> ApplyPersonalLinguisticPatterns(string text, ContextualCommunicationStyle style)
    {
        return ResultExtensions.Try(() =>
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var enhancedText = text;

            // Apply patterns based on context
            enhancedText = ApplyDirectnessPattern(enhancedText, style);
            enhancedText = ApplyStructuredThinkingPattern(enhancedText, style);
            enhancedText = ApplyPragmaticLanguagePattern(enhancedText, style);
            enhancedText = ApplyTechnicalPreferencePattern(enhancedText, style);

            return enhancedText;
        }, "Error applying personal linguistic patterns to text");
    }

    private static string ApplyDirectnessPattern(string text, ContextualCommunicationStyle style)
    {
        if (style.DirectnessLevel > 0.7)
        {
            // Apply direct, clear statements based on personality
            text = Regex.Replace(text, @"\b(возможно|может быть|вероятно)\b", "");
            text = Regex.Replace(text, @"\b(я думаю что|мне кажется что)\b", "");
            text = text.Replace("Это достаточно сложно", "Это сложно");
        }

        return text.Trim();
    }

    private static string ApplyStructuredThinkingPattern(string text, ContextualCommunicationStyle style)
    {
        if (style.StructuredApproach > 0.6)
        {
            // Add structured thinking indicators
            if (!text.Contains("1.") && !text.Contains("•") && text.Split('.').Length > 3)
            {
                // Add structure to longer responses
                text = "Структурно подходя к вопросу:\n\n" + text;
            }
        }

        return text;
    }

    private static string ApplyPragmaticLanguagePattern(string text, ContextualCommunicationStyle style)
    {
        if (style.PragmatismLevel > 0.8)
        {
            // Replace abstract concepts with concrete examples
            text = text.Replace("теоретически", "на практике");
            text = text.Replace("концептуально", "конкретно");
            text = text.Replace("в идеале", "реально");
        }

        return text;
    }

    private static string ApplyTechnicalPreferencePattern(string text, ContextualCommunicationStyle style)
    {
        if (style.TechnicalDepth > 0.7)
        {
            // Add technical preference indicators based on personality
            if (!text.Contains("C#") && !text.Contains(".NET") && text.Contains("programming"))
            {
                text = text.Replace("programming", "C#/.NET programming");
            }
            text = text.Replace("использовать", "применить");
            text = text.Replace("сделать", "реализовать");
            text = text.Replace("проверить", "валидировать");
        }

        return text;
    }
}