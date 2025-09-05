# Фронтенд архитектура - All .NET Stack

## Обзор фронтенд решений

Все фронтенды используют единую .NET экосистему для упрощения разработки и поддержки.

### 1. Blazor Web Application

**Проект**: `src/DigitalMe.Web/`
**Технологии**: Blazor Server + Blazor WebAssembly hybrid

#### Ключевые компоненты
```csharp
// src/DigitalMe.Web/Components/Chat/ChatComponent.razor
@using Microsoft.AspNetCore.SignalR.Client
@implements IAsyncDisposable

<div class="chat-container">
    <div class="messages" @ref="messagesContainer">
        @foreach (var message in messages)
        {
            <MessageComponent Message="message" />
        }
    </div>
    <MessageInputComponent OnMessageSent="HandleMessageSent" />
</div>

@code {
    private HubConnection? hubConnection;
    private List<ChatMessage> messages = new();
    
    protected override async Task OnInitializedAsync()
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl("/chathub")
            .Build();
            
        hubConnection.On<ChatMessage>("ReceiveMessage", HandleNewMessage);
        await hubConnection.StartAsync();
    }
}
```

#### Структура проекта
```
src/DigitalMe.Web/
├── Components/
│   ├── Chat/
│   │   ├── ChatComponent.razor
│   │   ├── MessageComponent.razor
│   │   └── MessageInputComponent.razor
│   ├── Dashboard/
│   │   ├── DashboardComponent.razor
│   │   └── StatsComponent.razor
│   └── Shared/
│       ├── MainLayout.razor
│       └── NavMenu.razor
├── Pages/
│   ├── Home.razor
│   ├── Chat.razor
│   └── Settings.razor
├── Services/
│   ├── IChatService.cs
│   └── ChatService.cs
└── wwwroot/
    ├── css/
    ├── js/
    └── favicon.ico
```

### 2. MAUI Mobile & Desktop Apps

**Проект**: `src/DigitalMe.Mobile/`
**Технологии**: .NET MAUI + Blazor Hybrid

#### MauiProgram.cs
```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Blazor Hybrid
        builder.Services.AddMauiBlazorWebView();
        
        // Shared Services
        builder.Services.AddScoped<IChatService, ChatService>();
        builder.Services.AddScoped<IPersonalityService, PersonalityService>();
        
        // Platform-specific services
#if ANDROID
        builder.Services.AddSingleton<IPlatformService, AndroidPlatformService>();
#elif IOS
        builder.Services.AddSingleton<IPlatformService, IOSPlatformService>();
#elif WINDOWS
        builder.Services.AddSingleton<IPlatformService, WindowsPlatformService>();
#endif

        return builder.Build();
    }
}
```

#### Общие Blazor компоненты
**Путь**: `src/DigitalMe.Mobile/Components/` (ссылается на `DigitalMe.Web/Components`)

```csharp
// Shared component usage
@using DigitalMe.Web.Components.Chat

<ContentPage Title="Chat">
    <BlazorWebView HostPage="wwwroot/index.html">
        <RootComponents>
            <RootComponent Selector="#app" ComponentType="typeof(ChatComponent)" />
        </RootComponents>
    </BlazorWebView>
</ContentPage>
```

### 3. Telegram Bot Interface

**Проект**: `src/DigitalMe.Integrations/Telegram/`
**Технологии**: Telegram.Bot 22.6.0

#### TelegramBotService.cs
```csharp
public class TelegramBotService : ITelegramBotService
{
    private readonly TelegramBotClient _botClient;
    private readonly IMCPService _mcpService;
    private readonly IPersonalityService _personalityService;
    
    public async Task HandleUpdateAsync(Update update)
    {
        if (update.Message?.Text is { } messageText)
        {
            var chatId = update.Message.Chat.Id;
            
            // Get Ivan's personality context
            var personality = await _personalityService.GetPersonalityAsync("Ivan");
            var context = new PersonalityContext 
            { 
                Profile = personality,
                Platform = "Telegram",
                ChatId = chatId.ToString()
            };
            
            // Send to Claude Code via MCP
            var response = await _mcpService.SendMessageAsync(messageText, context);
            
            // Send response back to Telegram
            await _botClient.SendTextMessageAsync(chatId, response);
        }
    }
    
    public async Task SendAsIvanAsync(long chatId, string message)
    {
        // Send message as if Ivan typed it
        await _botClient.SendTextMessageAsync(chatId, message);
        
        // Log as Ivan's message for learning
        await LogIvanMessageAsync(chatId, message);
    }
}
```

