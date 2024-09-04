using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;

namespace DigitalMe.Web.Services;

/// <summary>
/// Comprehensive query performance monitoring service for production optimization
/// Tracks slow queries, cache performance, and provides actionable insights
/// </summary>
public interface IQueryPerformanceMonitorService
{
    Task<QueryPerformanceReport> GetPerformanceReportAsync(TimeSpan timeWindow);
    Task<List<SlowQuery>> GetSlowQueriesAsync(int limit = 50);
    Task<CachePerformanceStats> GetCachePerformanceAsync();
    Task<List<QueryOptimizationSuggestion>> GetOptimizationSuggestionsAsync();
}

public class QueryPerformanceMonitorService : IQueryPerformanceMonitorService
{
    private readonly ConcurrentQueue<QueryExecutionRecord> _queryRecords;
    private readonly ConcurrentQueue<CacheAccessRecord> _cacheRecords;
    private readonly ILogger<QueryPerformanceMonitorService> _logger;
    private readonly Timer _cleanupTimer;
    private readonly object _lock = new();
    
    // Configuration
    private const int MAX_RECORDS = 10000;
    private const int SLOW_QUERY_THRESHOLD_MS = 100;
    private const int VERY_SLOW_QUERY_THRESHOLD_MS = 1000;
    
    public QueryPerformanceMonitorService(ILogger<QueryPerformanceMonitorService> logger)
    {
        _queryRecords = new ConcurrentQueue<QueryExecutionRecord>();
        _cacheRecords = new ConcurrentQueue<CacheAccessRecord>();
        _logger = logger;
        
        // Clean up old records every 5 minutes
        _cleanupTimer = new Timer(CleanupOldRecords, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }
    
    public void RecordQueryExecution(string sql, TimeSpan duration, int rowCount, string? queryPlan = null)
    {
        var record = new QueryExecutionRecord
        {
            Sql = NormalizeSql(sql),
            Duration = duration,
            RowCount = rowCount,
            ExecutedAt = DateTime.UtcNow,
            QueryPlan = queryPlan,
            IsSlowQuery = duration.TotalMilliseconds > SLOW_QUERY_THRESHOLD_MS
        };
        
        _queryRecords.Enqueue(record);
        
        // Log very slow queries immediately
        if (duration.TotalMilliseconds > VERY_SLOW_QUERY_THRESHOLD_MS)
        {
            _logger.LogWarning("Very slow query detected: {Duration}ms - {Sql}", 
                duration.TotalMilliseconds, record.Sql);
        }
        
        // Maintain size limit
        while (_queryRecords.Count > MAX_RECORDS)
        {
            _queryRecords.TryDequeue(out _);
        }
    }
    
    public void RecordCacheAccess(string key, bool isHit, TimeSpan duration)
    {
        var record = new CacheAccessRecord
        {
            Key = key,
            IsHit = isHit,
            Duration = duration,
            AccessedAt = DateTime.UtcNow
        };
        
        _cacheRecords.Enqueue(record);
        
        // Maintain size limit
        while (_cacheRecords.Count > MAX_RECORDS)
        {
            _cacheRecords.TryDequeue(out _);
        }
    }
    
    public async Task<QueryPerformanceReport> GetPerformanceReportAsync(TimeSpan timeWindow)
    {
        var cutoff = DateTime.UtcNow - timeWindow;
        var recentQueries = _queryRecords.Where(r => r.ExecutedAt > cutoff).ToList();
        var recentCacheAccess = _cacheRecords.Where(r => r.AccessedAt > cutoff).ToList();
        
        var report = new QueryPerformanceReport
        {
            TimeWindow = timeWindow,
            GeneratedAt = DateTime.UtcNow,
            
            // Query Statistics
            TotalQueries = recentQueries.Count,
            AverageQueryDuration = recentQueries.Any() ? 
                recentQueries.Average(q => q.Duration.TotalMilliseconds) : 0,
            SlowQueryCount = recentQueries.Count(q => q.IsSlowQuery),
            SlowQueryPercentage = recentQueries.Any() ? 
                (double)recentQueries.Count(q => q.IsSlowQuery) / recentQueries.Count * 100 : 0,
            
            // Query Type Analysis
            QueryTypeBreakdown = AnalyzeQueryTypes(recentQueries),
            
            // Performance Trends
            HourlyQueryCounts = AnalyzeHourlyTrends(recentQueries),
            
            // Cache Statistics
            CacheStats = AnalyzeCachePerformance(recentCacheAccess),
            
            // Top Issues
            SlowestQueries = recentQueries
                .OrderByDescending(q => q.Duration)
                .Take(10)
                .Select(q => new SlowQuery
                {
                    Sql = q.Sql,
                    Duration = q.Duration,
                    RowCount = q.RowCount,
                    ExecutedAt = q.ExecutedAt,
                    QueryPlan = q.QueryPlan
                })
                .ToList(),
                
            MostFrequentQueries = recentQueries
                .GroupBy(q => q.Sql)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new FrequentQuery
                {
                    Sql = g.Key,
                    ExecutionCount = g.Count(),
                    AverageDuration = g.Average(q => q.Duration.TotalMilliseconds),
                    TotalDuration = g.Sum(q => q.Duration.TotalMilliseconds)
                })
                .ToList()
        };
        
        return await Task.FromResult(report);
    }
    
