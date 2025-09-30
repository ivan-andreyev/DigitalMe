using DigitalMe.Models.Security;

namespace DigitalMe.Services.Security;

/// <summary>
/// Service for encrypting and decrypting API keys using AES-256-GCM.
/// Implements per-user encryption with key derivation from userId.
/// </summary>
public interface IKeyEncryptionService
{
    /// <summary>
    /// Encrypts an API key for a specific user using AES-256-GCM.
    /// Each encryption uses a unique IV and salt for maximum security.
    /// </summary>
    /// <param name="apiKey">Plain text API key to encrypt</param>
    /// <param name="userId">User ID for key derivation</param>
    /// <returns>Encrypted key information including IV, salt, tag, and fingerprint</returns>
    Task<EncryptedKeyInfo> EncryptApiKeyAsync(string apiKey, string userId);

    /// <summary>
    /// Decrypts an encrypted API key for a specific user.
    /// Verifies authentication tag to detect tampering.
    /// </summary>
    /// <param name="encryptedInfo">Encrypted key information</param>
    /// <param name="userId">User ID for key derivation (must match original encryption)</param>
    /// <returns>Decrypted API key</returns>
    /// <exception cref="CryptographicException">If decryption fails or data is tampered</exception>
    Task<string> DecryptApiKeyAsync(EncryptedKeyInfo encryptedInfo, string userId);

    /// <summary>
    /// Creates a SHA-256 fingerprint for an API key.
    /// Used for key identification without storing the actual key.
    /// </summary>
    /// <param name="apiKey">API key to fingerprint</param>
    /// <returns>16-character base64 fingerprint</returns>
    string CreateKeyFingerprint(string apiKey);
}