# MCP Service Implementation

**Родительский план**: [../02-02-mcp-integration.md](../02-02-mcp-integration.md)

## Overview
Complete implementation specifications for Model Context Protocol (MCP) integration with Claude Code, enabling seamless communication between the Digital Clone backend and LLM reasoning engine.

## Core MCP Service Interface

### IMCPService Interface
**File**: `src/DigitalMe.Integrations/MCP/IMCPService.cs`

```csharp
public interface IMCPService
{
    // Core messaging
    Task<string> SendMessageAsync(string message, PersonalityContext context);
    Task<MCPResponse> SendMessageWithToolsAsync(string message, PersonalityContext context, IEnumerable<MCPTool> availableTools);
    
    // Tool management
    Task<MCPResponse> CallToolAsync(string toolName, Dictionary<string, object> parameters);
    Task<IEnumerable<MCPTool>> GetAvailableToolsAsync();
    Task<bool> RegisterToolAsync(MCPTool tool);
    
    // Session management
    Task InitializeSessionAsync();
    Task<bool> IsConnectedAsync();
    Task DisconnectAsync();
    
    // Context management
    Task UpdatePersonalityContextAsync(PersonalityContext context);
    Task<PersonalityContext> GetCurrentContextAsync();
    
    // Health monitoring
    Task<MCPHealthStatus> CheckHealthAsync();
}
```

## MCP Service Implementation

### Core MCPService Class
**File**: `src/DigitalMe.Integrations/MCP/MCPService.cs`

```csharp
public class MCPService : IMCPService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MCPService> _logger;
    private readonly MCPConfiguration _mcpConfig;
    private string _sessionId;
    private PersonalityContext _currentContext;

    public MCPService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<MCPService> logger,
        IOptions<MCPConfiguration> mcpConfig)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _mcpConfig = mcpConfig.Value;
        
        ConfigureHttpClient();
    }

    public async Task<string> SendMessageAsync(string message, PersonalityContext context)
    {
        try
        {
            var request = new MCPMessageRequest
            {
                SessionId = _sessionId,
                Message = message,
                Context = context,
                Timestamp = DateTime.UtcNow
            };

            var response = await SendMCPRequestAsync<MCPMessageResponse>("message", request);
            
            _logger.LogInformation("MCP message sent successfully. ResponseId: {ResponseId}", response.Id);
            return response.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send MCP message: {Message}", message);
            throw;
        }
    }

    public async Task<MCPResponse> CallToolAsync(string toolName, Dictionary<string, object> parameters)
    {
        try
        {
            var request = new MCPToolRequest
            {
                SessionId = _sessionId,
                ToolName = toolName,
                Parameters = parameters,
                Timestamp = DateTime.UtcNow
            };

            var response = await SendMCPRequestAsync<MCPResponse>("tool", request);
            
            _logger.LogInformation("MCP tool called successfully. Tool: {ToolName}, Status: {Status}", 
                toolName, response.Status);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call MCP tool: {ToolName}", toolName);
            throw;
        }
    }

    public async Task InitializeSessionAsync()
    {
        try
        {
            var request = new MCPInitRequest
            {
                ClientId = _mcpConfig.ClientId,
                Version = _mcpConfig.ProtocolVersion,
                Capabilities = _mcpConfig.SupportedCapabilities
            };

            var response = await SendMCPRequestAsync<MCPInitResponse>("initialize", request);
            _sessionId = response.SessionId;
            
            _logger.LogInformation("MCP session initialized. SessionId: {SessionId}", _sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MCP session");
            throw;
        }
    }

    private async Task<T> SendMCPRequestAsync<T>(string endpoint, object request) where T : class
    {
        var json = JsonSerializer.Serialize(request, MCPJsonOptions.Default);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var httpResponse = await _httpClient.PostAsync($"mcp/{endpoint}", content);
        httpResponse.EnsureSuccessStatusCode();
        
        var responseJson = await httpResponse.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseJson, MCPJsonOptions.Default);
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_mcpConfig.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_mcpConfig.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", $"DigitalMe-MCP/{_mcpConfig.Version}");
        _httpClient.Timeout = TimeSpan.FromSeconds(_mcpConfig.TimeoutSeconds);
    }
}
```

## MCP Data Models

### Request/Response Models
**File**: `src/DigitalMe.Integrations/MCP/Models/MCPModels.cs`

```csharp
public class MCPMessageRequest
{
    public string SessionId { get; set; }
    public string Message { get; set; }
    public PersonalityContext Context { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MCPMessageResponse
{
    public string Id { get; set; }
    public string Content { get; set; }
    public PersonalityMood DetectedMood { get; set; }
    public double ConfidenceScore { get; set; }
    public IEnumerable<MCPTool> SuggestedTools { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MCPToolRequest
{
    public string SessionId { get; set; }
    public string ToolName { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MCPResponse
{
    public string Id { get; set; }
    public MCPStatus Status { get; set; }
    public object Result { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MCPInitRequest
{
    public string ClientId { get; set; }
    public string Version { get; set; }
    public IEnumerable<string> Capabilities { get; set; }
}

public class MCPInitResponse
{
    public string SessionId { get; set; }
    public string ServerVersion { get; set; }
    public IEnumerable<string> SupportedCapabilities { get; set; }
    public IEnumerable<MCPTool> AvailableTools { get; set; }
}
```

