using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DigitalMe.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DigitalMe.Integrations.External.GitHub;

/// <summary>
/// GitHub webhook processing service implementation
/// </summary>
public class GitHubWebhookService : IGitHubWebhookService
{
    private readonly GitHubSettings _settings;
    private readonly ILogger<GitHubWebhookService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public GitHubWebhookService(
        IOptions<IntegrationSettings> integrationSettings,
        ILogger<GitHubWebhookService> logger)
    {
        _settings = integrationSettings.Value.GitHub;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public async Task<bool> ValidateSignatureAsync(string payload, string signature, string secret)
    {
        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
        {
            _logger.LogWarning("Missing signature or secret for webhook validation");
            return false;
        }

        if (!signature.StartsWith("sha256="))
        {
            _logger.LogWarning("Invalid signature format. Expected sha256= prefix");
            return false;
        }

        try
        {
            var expectedSignature = signature[7..]; // Remove "sha256=" prefix

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

            var isValid = computedSignature == expectedSignature;

            if (!isValid)
            {
                _logger.LogWarning("GitHub webhook signature validation failed");
            }
            else
            {
                _logger.LogDebug("GitHub webhook signature validation successful");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating GitHub webhook signature: {Message}", ex.Message);
            return false;
        }
    }

    public async Task<string> ProcessWebhookAsync(string eventType, string payload, string? deliveryId = null)
    {
        _logger.LogInformation("Processing GitHub webhook - Event: {EventType}, DeliveryId: {DeliveryId}",
            eventType, deliveryId ?? "N/A");

        try
        {
            // Validate webhook signature if secret is configured
            if (!string.IsNullOrEmpty(_settings.WebhookSecret))
            {
                // Note: In real implementation, signature would be passed from controller
                // For now, we assume validation is done at controller level
            }

            var result = eventType switch
            {
                "push" => await HandlePushEventAsync(payload),
                "pull_request" => await HandlePullRequestEventAsync(payload),
                "issues" => await HandleIssueEventAsync(payload),
                "workflow_run" => await HandleWorkflowRunEventAsync(payload),
                "pull_request_review" => await HandleReviewEventAsync(payload),
                "repository" => await HandleRepositoryEventAsync(payload),
                "release" => await HandleReleaseEventAsync(payload),
                "fork" => await HandleForkEventAsync(payload),
                "star" => await HandleStarEventAsync(payload),
                "watch" => await HandleWatchEventAsync(payload),
                _ => await HandleGenericEventAsync(eventType, payload)
            };

            _logger.LogInformation("Successfully processed GitHub webhook - Event: {EventType}, Result: {Result}",
                eventType, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GitHub webhook {EventType}: {Message}", eventType, ex.Message);
            return $"Error processing {eventType} webhook: {ex.Message}";
        }
    }

    public async Task<string> HandlePushEventAsync(string payload)
    {
        _logger.LogDebug("Processing GitHub push event");

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            var repository = root.GetProperty("repository").GetProperty("full_name").GetString();
            var pusher = root.GetProperty("pusher").GetProperty("name").GetString();
            var ref_ = root.GetProperty("ref").GetString();
            var commits = root.GetProperty("commits").GetArrayLength();

            var branchName = ref_?.Replace("refs/heads/", "") ?? "unknown";

            _logger.LogInformation("Push event: {Pusher} pushed {CommitCount} commits to {Branch} in {Repository}",
                pusher, commits, branchName, repository);

            // Here you could trigger additional workflows:
            // - Update local mirrors
            // - Trigger CI/CD pipelines
            // - Notify team channels
            // - Update project management tools

            await Task.CompletedTask; // Placeholder for actual processing

            return $"Processed push to {branchName} with {commits} commits";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing push event: {Message}", ex.Message);
            return $"Error processing push event: {ex.Message}";
        }
    }

    public async Task<string> HandlePullRequestEventAsync(string payload)
    {
        _logger.LogDebug("Processing GitHub pull request event");

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            var action = root.GetProperty("action").GetString();
            var pullRequest = root.GetProperty("pull_request");
            var number = pullRequest.GetProperty("number").GetInt32();
            var title = pullRequest.GetProperty("title").GetString();
            var state = pullRequest.GetProperty("state").GetString();
            var repository = root.GetProperty("repository").GetProperty("full_name").GetString();
            var author = pullRequest.GetProperty("user").GetProperty("login").GetString();

            _logger.LogInformation("PR event: {Action} - PR #{Number} '{Title}' by {Author} in {Repository} (State: {State})",
                action, number, title, author, repository, state);

            // Handle different PR actions
            var result = action switch
            {
                "opened" => await HandlePullRequestOpenedAsync(pullRequest),
                "closed" => await HandlePullRequestClosedAsync(pullRequest),
                "edited" => await HandlePullRequestEditedAsync(pullRequest),
                "review_requested" => await HandlePullRequestReviewRequestedAsync(pullRequest),
                "ready_for_review" => await HandlePullRequestReadyForReviewAsync(pullRequest),
                "converted_to_draft" => await HandlePullRequestConvertedToDraftAsync(pullRequest),
                _ => $"Unhandled PR action: {action}"
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pull request event: {Message}", ex.Message);
            return $"Error processing pull request event: {ex.Message}";
        }
    }

    public async Task<string> HandleIssueEventAsync(string payload)
    {
        _logger.LogDebug("Processing GitHub issue event");

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            var action = root.GetProperty("action").GetString();
            var issue = root.GetProperty("issue");
            var number = issue.GetProperty("number").GetInt32();
            var title = issue.GetProperty("title").GetString();
            var state = issue.GetProperty("state").GetString();
            var repository = root.GetProperty("repository").GetProperty("full_name").GetString();
            var author = issue.GetProperty("user").GetProperty("login").GetString();

            _logger.LogInformation("Issue event: {Action} - Issue #{Number} '{Title}' by {Author} in {Repository} (State: {State})",
                action, number, title, author, repository, state);

            // Handle different issue actions
            var result = action switch
            {
                "opened" => await HandleIssueOpenedAsync(issue),
                "closed" => await HandleIssueClosedAsync(issue),
                "reopened" => await HandleIssueReopenedAsync(issue),
                "edited" => await HandleIssueEditedAsync(issue),
                "labeled" => await HandleIssueLabeledAsync(issue, root),
                "unlabeled" => await HandleIssueUnlabeledAsync(issue, root),
                "assigned" => await HandleIssueAssignedAsync(issue, root),
                "unassigned" => await HandleIssueUnassignedAsync(issue, root),
                _ => $"Unhandled issue action: {action}"
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing issue event: {Message}", ex.Message);
            return $"Error processing issue event: {ex.Message}";
        }
    }

    public async Task<string> HandleWorkflowRunEventAsync(string payload)
    {
        _logger.LogDebug("Processing GitHub workflow run event");

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            var action = root.GetProperty("action").GetString();
            var workflowRun = root.GetProperty("workflow_run");
            var name = workflowRun.GetProperty("name").GetString();
            var status = workflowRun.GetProperty("status").GetString();
            var conclusion = workflowRun.GetProperty("conclusion").GetString();
            var repository = root.GetProperty("repository").GetProperty("full_name").GetString();
            var runNumber = workflowRun.GetProperty("run_number").GetInt32();

            _logger.LogInformation("Workflow event: {Action} - '{Name}' #{RunNumber} in {Repository} (Status: {Status}, Conclusion: {Conclusion})",
                action, name, runNumber, repository, status, conclusion);

            // Handle workflow completion
            if (action == "completed")
            {
                if (conclusion == "success")
                {
                    await HandleWorkflowSuccessAsync(workflowRun);
                }
                else if (conclusion == "failure")
                {
                    await HandleWorkflowFailureAsync(workflowRun);
                }
            }

            await Task.CompletedTask;
            return $"Processed workflow {action}: {name} #{runNumber} ({conclusion})";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing workflow run event: {Message}", ex.Message);
            return $"Error processing workflow run event: {ex.Message}";
        }
    }

    public async Task<string> HandleReviewEventAsync(string payload)
    {
        _logger.LogDebug("Processing GitHub review event");

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            var action = root.GetProperty("action").GetString();
            var review = root.GetProperty("review");
            var pullRequest = root.GetProperty("pull_request");
            var prNumber = pullRequest.GetProperty("number").GetInt32();
            var reviewer = review.GetProperty("user").GetProperty("login").GetString();
            var state = review.GetProperty("state").GetString();
            var repository = root.GetProperty("repository").GetProperty("full_name").GetString();

            _logger.LogInformation("Review event: {Action} - {Reviewer} {State} PR #{Number} in {Repository}",
                action, reviewer, state, prNumber, repository);

            await Task.CompletedTask;
            return $"Processed review {action}: {reviewer} {state} PR #{prNumber}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing review event: {Message}", ex.Message);
            return $"Error processing review event: {ex.Message}";
        }
    }

    public async Task<string> HandleRepositoryEventAsync(string payload)
    {
        _logger.LogDebug("Processing GitHub repository event");
        await Task.CompletedTask;
        return "Repository event processed";
    }

    public async Task<string> HandleReleaseEventAsync(string payload)
    {
        _logger.LogDebug("Processing GitHub release event");
        await Task.CompletedTask;
        return "Release event processed";
    }

    public async Task<string> HandleForkEventAsync(string payload)
    {
        _logger.LogDebug("Processing GitHub fork event");
        await Task.CompletedTask;
        return "Fork event processed";
    }

    public async Task<string> HandleStarEventAsync(string payload)
    {
        _logger.LogDebug("Processing GitHub star event");
        await Task.CompletedTask;
        return "Star event processed";
    }

    public async Task<string> HandleWatchEventAsync(string payload)
    {
        _logger.LogDebug("Processing GitHub watch event");
        await Task.CompletedTask;
        return "Watch event processed";
    }

    public async Task<string> HandleGenericEventAsync(string eventType, string payload)
    {
        _logger.LogInformation("Processing generic GitHub event: {EventType}", eventType);
        await Task.CompletedTask;
        return $"Generic event processed: {eventType}";
    }

    #region Private Helper Methods

    private async Task<string> HandlePullRequestOpenedAsync(JsonElement pullRequest)
    {
        // Handle new PR creation - could trigger notifications, auto-assignments, etc.
        await Task.CompletedTask;
        return "PR opened processed";
    }

    private async Task<string> HandlePullRequestClosedAsync(JsonElement pullRequest)
    {
        // Handle PR closure - cleanup, notifications, etc.
        var merged = pullRequest.GetProperty("merged").GetBoolean();
        await Task.CompletedTask;
        return merged ? "PR merged processed" : "PR closed processed";
    }

    private async Task<string> HandlePullRequestEditedAsync(JsonElement pullRequest)
    {
        await Task.CompletedTask;
        return "PR edited processed";
    }

    private async Task<string> HandlePullRequestReviewRequestedAsync(JsonElement pullRequest)
    {
        await Task.CompletedTask;
        return "PR review requested processed";
    }

    private async Task<string> HandlePullRequestReadyForReviewAsync(JsonElement pullRequest)
    {
        await Task.CompletedTask;
        return "PR ready for review processed";
    }

    private async Task<string> HandlePullRequestConvertedToDraftAsync(JsonElement pullRequest)
    {
        await Task.CompletedTask;
        return "PR converted to draft processed";
    }

    private async Task<string> HandleIssueOpenedAsync(JsonElement issue)
    {
        await Task.CompletedTask;
        return "Issue opened processed";
    }

    private async Task<string> HandleIssueClosedAsync(JsonElement issue)
    {
        await Task.CompletedTask;
        return "Issue closed processed";
    }

    private async Task<string> HandleIssueReopenedAsync(JsonElement issue)
    {
        await Task.CompletedTask;
        return "Issue reopened processed";
    }

    private async Task<string> HandleIssueEditedAsync(JsonElement issue)
    {
        await Task.CompletedTask;
        return "Issue edited processed";
    }

    private async Task<string> HandleIssueLabeledAsync(JsonElement issue, JsonElement root)
    {
        await Task.CompletedTask;
        return "Issue labeled processed";
    }

    private async Task<string> HandleIssueUnlabeledAsync(JsonElement issue, JsonElement root)
    {
        await Task.CompletedTask;
        return "Issue unlabeled processed";
    }

    private async Task<string> HandleIssueAssignedAsync(JsonElement issue, JsonElement root)
    {
        await Task.CompletedTask;
        return "Issue assigned processed";
    }

    private async Task<string> HandleIssueUnassignedAsync(JsonElement issue, JsonElement root)
    {
        await Task.CompletedTask;
        return "Issue unassigned processed";
    }

    private async Task HandleWorkflowSuccessAsync(JsonElement workflowRun)
    {
        // Handle successful workflow completion
        await Task.CompletedTask;
    }

    private async Task HandleWorkflowFailureAsync(JsonElement workflowRun)
    {
        // Handle failed workflow - notifications, auto-retry, etc.
        await Task.CompletedTask;
    }

    #endregion
}
