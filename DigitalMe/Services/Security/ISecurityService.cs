namespace DigitalMe.Services.Security;

/// <summary>
/// Security and encryption service interface.
/// TODO: Implement for production security features.
/// </summary>
public interface ISecurityService
{
    /// <summary>
    /// Encrypts sensitive data.
    /// TODO: Implement data encryption.
    /// </summary>
    Task<string> EncryptAsync(string data);

    /// <summary>
    /// Decrypts sensitive data.
    /// TODO: Implement data decryption.
    /// </summary>
    Task<string> DecryptAsync(string encryptedData);

    /// <summary>
    /// Validates API request signature.
    /// TODO: Implement request signature validation.
    /// </summary>
    bool ValidateSignature(string signature, string data);
}

/// <summary>
/// Stub implementation of security service for MVP.
/// Throws NotImplementedException for all methods.
/// TODO: Replace with actual security implementation.
/// </summary>
public class SecurityServiceStub : ISecurityService
{
    public Task<string> EncryptAsync(string data)
    {
        throw new NotImplementedException("SecurityService requires implementation for production use");
    }

    public Task<string> DecryptAsync(string encryptedData)
    {
        throw new NotImplementedException("SecurityService requires implementation for production use");
    }

    public bool ValidateSignature(string signature, string data)
    {
        throw new NotImplementedException("SecurityService requires implementation for production use");
    }
}