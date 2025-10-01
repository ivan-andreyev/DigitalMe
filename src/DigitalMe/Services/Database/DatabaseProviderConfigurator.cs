using DigitalMe.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Database;

/// <summary>
/// Конфигурирует database provider (PostgreSQL или SQLite) на основе environment и connection strings.
/// Centralized logic для выбора и регистрации DbContext без дублирования.
/// </summary>
public class DatabaseProviderConfigurator
{
    private readonly ILogger? _logger;

    public DatabaseProviderConfigurator(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Конфигурирует database provider для DigitalMeDbContext.
    /// Проверяет environment variables, configuration, и выбирает PostgreSQL или SQLite.
    /// </summary>
    /// <param name="services">Service collection для регистрации DbContext.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="environment">Host environment (Production, Development, Testing).</param>
    public void Configure(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _logger?.LogInformation("🗄️ DATABASE PROVIDER CONFIGURATOR: Starting configuration...");

        // Detect connection string and provider type
        var (connectionString, usePostgreSQL) = DetectConnectionString(configuration);

        // Log decision
        LogProviderDecision(environment, connectionString, usePostgreSQL);

        // Register DbContext with appropriate provider
        RegisterDbContext(services, environment, connectionString, usePostgreSQL);

        _logger?.LogInformation("✅ DATABASE PROVIDER CONFIGURATOR: Configuration completed");
    }

    /// <summary>
    /// Detects connection string from multiple sources with priority order.
    /// </summary>
    /// <returns>Tuple (connectionString, usePostgreSQL)</returns>
    private (string? connectionString, bool usePostgreSQL) DetectConnectionString(IConfiguration configuration)
    {
        // Priority 1: DATABASE_URL environment variable (common in cloud environments)
        _logger?.LogInformation("🔍 Checking DATABASE_URL environment variable...");
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrEmpty(databaseUrl))
        {
            _logger?.LogInformation("✅ DATABASE_URL detected, converting to PostgreSQL connection string");
            var connectionString = ConvertDatabaseUrlToNpgsql(databaseUrl);
            return (connectionString, true);
        }

        _logger?.LogInformation("⚠️ DATABASE_URL not found in environment variables");

        // Priority 2: ConnectionStrings__DefaultConnection environment variable
        _logger?.LogInformation("🔍 Checking ConnectionStrings__DefaultConnection environment variable...");
        var envConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrEmpty(envConnectionString))
        {
            _logger?.LogInformation("✅ ConnectionStrings__DefaultConnection environment variable detected");
            bool isPostgreSQL = IsPostgreSqlConnectionString(envConnectionString);
            if (isPostgreSQL)
            {
                _logger?.LogInformation("🐘 PostgreSQL connection string detected from environment");
            }
            return (envConnectionString, isPostgreSQL);
        }

