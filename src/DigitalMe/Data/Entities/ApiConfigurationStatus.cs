namespace DigitalMe.Data.Entities;

/// <summary>
/// Represents the validation status of an API configuration.
/// Used to track the health and validity of configured API keys.
/// </summary>
public enum ApiConfigurationStatus
{
    /// <summary>
    /// Validation status has not yet been determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// API key has been validated and is working correctly.
    /// </summary>
    Valid = 1,

    /// <summary>
    /// API key is invalid or has been revoked.
    /// </summary>
    Invalid = 2,

    /// <summary>
    /// API key has expired and needs to be renewed.
    /// </summary>
    Expired = 3,

    /// <summary>
    /// API key is rate-limited or quota exhausted.
    /// </summary>
    RateLimited = 4,

    /// <summary>
    /// Validation failed due to network or connection issues.
    /// </summary>
    NetworkError = 5
}