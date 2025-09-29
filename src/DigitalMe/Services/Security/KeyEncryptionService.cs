using System.Security;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DigitalMe.Models.Security;

namespace DigitalMe.Services.Security;

/// <summary>
/// Implements secure encryption/decryption of API keys using AES-256-GCM.
/// Features:
/// - Per-user key derivation using PBKDF2
/// - Authenticated encryption with AES-GCM
/// - Cryptographically secure random generation
/// - Memory protection and cleanup
/// </summary>
public class KeyEncryptionService : IKeyEncryptionService
{
    private readonly ILogger<KeyEncryptionService> _logger;

    // Cryptographic constants
    private const int KeyDerivationIterations = 100000; // PBKDF2 iterations
    private const int SaltSize = 32; // 256 bits
    private const int IVSize = 12; // 96 bits for GCM
    private const int TagSize = 16; // 128 bits authentication tag
    private const int KeySize = 32; // 256 bits for AES-256
    private const int FingerprintLength = 16; // Truncated SHA-256 hash length

    public KeyEncryptionService(ILogger<KeyEncryptionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<EncryptedKeyInfo> EncryptApiKeyAsync(string apiKey, string userId)
    {
        // Validate inputs
        ValidateApiKey(apiKey, nameof(apiKey));
        ValidateUserId(userId, nameof(userId));

        return await Task.Run(() =>
        {
            try
            {
                // Generate cryptographically secure random values
                var salt = RandomNumberGenerator.GetBytes(SaltSize);
                var iv = RandomNumberGenerator.GetBytes(IVSize);

                // Derive encryption key from userId and salt
                var derivedKey = DeriveKeyFromUser(userId, salt);

                // Convert plaintext to bytes
                var plaintext = Encoding.UTF8.GetBytes(apiKey);
                var ciphertext = new byte[plaintext.Length];
                var tag = new byte[TagSize];

                // Encrypt with AES-256-GCM (authenticated encryption)
                using (var aes = new AesGcm(derivedKey, TagSize))
                {
                    aes.Encrypt(iv, plaintext, ciphertext, tag);
                }

                // Clear sensitive data from memory
                Array.Clear(plaintext, 0, plaintext.Length);
                Array.Clear(derivedKey, 0, derivedKey.Length);

                // Log success without exposing sensitive data
                var maskedUserId = MaskSensitiveData(userId);
                _logger.LogInformation("API key encrypted successfully for user {MaskedUserId}", maskedUserId);

                // Create fingerprint for key identification
                var fingerprint = CreateKeyFingerprint(apiKey);

                return new EncryptedKeyInfo(
                    Convert.ToBase64String(ciphertext),
                    Convert.ToBase64String(iv),
                    Convert.ToBase64String(salt),
                    Convert.ToBase64String(tag),
                    fingerprint);
            }
            catch (Exception ex) when (ex is not ArgumentException and not ArgumentNullException)
            {
                _logger.LogError(ex, "Encryption failed");
                throw new SecurityException("Failed to encrypt API key", ex);
            }
        }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string> DecryptApiKeyAsync(EncryptedKeyInfo encryptedInfo, string userId)
    {
        // Validate inputs
        ArgumentNullException.ThrowIfNull(encryptedInfo);
        ValidateUserId(userId, nameof(userId));

        return await Task.Run(() =>
        {
            try
            {
                // Decode Base64 components
                var ciphertext = Convert.FromBase64String(encryptedInfo.EncryptedData);
                var iv = Convert.FromBase64String(encryptedInfo.IV);
                var salt = Convert.FromBase64String(encryptedInfo.Salt);
                var tag = Convert.FromBase64String(encryptedInfo.Tag);

                // Derive key using same userId and salt
                var derivedKey = DeriveKeyFromUser(userId, salt);

                // Decrypt with AES-256-GCM
                var plaintext = new byte[ciphertext.Length];

                using (var aes = new AesGcm(derivedKey, TagSize))
                {
                    aes.Decrypt(iv, ciphertext, tag, plaintext);
                }

                // Clear sensitive data from memory
                Array.Clear(derivedKey, 0, derivedKey.Length);

                // Convert decrypted bytes to string
                var decryptedKey = Encoding.UTF8.GetString(plaintext);

                // Clear plaintext from memory
                Array.Clear(plaintext, 0, plaintext.Length);

                var maskedUserId = MaskSensitiveData(userId);
                _logger.LogInformation("API key decrypted successfully for user {MaskedUserId}", maskedUserId);

                return decryptedKey;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Decryption failed - invalid key or tampered data");

                // Wrap all decryption failures in CryptographicException for consistent API
                throw new CryptographicException("Decryption failed - invalid key or tampered data", ex);
            }
        }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public string CreateKeyFingerprint(string apiKey)
    {
        ValidateApiKey(apiKey, nameof(apiKey));

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        var base64Hash = Convert.ToBase64String(hash);

        // Return first FingerprintLength characters for compact fingerprint
        return base64Hash[..FingerprintLength];
    }

    /// <summary>
    /// Derives a 256-bit encryption key from userId and salt using PBKDF2.
    /// Uses SHA-256 and 100,000 iterations for strong key derivation.
    /// </summary>
    private byte[] DeriveKeyFromUser(string userId, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            userId,
            salt,
            KeyDerivationIterations,
            HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(KeySize);
    }

    /// <summary>
    /// Masks sensitive data for logging by showing only first few characters.
    /// </summary>
    private string MaskSensitiveData(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return "***";
        }

        var visibleLength = Math.Min(4, data.Length);
        return data.Substring(0, visibleLength) + "***";
    }

    /// <summary>
    /// Validates API key parameter for null and whitespace.
    /// </summary>
    private static void ValidateApiKey(string apiKey, string paramName)
    {
        ArgumentNullException.ThrowIfNull(apiKey, paramName);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be empty", paramName);
        }
    }

    /// <summary>
    /// Validates user ID parameter for null and whitespace.
    /// </summary>
    private static void ValidateUserId(string userId, string paramName)
    {
        ArgumentNullException.ThrowIfNull(userId, paramName);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be empty", paramName);
        }
    }
}