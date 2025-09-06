using Microsoft.AspNetCore.Mvc;
using DigitalMe.Services;
using DigitalMe.DTOs;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IMVPPersonalityService _personalityService;
    private readonly IMVPMessageProcessor _messageProcessor;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IMVPPersonalityService personalityService,
        IMVPMessageProcessor messageProcessor,
        ILogger<ChatController> logger)
    {
        _personalityService = personalityService;
        _messageProcessor = messageProcessor;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<ActionResult<MessageDto>> SendMessage([FromBody] ChatRequestDto request)
    {
        try
        {
            // Check if Ivan's personality is available
            var ivanProfile = await _personalityService.GetIvanProfileAsync();
            if (ivanProfile == null)
            {
                return BadRequest("Ivan's personality profile not found. Please create it first.");
            }

            // Process message through MVP pipeline
            var response = await _messageProcessor.ProcessMessageAsync(request.Message);

            // Return simplified response for MVP
            return Ok(new MessageDto
            {
                Id = Guid.NewGuid(),
                ConversationId = Guid.NewGuid(), // MVP: Simple conversation handling
                Role = "assistant",
                Content = response,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["platform"] = request.Platform,
                    ["userId"] = request.UserId,
                    ["processed_via"] = "MVP_Pipeline"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message: {Error}", ex.Message);
            return StatusCode(500, "An error occurred while processing your message");
        }
    }

    [HttpGet("status")]
    public async Task<ActionResult<object>> GetStatus()
    {
        var ivanProfile = await _personalityService.GetIvanProfileAsync();
        var personalityLoaded = ivanProfile != null;
        
        return Ok(new
        {
            McpConnected = true, // MVP: Always true for simplicity
            PersonalityLoaded = personalityLoaded,
            Status = personalityLoaded ? "Ready" : "Not Ready",
            Timestamp = DateTime.UtcNow
        });
    }
}