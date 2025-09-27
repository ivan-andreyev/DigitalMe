using DigitalMe.Data;
using Microsoft.AspNetCore.Identity;
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
    public async Task ApplyMigrationsAsync(DigitalMeDbContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

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
            await HandleDatabaseCreationAsync(context);
            return;
        }

        await HandleMigrationSyncAsync(context);
    }

    /// <summary>
    /// Handles database creation when connection fails
    /// </summary>
    public async Task HandleDatabaseCreationAsync(DigitalMeDbContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        _logger.LogWarning("⚠️ Cannot connect to database - attempting to create...");
        try
        {
            await context.Database.EnsureCreatedAsync();
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
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

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
    private async Task HandleMigrationSyncAsync(DigitalMeDbContext context)
    {
        _logger.LogInformation("🔍 Checking migration history consistency...");
        try
        {
            // Check if migrations history table exists - if not, skip migration checks
            var historyTableExists = false;
            try
            {
                await context.Database.ExecuteSqlRawAsync("SELECT 1 FROM __EFMigrationsHistory LIMIT 1");
                historyTableExists = true;
            }
            catch
            {
                _logger.LogInformation("🔍 No migration history table found - fresh database detected");
            }

            if (!historyTableExists)
            {
                _logger.LogInformation("🔍 Fresh database with no migration history - applying all migrations");
                try
                {
                    await context.Database.MigrateAsync();
                    _logger.LogInformation("✅ Successfully applied all migrations to fresh database");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to apply migrations to fresh database: {ErrorMessage}", ex.Message);
                    throw;
                }
            }

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

            await ApplyPendingMigrationsAsync(pendingMigrations, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Migration check failed: {ErrorMessage}", ex.Message);
            await AttemptRecoveryAsync(context, ex);
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
    private async Task ApplyPendingMigrationsAsync(List<string> pendingMigrations, DigitalMeDbContext context)
    {
        _logger.LogInformation("🔄 Applying {Count} pending migrations...", pendingMigrations.Count);
        try
        {
            // Check for existing tables + empty migration history scenario
            if (await HandleExistingSchemaWithoutHistory(context, pendingMigrations))
            {
                _logger.LogInformation("✅ Schema synchronized with migration history");
                return;
            }

            await context.Database.MigrateAsync();
            _logger.LogInformation("✅ Successfully applied {Count} migrations", pendingMigrations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ MIGRATION ERROR - Failed to apply database migrations. Error: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Handles scenario where database schema exists but migration history is empty
    /// This commonly occurs when database is created outside of EF Core migrations
    /// </summary>
    private async Task<bool> HandleExistingSchemaWithoutHistory(DigitalMeDbContext context, List<string> pendingMigrations)
    {
        try
        {
            _logger.LogInformation("🔧 BLOCKER #1 FIX: Checking existing schema synchronization...");
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

            _logger.LogInformation("🔧 Applied migrations count: {AppliedCount}", appliedMigrations.Count());

            // Only proceed if migration history is completely empty
            if (appliedMigrations.Any())
            {
                _logger.LogInformation("🔧 Migration history not empty, skipping sync");
                return false;
            }

            // Check if key tables exist (indicating database was created outside migrations)
            _logger.LogInformation("🔧 Checking for existing core schema...");
            bool hasExistingTables = await CheckForExistingCoreSchema(context);
            _logger.LogInformation("🔧 Has existing tables: {HasTables}", hasExistingTables);

            if (!hasExistingTables)
            {
                _logger.LogInformation("🔧 Fresh database detected, proceeding with normal migration");
                return false; // Fresh database, proceed with normal migration
            }

            _logger.LogInformation("🔍 Detected existing database schema with empty migration history");
            _logger.LogInformation("🔄 Synchronizing migration history with existing schema...");

            // For each pending migration, add it to migration history without executing
            foreach (var migration in pendingMigrations)
            {
                await context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ({0}, {1})",
                    migration,
                    "8.0.0" // EF Core version
                );

                _logger.LogInformation("📝 Marked migration as applied: {Migration}", migration);
            }

            _logger.LogInformation("✅ Successfully synchronized {Count} migrations with existing schema", pendingMigrations.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Failed to synchronize schema with migration history: {ErrorMessage}", ex.Message);
            return false; // Fall back to normal migration process
        }
    }

    /// <summary>
    /// Checks if core database tables exist
    /// </summary>
    private async Task<bool> CheckForExistingCoreSchema(DigitalMeDbContext context)
    {
        try
        {
            _logger.LogInformation("🔧 BLOCKER #1 FIX: Attempting to query AspNetRoles table...");

            // Check for AspNetRoles table by trying to query it directly
            var roleCount = await context.Set<IdentityRole>().CountAsync();

            _logger.LogInformation("🔧 SUCCESS: AspNetRoles table exists with {Count} roles", roleCount);
            _logger.LogInformation("🔧 BLOCKER #1 FIX: Core schema detected - should trigger sync");
            return true; // If we can query the table, it exists
        }
        catch (Exception ex)
        {
            _logger.LogInformation("🔧 BLOCKER #1 FIX: AspNetRoles table query failed - fresh database: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Attempts recovery from migration failures
    /// </summary>
    private async Task AttemptRecoveryAsync(DigitalMeDbContext context, Exception originalException)
    {
        _logger.LogWarning("🔄 Attempting database recovery from migration failure...");

        // CRITICAL: NEVER delete PostgreSQL databases - they contain production data!
        var isPostgreSQL = context.Database.ProviderName?.Contains("Npgsql") == true;
        if (isPostgreSQL)
        {
            _logger.LogError("🚫 POSTGRESQL DATABASE DETECTED - Recovery via deletion is FORBIDDEN to protect production data");
            _logger.LogError("🔧 For PostgreSQL issues, use migrations or manual intervention, never EnsureDeleted()");

            try
            {
                // For fresh PostgreSQL databases, try EnsureCreated instead of migrations
                var canConnect = await context.Database.CanConnectAsync();
                if (canConnect)
                {
                    var tableExists = await context.Database.ExecuteSqlRawAsync("SELECT 1 FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory' LIMIT 1") > 0;
                    if (!tableExists)
                    {
                        _logger.LogInformation("🔧 Fresh PostgreSQL database detected - using EnsureCreated");
                        await context.Database.EnsureCreatedAsync();
                        _logger.LogInformation("✅ PostgreSQL database created successfully");
                        return;
                    }
                }

                // Only try migrations for PostgreSQL with existing schema
                await context.Database.MigrateAsync();
                _logger.LogInformation("✅ PostgreSQL migration recovery successful");
                return;
            }
            catch (Exception pgRecoveryEx)
            {
                _logger.LogError("❌ PostgreSQL migration recovery failed: {ErrorMessage}", pgRecoveryEx.Message);
                throw new InvalidOperationException("PostgreSQL migration failed - manual intervention required to preserve data", originalException);
            }
        }

        // For non-PostgreSQL databases (SQLite, InMemory), allow controlled recreation
        var allowForcedRecreation = Environment.GetEnvironmentVariable("DIGITALME_ALLOW_DB_RECREATION")?.ToLowerInvariant() == "true";

        // In development with non-PostgreSQL databases, be more conservative with database recreation
        if (_environment.IsDevelopment() && allowForcedRecreation)
        {
            _logger.LogWarning("⚠️ Development environment + DIGITALME_ALLOW_DB_RECREATION=true - attempting database recreation");
            try
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
                _logger.LogInformation("✅ Development database recreated successfully");
                return;
            }
            catch (Exception recoveryEx)
            {
                _logger.LogError(recoveryEx, "❌ Database recreation failed: {ErrorMessage}", recoveryEx.Message);
            }
        }
        else if (_environment.IsDevelopment())
        {
            _logger.LogInformation("🛡️ Database preservation mode - set DIGITALME_ALLOW_DB_RECREATION=true to enable forced recreation");
            _logger.LogInformation("🔧 Attempting gentle recovery instead of database recreation...");

            try
            {
                // Try to run migrations without recreation
                await context.Database.MigrateAsync();
                _logger.LogInformation("✅ Gentle migration recovery successful");
                return;
            }
            catch (Exception gentleRecoveryEx)
            {
                _logger.LogWarning("⚠️ Gentle recovery failed: {ErrorMessage}", gentleRecoveryEx.Message);
            }
        }

        _logger.LogError("❌ Recovery failed - manual intervention required");
        throw new InvalidOperationException("Database migration failed and recovery unsuccessful", originalException);
    }
}
