using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DigitalMe.Configuration;
using DigitalMe.Integrations.External.GitHub.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalMe.Integrations.External.GitHub;

/// <summary>
/// Enhanced GitHub service implementation with PR, Issues, and Actions support
/// </summary>
public class GitHubEnhancedService : GitHubService, IGitHubEnhancedService
{
    private readonly HttpClient _httpClient;
    private readonly GitHubSettings _settings;
    private readonly ILogger<GitHubEnhancedService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public GitHubEnhancedService(
        HttpClient httpClient,
        IOptions<IntegrationSettings> integrationSettings,
        IOptions<GitHubConfiguration> config,
        ILogger<GitHubEnhancedService> logger)
        : base(config, logger)
    {
        _httpClient = httpClient;
        _settings = integrationSettings.Value.GitHub;
        _logger = logger;

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl ?? "https://api.github.com");
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DigitalMe", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

        if (!string.IsNullOrEmpty(_settings.PersonalAccessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.PersonalAccessToken);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    #region Pull Requests Management

    public async Task<GitHubPullRequest> CreatePullRequestAsync(string owner, string repo, CreatePullRequestRequest request)
    {
        _logger.LogInformation("Creating pull request: {Title} for {Owner}/{Repo}", request.Title, owner, repo);

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/pulls", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var pullRequest = JsonSerializer.Deserialize<GitHubPullRequest>(responseJson, _jsonOptions)!;

        _logger.LogInformation("Created pull request #{Number}: {Title}", pullRequest.Number, pullRequest.Title);
        return pullRequest;
    }

    public async Task<GitHubPullRequest> GetPullRequestAsync(string owner, string repo, int number)
    {
        _logger.LogDebug("Getting pull request #{Number} for {Owner}/{Repo}", number, owner, repo);

        var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/pulls/{number}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubPullRequest>(json, _jsonOptions)!;
    }

    public async Task<IEnumerable<GitHubPullRequest>> GetPullRequestsAsync(string owner, string repo, string state = "open", int limit = 30)
    {
        _logger.LogDebug("Getting pull requests for {Owner}/{Repo}, state: {State}, limit: {Limit}", owner, repo, state, limit);

        var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/pulls?state={state}&per_page={limit}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<GitHubPullRequest>>(json, _jsonOptions) ?? new List<GitHubPullRequest>();
    }

    public async Task<GitHubPullRequest> UpdatePullRequestAsync(string owner, string repo, int number, UpdatePullRequestRequest request)
    {
        _logger.LogInformation("Updating pull request #{Number} for {Owner}/{Repo}", number, owner, repo);

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PatchAsync($"/repos/{owner}/{repo}/pulls/{number}", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubPullRequest>(responseJson, _jsonOptions)!;
    }

    public async Task<bool> MergePullRequestAsync(string owner, string repo, int number, string? commitTitle = null, string? commitMessage = null)
    {
        _logger.LogInformation("Merging pull request #{Number} for {Owner}/{Repo}", number, owner, repo);

        var request = new
        {
            commit_title = commitTitle,
            commit_message = commitMessage,
            merge_method = "merge"
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync($"/repos/{owner}/{repo}/pulls/{number}/merge", content);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully merged pull request #{Number}", number);
            return true;
        }

        _logger.LogWarning("Failed to merge pull request #{Number}: {StatusCode}", number, response.StatusCode);
        return false;
    }

    public async Task<bool> ClosePullRequestAsync(string owner, string repo, int number)
    {
        return (await UpdatePullRequestAsync(owner, repo, number, new UpdatePullRequestRequest { State = "closed" })).State == "closed";
    }

    public async Task<IEnumerable<GitHubCommit>> GetPullRequestCommitsAsync(string owner, string repo, int number)
    {
        var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/pulls/{number}/commits");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<GitHubCommit>>(json, _jsonOptions) ?? new List<GitHubCommit>();
    }

    public async Task<IEnumerable<GitHubComment>> GetPullRequestCommentsAsync(string owner, string repo, int number)
    {
        var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/pulls/{number}/comments");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<GitHubComment>>(json, _jsonOptions) ?? new List<GitHubComment>();
    }

    public async Task<GitHubComment> AddPullRequestCommentAsync(string owner, string repo, int number, string body)
    {
        var request = new { body };
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/issues/{number}/comments", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubComment>(responseJson, _jsonOptions)!;
    }

    #endregion

    #region Issues Management

    public async Task<GitHubIssue> CreateIssueAsync(string owner, string repo, CreateIssueRequest request)
    {
        _logger.LogInformation("Creating issue: {Title} for {Owner}/{Repo}", request.Title, owner, repo);

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/issues", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var issue = JsonSerializer.Deserialize<GitHubIssue>(responseJson, _jsonOptions)!;

        _logger.LogInformation("Created issue #{Number}: {Title}", issue.Number, issue.Title);
        return issue;
    }

    public async Task<GitHubIssue> GetIssueAsync(string owner, string repo, int number)
    {
        var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/issues/{number}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubIssue>(json, _jsonOptions)!;
    }

    public async Task<IEnumerable<GitHubIssue>> GetIssuesAsync(string owner, string repo, string state = "open", int limit = 30)
    {
        var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/issues?state={state}&per_page={limit}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<GitHubIssue>>(json, _jsonOptions) ?? new List<GitHubIssue>();
    }

    public async Task<GitHubIssue> UpdateIssueAsync(string owner, string repo, int number, UpdateIssueRequest request)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PatchAsync($"/repos/{owner}/{repo}/issues/{number}", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubIssue>(responseJson, _jsonOptions)!;
    }

    public async Task<bool> CloseIssueAsync(string owner, string repo, int number, string? stateReason = null)
    {
        var updateRequest = new UpdateIssueRequest
        {
            State = "closed",
            StateReason = stateReason ?? "completed"
        };

        var result = await UpdateIssueAsync(owner, repo, number, updateRequest);
        return result.State == "closed";
    }

    public async Task<IEnumerable<GitHubComment>> GetIssueCommentsAsync(string owner, string repo, int number)
    {
        var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/issues/{number}/comments");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<GitHubComment>>(json, _jsonOptions) ?? new List<GitHubComment>();
    }

    public async Task<GitHubComment> AddIssueCommentAsync(string owner, string repo, int number, string body)
    {
        var request = new { body };
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/issues/{number}/comments", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubComment>(responseJson, _jsonOptions)!;
    }

    #endregion

    #region Code Reviews

    public async Task<IEnumerable<GitHubReview>> GetPullRequestReviewsAsync(string owner, string repo, int number)
    {
        var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/pulls/{number}/reviews");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<GitHubReview>>(json, _jsonOptions) ?? new List<GitHubReview>();
    }

    public async Task<GitHubReview> CreateReviewAsync(string owner, string repo, int number, CreateReviewRequest request)
    {
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/pulls/{number}/reviews", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubReview>(responseJson, _jsonOptions)!;
    }

    public async Task<GitHubReview> SubmitReviewAsync(string owner, string repo, int number, long reviewId, string eventType, string? body = null)
    {
        var request = new
        {
            body,
            @event = eventType
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/pulls/{number}/reviews/{reviewId}/events", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubReview>(responseJson, _jsonOptions)!;
    }

    public async Task<bool> DismissReviewAsync(string owner, string repo, int number, long reviewId, string message)
    {
        var request = new { message };
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync($"/repos/{owner}/{repo}/pulls/{number}/reviews/{reviewId}/dismissals", content);
        return response.IsSuccessStatusCode;
    }

    #endregion

    #region GitHub Actions & Workflows

    public async Task<IEnumerable<GitHubWorkflowRun>> GetWorkflowRunsAsync(string owner, string repo, string? workflowId = null, int limit = 30)
    {
        var url = $"/repos/{owner}/{repo}/actions/runs?per_page={limit}";
        if (!string.IsNullOrEmpty(workflowId))
        {
            url += $"&workflow_id={workflowId}";
        }

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json, _jsonOptions);
        var workflowRuns = result.GetProperty("workflow_runs").Deserialize<List<GitHubWorkflowRun>>(_jsonOptions);

        return workflowRuns ?? new List<GitHubWorkflowRun>();
    }

    public async Task<GitHubWorkflowRun> GetWorkflowRunAsync(string owner, string repo, long runId)
    {
        var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/actions/runs/{runId}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubWorkflowRun>(json, _jsonOptions)!;
    }

    public async Task<bool> TriggerWorkflowAsync(string owner, string repo, string workflowId, TriggerWorkflowRequest request)
    {
        _logger.LogInformation("Triggering workflow {WorkflowId} for {Owner}/{Repo}", workflowId, owner, repo);

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/actions/workflows/{workflowId}/dispatches", content);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully triggered workflow {WorkflowId}", workflowId);
            return true;
        }

        _logger.LogWarning("Failed to trigger workflow {WorkflowId}: {StatusCode}", workflowId, response.StatusCode);
        return false;
    }

