# Exception Handling Architecture 🛡️

> **Parent Plan**: [03-02-01-controllers-implementation.md](../03-02-01-controllers-implementation.md) | **Plan Type**: EXCEPTION ARCHITECTURE | **LLM Ready**: ✅ YES  
> **Prerequisites**: Custom exception classes in Core project | **Execution Time**: 1 day

📍 **Architecture** → **Implementation** → **Controllers** → **Exception Handling**

## Exception Handling Architecture Overview

### Core Responsibilities
- **Global Exception Interception**: Catch all unhandled exceptions
- **Type-Specific Error Responses**: Map exception types to HTTP responses
- **Consistent Error Format**: Standardized error response structure
- **Security**: Prevent sensitive information leakage
- **Logging**: Comprehensive error logging with correlation IDs

### Global Exception Middleware Architecture

```csharp
namespace DigitalMe.API.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    // TODO: Implement constructor with DI
    // TODO: Implement InvokeAsync method
    // TODO: Implement HandleExceptionAsync method
    // TODO: Implement exception type mapping
}
```

### Exception Mapping Architecture

#### Exception-to-HTTP Status Code Design
```csharp
// Architecture pattern for exception mapping:
private (HttpStatusCode statusCode, string errorCode, string message) MapException(Exception exception)
{
    return exception switch
    {
        // Domain-specific exceptions
        PersonalityNotFoundException => (HttpStatusCode.NotFound, "PERSONALITY_NOT_FOUND", exception.Message),
        ConversationNotFoundException => (HttpStatusCode.NotFound, "CONVERSATION_NOT_FOUND", exception.Message),
        InvalidTraitValueException => (HttpStatusCode.BadRequest, "INVALID_TRAIT_VALUE", exception.Message),
        
        // Service-specific exceptions  
        McpServiceException => (HttpStatusCode.BadGateway, "MCP_SERVICE_ERROR", "AI service temporarily unavailable"),
        
        // Built-in exceptions
        ArgumentNullException => (HttpStatusCode.BadRequest, "INVALID_ARGUMENT", "Required parameter is missing"),
        ArgumentException => (HttpStatusCode.BadRequest, "INVALID_ARGUMENT", exception.Message),
        UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "UNAUTHORIZED", "Access denied"),
        TimeoutException => (HttpStatusCode.RequestTimeout, "TIMEOUT", "Request timed out"),
        
        // Default fallback
        _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An internal server error occurred")
    };
}
```

### Middleware Implementation Architecture

```csharp
using System.Net;
using System.Text.Json;
using DigitalMe.API.Models;
using DigitalMe.Core.Exceptions;

namespace DigitalMe.API.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
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
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
            
            // TODO: Add correlation ID to logging context
            // TODO: Implement exception telemetry
            
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // TODO: Implement security-aware error message filtering
        var (statusCode, errorCode, message) = MapExceptionToResponse(exception);
        
        context.Response.StatusCode = (int)statusCode;

        var response = new ErrorResponse
        {
            Message = message,
            ErrorCode = errorCode,
            Timestamp = DateTime.UtcNow
            // TODO: Add correlation ID
            // TODO: Add request context details for debugging
        };

        // TODO: Configure JSON serialization options
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    // TODO: Implement exception mapping logic
    private (HttpStatusCode, string, string) MapExceptionToResponse(Exception exception)
    {
        throw new NotImplementedException("Exception mapping implementation pending");
    }
}
```

### Extension Method Architecture

```csharp
namespace DigitalMe.API.Middleware;

public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
    
    // TODO: Add configuration overloads
    // public static IApplicationBuilder UseGlobalExceptionHandling(
    //     this IApplicationBuilder builder, 
    //     Action<ExceptionHandlingOptions> configureOptions)
    // {
    //     // TODO: Implement configurable exception handling
    //     throw new NotImplementedException();
    // }
}
```

### Custom Exception Classes Architecture

```csharp
// Required custom exception classes in DigitalMe.Core.Exceptions:

namespace DigitalMe.Core.Exceptions;

// TODO: Implement domain-specific exceptions
public class PersonalityNotFoundException : Exception
{
    public string ProfileName { get; }
    
    public PersonalityNotFoundException(string profileName)
        : base($"Personality profile '{profileName}' not found")
    {
        ProfileName = profileName;
    }
}

public class ConversationNotFoundException : Exception
{
    public Guid ConversationId { get; }
    
    public ConversationNotFoundException(Guid conversationId)
        : base($"Conversation '{conversationId}' not found")
    {
        ConversationId = conversationId;
    }
}

public class InvalidTraitValueException : Exception
{
    public string TraitName { get; }
    public object? AttemptedValue { get; }
    
    public InvalidTraitValueException(string traitName, object? attemptedValue, string reason)
        : base($"Invalid value for trait '{traitName}': {reason}")
    {
        TraitName = traitName;
        AttemptedValue = attemptedValue;
    }
}

public class McpServiceException : Exception
{
    public string? ServiceEndpoint { get; }
    
    public McpServiceException(string message, string? serviceEndpoint = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ServiceEndpoint = serviceEndpoint;
    }
}
```

