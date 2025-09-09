namespace DigitalMe.Services.UserMapping;

/// <summary>
/// Cross-platform user mapping service interface.
/// TODO: Implement for production user identity management.
/// </summary>
public interface IUserMappingService
{
    /// <summary>
    /// Maps external platform user to internal user ID.
    /// TODO: Implement cross-platform user identification.
    /// </summary>
    Task<Guid> MapExternalUserAsync(string platform, string externalUserId);

    /// <summary>
    /// Gets internal user ID from external platform user.
    /// TODO: Implement user lookup by external ID.
    /// </summary>
    Task<Guid?> GetInternalUserIdAsync(string platform, string externalUserId);
}

/// <summary>
/// Stub implementation of user mapping service for MVP.
/// Throws NotImplementedException for all methods.
/// TODO: Replace with actual user mapping implementation.
/// </summary>
public class UserMappingServiceStub : IUserMappingService
{
    public Task<Guid> MapExternalUserAsync(string platform, string externalUserId)
    {
        throw new NotImplementedException("UserMappingService requires implementation for production use");
    }

    public Task<Guid?> GetInternalUserIdAsync(string platform, string externalUserId)
    {
        throw new NotImplementedException("UserMappingService requires implementation for production use");
    }
}
