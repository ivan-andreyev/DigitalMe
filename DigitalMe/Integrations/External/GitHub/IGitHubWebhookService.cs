namespace DigitalMe.Integrations.External.GitHub;

/// <summary>
/// Interface for GitHub webhook processing service
/// </summary>
public interface IGitHubWebhookService
{
    /// <summary>
    /// Validate webhook signature using HMAC-SHA256
    /// </summary>
    Task<bool> ValidateSignatureAsync(string payload, string signature, string secret);

    /// <summary>
    /// Process incoming GitHub webhook
    /// </summary>
    Task<string> ProcessWebhookAsync(string eventType, string payload, string? deliveryId = null);

    /// <summary>
    /// Handle push events (commits, branches)
    /// </summary>
    Task<string> HandlePushEventAsync(string payload);

    /// <summary>
    /// Handle pull request events (opened, closed, merged, etc.)
    /// </summary>
    Task<string> HandlePullRequestEventAsync(string payload);

    /// <summary>
    /// Handle issue events (opened, closed, labeled, etc.)
    /// </summary>
    Task<string> HandleIssueEventAsync(string payload);

    /// <summary>
    /// Handle workflow run events (completed, failed, etc.)
    /// </summary>
    Task<string> HandleWorkflowRunEventAsync(string payload);

    /// <summary>
    /// Handle pull request review events (submitted, dismissed, etc.)
    /// </summary>
    Task<string> HandleReviewEventAsync(string payload);

    /// <summary>
    /// Handle repository events (created, deleted, archived, etc.)
    /// </summary>
    Task<string> HandleRepositoryEventAsync(string payload);

    /// <summary>
    /// Handle release events (published, edited, deleted)
    /// </summary>
    Task<string> HandleReleaseEventAsync(string payload);

    /// <summary>
    /// Handle fork events
    /// </summary>
    Task<string> HandleForkEventAsync(string payload);

    /// <summary>
    /// Handle star events
    /// </summary>
    Task<string> HandleStarEventAsync(string payload);

    /// <summary>
    /// Handle watch events
    /// </summary>
    Task<string> HandleWatchEventAsync(string payload);

    /// <summary>
    /// Handle unknown/generic events
    /// </summary>
    Task<string> HandleGenericEventAsync(string eventType, string payload);
}