    public async Task<List<SlowQuery>> GetSlowQueriesAsync(int limit = 50)
    {
        var slowQueries = _queryRecords
            .Where(r => r.IsSlowQuery)
            .OrderByDescending(r => r.Duration)
            .Take(limit)
            .Select(r => new SlowQuery
            {
                Sql = r.Sql,
                Duration = r.Duration,
                RowCount = r.RowCount,
                ExecutedAt = r.ExecutedAt,
                QueryPlan = r.QueryPlan
            })
            .ToList();
        
        return await Task.FromResult(slowQueries);
    }
    
    public async Task<CachePerformanceStats> GetCachePerformanceAsync()
    {
        var recentCache = _cacheRecords.Where(r => r.AccessedAt > DateTime.UtcNow.AddHours(-1)).ToList();
        
        var stats = new CachePerformanceStats
        {
            TotalAccesses = recentCache.Count,
            HitCount = recentCache.Count(r => r.IsHit),
            MissCount = recentCache.Count(r => !r.IsHit),
            HitRatio = recentCache.Any() ? 
                (double)recentCache.Count(r => r.IsHit) / recentCache.Count * 100 : 0,
            AverageAccessTime = recentCache.Any() ? 
                recentCache.Average(r => r.Duration.TotalMilliseconds) : 0,
            
            KeyPatternStats = recentCache
                .GroupBy(r => ExtractKeyPattern(r.Key))
                .Select(g => new KeyPatternStat
                {
                    Pattern = g.Key,
                    Accesses = g.Count(),
                    HitRatio = (double)g.Count(r => r.IsHit) / g.Count() * 100
                })
                .OrderByDescending(s => s.Accesses)
                .ToList()
        };
        
        return await Task.FromResult(stats);
    }
    
    public async Task<List<QueryOptimizationSuggestion>> GetOptimizationSuggestionsAsync()
    {
        var suggestions = new List<QueryOptimizationSuggestion>();
        var recentQueries = _queryRecords.Where(r => r.ExecutedAt > DateTime.UtcNow.AddHours(-24)).ToList();
        
        // Analyze for common optimization opportunities
        AnalyzeMissingIndexSuggestions(recentQueries, suggestions);
        AnalyzeCachingOpportunities(recentQueries, suggestions);
        AnalyzeQueryPatternIssues(recentQueries, suggestions);
        
        return await Task.FromResult(suggestions.OrderByDescending(s => s.Priority).ToList());
    }
    
    private string NormalizeSql(string sql)
    {
        // Normalize SQL by removing parameter values and formatting
        // This helps group similar queries together
        return System.Text.RegularExpressions.Regex.Replace(sql, @"'[^']*'|[0-9]+", "?")
            .Trim()
            .Substring(0, Math.Min(sql.Length, 500)); // Truncate very long queries
    }
    
    private Dictionary<string, QueryTypeStats> AnalyzeQueryTypes(List<QueryExecutionRecord> queries)
    {
        return queries
            .GroupBy(q => DetermineQueryType(q.Sql))
            .ToDictionary(g => g.Key, g => new QueryTypeStats
            {
                Count = g.Count(),
                AverageDuration = g.Average(q => q.Duration.TotalMilliseconds),
                MaxDuration = g.Max(q => q.Duration.TotalMilliseconds)
            });
    }
    
