using DigitalMe.Integrations.External.Slack.Models;

namespace DigitalMe.Integrations.External.Slack.Services;

/// <summary>
/// Service for Slack user management
/// </summary>
public interface ISlackUserService
{
    /// <summary>
    /// Get list of all users in the workspace
    /// </summary>
    Task<IEnumerable<SlackUser>> GetUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get information about a specific user
    /// </summary>
    Task<SlackUser?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by email address
    /// </summary>
    Task<SlackUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set user status
    /// </summary>
    Task<bool> SetUserStatusAsync(string status, string? emoji = null, CancellationToken cancellationToken = default);
}