### Supporting Models
```csharp
public class PersonalityContext
{
    public string ProfileName { get; set; }
    public Dictionary<string, double> Traits { get; set; }
    public PersonalityMood CurrentMood { get; set; }
    public IEnumerable<Message> RecentMessages { get; set; }
    public Dictionary<string, object> EnvironmentContext { get; set; }
}

public class MCPTool
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, MCPParameter> Parameters { get; set; }
    public IEnumerable<string> RequiredPermissions { get; set; }
}

public class MCPParameter
{
    public string Type { get; set; }
    public string Description { get; set; }
    public bool Required { get; set; }
    public object DefaultValue { get; set; }
}

public enum MCPStatus
{
    Success = 0, Failed = 1, Timeout = 2, ToolNotFound = 3, PermissionDenied = 4
}

public class MCPHealthStatus
{
    public bool IsConnected { get; set; }
    public string ServerVersion { get; set; }
    public TimeSpan LastResponseTime { get; set; }
    public int ActiveSessions { get; set; }
    public DateTime LastHealthCheck { get; set; }
}
```

## Configuration

### MCPConfiguration Class
**File**: `src/DigitalMe.Integrations/MCP/Configuration/MCPConfiguration.cs`

```csharp
public class MCPConfiguration
{
    public const string SectionName = "MCP";
    
    public string BaseUrl { get; set; } = "https://api.claude.ai/v1/";
    public string ApiKey { get; set; }
    public string ClientId { get; set; } = "DigitalMe-Client";
    public string ProtocolVersion { get; set; } = "2024-11-01";
    public string Version { get; set; } = "1.0.0";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    
    public IEnumerable<string> SupportedCapabilities { get; set; } = new[]
    {
        "messaging",
        "tools",
        "context-management", 
        "personality-adaptation"
    };
}
```

### appsettings Configuration
```json
{
  "MCP": {
    "BaseUrl": "https://api.claude.ai/v1/",
    "ApiKey": "sk-ant-api03-...",
    "ClientId": "DigitalMe-Client-Prod",
    "ProtocolVersion": "2024-11-01",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "RetryDelayMs": 1000
  }
}
```

## Dependency Injection Setup

### Service Registration
**File**: `src/DigitalMe.API/Extensions/ServiceCollectionExtensions.cs`

```csharp
public static IServiceCollection AddMCPIntegration(this IServiceCollection services, IConfiguration configuration)
{
    // Configuration
    services.Configure<MCPConfiguration>(configuration.GetSection(MCPConfiguration.SectionName));
    
    // HTTP Client with Polly retry policy
    services.AddHttpClient<IMCPService, MCPService>(client =>
    {
        var mcpConfig = configuration.GetSection(MCPConfiguration.SectionName).Get<MCPConfiguration>();
        client.BaseAddress = new Uri(mcpConfig.BaseUrl);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {mcpConfig.ApiKey}");
    })
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
    
    // Services
    services.AddScoped<IMCPService, MCPService>();
    
    return services;
}

private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(1000 * Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"MCP retry attempt {retryCount} in {timespan} seconds");
            });
}

private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30));
}
```

## Testing Strategy

### Unit Tests
**File**: `tests/DigitalMe.Tests.Unit/MCP/MCPServiceTests.cs`

```csharp
public class MCPServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly MCPService _mcpService;

    public MCPServiceTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHandler.Object);
        
        var configuration = new Mock<IConfiguration>();
        var logger = new Mock<ILogger<MCPService>>();
        var mcpConfig = Options.Create(new MCPConfiguration());
        
        _mcpService = new MCPService(_httpClient, configuration.Object, logger.Object, mcpConfig);
    }

    [Fact]
    public async Task SendMessageAsync_Should_Return_Response_Content()
    {
        // Arrange
        var expectedResponse = new MCPMessageResponse 
        { 
            Id = "test-id", 
            Content = "Test response",
            ConfidenceScore = 0.95
        };
        
        _mockHandler.Setup(/* HTTP response setup */)
                   .Returns(/* mocked HTTP response */);

        // Act
        var result = await _mcpService.SendMessageAsync("test message", new PersonalityContext());

        // Assert
        Assert.Equal("Test response", result);
    }

    [Fact]
    public async Task CallToolAsync_Should_Handle_Tool_Execution()
    {
        // Test tool execution logic
    }

    [Fact]
    public async Task InitializeSessionAsync_Should_Set_SessionId()
    {
        // Test session initialization
    }
}
```

## Success Criteria
- [ ] MCP service interface fully implemented
- [ ] HTTP client configured with retry policies and circuit breaker
- [ ] Session management working correctly
- [ ] Tool execution functionality operational
- [ ] Configuration system properly set up
- [ ] Comprehensive error handling and logging
- [ ] Unit tests with >90% coverage
- [ ] Integration tests with real MCP endpoints

## Navigation
- **Parent**: [MCP Integration Specification](../02-02-mcp-integration.md)
- **Next**: [MCP Tool Registration](./02-02-02-mcp-tools.md)
- **Related**: [Personality Context Management](./02-02-03-context-management.md)