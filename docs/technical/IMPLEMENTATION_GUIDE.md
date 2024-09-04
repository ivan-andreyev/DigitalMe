# Руководство по реализации персонального агента Ивана

## Конкретные технологии и библиотеки с версиями

### 1. NuGet пакеты для Core функциональности

```xml
<PackageReference Include="Microsoft.AspNetCore.App" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />

<!-- Semantic Kernel - Агентский фреймворк -->
<PackageReference Include="Microsoft.SemanticKernel" Version="1.28.0" />
<PackageReference Include="Microsoft.SemanticKernel.Plugins.Core" Version="1.28.0-alpha" />
<PackageReference Include="Microsoft.SemanticKernel.Plugins.Web" Version="1.28.0-alpha" />

<!-- Microsoft AI Extensions -->
<PackageReference Include="Microsoft.Extensions.AI" Version="8.11.0" />
<PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="8.11.0" />

<!-- MCP Protocol Support -->
<PackageReference Include="ModelContextProtocol" Version="1.0.0" />
```

### 2. LLM провайдеры

```xml
<!-- OpenAI -->
<PackageReference Include="OpenAI-DotNet" Version="8.7.2" />

<!-- Anthropic Claude (Unofficial) -->
<PackageReference Include="Anthropic.SDK" Version="2.0.0" />
<!-- Альтернатива: -->
<PackageReference Include="Claudia" Version="1.0.0" />
```

### 3. Интеграции

```xml
<!-- Telegram Bot -->
<PackageReference Include="Telegram.Bot" Version="22.6.0" />
<!-- Для advanced сценариев: -->
<PackageReference Include="WTelegramClient" Version="3.9.4" />

<!-- Google APIs -->
<PackageReference Include="Google.Apis.Calendar.v3" Version="1.69.0.3746" />
<PackageReference Include="Google.Apis.Drive.v3" Version="1.69.0.3745" />
<PackageReference Include="Google.Apis.Docs.v1" Version="1.69.0.3740" />
<PackageReference Include="Google.Apis.Sheets.v4" Version="1.69.0.3745" />
<PackageReference Include="Google.Apis.Auth" Version="1.69.0" />

<!-- GitHub -->
<PackageReference Include="Octokit" Version="13.0.1" />
<PackageReference Include="Octokit.GraphQL" Version="2.1.1" />
```

### 4. Дополнительные утилиты

```xml
<!-- Конфигурация и логирование -->
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />

<!-- Кэширование и производительность -->
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />

<!-- Безопасность -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.DataProtection" Version="8.0.0" />

<!-- HTTP клиенты -->
<PackageReference Include="Polly.Extensions.Http" Version="3.0.0" />
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.0.0" />
```

## 2. Конфигурация проекта

### Program.cs - Dependency Injection Setup

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using PersonalAgent.Data;
using PersonalAgent.Services;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<PersonalAgentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Semantic Kernel
builder.Services.AddSingleton<Kernel>(serviceProvider =>
{
    var kernelBuilder = Kernel.CreateBuilder()
        .AddOpenAIChatCompletion(
            "gpt-4o", 
            builder.Configuration["OpenAI:ApiKey"]!)
        .AddAnthropicChatCompletion(
            "claude-3-5-sonnet-20241022",
            builder.Configuration["Anthropic:ApiKey"]!);

    // Add plugins
    kernelBuilder.Plugins.AddFromType<CalendarPlugin>();
    kernelBuilder.Plugins.AddFromType<ContactsPlugin>();
    kernelBuilder.Plugins.AddFromType<GitHubPlugin>();
    
    return kernelBuilder.Build();
});

// Telegram Bot
builder.Services.AddSingleton<ITelegramBotClient>(provider =>
    new TelegramBotClient(builder.Configuration["Telegram:BotToken"]!));

// Google APIs
builder.Services.AddGoogleCalendar(options =>
{
    options.ClientId = builder.Configuration["Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Google:ClientSecret"]!;
});

// GitHub API
builder.Services.AddScoped<GitHubClient>(provider =>
    new GitHubClient(new ProductHeaderValue("PersonalAgent"))
    {
        Credentials = new Credentials(builder.Configuration["GitHub:Token"]!)
    });