    private string DetermineQueryType(string sql)
    {
        var sqlUpper = sql.ToUpperInvariant().Trim();
        if (sqlUpper.StartsWith("SELECT")) return "SELECT";
        if (sqlUpper.StartsWith("INSERT")) return "INSERT";
        if (sqlUpper.StartsWith("UPDATE")) return "UPDATE";
        if (sqlUpper.StartsWith("DELETE")) return "DELETE";
        return "OTHER";
    }
    
    private Dictionary<int, int> AnalyzeHourlyTrends(List<QueryExecutionRecord> queries)
    {
        return queries
            .GroupBy(q => q.ExecutedAt.Hour)
            .ToDictionary(g => g.Key, g => g.Count());
    }
    
    private CachePerformanceStats AnalyzeCachePerformance(List<CacheAccessRecord> cacheAccess)
    {
        if (!cacheAccess.Any())
        {
            return new CachePerformanceStats();
        }
        
        return new CachePerformanceStats
        {
            TotalAccesses = cacheAccess.Count,
            HitCount = cacheAccess.Count(r => r.IsHit),
            MissCount = cacheAccess.Count(r => !r.IsHit),
            HitRatio = (double)cacheAccess.Count(r => r.IsHit) / cacheAccess.Count * 100,
            AverageAccessTime = cacheAccess.Average(r => r.Duration.TotalMilliseconds)
        };
    }
    
    private string ExtractKeyPattern(string key)
    {
        // Extract pattern from cache keys (e.g., "user_profile_123" -> "user_profile_*")
        return System.Text.RegularExpressions.Regex.Replace(key, @"[0-9a-f-]+", "*");
    }
    
    private void AnalyzeMissingIndexSuggestions(List<QueryExecutionRecord> queries, List<QueryOptimizationSuggestion> suggestions)
    {
        // Look for slow queries that might benefit from indexes
        var slowSelectQueries = queries
            .Where(q => q.IsSlowQuery && q.Sql.ToUpperInvariant().Contains("SELECT"))
            .GroupBy(q => q.Sql)
            .Where(g => g.Count() > 1) // Recurring slow queries
            .ToList();
        
        foreach (var queryGroup in slowSelectQueries)
        {
            suggestions.Add(new QueryOptimizationSuggestion
            {
                Type = "Missing Index",
                Priority = queryGroup.Average(q => q.Duration.TotalMilliseconds) > 500 ? 
                    SuggestionPriority.High : SuggestionPriority.Medium,
                Description = $"Consider adding index for frequently executed slow query",
                Query = queryGroup.Key,
                EstimatedImpact = "30-80% performance improvement",
                Implementation = "Analyze query execution plan and add composite index on filtered/joined columns"
            });
        }
    }
    
    private void AnalyzeCachingOpportunities(List<QueryExecutionRecord> queries, List<QueryOptimizationSuggestion> suggestions)
    {
        // Look for frequently executed queries that might benefit from caching
        var frequentQueries = queries
            .Where(q => q.Sql.ToUpperInvariant().StartsWith("SELECT"))
            .GroupBy(q => q.Sql)
            .Where(g => g.Count() > 10) // Frequently executed
            .ToList();
        
        foreach (var queryGroup in frequentQueries)
        {
            suggestions.Add(new QueryOptimizationSuggestion
            {
                Type = "Caching Opportunity",
                Priority = SuggestionPriority.Medium,
                Description = $"Query executed {queryGroup.Count()} times - consider caching",
                Query = queryGroup.Key,
                EstimatedImpact = "50-90% reduction in database load",
                Implementation = "Implement application-level caching with appropriate TTL"
            });
        }
    }
    
    private void AnalyzeQueryPatternIssues(List<QueryExecutionRecord> queries, List<QueryOptimizationSuggestion> suggestions)
    {
        // Look for N+1 query patterns
        var groupedByTimeAndPattern = queries
            .GroupBy(q => new { 
                Minute = q.ExecutedAt.ToString("yyyy-MM-dd HH:mm"),
                Pattern = NormalizeSql(q.Sql)
            })
            .Where(g => g.Count() > 50) // Many similar queries in short time
            .ToList();
        
        foreach (var group in groupedByTimeAndPattern)
        {
            suggestions.Add(new QueryOptimizationSuggestion
            {
                Type = "N+1 Query Pattern",
                Priority = SuggestionPriority.High,
                Description = $"Detected {group.Count()} similar queries in 1 minute - possible N+1 problem",
                Query = group.Key.Pattern,
                EstimatedImpact = "90%+ performance improvement",
                Implementation = "Use Include() for eager loading or implement projection with single query"
            });
        }
    }
    
