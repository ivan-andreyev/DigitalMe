using DigitalMe.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DigitalMe.Services.HealthChecks;

/// <summary>
/// Health check to verify data consistency and Foreign Key integrity.
/// Specifically checks for issues that caused production FK constraint failures.
/// </summary>
public class DataConsistencyHealthCheck : IHealthCheck
{
    private readonly DigitalMeDbContext _context;
    private readonly ILogger<DataConsistencyHealthCheck> _logger;

    public DataConsistencyHealthCheck(DigitalMeDbContext context, ILogger<DataConsistencyHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var issues = new List<string>();
            var warnings = new List<string>();

            // Pre-check: Verify database connection is available
            try
            {
                await _context.Database.CanConnectAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot connect to database in DataConsistencyHealthCheck");
                return HealthCheckResult.Unhealthy(
                    $"Database connection failed: {ex.Message}",
                    ex,
                    data: new Dictionary<string, object>
                    {
                        { "exception", ex.GetType().Name },
                        { "connectionIssue", true }
                    });
            }

            // Check 1: Verify PersonalityProfiles exist
            var personalityProfileCount = await _context.PersonalityProfiles.CountAsync(cancellationToken);
            if (personalityProfileCount == 0)
            {
                // In production, this is a warning, not a critical issue
                warnings.Add("No PersonalityProfiles found in database - initial setup may be required");
            }

            // Check 2: Verify Ivan's profile exists
            var ivanProfile = await _context.PersonalityProfiles
                .FirstOrDefaultAsync(p => p.Name == "Ivan", cancellationToken);
            if (ivanProfile == null)
            {
                // In production, this is a warning, not a critical issue
                warnings.Add("Ivan's PersonalityProfile not found - initial data seeding may be required");
            }

            // Check 3: Check for orphaned conversations (FK constraint violations)
            var orphanedConversations = await _context.Database
                .SqlQuery<int>($@"
                    SELECT COUNT(*) as Value
                    FROM Conversations c
                    LEFT JOIN PersonalityProfiles pp ON c.PersonalityProfileId = pp.Id
                    WHERE pp.Id IS NULL")
                .FirstOrDefaultAsync(cancellationToken);

            if (orphanedConversations > 0)
            {
                issues.Add($"Found {orphanedConversations} orphaned conversations with invalid PersonalityProfileId");
            }

            // Check 4: Verify PersonalityTraits exist for Ivan
            if (ivanProfile != null)
            {
                var traitCount = await _context.PersonalityTraits
                    .CountAsync(t => t.PersonalityProfileId == ivanProfile.Id, cancellationToken);
                if (traitCount == 0)
                {
                    warnings.Add("Ivan's PersonalityProfile has no traits");
                }
                else if (traitCount < 5)
                {
                    warnings.Add($"Ivan's PersonalityProfile has only {traitCount} traits (expected 10+)");
                }
            }

            // Check 5: Verify database constraints are enabled
            try
            {
                // This is SQLite specific - adapt for other databases
                var foreignKeyStatus = await _context.Database
                    .SqlQuery<int>($"PRAGMA foreign_keys")
                    .FirstOrDefaultAsync(cancellationToken);

                if (foreignKeyStatus == 0)
                {
                    warnings.Add("Foreign key constraints are disabled");
                }
            }
            catch (Exception ex)
            {
                warnings.Add($"Could not check FK constraint status: {ex.Message}");
            }

            // Determine health status
            if (issues.Any())
            {
                var data = new Dictionary<string, object>
                {
                    { "issues", issues },
                    { "warnings", warnings },
                    { "personalityProfileCount", personalityProfileCount },
                    { "orphanedConversations", orphanedConversations }
                };

                _logger.LogError("Data consistency check failed with {IssueCount} critical issues", issues.Count);
                return HealthCheckResult.Unhealthy(
                    $"Data consistency issues found: {string.Join(", ", issues)}",
                    data: data);
            }

            if (warnings.Any())
            {
                var data = new Dictionary<string, object>
                {
                    { "warnings", warnings },
                    { "personalityProfileCount", personalityProfileCount }
                };

                _logger.LogWarning("Data consistency check passed with {WarningCount} warnings", warnings.Count);
                return HealthCheckResult.Degraded(
                    $"Data consistency warnings: {string.Join(", ", warnings)}",
                    data: data);
            }

            _logger.LogInformation("Data consistency check passed successfully");
            return HealthCheckResult.Healthy("All data consistency checks passed", new Dictionary<string, object>
            {
                { "personalityProfileCount", personalityProfileCount },
                { "orphanedConversations", 0 }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data consistency health check failed with exception");
            return HealthCheckResult.Unhealthy(
                $"Health check failed: {ex.Message}",
                ex,
                data: new Dictionary<string, object> { { "exception", ex.GetType().Name } });
        }
    }
}