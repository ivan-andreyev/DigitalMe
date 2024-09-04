using Microsoft.Extensions.Caching.Memory;
using DigitalMe.Web.Models;
using System.Collections;

namespace DigitalMe.Web.Services;

/// <summary>
/// Service responsible for intelligent cache invalidation when data changes
/// Ensures cache coherency while maintaining performance benefits
/// </summary>
public interface ICacheInvalidationService
{
    Task InvalidateUserProfileAsync(Guid userId);
    Task InvalidateUserChatSessionsAsync(Guid userId);
    Task InvalidateChatSessionAsync(Guid sessionId);
    Task InvalidateChatMessagesAsync(Guid sessionId);
    Task InvalidateSystemConfigurationAsync(string key);
    Task InvalidateSystemConfigurationsAsync(string[] keys);
    
    // Bulk invalidation for complex operations
    Task InvalidateUserDataAsync(Guid userId);
    Task InvalidateSessionDataAsync(Guid sessionId);
}

public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CacheInvalidationService> _logger;
    
    public CacheInvalidationService(IMemoryCache memoryCache, ILogger<CacheInvalidationService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }
    
    public async Task InvalidateUserProfileAsync(Guid userId)
    {
        var keysToInvalidate = new[]
        {
            $"user_profile_{userId}",
            // Also invalidate email-based cache if we don't know the email
            // This is a trade-off between cache efficiency and consistency
        };
        
        foreach (var key in keysToInvalidate)
        {
            _memoryCache.Remove(key);
        }
        
        _logger.LogDebug("Invalidated user profile cache for user {UserId}", userId);
        await Task.CompletedTask;
    }
    
    public async Task InvalidateUserChatSessionsAsync(Guid userId)
    {
        // Remove all cached chat session queries for this user
        var patterns = new[]
        {
            $"user_chats_{userId}_",
            $"recent_summaries_{userId}_",
        };
        
        // Since IMemoryCache doesn't support pattern removal, we'll track keys
        // In production, this would be better handled with a distributed cache like Redis
        RemoveByPattern(patterns);
        
        _logger.LogDebug("Invalidated user chat sessions cache for user {UserId}", userId);
        await Task.CompletedTask;
    }
    
    public async Task InvalidateChatSessionAsync(Guid sessionId)
    {
        var patterns = new[]
        {
            $"chat_session_{sessionId}_",
            $"chat_summary_{sessionId}",
        };
        
        RemoveByPattern(patterns);
        
        _logger.LogDebug("Invalidated chat session cache for session {SessionId}", sessionId);
        await Task.CompletedTask;
    }
    
    public async Task InvalidateChatMessagesAsync(Guid sessionId)
    {
        var patterns = new[]
        {
            $"chat_messages_{sessionId}_",
            $"chat_session_{sessionId}_true", // Sessions with messages included
            $"chat_summary_{sessionId}",
        };
        
        RemoveByPattern(patterns);
        
        _logger.LogDebug("Invalidated chat messages cache for session {SessionId}", sessionId);
        await Task.CompletedTask;
    }
    
    public async Task InvalidateSystemConfigurationAsync(string key)
    {
        var cacheKey = $"sys_config_{key}";
        _memoryCache.Remove(cacheKey);
        
        _logger.LogDebug("Invalidated system configuration cache for key {Key}", key);
        await Task.CompletedTask;
    }
    
    public async Task InvalidateSystemConfigurationsAsync(string[] keys)
    {
        var cacheKey = $"sys_configs_{string.Join(",", keys.OrderBy(k => k))}";
        _memoryCache.Remove(cacheKey);
        
        // Also invalidate individual keys
        foreach (var key in keys)
        {
            await InvalidateSystemConfigurationAsync(key);
        }
        
        _logger.LogDebug("Invalidated system configurations cache for {Count} keys", keys.Length);
    }
    
    public async Task InvalidateUserDataAsync(Guid userId)
    {
        // Comprehensive invalidation for user-related data
        await InvalidateUserProfileAsync(userId);
        await InvalidateUserChatSessionsAsync(userId);
        
        _logger.LogInformation("Performed bulk cache invalidation for user {UserId}", userId);
    }
    
    public async Task InvalidateSessionDataAsync(Guid sessionId)
    {
        // Comprehensive invalidation for session-related data
        await InvalidateChatSessionAsync(sessionId);
        await InvalidateChatMessagesAsync(sessionId);
        
        _logger.LogInformation("Performed bulk cache invalidation for session {SessionId}", sessionId);
    }
    
    private void RemoveByPattern(string[] patterns)
    {
        // Note: IMemoryCache doesn't natively support pattern-based removal
        // In production, consider using Redis with pattern support
        // For now, we implement a limited pattern matching approach
        
        foreach (var pattern in patterns)
        {
            // This is a simplified approach - in production you'd maintain a key registry
            // or use a cache implementation that supports pattern removal
            var field = typeof(MemoryCache).GetField("_coherentState", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var coherentState = field?.GetValue(_memoryCache);
            var entriesCollection = coherentState?.GetType()
                .GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic);
            
            if (entriesCollection?.GetValue(coherentState) is IDictionary entries)
            {
                var keysToRemove = new List<object>();
                foreach (DictionaryEntry entry in entries)
                {
                    if (entry.Key.ToString()?.Contains(pattern.TrimEnd('_')) == true)
                    {
                        keysToRemove.Add(entry.Key);
                    }
                }
                
                foreach (var key in keysToRemove)
                {
                    _memoryCache.Remove(key);
                }
            }
        }
    }
}

/// <summary>
/// Extension methods for automatic cache invalidation in repository operations
/// </summary>
public static class CacheInvalidationExtensions
{
    public static async Task InvalidateAfterUserProfileUpdateAsync(
        this ICacheInvalidationService service, 
        UserProfile profile)
    {
        await service.InvalidateUserProfileAsync(profile.Id);
        // If email changed, we might need to invalidate email-based caches too
        // This would require additional logic to track email changes
    }
    
    public static async Task InvalidateAfterChatSessionUpdateAsync(
        this ICacheInvalidationService service, 
        ChatSession session)
    {
        await service.InvalidateChatSessionAsync(session.Id);
        await service.InvalidateUserChatSessionsAsync(session.UserId);
    }
    
    public static async Task InvalidateAfterMessageUpdateAsync(
        this ICacheInvalidationService service, 
        ChatMessageEntity message)
    {
        await service.InvalidateChatMessagesAsync(message.SessionId);
        await service.InvalidateChatSessionAsync(message.SessionId);
        
        // If we can get the user ID from the session, invalidate user summaries too
        // This would require additional context or a lookup
    }
}