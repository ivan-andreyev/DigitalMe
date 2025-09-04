# MCP интеграция с Claude Code

## Обзор Model Context Protocol (MCP)

MCP - протокол для интеграции с агентскими системами, позволяющий Claude Code выступать как "мозг" нашего агента.

### Архитектура интеграции
```
┌─────────────────┐    MCP Protocol     ┌─────────────────┐
│   Claude Code   │◄───────────────────►│  Digital Clone  │
│   (LLM Brain)   │   JSON-RPC over     │    Backend      │
│                 │   HTTP/WebSocket    │                 │
├─────────────────┤                     ├─────────────────┤
│ • Context       │                     │ • Personality   │
│ • Tools         │                     │ • Memory        │
│ • Reasoning     │                     │ • Integrations  │
└─────────────────┘                     └─────────────────┘
```

## .NET MCP Client Implementation

### Core Service

**Файл**: `src/DigitalMe.Integrations/MCP/IMCPService.cs`
```csharp
public interface IMCPService
{
    Task<string> SendMessageAsync(string message, PersonalityContext context);
    Task<MCPResponse> CallToolAsync(string toolName, Dictionary<string, object> parameters);
    Task InitializeSessionAsync();
    Task<bool> IsConnectedAsync();
}
```

**Файл**: `src/DigitalMe.Integrations/MCP/MCPService.cs`
```csharp
public class MCPService : IMCPService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MCPService> _logger;
    
    public MCPService(
        HttpClient httpClient,
        IConfiguration configuration, 
        ILogger<MCPService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task<string> SendMessageAsync(string message, PersonalityContext context)
    {
        var request = new MCPRequest
        {
            Method = "completion",
            Params = new
            {
                messages = new[]
                {
                    new { role = "system", content = context.ToSystemPrompt() },
                    new { role = "user", content = message }
                },
                tools = GetAvailableTools()
            }
        };
        
        var response = await SendMCPRequestAsync(request);
        return response.Result?.Content ?? string.Empty;
    }
}
```

### MCP Protocol Models

**Файл**: `src/DigitalMe.Integrations/MCP/Models/MCPRequest.cs`
```csharp
public class MCPRequest
{
    public string JsonRpc { get; set; } = "2.0";
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Method { get; set; }
    public object Params { get; set; }
}

public class MCPResponse
{
    public string JsonRpc { get; set; }
    public string Id { get; set; }
    public MCPResult Result { get; set; }
    public MCPError Error { get; set; }
}

public class MCPResult
{
    public string Content { get; set; }
    public IEnumerable<MCPTool> ToolCalls { get; set; }
}
```

### Personality Context Integration

**Файл**: `src/DigitalMe.Core/Models/PersonalityContext.cs`
```csharp
public class PersonalityContext
{
    public PersonalityProfile Profile { get; set; }
    public IEnumerable<Message> RecentMessages { get; set; }
    public Dictionary<string, object> CurrentState { get; set; }
    
    public string ToSystemPrompt()
    {
        var prompt = $@"
Ты - цифровая копия Ивана, 34-летнего программиста и руководителя отдела R&D.

ЛИЧНОСТЬ:
{Profile.Description}

ПРИНЦИПЫ:
- Всем похуй (философия независимости)
- Сила в правде (честность превыше всего)
- Живи и дай жить другим

СТИЛЬ ОБЩЕНИЯ:
- Прямолинейный, без лишних слов
- Технически компетентный
- Иногда резкий, но справедливый

КОНТЕКСТ ПОСЛЕДНИХ СООБЩЕНИЙ:
{string.Join('\n', RecentMessages.TakeLast(5).Select(m => $"{m.Role}: {m.Content}"))}

Отвечай как Иван, используя его стиль и принципы.
";
        return prompt;
    }
}
```

## Available Tools для Claude Code

### Tool Registration

