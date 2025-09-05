# MCP Integration Service

**Родительский план**: [../02-05-interfaces.md](../02-05-interfaces.md)

## IMCPService Interface
**Файл**: `src/DigitalMe.Integrations/MCP/IMCPService.cs:1-35`

```csharp
public interface IMCPService
{
    /// <summary>
    /// Отправить сообщение Claude Code через MCP протокол
    /// </summary>
    /// <param name="message">Текст сообщения пользователя</param>
    /// <param name="context">Контекст личности и диалога</param>
    /// <returns>Ответ от Claude Code</returns>
    Task<MCPResponse> SendMessageAsync(string message, PersonalityContext context);
    
    /// <summary>
    /// Вызвать инструмент через MCP
    /// </summary>
    /// <param name="toolName">Имя инструмента (send_telegram_message, create_calendar_event)</param>
    /// <param name="parameters">Параметры инструмента</param>
    /// <returns>Результат выполнения инструмента</returns>
    Task<MCPToolResult> CallToolAsync(string toolName, Dictionary<string, object> parameters);
    
    /// <summary>
    /// Инициализировать MCP сессию
    /// </summary>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Статус подключения</returns>
    Task<MCPConnectionStatus> InitializeSessionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Проверить статус подключения
    /// </summary>
    /// <returns>true если подключение активно</returns>
    Task<bool> IsConnectedAsync();
    
    /// <summary>
    /// Получить список доступных инструментов
    /// </summary>
    /// <returns>Список MCP инструментов с описаниями</returns>
    Task<IEnumerable<MCPTool>> GetAvailableToolsAsync();
}
```

## MCPService Implementation
**Файл**: `src/DigitalMe.Integrations/MCP/MCPService.cs:1-200`

```csharp
public class MCPService : IMCPService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<MCPOptions> _options;
    private readonly ILogger<MCPService> _logger;
    private readonly CircuitBreaker _circuitBreaker;
    private string? _sessionId;
    private bool _disposed = false;
    
    public MCPService(
        HttpClient httpClient,
        IOptions<MCPOptions> options,
        ILogger<MCPService> logger,
        CircuitBreaker circuitBreaker)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
        _circuitBreaker = circuitBreaker;
    }
    
    public async Task<MCPResponse> SendMessageAsync(string message, PersonalityContext context)
    {
        // Input validation - line 25-30
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty", nameof(message));
            
        if (context?.Profile == null)
            throw new ArgumentNullException(nameof(context), "PersonalityContext is required");
        
        // Ensure connection - line 31-35
        if (!await IsConnectedAsync())
        {
            var status = await InitializeSessionAsync();
            if (status != MCPConnectionStatus.Connected)
            {
                throw new MCPException("Failed to establish MCP connection");
            }
        }
        
        // Prepare request - line 36-50
        var request = new MCPRequest
        {
            Type = "message",
            Content = message,
            PersonalityProfile = new
            {
                Name = context.Profile.Name,
                Traits = context.Profile.CoreTraits,
                CommunicationStyle = context.Profile.CommunicationStyle,
                CurrentMood = context.CurrentMood?.ToString()
            },
            ConversationHistory = context.RecentMessages.Take(10).Select(m => new
            {
                Role = m.IsFromUser ? "user" : "assistant",
                Content = m.Content,
                Timestamp = m.Timestamp
            }).ToArray(),
            SessionId = _sessionId
        };
        
        // Send with retry and circuit breaker - line 51-65
        try
        {
            var response = await SendWithRetryAsync(request);
            
            _logger.LogInformation("MCP message sent successfully for profile {Profile}, tokens used: {Tokens}", 
                context.Profile.Name, response.TokensUsed);
                
            return response;
        }
        catch (MCPException ex)
        {
            _logger.LogError(ex, "MCP request failed for profile {Profile}: {Error}", 
                context.Profile.Name, ex.Message);
            throw;
        }
    }
    
    public async Task<MCPConnectionStatus> InitializeSessionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var initRequest = new MCPInitRequest
            {
                ClientInfo = new
                {
                    Name = "DigitalMe",
                    Version = "1.0.0"
                },
                Capabilities = new[]
                {
                    "message_processing",
                    "tool_execution",
                    "personality_modeling"
                }
            };
            
            var response = await _httpClient.PostAsJsonAsync(
                $"{_options.Value.Endpoint}/initialize", 
                initRequest, 
                cancellationToken);
                
            if (response.IsSuccessStatusCode)
            {
                var initResponse = await response.Content.ReadFromJsonAsync<MCPInitResponse>(cancellationToken: cancellationToken);
                _sessionId = initResponse?.SessionId;
                
                _logger.LogInformation("MCP session initialized successfully: {SessionId}", _sessionId);
                return MCPConnectionStatus.Connected;
            }
            else
            {
                _logger.LogError("MCP initialization failed with status: {StatusCode}", response.StatusCode);
                return MCPConnectionStatus.Failed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP initialization exception");
            return MCPConnectionStatus.Failed;
        }
    }
}
```

## MCP Models
**Файл**: `src/DigitalMe.Integrations/MCP/Models/MCPModels.cs:1-50`

```csharp
namespace DigitalMe.Integrations.MCP.Models;

public class MCPRequest
{
    public string Type { get; set; } = default!;
    public string Content { get; set; } = default!;
    public object? PersonalityProfile { get; set; }
    public object[]? ConversationHistory { get; set; }
    public string? SessionId { get; set; }
}

public class MCPResponse
{
    public string Content { get; set; } = default!;
    public int TokensUsed { get; set; }
    public double Confidence { get; set; }
    public string? SessionId { get; set; }
    public MCPError? Error { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class MCPError
{
    public string Code { get; set; } = default!;
    public string Message { get; set; } = default!;
    public Dictionary<string, object>? Details { get; set; }
}

public enum MCPConnectionStatus
{
    Connected,
    Disconnected,
    Failed,
    Initializing
}

public class MCPTool
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public Dictionary<string, object> Schema { get; set; } = new();
}

public class MCPToolResult
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
}
```