// Application Services
builder.Services.AddScoped<IPersonalAgentService, PersonalAgentService>();
builder.Services.AddScoped<ITelegramBotService, TelegramBotService>();
builder.Services.AddScoped<ICalendarService, GoogleCalendarService>();
builder.Services.AddScoped<IContactService, ContactService>();

// MCP Server
builder.Services.AddMCPServer<PersonalAgentMCPServer>();

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// Start Telegram Bot
var telegramService = app.Services.GetRequiredService<ITelegramBotService>();
await telegramService.StartAsync();

app.Run();
```

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db.your-supabase-project.supabase.co;Database=postgres;Username=postgres;Password=your-password;Port=5432;SSL Mode=Require;Trust Server Certificate=true"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key"
  },
  "Anthropic": {
    "ApiKey": "your-anthropic-api-key"
  },
  "Telegram": {
    "BotToken": "your-telegram-bot-token"
  },
  "Google": {
    "ClientId": "your-google-client-id",
    "ClientSecret": "your-google-client-secret"
  },
  "GitHub": {
    "Token": "your-github-token"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## 3. Примеры реализации ключевых компонентов

### PersonalAgentService.cs

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PersonalAgent.Models;

namespace PersonalAgent.Services;

public interface IPersonalAgentService
{
    Task<string> ProcessRequestAsync(string userRequest, AgentContext context);
    Task<AgentSession> StartSessionAsync(long userId, string sessionType);
    Task EndSessionAsync(Guid sessionId);
}

public class PersonalAgentService : IPersonalAgentService
{
    private readonly Kernel _kernel;
    private readonly PersonalAgentDbContext _dbContext;
    private readonly ILogger<PersonalAgentService> _logger;

    public PersonalAgentService(
        Kernel kernel,
        PersonalAgentDbContext dbContext,
        ILogger<PersonalAgentService> logger)
    {
        _kernel = kernel;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<string> ProcessRequestAsync(string userRequest, AgentContext context)
    {
        try
        {
            var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
            
            var systemPrompt = $"""
                Ты персональный агент пользователя {context.User.Username}.
                
                Твои основные возможности:
                - Управление календарем (создание, изменение, просмотр событий)
                - Работа с контактами (поиск, добавление, обновление)
                - Интеграция с GitHub (создание issues, просмотр репозиториев)
                - Отправка сообщений через Telegram
                
                Текущее время: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
                
                Анализируй запросы пользователя и выполняй соответствующие действия,
                используя доступные инструменты.
                """;

            var chatHistory = new ChatHistory(systemPrompt);
            chatHistory.AddUserMessage(userRequest);

            var result = await chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                kernel: _kernel);

            // Сохраняем interaction в базе
            await SaveInteractionAsync(context.SessionId, userRequest, result.Content);

            return result.Content ?? "Извини, произошла ошибка при обработке запроса.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request: {Request}", userRequest);
            return "Произошла ошибка при обработке вашего запроса. Попробуйте позже.";
        }
    }

    public async Task<AgentSession> StartSessionAsync(long userId, string sessionType)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.TelegramUserId == userId);
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                TelegramUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(user);
        }

        var session = new AgentSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionType = sessionType,
            Status = "active",
            StartedAt = DateTime.UtcNow,
            ContextData = new Dictionary<string, object>()
        };

        _dbContext.AgentSessions.Add(session);
        await _dbContext.SaveChangesAsync();

        return session;
    }

    public async Task EndSessionAsync(Guid sessionId)
    {
        var session = await _dbContext.AgentSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.Status = "ended";
            session.EndedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }

    private async Task SaveInteractionAsync(Guid sessionId, string request, string response)
    {
        // Implement interaction logging
        _logger.LogInformation("Session {SessionId}: Request: {Request}, Response: {Response}", 
            sessionId, request, response);
    }
}
```

### CalendarPlugin.cs - Semantic Kernel Plugin