### Error Response Enhancement Architecture

```csharp
namespace DigitalMe.API.Models;

// Enhanced error response with debugging information
public class ErrorResponse
{
    public string Message { get; set; } = default!;
    public string ErrorCode { get; set; } = default!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // TODO: Add correlation tracking
    // public string? CorrelationId { get; set; }
    
    // TODO: Add request context for debugging
    // public string? RequestPath { get; set; }
    // public string? HttpMethod { get; set; }
    
    // TODO: Add structured error details
    public Dictionary<string, object>? Details { get; set; }
    
    // TODO: Add validation error support
    // public List<ValidationError>? ValidationErrors { get; set; }
}
```

### Logging Architecture Integration

```csharp
// Structured logging for exception handling:
public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await _next(context);
    }
    catch (Exception exception)
    {
        // TODO: Implement structured logging with correlation ID
        using var logScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestPath"] = context.Request.Path,
            ["RequestMethod"] = context.Request.Method,
            ["CorrelationId"] = GetOrCreateCorrelationId(context),
            ["UserId"] = GetCurrentUserId(context)
        });

        _logger.LogError(exception, 
            "Unhandled exception in request {RequestPath} {RequestMethod}: {Message}",
            context.Request.Path,
            context.Request.Method, 
            exception.Message);

        await HandleExceptionAsync(context, exception);
    }
}

// TODO: Implement correlation ID management
private string GetOrCreateCorrelationId(HttpContext context)
{
    throw new NotImplementedException("Correlation ID implementation pending");
}

// TODO: Implement user context extraction
private string? GetCurrentUserId(HttpContext context)
{
    throw new NotImplementedException("User context implementation pending");
}
```

### Configuration Architecture

```csharp
// TODO: Configuration options for exception handling behavior
public class ExceptionHandlingOptions
{
    public bool IncludeStackTrace { get; set; } = false;
    public bool IncludeInnerException { get; set; } = false;
    public Dictionary<Type, (HttpStatusCode, string)> CustomMappings { get; set; } = new();
    public List<Type> SensitiveExceptions { get; set; } = new();
}
```

### Startup Configuration Architecture

```csharp
// In Program.cs or Startup.cs:
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // TODO: Add exception handling middleware early in pipeline
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseGlobalExceptionHandling();
    }
    
    // TODO: Add after authentication but before controllers
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseGlobalExceptionHandling(); // Position matters
    
    app.UseControllers();
}
```

## Success Criteria

✅ **Global Coverage**: All unhandled exceptions intercepted
✅ **Consistent Responses**: Standardized error response format
✅ **Security**: No sensitive information leaked in error responses
✅ **Logging**: Comprehensive error logging with context
✅ **Type Safety**: Strongly-typed exception handling
✅ **Extensibility**: Easy to add new exception types

### Implementation Guidance

1. **Create Custom Exceptions**: Define all domain-specific exception classes
2. **Implement Middleware**: Build GlobalExceptionHandlingMiddleware
3. **Add Exception Mapping**: Implement exception-to-HTTP status mapping
4. **Configure Logging**: Set up structured logging with correlation IDs
5. **Test Exception Scenarios**: Verify all exception types handled correctly

---

## 🔗 NAVIGATION & DEPENDENCIES

### Prerequisites
- **Custom Exception Classes**: Must exist in DigitalMe.Core.Exceptions
- **ErrorResponse DTO**: Must be defined in API.Models
- **Logging Configuration**: Structured logging must be configured

### Next Steps
- **Implement**: Fill in NotImplementedException stubs
- **Testing**: Create comprehensive exception handling tests
- **Monitoring**: Integrate with application monitoring/telemetry

### Related Plans
- **Parent**: [03-02-01-controllers-implementation.md](../03-02-01-controllers-implementation.md)
- **DTOs**: [03-02-01-03-dto-models.md](03-02-01-03-dto-models.md)
- **Controllers**: All controller implementations must use this exception handling

---

## 📊 PLAN METADATA

- **Type**: EXCEPTION HANDLING ARCHITECTURE PLAN
- **LLM Ready**: ✅ YES
- **Implementation Depth**: 85% architecture / 15% implementation stubs
- **Execution Time**: 1 day
- **Code Coverage**: ~400 lines architectural guidance
- **Balance Compliance**: ✅ ARCHITECTURAL FOCUS maintained

### 🎯 EXCEPTION HANDLING INDICATORS
- **✅ Global Interception**: All exceptions caught by middleware
- **✅ Type-Specific Mapping**: Exception types mapped to HTTP responses
- **✅ Consistent Format**: Standardized ErrorResponse structure
- **✅ Implementation Stubs**: NotImplementedException placeholders
- **✅ Security Design**: No sensitive information leakage
- **✅ Logging Integration**: Structured logging with context
- **✅ Extensibility**: Easy to add new exception types and mappings