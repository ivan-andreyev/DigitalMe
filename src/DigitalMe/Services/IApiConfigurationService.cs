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

    /// <summary>
    /// Resolves and retrieves the API key for a given provider and user.
    /// First attempts to use the user's personal encrypted key if configured.
    /// Falls back to the system-wide key from configuration if:
    /// - No user configuration exists
    /// - User configuration is inactive
    /// - Decryption fails
    /// </summary>
    /// <param name="provider">The AI provider name (e.g., "Anthropic", "OpenAI").</param>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The resolved API key (decrypted user key or system key).</returns>
    /// <exception cref="ArgumentException">Thrown when provider or userId is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no API key is available for the provider.</exception>
    Task<string> GetApiKeyAsync(string provider, string userId);

    /// <summary>
    /// Stores an encrypted user API key for the specified provider.
    /// If a configuration already exists, it will be updated.
    /// If no configuration exists, a new one will be created.
    /// </summary>
    /// <param name="provider">The AI provider name (e.g., "Anthropic", "OpenAI").</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="plainApiKey">The plain-text API key to encrypt and store.</param>
    /// <exception cref="ArgumentException">Thrown when any parameter is invalid.</exception>
    Task SetUserApiKeyAsync(string provider, string userId, string plainApiKey);

    /// <summary>
    /// Rotates an existing API key for a user and provider.
    /// Encrypts the new key and updates the existing configuration.
    /// If no configuration exists, creates a new one.
    /// </summary>
    /// <param name="provider">The AI provider name (e.g., "Anthropic", "OpenAI").</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="newPlainApiKey">The new plain-text API key to encrypt and store.</param>
    /// <exception cref="ArgumentException">Thrown when any parameter is invalid.</exception>
    Task RotateUserApiKeyAsync(string provider, string userId, string newPlainApiKey);

    /// <summary>
    /// Retrieves the complete history of API key configurations for a user and provider.
    /// Returns all configurations (active and inactive) sorted by creation date descending.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="provider">The AI provider name (e.g., "Anthropic", "OpenAI").</param>
    /// <returns>A list of all configurations including historical (inactive) entries.</returns>
    /// <exception cref="ArgumentException">Thrown when userId or provider is invalid.</exception>
    Task<List<ApiConfiguration>> GetKeyHistoryAsync(string userId, string provider);
}