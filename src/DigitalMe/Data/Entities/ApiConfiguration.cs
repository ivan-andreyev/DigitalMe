using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalMe.Data.Entities;

/// <summary>
/// Represents a user's encrypted API key configuration for external AI providers.
/// Stores encrypted credentials with associated metadata for secure API access.
/// </summary>
[Table("ApiConfigurations")]
public class ApiConfiguration : BaseEntity
{
    /// <summary>
    /// User ID who owns this API configuration.
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// AI provider name (e.g., "Anthropic", "OpenAI", "Google").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Optional display name for this configuration.
    /// </summary>
    [MaxLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Encrypted API key (Base64-encoded cipher text).
    /// </summary>
    [Required]
    public string EncryptedApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Initialization vector used for AES encryption (Base64-encoded).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EncryptionIV { get; set; } = string.Empty;

    /// <summary>
    /// Salt used for key derivation (Base64-encoded).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EncryptionSalt { get; set; } = string.Empty;

    /// <summary>
    /// Authentication tag from AES-GCM encryption for tamper detection (Base64-encoded).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string AuthenticationTag { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 fingerprint of the plaintext API key for verification.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string KeyFingerprint { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this configuration is currently active and should be used.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp of the last time this API key was used.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Timestamp of the last validation check for this API key.
    /// </summary>
    public DateTime? LastValidatedAt { get; set; }

    /// <summary>
    /// Status of the last validation attempt.
    /// </summary>
    public ApiConfigurationStatus ValidationStatus { get; set; } = ApiConfigurationStatus.Unknown;

    /// <summary>
    /// Default constructor for Entity Framework.
    /// </summary>
    public ApiConfiguration() : base()
    {
    }
}