        // Priority 3: Standard connection string from configuration (appsettings.json)
        _logger?.LogInformation("🔍 Checking configuration for DefaultConnection...");
        var configConnectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(configConnectionString))
        {
            _logger?.LogInformation("✅ Found connection string in configuration");
            bool isPostgreSQL = IsPostgreSqlConnectionString(configConnectionString);
            if (isPostgreSQL)
            {
                _logger?.LogInformation("🐘 PostgreSQL connection string detected from configuration");
            }
            else
            {
                _logger?.LogInformation("📦 SQLite connection string pattern detected");
            }
            return (configConnectionString, isPostgreSQL);
        }

        _logger?.LogInformation("⚠️ No connection string found in configuration");

        // Priority 4: POSTGRES_CONNECTION_STRING environment variable
        _logger?.LogInformation("🔍 Checking POSTGRES_CONNECTION_STRING environment variable...");
        var postgresConnStr = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(postgresConnStr))
        {
            _logger?.LogInformation("✅ POSTGRES_CONNECTION_STRING environment variable detected");
            return (postgresConnStr, true);
        }

        _logger?.LogInformation("⚠️ POSTGRES_CONNECTION_STRING not found");
        return (null, false);
    }

    /// <summary>
    /// Checks if connection string is PostgreSQL format.
    /// </summary>
    private bool IsPostgreSqlConnectionString(string connectionString)
    {
        return connectionString.Contains("Host=") ||
               connectionString.Contains("Server=") ||
               connectionString.Contains("/cloudsql/") ||
               connectionString.Contains("postgres", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Logs final provider decision.
    /// </summary>
    private void LogProviderDecision(IHostEnvironment environment, string? connectionString, bool usePostgreSQL)
    {
        _logger?.LogInformation("📊 Database Provider Decision:");
        _logger?.LogInformation("   - Environment: {Environment}", environment.EnvironmentName);
        _logger?.LogInformation("   - Use PostgreSQL: {UsePostgreSQL}", usePostgreSQL);
        _logger?.LogInformation("   - Has Connection String: {HasConnString}", !string.IsNullOrEmpty(connectionString));
    }

    /// <summary>
    /// Registers DbContext with appropriate provider based on environment and connection string.
    /// </summary>
    private void RegisterDbContext(
        IServiceCollection services,
        IHostEnvironment environment,
        string? connectionString,
        bool usePostgreSQL)
    {
        // Allow overriding Testing check for DatabaseProviderSelectionTests
        // This enables testing Production/Development provider selection logic
        var skipTestingCheck = Environment.GetEnvironmentVariable("SKIP_TESTING_CHECK");
        if (skipTestingCheck != "true" && environment.EnvironmentName == "Testing")
        {
            _logger?.LogInformation("⚠️ Testing environment - DbContext registration skipped (will be configured by test infrastructure)");
            return;
        }

        if (skipTestingCheck == "true")
        {
            _logger?.LogInformation("🔓 SKIP_TESTING_CHECK enabled - Testing environment check bypassed for provider selection tests");
        }

        // PostgreSQL provider
        if (usePostgreSQL && !string.IsNullOrEmpty(connectionString))
        {
            _logger?.LogInformation("✅ Configuring PostgreSQL database provider with connection string");
            services.AddDbContext<DigitalMeDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(30);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                });

                ConfigureDbContextOptions(options, environment);
            });
            return;
        }

        // Production environment MUST have PostgreSQL
        if (environment.IsProduction())
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger?.LogError("❌ CRITICAL: PostgreSQL connection string is required in production environment");
                _logger?.LogInformation("🔍 Environment variables check:");
                _logger?.LogInformation("  - DATABASE_URL: {DatabaseUrl}",
                    Environment.GetEnvironmentVariable("DATABASE_URL") != null ? "SET" : "NOT SET");
                _logger?.LogInformation("  - ConnectionStrings__DefaultConnection: {ConnString}",
                    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") != null ? "SET" : "NOT SET");
                _logger?.LogInformation("  - POSTGRES_CONNECTION_STRING: {PostgresString}",
                    Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING") != null ? "SET" : "NOT SET");

                throw new InvalidOperationException(
                    "PostgreSQL connection string is required in production environment. " +
                    "Set DATABASE_URL, ConnectionStrings__DefaultConnection, or POSTGRES_CONNECTION_STRING environment variable.");
            }

            _logger?.LogInformation("✅ Configuring PostgreSQL database provider for production");
            services.AddDbContext<DigitalMeDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(30);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                });

                ConfigureDbContextOptions(options, environment);
            });
            return;
        }

        // Development environment
        _logger?.LogInformation("⚠️ Development environment detected: {EnvName}", environment.EnvironmentName);

        // Development environment with PostgreSQL connection string
        if (usePostgreSQL && !string.IsNullOrEmpty(connectionString))
        {
            _logger?.LogInformation("🐘 Development environment - using PostgreSQL");
            services.AddDbContext<DigitalMeDbContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(30);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                });

                ConfigureDbContextOptions(options, environment);
            });
            return;
        }

        // Development environment with SQLite connection string
        if (!string.IsNullOrEmpty(connectionString))
        {
            _logger?.LogInformation("📦 Development environment - using SQLite with connection: {ConnectionString}", connectionString);
            services.AddDbContext<DigitalMeDbContext>(options => options.UseSqlite(connectionString));
            return;
        }

        // Development without connection string - default SQLite
        _logger?.LogInformation("📦 Development environment - using default SQLite database");
        services.AddDbContext<DigitalMeDbContext>(options => options.UseSqlite("Data Source=digitalme.db"));
    }

    /// <summary>
    /// Configures DbContext options based on environment (logging, tracking, etc.).
    /// </summary>
    private void ConfigureDbContextOptions(DbContextOptionsBuilder options, IHostEnvironment environment)
    {
        if (environment.IsProduction())
        {
            // Production optimizations
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }
        else
        {
            // Development debugging
            options.EnableSensitiveDataLogging(true);
            options.EnableDetailedErrors(true);
        }
    }

    /// <summary>
    /// Converts DATABASE_URL (postgres://user:pass@host:port/db) to Npgsql connection string format.
    /// </summary>
    /// <param name="databaseUrl">DATABASE_URL in Heroku/Cloud Run format.</param>
    /// <returns>Npgsql connection string.</returns>
    private string ConvertDatabaseUrlToNpgsql(string databaseUrl)
    {
        try
        {
            _logger?.LogInformation("📊 Parsing DATABASE_URL format: {UrlPrefix}",
                databaseUrl.Length > 20 ? databaseUrl.Substring(0, 20) + "..." : databaseUrl);

            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo[0];
            var password = userInfo.Length > 1 ? userInfo[1] : "";
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');

            // Handle optional SSL mode - parse query string manually to avoid System.Web dependency
            var sslMode = "Require"; // default SSL mode
            if (!string.IsNullOrEmpty(uri.Query))
            {
                var queryParams = uri.Query.TrimStart('?').Split('&');
                foreach (var param in queryParams)
                {
                    var parts = param.Split('=');
                    if (parts.Length == 2 && parts[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                    {
                        sslMode = Uri.UnescapeDataString(parts[1]);
                        break;
                    }
                }
            }

            var npgsqlConnStr = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode={sslMode}";
            _logger?.LogInformation("✅ Converted DATABASE_URL to Npgsql format successfully");
            return npgsqlConnStr;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to parse DATABASE_URL");
            throw new InvalidOperationException($"Invalid DATABASE_URL format: {ex.Message}");
        }
    }
}
