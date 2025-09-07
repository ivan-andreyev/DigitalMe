using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using DigitalMe.Configuration;
using DigitalMe.Integrations.External.GitHub;
using System.Text;

namespace DigitalMe.Controllers;

/// <summary>
/// Controller for handling GitHub webhook events
/// </summary>
[ApiController]
[Route("api/webhooks/github")]
public class GitHubWebhookController : ControllerBase
{
    private readonly IGitHubWebhookService _webhookService;
    private readonly GitHubSettings _settings;
    private readonly ILogger<GitHubWebhookController> _logger;

    public GitHubWebhookController(
        IGitHubWebhookService webhookService,
        IOptions<IntegrationSettings> integrationSettings,
        ILogger<GitHubWebhookController> logger)
    {
        _webhookService = webhookService;
        _settings = integrationSettings.Value.GitHub;
        _logger = logger;
    }

    /// <summary>
    /// Main webhook endpoint for GitHub events
    /// </summary>
    [HttpPost]
    [EnableRateLimiting("webhook")]
    public async Task<IActionResult> ReceiveWebhook()
    {
        try
        {
            // Read the raw request body
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var payload = await reader.ReadToEndAsync();

            // Get headers
            var eventType = Request.Headers["X-GitHub-Event"].FirstOrDefault();
            var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
            var deliveryId = Request.Headers["X-GitHub-Delivery"].FirstOrDefault();
            var userAgent = Request.Headers["User-Agent"].FirstOrDefault();

            _logger.LogInformation("Received GitHub webhook - Event: {EventType}, DeliveryId: {DeliveryId}, UserAgent: {UserAgent}", 
                eventType, deliveryId, userAgent);

            // Validate required headers
            if (string.IsNullOrEmpty(eventType))
            {
                _logger.LogWarning("Missing X-GitHub-Event header");
                return BadRequest("Missing X-GitHub-Event header");
            }

            // Validate user agent (GitHub webhooks have specific user agent)
            if (string.IsNullOrEmpty(userAgent) || !userAgent.StartsWith("GitHub-Hookshot/"))
            {
                _logger.LogWarning("Invalid or missing User-Agent header: {UserAgent}", userAgent);
                return BadRequest("Invalid User-Agent");
            }

            // Validate payload
            if (string.IsNullOrEmpty(payload))
            {
                _logger.LogWarning("Empty webhook payload received");
                return BadRequest("Empty payload");
            }

            // Validate webhook signature if secret is configured
            if (!string.IsNullOrEmpty(_settings.WebhookSecret))
            {
                if (string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("Missing webhook signature but secret is configured");
                    return BadRequest("Missing signature");
                }

                var isValidSignature = await _webhookService.ValidateSignatureAsync(payload, signature, _settings.WebhookSecret);
                if (!isValidSignature)
                {
                    _logger.LogWarning("Invalid webhook signature for delivery {DeliveryId}", deliveryId);
                    return Unauthorized("Invalid signature");
                }
            }
            else
            {
                _logger.LogWarning("Webhook secret not configured - signature validation skipped");
            }

            // Process the webhook
            var result = await _webhookService.ProcessWebhookAsync(eventType, payload, deliveryId);

            _logger.LogInformation("Successfully processed GitHub webhook - Event: {EventType}, DeliveryId: {DeliveryId}, Result: {Result}", 
                eventType, deliveryId, result);

            return Ok(new
            {
                success = true,
                eventType,
                deliveryId,
                message = result,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GitHub webhook: {Message}", ex.Message);
            
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Health check endpoint for GitHub webhooks
    /// </summary>
    [HttpGet("health")]
    [EnableRateLimiting("api")]
    public IActionResult Health()
    {
        return Ok(new
        {
            service = "GitHub Webhook Service",
            status = "healthy",
            enabled = _settings.Enabled,
            hasSecret = !string.IsNullOrEmpty(_settings.WebhookSecret),
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Configuration endpoint to check webhook settings
    /// </summary>
    [HttpGet("config")]
    [EnableRateLimiting("api")]
    public IActionResult Config()
    {
        return Ok(new
        {
            enabled = _settings.Enabled,
            baseUrl = _settings.BaseUrl,
            organization = _settings.Organization,
            hasToken = !string.IsNullOrEmpty(_settings.PersonalAccessToken),
            hasWebhookSecret = !string.IsNullOrEmpty(_settings.WebhookSecret),
            timeoutSeconds = _settings.TimeoutSeconds,
            maxRetries = _settings.MaxRetries,
            rateLimitPerMinute = _settings.RateLimitPerMinute
        });
    }

    /// <summary>
    /// Test endpoint for validating webhook configuration
    /// </summary>
    [HttpPost("test")]
    [EnableRateLimiting("webhook")]
    public async Task<IActionResult> TestWebhook([FromBody] TestWebhookRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.EventType))
            {
                return BadRequest("EventType is required");
            }

            if (string.IsNullOrEmpty(request.Payload))
            {
                return BadRequest("Payload is required");
            }

            // Test signature validation if provided
            if (!string.IsNullOrEmpty(request.Signature) && !string.IsNullOrEmpty(_settings.WebhookSecret))
            {
                var isValid = await _webhookService.ValidateSignatureAsync(request.Payload, request.Signature, _settings.WebhookSecret);
                if (!isValid)
                {
                    return BadRequest("Invalid signature");
                }
            }

            // Process test webhook
            var result = await _webhookService.ProcessWebhookAsync(request.EventType, request.Payload, "test-delivery");

            return Ok(new
            {
                success = true,
                eventType = request.EventType,
                result,
                signatureValid = !string.IsNullOrEmpty(request.Signature) && !string.IsNullOrEmpty(_settings.WebhookSecret),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing GitHub webhook: {Message}", ex.Message);
            
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}

/// <summary>
/// Request model for testing webhooks
/// </summary>
public class TestWebhookRequest
{
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string? Signature { get; set; }
}