using DigitalMe.Integrations.External.ClickUp.Models;

namespace DigitalMe.Integrations.External.ClickUp;

/// <summary>
/// Interface for ClickUp webhook processing service.
/// Handles incoming webhook events from ClickUp for real-time notifications.
/// </summary>
public interface IClickUpWebhookService
{
    /// <summary>
    /// Validates webhook signature for security.
    /// </summary>
    Task<bool> ValidateWebhookAsync(string signature, string payload);

    /// <summary>
    /// Processes incoming webhook payload.
    /// </summary>
    Task<bool> ProcessWebhookAsync(string payload);

    /// <summary>
    /// Handles task created event.
    /// </summary>
    Task HandleTaskCreatedAsync(ClickUpWebhookEvent webhookEvent);

    /// <summary>
    /// Handles task updated event.
    /// </summary>
    Task HandleTaskUpdatedAsync(ClickUpWebhookEvent webhookEvent);

    /// <summary>
    /// Handles task deleted event.
    /// </summary>
    Task HandleTaskDeletedAsync(ClickUpWebhookEvent webhookEvent);

    /// <summary>
    /// Handles task status changed event.
    /// </summary>
    Task HandleTaskStatusChangedAsync(ClickUpWebhookEvent webhookEvent);

    /// <summary>
    /// Handles task assignee changed event.
    /// </summary>
    Task HandleTaskAssigneeChangedAsync(ClickUpWebhookEvent webhookEvent);

    /// <summary>
    /// Handles task time tracked event.
    /// </summary>
    Task HandleTaskTimeTrackedAsync(ClickUpWebhookEvent webhookEvent);

    /// <summary>
    /// Handles task comment posted event.
    /// </summary>
    Task HandleTaskCommentPostedAsync(ClickUpWebhookEvent webhookEvent);

    /// <summary>
    /// Handles list created event.
    /// </summary>
    Task HandleListCreatedAsync(ClickUpWebhookEvent webhookEvent);

    /// <summary>
    /// Handles folder created event.
    /// </summary>
    Task HandleFolderCreatedAsync(ClickUpWebhookEvent webhookEvent);

    /// <summary>
    /// Handles space created event.
    /// </summary>
    Task HandleSpaceCreatedAsync(ClickUpWebhookEvent webhookEvent);

    /// <summary>
    /// Handles goal created event.
    /// </summary>
    Task HandleGoalCreatedAsync(ClickUpWebhookEvent webhookEvent);

    /// <summary>
    /// Handles key result created event.
    /// </summary>
    Task HandleKeyResultCreatedAsync(ClickUpWebhookEvent webhookEvent);
}

/// <summary>
/// ClickUp webhook event data structure.
/// </summary>
public class ClickUpWebhookEvent
{
    public string Event { get; set; } = string.Empty;
    public long TaskId { get; set; }
    public string WebhookId { get; set; } = string.Empty;
    public ClickUpTask? Task { get; set; }
    public ClickUpList? List { get; set; }
    public ClickUpFolder? Folder { get; set; }
    public ClickUpSpace? Space { get; set; }
    public Dictionary<string, object> HistoryItems { get; set; } = new();
}