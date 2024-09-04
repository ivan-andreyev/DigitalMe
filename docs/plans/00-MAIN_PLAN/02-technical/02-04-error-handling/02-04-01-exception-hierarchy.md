# Custom Exception Hierarchy

**Родительский план**: [../02-04-error-handling.md](../02-04-error-handling.md)

## Base Exception Class
**Файл**: `src/DigitalMe.Core/Exceptions/DigitalMeException.cs:1-25`

```csharp
using System.Runtime.Serialization;

namespace DigitalMe.Core.Exceptions;

/// <summary>
/// Base exception для всех custom исключений в DigitalMe системе
/// </summary>
[Serializable]
public abstract class DigitalMeException : Exception
{
    public string ErrorCode { get; }
    public Dictionary<string, object> Context { get; }
    
    protected DigitalMeException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
        Context = new Dictionary<string, object>();
    }
    
    protected DigitalMeException(string errorCode, string message, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Context = new Dictionary<string, object>();
    }
    
    protected DigitalMeException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ErrorCode = info.GetString(nameof(ErrorCode)) ?? "UNKNOWN";
        Context = (Dictionary<string, object>?)info.GetValue(nameof(Context), typeof(Dictionary<string, object>)) 
                 ?? new Dictionary<string, object>();
    }
}
```

## Domain-Specific Exceptions
**Файл**: `src/DigitalMe.Core/Exceptions/PersonalityServiceException.cs:1-20`

```csharp
namespace DigitalMe.Core.Exceptions;

/// <summary>
/// Исключения связанные с PersonalityService операциями
/// </summary>
public class PersonalityServiceException : DigitalMeException
{
    public const string PROFILE_NOT_FOUND = "PERSONALITY_PROFILE_NOT_FOUND";
    public const string INVALID_TRAIT_VALUE = "INVALID_PERSONALITY_TRAIT_VALUE";
    public const string MOOD_ANALYSIS_FAILED = "MOOD_ANALYSIS_FAILED";
    public const string SYSTEM_PROMPT_GENERATION_FAILED = "SYSTEM_PROMPT_GENERATION_FAILED";
    
    public PersonalityServiceException(string errorCode, string message) : base(errorCode, message) { }
    
    public PersonalityServiceException(string errorCode, string message, Exception innerException) 
        : base(errorCode, message, innerException) { }
    
    public static PersonalityServiceException ProfileNotFound(string profileName)
        => new(PROFILE_NOT_FOUND, $"Personality profile '{profileName}' not found");
        
    public static PersonalityServiceException InvalidTraitValue(string traitName, object value)
        => new(INVALID_TRAIT_VALUE, $"Invalid value '{value}' for trait '{traitName}'");
}
```

**Файл**: `src/DigitalMe.Integrations/MCP/Exceptions/MCPException.cs:1-25`

```csharp
namespace DigitalMe.Integrations.MCP.Exceptions;

/// <summary>
/// MCP протокол исключения
/// </summary>
public class MCPException : DigitalMeException
{
    public const string CONNECTION_FAILED = "MCP_CONNECTION_FAILED";
    public const string REQUEST_TIMEOUT = "MCP_REQUEST_TIMEOUT";
    public const string INVALID_RESPONSE = "MCP_INVALID_RESPONSE";
    public const string TOOL_EXECUTION_FAILED = "MCP_TOOL_EXECUTION_FAILED";
    public const string SESSION_EXPIRED = "MCP_SESSION_EXPIRED";
    
    public MCPException(string message) : base(CONNECTION_FAILED, message) { }
    
    public MCPException(string message, Exception innerException) 
        : base(CONNECTION_FAILED, message, innerException) { }
        
    public MCPException(string errorCode, string message) : base(errorCode, message) { }
    
    public MCPException(string errorCode, string message, Exception innerException) 
        : base(errorCode, message, innerException) { }
        
    public static MCPException Timeout(int timeoutMs)
        => new(REQUEST_TIMEOUT, $"MCP request timed out after {timeoutMs}ms");
        
    public static MCPException SessionExpired(string sessionId)
        => new(SESSION_EXPIRED, $"MCP session {sessionId} has expired");
}
```