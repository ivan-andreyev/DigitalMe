using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DigitalMe.Integrations.External.Slack;
using DigitalMe.Integrations.External.Slack.Models;
using System.Text;
using System.Text.Json;

namespace DigitalMe.Controllers;

/// <summary>
/// ASP.NET Core controller for handling Slack webhook endpoints
/// Processes incoming webhooks from Slack: events, interactive components, slash commands
/// </summary>
[ApiController]
[Route("api/webhooks/slack")]
public class SlackWebhookController : ControllerBase
{
    private readonly ILogger<SlackWebhookController> _logger;
    private readonly ISlackWebhookService _webhookService;
    private readonly IConfiguration _configuration;

    public SlackWebhookController(
        ILogger<SlackWebhookController> logger,
        ISlackWebhookService webhookService,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _webhookService = webhookService ?? throw new ArgumentNullException(nameof(webhookService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Main webhook endpoint for Slack events
    /// Handles event subscriptions, URL verification, and other JSON payloads
    /// </summary>
    [HttpPost("events")]
    [EnableRateLimiting("webhook")]
    public async Task<IActionResult> HandleEvents()
    {
        try
        {
            // Read the raw body
            string body;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
            }

            _logger.LogInformation("Received Slack webhook event. Content-Type: {ContentType}, Body length: {Length}",
                Request.ContentType, body.Length);

            // Validate webhook signature for security
            var signature = Request.Headers["X-Slack-Signature"].FirstOrDefault();
            var timestamp = Request.Headers["X-Slack-Request-Timestamp"].FirstOrDefault();
            var signingSecret = _configuration["Slack:SigningSecret"];

            if (!string.IsNullOrEmpty(signingSecret) && !string.IsNullOrEmpty(signature) && !string.IsNullOrEmpty(timestamp))
            {
                var isValidSignature = await _webhookService.ValidateWebhookSignatureAsync(
                    signature, timestamp, body, signingSecret);

                if (!isValidSignature)
                {
                    _logger.LogWarning("Invalid webhook signature received from IP: {RemoteIp}", 
                        Request.HttpContext.Connection.RemoteIpAddress);
                    return Unauthorized("Invalid signature");
                }
            }
            else if (!string.IsNullOrEmpty(signingSecret))
            {
                _logger.LogWarning("Missing signature headers in webhook request");
                return BadRequest("Missing signature headers");
            }

            // Handle URL verification challenge
            var urlVerificationResult = await _webhookService.HandleUrlVerificationAsync(body);
            if (urlVerificationResult != null)
            {
                _logger.LogInformation("URL verification challenge handled successfully");
                return Content(urlVerificationResult, "text/plain");
            }

            // Process the webhook payload
            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var result = await _webhookService.ProcessWebhookPayloadAsync(
                Request.ContentType ?? "application/json", body, headers);

            return Ok(result ?? new { status = "ok" });
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Invalid JSON in webhook payload");
            return BadRequest("Invalid JSON payload");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Slack webhook event");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Webhook endpoint for interactive components (buttons, menus, modals)
    /// Handles form-encoded payloads from Slack interactive elements
    /// </summary>
    [HttpPost("interactive")]
    [EnableRateLimiting("webhook")]
    public async Task<IActionResult> HandleInteractive()
    {
        try
        {
            // Read form data
            string body;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
            }

            _logger.LogInformation("Received Slack interactive component. Body length: {Length}", body.Length);

            // Validate webhook signature
            var signature = Request.Headers["X-Slack-Signature"].FirstOrDefault();
            var timestamp = Request.Headers["X-Slack-Request-Timestamp"].FirstOrDefault();
            var signingSecret = _configuration["Slack:SigningSecret"];

            if (!string.IsNullOrEmpty(signingSecret) && !string.IsNullOrEmpty(signature) && !string.IsNullOrEmpty(timestamp))
            {
                var isValidSignature = await _webhookService.ValidateWebhookSignatureAsync(
                    signature, timestamp, body, signingSecret);

                if (!isValidSignature)
                {
                    _logger.LogWarning("Invalid webhook signature for interactive component from IP: {RemoteIp}", 
                        Request.HttpContext.Connection.RemoteIpAddress);
                    return Unauthorized("Invalid signature");
                }
            }

            // Parse the form-encoded payload
            var formData = System.Web.HttpUtility.ParseQueryString(body);
            var payloadJson = formData["payload"];

            if (string.IsNullOrEmpty(payloadJson))
            {
                _logger.LogWarning("Missing payload in interactive component request");
                return BadRequest("Missing payload");
            }

            // Deserialize and handle the payload
            var payload = JsonSerializer.Deserialize<SlackInteractionPayload>(payloadJson);
            if (payload == null)
            {
                _logger.LogWarning("Failed to deserialize interactive component payload");
                return BadRequest("Invalid payload format");
            }

            var response = await _webhookService.HandleInteractiveComponentAsync(payload);
            
            if (response != null)
            {
                return Ok(response);
            }

            return Ok(new { status = "ok" });
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Invalid JSON in interactive component payload");
            return BadRequest("Invalid JSON payload");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing interactive component");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Webhook endpoint for slash commands
    /// Handles slash command requests from Slack
    /// </summary>
    [HttpPost("commands")]
    [EnableRateLimiting("webhook")]
    public async Task<IActionResult> HandleCommands()
    {
        try
        {
            // Read form data
            string body;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
            }

            _logger.LogInformation("Received Slack slash command. Body length: {Length}", body.Length);

            // Validate webhook signature
            var signature = Request.Headers["X-Slack-Signature"].FirstOrDefault();
            var timestamp = Request.Headers["X-Slack-Request-Timestamp"].FirstOrDefault();
            var signingSecret = _configuration["Slack:SigningSecret"];

            if (!string.IsNullOrEmpty(signingSecret) && !string.IsNullOrEmpty(signature) && !string.IsNullOrEmpty(timestamp))
            {
                var isValidSignature = await _webhookService.ValidateWebhookSignatureAsync(
                    signature, timestamp, body, signingSecret);

                if (!isValidSignature)
                {
                    _logger.LogWarning("Invalid webhook signature for slash command from IP: {RemoteIp}", 
                        Request.HttpContext.Connection.RemoteIpAddress);
                    return Unauthorized("Invalid signature");
                }
            }

            // Parse the form-encoded slash command data
            var formData = System.Web.HttpUtility.ParseQueryString(body);
            
            var command = new SlackSlashCommand
            {
                Token = formData["token"],
                TeamId = formData["team_id"],
                TeamDomain = formData["team_domain"],
                ChannelId = formData["channel_id"],
                ChannelName = formData["channel_name"],
                UserId = formData["user_id"],
                UserName = formData["user_name"],
                Command = formData["command"] ?? string.Empty,
                Text = formData["text"],
                ApiAppId = formData["api_app_id"],
                IsEnterpriseInstall = formData["is_enterprise_install"],
                ResponseUrl = formData["response_url"],
                TriggerId = formData["trigger_id"]
            };

            var response = await _webhookService.HandleSlashCommandAsync(command);
            
            if (response != null)
            {
                return Ok(response);
            }

            return Ok(new { text = "Command processed successfully!" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing slash command");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Health check endpoint for Slack webhook configuration
    /// </summary>
    [HttpGet("health")]
    [EnableRateLimiting("api")]
    public IActionResult Health()
    {
        return Ok(new 
        { 
            status = "healthy", 
            service = "Slack Webhook Handler",
            timestamp = DateTime.UtcNow,
            endpoints = new
            {
                events = "/api/webhooks/slack/events",
                interactive = "/api/webhooks/slack/interactive", 
                commands = "/api/webhooks/slack/commands"
            }
        });
    }

    /// <summary>
    /// Test endpoint for webhook functionality (development only)
    /// </summary>
    [HttpPost("test")]
    [EnableRateLimiting("webhook")]
    public async Task<IActionResult> TestWebhook([FromBody] object payload)
    {
        if (!_configuration.GetValue<bool>("Environment:IsDevelopment"))
        {
            return NotFound();
        }

        _logger.LogInformation("Test webhook endpoint called with payload: {Payload}", 
            JsonSerializer.Serialize(payload));

        return Ok(new 
        { 
            status = "test_ok", 
            message = "Test webhook received",
            receivedAt = DateTime.UtcNow 
        });
    }

    /// <summary>
    /// OAuth callback endpoint for Slack app installation
    /// </summary>
    [HttpGet("oauth/callback")]
    [EnableRateLimiting("api")]
    public async Task<IActionResult> OAuthCallback([FromQuery] string code, [FromQuery] string? state)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
            {
                _logger.LogWarning("OAuth callback received without code parameter");
                return BadRequest("Missing authorization code");
            }

            _logger.LogInformation("Slack OAuth callback received with code: {Code}", code);

            // Here you would typically exchange the code for an access token
            // This is a simplified implementation
            return Ok(new 
            { 
                status = "oauth_success", 
                message = "Slack app installation completed",
                timestamp = DateTime.UtcNow 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OAuth callback");
            return StatusCode(500, "OAuth processing failed");
        }
    }

    /// <summary>
    /// Endpoint to initiate Slack app installation
    /// </summary>
    [HttpGet("install")]
    [EnableRateLimiting("api")]
    public IActionResult Install()
    {
        var clientId = _configuration["Slack:ClientId"];
        var redirectUri = _configuration["Slack:RedirectUri"];
        
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
        {
            return BadRequest("Slack app not properly configured");
        }

        var scope = "app_mentions:read,channels:history,channels:read,chat:write,commands,files:read,reactions:read,users:read";
        var installUrl = $"https://slack.com/oauth/v2/authorize?client_id={clientId}&scope={scope}&redirect_uri={Uri.EscapeDataString(redirectUri)}";

        return Ok(new 
        { 
            install_url = installUrl,
            message = "Visit the install_url to add the DigitalMe bot to your Slack workspace"
        });
    }
}