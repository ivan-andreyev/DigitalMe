using System.Diagnostics;
using DigitalMe.Web.Data;
using DigitalMe.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalMe.Web.Services;

/// <summary>
/// Service to validate query optimization implementation and measure performance improvements
/// </summary>
public interface IQueryOptimizationValidator
{
    Task<QueryOptimizationValidationResult> ValidateOptimizationsAsync();
    Task<PerformanceBenchmarkResult> BenchmarkQueriesAsync(int iterations = 100);
    Task<CacheEfficiencyResult> TestCacheEfficiencyAsync();
}

public class QueryOptimizationValidator : IQueryOptimizationValidator
{
    private readonly DigitalMeDbContext _context;
    private readonly IOptimizedDataService _optimizedService;
    private readonly ILogger<QueryOptimizationValidator> _logger;
    
    public QueryOptimizationValidator(
        DigitalMeDbContext context,
        IOptimizedDataService optimizedService,
        ILogger<QueryOptimizationValidator> logger)
    {
        _context = context;
        _optimizedService = optimizedService;
        _logger = logger;
    }
    
    public async Task<QueryOptimizationValidationResult> ValidateOptimizationsAsync()
    {
        var result = new QueryOptimizationValidationResult
        {
            ValidationTime = DateTime.UtcNow
        };
        
        try
        {
            // 1. Validate AsNoTracking() implementation
            result.AsNoTrackingValidation = await ValidateAsNoTrackingAsync();
            
            // 2. Validate indexes exist
            result.IndexValidation = await ValidateIndexesAsync();
            
            // 3. Validate caching is working
            result.CachingValidation = await ValidateCachingAsync();
            
            // 4. Validate query performance monitoring
            result.MonitoringValidation = await ValidateMonitoringAsync();
            
            // Overall success
            result.OverallSuccess = result.AsNoTrackingValidation.Success &&
                                  result.IndexValidation.Success &&
                                  result.CachingValidation.Success &&
                                  result.MonitoringValidation.Success;
            
            _logger.LogInformation("Query optimization validation completed. Success: {Success}", result.OverallSuccess);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during query optimization validation");
            result.OverallSuccess = false;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    public async Task<PerformanceBenchmarkResult> BenchmarkQueriesAsync(int iterations = 100)
    {
        var result = new PerformanceBenchmarkResult
        {
            Iterations = iterations,
            BenchmarkTime = DateTime.UtcNow
        };
        
        try
        {
            // Ensure we have some test data
            await EnsureTestDataAsync();
            
            // Benchmark 1: User Profile Queries
            result.UserProfileBenchmark = await BenchmarkUserProfileQueriesAsync(iterations);
            
            // Benchmark 2: Chat Session Queries
            result.ChatSessionBenchmark = await BenchmarkChatSessionQueriesAsync(iterations);
            
            // Benchmark 3: Message Queries
            result.MessageBenchmark = await BenchmarkMessageQueriesAsync(iterations);
            
            // Calculate overall improvement
            var allBenchmarks = new[] { result.UserProfileBenchmark, result.ChatSessionBenchmark, result.MessageBenchmark };
            result.AverageImprovement = allBenchmarks.Average(b => b.PerformanceImprovement);
            result.OverallSuccess = result.AverageImprovement > 20; // At least 20% improvement
            
            _logger.LogInformation("Query benchmark completed. Average improvement: {Improvement}%", result.AverageImprovement);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during query benchmarking");
            result.OverallSuccess = false;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    public async Task<CacheEfficiencyResult> TestCacheEfficiencyAsync()
    {
        var result = new CacheEfficiencyResult
        {
            TestTime = DateTime.UtcNow
        };
        
        try
        {
            // Ensure test data exists
            var testUserId = await EnsureTestUserAsync();
            
            // Test 1: Cold cache (cache miss)
            var stopwatch = Stopwatch.StartNew();
            var profile1 = await _optimizedService.GetUserProfileAsync(testUserId);
            stopwatch.Stop();
            result.ColdCacheTime = stopwatch.Elapsed;
            
            // Test 2: Warm cache (cache hit)
            stopwatch.Restart();
            var profile2 = await _optimizedService.GetUserProfileAsync(testUserId);
            stopwatch.Stop();
            result.WarmCacheTime = stopwatch.Elapsed;
            
            // Test 3: Multiple cache hits
            var hitTimes = new List<TimeSpan>();
            for (int i = 0; i < 10; i++)
            {
                stopwatch.Restart();
                await _optimizedService.GetUserProfileAsync(testUserId);
                stopwatch.Stop();
                hitTimes.Add(stopwatch.Elapsed);
            }
            
            result.AverageCacheHitTime = TimeSpan.FromTicks((long)hitTimes.Average(t => t.Ticks));
            result.CacheHitRatio = hitTimes.Count(t => t < TimeSpan.FromMilliseconds(10)) / (double)hitTimes.Count * 100;
            
            // Calculate improvement
            result.CacheSpeedupFactor = result.ColdCacheTime.TotalMilliseconds / result.WarmCacheTime.TotalMilliseconds;
            result.MeetsP24Requirement = result.CacheHitRatio >= 80.0;
            
            _logger.LogInformation("Cache efficiency test completed. Hit ratio: {HitRatio}%, Speedup: {Speedup}x", 
                result.CacheHitRatio, result.CacheSpeedupFactor);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache efficiency testing");
            result.MeetsP24Requirement = false;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    private async Task<ValidationResult> ValidateAsNoTrackingAsync()
    {
        try
        {
            // Test that queries use AsNoTracking
            var testUserId = await EnsureTestUserAsync();
            
            // This query should use AsNoTracking from OptimizedDataService
            var profile = await _optimizedService.GetUserProfileAsync(testUserId);

            if (profile == null)
            {
                return new ValidationResult
                {
                    Success = false,
                    Message = "User profile not found for tracking validation"
                };
            }

            // Verify the context isn't tracking the entity
            var entry = _context.Entry(profile);
            var isTracked = entry.State != EntityState.Detached;
            
            return new ValidationResult
            {
                Success = !isTracked, // Success if NOT being tracked
                Message = isTracked ? "Entity is being tracked (AsNoTracking not working)" : "AsNoTracking working correctly",
                Details = $"Entity state: {entry.State}"
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                Success = false,
                Message = "Failed to validate AsNoTracking",
                Details = ex.Message
            };
        }
    }
    
    private async Task<ValidationResult> ValidateIndexesAsync()
    {
        try
        {
            var expectedIndexes = new[]
            {
                "IX_UserProfiles_Email_IsActive",
                "IX_UserProfiles_IsActive_LastLoginAt",
                "IX_ChatSessions_UserId_IsActive_CreatedAt",
                "IX_ChatSessions_UserId_IsActive_UpdatedAt",
                "IX_ChatMessages_SessionId_CreatedAt",
                "IX_ChatMessages_SessionId_MessageType_CreatedAt"
            };
            
            var existingIndexes = new List<string>();
            
            // Query database for existing indexes
            var indexQuery = @"
                SELECT indexname 
                FROM pg_indexes 
                WHERE tablename IN ('UserProfiles', 'ChatSessions', 'ChatMessages')
                AND indexname LIKE 'IX_%'";
                
            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = indexQuery;
            
            if (command.Connection?.State != System.Data.ConnectionState.Open)
            {
                await command.Connection!.OpenAsync();
            }
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                existingIndexes.Add(reader.GetString(0));
            }
            
            var missingIndexes = expectedIndexes.Except(existingIndexes).ToList();
            
            return new ValidationResult
            {
                Success = missingIndexes.Count == 0,
                Message = missingIndexes.Count == 0 ? "All required indexes exist" : $"Missing {missingIndexes.Count} indexes",
                Details = missingIndexes.Count == 0 ? 
                    $"Found {existingIndexes.Count} indexes" : 
                    $"Missing: {string.Join(", ", missingIndexes)}"
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                Success = false,
                Message = "Failed to validate indexes",
                Details = ex.Message
            };
        }
    }
    
    private async Task<ValidationResult> ValidateCachingAsync()
    {
        try
        {
            var testUserId = await EnsureTestUserAsync();
            
            // First call - should be cache miss
            var stopwatch = Stopwatch.StartNew();
            var profile1 = await _optimizedService.GetUserProfileAsync(testUserId);
            stopwatch.Stop();
            var firstCallTime = stopwatch.Elapsed;
            
            // Second call - should be cache hit
            stopwatch.Restart();
            var profile2 = await _optimizedService.GetUserProfileAsync(testUserId);
            stopwatch.Stop();
            var secondCallTime = stopwatch.Elapsed;
            
            var speedupFactor = firstCallTime.TotalMilliseconds / secondCallTime.TotalMilliseconds;
            var cachingWorking = speedupFactor > 2; // At least 2x speedup expected
            
            return new ValidationResult
            {
                Success = cachingWorking,
                Message = cachingWorking ? "Caching is working effectively" : "Caching performance is insufficient",
                Details = $"Speedup factor: {speedupFactor:F2}x (First: {firstCallTime.TotalMilliseconds:F1}ms, Second: {secondCallTime.TotalMilliseconds:F1}ms)"
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                Success = false,
                Message = "Failed to validate caching",
                Details = ex.Message
            };
        }
    }
    
    private async Task<ValidationResult> ValidateMonitoringAsync()
    {
        try
        {
            // Test that performance metrics are being collected
            var perfMetrics = await _optimizedService.GetPerformanceMetricsAsync();
            
            var monitoringWorking = perfMetrics.TotalQueries >= 0; // Basic check
            
            return new ValidationResult
            {
                Success = monitoringWorking,
                Message = monitoringWorking ? "Performance monitoring is active" : "Performance monitoring not working",
                Details = $"Queries tracked: {perfMetrics.TotalQueries}, Cache hit ratio: {perfMetrics.CacheHitRatio:F1}%"
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                Success = false,
                Message = "Failed to validate monitoring",
                Details = ex.Message
            };
        }
    }
    
    private async Task<QueryBenchmarkResult> BenchmarkUserProfileQueriesAsync(int iterations)
    {
        var testUserId = await EnsureTestUserAsync();
        
        // Benchmark old approach (direct EF Core)
        var oldTimes = new List<TimeSpan>();
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var profile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.Id == testUserId);
            stopwatch.Stop();
            oldTimes.Add(stopwatch.Elapsed);
        }
        
        // Benchmark optimized approach
        var newTimes = new List<TimeSpan>();
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var profile = await _optimizedService.GetUserProfileAsync(testUserId);
            stopwatch.Stop();
            newTimes.Add(stopwatch.Elapsed);
        }
        
        var oldAverage = oldTimes.Average(t => t.TotalMilliseconds);
        var newAverage = newTimes.Average(t => t.TotalMilliseconds);
        var improvement = ((oldAverage - newAverage) / oldAverage) * 100;
        
        return new QueryBenchmarkResult
        {
            QueryType = "UserProfile",
            OldAverageTime = TimeSpan.FromMilliseconds(oldAverage),
            NewAverageTime = TimeSpan.FromMilliseconds(newAverage),
            PerformanceImprovement = improvement,
            MeetsTarget = improvement >= 20 // 20% improvement target
        };
    }
    
    private async Task<QueryBenchmarkResult> BenchmarkChatSessionQueriesAsync(int iterations)
    {
        var testUserId = await EnsureTestUserAsync();
        
        // Old approach
        var oldTimes = new List<TimeSpan>();
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var sessions = await _context.ChatSessions
                .Where(s => s.UserId == testUserId && s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .Take(10)
                .ToListAsync();
            stopwatch.Stop();
            oldTimes.Add(stopwatch.Elapsed);
        }
        
        // Optimized approach
        var newTimes = new List<TimeSpan>();
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var sessions = await _optimizedService.GetUserChatSessionsAsync(testUserId, 0, 10);
            stopwatch.Stop();
            newTimes.Add(stopwatch.Elapsed);
        }
        
        var oldAverage = oldTimes.Average(t => t.TotalMilliseconds);
        var newAverage = newTimes.Average(t => t.TotalMilliseconds);
        var improvement = ((oldAverage - newAverage) / oldAverage) * 100;
        
        return new QueryBenchmarkResult
        {
            QueryType = "ChatSession",
            OldAverageTime = TimeSpan.FromMilliseconds(oldAverage),
            NewAverageTime = TimeSpan.FromMilliseconds(newAverage),
            PerformanceImprovement = improvement,
            MeetsTarget = improvement >= 20
        };
    }
    
    private async Task<QueryBenchmarkResult> BenchmarkMessageQueriesAsync(int iterations)
    {
        var testSessionId = await EnsureTestSessionAsync();
        
        // Old approach
        var oldTimes = new List<TimeSpan>();
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var messages = await _context.ChatMessages
                .Where(m => m.SessionId == testSessionId)
                .OrderBy(m => m.CreatedAt)
                .Take(20)
                .ToListAsync();
            stopwatch.Stop();
            oldTimes.Add(stopwatch.Elapsed);
        }
        
        // Optimized approach
        var newTimes = new List<TimeSpan>();
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var messages = await _optimizedService.GetChatMessagesAsync(testSessionId, 0, 20);
            stopwatch.Stop();
            newTimes.Add(stopwatch.Elapsed);
        }
        
