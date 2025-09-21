namespace DigitalMe.Services.UserMapping;

/// <summary>
/// Basic UserMappingService implementation.
/// Returns default user ID for MVP - no cross-platform mapping yet.
/// </summary>
public class UserMappingService : IUserMappingService
{
    private readonly ILogger<UserMappingService> _logger;

    // Default user ID for MVP - represents Ivan
    private static readonly Guid DefaultUserId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000");

    public UserMappingService(ILogger<UserMappingService> logger)
    {
        _logger = logger;
    }

    public Task<Guid> MapExternalUserAsync(string platform, string externalUserId)
    {
        _logger.LogInformation("MVP: Mapping {Platform} user {ExternalUserId} to default user {UserId}",
            platform, externalUserId, DefaultUserId);

        // MVP: Always return Ivan's user ID
        return Task.FromResult(DefaultUserId);
    }

    public Task<Guid?> GetInternalUserIdAsync(string platform, string externalUserId)
    {
        _logger.LogInformation("MVP: Getting internal user ID for {Platform} user {ExternalUserId}",
            platform, externalUserId);

        // MVP: Always return Ivan's user ID
        return Task.FromResult<Guid?>(DefaultUserId);
    }
}
