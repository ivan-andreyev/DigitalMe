using DigitalMe.Common.Exceptions;
using DigitalMe.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalMe.Controllers;

/// <summary>
/// Simple MVP Conversation Controller for Blazor UI integration.
/// SOLID-compliant with single responsibility: Handle HTTP requests/responses.
/// </summary>
[ApiController]
[Route("api/mvp/[controller]")]
public class MvpConversationController : ControllerBase
{
    private readonly IMvpMessageProcessor _messageProcessor;
    private readonly ILogger<MvpConversationController> _logger;

    public MvpConversationController(
        IMvpMessageProcessor messageProcessor,
        ILogger<MvpConversationController> logger)
    {
        _messageProcessor = messageProcessor ?? throw new ArgumentNullException(nameof(messageProcessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Send message to Ivan and get his personality-aware response
    /// </summary>
    /// <param name="request">Chat request with user message</param>
    /// <returns>Ivan's response</returns>
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] MvpChatRequest request)
    {
        try
        {
            if (request == null)
            {
                _logger.LogWarning("⚠️ Received null chat request");
                return BadRequest(new { error = "Request cannot be null" });
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                _logger.LogWarning("⚠️ Received empty message");
                return BadRequest(new { error = "Message cannot be empty" });
            }

            _logger.LogInformation("📨 Processing chat request (message length: {MessageLength})",
                request.Message.Length);

            var response = await _messageProcessor.ProcessMessageAsync(request.Message);

            var result = new MvpChatResponse
            {
                Response = response,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("✅ Chat response generated successfully (response length: {ResponseLength})",
                response.Length);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "⚠️ Bad request: {ErrorMessage}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (PersonalityServiceException ex)
        {
            _logger.LogError(ex, "💥 Personality service error");
            return StatusCode(503, new { error = "Ivan's personality is temporarily unavailable" });
        }
        catch (ExternalServiceException ex)
        {
            _logger.LogError(ex, "💥 External service error");
            return StatusCode(503, new { error = "AI service is temporarily unavailable" });
        }
        catch (MessageProcessingException ex)
        {
            _logger.LogError(ex, "💥 Message processing error");
            return StatusCode(500, new { error = "Failed to process your message" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Unexpected error in MVPConversationController");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Health check endpoint for the MVP conversation service
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        try
        {
            return Ok(new
            {
                status = "healthy",
                service = "MVPConversationController",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Health check failed");
            return StatusCode(500, new { status = "unhealthy" });
        }
    }
}

/// <summary>
/// Simple request model for MVP chat messages
/// </summary>
public class MvpChatRequest
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Simple response model for MVP chat responses
/// </summary>
public class MvpChatResponse
{
    public string Response { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
