using DigitalMe.Data;

namespace DigitalMe.Services.Database;

/// <summary>
/// Service responsible for managing database migrations and consistency validation
/// </summary>
public interface IDatabaseMigrationService
{
    /// <summary>
    /// Applies database migrations with proper error handling and recovery
    /// </summary>
    /// <param name="context">Database context for migration operations</param>
    Task ApplyMigrationsAsync(DigitalMeDbContext context);
    
    /// <summary>
    /// Validates database migration consistency and detects sync issues
    /// </summary>
    /// <param name="context">Database context for validation</param>
    /// <returns>True if migrations are consistent, false otherwise</returns>
    bool ValidateMigrationConsistency(DigitalMeDbContext context);
    
    /// <summary>
    /// Handles database creation when connection fails
    /// </summary>
    /// <param name="context">Database context for creation operations</param>
    Task HandleDatabaseCreationAsync(DigitalMeDbContext context);
}