```csharp
using Microsoft.SemanticKernel;
using System.ComponentModel;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;

namespace PersonalAgent.Plugins;

public class CalendarPlugin
{
    private readonly ICalendarService _calendarService;

    public CalendarPlugin(ICalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    [KernelFunction]
    [Description("Получить события календаря за указанный период")]
    public async Task<string> GetCalendarEventsAsync(
        [Description("Дата начала (YYYY-MM-DD)")] string startDate,
        [Description("Дата окончания (YYYY-MM-DD)")] string endDate,
        [Description("ID календаря (по умолчанию основной)")] string? calendarId = null)
    {
        try
        {
            var start = DateTime.Parse(startDate);
            var end = DateTime.Parse(endDate);
            
            var events = await _calendarService.GetEventsAsync(start, end, calendarId);
            
            if (!events.Any())
            {
                return $"На период с {startDate} по {endDate} событий не найдено.";
            }

            var result = $"События с {startDate} по {endDate}:\n";
            foreach (var evt in events.OrderBy(e => e.StartTime))
            {
                result += $"• {evt.StartTime:dd.MM HH:mm} - {evt.Title}\n";
                if (!string.IsNullOrEmpty(evt.Location))
                    result += $"  📍 {evt.Location}\n";
            }

            return result;
        }
        catch (Exception ex)
        {
            return $"Ошибка получения событий календаря: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Создать новое событие в календаре")]
    public async Task<string> CreateCalendarEventAsync(
        [Description("Название события")] string title,
        [Description("Дата и время начала (YYYY-MM-DD HH:mm)")] string startDateTime,
        [Description("Длительность в минутах")] int durationMinutes,
        [Description("Описание события")] string? description = null,
        [Description("Место проведения")] string? location = null)
    {
        try
        {
            var start = DateTime.Parse(startDateTime);
            var end = start.AddMinutes(durationMinutes);

            var calendarEvent = new CalendarEventModel
            {
                Title = title,
                Description = description,
                StartTime = start,
                EndTime = end,
                Location = location
            };

            var createdEvent = await _calendarService.CreateEventAsync(calendarEvent);
            
            return $"✅ Событие '{title}' создано на {start:dd.MM.yyyy HH:mm}";
        }
        catch (Exception ex)
        {
            return $"Ошибка создания события: {ex.Message}";
        }
    }
}
```

### TelegramBotService.cs

```csharp
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PersonalAgent.Services;

public interface ITelegramBotService
{
    Task StartAsync();
    Task StopAsync();
}

public class TelegramBotService : ITelegramBotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IPersonalAgentService _agentService;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public TelegramBotService(
        ITelegramBotClient botClient,
        IPersonalAgentService agentService,
        ILogger<TelegramBotService> logger)
    {
        _botClient = botClient;
        _agentService = agentService;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: _cancellationTokenSource.Token);

        var me = await _botClient.GetMeAsync();
        _logger.LogInformation("Telegram bot {BotName} started", me.Username);
    }

    public Task StopAsync()
    {
        _cancellationTokenSource.Cancel();
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message || message.Text is not { } messageText)
            return;

        try
        {
            var session = await _agentService.StartSessionAsync(message.From!.Id, "telegram");
            
            var context = new AgentContext
            {
                SessionId = session.Id,
                User = new UserModel { Username = message.From.Username ?? message.From.FirstName },
                ChatId = message.Chat.Id
            };

            var response = await _agentService.ProcessRequestAsync(messageText, context);

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: response,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Telegram update");
            
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Извините, произошла ошибка. Попробуйте позже.",
                cancellationToken: cancellationToken);
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Telegram bot polling error");
        return Task.CompletedTask;
    }
}
```

### MCP Server Implementation

