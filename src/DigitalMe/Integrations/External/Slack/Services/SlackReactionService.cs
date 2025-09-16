using DigitalMe.Integrations.External.Slack.Models;

namespace DigitalMe.Integrations.External.Slack.Services;

/// <summary>
/// Service for Slack reaction operations
/// </summary>
public class SlackReactionService : ISlackReactionService
{
    private readonly ISlackApiClient _apiClient;
    private readonly ILogger<SlackReactionService> _logger;

    public SlackReactionService(ISlackApiClient apiClient, ILogger<SlackReactionService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<bool> AddReactionAsync(string channel, string timestamp, string reaction, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("👍 Adding reaction {Reaction} to message {Timestamp} in channel {Channel}", reaction, timestamp, channel);

            var request = new
            {
                channel = channel,
                timestamp = timestamp,
                name = reaction
            };

            var response = await _apiClient.PostAsync<SlackApiResponse>("reactions.add", request, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Successfully added reaction {Reaction} to message {Timestamp}", reaction, timestamp);
                return true;
            }
            else
            {
                _logger.LogError("❌ Failed to add reaction {Reaction}: {Error}",
                    reaction, response?.Error ?? "Unknown error");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception adding reaction {Reaction} to message {Timestamp} in channel {Channel}", reaction, timestamp, channel);
            return false;
        }
    }

    public async Task<bool> RemoveReactionAsync(string channel, string timestamp, string reaction, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("👎 Removing reaction {Reaction} from message {Timestamp} in channel {Channel}", reaction, timestamp, channel);

            var request = new
            {
                channel = channel,
                timestamp = timestamp,
                name = reaction
            };

            var response = await _apiClient.PostAsync<SlackApiResponse>("reactions.remove", request, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Successfully removed reaction {Reaction} from message {Timestamp}", reaction, timestamp);
                return true;
            }
            else
            {
                _logger.LogError("❌ Failed to remove reaction {Reaction}: {Error}",
                    reaction, response?.Error ?? "Unknown error");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception removing reaction {Reaction} from message {Timestamp} in channel {Channel}", reaction, timestamp, channel);
            return false;
        }
    }

    public async Task<IEnumerable<SlackReaction>> GetReactionsAsync(string channel, string timestamp, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("👁️ Getting reactions for message {Timestamp} in channel {Channel}", timestamp, channel);

            var parameters = new Dictionary<string, string>
            {
                ["channel"] = channel,
                ["timestamp"] = timestamp
            };

            var response = await _apiClient.GetAsync<SlackReactionsResponse>("reactions.get", parameters, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Retrieved {ReactionCount} reactions for message {Timestamp}",
                    response.Message?.Reactions?.Count() ?? 0, timestamp);
                return response.Message?.Reactions ?? Enumerable.Empty<SlackReaction>();
            }
            else
            {
                _logger.LogError("❌ Failed to get reactions for message {Timestamp}: {Error}",
                    timestamp, response?.Error ?? "Unknown error");
                return Enumerable.Empty<SlackReaction>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception getting reactions for message {Timestamp} in channel {Channel}", timestamp, channel);
            return Enumerable.Empty<SlackReaction>();
        }
    }
}