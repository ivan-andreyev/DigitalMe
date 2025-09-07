using Microsoft.AspNetCore.Mvc;
using DigitalMe.Services;
using DigitalMe.DTOs;
using DigitalMe.Models;

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
        try
        {
            // Return Ivan's personality as default for integration tests
            var personality = await _personalityService.GetPersonalityAsync("Ivan");
            if (personality == null)
            {
                return Ok(new { message = "No personalities configured yet", status = "empty" });
            }

            return Ok(new PersonalityProfileDto
            {
                Id = personality.Id,
                Name = personality.Name,
                Description = personality.Description,
                CreatedAt = personality.CreatedAt,
                UpdatedAt = personality.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<PersonalityProfileDto>> GetPersonality(string name)
    {
        var personality = await _personalityService.GetPersonalityAsync(name);
        if (personality == null)
            return NotFound($"Personality '{name}' not found");

        var traits = await _personalityService.GetPersonalityTraitsAsync(personality.Id);
        
        return Ok(new PersonalityProfileDto
        {
            Id = personality.Id,
            Name = personality.Name,
            Description = personality.Description,
            CreatedAt = personality.CreatedAt,
            UpdatedAt = personality.UpdatedAt,
            Traits = traits.Select(t => new PersonalityTraitDto
            {
                Id = t.Id,
                Category = t.Category,
                Name = t.Name,
                Description = t.Description,
                Weight = t.Weight,
                CreatedAt = t.CreatedAt
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<ActionResult<PersonalityProfileDto>> CreatePersonality([FromBody] CreatePersonalityProfileDto dto)
    {
        var personality = await _personalityService.CreatePersonalityAsync(dto.Name, dto.Description);
        
        return CreatedAtAction(nameof(GetPersonality), new { name = personality.Name }, new PersonalityProfileDto
        {
            Id = personality.Id,
            Name = personality.Name,
            Description = personality.Description,
            CreatedAt = personality.CreatedAt,
            UpdatedAt = personality.UpdatedAt,
            Traits = new List<PersonalityTraitDto>()
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PersonalityProfileDto>> UpdatePersonality(Guid id, [FromBody] UpdatePersonalityProfileDto dto)
    {
        try
        {
            var personality = await _personalityService.UpdatePersonalityAsync(id, dto.Description);
            var traits = await _personalityService.GetPersonalityTraitsAsync(personality.Id);
            
            return Ok(new PersonalityProfileDto
            {
                Id = personality.Id,
                Name = personality.Name,
                Description = personality.Description,
                CreatedAt = personality.CreatedAt,
                UpdatedAt = personality.UpdatedAt,
                Traits = traits.Select(t => new PersonalityTraitDto
                {
                    Id = t.Id,
                    Category = t.Category,
                    Name = t.Name,
                    Description = t.Description,
                    Weight = t.Weight,
                    CreatedAt = t.CreatedAt
                }).ToList()
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{personalityId}/traits")]
    public async Task<ActionResult<PersonalityTraitDto>> AddTrait(Guid personalityId, [FromBody] CreatePersonalityTraitDto dto)
    {
        var trait = await _personalityService.AddTraitAsync(personalityId, dto.Category, dto.Name, dto.Description, dto.Weight);
        
        return Ok(new PersonalityTraitDto
        {
            Id = trait.Id,
            Category = trait.Category,
            Name = trait.Name,
            Description = trait.Description,
            Weight = trait.Weight,
            CreatedAt = trait.CreatedAt
        });
    }

    [HttpGet("{personalityId}/system-prompt")]
    public async Task<ActionResult<string>> GetSystemPrompt(Guid personalityId)
    {
        try
        {
            var systemPrompt = await _personalityService.GenerateSystemPromptAsync(personalityId);
            return Ok(systemPrompt);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePersonality(Guid id)
    {
        var deleted = await _personalityService.DeletePersonalityAsync(id);
        if (!deleted)
            return NotFound($"Personality with ID {id} not found");

        return NoContent();
    }
}