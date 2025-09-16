using DigitalMe.Common;
using DigitalMe.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalMe.Controllers;

/// <summary>
/// Ivan-specific personality controller for enhanced profile integration.
/// Provides endpoints for Ivan's personality data and system prompts.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IvanController : ControllerBase
{
    private readonly IPersonalityService _personalityService;
    private readonly ILogger<IvanController> _logger;

    public IvanController(
        IPersonalityService personalityService,
        ILogger<IvanController> logger)
    {
        _personalityService = personalityService;
        _logger = logger;
    }

    /// <summary>
    /// Gets Ivan's personality profile
    /// </summary>
    [HttpGet("personality")]
    public async Task<ActionResult<object>> GetPersonality()
    {
        var personalityResult = await _personalityService.GetPersonalityAsync();

        if (personalityResult.IsFailure)
        {
            _logger.LogError("Failed to retrieve Ivan's personality profile: {Error}", personalityResult.Error);
            return StatusCode(500, new { message = "Failed to retrieve personality profile", error = personalityResult.Error });
        }

        var personality = personalityResult.Value!;

        _logger.LogInformation("Retrieved Ivan's personality profile with {TraitCount} traits",
            personality.Traits?.Count ?? 0);

        return Ok(new
        {
            name = personality.Name,
            description = personality.Description,
            traits = personality.Traits?.Select(t => new
            {
                name = t.Name,
                description = t.Description,
                category = t.Category,
                weight = t.Weight
            }).ToList()
        });
    }

    /// <summary>
    /// Gets basic system prompt for Ivan's personality
    /// </summary>
    [HttpGet("prompt/basic")]
    public async Task<ActionResult<object>> GetBasicSystemPrompt()
    {
        var personalityResult = await _personalityService.GetPersonalityAsync();

        if (personalityResult.IsFailure)
        {
            _logger.LogError("Failed to retrieve personality for prompt generation: {Error}", personalityResult.Error);
            return StatusCode(500, new { message = "Failed to generate system prompt", error = personalityResult.Error });
        }

        var promptResult = _personalityService.GenerateSystemPrompt(personalityResult.Value!);

        if (promptResult.IsFailure)
        {
            _logger.LogError("Failed to generate system prompt: {Error}", promptResult.Error);
            return StatusCode(500, new { message = "Failed to generate system prompt", error = promptResult.Error });
        }

        var prompt = promptResult.Value!;

        _logger.LogInformation("Generated basic system prompt ({PromptLength} characters)", prompt.Length);

        return Ok(new {
            prompt,
            type = "basic",
            generatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Gets enhanced system prompt with profile data integration
    /// </summary>
    [HttpGet("prompt/enhanced")]
    public async Task<ActionResult<object>> GetEnhancedSystemPrompt()
    {
        var enhancedPromptResult = await _personalityService.GenerateEnhancedSystemPromptAsync();

        if (enhancedPromptResult.IsFailure)
        {
            _logger.LogError("Failed to generate enhanced system prompt: {Error}", enhancedPromptResult.Error);
            return StatusCode(500, new { message = "Failed to generate enhanced system prompt", error = enhancedPromptResult.Error });
        }

        var enhancedPrompt = enhancedPromptResult.Value!;

        _logger.LogInformation("Generated enhanced system prompt with profile data ({PromptLength} characters)",
            enhancedPrompt.Length);

        return Ok(new {
            prompt = enhancedPrompt,
            type = "enhanced",
            source = "IVAN_PROFILE_DATA.md",
            generatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Health check endpoint for Ivan personality integration
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<object>> GetHealthStatus()
    {
        var personalityResult = await _personalityService.GetPersonalityAsync();
        var basicPromptResult = personalityResult.IsSuccess ?
            _personalityService.GenerateSystemPrompt(personalityResult.Value!) :
            Result<string>.Failure("Cannot generate prompt - personality loading failed");
        var enhancedPromptResult = await _personalityService.GenerateEnhancedSystemPromptAsync();

        var personality = personalityResult.IsSuccess ? personalityResult.Value : null;
        var basicPrompt = basicPromptResult.IsSuccess ? basicPromptResult.Value : string.Empty;
        var enhancedPrompt = enhancedPromptResult.IsSuccess ? enhancedPromptResult.Value : string.Empty;

        var health = new
        {
            status = personalityResult.IsSuccess && basicPromptResult.IsSuccess && enhancedPromptResult.IsSuccess ? "healthy" : "degraded",
            personalityLoaded = personalityResult.IsSuccess,
            personalityError = personalityResult.IsFailure ? personalityResult.Error : null,
            traitCount = personality?.Traits?.Count ?? 0,
            basicPromptGenerated = basicPromptResult.IsSuccess && !string.IsNullOrEmpty(basicPrompt) && basicPrompt.Contains("Ivan"),
            basicPromptError = basicPromptResult.IsFailure ? basicPromptResult.Error : null,
            basicPromptLength = basicPrompt?.Length ?? 0,
            enhancedPromptGenerated = enhancedPromptResult.IsSuccess && !string.IsNullOrEmpty(enhancedPrompt) && enhancedPrompt.Contains("Ivan"),
            enhancedPromptError = enhancedPromptResult.IsFailure ? enhancedPromptResult.Error : null,
            enhancedPromptLength = enhancedPrompt?.Length ?? 0,
            profileDataIntegrated = !string.IsNullOrEmpty(enhancedPrompt) && (enhancedPrompt.Contains("EllyAnalytics") || enhancedPrompt.Contains("Batumi")),
            checkedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Ivan personality health check: {Status}",
            health.personalityLoaded && health.basicPromptGenerated && health.enhancedPromptGenerated ? "Healthy" : "Degraded");

        return Ok(health);
    }
}