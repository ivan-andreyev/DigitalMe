namespace DigitalMe.Models.Security;

/// <summary>
/// Represents encrypted API key data with all cryptographic parameters.
/// Contains encrypted data, IV, salt, authentication tag, and key fingerprint.
/// </summary>
public record EncryptedKeyInfo(
    string EncryptedData,
    string IV,
    string Salt,
    string Tag,
    string KeyFingerprint);