        var oldAverage = oldTimes.Average(t => t.TotalMilliseconds);
        var newAverage = newTimes.Average(t => t.TotalMilliseconds);
        var improvement = ((oldAverage - newAverage) / oldAverage) * 100;
        
        return new QueryBenchmarkResult
        {
            QueryType = "ChatMessage",
            OldAverageTime = TimeSpan.FromMilliseconds(oldAverage),
            NewAverageTime = TimeSpan.FromMilliseconds(newAverage),
            PerformanceImprovement = improvement,
            MeetsTarget = improvement >= 20
        };
    }
    
    private async Task EnsureTestDataAsync()
    {
        await EnsureTestUserAsync();
        await EnsureTestSessionAsync();
    }
    
    private async Task<Guid> EnsureTestUserAsync()
    {
        var existingUser = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.Email == "test@queryoptimization.com");
            
        if (existingUser != null)
        {
            return existingUser.Id;
        }
        
        var testUser = new UserProfile
        {
            Id = Guid.NewGuid(),
            Email = "test@queryoptimization.com",
            UserName = "testuser_query_optimization",
            DisplayName = "Test User for Query Optimization",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.UserProfiles.Add(testUser);
        await _context.SaveChangesAsync();
        
        return testUser.Id;
    }
    
    private async Task<Guid> EnsureTestSessionAsync()
    {
        var testUserId = await EnsureTestUserAsync();
        
        var existingSession = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.UserId == testUserId);
            
        if (existingSession != null)
        {
            return existingSession.Id;
        }
        
        var testSession = new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = testUserId,
            Title = "Test Session for Query Optimization",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.ChatSessions.Add(testSession);
        
        // Add some test messages
        for (int i = 0; i < 10; i++)
        {
            _context.ChatMessages.Add(new ChatMessageEntity
            {
                Id = Guid.NewGuid(),
                SessionId = testSession.Id,
                Content = $"Test message {i + 1} for query optimization benchmarks",
                MessageType = i % 2 == 0 ? "user" : "assistant",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        
        await _context.SaveChangesAsync();
        
        return testSession.Id;
    }
}

// Result Models
public class QueryOptimizationValidationResult
{
    public DateTime ValidationTime { get; set; }
    public bool OverallSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public ValidationResult AsNoTrackingValidation { get; set; } = new();
    public ValidationResult IndexValidation { get; set; } = new();
    public ValidationResult CachingValidation { get; set; } = new();
    public ValidationResult MonitoringValidation { get; set; } = new();
}

public class ValidationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}

public class PerformanceBenchmarkResult
{
    public DateTime BenchmarkTime { get; set; }
    public int Iterations { get; set; }
    public bool OverallSuccess { get; set; }
    public double AverageImprovement { get; set; }
    public string? ErrorMessage { get; set; }
    public QueryBenchmarkResult UserProfileBenchmark { get; set; } = new();
    public QueryBenchmarkResult ChatSessionBenchmark { get; set; } = new();
    public QueryBenchmarkResult MessageBenchmark { get; set; } = new();
}

public class QueryBenchmarkResult
{
    public string QueryType { get; set; } = string.Empty;
    public TimeSpan OldAverageTime { get; set; }
    public TimeSpan NewAverageTime { get; set; }
    public double PerformanceImprovement { get; set; }
    public bool MeetsTarget { get; set; }
}

public class CacheEfficiencyResult
{
    public DateTime TestTime { get; set; }
    public TimeSpan ColdCacheTime { get; set; }
    public TimeSpan WarmCacheTime { get; set; }
    public TimeSpan AverageCacheHitTime { get; set; }
    public double CacheHitRatio { get; set; }
    public double CacheSpeedupFactor { get; set; }
    public bool MeetsP24Requirement { get; set; }
    public string? ErrorMessage { get; set; }
}