**Файл**: `src/DigitalMe.Integrations/MCP/Tools/ToolRegistry.cs`
```csharp
public class ToolRegistry
{
    public static IEnumerable<MCPTool> GetAvailableTools()
    {
        return new[]
        {
            new MCPTool
            {
                Name = "send_telegram_message",
                Description = "Отправить сообщение в Telegram",
                Parameters = new
                {
                    chat_id = new { type = "number", description = "ID чата" },
                    message = new { type = "string", description = "Текст сообщения" }
                }
            },
            new MCPTool
            {
                Name = "create_calendar_event", 
                Description = "Создать событие в Google Calendar",
                Parameters = new
                {
                    title = new { type = "string", description = "Название события" },
                    start_time = new { type = "string", description = "Время начала (ISO 8601)" },
                    end_time = new { type = "string", description = "Время окончания (ISO 8601)" }
                }
            },
            new MCPTool
            {
                Name = "search_github_repositories",
                Description = "Поиск репозиториев на GitHub",
                Parameters = new
                {
                    query = new { type = "string", description = "Поисковый запрос" }
                }
            }
        };
    }
}
```

### Tool Execution

**Файл**: `src/DigitalMe.Integrations/MCP/Tools/ToolExecutor.cs`
```csharp
public class ToolExecutor
{
    private readonly ITelegramService _telegramService;
    private readonly ICalendarService _calendarService;
    private readonly IGitHubService _githubService;
    
    public async Task<object> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters)
    {
        return toolName switch
        {
            "send_telegram_message" => await _telegramService.SendMessageAsync(
                (long)parameters["chat_id"], 
                (string)parameters["message"]),
                
            "create_calendar_event" => await _calendarService.CreateEventAsync(
                (string)parameters["title"],
                DateTime.Parse((string)parameters["start_time"]),
                DateTime.Parse((string)parameters["end_time"])),
                
            "search_github_repositories" => await _githubService.SearchRepositoriesAsync(
                (string)parameters["query"]),
                
            _ => throw new NotSupportedException($"Tool {toolName} not supported")
        };
    }
}
```

## Configuration

**Файл**: `src/DigitalMe.API/appsettings.json:MCP`
```json
{
  "MCP": {
    "ClaudeCodeEndpoint": "https://claude-code-api.anthropic.com/mcp",
    "ApiKey": "{{CLAUDE_CODE_API_KEY}}",
    "TimeoutSeconds": 30,
    "MaxRetries": 3
  }
}
```

## Dependency Injection Setup

**Файл**: `src/DigitalMe.API/Program.cs:ConfigureServices`
```csharp
// MCP Integration
services.AddHttpClient<MCPService>();
services.AddScoped<IMCPService, MCPService>();
services.AddScoped<ToolExecutor>();
services.AddSingleton<ToolRegistry>();

// MCP Configuration
services.Configure<MCPOptions>(
    configuration.GetSection("MCP"));
```

## Error Handling & Retry Policy

**Файл**: `src/DigitalMe.Integrations/MCP/MCPService.cs:SendMCPRequestAsync`
```csharp
private async Task<MCPResponse> SendMCPRequestAsync(MCPRequest request)
{
    var policy = Policy
        .Handle<HttpRequestException>()
        .Or<TaskCanceledException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                _logger.LogWarning("MCP request failed, retry {RetryCount} in {Delay}ms", 
                    retryCount, timespan.TotalMilliseconds);
            });

    return await policy.ExecuteAsync(async () =>
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("/mcp", content);
        response.EnsureSuccessStatusCode();
        
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MCPResponse>(responseJson);
    });
}
```

---

### 🔙 Navigation
- **← Parent Plan**: [Main Plan](../../00-MAIN_PLAN.md)
- **← Technical Coordinator**: [../02-technical.md](../02-technical.md)
- **← Architecture Overview**: [../00-ARCHITECTURE_OVERVIEW.md](../00-ARCHITECTURE_OVERVIEW.md)

---

**Следующий план**: [Фронтенд архитектура](02-03-frontend-specs.md)