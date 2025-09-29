using DigitalMe.Data.Entities;

namespace DigitalMe.Services;

/// <summary>
/// Service interface for managing API configuration business logic and orchestration.
/// Provides high-level operations for API key management, validation, and usage tracking.
/// </summary>
public interface IApiConfigurationService
{
    /// <summary>
    /// Deactivates an API configuration, preventing it from being used for API requests.
    /// </summary>
    /// <param name="configurationId">The unique identifier of the configuration to deactivate.</param>
    /// <returns>True if deactivation was successful; false if configuration not found.</returns>
    Task<bool> DeactivateConfigurationAsync(Guid configurationId);

    /// <summary>
    /// Tracks usage of an API configuration by updating the last used timestamp.
    /// </summary>
    /// <param name="configurationId">The unique identifier of the configuration.</param>
    /// <exception cref="InvalidOperationException">Thrown when the configuration doesn't exist.</exception>
    Task TrackUsageAsync(Guid configurationId);

    /// <summary>
    /// Updates the validation status of an API configuration.
    /// </summary>
    /// <param name="configurationId">The unique identifier of the configuration.</param>
    /// <param name="status">The new validation status.</param>
    /// <exception cref="InvalidOperationException">Thrown when the configuration doesn't exist.</exception>
    Task ValidateConfigurationAsync(Guid configurationId, ApiConfigurationStatus status);

    /// <summary>
    /// Retrieves an existing active configuration or creates a new one if none exists.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="provider">The AI provider name.</param>
    /// <param name="encryptedApiKey">The encrypted API key for new configurations.</param>
    /// <param name="encryptionIV">The encryption IV for new configurations.</param>
    /// <param name="encryptionSalt">The encryption salt for new configurations.</param>
    /// <param name="keyFingerprint">The key fingerprint for new configurations.</param>
    /// <returns>The existing or newly created configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when required parameters are invalid.</exception>
    Task<ApiConfiguration> GetOrCreateConfigurationAsync(
        string userId,
        string provider,
        string? encryptedApiKey = null,
        string? encryptionIV = null,
        string? encryptionSalt = null,
        string? keyFingerprint = null);

    /// <summary>
    /// Rotates the API key for an existing configuration with a new encrypted key.
    /// </summary>
    /// <param name="configurationId">The unique identifier of the configuration.</param>
    /// <param name="newEncryptedApiKey">The new encrypted API key.</param>
    /// <param name="newEncryptionIV">The new encryption IV.</param>
    /// <param name="newEncryptionSalt">The new encryption salt.</param>
    /// <param name="newKeyFingerprint">The new key fingerprint.</param>
    /// <returns>The updated configuration with the new key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the configuration doesn't exist.</exception>
    /// <exception cref="ArgumentException">Thrown when required parameters are invalid.</exception>
    Task<ApiConfiguration> RotateApiKeyAsync(
        Guid configurationId,
        string newEncryptedApiKey,
        string newEncryptionIV,
        string newEncryptionSalt,
        string newKeyFingerprint);

    /// <summary>
    /// Retrieves the active configuration for a user and provider.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="provider">The AI provider name.</param>
    /// <returns>The active configuration if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown when userId or provider is invalid.</exception>
    Task<ApiConfiguration?> GetActiveConfigurationAsync(string userId, string provider);

    /// <summary>
    /// Retrieves all configurations for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A list of all configurations belonging to the user.</returns>
    /// <exception cref="ArgumentException">Thrown when userId is invalid.</exception>
    Task<List<ApiConfiguration>> GetUserConfigurationsAsync(string userId);
}