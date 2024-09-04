using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DigitalMe.Web.Services;

public class DatabasePoolHealthCheck : IHealthCheck
{
    private readonly IDatabaseConnectionMonitor _monitor;
    private readonly ILogger<DatabasePoolHealthCheck> _logger;

    public DatabasePoolHealthCheck(
        IDatabaseConnectionMonitor monitor, 
        ILogger<DatabasePoolHealthCheck> logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = await _monitor.GetPoolMetricsAsync(cancellationToken);
            var efficiency = metrics.PoolEfficiency;
            var isHealthy = efficiency >= 90.0; // P2.4 requirement

            var data = new Dictionary<string, object>
            {
                ["pool_efficiency"] = efficiency,
                ["min_pool_size"] = metrics.MinPoolSize,
                ["max_pool_size"] = metrics.MaxPoolSize,
                ["active_connections"] = metrics.CurrentActiveConnections,
                ["connections_per_minute"] = metrics.ConnectionsPerMinute,
                ["queries_per_minute"] = metrics.QueriesPerMinute,
                ["average_query_duration_ms"] = metrics.AverageQueryDurationMs,
                ["can_connect"] = metrics.CanConnect,
                ["meets_p24_requirement"] = isHealthy,
                ["collected_at"] = metrics.CollectedAt
            };

            if (!metrics.CanConnect)
            {
                return HealthCheckResult.Unhealthy(
                    "Cannot connect to database", 
                    data: data);
            }

            if (!isHealthy)
            {
                return HealthCheckResult.Degraded(
                    $"Database pool efficiency ({efficiency:F2}%) below 90% requirement", 
                    data: data);
            }

            if (efficiency >= 95.0)
            {
                return HealthCheckResult.Healthy(
                    $"Database pool operating at excellent efficiency ({efficiency:F2}%)", 
                    data);
            }

            return HealthCheckResult.Healthy(
                $"Database pool efficiency acceptable ({efficiency:F2}%)", 
                data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database pool health check failed");
            return HealthCheckResult.Unhealthy(
                "Database pool health check failed", 
                ex);
        }
    }
}