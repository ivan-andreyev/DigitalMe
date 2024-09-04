using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;

namespace DigitalMe.Integrations.External.GitHub;

public class GitHubService : IGitHubService
{
    private readonly GitHubClient _client;
    private readonly GitHubConfiguration _config;
    private readonly ILogger<GitHubService> _logger;
    private bool _isConnected = false;

    public GitHubService(IOptions<GitHubConfiguration> config, ILogger<GitHubService> logger)
    {
        _config = config.Value;
        _logger = logger;
        
        _client = new GitHubClient(new ProductHeaderValue("DigitalMe", "1.0"));
        
        if (!string.IsNullOrEmpty(_config.PersonalAccessToken))
        {
            _client.Credentials = new Credentials(_config.PersonalAccessToken);
        }
    }

    public async Task<bool> InitializeAsync(string accessToken)
    {
        try
        {
            _logger.LogInformation("Initializing GitHub API connection...");
            
            if (!string.IsNullOrEmpty(accessToken))
            {
                _client.Credentials = new Credentials(accessToken);
            }
            
            if (_client.Credentials == null && string.IsNullOrEmpty(_config.PersonalAccessToken))
            {
                _logger.LogWarning("No GitHub credentials configured. API rate limits will apply.");
                _isConnected = false;
                return false;
            }
            
            // Test connection by getting current user
            try
            {
                var user = await _client.User.Current();
                _logger.LogInformation("GitHub API connection established for user: {Username}", user.Login);
                _isConnected = true;
                return true;
            }
            catch (RateLimitExceededException)
            {
                _logger.LogWarning("GitHub API rate limit exceeded during connection test");
                _isConnected = true; // Still connected, just rate limited
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize GitHub API connection");
            _isConnected = false;
            return false;
        }
    }

    public async Task<IEnumerable<GitHubRepository>> SearchRepositoriesAsync(string query, int limit = 10)
    {
        _logger.LogInformation("Searching GitHub repositories: {Query}, limit: {Limit}", query, limit);

        try
        {
            var searchRequest = new SearchRepositoriesRequest(query)
            {
                PerPage = limit,
                SortField = RepoSearchSort.Stars,
                Order = SortDirection.Descending
            };

            var searchResult = await _client.Search.SearchRepo(searchRequest);
            
            return searchResult.Items.Select(repo => new GitHubRepository
            {
                Id = repo.Id,
                Name = repo.Name,
                FullName = repo.FullName,
                Description = repo.Description,
                HtmlUrl = repo.HtmlUrl,
                Language = repo.Language,
                StargazersCount = repo.StargazersCount,
                ForksCount = repo.ForksCount,
                IsPrivate = repo.Private,
                CreatedAt = repo.CreatedAt.DateTime,
                UpdatedAt = repo.UpdatedAt.DateTime,
                Owner = repo.Owner.Login
            });
        }
        catch (RateLimitExceededException ex)
        {
            _logger.LogWarning("GitHub API rate limit exceeded. Reset at: {ResetTime}", ex.Reset);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search GitHub repositories");
            throw;
        }
    }

    public async Task<IEnumerable<GitHubRepository>> GetUserRepositoriesAsync(string username, int limit = 20)
    {
        _logger.LogInformation("Getting repositories for user: {Username}, limit: {Limit}", username, limit);

        try
        {
            var repositories = await _client.Repository.GetAllForUser(username, new ApiOptions
            {
                PageSize = limit,
                PageCount = 1
            });

            return repositories.Select(repo => new GitHubRepository
            {
                Id = repo.Id,
                Name = repo.Name,
                FullName = repo.FullName,
                Description = repo.Description,
                HtmlUrl = repo.HtmlUrl,
                Language = repo.Language,
                StargazersCount = repo.StargazersCount,
                ForksCount = repo.ForksCount,
                IsPrivate = repo.Private,
                CreatedAt = repo.CreatedAt.DateTime,
                UpdatedAt = repo.UpdatedAt.DateTime,
                Owner = repo.Owner.Login
            });
        }
        catch (RateLimitExceededException ex)
        {
            _logger.LogWarning("GitHub API rate limit exceeded. Reset at: {ResetTime}", ex.Reset);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user repositories");
            throw;
        }
    }

    public async Task<GitHubRepository> GetRepositoryAsync(string owner, string repo)
    {
        _logger.LogInformation("Getting repository: {Owner}/{Repo}", owner, repo);

        try
        {
            var repository = await _client.Repository.Get(owner, repo);
            
            return new GitHubRepository
            {
                Id = repository.Id,
                Name = repository.Name,
                FullName = repository.FullName,
                Description = repository.Description,
                HtmlUrl = repository.HtmlUrl,
                Language = repository.Language,
                StargazersCount = repository.StargazersCount,
                ForksCount = repository.ForksCount,
                IsPrivate = repository.Private,
                CreatedAt = repository.CreatedAt.DateTime,
                UpdatedAt = repository.UpdatedAt.DateTime,
                Owner = repository.Owner.Login
            };
        }
        catch (RateLimitExceededException ex)
        {
            _logger.LogWarning("GitHub API rate limit exceeded. Reset at: {ResetTime}", ex.Reset);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get repository: {Owner}/{Repo}", owner, repo);
            throw;
        }
    }

    public async Task<IEnumerable<GitHubCommit>> GetRepositoryCommitsAsync(string owner, string repo, int limit = 10)
    {
        _logger.LogInformation("Getting commits for repository: {Owner}/{Repo}, limit: {Limit}", owner, repo, limit);

        try
        {
            var commits = await _client.Repository.Commit.GetAll(owner, repo, new ApiOptions
            {
                PageSize = limit,
                PageCount = 1
            });
            
            return commits.Select(commit => new GitHubCommit
            {
                Sha = commit.Sha,
                Message = commit.Commit.Message,
                Author = commit.Commit.Author.Name,
                Date = commit.Commit.Author.Date.DateTime,
                HtmlUrl = commit.HtmlUrl
            });
        }
        catch (RateLimitExceededException ex)
        {
            _logger.LogWarning("GitHub API rate limit exceeded. Reset at: {ResetTime}", ex.Reset);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get repository commits: {Owner}/{Repo}", owner, repo);
            throw;
        }
    }

    public async Task<bool> IsConnectedAsync()
    {
        if (!_isConnected || _client.Credentials == null)
            return false;

        try
        {
            // Test connection with a simple API call
            await _client.User.Current();
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class GitHubConfiguration
{
    public string PersonalAccessToken { get; set; } = string.Empty;
}