using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DigitalMe.Integrations.External.Slack.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalMe.Integrations.External.Slack;

/// <summary>
/// Service for handling Slack webhook events and interactions
/// Processes incoming webhooks: events, interactive components, slash commands
/// </summary>
public class SlackWebhookService : ISlackWebhookService
{
    private readonly ILogger<SlackWebhookService> _logger;
    private readonly ISlackService _slackService;
    private readonly SlackConfiguration _config;

    public SlackWebhookService(
        ILogger<SlackWebhookService> logger,
        ISlackService slackService,
        IOptions<SlackConfiguration> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _slackService = slackService ?? throw new ArgumentNullException(nameof(slackService));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Validates Slack webhook signature for security
    /// </summary>
    public async Task<bool> ValidateWebhookSignatureAsync(string signature, string timestamp, string body, string signingSecret)
    {
        try
        {
            if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(body))
            {
                _logger.LogWarning("Missing required parameters for signature validation");
                return false;
            }

            // Check timestamp to prevent replay attacks (should be within 5 minutes)
            var requestTimestamp = long.Parse(timestamp);
            var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(currentTimestamp - requestTimestamp) > 300) // 5 minutes
            {
                _logger.LogWarning("Request timestamp too old: {RequestTime} vs {CurrentTime}", 
                    requestTimestamp, currentTimestamp);
                return false;
            }

            // Create the signature base string
            var signatureBaseString = $"v0:{timestamp}:{body}";
            
            // Calculate HMAC-SHA256
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingSecret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureBaseString));
            var computedSignature = $"v0={Convert.ToHexString(computedHash).ToLower()}";

            var isValid = signature.Equals(computedSignature, StringComparison.OrdinalIgnoreCase);
            
            if (!isValid)
            {
                _logger.LogWarning("Webhook signature validation failed. Expected: {Expected}, Received: {Received}",
                    computedSignature, signature);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating webhook signature");
            return false;
        }
    }

    /// <summary>
    /// Handles URL verification challenge from Slack during app setup
    /// </summary>
    public async Task<string?> HandleUrlVerificationAsync(string body)
    {
        try
        {
            var webhookEvent = JsonSerializer.Deserialize<SlackWebhookEvent>(body);
            
            if (webhookEvent?.Type == "url_verification" && !string.IsNullOrEmpty(webhookEvent.Challenge))
            {
                _logger.LogInformation("Handling URL verification challenge");
                return webhookEvent.Challenge;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling URL verification");
            return null;
        }
    }

    /// <summary>
    /// Processes event subscription webhooks from Slack
    /// </summary>
    public async Task<bool> HandleEventAsync(SlackWebhookEvent webhookEvent)
    {
        try
        {
            if (webhookEvent?.Event == null)
            {
                _logger.LogWarning("Received webhook event with null event data");
                return false;
            }

            _logger.LogInformation("Handling Slack event: {EventType} from user {UserId} in channel {ChannelId}",
                webhookEvent.Event.Type, webhookEvent.Event.User, webhookEvent.Event.Channel);

            // Route the event based on type
            return await RouteEventAsync(webhookEvent.Event.Type, webhookEvent.Event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling webhook event");
            return false;
        }
    }

    /// <summary>
    /// Handles interactive component payloads (buttons, menus, modals)
    /// </summary>
    public async Task<SlackInteractionResponse?> HandleInteractiveComponentAsync(SlackInteractionPayload payload)
    {
        try
        {
            _logger.LogInformation("Handling interactive component: {Type} from user {UserId}",
                payload.Type, payload.User?.Id);

            return payload.Type switch
            {
                "block_actions" => await HandleBlockActionsAsync(payload),
                "interactive_message" => await HandleInteractiveMessageAsync(payload),
                "dialog_submission" => await HandleDialogSubmissionAsync(payload),
                "view_submission" => await HandleViewSubmissionAsync(payload),
                "view_closed" => await HandleViewClosedAsync(payload),
                _ => CreateButtonResponse($"Unknown interaction type: {payload.Type}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling interactive component");
            return CreateButtonResponse("Sorry, an error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Handles slash command requests
    /// </summary>
    public async Task<SlackSlashCommandResponse?> HandleSlashCommandAsync(SlackSlashCommand command)
    {
        try
        {
            _logger.LogInformation("Handling slash command: {Command} with text '{Text}' from user {UserId}",
                command.Command, command.Text, command.UserId);

            return command.Command switch
            {
                "/digitalme" => await HandleDigitalMeCommandAsync(command),
                "/help" => CreateSlashCommandResponse("Available commands:\n• /digitalme - Interact with DigitalMe\n• /help - Show this help"),
                _ => CreateSlashCommandResponse($"Unknown command: {command.Command}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling slash command: {Command}", command.Command);
            return CreateSlashCommandResponse("Sorry, an error occurred while processing your command.");
        }
    }

    /// <summary>
    /// Generic webhook payload processor
    /// </summary>
    public async Task<object?> ProcessWebhookPayloadAsync(string contentType, string body, IDictionary<string, string> headers)
    {
        try
        {
            _logger.LogInformation("Processing webhook payload with content type: {ContentType}", contentType);

            // Handle URL verification first
            var urlVerificationResult = await HandleUrlVerificationAsync(body);
            if (urlVerificationResult != null)
            {
                return urlVerificationResult;
            }

            // Handle different content types
            return contentType.ToLower() switch
            {
                "application/json" => await ProcessJsonPayloadAsync(body),
                "application/x-www-form-urlencoded" => await ProcessFormPayloadAsync(body),
                _ => throw new NotSupportedException($"Content type {contentType} is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook payload");
            return null;
        }
    }

    /// <summary>
    /// Routes events to specific handlers based on event type
    /// </summary>
    public async Task<bool> RouteEventAsync(string eventType, object eventData)
    {
        try
        {
            var slackEvent = eventData as SlackEvent;
            if (slackEvent == null)
            {
                _logger.LogWarning("Event data is not of type SlackEvent");
                return false;
            }

            return eventType switch
            {
                "message" => await HandleMessageEventAsync(slackEvent),
                "app_mention" => await HandleAppMentionEventAsync(slackEvent),
                "reaction_added" => await HandleReactionAddedEventAsync(slackEvent),
                "reaction_removed" => await HandleReactionRemovedEventAsync(slackEvent),
                "channel_created" => await HandleChannelCreatedEventAsync(slackEvent),
                "channel_deleted" => await HandleChannelDeletedEventAsync(slackEvent),
                "member_joined_channel" => await HandleMemberJoinedChannelEventAsync(slackEvent),
                "member_left_channel" => await HandleMemberLeftChannelEventAsync(slackEvent),
                "file_shared" => await HandleFileSharedEventAsync(slackEvent),
                _ => await HandleUnknownEventAsync(eventType, slackEvent)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing event: {EventType}", eventType);
            return false;
        }
    }

    /// <summary>
    /// Creates a button interaction response
    /// </summary>
    public SlackInteractionResponse CreateButtonResponse(string text, bool replaceOriginal = false)
    {
        return new SlackInteractionResponse
        {
            ResponseType = "ephemeral",
            Text = text,
            ReplaceOriginal = replaceOriginal
        };
    }

    /// <summary>
    /// Creates a modal interaction response
    /// </summary>
    public SlackInteractionResponse CreateModalResponse(SlackModal modal)
    {
        return new SlackInteractionResponse
        {
            // Modal responses are handled differently in Slack API
            // This would typically trigger a views.open API call
        };
    }

    /// <summary>
    /// Creates a slash command response
    /// </summary>
    public SlackSlashCommandResponse CreateSlashCommandResponse(string text, bool ephemeral = true)
    {
        return new SlackSlashCommandResponse
        {
            ResponseType = ephemeral ? "ephemeral" : "in_channel",
            Text = text
        };
    }

    // Private helper methods

    private async Task<object?> ProcessJsonPayloadAsync(string body)
    {
        var webhookEvent = JsonSerializer.Deserialize<SlackWebhookEvent>(body);
        if (webhookEvent != null)
        {
            await HandleEventAsync(webhookEvent);
        }
        return new { status = "ok" };
    }

    private async Task<object?> ProcessFormPayloadAsync(string body)
    {
        // Parse form-encoded payload (typically for interactive components and slash commands)
        var formData = System.Web.HttpUtility.ParseQueryString(body);
        
        if (formData["payload"] != null)
        {
            // Interactive component
            var payload = JsonSerializer.Deserialize<SlackInteractionPayload>(formData["payload"]!);
            if (payload != null)
            {
                return await HandleInteractiveComponentAsync(payload);
            }
        }
        else
        {
            // Slash command
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
            
            return await HandleSlashCommandAsync(command);
        }

        return new { status = "ok" };
    }

    // Event handlers
    private Task<bool> HandleMessageEventAsync(SlackEvent slackEvent)
    {
        _logger.LogInformation("Handling message event from user {UserId} in channel {ChannelId}: {Text}",
            slackEvent.User, slackEvent.Channel, slackEvent.Text);

        // Ignore bot messages to prevent loops
        if (slackEvent.Subtype == "bot_message")
        {
            return Task.FromResult(true);
        }

        // Process the message - this is where you'd integrate with your AI/personality engine
        // For now, just log it
        return Task.FromResult(true);
    }

    private async Task<bool> HandleAppMentionEventAsync(SlackEvent slackEvent)
    {
        _logger.LogInformation("Bot mentioned by user {UserId} in channel {ChannelId}: {Text}",
            slackEvent.User, slackEvent.Channel, slackEvent.Text);

        // Respond to the mention
        if (!string.IsNullOrEmpty(slackEvent.Channel))
        {
            await _slackService.SendMessageAsync(slackEvent.Channel, 
                $"Hello <@{slackEvent.User}>! You mentioned me. How can I help you?");
        }

        return true;
    }

    private Task<bool> HandleReactionAddedEventAsync(SlackEvent slackEvent)
    {
        _logger.LogInformation("Reaction '{Reaction}' added by user {UserId} to message in channel {ChannelId}",
            slackEvent.Reaction, slackEvent.User, slackEvent.Item?.Channel);
        return Task.FromResult(true);
    }

    private Task<bool> HandleReactionRemovedEventAsync(SlackEvent slackEvent)
    {
        _logger.LogInformation("Reaction '{Reaction}' removed by user {UserId} from message in channel {ChannelId}",
            slackEvent.Reaction, slackEvent.User, slackEvent.Item?.Channel);
        return Task.FromResult(true);
    }

    private Task<bool> HandleChannelCreatedEventAsync(SlackEvent slackEvent)
    {
        _logger.LogInformation("Channel created: {ChannelId}", slackEvent.Channel);
        return Task.FromResult(true);
    }

    private Task<bool> HandleChannelDeletedEventAsync(SlackEvent slackEvent)
    {
        _logger.LogInformation("Channel deleted: {ChannelId}", slackEvent.Channel);
        return Task.FromResult(true);
    }

    private Task<bool> HandleMemberJoinedChannelEventAsync(SlackEvent slackEvent)
    {
        _logger.LogInformation("User {UserId} joined channel {ChannelId}", slackEvent.User, slackEvent.Channel);
        return Task.FromResult(true);
    }

    private Task<bool> HandleMemberLeftChannelEventAsync(SlackEvent slackEvent)
    {
        _logger.LogInformation("User {UserId} left channel {ChannelId}", slackEvent.User, slackEvent.Channel);
        return Task.FromResult(true);
    }

    private Task<bool> HandleFileSharedEventAsync(SlackEvent slackEvent)
    {
        _logger.LogInformation("File shared by user {UserId} in channel {ChannelId}", 
            slackEvent.User, slackEvent.Channel);
        return Task.FromResult(true);
    }

    private Task<bool> HandleUnknownEventAsync(string eventType, SlackEvent slackEvent)
    {
        _logger.LogWarning("Unknown event type: {EventType}", eventType);
        return Task.FromResult(true);
    }

    // Interactive component handlers
    private async Task<SlackInteractionResponse?> HandleBlockActionsAsync(SlackInteractionPayload payload)
    {
        var action = payload.Actions?.FirstOrDefault();
        if (action == null)
        {
            return CreateButtonResponse("No action found in payload.");
        }

        _logger.LogInformation("Handling block action: {ActionId} with value: {Value}", 
            action.ActionId, action.Value);

        return action.ActionId switch
        {
            "help_button" => CreateButtonResponse("Here's some help information!", true),
            "settings_button" => CreateButtonResponse("Settings panel would open here.", true),
            _ => CreateButtonResponse($"Action {action.ActionId} handled successfully!")
        };
    }

    private async Task<SlackInteractionResponse?> HandleInteractiveMessageAsync(SlackInteractionPayload payload)
    {
        _logger.LogInformation("Handling interactive message");
        return CreateButtonResponse("Interactive message handled.");
    }

    private async Task<SlackInteractionResponse?> HandleDialogSubmissionAsync(SlackInteractionPayload payload)
    {
        _logger.LogInformation("Handling dialog submission");
        return CreateButtonResponse("Dialog submission processed.");
    }

    private async Task<SlackInteractionResponse?> HandleViewSubmissionAsync(SlackInteractionPayload payload)
    {
        _logger.LogInformation("Handling view submission");
        return null; // View submissions typically don't return content
    }

    private async Task<SlackInteractionResponse?> HandleViewClosedAsync(SlackInteractionPayload payload)
    {
        _logger.LogInformation("Handling view closed");
        return null; // View closed events don't return content
    }

    // Slash command handlers
    private async Task<SlackSlashCommandResponse?> HandleDigitalMeCommandAsync(SlackSlashCommand command)
    {
        if (string.IsNullOrEmpty(command.Text))
        {
            return CreateSlashCommandResponse(
                "Hello! I'm DigitalMe. You can interact with me by typing:\n" +
                "• `/digitalme help` - Show available commands\n" +
                "• `/digitalme chat <message>` - Chat with me\n" +
                "• `/digitalme status` - Check my status");
        }

        var args = command.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var subCommand = args.FirstOrDefault()?.ToLower();

        return subCommand switch
        {
            "help" => CreateSlashCommandResponse(
                "DigitalMe Commands:\n" +
                "• `help` - Show this help\n" +
                "• `chat <message>` - Chat with me\n" +
                "• `status` - Check my status\n" +
                "• `settings` - Configure settings"),
            
            "chat" => await HandleChatCommandAsync(string.Join(" ", args.Skip(1))),
            
            "status" => CreateSlashCommandResponse("DigitalMe is online and ready to help!"),
            
            "settings" => CreateSlashCommandResponse("Settings panel would be displayed here."),
            
            _ => CreateSlashCommandResponse($"Unknown subcommand: {subCommand}. Type `/digitalme help` for available commands.")
        };
    }

    private async Task<SlackSlashCommandResponse> HandleChatCommandAsync(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return CreateSlashCommandResponse("Please provide a message to chat with me!");
        }

        // This is where you'd integrate with your AI/personality engine
        // For now, just echo back with a simple response
        var response = $"You said: '{message}'. I'm processing this with my personality engine...";
        
        return CreateSlashCommandResponse(response);
    }
}

/// <summary>
/// Configuration class for Slack webhook service
/// </summary>
public class SlackConfiguration
{
    public string BotToken { get; set; } = string.Empty;
    public string SigningSecret { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}