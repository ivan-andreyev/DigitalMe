using DigitalMe.Integrations.MCP;
using DigitalMe.Models;
using Microsoft.AspNetCore.Mvc;

namespace DigitalMe.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnthropicController : ControllerBase
{
    private readonly IAnthropicService _anthropicService;
    private readonly ILogger<AnthropicController> _logger;

    public AnthropicController(IAnthropicService anthropicService, ILogger<AnthropicController> logger)
    {
        _anthropicService = anthropicService;
        _logger = logger;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<object>> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            var response = await _anthropicService.SendMessageAsync(request.Message, request.Personality);
            return Ok(new { response, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to Anthropic API");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { message = "Anthropic integration is ready", timestamp = DateTime.UtcNow });
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public PersonalityProfile? Personality { get; set; }
}
