using System.Text.RegularExpressions;
using DigitalMe.Data;
using Microsoft.EntityFrameworkCore;

namespace DigitalMe.Services;

public class HealthChecker : IHealthChecker
{
    private readonly DigitalMeDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HealthChecker> _logger;

    public HealthChecker(DigitalMeDbContext dbContext, IConfiguration configuration, ILogger<HealthChecker> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthStatus> GetHealthStatusAsync()
    {
        var healthStatus = new HealthStatus
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Unknown",
            Database = await GetDatabaseHealthAsync(),
            Configuration = GetConfigurationHealth()
        };

        return healthStatus;
    }

    private async Task<DatabaseHealth> GetDatabaseHealthAsync()
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();
            var connectionString = _dbContext.Database.GetConnectionString();

            return new DatabaseHealth
            {
                Status = canConnect ? "Connected" : "Disconnected",
                Provider = _dbContext.Database.ProviderName ?? "Unknown",
                Host = ExtractHostFromConnectionString(connectionString),
                Database = ExtractDatabaseFromConnectionString(connectionString),
                LastChecked = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking database health");
            return new DatabaseHealth
            {
                Status = "Error",
                Error = ex.Message,
                LastChecked = DateTime.UtcNow
            };
        }
    }

    private ConfigurationHealth GetConfigurationHealth()
    {
        return new ConfigurationHealth
        {
            DatabaseProvider = "PostgreSQL",
            CloudSqlInstance = ExtractInstanceName(_configuration.GetConnectionString("DefaultConnection")),
            AnthropicConfigured = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")),
            GitHubConfigured = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_TOKEN")),
            TelegramConfigured = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN"))
        };
    }

    private static string ExtractInstanceName(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return "Unknown";
        }

        // Extract instance name from Cloud SQL connection string
        // Format: Host=/cloudsql/PROJECT:REGION:INSTANCE;...
        var hostMatch = Regex.Match(connectionString, @"Host=/cloudsql/([^;]+)");
        if (hostMatch.Success)
        {
            return hostMatch.Groups[1].Value; // Returns "digitalme-470613:us-central1:digitalme-db"
        }

        // If using SQLite, show that Cloud SQL is not configured
        if (connectionString.Contains("Data Source"))
        {
            return "Not configured (using SQLite)";
        }

        return "Unknown";
    }

    private static string ExtractHostFromConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return "Unknown";
        }

        // Try PostgreSQL format first: Host=...
        var hostMatch = Regex.Match(connectionString, @"Host=([^;]+)");
        if (hostMatch.Success)
        {
            return hostMatch.Groups[1].Value;
        }

        // Try SQLite format: Data Source=...
        var sqliteMatch = Regex.Match(connectionString, @"Data Source=([^;]+)");
        if (sqliteMatch.Success)
        {
            return $"SQLite: {Path.GetFileName(sqliteMatch.Groups[1].Value)}";
        }

        return "Unknown";
    }

    private static string ExtractDatabaseFromConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return "Unknown";
        }

        // Try PostgreSQL format first: Database=...
        var dbMatch = Regex.Match(connectionString, @"Database=([^;]+)");
        if (dbMatch.Success)
        {
            return dbMatch.Groups[1].Value;
        }

        // For SQLite, database is the file itself
        var sqliteMatch = Regex.Match(connectionString, @"Data Source=([^;]+)");
        if (sqliteMatch.Success)
        {
            return Path.GetFileNameWithoutExtension(sqliteMatch.Groups[1].Value);
        }

        return "Unknown";
    }
}
