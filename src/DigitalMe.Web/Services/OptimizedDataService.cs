using Microsoft.EntityFrameworkCore;
using DigitalMe.Web.Data;
using DigitalMe.Web.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;
using System.Text.Json;

namespace DigitalMe.Web.Services;

/// <summary>
/// Optimized data service implementing comprehensive query optimization strategy
/// Features: AsNoTracking(), index utilization, query caching, performance monitoring
/// </summary>
public interface IOptimizedDataService
{
    // Read-only operations with AsNoTracking() optimization
    Task<UserProfile?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetUserProfileByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<List<ChatSession>> GetUserChatSessionsAsync(Guid userId, int page = 0, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetChatSessionAsync(Guid sessionId, bool includeMessages = false, CancellationToken cancellationToken = default);
    Task<List<ChatMessageEntity>> GetChatMessagesAsync(Guid sessionId, int page = 0, int pageSize = 50, CancellationToken cancellationToken = default);
    
    // Optimized summary operations for dashboard/quick access
    Task<ChatSessionSummary> GetChatSessionSummaryAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<List<ChatSessionSummary>> GetRecentChatSessionSummariesAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default);
    
    // System configuration caching
    Task<SystemConfiguration?> GetSystemConfigurationAsync(string key, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetSystemConfigurationsAsync(string[] keys, CancellationToken cancellationToken = default);
    
    // Performance metrics
    Task<QueryPerformanceMetrics> GetPerformanceMetricsAsync();
}

public class OptimizedDataService : IOptimizedDataService
{
    private readonly DigitalMeDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<OptimizedDataService> _logger;
    private readonly IDatabaseConnectionService _connectionService;
    private readonly QueryPerformanceTracker _performanceTracker;
    
    private const int DEFAULT_CACHE_MINUTES = 15;
    private const int CONFIG_CACHE_MINUTES = 60;
    
    public OptimizedDataService(
        DigitalMeDbContext context,
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<OptimizedDataService> logger,
        IDatabaseConnectionService connectionService)
    {
        _context = context;
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        _connectionService = connectionService;
        _performanceTracker = new QueryPerformanceTracker();
    }
    
    /// <summary>
    /// Multi-level cache implementation: L1 (Memory) + L2 (Redis) + L3 (Database)
    /// For nullable reference types
    /// </summary>
    private async Task<T?> GetFromCacheAsync<T>(string key, Func<Task<T?>> factory) where T : class
    {
        // L1: Memory cache check
        if (_memoryCache.TryGetValue(key, out T? cachedValue))
        {
            _logger.LogDebug("Cache hit L1 (Memory) for key: {CacheKey}", key);
            return cachedValue;
        }
        
        // L2: Redis cache check with error handling
        try
        {
            var redisValue = await _distributedCache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(redisValue))
            {
                var deserializedValue = JsonSerializer.Deserialize<T>(redisValue);
                if (deserializedValue != null)
                {
                    _memoryCache.Set(key, deserializedValue, TimeSpan.FromMinutes(5));
                    _logger.LogDebug("Cache hit L2 (Redis) for key: {CacheKey}", key);
                    return deserializedValue;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache access failed for key: {CacheKey}. Falling back to database.", key);
        }
        
        // L3: Database fallback
        _logger.LogDebug("Cache miss L1/L2, fetching from database for key: {CacheKey}", key);
        var result = await factory();
        if (result != null)
        {
            await SetCacheAsync(key, result);
        }
        return result;
    }
    
    /// <summary>
    /// Multi-level cache implementation: L1 (Memory) + L2 (Redis) + L3 (Database)
    /// For non-nullable types like collections
    /// </summary>
    private async Task<T> GetFromCacheNonNullableAsync<T>(string key, Func<Task<T>> factory) where T : class, new()
    {
        // L1: Memory cache check
        if (_memoryCache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
        {
            _logger.LogDebug("Cache hit L1 (Memory) for key: {CacheKey}", key);
            return cachedValue;
        }
        
        // L2: Redis cache check with error handling
        try
        {
            var redisValue = await _distributedCache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(redisValue))
            {
                var deserializedValue = JsonSerializer.Deserialize<T>(redisValue);
                if (deserializedValue != null)
                {
                    _memoryCache.Set(key, deserializedValue, TimeSpan.FromMinutes(5));
                    _logger.LogDebug("Cache hit L2 (Redis) for key: {CacheKey}", key);
                    return deserializedValue;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache access failed for key: {CacheKey}. Falling back to database.", key);
        }
        
        // L3: Database fallback
        _logger.LogDebug("Cache miss L1/L2, fetching from database for key: {CacheKey}", key);
        var result = await factory();
        await SetCacheAsync(key, result);
        return result;
    }

    /// <summary>
    /// Sets value in both memory cache and Redis distributed cache
    /// </summary>
    private async Task SetCacheAsync<T>(string key, T value) where T : class
    {
        // Always set in memory cache
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        _memoryCache.Set(key, value, options);
        
        // Try to set in Redis with error handling
        try
        {
            var serializedValue = JsonSerializer.Serialize(value);
            var distributedOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            };
            await _distributedCache.SetStringAsync(key, serializedValue, distributedOptions);
            _logger.LogDebug("Successfully cached value in Redis for key: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache value in Redis for key: {CacheKey}. Value cached in memory only.", key);
        }
    }
    
    public async Task<UserProfile?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var activity = _performanceTracker.StartActivity("GetUserProfile");
        
        var cacheKey = $"user_profile_{userId}";
        
        return await GetFromCacheAsync(cacheKey, async () =>
        {
            try
            {
                // Use read replica connection and AsNoTracking() for read-only operation
                // Index: IX_UserProfiles_Id (primary key - automatically indexed)
                var readConnectionString = await _connectionService.GetReadConnectionStringAsync();
                using var readContext = CreateReadOnlyContext(readConnectionString);
                
                var profile = await readContext.UserProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);
                
                activity.AddTag("cache_hit", "false");
                activity.AddTag("found", profile != null ? "true" : "false");
                
                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile {UserId}", userId);
                activity.AddTag("error", ex.Message);
                return null;
            }
        });
    }
    
