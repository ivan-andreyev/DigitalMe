# MCP Service Implementation Guide 🔧

> **Parent Plan**: [03-02-02-03-mcp-service-architecture.md](03-02-02-03-mcp-service-architecture.md) | **Plan Type**: IMPLEMENTATION GUIDANCE | **LLM Ready**: ✅ YES  
> **Prerequisites**: MCP architecture patterns defined | **Execution Time**: 1.5 days

📍 **Architecture** → **Implementation** → **Services** → **MCP Integration** → **Implementation**

## Implementation Execution Guidance

### File Location Specification
**Target File**: `DigitalMe/Core/Services/McpService.cs`
**Line References**: Create new file with namespace structure

### Constructor Implementation
**File**: `McpService.cs` **Lines**: 20-35
```csharp
public McpService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<McpService> logger)
{
    _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    
    // Load MCP configuration from appsettings
    _mcpConfig = configuration.GetSection("McpService").Get<McpClientConfiguration>()
                ?? throw new InvalidOperationException("McpService configuration section missing");
}
```

### GenerateResponseAsync Implementation
**File**: `McpService.cs` **Lines**: 40-80

**Critical Implementation Points**:
- **Line 45**: `ValidateMcpRequest(request)` input validation
- **Line 50**: `BuildMcpRequestPayload(request)` protocol conversion
- **Line 55**: `await _httpClient.PostAsJsonAsync(_mcpConfig.ChatEndpoint, payload)` HTTP call
- **Line 60**: `response.EnsureSuccessStatusCode()` error handling
- **Line 65**: `await response.Content.ReadFromJsonAsync<McpResponse>()` deserialization
- **Line 70**: `ProcessMcpResponse(mcpResponse)` response processing

**Implementation Template**:
```csharp
public async Task<McpResponse> GenerateResponseAsync(McpChatRequest request)
{
    _logger.LogInformation("Generating MCP response for method {Method}", request.Method);
    
    // Replace with actual validation, HTTP calls, and response processing
    throw new NotImplementedException("MCP protocol integration at lines 45-75 required");
}
```

### GenerateResponseWithToolsAsync Implementation
**File**: `McpService.cs` **Lines**: 85-125

**Critical Implementation Points**:
- **Line 90**: Tool validation and schema checking
- **Line 95**: Enhanced MCP payload with tools array
- **Line 100**: `BuildToolsArray(tools)` tools serialization
- **Line 105**: HTTP call with tools context
- **Line 110**: Tool execution result processing
- **Line 115**: Response merging with tool results

### IsServiceAvailableAsync Implementation
**File**: `McpService.cs` **Lines**: 130-150

**Critical Implementation Points**:
- **Line 135**: `await _httpClient.GetAsync(_mcpConfig.HealthEndpoint)` health check
- **Line 140**: Status code validation (200 OK)
- **Line 145**: Response time measurement for diagnostics

### GetServiceCapabilitiesAsync Implementation
**File**: `McpService.cs` **Lines**: 155-175

**Critical Implementation Points**:
- **Line 160**: `await _httpClient.GetAsync(_mcpConfig.CapabilitiesEndpoint)` capabilities query
- **Line 165**: `ReadFromJsonAsync<McpCapabilities>()` deserialization
- **Line 170**: Capability validation and caching

### Tool Management Implementation
**File**: `McpService.cs` **Lines**: 180-220

**GetAvailableToolsAsync Critical Points**:
- **Line 185**: Tools endpoint HTTP call
- **Line 190**: Tools array deserialization
- **Line 195**: Tool schema validation

**ExecuteToolAsync Critical Points**:
- **Line 205**: Tool parameter validation
- **Line 210**: Tool execution HTTP call
- **Line 215**: Execution result processing

### Error Handling Implementation
**File**: `McpService.cs` **Lines**: 225-260

**Critical Implementation Points**:
- **Line 230**: `HttpRequestException` handling for network failures
- **Line 235**: `TaskCanceledException` for timeouts
- **Line 240**: `JsonException` for deserialization failures
- **Line 245**: Custom `McpServiceException` creation
- **Line 250**: Structured logging with correlation IDs

### Request/Response Processing Helpers
**File**: `McpService.cs` **Lines**: 265-300

**BuildMcpRequestPayload Method**:
```csharp
private object BuildMcpRequestPayload(McpChatRequest request)
{
    return new
    {
        method = request.Method,
        @params = new
        {
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }),
            options = request.Options
        }
    };
}
```