#### Bot Commands
**Файл**: `src/DigitalMe.Integrations/Telegram/Commands/BotCommands.cs`
```csharp
public static class BotCommands
{
    public const string START = "/start";
    public const string HELP = "/help";
    public const string SETTINGS = "/settings";
    public const string PERSONALITY = "/personality";
    
    public static async Task<string> HandleCommandAsync(
        string command, 
        long chatId,
        IPersonalityService personalityService)
    {
        return command switch
        {
            START => "Привет! Я - цифровая копия Ивана. Чем могу помочь?",
            HELP => GetHelpMessage(),
            SETTINGS => await GetSettingsAsync(chatId),
            PERSONALITY => await GetPersonalityInfoAsync(personalityService),
            _ => "Команда не распознана. Используй /help для списка команд."
        };
    }
    
    private static string GetHelpMessage()
    {
        return @"
🤖 Доступные команды:
/start - Начать диалог
/help - Показать эту справку  
/settings - Настройки
/personality - Информация о личности

Просто пиши сообщения - я отвечу как Иван!";
    }
}
```

### 4. Voice Interface (будущая разработка)

**Планируемые технологии**:
- **Microsoft.CognitiveServices.Speech** - Speech-to-Text и Text-to-Speech
- **NAudio** - аудио обработка для .NET
- **WebRTC.NET** - real-time voice в браузере

## Shared UI Components Library

**Проект**: `src/DigitalMe.UI.Shared/`

Общая библиотека UI компонентов для повторного использования:

```csharp
// src/DigitalMe.UI.Shared/Components/PersonalityIndicator.razor
@using DigitalMe.Core.Models

<div class="personality-indicator @CssClass">
    <div class="avatar">
        <img src="/images/ivan-avatar.png" alt="Ivan" />
    </div>
    <div class="status">
        <span class="name">Ivan</span>
        <span class="state @StateClass">@CurrentState</span>
    </div>
    <div class="mood-indicator" style="background-color: @MoodColor">
        @MoodEmoji
    </div>
</div>

@code {
    [Parameter] public PersonalityState State { get; set; }
    [Parameter] public string CssClass { get; set; } = "";
    
    private string StateClass => State.IsActive ? "active" : "inactive";
    private string CurrentState => State.IsActive ? "Онлайн" : "Неактивен";
    private string MoodColor => State.Mood switch
    {
        PersonalityMood.Focused => "#4CAF50",
        PersonalityMood.Tired => "#FF9800", 
        PersonalityMood.Irritated => "#F44336",
        _ => "#2196F3"
    };
    private string MoodEmoji => State.Mood switch
    {
        PersonalityMood.Focused => "🎯",
        PersonalityMood.Tired => "😴",
        PersonalityMood.Irritated => "😤",
        _ => "🤔"
    };
}
```

## SignalR Hubs для Real-time

**Файл**: `src/DigitalMe.API/Hubs/ChatHub.cs`
```csharp
public class ChatHub : Hub
{
    private readonly IMCPService _mcpService;
    private readonly IPersonalityService _personalityService;
    
    public async Task SendMessage(string message)
    {
        var personality = await _personalityService.GetPersonalityAsync("Ivan");
        var context = new PersonalityContext 
        { 
            Profile = personality,
            Platform = "Web",
            ConnectionId = Context.ConnectionId
        };
        
        var response = await _mcpService.SendMessageAsync(message, context);
        
        await Clients.Caller.SendAsync("ReceiveMessage", new ChatMessage
        {
            Content = response,
            Role = "assistant",
            Timestamp = DateTime.UtcNow
        });
    }
    
    public async Task JoinRoom(string roomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
    }
}
```

## Стилизация и темизация

**Файл**: `src/DigitalMe.Web/wwwroot/css/ivan-theme.css`
```css
:root {
    /* Ivan's personality colors */
    --primary-color: #1a237e;      /* Deep blue - focus */
    --secondary-color: #ff6f00;    /* Orange - energy */
    --accent-color: #4caf50;       /* Green - success */
    --warning-color: #ff9800;      /* Amber - caution */
    --error-color: #f44336;        /* Red - directness */
    
    /* Typography reflecting Ivan's style */
    --font-family: 'Roboto Mono', monospace;  /* Technical, precise */
    --font-size-base: 14px;
    --line-height: 1.4;
}

.ivan-chat {
    font-family: var(--font-family);
    background: linear-gradient(135deg, #1a237e 0%, #303f9f 100%);
    color: #ffffff;
}

.message.assistant {
    border-left: 3px solid var(--accent-color);
    background: rgba(76, 175, 80, 0.1);
}

.message.user {
    border-left: 3px solid var(--secondary-color);
    background: rgba(255, 111, 0, 0.1);
}
```

---

### 🔙 Navigation
- **← Parent Plan**: [Main Plan](../../00-MAIN_PLAN.md)
- **← Technical Coordinator**: [../02-technical.md](../02-technical.md)
- **← Architecture Overview**: [../00-ARCHITECTURE_OVERVIEW.md](../00-ARCHITECTURE_OVERVIEW.md)

---

**Следующий план**: [План разработки](../03-implementation/03-01-development-phases.md)