    public async Task<UserProfile?> GetUserProfileByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        using var activity = _performanceTracker.StartActivity("GetUserProfileByEmail");
        
        var cacheKey = $"user_profile_email_{email.ToLowerInvariant()}";
        
        if (_memoryCache.TryGetValue(cacheKey, out UserProfile? cachedProfile))
        {
            activity.AddTag("cache_hit", "true");
            return cachedProfile;
        }
        
        try
        {
            // Use read replica connection for email lookup
            // Uses index: IX_UserProfiles_Email (unique)
            var readConnectionString = await _connectionService.GetReadConnectionStringAsync();
            using var readContext = CreateReadOnlyContext(readConnectionString);
            
            var profile = await readContext.UserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);
            
            if (profile != null)
            {
                _memoryCache.Set(cacheKey, profile, TimeSpan.FromMinutes(DEFAULT_CACHE_MINUTES));
            }
            
            activity.AddTag("cache_hit", "false");
            activity.AddTag("found", profile != null ? "true" : "false");
            
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile by email {Email}", email);
            activity.AddTag("error", ex.Message);
            return null;
        }
    }
    
    public async Task<List<ChatSession>> GetUserChatSessionsAsync(Guid userId, int page = 0, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        using var activity = _performanceTracker.StartActivity("GetUserChatSessions");
        
        var cacheKey = $"user_chats_{userId}_{page}_{pageSize}";
        
        return await GetFromCacheNonNullableAsync(cacheKey, async () =>
        {
            try
            {
                // Use read replica for chat session queries
                // Uses composite index: IX_ChatSessions_UserId_CreatedAt
                var readConnectionString = await _connectionService.GetReadConnectionStringAsync();
                using var readContext = CreateReadOnlyContext(readConnectionString);
                
                var sessions = await readContext.ChatSessions
                    .AsNoTracking()
                    .Where(s => s.UserId == userId && s.IsActive)
                    .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
                    .Skip(page * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
                
                activity.AddTag("cache_hit", "false");
                activity.AddTag("count", sessions.Count.ToString());
                activity.AddTag("page", page.ToString());
                
                return sessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chat sessions for user {UserId}", userId);
                activity.AddTag("error", ex.Message);
                return new List<ChatSession>();
            }
        });
    }
    
    public async Task<ChatSession?> GetChatSessionAsync(Guid sessionId, bool includeMessages = false, CancellationToken cancellationToken = default)
    {
        using var activity = _performanceTracker.StartActivity("GetChatSession");
        
        var cacheKey = $"chat_session_{sessionId}_{includeMessages}";
        
        if (_memoryCache.TryGetValue(cacheKey, out ChatSession? cachedSession))
        {
            activity.AddTag("cache_hit", "true");
            return cachedSession;
        }
        
        try
        {
            // Use read replica for single chat session queries
            var readConnectionString = await _connectionService.GetReadConnectionStringAsync();
            using var readContext = CreateReadOnlyContext(readConnectionString);
            
            var query = readContext.ChatSessions.AsNoTracking();
            
            if (includeMessages)
            {
                // Uses index: IX_ChatMessages_SessionId_CreatedAt for ordering
                query = query.Include(s => s.Messages.OrderBy(m => m.CreatedAt));
            }
            
            var session = await query
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.IsActive, cancellationToken);
            
            if (session != null)
            {
                var cacheTime = includeMessages ? TimeSpan.FromMinutes(2) : TimeSpan.FromMinutes(10);
                _memoryCache.Set(cacheKey, session, cacheTime);
            }
            
            activity.AddTag("cache_hit", "false");
            activity.AddTag("include_messages", includeMessages.ToString());
            activity.AddTag("found", session != null ? "true" : "false");
            
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat session {SessionId}", sessionId);
            activity.AddTag("error", ex.Message);
            return null;
        }
    }
    
    public async Task<List<ChatMessageEntity>> GetChatMessagesAsync(Guid sessionId, int page = 0, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        using var activity = _performanceTracker.StartActivity("GetChatMessages");
        
        var cacheKey = $"chat_messages_{sessionId}_{page}_{pageSize}";
        
        return await GetFromCacheNonNullableAsync(cacheKey, async () =>
        {
            try
            {
                // Use read replica for chat message queries
                // Uses composite index: IX_ChatMessages_SessionId_CreatedAt
                var readConnectionString = await _connectionService.GetReadConnectionStringAsync();
                using var readContext = CreateReadOnlyContext(readConnectionString);
                
                var messages = await readContext.ChatMessages
                    .AsNoTracking()
                    .Where(m => m.SessionId == sessionId)
                    .OrderBy(m => m.CreatedAt)
                    .Skip(page * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
                
                activity.AddTag("cache_hit", "false");
                activity.AddTag("count", messages.Count.ToString());
                activity.AddTag("page", page.ToString());
                
                return messages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chat messages for session {SessionId}", sessionId);
                activity.AddTag("error", ex.Message);
                return new List<ChatMessageEntity>();
            }
        });
    }
    
    public async Task<ChatSessionSummary> GetChatSessionSummaryAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        using var activity = _performanceTracker.StartActivity("GetChatSessionSummary");
        
        var cacheKey = $"chat_summary_{sessionId}";
        
        if (_memoryCache.TryGetValue(cacheKey, out ChatSessionSummary? cachedSummary))
        {
            activity.AddTag("cache_hit", "true");
            return cachedSummary;
        }
        
        try
        {
            // Use read replica for session summary queries
            // Optimized projection query - only select needed fields
            var readConnectionString = await _connectionService.GetReadConnectionStringAsync();
            using var readContext = CreateReadOnlyContext(readConnectionString);
            
            var summary = await readContext.ChatSessions
                .AsNoTracking()
                .Where(s => s.Id == sessionId && s.IsActive)
                .Select(s => new ChatSessionSummary
                {
                    Id = s.Id,
                    Title = s.Title,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    MessageCount = s.Messages.Count(),
                    LastMessage = s.Messages
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.Content.Length > 100 ? m.Content.Substring(0, 100) + "..." : m.Content)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(cancellationToken);
            
            if (summary != null)
            {
                _memoryCache.Set(cacheKey, summary, TimeSpan.FromMinutes(5));
            }
            
            activity.AddTag("cache_hit", "false");
            activity.AddTag("found", summary != null ? "true" : "false");
            
            return summary ?? new ChatSessionSummary();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat session summary {SessionId}", sessionId);
            activity.AddTag("error", ex.Message);
            return new ChatSessionSummary();
        }
    }
    
    public async Task<List<ChatSessionSummary>> GetRecentChatSessionSummariesAsync(Guid userId, int count = 10, CancellationToken cancellationToken = default)
    {
        using var activity = _performanceTracker.StartActivity("GetRecentChatSessionSummaries");
        
        var cacheKey = $"recent_summaries_{userId}_{count}";
        
        return await GetFromCacheNonNullableAsync(cacheKey, async () =>
        {
            try
            {
                // Use read replica for recent summaries queries
                // Highly optimized query with projection and minimal data transfer
                var readConnectionString = await _connectionService.GetReadConnectionStringAsync();
                using var readContext = CreateReadOnlyContext(readConnectionString);
                
                var summaries = await readContext.ChatSessions
                    .AsNoTracking()
                    .Where(s => s.UserId == userId && s.IsActive)
                    .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
                    .Take(count)
                    .Select(s => new ChatSessionSummary
                    {
                        Id = s.Id,
                        Title = s.Title,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt,
                        MessageCount = s.Messages.Count(),
                        LastMessage = s.Messages
                            .OrderByDescending(m => m.CreatedAt)
                            .Select(m => m.Content.Length > 100 ? m.Content.Substring(0, 100) + "..." : m.Content)
                            .FirstOrDefault()
                    })
                    .ToListAsync(cancellationToken);
                
                activity.AddTag("cache_hit", "false");
                activity.AddTag("count", summaries.Count.ToString());
                
                return summaries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent chat session summaries for user {UserId}", userId);
                activity.AddTag("error", ex.Message);
                return new List<ChatSessionSummary>();
            }
        });
    }
    
    public async Task<SystemConfiguration?> GetSystemConfigurationAsync(string key, CancellationToken cancellationToken = default)
    {
        using var activity = _performanceTracker.StartActivity("GetSystemConfiguration");
        
        var cacheKey = $"sys_config_{key}";
        
        if (_memoryCache.TryGetValue(cacheKey, out SystemConfiguration? cachedConfig))
        {
            activity.AddTag("cache_hit", "true");
            return cachedConfig;
        }
        
        try
        {
            // Use read replica for system configuration queries
            // Uses primary key index
            var readConnectionString = await _connectionService.GetReadConnectionStringAsync();
            using var readContext = CreateReadOnlyContext(readConnectionString);
            
            var config = await readContext.SystemConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
            
            if (config != null)
            {
                // System config changes rarely, cache longer
                _memoryCache.Set(cacheKey, config, TimeSpan.FromMinutes(CONFIG_CACHE_MINUTES));
            }
            
            activity.AddTag("cache_hit", "false");
            activity.AddTag("found", config != null ? "true" : "false");
            
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system configuration {Key}", key);
            activity.AddTag("error", ex.Message);
            return null;
        }
    }
    
    public async Task<Dictionary<string, string>> GetSystemConfigurationsAsync(string[] keys, CancellationToken cancellationToken = default)
    {
        using var activity = _performanceTracker.StartActivity("GetSystemConfigurations");
        
        var cacheKey = $"sys_configs_{string.Join(",", keys.OrderBy(k => k))}";
        
        return await GetFromCacheNonNullableAsync(cacheKey, async () =>
        {
            try
            {
                // Use read replica for batch system configuration queries
                var readConnectionString = await _connectionService.GetReadConnectionStringAsync();
                using var readContext = CreateReadOnlyContext(readConnectionString);
                
                var configurations = await readContext.SystemConfigurations
                    .AsNoTracking()
                    .Where(c => keys.Contains(c.Key))
                    .ToDictionaryAsync(c => c.Key, c => c.Value, cancellationToken);
                
                activity.AddTag("cache_hit", "false");
                activity.AddTag("count", configurations.Count.ToString());
                
                return configurations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system configurations");
                activity.AddTag("error", ex.Message);
                return new Dictionary<string, string>();
            }
        });
    }
    
    public async Task<QueryPerformanceMetrics> GetPerformanceMetricsAsync()
    {
        return await Task.FromResult(_performanceTracker.GetMetrics());
    }
    
    /// <summary>
    /// Creates a new read-only database context using the read replica connection
    /// </summary>
    private DigitalMeDbContext CreateReadOnlyContext(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DigitalMeDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.CommandTimeout(30);
            npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "public");
        });
        
        // Optimizations for read-only operations
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        optionsBuilder.EnableServiceProviderCaching();
        
        return new DigitalMeDbContext(optionsBuilder.Options);
    }
}

