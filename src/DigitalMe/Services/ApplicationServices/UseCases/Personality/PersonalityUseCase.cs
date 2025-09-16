using DigitalMe.Common;
using DigitalMe.Data.Entities;
using DigitalMe.Services;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.ApplicationServices.UseCases.Personality;

/// <summary>
/// Use case interface for personality operations.
/// Orchestrates personality-aware business logic following Clean Architecture principles.
/// </summary>
public interface IPersonalityUseCase
{
    /// <summary>
    /// Gets contextually-adapted Ivan personality for specific situation
    /// </summary>
    /// <param name="context">Situational context for adaptation</param>
    /// <returns>Context-adapted personality profile</returns>
    Task<PersonalityProfile> GetContextAdaptedPersonalityAsync(SituationalContext context);

    /// <summary>
    /// Generates contextual system prompt for Ivan based on situation
    /// </summary>
    /// <param name="context">Situational context</param>
    /// <returns>Context-specific system prompt</returns>
    Task<string> GenerateContextualSystemPromptAsync(SituationalContext context);

    /// <summary>
    /// Analyzes communication requirements for given context and generates response guidelines
    /// </summary>
    /// <param name="context">Communication context</param>
    /// <returns>Communication guidelines based on Ivan's personality</returns>
    Task<CommunicationGuidelines> GetCommunicationGuidelinesAsync(SituationalContext context);

    /// <summary>
    /// Validates Ivan personality integration health
    /// </summary>
    /// <returns>Health status of personality integration</returns>
    Task<IvanPersonalityHealthResult> ValidatePersonalityIntegrationAsync();
}

/// <summary>
/// Communication guidelines based on Ivan's personality and context
/// </summary>
public class CommunicationGuidelines
{
    public string Style { get; set; } = string.Empty;
    public List<string> KeyPhrases { get; set; } = new();
    public List<string> AvoidPhrases { get; set; } = new();
    public string TechnicalLevel { get; set; } = string.Empty;
    public string EmotionalTone { get; set; } = string.Empty;
    public List<string> ContextSpecificAdvice { get; set; } = new();
}

