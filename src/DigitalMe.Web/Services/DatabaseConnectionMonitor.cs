using System.Diagnostics;
using DigitalMe.Web.Data;
using DigitalMe.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DigitalMe.Web.Services;

public interface IDatabaseConnectionMonitor
{
    Task<DatabasePoolMetrics> GetPoolMetricsAsync(CancellationToken cancellationToken = default);
    Task<double> GetPoolEfficiencyAsync(CancellationToken cancellationToken = default);
    Task<bool> IsPoolHealthyAsync(CancellationToken cancellationToken = default);
    void RecordConnectionOpened();
    void RecordConnectionClosed();
    void RecordQueryExecution(TimeSpan duration);
}

public class DatabaseConnectionMonitor : IDatabaseConnectionMonitor, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseConnectionMonitor> _logger;
    private readonly DigitalMeConfiguration _config;
    private readonly Timer _metricsTimer;
    
    // Metrics tracking
    private long _totalConnectionsOpened = 0;
    private long _totalConnectionsClosed = 0;
    private long _totalQueriesExecuted = 0;
    private readonly Queue<DateTime> _recentConnections = new();
    private readonly Queue<TimeSpan> _recentQueryDurations = new();
    private readonly object _metricsLock = new();

    public DatabaseConnectionMonitor(
        IServiceProvider serviceProvider, 
        ILogger<DatabaseConnectionMonitor> logger,
        IOptions<DigitalMeConfiguration> config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = config.Value;
        
        // Clean up old metrics every minute
        _metricsTimer = new Timer(CleanupOldMetrics, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public async Task<DatabasePoolMetrics> GetPoolMetricsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
            
            var connectionString = context.Database.GetConnectionString();
            var poolSize = ExtractPoolSizeFromConnectionString(connectionString);
            
            // Test connection performance
            var stopwatch = Stopwatch.StartNew();
            var canConnect = await context.Database.CanConnectAsync(cancellationToken);
            stopwatch.Stop();
            
            lock (_metricsLock)
            {
                var now = DateTime.UtcNow;
                var oneMinuteAgo = now.AddMinutes(-1);
                
                var connectionsInLastMinute = _recentConnections.Count(c => c > oneMinuteAgo);
                var queriesInLastMinute = _recentQueryDurations
                    .Where((_, index) => _recentConnections.ElementAtOrDefault(index) > oneMinuteAgo)
                    .Count();
                
                var averageQueryDuration = _recentQueryDurations.Any() 
                    ? _recentQueryDurations.Average(q => q.TotalMilliseconds)
                    : 0;

                var poolEfficiency = CalculatePoolEfficiency();
                
                return new DatabasePoolMetrics
                {
                    MinPoolSize = poolSize.min,
                    MaxPoolSize = poolSize.max,
                    CurrentActiveConnections = connectionsInLastMinute,
                    TotalConnectionsOpened = _totalConnectionsOpened,
                    TotalConnectionsClosed = _totalConnectionsClosed,
                    ConnectionsPerMinute = connectionsInLastMinute,
                    QueriesPerMinute = queriesInLastMinute,
                    AverageQueryDurationMs = averageQueryDuration,
                    ConnectionTestDurationMs = stopwatch.ElapsedMilliseconds,
                    CanConnect = canConnect,
                    PoolEfficiency = poolEfficiency,
                    CollectedAt = now
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting database pool metrics");
            return new DatabasePoolMetrics 
            { 
                CanConnect = false, 
                CollectedAt = DateTime.UtcNow 
            };
        }
    }

    public Task<double> GetPoolEfficiencyAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CalculatePoolEfficiency());
    }

    private double CalculatePoolEfficiency()
    {
        try
        {
            lock (_metricsLock)
            {
                if (_recentQueryDurations.Count == 0)
                    return 100.0; // Perfect efficiency if no queries

                var averageQueryTime = _recentQueryDurations.Average(q => q.TotalMilliseconds);
                var optimalQueryTime = 50; // 50ms as baseline for optimal query
                
                // Calculate efficiency: closer to optimal = higher efficiency
                var efficiency = Math.Max(0, 100 - Math.Max(0, (averageQueryTime - optimalQueryTime) / optimalQueryTime * 100));
                
                // Factor in connection reuse
                var connectionReuseRatio = _totalConnectionsClosed > 0 
                    ? Math.Min(1.0, (double)_totalConnectionsClosed / _totalConnectionsOpened)
                    : 0.5;
                
                return Math.Min(100, efficiency * (0.7 + 0.3 * connectionReuseRatio));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating pool efficiency");
            return 0;
        }
    }

    public async Task<bool> IsPoolHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var efficiency = await GetPoolEfficiencyAsync(cancellationToken);
            return efficiency >= 90.0; // P2.4 requirement: >90% pool efficiency
        }
        catch
        {
            return false;
        }
    }

    public void RecordConnectionOpened()
    {
        Interlocked.Increment(ref _totalConnectionsOpened);
        lock (_metricsLock)
        {
            _recentConnections.Enqueue(DateTime.UtcNow);
        }
    }

    public void RecordConnectionClosed()
    {
        Interlocked.Increment(ref _totalConnectionsClosed);
    }

    public void RecordQueryExecution(TimeSpan duration)
    {
        Interlocked.Increment(ref _totalQueriesExecuted);
        lock (_metricsLock)
        {
            _recentQueryDurations.Enqueue(duration);
        }
    }

    private void CleanupOldMetrics(object? state)
    {
        try
        {
            lock (_metricsLock)
            {
                var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
                
                // Clean up old connection timestamps
                while (_recentConnections.Count > 0 && _recentConnections.Peek() < fiveMinutesAgo)
                {
                    _recentConnections.Dequeue();
                }
                
                // Keep only the most recent query durations (up to 1000)
                while (_recentQueryDurations.Count > 1000)
                {
                    _recentQueryDurations.Dequeue();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old metrics");
        }
    }

    private static (int min, int max) ExtractPoolSizeFromConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return (10, 200); // Default production values

        try
        {
            var parts = connectionString.Split(';');
            var minSize = 10; // Default
            var maxSize = 200; // Default

            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToLowerInvariant();
                    var value = keyValue[1].Trim();

                    if (key.Contains("minimum") && key.Contains("pool") && int.TryParse(value, out var min))
                        minSize = min;
                    else if (key.Contains("maximum") && key.Contains("pool") && int.TryParse(value, out var max))
                        maxSize = max;
                }
            }

            return (minSize, maxSize);
        }
        catch
        {
            return (10, 200); // Default on error
        }
    }

    public void Dispose()
    {
        _metricsTimer?.Dispose();
    }
}

public class DatabasePoolMetrics
{
    public int MinPoolSize { get; set; }
    public int MaxPoolSize { get; set; }
    public int CurrentActiveConnections { get; set; }
    public long TotalConnectionsOpened { get; set; }
    public long TotalConnectionsClosed { get; set; }
    public int ConnectionsPerMinute { get; set; }
    public int QueriesPerMinute { get; set; }
    public double AverageQueryDurationMs { get; set; }
    public double ConnectionTestDurationMs { get; set; }
    public bool CanConnect { get; set; }
    public double PoolEfficiency { get; set; }
    public DateTime CollectedAt { get; set; }
}