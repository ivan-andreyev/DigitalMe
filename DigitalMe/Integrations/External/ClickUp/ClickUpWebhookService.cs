using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DigitalMe.Configuration;
using DigitalMe.Integrations.External.ClickUp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalMe.Integrations.External.ClickUp;

/// <summary>
/// Service for processing ClickUp webhook events with security validation and comprehensive event handling.
/// Provides real-time notifications for task management, time tracking, and project organization events.
/// </summary>
public class ClickUpWebhookService : IClickUpWebhookService
{
    private readonly ILogger<ClickUpWebhookService> _logger;
    private readonly ClickUpSettings _settings;
    private readonly JsonSerializerOptions _jsonOptions;

    public ClickUpWebhookService(
        ILogger<ClickUpWebhookService> logger,
        IOptions<ClickUpSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };
    }

    public async Task<bool> ValidateWebhookAsync(string signature, string payload)
    {
        try
        {
            if (string.IsNullOrEmpty(_settings.WebhookSecret))
            {
                _logger.LogWarning("ClickUp webhook secret not configured, skipping validation");
                return true; // Allow for development/testing
            }

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("No signature provided for ClickUp webhook validation");
                return false;
            }

            // Remove 'sha256=' prefix if present
            var cleanSignature = signature.StartsWith("sha256=") 
                ? signature.Substring("sha256=".Length) 
                : signature;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

            var isValid = string.Equals(cleanSignature, computedSignature, StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                _logger.LogWarning("ClickUp webhook signature validation failed");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating ClickUp webhook signature");
            return false;
        }
    }

    public async Task<bool> ProcessWebhookAsync(string payload)
    {
        try
        {
            _logger.LogInformation("Processing ClickUp webhook payload");

            var webhookEvent = JsonSerializer.Deserialize<ClickUpWebhookEvent>(payload, _jsonOptions);
            if (webhookEvent == null)
            {
                _logger.LogWarning("Failed to deserialize ClickUp webhook payload");
                return false;
            }

            _logger.LogInformation("Processing ClickUp webhook event: {EventType} for TaskId: {TaskId}", 
                webhookEvent.Event, webhookEvent.TaskId);

            await HandleWebhookEventAsync(webhookEvent);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ClickUp webhook payload");
            return false;
        }
    }

    private async Task HandleWebhookEventAsync(ClickUpWebhookEvent webhookEvent)
    {
        switch (webhookEvent.Event?.ToLowerInvariant())
        {
            case "taskcreated":
                await HandleTaskCreatedAsync(webhookEvent);
                break;
            case "taskupdated":
                await HandleTaskUpdatedAsync(webhookEvent);
                break;
            case "taskdeleted":
                await HandleTaskDeletedAsync(webhookEvent);
                break;
            case "taskstatuschanged":
                await HandleTaskStatusChangedAsync(webhookEvent);
                break;
            case "taskassigneechanged":
                await HandleTaskAssigneeChangedAsync(webhookEvent);
                break;
            case "tasktimetracked":
                await HandleTaskTimeTrackedAsync(webhookEvent);
                break;
            case "taskcommentposted":
                await HandleTaskCommentPostedAsync(webhookEvent);
                break;
            case "listcreated":
                await HandleListCreatedAsync(webhookEvent);
                break;
            case "foldercreated":
                await HandleFolderCreatedAsync(webhookEvent);
                break;
            case "spacecreated":
                await HandleSpaceCreatedAsync(webhookEvent);
                break;
            case "goalcreated":
                await HandleGoalCreatedAsync(webhookEvent);
                break;
            case "keyresultcreated":
                await HandleKeyResultCreatedAsync(webhookEvent);
                break;
            default:
                _logger.LogInformation("Unhandled ClickUp webhook event: {EventType}", webhookEvent.Event);
                break;
        }
    }

    public async Task HandleTaskCreatedAsync(ClickUpWebhookEvent webhookEvent)
    {
        _logger.LogInformation("ClickUp Task Created: {TaskId} - {TaskName}", 
            webhookEvent.TaskId, webhookEvent.Task?.Name);

        // TODO: Implement business logic for task creation
        // Examples:
        // - Send notification to assigned users
        // - Create corresponding entries in personality tracking
        // - Update project metrics
        // - Trigger automation workflows

        await Task.CompletedTask;
    }

    public async Task HandleTaskUpdatedAsync(ClickUpWebhookEvent webhookEvent)
    {
        _logger.LogInformation("ClickUp Task Updated: {TaskId} - {TaskName}", 
            webhookEvent.TaskId, webhookEvent.Task?.Name);

        // TODO: Implement business logic for task updates
        // Examples:
        // - Analyze what changed in history items
        // - Update time tracking records
        // - Send notifications for significant changes
        // - Update personality insights based on task progress

        await Task.CompletedTask;
    }

    public async Task HandleTaskDeletedAsync(ClickUpWebhookEvent webhookEvent)
    {
        _logger.LogInformation("ClickUp Task Deleted: {TaskId}", webhookEvent.TaskId);

        // TODO: Implement business logic for task deletion
        // Examples:
        // - Clean up related data
        // - Archive time entries
        // - Send notifications
        // - Update project metrics

        await Task.CompletedTask;
    }

    public async Task HandleTaskStatusChangedAsync(ClickUpWebhookEvent webhookEvent)
    {
        _logger.LogInformation("ClickUp Task Status Changed: {TaskId} - Status: {Status}", 
            webhookEvent.TaskId, webhookEvent.Task?.Status?.Status);

        // TODO: Implement business logic for status changes
        // Examples:
        // - Track productivity metrics
        // - Send notifications for completed tasks
        // - Update personality insights about work patterns
        // - Trigger next steps in workflow

        await Task.CompletedTask;
    }

    public async Task HandleTaskAssigneeChangedAsync(ClickUpWebhookEvent webhookEvent)
    {
        _logger.LogInformation("ClickUp Task Assignee Changed: {TaskId} - Assignees: {AssigneeCount}", 
            webhookEvent.TaskId, webhookEvent.Task?.Assignees?.Count ?? 0);

        // TODO: Implement business logic for assignee changes
        // Examples:
        // - Send notifications to new assignees
        // - Update workload tracking
        // - Analyze delegation patterns
        // - Update team collaboration insights

        await Task.CompletedTask;
    }

    public async Task HandleTaskTimeTrackedAsync(ClickUpWebhookEvent webhookEvent)
    {
        _logger.LogInformation("ClickUp Time Tracked: {TaskId} - Duration recorded", 
            webhookEvent.TaskId);

        // TODO: Implement business logic for time tracking
        // Examples:
        // - Update personal productivity analytics
        // - Calculate time estimates accuracy
        // - Generate time reports
        // - Analyze work patterns and efficiency

        await Task.CompletedTask;
    }

    public async Task HandleTaskCommentPostedAsync(ClickUpWebhookEvent webhookEvent)
    {
        _logger.LogInformation("ClickUp Comment Posted: {TaskId} - New comment added", 
            webhookEvent.TaskId);

        // TODO: Implement business logic for comments
        // Examples:
        // - Send notifications to stakeholders
        // - Analyze communication patterns
        // - Extract insights from comment content
        // - Update task collaboration metrics

        await Task.CompletedTask;
    }

    public async Task HandleListCreatedAsync(ClickUpWebhookEvent webhookEvent)
    {
        _logger.LogInformation("ClickUp List Created: {ListName} in Space: {SpaceName}", 
            webhookEvent.List?.Name, webhookEvent.Space?.Name);

        // TODO: Implement business logic for list creation
        // Examples:
        // - Track project structure changes
        // - Set up default templates
        // - Configure automation rules
        // - Update project organization insights

        await Task.CompletedTask;
    }

    public async Task HandleFolderCreatedAsync(ClickUpWebhookEvent webhookEvent)
    {
        _logger.LogInformation("ClickUp Folder Created: {FolderName} in Space: {SpaceName}", 
            webhookEvent.Folder?.Name, webhookEvent.Space?.Name);

        // TODO: Implement business logic for folder creation
        // Examples:
        // - Track organizational changes
        // - Set up folder-specific configurations
        // - Update project hierarchy
        // - Analyze project organization patterns

        await Task.CompletedTask;
    }

    public async Task HandleSpaceCreatedAsync(ClickUpWebhookEvent webhookEvent)
    {
        _logger.LogInformation("ClickUp Space Created: {SpaceName}", 
            webhookEvent.Space?.Name);

        // TODO: Implement business logic for space creation
        // Examples:
        // - Initialize space configuration
        // - Set up integrations and permissions
        // - Create default folder structure
        // - Update workspace organization tracking

        await Task.CompletedTask;
    }

    public async Task HandleGoalCreatedAsync(ClickUpWebhookEvent webhookEvent)
    {
        _logger.LogInformation("ClickUp Goal Created for Task: {TaskId}", 
            webhookEvent.TaskId);

        // TODO: Implement business logic for goal creation
        // Examples:
        // - Track goal-setting patterns
        // - Set up goal progress monitoring
        // - Connect goals to personality insights
        // - Update achievement tracking

        await Task.CompletedTask;
    }

    public async Task HandleKeyResultCreatedAsync(ClickUpWebhookEvent webhookEvent)
    {
        _logger.LogInformation("ClickUp Key Result Created for Task: {TaskId}", 
            webhookEvent.TaskId);

        // TODO: Implement business logic for key result creation
        // Examples:
        // - Monitor OKR progress
        // - Set up measurement tracking
        // - Connect to goal achievement analysis
        // - Update performance metrics

        await Task.CompletedTask;
    }
}