    public async Task<bool> CancelWorkflowRunAsync(string owner, string repo, long runId)
    {
        var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/actions/runs/{runId}/cancel", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RerunWorkflowAsync(string owner, string repo, long runId)
    {
        var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/actions/runs/{runId}/rerun", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<string> GetWorkflowRunLogsAsync(string owner, string repo, long runId)
    {
        var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/actions/runs/{runId}/logs");

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }

        return string.Empty;
    }

    #endregion

    #region Repository Management Extensions

    public async Task<IEnumerable<GitHubLabel>> GetLabelsAsync(string owner, string repo)
    {
        var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/labels");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<GitHubLabel>>(json, _jsonOptions) ?? new List<GitHubLabel>();
    }

    public async Task<GitHubLabel> CreateLabelAsync(string owner, string repo, string name, string color, string? description = null)
    {
        var request = new
        {
            name,
            color,
            description
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/labels", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubLabel>(responseJson, _jsonOptions)!;
    }

    public async Task<IEnumerable<GitHubMilestone>> GetMilestonesAsync(string owner, string repo, string state = "open")
    {
        var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/milestones?state={state}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<GitHubMilestone>>(json, _jsonOptions) ?? new List<GitHubMilestone>();
    }

    public async Task<GitHubMilestone> CreateMilestoneAsync(string owner, string repo, string title, string? description = null, DateTime? dueOn = null)
    {
        var request = new
        {
            title,
            description,
            due_on = dueOn?.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/milestones", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubMilestone>(responseJson, _jsonOptions)!;
    }

    #endregion

    #region Branch Management

    public async Task<IEnumerable<GitHubBranch>> GetBranchesAsync(string owner, string repo)
    {
        var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/branches");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<GitHubBranch>>(json, _jsonOptions) ?? new List<GitHubBranch>();
    }

    public async Task<GitHubBranch> GetBranchAsync(string owner, string repo, string branch)
    {
        var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/branches/{branch}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubBranch>(json, _jsonOptions)!;
    }

    public async Task<bool> CreateBranchAsync(string owner, string repo, string branchName, string fromSha)
    {
        var request = new
        {
            @ref = $"refs/heads/{branchName}",
            sha = fromSha
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/git/refs", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteBranchAsync(string owner, string repo, string branchName)
    {
        var response = await _httpClient.DeleteAsync($"/repos/{owner}/{repo}/git/refs/heads/{branchName}");
        return response.IsSuccessStatusCode;
    }

    #endregion

    #region Webhook Support

    public async Task<bool> ValidateWebhookSignatureAsync(string payload, string signature, string secret)
    {
        if (string.IsNullOrEmpty(signature) || !signature.StartsWith("sha256="))
            return false;

        var expectedSignature = signature[7..]; // Remove "sha256=" prefix

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

        return computedSignature == expectedSignature;
    }

    public async Task<string> ProcessWebhookAsync(string eventType, string payload)
    {
        _logger.LogInformation("Processing GitHub webhook: {EventType}", eventType);

        try
        {
            // Parse the webhook payload based on event type
            var result = eventType switch
            {
                "push" => await ProcessPushWebhookAsync(payload),
                "pull_request" => await ProcessPullRequestWebhookAsync(payload),
                "issues" => await ProcessIssueWebhookAsync(payload),
                "workflow_run" => await ProcessWorkflowRunWebhookAsync(payload),
                "pull_request_review" => await ProcessReviewWebhookAsync(payload),
                _ => await ProcessGenericWebhookAsync(eventType, payload)
            };

            _logger.LogInformation("Successfully processed webhook: {EventType}", eventType);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook {EventType}: {Message}", eventType, ex.Message);
            return $"Error: {ex.Message}";
        }
    }

    private async Task<string> ProcessPushWebhookAsync(string payload)
    {
        // Process push events (commits, branch updates)
        await Task.CompletedTask; // Placeholder for actual processing
        return "Push event processed";
    }

    private async Task<string> ProcessPullRequestWebhookAsync(string payload)
    {
        // Process PR events (opened, closed, merged, etc.)
        await Task.CompletedTask;
        return "Pull request event processed";
    }

    private async Task<string> ProcessIssueWebhookAsync(string payload)
    {
        // Process issue events (opened, closed, labeled, etc.)
        await Task.CompletedTask;
        return "Issue event processed";
    }

    private async Task<string> ProcessWorkflowRunWebhookAsync(string payload)
    {
        // Process workflow run events (completed, failed, etc.)
        await Task.CompletedTask;
        return "Workflow run event processed";
    }

    private async Task<string> ProcessReviewWebhookAsync(string payload)
    {
        // Process review events (submitted, dismissed, etc.)
        await Task.CompletedTask;
        return "Review event processed";
    }

    private async Task<string> ProcessGenericWebhookAsync(string eventType, string payload)
    {
        // Handle unknown event types
        await Task.CompletedTask;
        return $"Generic event processed: {eventType}";
    }

    #endregion
}
