using DigitalMe.Integrations.External.Slack.Models;

namespace DigitalMe.Integrations.External.Slack.Services;

/// <summary>
/// Service for Slack user management
/// </summary>
public class SlackUserService : ISlackUserService
{
    private readonly SlackApiClient _apiClient;
    private readonly ILogger<SlackUserService> _logger;

    public SlackUserService(SlackApiClient apiClient, ILogger<SlackUserService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IEnumerable<SlackUser>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("👥 Getting users list");

            var parameters = new Dictionary<string, string>
            {
                ["include_locale"] = "true"
            };

            var response = await _apiClient.GetAsync<SlackUsersResponse>("users.list", parameters, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Retrieved {UserCount} users", response.Members?.Count() ?? 0);
                return response.Members ?? Enumerable.Empty<SlackUser>();
            }
            else
            {
                _logger.LogError("❌ Failed to get users: {Error}", response?.Error ?? "Unknown error");
                return Enumerable.Empty<SlackUser>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception getting users");
            return Enumerable.Empty<SlackUser>();
        }
    }

    public async Task<SlackUser?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("👤 Getting user info for {UserId}", userId);

            var parameters = new Dictionary<string, string>
            {
                ["user"] = userId,
                ["include_locale"] = "true"
            };

            var response = await _apiClient.GetAsync<SlackUserResponse>("users.info", parameters, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Retrieved user info for {UserId}: {UserName}",
                    userId, response.User?.Name ?? "Unknown");
                return response.User;
            }
            else
            {
                _logger.LogError("❌ Failed to get user info for {UserId}: {Error}",
                    userId, response?.Error ?? "Unknown error");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception getting user info for {UserId}", userId);
            return null;
        }
    }

    public async Task<SlackUser?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📧 Getting user by email {Email}", email);

            var parameters = new Dictionary<string, string>
            {
                ["email"] = email
            };

            var response = await _apiClient.GetAsync<SlackUserResponse>("users.lookupByEmail", parameters, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Found user by email {Email}: {UserName}",
                    email, response.User?.Name ?? "Unknown");
                return response.User;
            }
            else
            {
                _logger.LogError("❌ Failed to find user by email {Email}: {Error}",
                    email, response?.Error ?? "Unknown error");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception getting user by email {Email}", email);
            return null;
        }
    }

    public async Task<bool> SetUserPresenceAsync(string presence, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🟢 Setting user presence to {Presence}", presence);

            var request = new
            {
                presence = presence
            };

            var response = await _apiClient.PostAsync<SlackApiResponse>("users.setPresence", request, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ User presence set to {Presence}", presence);
                return true;
            }
            else
            {
                _logger.LogError("❌ Failed to set user presence to {Presence}: {Error}",
                    presence, response?.Error ?? "Unknown error");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception setting user presence to {Presence}", presence);
            return false;
        }
    }

    public async Task<string?> GetUserPresenceAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🟢 Getting user presence for {UserId}", userId);

            var parameters = new Dictionary<string, string>
            {
                ["user"] = userId
            };

            var response = await _apiClient.GetAsync<SlackUserPresenceResponse>("users.getPresence", parameters, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Retrieved presence for {UserId}: {Presence}",
                    userId, response.Presence ?? "Unknown");
                return response.Presence;
            }
            else
            {
                _logger.LogError("❌ Failed to get user presence for {UserId}: {Error}",
                    userId, response?.Error ?? "Unknown error");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception getting user presence for {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> SetUserStatusAsync(string status, string? emoji = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("💬 Setting user status: {Status} {Emoji}", status, emoji ?? "");

            var request = new
            {
                profile = new
                {
                    status_text = status,
                    status_emoji = emoji ?? ""
                }
            };

            var response = await _apiClient.PostAsync<SlackApiResponse>("users.profile.set", request, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ User status set successfully: {Status}", status);
                return true;
            }
            else
            {
                _logger.LogError("❌ Failed to set user status: {Error}",
                    response?.Error ?? "Unknown error");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception setting user status");
            return false;
        }
    }
}