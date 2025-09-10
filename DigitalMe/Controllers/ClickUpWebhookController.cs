using DigitalMe.Integrations.External.ClickUp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DigitalMe.Controllers;

/// <summary>
/// Controller for handling ClickUp webhook events.
/// Provides secure endpoints for receiving real-time notifications from ClickUp.
/// </summary>
[ApiController]
[Route("api/webhooks/clickup")]
public class ClickUpWebhookController : ControllerBase
{
    private readonly IClickUpWebhookService _webhookService;
    private readonly ILogger<ClickUpWebhookController> _logger;

    public ClickUpWebhookController(
        IClickUpWebhookService webhookService,
        ILogger<ClickUpWebhookController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// Receives ClickUp webhook events.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("webhook")]
    public async Task<IActionResult> ReceiveWebhook()
    {
        try
        {
            _logger.LogInformation("Received ClickUp webhook request from {RemoteAddress}",
                HttpContext.Connection.RemoteIpAddress);

            // Read the request body
            var body = await ReadRequestBodyAsync();
            if (string.IsNullOrEmpty(body))
            {
                _logger.LogWarning("ClickUp webhook request body is empty");
                return BadRequest("Request body is empty");
            }

            // Get signature header for validation
            var signature = Request.Headers["X-Signature"].FirstOrDefault();

            // Validate webhook signature
            var isValid = await _webhookService.ValidateWebhookAsync(signature ?? string.Empty, body);
            if (!isValid)
            {
                _logger.LogWarning("ClickUp webhook signature validation failed");
                return Unauthorized("Invalid webhook signature");
            }

            // Process the webhook
            var processed = await _webhookService.ProcessWebhookAsync(body);
            if (!processed)
            {
                _logger.LogError("Failed to process ClickUp webhook payload");
                return BadRequest("Failed to process webhook payload");
            }

            _logger.LogInformation("ClickUp webhook processed successfully");
            return Ok(new { status = "success", message = "Webhook processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ClickUp webhook");
            return StatusCode(500, new { status = "error", message = "Internal server error" });
        }
    }

    /// <summary>
    /// Handles ClickUp webhook verification/ping requests.
    /// </summary>
    [HttpGet]
    [EnableRateLimiting("api")]
    public IActionResult VerifyWebhook()
    {
        _logger.LogInformation("ClickUp webhook verification request received");

        // Return challenge parameter if provided (for webhook setup verification)
        var challenge = Request.Query["challenge"].FirstOrDefault();
        if (!string.IsNullOrEmpty(challenge))
        {
            _logger.LogInformation("Responding to ClickUp webhook challenge");
            return Ok(challenge);
        }

        return Ok(new
        {
            status = "active",
            service = "DigitalMe ClickUp Integration",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Health check endpoint for ClickUp webhook integration.
    /// </summary>
    [HttpGet("health")]
    [EnableRateLimiting("api")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            service = "ClickUp Webhook Service",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    /// <summary>
    /// Endpoint for testing ClickUp webhook processing (development only).
    /// </summary>
    [HttpPost("test")]
    [EnableRateLimiting("webhook")]
    public async Task<IActionResult> TestWebhook([FromBody] object testPayload)
    {
        if (!IsDevelopmentEnvironment())
        {
            return NotFound();
        }

        try
        {
            _logger.LogInformation("Testing ClickUp webhook processing");

            var json = System.Text.Json.JsonSerializer.Serialize(testPayload);
            var processed = await _webhookService.ProcessWebhookAsync(json);

            return Ok(new
            {
                status = processed ? "success" : "failed",
                message = processed ? "Test webhook processed successfully" : "Failed to process test webhook",
                payload = testPayload
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing ClickUp webhook");
            return StatusCode(500, new { status = "error", message = ex.Message });
        }
    }

    private async Task<string> ReadRequestBodyAsync()
    {
        using var reader = new StreamReader(Request.Body);
        return await reader.ReadToEndAsync();
    }

    private bool IsDevelopmentEnvironment()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
    }
}
