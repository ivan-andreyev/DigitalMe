using DigitalMe.Integrations.External.GitHub.Models;

namespace DigitalMe.Integrations.External.GitHub;

/// <summary>
/// Enhanced GitHub integration service interface with PR, Issues, and Actions support
/// </summary>
public interface IGitHubEnhancedService : IGitHubService
{
    // Pull Requests Management
    Task<GitHubPullRequest> CreatePullRequestAsync(string owner, string repo, CreatePullRequestRequest request);
    Task<GitHubPullRequest> GetPullRequestAsync(string owner, string repo, int number);
    Task<IEnumerable<GitHubPullRequest>> GetPullRequestsAsync(string owner, string repo, string state = "open", int limit = 30);
    Task<GitHubPullRequest> UpdatePullRequestAsync(string owner, string repo, int number, UpdatePullRequestRequest request);
    Task<bool> MergePullRequestAsync(string owner, string repo, int number, string? commitTitle = null, string? commitMessage = null);
    Task<bool> ClosePullRequestAsync(string owner, string repo, int number);
    Task<IEnumerable<GitHubCommit>> GetPullRequestCommitsAsync(string owner, string repo, int number);
    Task<IEnumerable<GitHubComment>> GetPullRequestCommentsAsync(string owner, string repo, int number);
    Task<GitHubComment> AddPullRequestCommentAsync(string owner, string repo, int number, string body);

    // Issues Management
    Task<GitHubIssue> CreateIssueAsync(string owner, string repo, CreateIssueRequest request);
    Task<GitHubIssue> GetIssueAsync(string owner, string repo, int number);
    Task<IEnumerable<GitHubIssue>> GetIssuesAsync(string owner, string repo, string state = "open", int limit = 30);
    Task<GitHubIssue> UpdateIssueAsync(string owner, string repo, int number, UpdateIssueRequest request);
    Task<bool> CloseIssueAsync(string owner, string repo, int number, string? stateReason = null);
    Task<IEnumerable<GitHubComment>> GetIssueCommentsAsync(string owner, string repo, int number);
    Task<GitHubComment> AddIssueCommentAsync(string owner, string repo, int number, string body);

    // Code Reviews
    Task<IEnumerable<GitHubReview>> GetPullRequestReviewsAsync(string owner, string repo, int number);
    Task<GitHubReview> CreateReviewAsync(string owner, string repo, int number, CreateReviewRequest request);
    Task<GitHubReview> SubmitReviewAsync(string owner, string repo, int number, long reviewId, string eventType, string? body = null);
    Task<bool> DismissReviewAsync(string owner, string repo, int number, long reviewId, string message);

    // GitHub Actions & Workflows
    Task<IEnumerable<GitHubWorkflowRun>> GetWorkflowRunsAsync(string owner, string repo, string? workflowId = null, int limit = 30);
    Task<GitHubWorkflowRun> GetWorkflowRunAsync(string owner, string repo, long runId);
    Task<bool> TriggerWorkflowAsync(string owner, string repo, string workflowId, TriggerWorkflowRequest request);
    Task<bool> CancelWorkflowRunAsync(string owner, string repo, long runId);
    Task<bool> RerunWorkflowAsync(string owner, string repo, long runId);
    Task<string> GetWorkflowRunLogsAsync(string owner, string repo, long runId);

    // Repository Management Extensions
    Task<IEnumerable<GitHubLabel>> GetLabelsAsync(string owner, string repo);
    Task<GitHubLabel> CreateLabelAsync(string owner, string repo, string name, string color, string? description = null);
    Task<IEnumerable<GitHubMilestone>> GetMilestonesAsync(string owner, string repo, string state = "open");
    Task<GitHubMilestone> CreateMilestoneAsync(string owner, string repo, string title, string? description = null, DateTime? dueOn = null);

    // Branch Management
    Task<IEnumerable<GitHubBranch>> GetBranchesAsync(string owner, string repo);
    Task<GitHubBranch> GetBranchAsync(string owner, string repo, string branch);
    Task<bool> CreateBranchAsync(string owner, string repo, string branchName, string fromSha);
    Task<bool> DeleteBranchAsync(string owner, string repo, string branchName);

    // Webhook Support
    Task<bool> ValidateWebhookSignatureAsync(string payload, string signature, string secret);
    Task<string> ProcessWebhookAsync(string eventType, string payload);
}
