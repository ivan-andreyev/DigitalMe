namespace DigitalMe.Common;

/// <summary>
/// Provides centralized validation helper methods to eliminate code duplication
/// and ensure consistent parameter validation across the application.
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validates that a user ID is not null or whitespace.
    /// </summary>
    /// <param name="userId">The user identifier to validate.</param>
    /// <param name="paramName">The parameter name for the exception message.</param>
    /// <exception cref="ArgumentException">Thrown when userId is null, empty, or whitespace.</exception>
    public static void ValidateUserId(string userId, string paramName)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty.", paramName);
        }
    }

    /// <summary>
    /// Validates that a provider name is not null or whitespace.
    /// </summary>
    /// <param name="provider">The provider name to validate.</param>
    /// <param name="paramName">The parameter name for the exception message.</param>
    /// <exception cref="ArgumentException">Thrown when provider is null, empty, or whitespace.</exception>
    public static void ValidateProvider(string provider, string paramName)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("Provider cannot be null or empty.", paramName);
        }
    }

    /// <summary>
    /// Ensures that an entity exists, throwing an exception if it is null.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <param name="id">The identifier of the entity.</param>
    /// <exception cref="InvalidOperationException">Thrown when the entity is null.</exception>
    public static void EnsureEntityExists<T>(T? entity, Guid id) where T : class
    {
        if (entity == null)
        {
            throw new InvalidOperationException($"Entity of type {typeof(T).Name} with ID {id} not found.");
        }
    }

    /// <summary>
    /// Validates that an object reference is not null.
    /// </summary>
    /// <typeparam name="T">The object type.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">The parameter name for the exception message.</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public static void ValidateNotNull<T>(T? value, string paramName) where T : class
    {
        if (value == null)
        {
            throw new ArgumentNullException(paramName);
        }
    }
}