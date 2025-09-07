using DigitalMe.Data;
using Microsoft.EntityFrameworkCore;

namespace DigitalMe.Services.Database;

/// <summary>
/// Service responsible for managing database migrations and consistency validation
/// </summary>
public class DatabaseMigrationService : IDatabaseMigrationService
{
    private readonly ILogger<DatabaseMigrationService> _logger;
    private readonly IWebHostEnvironment _environment;

    public DatabaseMigrationService(ILogger<DatabaseMigrationService> logger, IWebHostEnvironment environment)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>
    /// Applies database migrations with SQLite synchronization handling and recovery mechanisms
    /// </summary>
    public void ApplyMigrations(DigitalMeDbContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        
        _logger.LogInformation("🔍 Starting database migration check...");
        
        // Check database provider type
        _logger.LogInformation("🔍 Checking database provider...");
        var isInMemory = context.Database.ProviderName?.Contains("InMemory") == true;
        _logger.LogInformation("🔍 Database provider is InMemory: {IsInMemory}", isInMemory);
        
        // Handle InMemory database
        if (isInMemory)
        {
            _logger.LogInformation("🔍 InMemory database detected - using EnsureCreated instead of migrations");
            var created = context.Database.EnsureCreated();
            _logger.LogInformation("✅ InMemory database created: {Created}", created);
            return;
        }

        // Check database connection
        _logger.LogInformation("🔍 Checking database connection...");
        var canConnect = context.Database.CanConnect();
        _logger.LogInformation("🔍 Database connection check result: {CanConnect}", canConnect);
        
        if (!canConnect)
        {
            HandleDatabaseCreation(context);
            return;
        }
        
        HandleMigrationSync(context);
    }

    /// <summary>
    /// Handles database creation when connection fails
    /// </summary>
    public void HandleDatabaseCreation(DigitalMeDbContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        
        _logger.LogWarning("⚠️ Cannot connect to database - attempting to create...");
        try
        {
            context.Database.EnsureCreated();
            _logger.LogInformation("✅ Database created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to create database: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Validates database migration consistency and detects sync issues
    /// </summary>
    public bool ValidateMigrationConsistency(DigitalMeDbContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        
        try
        {
            var appliedMigrations = context.Database.GetAppliedMigrations().ToList();
            var pendingMigrations = context.Database.GetPendingMigrations().ToList();
            var allMigrations = context.Database.GetMigrations().ToList();
            
            _logger.LogInformation("Applied migrations: [{Applied}]", string.Join(", ", appliedMigrations));
            _logger.LogInformation("Pending migrations: [{Pending}]", string.Join(", ", pendingMigrations));
            _logger.LogInformation("All available migrations: [{All}]", string.Join(", ", allMigrations));
            
            return CheckMigrationConsistency(appliedMigrations, pendingMigrations, allMigrations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration consistency validation failed: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Handles migration synchronization checking and application
    /// </summary>
    private void HandleMigrationSync(DigitalMeDbContext context)
    {
        _logger.LogInformation("🔍 Checking migration history consistency...");
        try
        {
            var appliedMigrations = context.Database.GetAppliedMigrations().ToList();
            var pendingMigrations = context.Database.GetPendingMigrations().ToList();
            
            _logger.LogInformation("🔍 Applied migrations: [{Applied}]", string.Join(", ", appliedMigrations));
            _logger.LogInformation("🔍 Pending migrations: [{Pending}]", string.Join(", ", pendingMigrations));
            
            var allMigrations = context.Database.GetMigrations().ToList();
            if (!CheckMigrationConsistency(appliedMigrations, pendingMigrations, allMigrations))
            {
                throw new InvalidOperationException("Migration consistency check failed");
            }
            
            if (!pendingMigrations.Any())
            {
                _logger.LogInformation("✅ Database is up to date - no migrations to apply");
                return;
            }

            ApplyPendingMigrations(pendingMigrations, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Migration check failed: {ErrorMessage}", ex.Message);
            AttemptRecovery(context, ex);
        }
    }

    /// <summary>
    /// Checks for migration synchronization issues and handles stale migration history
    /// </summary>
    private bool CheckMigrationConsistency(List<string> appliedMigrations, List<string> pendingMigrations, List<string> allMigrations)
    {
        if (!appliedMigrations.Any() && !pendingMigrations.Any())
        {
            return true;
        }
        
        // Check for stale migration entries - migrations applied in DB but no longer exist in codebase
        var staleMigrations = appliedMigrations.Where(applied => !allMigrations.Contains(applied)).ToList();
        if (staleMigrations.Any())
        {
            _logger.LogError("🚨 STALE MIGRATION HISTORY DETECTED - Applied migrations no longer exist in codebase:");
            foreach (var staleMigration in staleMigrations)
            {
                _logger.LogError("   - {StaleMigration}", staleMigration);
            }
            _logger.LogError("🔧 CRITICAL: Database must be recreated or migration history manually cleaned");
            return false;
        }
        
        var hasGapInHistory = appliedMigrations.Count + pendingMigrations.Count != allMigrations.Count;
        if (hasGapInHistory)
        {
            _logger.LogWarning("⚠️ Migration history gap detected - some migrations may have been skipped");
            _logger.LogWarning("Applied: {AppliedCount}, Pending: {PendingCount}, Total: {TotalCount}", 
                appliedMigrations.Count, pendingMigrations.Count, allMigrations.Count);
        }
        
        return true;
    }

    /// <summary>
    /// Applies pending migrations to the database
    /// </summary>
    private void ApplyPendingMigrations(List<string> pendingMigrations, DigitalMeDbContext context)
    {
        _logger.LogInformation("🔄 Applying {Count} pending migrations...", pendingMigrations.Count);
        try
        {
            context.Database.Migrate();
            _logger.LogInformation("✅ Successfully applied {Count} migrations", pendingMigrations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MIGRATION ERROR - Failed to apply database migrations. Error: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Attempts recovery from migration failures
    /// </summary>
    private void AttemptRecovery(DigitalMeDbContext context, Exception originalException)
    {
        _logger.LogWarning("🔄 Attempting SQLite recovery from migration failure...");
        
        // In development, we can be more aggressive with recovery
        if (_environment.IsDevelopment())
        {
            _logger.LogWarning("⚠️ Development environment detected - attempting database recreation");
            try
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
                _logger.LogInformation("✅ Development database recreated successfully");
                return;
            }
            catch (Exception recoveryEx)
            {
                _logger.LogError(recoveryEx, "❌ Database recreation failed: {ErrorMessage}", recoveryEx.Message);
            }
        }
        
        _logger.LogError("❌ Recovery failed - manual intervention required");
        throw new InvalidOperationException("Database migration failed and recovery unsuccessful", originalException);
    }
}