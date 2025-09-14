namespace DigitalMe.Services.Configuration;

/// <summary>
/// Dynamic configuration management service interface.
/// TODO: Implement for production configuration management.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets configuration value by key.
    /// TODO: Implement dynamic configuration retrieval.
    /// </summary>
    Task<T?> GetConfigurationAsync<T>(string key);

    /// <summary>
    /// Updates configuration value.
    /// TODO: Implement configuration persistence.
    /// </summary>
    Task SetConfigurationAsync<T>(string key, T value);
}

/// <summary>
/// Stub implementation of configuration service for MVP.
/// Throws NotImplementedException for all methods.
/// TODO: Replace with actual configuration management implementation.
/// </summary>
public class ConfigurationServiceStub : IConfigurationService
{
    public Task<T?> GetConfigurationAsync<T>(string key)
    {
        throw new NotImplementedException("ConfigurationService requires implementation for production use");
    }

    public Task SetConfigurationAsync<T>(string key, T value)
    {
        throw new NotImplementedException("ConfigurationService requires implementation for production use");
    }
}
