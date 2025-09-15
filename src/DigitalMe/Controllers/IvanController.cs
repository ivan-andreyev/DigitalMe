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
    private readonly IIvanPersonalityService _ivanPersonalityService;
    private readonly ILogger<IvanController> _logger;

    public IvanController(
        IIvanPersonalityService ivanPersonalityService,
        ILogger<IvanController> logger)
    {
        _ivanPersonalityService = ivanPersonalityService;
        _logger = logger;
    }

    /// <summary>
    /// Gets Ivan's personality profile
    /// </summary>
    [HttpGet("personality")]
    public async Task<ActionResult<object>> GetPersonality()
    {
        try
        {
            var personality = await _ivanPersonalityService.GetIvanPersonalityAsync();

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Ivan's personality profile");
            return StatusCode(500, new { message = "Failed to retrieve personality profile", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets basic system prompt for Ivan's personality
    /// </summary>
    [HttpGet("prompt/basic")]
    public async Task<ActionResult<object>> GetBasicSystemPrompt()
    {
        try
        {
            var personality = await _ivanPersonalityService.GetIvanPersonalityAsync();
            var prompt = _ivanPersonalityService.GenerateSystemPrompt(personality);

            _logger.LogInformation("Generated basic system prompt ({PromptLength} characters)", prompt.Length);

            return Ok(new {
                prompt,
                type = "basic",
                generatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate basic system prompt");
            return StatusCode(500, new { message = "Failed to generate system prompt", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets enhanced system prompt with profile data integration
    /// </summary>
    [HttpGet("prompt/enhanced")]
    public async Task<ActionResult<object>> GetEnhancedSystemPrompt()
    {
        try
        {
            var enhancedPrompt = await _ivanPersonalityService.GenerateEnhancedSystemPromptAsync();

            _logger.LogInformation("Generated enhanced system prompt with profile data ({PromptLength} characters)",
                enhancedPrompt.Length);

            return Ok(new {
                prompt = enhancedPrompt,
                type = "enhanced",
                source = "IVAN_PROFILE_DATA.md",
                generatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate enhanced system prompt");
            return StatusCode(500, new { message = "Failed to generate enhanced system prompt", error = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint for Ivan personality integration
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<object>> GetHealthStatus()
    {
        try
        {
            var personality = await _ivanPersonalityService.GetIvanPersonalityAsync();
            var basicPrompt = _ivanPersonalityService.GenerateSystemPrompt(personality);
            var enhancedPrompt = await _ivanPersonalityService.GenerateEnhancedSystemPromptAsync();

            var health = new
            {
                status = "healthy",
                personalityLoaded = personality != null,
                traitCount = personality?.Traits?.Count ?? 0,
                basicPromptGenerated = !string.IsNullOrEmpty(basicPrompt) && basicPrompt.Contains("Ivan"),
                basicPromptLength = basicPrompt.Length,
                enhancedPromptGenerated = !string.IsNullOrEmpty(enhancedPrompt) && enhancedPrompt.Contains("Ivan"),
                enhancedPromptLength = enhancedPrompt.Length,
                profileDataIntegrated = enhancedPrompt.Contains("EllyAnalytics") || enhancedPrompt.Contains("Batumi"),
                checkedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Ivan personality health check: {Status}",
                health.personalityLoaded && health.basicPromptGenerated && health.enhancedPromptGenerated ? "Healthy" : "Degraded");

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ivan personality health check failed");
            return StatusCode(500, new {
                status = "unhealthy",
                error = ex.Message,
                checkedAt = DateTime.UtcNow
            });
        }
    }
}