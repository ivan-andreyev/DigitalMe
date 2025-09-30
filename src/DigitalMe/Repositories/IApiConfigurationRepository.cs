using DigitalMe.Data.Entities;

namespace DigitalMe.Repositories;

/// <summary>
/// Repository interface for managing API configuration entities.
/// Provides CRUD operations and specialized query methods for secure API key management.
/// </summary>
public interface IApiConfigurationRepository
{
    /// <summary>
    /// Retrieves an API configuration by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the configuration.</param>
    /// <returns>The configuration if found; otherwise, null.</returns>
    Task<ApiConfiguration?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves the active API configuration for a specific user and provider.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="provider">The AI provider name (e.g., "Anthropic", "OpenAI").</param>
    /// <returns>The active configuration if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown when userId or provider is null or empty.</exception>
    Task<ApiConfiguration?> GetByUserAndProviderAsync(string userId, string provider);

    /// <summary>
    /// Retrieves all API configurations for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A list of all configurations belonging to the user.</returns>
    /// <exception cref="ArgumentException">Thrown when userId is null or empty.</exception>
    Task<List<ApiConfiguration>> GetAllByUserAsync(string userId);

    /// <summary>
    /// Retrieves all active API configurations for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A list of active configurations belonging to the user.</returns>
    Task<List<ApiConfiguration>> GetActiveConfigurationsAsync(string userId);

    /// <summary>
    /// Creates a new API configuration in the database.
    /// </summary>
    /// <param name="configuration">The configuration entity to create.</param>
    /// <returns>The created configuration with generated Id and timestamps.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    Task<ApiConfiguration> CreateAsync(ApiConfiguration configuration);

    /// <summary>
    /// Updates an existing API configuration in the database.
    /// </summary>
    /// <param name="configuration">The configuration entity with updated values.</param>
    /// <returns>The updated configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Thrown when the configuration doesn't exist.</exception>
    Task<ApiConfiguration> UpdateAsync(ApiConfiguration configuration);

    /// <summary>
    /// Permanently deletes an API configuration from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the configuration to delete.</param>
    /// <returns>True if deletion was successful; false if configuration not found.</returns>
    Task<bool> DeleteAsync(Guid id);
}