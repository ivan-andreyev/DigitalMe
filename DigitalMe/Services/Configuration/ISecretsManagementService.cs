using System.ComponentModel.DataAnnotations;

namespace DigitalMe.Services.Configuration;

/// <summary>
/// Service for secure secrets management with environment variable fallbacks
/// </summary>
public interface ISecretsManagementService
{
    /// <summary>
    /// Gets a secret value with environment variable fallback
    /// </summary>
    /// <param name="secretKey">Configuration key path (e.g., "JWT:Key")</param>
    /// <param name="environmentVariableName">Environment variable name for fallback</param>
    /// <returns>Secret value or null if not found</returns>
    string? GetSecret(string secretKey, string? environmentVariableName = null);

    /// <summary>
    /// Gets a required secret value with environment variable fallback
    /// </summary>
    /// <param name="secretKey">Configuration key path (e.g., "JWT:Key")</param>
    /// <param name="environmentVariableName">Environment variable name for fallback</param>
    /// <returns>Secret value</returns>
    /// <exception cref="InvalidOperationException">Thrown when secret is not found</exception>
    string GetRequiredSecret(string secretKey, string? environmentVariableName = null);

    /// <summary>
    /// Validates all required secrets are configured
    /// </summary>
    /// <returns>Validation results</returns>
    SecretsValidationResult ValidateSecrets();

    /// <summary>
    /// Gets a secure JWT key, generating one if not configured
    /// </summary>
    /// <returns>Secure JWT key (minimum 32 characters)</returns>
    string GetSecureJwtKey();

    /// <summary>
    /// Checks if running in a secure environment (production)
    /// </summary>
    /// <returns>True if in production environment</returns>
    bool IsSecureEnvironment();
    
    /// <summary>
    /// Checks if running in Testing environment
    /// </summary>
    /// <returns>True if in Testing environment</returns>
    bool IsTestEnvironment();
}

/// <summary>
/// Results of secrets validation
/// </summary>
public class SecretsValidationResult
{
    public bool IsValid { get; set; }
    public List<string> MissingSecrets { get; set; } = new();
    public List<string> WeakSecrets { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> SecurityRecommendations { get; set; } = new();
}