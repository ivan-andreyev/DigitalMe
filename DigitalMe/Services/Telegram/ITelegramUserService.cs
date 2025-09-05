namespace DigitalMe.Services.Telegram;

/// <summary>
/// Telegram user management service interface.
/// TODO: Implement for production Telegram user handling.
/// </summary>
public interface ITelegramUserService
{
    /// <summary>
    /// Gets or creates a user mapping for Telegram user.
    /// TODO: Implement user mapping and persistence.
    /// </summary>
    Task<Guid> GetOrCreateUserAsync(long telegramUserId, string username);

    /// <summary>
    /// Updates Telegram user preferences.
    /// TODO: Implement user preference management.
    /// </summary>
    Task UpdateUserPreferencesAsync(long telegramUserId, object preferences);
}

/// <summary>
/// Stub implementation of Telegram user service for MVP.
/// Throws NotImplementedException for all methods.
/// TODO: Replace with actual user management implementation.
/// </summary>
public class TelegramUserServiceStub : ITelegramUserService
{
    public Task<Guid> GetOrCreateUserAsync(long telegramUserId, string username)
    {
        throw new NotImplementedException("TelegramUserService requires implementation for production use");
    }

    public Task UpdateUserPreferencesAsync(long telegramUserId, object preferences)
    {
        throw new NotImplementedException("TelegramUserService requires implementation for production use");
    }
}