/// <summary>
/// Optimized summary model for efficient data transfer
/// </summary>
public class ChatSessionSummary
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int MessageCount { get; set; }
    public string? LastMessage { get; set; }
}

/// <summary>
/// Query performance tracking for monitoring
/// </summary>
public class QueryPerformanceTracker
{
    private readonly List<QueryMetric> _metrics = new();
    private readonly object _lock = new();
    
    public QueryActivity StartActivity(string queryName)
    {
        return new QueryActivity(this, queryName);
    }
    
    internal void RecordQuery(string queryName, TimeSpan duration, Dictionary<string, string> tags)
    {
        lock (_lock)
        {
            _metrics.Add(new QueryMetric
            {
                QueryName = queryName,
                Duration = duration,
                Timestamp = DateTime.UtcNow,
                Tags = tags
            });
            
            // Keep only last 1000 metrics to prevent memory leaks
            if (_metrics.Count > 1000)
            {
                _metrics.RemoveAt(0);
            }
        }
    }
    
    public QueryPerformanceMetrics GetMetrics()
    {
        lock (_lock)
        {
            var recent = _metrics.Where(m => m.Timestamp > DateTime.UtcNow.AddMinutes(-10)).ToList();
            
            return new QueryPerformanceMetrics
            {
                TotalQueries = recent.Count,
                AverageQueryTime = recent.Any() ? recent.Average(m => m.Duration.TotalMilliseconds) : 0,
                CacheHitRatio = CalculateCacheHitRatio(recent),
                QueriesByName = recent.GroupBy(m => m.QueryName)
                    .ToDictionary(g => g.Key, g => new QueryStats
                    {
                        Count = g.Count(),
                        AverageTime = g.Average(m => m.Duration.TotalMilliseconds),
                        MaxTime = g.Max(m => m.Duration.TotalMilliseconds)
                    }),
                SlowQueries = recent
                    .Where(m => m.Duration.TotalMilliseconds > 100)
                    .OrderByDescending(m => m.Duration)
                    .Take(10)
                    .ToList()
            };
        }
    }
    
