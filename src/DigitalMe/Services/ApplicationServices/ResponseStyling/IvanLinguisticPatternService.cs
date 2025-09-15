using DigitalMe.Data.Entities;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DigitalMe.Services.ApplicationServices.ResponseStyling;

/// <summary>
/// Implementation of Ivan-specific linguistic pattern application.
/// Applies Ivan's characteristic communication patterns to text.
/// </summary>
public class IvanLinguisticPatternService : IIvanLinguisticPatternService
{
    private readonly ILogger<IvanLinguisticPatternService> _logger;

    public IvanLinguisticPatternService(ILogger<IvanLinguisticPatternService> logger)
    {
        _logger = logger;
    }

    public string ApplyIvanLinguisticPatterns(string text, ContextualCommunicationStyle style)
    {
        try
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying linguistic patterns to text");
            return text; // Return original text on error
        }
    }

    private static string ApplyDirectnessPattern(string text, ContextualCommunicationStyle style)
    {
        if (style.DirectnessLevel > 0.7)
        {
            // Ivan prefers direct, clear statements
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
            // Add Ivan's structured thinking indicators
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
            // Add Ivan's technical preference indicators
            if (!text.Contains("C#") && !text.Contains(".NET") && text.Contains("programming"))
            {
                text = text.Replace("programming", "C#/.NET programming");
            }
            text = text.Replace("использовать", "применить");
            text = text.Replace("сделать", "реализовать");
            text = text.Replace("проверить", "валидировать");
        }

        // Add personal honesty patterns for personal contexts
        if (style.Context?.ContextType == ContextType.Personal && style.EmotionalTone > 0.5)
        {
            if (text.Contains("balance") && !text.Contains("struggle"))
            {
                text = text.Replace("balance", "struggle to balance");
            }
        }

        return text;
    }
}