```csharp
using ModelContextProtocol;
using System.ComponentModel;

namespace PersonalAgent.MCP;

public class PersonalAgentMCPServer : IMCPServer
{
    private readonly IPersonalAgentService _agentService;
    private readonly ICalendarService _calendarService;
    private readonly ITelegramBotClient _telegramBot;

    public PersonalAgentMCPServer(
        IPersonalAgentService agentService,
        ICalendarService calendarService,
        ITelegramBotClient telegramBot)
    {
        _agentService = agentService;
        _calendarService = calendarService;
        _telegramBot = telegramBot;
    }

    [MCPTool("get_calendar_events")]
    [Description("Получить события календаря за указанный период")]
    public async Task<CalendarEventModel[]> GetCalendarEventsAsync(
        [Description("Дата начала (YYYY-MM-DD)")] DateTime startDate,
        [Description("Дата окончания (YYYY-MM-DD)")] DateTime endDate,
        [Description("ID календаря")] string? calendarId = null)
    {
        return await _calendarService.GetEventsAsync(startDate, endDate, calendarId);
    }

    [MCPTool("create_calendar_event")]
    [Description("Создать событие в календаре")]
    public async Task<CalendarEventModel> CreateCalendarEventAsync(
        [Description("Название события")] string title,
        [Description("Дата и время начала")] DateTime startTime,
        [Description("Дата и время окончания")] DateTime endTime,
        [Description("Описание")] string? description = null,
        [Description("Место")] string? location = null)
    {
        var eventModel = new CalendarEventModel
        {
            Title = title,
            StartTime = startTime,
            EndTime = endTime,
            Description = description,
            Location = location
        };

        return await _calendarService.CreateEventAsync(eventModel);
    }

    [MCPTool("send_telegram_message")]
    [Description("Отправить сообщение через Telegram")]
    public async Task<bool> SendTelegramMessageAsync(
        [Description("ID чата или пользователя")] long chatId,
        [Description("Текст сообщения")] string message)
    {
        try
        {
            await _telegramBot.SendTextMessageAsync(chatId, message);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [MCPResource("user_contacts")]
    [Description("Контакты пользователя")]
    public async Task<ContactModel[]> GetUserContactsAsync()
    {
        // Implementation to get user contacts
        return Array.Empty<ContactModel>();
    }
}
```

## 4. Dockerfile для развертывания

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files
COPY *.csproj ./
RUN dotnet restore

# Copy source code
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "PersonalAgent.dll"]
```

## 5. Azure Container Apps - bicep template

```bicep
param containerAppName string = 'personal-agent'
param location string = resourceGroup().location
param environmentName string = 'personal-agent-env'

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2023-11-01-preview' = {
  name: environmentName
  location: location
  properties: {
    zoneRedundant: false
  }
}

resource containerApp 'Microsoft.App/containerApps@2023-11-01-preview' = {
  name: containerAppName
  location: location
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        corsPolicy: {
          allowedOrigins: ['*']
        }
      }
      secrets: [
        {
          name: 'openai-api-key'
          value: 'your-openai-key'
        }
        {
          name: 'anthropic-api-key'
          value: 'your-anthropic-key'
        }
        {
          name: 'telegram-bot-token'
          value: 'your-telegram-token'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'personal-agent'
          image: 'your-registry.azurecr.io/personal-agent:latest'
          resources: {
            cpu: '0.25'
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'OpenAI__ApiKey'
              secretRef: 'openai-api-key'
            }
            {
              name: 'Anthropic__ApiKey'
              secretRef: 'anthropic-api-key'
            }
            {
              name: 'Telegram__BotToken'
              secretRef: 'telegram-bot-token'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 5
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '10'
              }
            }
          }
        ]
      }
    }
  }
}
```

## 6. Быстрый старт

### Шаги для запуска MVP:

1. **Создать проект:**
```bash
dotnet new webapi -n PersonalAgent
cd PersonalAgent
```

2. **Добавить пакеты:**
```bash
dotnet add package Microsoft.SemanticKernel --version 1.28.0
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0
dotnet add package Telegram.Bot --version 22.6.0
dotnet add package Google.Apis.Calendar.v3 --version 1.69.0.3746
dotnet add package Anthropic.SDK --version 2.0.0
```

3. **Настроить Supabase PostgreSQL:**
   - Создать проект в Supabase
   - Получить connection string
   - Добавить в appsettings.json

4. **Создать Telegram бота:**
   - Написать @BotFather в Telegram
   - Создать бота командой /newbot
   - Получить токен

5. **Настроить Google APIs:**
   - Создать проект в Google Cloud Console
   - Включить Calendar API
   - Создать OAuth2 credentials

6. **Запустить:**
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet run
```

Этот implementation guide предоставляет все необходимые детали для быстрого старта разработки персонального агента Ивана с использованием современного .NET стека и лучших практик 2024-2025 года.