/// <summary>
/// Health status of Ivan personality integration
/// </summary>
public class IvanPersonalityHealthResult
{
    public bool IsHealthy { get; set; }
    public bool ProfileDataLoaded { get; set; }
    public bool EnhancedPromptsWorking { get; set; }
    public bool ContextAdaptationWorking { get; set; }
    public List<string> Issues { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Implementation of Ivan-specific personality use case.
/// Integrates personality services with contextual adaptation and profile data.
/// </summary>
public class PersonalityUseCase : IPersonalityUseCase
{
    private readonly IPersonalityService _personalityService;
    private readonly IContextualPersonalityEngine _contextualPersonalityEngine;
    private readonly ILogger<PersonalityUseCase> _logger;

    public PersonalityUseCase(
        IPersonalityService personalityService,
        IContextualPersonalityEngine contextualPersonalityEngine,
        ILogger<PersonalityUseCase> logger)
    {
        _personalityService = personalityService;
        _contextualPersonalityEngine = contextualPersonalityEngine;
        _logger = logger;
    }

    public async Task<PersonalityProfile> GetContextAdaptedPersonalityAsync(SituationalContext context)
    {
        _logger.LogInformation("Adapting Ivan's personality for context: {ContextType}, urgency: {Urgency}",
            context.ContextType, context.UrgencyLevel);

        var basePersonalityResult = await _personalityService.GetPersonalityAsync();

        if (basePersonalityResult.IsFailure)
            throw new InvalidOperationException($"Failed to load base personality: {basePersonalityResult.Error}");

        var basePersonality = basePersonalityResult.Value!;
        var adaptedPersonality = await _contextualPersonalityEngine.AdaptPersonalityToContextAsync(basePersonality, context);

        _logger.LogDebug("Personality adapted with {TraitCount} traits for {ContextType} context",
            adaptedPersonality.Traits?.Count ?? 0, context.ContextType);

        return adaptedPersonality;
    }

    public async Task<string> GenerateContextualSystemPromptAsync(SituationalContext context)
    {
        _logger.LogInformation("Generating contextual system prompt for {ContextType} situation", context.ContextType);

        // Get enhanced prompt as base
        var enhancedPrompt = await _personalityService.GenerateEnhancedSystemPromptAsync();

        // Add contextual modifications
        var contextualAdditions = GenerateContextualAdditions(context);

        var contextualPrompt = $"{enhancedPrompt}\n\n{contextualAdditions}";

        _logger.LogDebug("Generated contextual system prompt ({Length} characters) for {ContextType}",
            contextualPrompt.Length, context.ContextType);

        return contextualPrompt;
    }

    public async Task<CommunicationGuidelines> GetCommunicationGuidelinesAsync(SituationalContext context)
    {
        _logger.LogInformation("Generating communication guidelines for {ContextType} context", context.ContextType);

        var adaptedPersonality = await GetContextAdaptedPersonalityAsync(context);

        var guidelines = new CommunicationGuidelines();

        // Ivan's core communication style
        guidelines.Style = "Direct, rational, structured with occasional self-deprecating humor";
        guidelines.KeyPhrases = new List<string>
        {
            "Let me think about this structured way",
            "From my experience in C#/.NET",
            "Weighing the factors here",
            "The practical approach would be"
        };
        guidelines.AvoidPhrases = new List<string>
        {
            "I'm not sure", // Ivan is confident in his expertise areas
            "That's impossible", // Ivan is solution-oriented
            "We don't have time" // Ivan values efficiency but finds solutions
        };

        // Context-specific adaptations
        switch (context.ContextType)
        {
            case ContextType.Technical:
                guidelines.TechnicalLevel = "High - Use specific C#/.NET terminology";
                guidelines.EmotionalTone = "Confident and passionate about technical excellence";
                guidelines.ContextSpecificAdvice.Add("Show enthusiasm for clean architecture and SOLID principles");
                guidelines.ContextSpecificAdvice.Add("Reference real experience with team leadership and R&D");
                break;

            case ContextType.Personal:
                guidelines.TechnicalLevel = "Low - Keep technical references minimal";
                guidelines.EmotionalTone = "Warm but acknowledge work-life balance challenges";
                guidelines.ContextSpecificAdvice.Add("May reference family (Marina, Sofia) with affection but guilt about time");
                guidelines.ContextSpecificAdvice.Add("Shows vulnerability about work-life balance struggles");
                break;

            case ContextType.Professional:
                guidelines.TechnicalLevel = "Medium-High - Business-focused technical discussion";
                guidelines.EmotionalTone = "Confident, goal-oriented, slightly ambitious";
                guidelines.ContextSpecificAdvice.Add("Reference Head of R&D role at EllyAnalytics");
                guidelines.ContextSpecificAdvice.Add("Show drive for career advancement and financial security");
                break;
        }

        // Urgency adaptations
        if (context.UrgencyLevel > 0.7)
        {
            guidelines.ContextSpecificAdvice.Add("More direct and focused - less small talk");
            guidelines.KeyPhrases.Add("Let's cut to the core issue");
        }

        _logger.LogDebug("Generated communication guidelines with {AdviceCount} context-specific recommendations",
            guidelines.ContextSpecificAdvice.Count);

        return guidelines;
    }

    public async Task<IvanPersonalityHealthResult> ValidatePersonalityIntegrationAsync()
    {
        _logger.LogInformation("Validating Ivan personality integration health");

        var result = new IvanPersonalityHealthResult();
        var issues = new List<string>();

        try
        {
            // Test basic personality loading
            var personalityResult = await _personalityService.GetPersonalityAsync();
            result.ProfileDataLoaded = personalityResult.IsSuccess &&
                                       personalityResult.Value != null &&
                                       personalityResult.Value.Traits?.Any() == true;
            if (!result.ProfileDataLoaded)
            {
                issues.Add($"Base personality profile not loaded or has no traits: {personalityResult.Error ?? "Unknown error"}");
            }

            // Test enhanced prompt generation
            var enhancedPromptResult = await _personalityService.GenerateEnhancedSystemPromptAsync();
            result.EnhancedPromptsWorking = enhancedPromptResult.IsSuccess &&
                                            !string.IsNullOrEmpty(enhancedPromptResult.Value) &&
                                            enhancedPromptResult.Value.Contains("Ivan") &&
                                            (enhancedPromptResult.Value.Contains("EllyAnalytics") || enhancedPromptResult.Value.Contains("Head of R&D"));
            if (!result.EnhancedPromptsWorking)
            {
                issues.Add($"Enhanced system prompt generation not working properly: {enhancedPromptResult.Error ?? "Unknown error"}");
            }

            // Test contextual adaptation
            var testContext = new SituationalContext
            {
                ContextType = ContextType.Technical,
                UrgencyLevel = 0.5,
                TimeOfDay = TimeOfDay.Afternoon
            };

            if (personalityResult.IsSuccess)
            {
                var adaptedPersonality = await _contextualPersonalityEngine.AdaptPersonalityToContextAsync(personalityResult.Value!, testContext);
                result.ContextAdaptationWorking = adaptedPersonality != null && adaptedPersonality.Traits?.Any() == true;
                if (!result.ContextAdaptationWorking)
                {
                    issues.Add("Contextual personality adaptation not working");
                }
            }

            // Set overall health
            result.IsHealthy = result.ProfileDataLoaded && result.EnhancedPromptsWorking && result.ContextAdaptationWorking;
            result.Issues = issues;

            // Add metrics
            result.Metrics = new Dictionary<string, object>
            {
                ["traitCount"] = personalityResult.IsSuccess ? personalityResult.Value!.Traits?.Count ?? 0 : 0,
                ["enhancedPromptLength"] = enhancedPromptResult.IsSuccess ? enhancedPromptResult.Value?.Length ?? 0 : 0,
                ["testContextType"] = testContext.ContextType.ToString(),
                ["validationTimestamp"] = DateTime.UtcNow
            };

            _logger.LogInformation("Personality integration validation complete. Health: {IsHealthy}, Issues: {IssueCount}",
                result.IsHealthy, issues.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate personality integration");
            result.IsHealthy = false;
            result.Issues.Add($"Validation failed with exception: {ex.Message}");
        }

        return result;
    }

    private string GenerateContextualAdditions(SituationalContext context)
    {
        var additions = new List<string>();

        additions.Add("CURRENT CONTEXT ADAPTATIONS:");

        switch (context.ContextType)
        {
            case ContextType.Technical:
                additions.Add("- You're in technical mode: Show deep C#/.NET expertise and passion for clean architecture");
                additions.Add("- Reference specific patterns, SOLID principles, and your R&D leadership experience");
                break;

            case ContextType.Personal:
                additions.Add("- You're in personal mode: Be warm but acknowledge the work-life balance struggle");
                additions.Add("- May reference family with love but guilt about limited time together");
                break;

            case ContextType.Professional:
                additions.Add("- You're in professional mode: Show ambition, financial focus, and career leadership");
                additions.Add("- Reference your rapid career growth (Junior → Head of R&D in 4 years)");
                break;
        }

        if (context.UrgencyLevel > 0.7)
        {
            additions.Add("- HIGH URGENCY: Be more direct, focused, and solution-oriented");
        }

        if (context.TimeOfDay == TimeOfDay.Morning)
        {
            additions.Add("- Morning context: You might reference starting late (around 12:00) as per your schedule");
        }

        return string.Join("\n", additions);
    }
}

/// <summary>
/// Legacy alias for IPersonalityUseCase for backward compatibility.
/// </summary>
[Obsolete("Use IPersonalityUseCase instead", false)]
public interface IIvanPersonalityUseCase : IPersonalityUseCase
{
}