**ValidateMcpRequest Method**:
```csharp
private void ValidateMcpRequest(McpChatRequest request)
{
    if (request == null) throw new ArgumentNullException(nameof(request));
    if (string.IsNullOrEmpty(request.Method)) throw new ArgumentException("Method required");
    if (request.Messages == null || !request.Messages.Any()) 
        throw new ArgumentException("Messages required");
}
```

### Configuration Classes Implementation
**File**: `McpService.cs` **Lines**: 305-340

```csharp
public class McpClientConfiguration
{
    public string BaseUrl { get; set; } = default!;
    public string ChatEndpoint { get; set; } = "chat/completions";
    public string ToolsEndpoint { get; set; } = "tools";
    public string HealthEndpoint { get; set; } = "health";
    public string CapabilitiesEndpoint { get; set; } = "capabilities";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public string ApiKey { get; set; } = default!;
}
```

### Custom Exceptions Implementation
**File**: `McpService.cs` **Lines**: 345-365

```csharp
public class McpServiceException : Exception
{
    public string? McpErrorCode { get; }
    
    public McpServiceException(string message, string? mcpErrorCode = null) 
        : base(message)
    {
        McpErrorCode = mcpErrorCode;
    }
    
    public McpServiceException(string message, Exception innerException, string? mcpErrorCode = null) 
        : base(message, innerException)
    {
        McpErrorCode = mcpErrorCode;
    }
}
```

### Dependency Injection Registration
**File**: `Program.cs` or `ServiceCollectionExtensions.cs`
```csharp
services.AddHttpClient<McpService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "DigitalMe/1.0");
});
services.AddScoped<IMcpService, McpService>();
```

### Configuration in appsettings.json
```json
{
  "McpService": {
    "BaseUrl": "https://api.mcp-service.com",
    "ChatEndpoint": "chat/completions",
    "ToolsEndpoint": "tools",
    "HealthEndpoint": "health", 
    "CapabilitiesEndpoint": "capabilities",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "ApiKey": "your-mcp-api-key"
  }
}
```

### Success Criteria - Measurable Implementation

✅ **File Creation**: McpService.cs exists in Core/Services/
✅ **Method Count**: 7 public methods implemented (not NotImplementedException)
✅ **HTTP Integration**: HttpClient calls in 5+ methods
✅ **Error Handling**: Try-catch blocks in all HTTP operations
✅ **Configuration**: McpClientConfiguration class with 7+ properties
✅ **Validation**: Input validation on all public methods
✅ **Logging**: Structured logging on all major operations
✅ **Custom Exceptions**: McpServiceException with error codes
✅ **Unit Tests**: 12+ test methods covering success and error scenarios
✅ **Integration Tests**: 3+ tests with mock HTTP responses

### Performance Targets
- **GenerateResponseAsync**: < 2000ms for simple requests
- **GenerateResponseWithToolsAsync**: < 5000ms with tool execution
- **IsServiceAvailableAsync**: < 500ms health check response
- **GetAvailableToolsAsync**: < 1000ms tools enumeration

### MCP Protocol Compliance
- **JSON-RPC 2.0**: Proper request/response format
- **Method Routing**: Correct MCP method names
- **Error Codes**: Standard MCP error code handling
- **Schema Validation**: Tool parameter schema compliance

---

## 🔗 NAVIGATION & DEPENDENCIES

### Prerequisites
- **Architecture**: [03-02-02-03-mcp-service-architecture.md](03-02-02-03-mcp-service-architecture.md) must be reviewed
- **HttpClient**: Properly configured HTTP client with timeouts
- **Configuration**: MCP service configuration in appsettings.json

### Next Steps
- **Integration Testing**: Create end-to-end MCP integration tests
- **Tool Implementation**: Implement specific MCP tools
- **Performance Monitoring**: Add metrics collection

---

## 📊 PLAN METADATA

- **Type**: IMPLEMENTATION GUIDANCE PLAN
- **LLM Ready**: ✅ YES
- **Implementation Depth**: 15% guidance / 85% architectural direction
- **Execution Time**: 1.5 days  
- **Lines**: 198 (under 400 limit)
- **Concrete Targets**: File:line references, measurable success criteria
- **Balance Compliance**: ✅ IMPLEMENTATION GUIDANCE FOCUS maintained