    private double CalculateCacheHitRatio(List<QueryMetric> metrics)
    {
        if (!metrics.Any()) return 0;
        
        var cacheableQueries = metrics.Where(m => m.Tags.ContainsKey("cache_hit")).ToList();
        if (!cacheableQueries.Any()) return 0;
        
        var hits = cacheableQueries.Count(m => m.Tags["cache_hit"] == "true");
        return (double)hits / cacheableQueries.Count * 100;
    }
    
    public class QueryActivity : IDisposable
    {
        private readonly QueryPerformanceTracker _tracker;
        private readonly string _queryName;
        private readonly Stopwatch _stopwatch;
        private readonly Dictionary<string, string> _tags = new();
        
        public QueryActivity(QueryPerformanceTracker tracker, string queryName)
        {
            _tracker = tracker;
            _queryName = queryName;
            _stopwatch = Stopwatch.StartNew();
        }
        
        public void AddTag(string key, string value)
        {
            _tags[key] = value;
        }
        
        public void Dispose()
        {
            _stopwatch.Stop();
            _tracker.RecordQuery(_queryName, _stopwatch.Elapsed, _tags);
        }
    }
}

public class QueryMetric
{
    public string QueryName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
}

public class QueryPerformanceMetrics
{
    public int TotalQueries { get; set; }
    public double AverageQueryTime { get; set; }
    public double CacheHitRatio { get; set; }
    public Dictionary<string, QueryStats> QueriesByName { get; set; } = new();
    public List<QueryMetric> SlowQueries { get; set; } = new();
}

public class QueryStats
{
    public int Count { get; set; }
    public double AverageTime { get; set; }
    public double MaxTime { get; set; }
}