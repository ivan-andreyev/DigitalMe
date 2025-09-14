using DigitalMe.Integrations.External.Slack.Models;

namespace DigitalMe.Integrations.External.Slack;

/// <summary>
/// Interface for Slack webhook handling service
/// Handles incoming webhooks from Slack: events, interactive components, slash commands
/// </summary>
public interface ISlackWebhookService
{
    // Webhook signature validation
    Task<bool> ValidateWebhookSignatureAsync(string signature, string timestamp, string body, string signingSecret);

    // URL verification for Slack app setup
    Task<string?> HandleUrlVerificationAsync(string body);

    // Event subscriptions handling
    Task<bool> HandleEventAsync(SlackWebhookEvent webhookEvent);

    // Interactive components handling (buttons, menu selections, modals)
    Task<SlackInteractionResponse?> HandleInteractiveComponentAsync(SlackInteractionPayload payload);

    // Slash commands handling
    Task<SlackSlashCommandResponse?> HandleSlashCommandAsync(SlackSlashCommand command);

    // Generic webhook payload processing
    Task<object?> ProcessWebhookPayloadAsync(string contentType, string body, IDictionary<string, string> headers);

    // Event routing based on event type
    Task<bool> RouteEventAsync(string eventType, object eventData);

    // Response helpers for different webhook types
    SlackInteractionResponse CreateButtonResponse(string text, bool replaceOriginal = false);
    SlackInteractionResponse CreateModalResponse(SlackModal modal);
    SlackSlashCommandResponse CreateSlashCommandResponse(string text, bool ephemeral = true);
}
