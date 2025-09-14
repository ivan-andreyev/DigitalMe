using DigitalMe.Integrations.External.GitHub;
using Microsoft.AspNetCore.Mvc;

namespace DigitalMe.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GitHubController : ControllerBase
{
    private readonly IGitHubService _gitHubService;
    private readonly ILogger<GitHubController> _logger;

    public GitHubController(IGitHubService gitHubService, ILogger<GitHubController> logger)
    {
        _gitHubService = gitHubService;
        _logger = logger;
    }

    [HttpGet("repositories/{username}")]
    public async Task<ActionResult<IEnumerable<GitHubRepository>>> GetUserRepositories(string username)
    {
        try
        {
            var repositories = await _gitHubService.GetUserRepositoriesAsync(username);
            return Ok(repositories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repositories for user {Username}", username);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("repository/{owner}/{repo}")]
    public async Task<ActionResult<GitHubRepository>> GetRepository(string owner, string repo)
    {
        try
        {
            var repository = await _gitHubService.GetRepositoryAsync(owner, repo);
            return Ok(repository);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repository {Owner}/{Repo}", owner, repo);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { message = "GitHub integration is working", timestamp = DateTime.UtcNow });
    }
}
