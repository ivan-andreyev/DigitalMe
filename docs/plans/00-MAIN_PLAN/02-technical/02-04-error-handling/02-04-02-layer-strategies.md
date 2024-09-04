# Error Handling Strategies by Layer

**Родительский план**: [../02-04-error-handling.md](../02-04-error-handling.md)

## Controller Layer Error Handling
**Файл**: `src/DigitalMe.API/Middleware/GlobalExceptionMiddleware.cs:1-80`

```csharp
using System.Net;
using System.Text.Json;
using DigitalMe.Core.Exceptions;
using DigitalMe.Integrations.MCP.Exceptions;

namespace DigitalMe.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
            await HandleExceptionAsync(context, exception);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var errorResponse = exception switch
        {
            // Domain exceptions - line 30-45
            PersonalityServiceException pse => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                ErrorCode = pse.ErrorCode,
                Message = pse.Message,
                Details = pse.Context,
                Timestamp = DateTime.UtcNow
            },
            
            // Integration exceptions - line 46-55  
            MCPException mcpe when mcpe.ErrorCode == MCPException.CONNECTION_FAILED => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.ServiceUnavailable,
                ErrorCode = mcpe.ErrorCode,
                Message = "External service temporarily unavailable",
                Details = new Dictionary<string, object> { ["service"] = "Claude Code MCP" },
                Timestamp = DateTime.UtcNow
            },
            
            MCPException mcpe when mcpe.ErrorCode == MCPException.REQUEST_TIMEOUT => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.RequestTimeout,
                ErrorCode = mcpe.ErrorCode,
                Message = mcpe.Message,
                Timestamp = DateTime.UtcNow
            },
            
            // Validation exceptions - line 56-65
            ArgumentException ae => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                ErrorCode = "INVALID_ARGUMENT",
                Message = ae.Message,
                Details = new Dictionary<string, object> { ["parameter"] = ae.ParamName ?? "unknown" },
                Timestamp = DateTime.UtcNow
            },
            
            // Default server error - line 66-75
            _ => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                ErrorCode = "INTERNAL_SERVER_ERROR",
                Message = "An unexpected error occurred",
                Timestamp = DateTime.UtcNow
            }
        };
        
        context.Response.StatusCode = errorResponse.StatusCode;
        
        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await context.Response.WriteAsync(jsonResponse);
    }
}

// Error response model - line 80-95
public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string ErrorCode { get; set; } = default!;
    public string Message { get; set; } = default!;
    public Dictionary<string, object>? Details { get; set; }
    public DateTime Timestamp { get; set; }
    public string? TraceId { get; set; }
}
```

## Service Layer Error Handling with Retry Logic
**Файл**: `src/DigitalMe.Core/Services/PersonalityService.cs:15-25` (error handling update)

```csharp
public async Task<PersonalityProfile?> GetPersonalityAsync(string name)
{
    // Input validation - line 17-22
    if (string.IsNullOrWhiteSpace(name))
    {
        _logger.LogWarning("GetPersonalityAsync called with empty name");
        throw new ArgumentException("Profile name cannot be empty", nameof(name));
    }
    
    // Cache check with error handling - line 23-35
    var cacheKey = $"personality_{name}";
    try
    {
        if (_cache.TryGetValue(cacheKey, out PersonalityProfile? cached))
        {
            _logger.LogDebug("Personality profile {Name} loaded from cache", name);
            return cached;
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Cache error for profile {Name}, proceeding to database", name);
        // Continue to database lookup if cache fails
    }
    
    // Database lookup with retry policy - line 36-55
    try
    {
        var profile = await ExecuteWithRetryAsync(
            async () => await _repository.GetByNameAsync(name),
            maxAttempts: 3,
            delayMs: 1000,
            operationName: $"GetPersonalityProfile_{name}");
        
        if (profile == null)
        {
            _logger.LogWarning("Personality profile {Name} not found in database", name);
            throw PersonalityServiceException.ProfileNotFound(name);
        }
        
        // Cache successful result - line 45-55
        try
        {
            _cache.Set(cacheKey, profile, TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache profile {Name}", name);
            // Don't fail the request if caching fails
        }
        
        _logger.LogInformation("Personality profile {Name} loaded from database", name);
        return profile;
    }
    catch (PersonalityServiceException)
    {
        throw; // Re-throw domain exceptions as-is
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error loading personality profile {Name}", name);
        throw new PersonalityServiceException(
            PersonalityServiceException.PROFILE_NOT_FOUND,
            $"Failed to load profile due to system error: {name}",
            ex);
    }
}

// Retry helper method - line 60-85
private async Task<T> ExecuteWithRetryAsync<T>(
    Func<Task<T>> operation,
    int maxAttempts,
    int delayMs,
    string operationName)
{
    var attempts = 0;
    Exception? lastException = null;
    
    while (attempts < maxAttempts)
    {
        attempts++;
        try
        {
            return await operation();
        }
        catch (Exception ex) when (attempts < maxAttempts && IsRetryableException(ex))
        {
            lastException = ex;
            _logger.LogWarning(ex, "Attempt {Attempt}/{MaxAttempts} failed for {Operation}, retrying in {Delay}ms", 
                attempts, maxAttempts, operationName, delayMs);
            
            await Task.Delay(delayMs);
            delayMs *= 2; // Exponential backoff
        }
    }
    
    throw lastException ?? new InvalidOperationException($"Operation {operationName} failed after {maxAttempts} attempts");
}

private static bool IsRetryableException(Exception ex) => ex switch
{
    TimeoutException => true,
    HttpRequestException => true,
    InvalidOperationException ioe when ioe.Message.Contains("connection") => true,
    _ => false
};
```