using System.Text.Json.Serialization;

namespace DigitalMe.Integrations.External.GitHub.Models;

/// <summary>
/// GitHub Pull Request model
/// </summary>
public class GitHubPullRequest
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty; // open, closed, merged
    public string HtmlUrl { get; set; } = string.Empty;
    public string DiffUrl { get; set; } = string.Empty;
    public string PatchUrl { get; set; } = string.Empty;
    public GitHubUser User { get; set; } = new();
    public GitHubUser? Assignee { get; set; }
    public List<GitHubUser> Assignees { get; set; } = new();
    public List<GitHubLabel> Labels { get; set; } = new();
    public GitHubMilestone? Milestone { get; set; }
    public GitHubBranch Head { get; set; } = new();
    public GitHubBranch Base { get; set; } = new();
    public bool Merged { get; set; }
    public DateTime? MergedAt { get; set; }
    public GitHubUser? MergedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool Draft { get; set; }
    public int Commits { get; set; }
    public int Additions { get; set; }
    public int Deletions { get; set; }
    public int ChangedFiles { get; set; }
}

/// <summary>
/// GitHub Issue model
/// </summary>
public class GitHubIssue
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty; // open, closed
    public string StateReason { get; set; } = string.Empty; // completed, not_planned, reopened
    public string HtmlUrl { get; set; } = string.Empty;
    public GitHubUser User { get; set; } = new();
    public GitHubUser? Assignee { get; set; }
    public List<GitHubUser> Assignees { get; set; } = new();
    public List<GitHubLabel> Labels { get; set; } = new();
    public GitHubMilestone? Milestone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public GitHubUser? ClosedBy { get; set; }
    public int Comments { get; set; }
}

/// <summary>
/// GitHub workflow run model
/// </summary>
public class GitHubWorkflowRun
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RunNumber { get; set; }
    public string Event { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // queued, in_progress, completed
    public string Conclusion { get; set; } = string.Empty; // success, failure, cancelled, skipped
    public string WorkflowId { get; set; } = string.Empty;
    public string HeadBranch { get; set; } = string.Empty;
    public string HeadSha { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? RunStartedAt { get; set; }
    public GitHubUser Actor { get; set; } = new();
    public GitHubUser TriggeringActor { get; set; } = new();
}

/// <summary>
/// GitHub Review model
/// </summary>
public class GitHubReview
{
    public long Id { get; set; }
    public GitHubUser User { get; set; } = new();
    public string Body { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty; // PENDING, APPROVED, CHANGES_REQUESTED, COMMENTED, DISMISSED
    public string HtmlUrl { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string CommitId { get; set; } = string.Empty;
}

/// <summary>
/// GitHub User model
/// </summary>
public class GitHubUser
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // User, Bot, Organization
}

/// <summary>
/// GitHub Label model
/// </summary>
public class GitHubLabel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Default { get; set; }
}

/// <summary>
/// GitHub Milestone model
/// </summary>
public class GitHubMilestone
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty; // open, closed
    public int OpenIssues { get; set; }
    public int ClosedIssues { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DueOn { get; set; }
    public DateTime? ClosedAt { get; set; }
}

/// <summary>
/// GitHub Branch model
/// </summary>
public class GitHubBranch
{
    public string Label { get; set; } = string.Empty;
    public string Ref { get; set; } = string.Empty;
    public string Sha { get; set; } = string.Empty;
    public GitHubUser User { get; set; } = new();
    public GitHubRepository Repo { get; set; } = new();
}

/// <summary>
/// GitHub Comment model
/// </summary>
public class GitHubComment
{
    public long Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public GitHubUser User { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public string IssueUrl { get; set; } = string.Empty;
}

/// <summary>
/// Request models for API operations
/// </summary>
public class CreatePullRequestRequest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
    
    [JsonPropertyName("head")]
    public string Head { get; set; } = string.Empty; // branch name
    
    [JsonPropertyName("base")]
    public string Base { get; set; } = string.Empty; // target branch
    
    [JsonPropertyName("draft")]
    public bool Draft { get; set; } = false;
    
    [JsonPropertyName("maintainer_can_modify")]
    public bool MaintainerCanModify { get; set; } = true;
}

public class UpdatePullRequestRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("body")]
    public string? Body { get; set; }
    
    [JsonPropertyName("state")]
    public string? State { get; set; } // open, closed
    
    [JsonPropertyName("base")]
    public string? Base { get; set; }
    
    [JsonPropertyName("maintainer_can_modify")]
    public bool? MaintainerCanModify { get; set; }
}

public class CreateIssueRequest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
    
    [JsonPropertyName("assignees")]
    public List<string> Assignees { get; set; } = new();
    
    [JsonPropertyName("milestone")]
    public int? Milestone { get; set; }
    
    [JsonPropertyName("labels")]
    public List<string> Labels { get; set; } = new();
}

public class UpdateIssueRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("body")]
    public string? Body { get; set; }
    
    [JsonPropertyName("state")]
    public string? State { get; set; } // open, closed
    
    [JsonPropertyName("state_reason")]
    public string? StateReason { get; set; } // completed, not_planned, reopened
    
    [JsonPropertyName("assignees")]
    public List<string>? Assignees { get; set; }
    
    [JsonPropertyName("milestone")]
    public int? Milestone { get; set; }
    
    [JsonPropertyName("labels")]
    public List<string>? Labels { get; set; }
}

public class CreateReviewRequest
{
    [JsonPropertyName("body")]
    public string? Body { get; set; }
    
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty; // APPROVE, REQUEST_CHANGES, COMMENT
    
    [JsonPropertyName("comments")]
    public List<ReviewComment>? Comments { get; set; }
}

public class ReviewComment
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
    
    [JsonPropertyName("position")]
    public int? Position { get; set; }
    
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
    
    [JsonPropertyName("line")]
    public int? Line { get; set; }
    
    [JsonPropertyName("side")]
    public string Side { get; set; } = "RIGHT"; // RIGHT, LEFT
}

public class TriggerWorkflowRequest
{
    [JsonPropertyName("ref")]
    public string Ref { get; set; } = string.Empty; // branch or tag
    
    [JsonPropertyName("inputs")]
    public Dictionary<string, object>? Inputs { get; set; }
}