using DigitalMe.Integrations.External.Slack.Models;

namespace DigitalMe.Integrations.External.Slack.Services;

/// <summary>
/// Service for Slack reaction operations
/// </summary>
public interface ISlackReactionService
{
    /// <summary>
    /// Add a reaction to a message
    /// </summary>
    Task<bool> AddReactionAsync(string channel, string timestamp, string reaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a reaction from a message
    /// </summary>
    Task<bool> RemoveReactionAsync(string channel, string timestamp, string reaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all reactions for a message
    /// </summary>
    Task<IEnumerable<SlackReaction>> GetReactionsAsync(string channel, string timestamp, CancellationToken cancellationToken = default);
}