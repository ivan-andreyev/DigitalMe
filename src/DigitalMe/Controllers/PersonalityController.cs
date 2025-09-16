using DigitalMe.DTOs;
using DigitalMe.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalMe.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonalityController : ControllerBase
{
    private readonly IPersonalityService _personalityService;

    public PersonalityController(IPersonalityService personalityService)
    {
        _personalityService = personalityService;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetDefaultPersonality()
    {
        var personalityResult = await _personalityService.GetPersonalityAsync();
        if (!personalityResult.IsSuccess)
        {
            return Ok(new {
                message = "No personalities configured yet",
                status = "empty",
                error = personalityResult.Error
            });
        }

        var personality = personalityResult.Value!;
        return Ok(new PersonalityProfileDto
        {
            Id = personality.Id,
            Name = personality.Name,
            Description = personality.Description,
            Traits = personality.Traits?.Select(t => new PersonalityTraitDto
            {
                Id = t.Id,
                Name = t.Name,
                Category = t.Category,
                Description = t.Description,
                Weight = t.Weight
            }).ToList() ?? new List<PersonalityTraitDto>()
        });
    }

    [HttpGet("system-prompt")]
    public async Task<ActionResult<object>> GetSystemPrompt()
    {
        var personalityResult = await _personalityService.GetPersonalityAsync();
        if (!personalityResult.IsSuccess)
        {
            return BadRequest(new { error = personalityResult.Error });
        }

        var systemPromptResult = _personalityService.GenerateSystemPrompt(personalityResult.Value!);
        if (!systemPromptResult.IsSuccess)
        {
            return BadRequest(new { error = systemPromptResult.Error });
        }

        return Ok(new { systemPrompt = systemPromptResult.Value });
    }

    [HttpGet("enhanced-system-prompt")]
    public async Task<ActionResult<object>> GetEnhancedSystemPrompt()
    {
        var enhancedPromptResult = await _personalityService.GenerateEnhancedSystemPromptAsync();
        if (!enhancedPromptResult.IsSuccess)
        {
            return BadRequest(new { error = enhancedPromptResult.Error });
        }

        return Ok(new { enhancedSystemPrompt = enhancedPromptResult.Value });
    }
}