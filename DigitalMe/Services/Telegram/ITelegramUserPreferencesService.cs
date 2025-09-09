namespace DigitalMe.Services.Telegram;

public interface ITelegramUserPreferencesService
{
    Task<T?> GetPreferenceAsync<T>(long chatId, string key);
    Task SetPreferenceAsync<T>(long chatId, string key, T value);
}

public class TelegramUserPreferencesService : ITelegramUserPreferencesService
{
    public Task<T?> GetPreferenceAsync<T>(long chatId, string key)
    {
        throw new NotImplementedException("TelegramUserPreferencesService implementation pending");
    }

    public Task SetPreferenceAsync<T>(long chatId, string key, T value)
    {
        throw new NotImplementedException("TelegramUserPreferencesService implementation pending");
    }
}
