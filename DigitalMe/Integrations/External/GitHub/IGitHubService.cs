namespace DigitalMe.Integrations.External.GitHub;

public interface IGitHubService
{
    Task<bool> InitializeAsync(string accessToken);
    Task<IEnumerable<GitHubRepository>> SearchRepositoriesAsync(string query, int limit = 10);
    Task<IEnumerable<GitHubRepository>> GetUserRepositoriesAsync(string username, int limit = 20);
    Task<GitHubRepository> GetRepositoryAsync(string owner, string repo);
    Task<IEnumerable<GitHubCommit>> GetRepositoryCommitsAsync(string owner, string repo, int limit = 10);
    Task<bool> IsConnectedAsync();
}

public class GitHubRepository
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int StargazersCount { get; set; }
    public int ForksCount { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Owner { get; set; } = string.Empty;
}

public class GitHubCommit
{
    public string Sha { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
}