    private void CleanupOldRecords(object? state)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddHours(-24);
            
            // Clean up old query records
            while (_queryRecords.TryPeek(out var queryRecord) && queryRecord.ExecutedAt < cutoff)
            {
                _queryRecords.TryDequeue(out _);
            }
            
            // Clean up old cache records
            while (_cacheRecords.TryPeek(out var cacheRecord) && cacheRecord.AccessedAt < cutoff)
            {
                _cacheRecords.TryDequeue(out _);
            }
            
            _logger.LogDebug("Cleaned up old performance monitoring records");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during performance monitoring cleanup");
        }
    }
    
    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}

// Data Models
public class QueryExecutionRecord
{
    public string Sql { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public int RowCount { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string? QueryPlan { get; set; }
    public bool IsSlowQuery { get; set; }
}

public class CacheAccessRecord
{
    public string Key { get; set; } = string.Empty;
    public bool IsHit { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime AccessedAt { get; set; }
}

public class QueryPerformanceReport
{
    public TimeSpan TimeWindow { get; set; }
    public DateTime GeneratedAt { get; set; }
    public int TotalQueries { get; set; }
    public double AverageQueryDuration { get; set; }
    public int SlowQueryCount { get; set; }
    public double SlowQueryPercentage { get; set; }
    public Dictionary<string, QueryTypeStats> QueryTypeBreakdown { get; set; } = new();
    public Dictionary<int, int> HourlyQueryCounts { get; set; } = new();
    public CachePerformanceStats CacheStats { get; set; } = new();
    public List<SlowQuery> SlowestQueries { get; set; } = new();
    public List<FrequentQuery> MostFrequentQueries { get; set; } = new();
}

public class QueryTypeStats
{
    public int Count { get; set; }
    public double AverageDuration { get; set; }
    public double MaxDuration { get; set; }
}

public class SlowQuery
{
    public string Sql { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public int RowCount { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string? QueryPlan { get; set; }
}

public class FrequentQuery
{
    public string Sql { get; set; } = string.Empty;
    public int ExecutionCount { get; set; }
    public double AverageDuration { get; set; }
    public double TotalDuration { get; set; }
}

public class CachePerformanceStats
{
    public int TotalAccesses { get; set; }
    public int HitCount { get; set; }
    public int MissCount { get; set; }
    public double HitRatio { get; set; }
    public double AverageAccessTime { get; set; }
    public List<KeyPatternStat> KeyPatternStats { get; set; } = new();
}

public class KeyPatternStat
{
    public string Pattern { get; set; } = string.Empty;
    public int Accesses { get; set; }
    public double HitRatio { get; set; }
}

public class QueryOptimizationSuggestion
{
    public string Type { get; set; } = string.Empty;
    public SuggestionPriority Priority { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public string EstimatedImpact { get; set; } = string.Empty;
    public string Implementation { get; set; } = string.Empty;
}

public enum SuggestionPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// EF Core interceptor to automatically capture query performance metrics
/// </summary>
public class QueryPerformanceInterceptor : DbCommandInterceptor
{
    private readonly QueryPerformanceMonitorService _monitor;
    
    public QueryPerformanceInterceptor(QueryPerformanceMonitorService monitor)
    {
        _monitor = monitor;
    }
    
    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        var duration = eventData.Duration;
        var rowCount = 0;
        
        // Count rows if possible without affecting performance
        if (result.HasRows && duration.TotalMilliseconds > 50) // Only for slower queries
        {
            try
            {
                var position = 0;
                while (result.Read())
                {
                    position++;
                    if (position > 1000) break; // Limit counting for very large results
                }
                rowCount = position;
                
                // Close the reader
                result.Close();
            }
            catch
            {
                // Ignore errors in row counting
            }
        }
        
        _monitor.RecordQueryExecution(command.CommandText, duration, rowCount);
        
        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}