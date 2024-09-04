# Frontend Architecture Specification - Digital Ivan

> **Architecture Philosophy**: Shared components, unified state, platform-specific optimizations  
> **Primary Stack**: Blazor Server + MAUI Hybrid + Telegram Bot integration  
> **Code Reuse Target**: 90% shared UI components between Web and MAUI

---

## 🏗️ ARCHITECTURE OVERVIEW

### High-Level Architecture Diagram
```
┌─────────────────── DIGITAL IVAN FRONTEND ECOSYSTEM ───────────────────┐
│                                                                        │
│  ┌─ Web (Blazor Server) ──┐  ┌─ Mobile (MAUI Hybrid) ─┐  ┌─ Telegram ─┐ │
│  │ • SignalR Real-time    │  │ • Blazor WebView       │  │ • Bot API   │ │
│  │ • Server-side render   │  │ • Native platform      │  │ • Rich text │ │
│  │ • Progressive Web App  │  │ • Cross-platform       │  │ • Commands  │ │
│  └─────────┬──────────────┘  └─────────┬──────────────┘  └──────┬──────┘ │
│            │                           │                        │        │
│            └─────────────┐             │             ┌──────────┘        │
│                          │             │             │                   │
│  ┌─ Shared Component Library (DigitalIvan.UI.Shared) ─┐                  │
│  │                                                     │                  │
│  │ • Chat Components      • Dashboard Widgets         │                  │
│  │ • Message Bubbles      • Personality Visualizers   │                  │
│  │ • Input Controls       • Status Indicators         │                  │
│  │ • Code Highlighting    • Responsive Layouts        │                  │
│  └─────────────────────────┬───────────────────────────┘                  │
│                            │                                              │
│  ┌─ State Management (Fluxor/Custom) ─────────────────┐                  │
│  │ • Chat State          • Personality State         │                  │
│  │ • User Preferences    • Connection State          │                  │
│  │ • Notification State  • Cross-platform Sync       │                  │
│  └─────────────────────────┬───────────────────────────┘                  │
│                            │                                              │
│  ┌─ API Integration Layer ─────────────────────────────┐                  │
│  │ • HTTP Client (REST)   • SignalR Client            │                  │
│  │ • Personality Service • Chat Service               │                  │
│  │ • Authentication      • Real-time Updates          │                  │
│  └─────────────────────────────────────────────────────┘                  │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 📁 PROJECT STRUCTURE

### Solution Architecture
```
DigitalIvan.Frontend.sln
├── src/
│   ├── DigitalIvan.UI.Shared/              # Shared Component Library
│   │   ├── Components/
│   │   │   ├── Chat/
│   │   │   │   ├── ChatContainer.razor
│   │   │   │   ├── MessageBubble.razor
│   │   │   │   ├── MessageInput.razor
│   │   │   │   ├── TypingIndicator.razor
│   │   │   │   └── CodeBlock.razor
│   │   │   ├── Dashboard/
│   │   │   │   ├── PersonalityDashboard.razor
│   │   │   │   ├── TraitVisualization.razor
│   │   │   │   ├── MoodIndicator.razor
│   │   │   │   ├── ActivityTimeline.razor
│   │   │   │   └── StatusWidget.razor
│   │   │   ├── Layout/
│   │   │   │   ├── AppShell.razor
│   │   │   │   ├── NavigationMenu.razor
│   │   │   │   └── StatusBar.razor
│   │   │   └── Shared/
│   │   │       ├── LoadingSpinner.razor
│   │   │       ├── ErrorBoundary.razor
│   │   │       ├── Avatar.razor
│   │   │       └── Tooltip.razor
│   │   ├── Models/
│   │   │   ├── ChatMessage.cs
│   │   │   ├── PersonalityState.cs
│   │   │   ├── UserPreferences.cs
│   │   │   └── ConversationContext.cs
│   │   ├── Services/
│   │   │   ├── Interfaces/
│   │   │   │   ├── IChatService.cs
│   │   │   │   ├── IPersonalityService.cs
│   │   │   │   ├── IStateManager.cs
│   │   │   │   └── INotificationService.cs
│   │   │   └── Implementation/
│   │   │       ├── ChatService.cs
│   │   │       ├── PersonalityService.cs
│   │   │       ├── StateManager.cs
│   │   │       └── NotificationService.cs
│   │   ├── State/
│   │   │   ├── AppState.cs
│   │   │   ├── ChatState.cs
│   │   │   ├── PersonalityState.cs
│   │   │   └── UserState.cs
│   │   └── wwwroot/
│   │       ├── css/
│   │       │   ├── components.css
│   │       │   ├── ivan-theme.css
│   │       │   └── responsive.css
│   │       ├── js/
│   │       │   ├── interop.js
│   │       │   ├── signalr-client.js
│   │       │   └── platform-bridge.js
│   │       └── fonts/
│   │           ├── roboto-mono/
│   │           └── inter/
│   │
│   ├── DigitalIvan.Web/                    # Blazor Server Web App
│   │   ├── Program.cs
│   │   ├── Startup.cs
│   │   ├── Pages/
│   │   │   ├── Index.razor
│   │   │   ├── Chat.razor
│   │   │   ├── Dashboard.razor
│   │   │   └── Settings.razor
│   │   ├── Hubs/
│   │   │   └── ChatHub.cs
│   │   ├── Controllers/
│   │   │   └── ApiController.cs
│   │   ├── Services/
│   │   │   ├── WebChatService.cs
│   │   │   └── WebPersonalityService.cs
│   │   └── wwwroot/
│   │       ├── manifest.json              # PWA manifest
│   │       ├── service-worker.js          # Offline support
│   │       └── favicon.ico
│   │
│   ├── DigitalIvan.MAUI/                  # MAUI Cross-platform
│   │   ├── Platforms/
│   │   │   ├── Android/
│   │   │   ├── iOS/
│   │   │   ├── Windows/
│   │   │   └── MacCatalyst/
│   │   ├── MauiProgram.cs
│   │   ├── App.xaml
│   │   ├── AppShell.xaml
│   │   ├── MainPage.xaml
│   │   ├── Services/
│   │   │   ├── MauiChatService.cs
│   │   │   ├── PlatformService.cs
│   │   │   └── NotificationService.cs
│   │   ├── Views/
│   │   │   ├── ChatPage.xaml
│   │   │   ├── DashboardPage.xaml
│   │   │   └── SettingsPage.xaml
│   │   └── wwwroot/
│   │       └── index.html                 # Blazor Hybrid host page
│   │
│   └── DigitalIvan.Telegram/              # Telegram Bot Integration
│       ├── Program.cs
│       ├── Services/
│       │   ├── TelegramBotService.cs
│       │   ├── CommandHandler.cs
│       │   └── MessageFormatter.cs
│       ├── Commands/
│       │   ├── StartCommand.cs
│       │   ├── HelpCommand.cs
│       │   ├── StatusCommand.cs
│       │   └── SettingsCommand.cs
│       └── Models/
│           ├── TelegramUser.cs
│           └── BotConfiguration.cs
│
├── tests/
│   ├── DigitalIvan.UI.Shared.Tests/
│   ├── DigitalIvan.Web.Tests/
│   └── DigitalIvan.MAUI.Tests/
│
└── docs/
    ├── architecture/
    ├── deployment/
    └── api-integration/
```

---

## 🔧 STATE MANAGEMENT ARCHITECTURE

### State Management Strategy: Fluxor + Custom Services
```csharp
// Global State Container
public class AppState
{
    public ChatState Chat { get; init; }
    public PersonalityState Personality { get; init; }
    public UserState User { get; init; }
    public ConnectionState Connection { get; init; }
    public NotificationState Notifications { get; init; }
}

// Chat State with Actions and Effects
public class ChatState
{
    public ImmutableList<ChatMessage> Messages { get; init; } = ImmutableList<ChatMessage>.Empty;
    public string CurrentConversationId { get; init; } = string.Empty;
    public bool IsIvanTyping { get; init; } = false;
    public bool IsLoading { get; init; } = false;
    public string LastError { get; init; } = string.Empty;
    public ConversationContext Context { get; init; } = new();
}

// Chat Actions (Fluxor pattern)
public class SendMessageAction
{
    public string Message { get; }
    public string ConversationId { get; }
    public PersonalityContext Context { get; }
    
    public SendMessageAction(string message, string conversationId, PersonalityContext context)
    {
        Message = message;
        ConversationId = conversationId;
        Context = context;
    }
}

public class MessageReceivedAction
{
    public ChatMessage Message { get; }
    public MessageReceivedAction(ChatMessage message) => Message = message;
}

public class TypingIndicatorAction
{
    public bool IsTyping { get; }
    public TypingIndicatorAction(bool isTyping) => IsTyping = isTyping;
}

// Chat Reducer
public static class ChatReducers
{
    [ReducerMethod]
    public static ChatState ReduceSendMessage(ChatState state, SendMessageAction action)
    {
        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            Content = action.Message,
            Role = MessageRole.User,
            Timestamp = DateTime.UtcNow,
            ConversationId = action.ConversationId
        };

        return state with
        {
            Messages = state.Messages.Add(userMessage),
            IsLoading = true,
            LastError = string.Empty
        };
    }

    [ReducerMethod] 
    public static ChatState ReduceMessageReceived(ChatState state, MessageReceivedAction action)
    {
        return state with
        {
            Messages = state.Messages.Add(action.Message),
            IsLoading = false,
            IsIvanTyping = false
        };
    }

    [ReducerMethod]
    public static ChatState ReduceTypingIndicator(ChatState state, TypingIndicatorAction action)
    {
        return state with { IsIvanTyping = action.IsTyping };
    }
}

// Chat Effects (Side effects handling)
public class ChatEffects
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatEffects> _logger;

    public ChatEffects(IChatService chatService, ILogger<ChatEffects> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [EffectMethod]
    public async Task HandleSendMessage(SendMessageAction action, IDispatcher dispatcher)
    {
        try
        {
            var response = await _chatService.SendMessageAsync(action.Message, action.Context);
            
            var ivanMessage = new ChatMessage
            {
                Id = response.Id,
                Content = response.Content,
                Role = MessageRole.Assistant,
                Timestamp = response.Timestamp,
                ConversationId = action.ConversationId,
                Metadata = response.Metadata
            };

            dispatcher.Dispatch(new MessageReceivedAction(ivanMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message");
            dispatcher.Dispatch(new MessageErrorAction(ex.Message));
        }
    }
}
```

### Personality State Management
```csharp
public class PersonalityState
{
    public Dictionary<string, TraitLevel> Traits { get; init; } = new();
    public MoodState CurrentMood { get; init; } = new();
    public WorkContext WorkSituation { get; init; } = new();
    public ImmutableList<RecentActivity> ActivityHistory { get; init; } = ImmutableList<RecentActivity>.Empty;
    public InteractionPreferences OptimalInteraction { get; init; } = new();
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
    public bool IsOnline { get; init; } = false;
}

// Real-time personality updates
public class PersonalityEffects
{
    private readonly IPersonalityService _personalityService;
    private readonly IHubConnection _hubConnection;

    [EffectMethod]
    public async Task HandleSubscribeToPersonalityUpdates(SubscribeToPersonalityUpdatesAction action, IDispatcher dispatcher)
    {
        // Subscribe to SignalR personality updates
        _hubConnection.On<PersonalityState>("PersonalityUpdated", (state) =>
        {
            dispatcher.Dispatch(new PersonalityUpdatedAction(state));
        });

        _hubConnection.On<MoodState>("MoodChanged", (mood) =>
        {
            dispatcher.Dispatch(new MoodChangedAction(mood));
        });

        _hubConnection.On<WorkContext>("WorkContextChanged", (context) =>
        {
            dispatcher.Dispatch(new WorkContextChangedAction(context));
        });

        await _hubConnection.StartAsync();
    }
}
```

---

## 🌐 API INTEGRATION LAYER

### HTTP Client Configuration
```csharp
// API Client Service Registration
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDigitalIvanApiClient(this IServiceCollection services, 
        IConfiguration configuration)
    {
        var apiBaseUrl = configuration["DigitalIvan:ApiBaseUrl"];
        
        services.AddHttpClient<IDigitalIvanApiClient, DigitalIvanApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning("Retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }
}

// API Client Implementation
public class DigitalIvanApiClient : IDigitalIvanApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DigitalIvanApiClient> _logger;

    public DigitalIvanApiClient(HttpClient httpClient, ILogger<DigitalIvanApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ChatResponse> SendMessageAsync(string message, PersonalityContext context)
    {
        var request = new ChatRequest
        {
            Message = message,
            ConversationId = context.ConversationId,
            PersonalityContext = context,
            Timestamp = DateTime.UtcNow
        };

        var response = await _httpClient.PostAsJsonAsync("/api/chat/send", request);
        response.EnsureSuccessStatusCode();

        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();
        return chatResponse ?? throw new InvalidOperationException("Invalid response from API");
    }

    public async Task<PersonalityState> GetCurrentStateAsync()
    {
        var response = await _httpClient.GetAsync("/api/personality/current");
        response.EnsureSuccessStatusCode();

        var personalityState = await response.Content.ReadFromJsonAsync<PersonalityState>();
        return personalityState ?? throw new InvalidOperationException("Invalid personality state response");
    }
}
```

### SignalR Integration
```csharp
// SignalR Hub Client
public class ChatHubClient : IChatHubClient
{
    private HubConnection? _connection;
    private readonly ILogger<ChatHubClient> _logger;
    private readonly IDispatcher _dispatcher;

    public ChatHubClient(ILogger<ChatHubClient> logger, IDispatcher dispatcher)
    {
        _logger = logger;
        _dispatcher = dispatcher;
    }

    public async Task StartConnectionAsync(string hubUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        // Register event handlers
        _connection.On<ChatMessage>("ReceiveMessage", HandleMessageReceived);
        _connection.On<bool>("TypingIndicator", HandleTypingIndicator);
        _connection.On<PersonalityState>("PersonalityUpdated", HandlePersonalityUpdated);
        _connection.On<string>("Error", HandleError);

        // Connection state monitoring
        _connection.Closed += HandleConnectionClosed;
        _connection.Reconnecting += HandleReconnecting;
        _connection.Reconnected += HandleReconnected;

        await _connection.StartAsync();
        _logger.LogInformation("SignalR connection started successfully");
    }

    private void HandleMessageReceived(ChatMessage message)
    {
        _dispatcher.Dispatch(new MessageReceivedAction(message));
    }

    private void HandleTypingIndicator(bool isTyping)
    {
        _dispatcher.Dispatch(new TypingIndicatorAction(isTyping));
    }

    private void HandlePersonalityUpdated(PersonalityState state)
    {
        _dispatcher.Dispatch(new PersonalityUpdatedAction(state));
    }

    public async Task SendMessageAsync(string message, string conversationId)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("SendMessage", message, conversationId);
        }
        else
        {
            throw new InvalidOperationException("SignalR connection is not active");
        }
    }
}
```

---

## 📱 PLATFORM-SPECIFIC IMPLEMENTATIONS

### Blazor Server Web Configuration
```csharp
// Program.cs for Blazor Server
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        
        // Add Fluxor state management
        builder.Services.AddFluxor(options =>
        {
            options.ScanAssemblies(typeof(ChatState).Assembly);
            options.UseReduxDevTools(rdt => rdt.Name = "DigitalIvan");
        });

        // Add shared services
        builder.Services.AddDigitalIvanSharedServices(builder.Configuration);
        builder.Services.AddDigitalIvanApiClient(builder.Configuration);
        
        // Add SignalR
        builder.Services.AddSignalR();
        
        // PWA services
        builder.Services.AddPwa();

        var app = builder.Build();

        // Configure pipeline
        app.UseStaticFiles();
        app.UseRouting();

        app.MapRazorPages();
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");
        app.MapHub<ChatHub>("/chathub");

        app.Run();
    }
}

// Blazor Server-specific chat service
public class BlazorServerChatService : IChatService
{
    private readonly IChatHubClient _hubClient;
    private readonly IDigitalIvanApiClient _apiClient;

    public async Task<ChatResponse> SendMessageAsync(string message, PersonalityContext context)
    {
        // Try SignalR first for real-time experience
        try
        {
            await _hubClient.SendMessageAsync(message, context.ConversationId);
            // Response will come through SignalR event handler
            return new ChatResponse { Success = true };
        }
        catch (Exception)
        {
            // Fallback to HTTP API
            return await _apiClient.SendMessageAsync(message, context);
        }
    }
}
```

### MAUI Hybrid Configuration
```csharp
// MauiProgram.cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                fonts.AddFont("RobotoMono-Regular.ttf", "RobotoMono");
            });

        // Add Blazor Hybrid
        builder.Services.AddMauiBlazorWebView();

        // Add Fluxor state management (shared with Web)
        builder.Services.AddFluxor(options =>
        {
            options.ScanAssemblies(typeof(ChatState).Assembly);
        });

        // Add shared services
        builder.Services.AddDigitalIvanSharedServices(builder.Configuration);
        builder.Services.AddDigitalIvanApiClient(builder.Configuration);

        // Platform-specific services
#if ANDROID
        builder.Services.AddTransient<IPlatformService, AndroidPlatformService>();
        builder.Services.AddTransient<INotificationService, AndroidNotificationService>();
#elif IOS
        builder.Services.AddTransient<IPlatformService, IOSPlatformService>();
        builder.Services.AddTransient<INotificationService, IOSNotificationService>();
#elif WINDOWS
        builder.Services.AddTransient<IPlatformService, WindowsPlatformService>();
        builder.Services.AddTransient<INotificationService, WindowsNotificationService>();
#endif

        return builder.Build();
    }
}

// Platform-specific service interfaces
public interface IPlatformService
{
    Task<bool> RequestPermissionsAsync();
    Task ShareTextAsync(string text);
    Task OpenUrlAsync(string url);
    Task ShowNotificationAsync(string title, string message);
}

// Android implementation example
#if ANDROID
public class AndroidPlatformService : IPlatformService
{
    public async Task<bool> RequestPermissionsAsync()
    {
        var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
        return status == PermissionStatus.Granted;
    }

    public async Task ShareTextAsync(string text)
    {
        await Share.RequestAsync(new ShareTextRequest
        {
            Text = text,
            Title = "Share Ivan's Response"
        });
    }

    public async Task OpenUrlAsync(string url)
    {
        await Browser.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
    }

    public async Task ShowNotificationAsync(string title, string message)
    {
        var notification = new NotificationRequest
        {
            NotificationId = Random.Shared.Next(),
            Title = title,
            Description = message,
            BadgeNumber = 1
        };

        await LocalNotificationCenter.Current.Show(notification);
    }
}
#endif
```

---

## 🎨 COMPONENT ARCHITECTURE PATTERNS

### Shared Component Design Pattern
```csharp
// Base component with common functionality
public abstract class DigitalIvanComponentBase : ComponentBase
{
    [Inject] protected IState<AppState> AppState { get; set; } = default!;
    [Inject] protected IDispatcher Dispatcher { get; set; } = default!;
    [Inject] protected ILogger Logger { get; set; } = default!;

    protected override void OnInitialized()
    {
        AppState.StateChanged += StateHasChanged;
        base.OnInitialized();
    }

    public void Dispose()
    {
        AppState.StateChanged -= StateHasChanged;
    }
}

// Chat container component
@inherits DigitalIvanComponentBase
@using DigitalIvan.UI.Shared.Models
@using DigitalIvan.UI.Shared.Components.Chat

<div class="chat-container @CssClass">
    <div class="chat-header">
        <StatusWidget State="@PersonalityState" />
    </div>
    
    <div class="chat-messages" @ref="messagesContainer">
        <MessageHistory Messages="@ChatState.Messages" />
        
        @if (ChatState.IsIvanTyping)
        {
            <TypingIndicator />
        }
    </div>
    
    <div class="chat-input">
        <MessageInput OnMessageSent="@HandleMessageSent" 
                     IsLoading="@ChatState.IsLoading" />
    </div>
</div>

@code {
    [Parameter] public string CssClass { get; set; } = string.Empty;
    
    private ElementReference messagesContainer;
    
    private ChatState ChatState => AppState.Value.Chat;
    private PersonalityState PersonalityState => AppState.Value.Personality;
    
    private async Task HandleMessageSent(string message)
    {
        var action = new SendMessageAction(
            message, 
            ChatState.CurrentConversationId, 
            CreatePersonalityContext()
        );
        
        Dispatcher.Dispatch(action);
        
        // Auto-scroll to bottom
        await messagesContainer.FocusAsync();
        await InvokeAsync(() => JS.InvokeVoidAsync("scrollToBottom", messagesContainer));
    }
    
    private PersonalityContext CreatePersonalityContext()
    {
        return new PersonalityContext
        {
            ConversationId = ChatState.CurrentConversationId,
            UserPlatform = GetCurrentPlatform(),
            PersonalityState = PersonalityState,
            Timestamp = DateTime.UtcNow
        };
    }
    
    private string GetCurrentPlatform()
    {
#if ANDROID
        return "Android";
#elif IOS
        return "iOS";
#elif WINDOWS
        return "Windows";
#else
        return "Web";
#endif
    }
}
```

### Message Bubble Component
```csharp
// MessageBubble.razor
@using DigitalIvan.UI.Shared.Models

<div class="message-bubble @GetMessageClass()">
    <div class="message-header">
        <span class="message-sender">@GetSenderName()</span>
        <span class="message-timestamp">@GetFormattedTime()</span>
    </div>
    
    <div class="message-content">
        @if (HasCodeContent())
        {
            <CodeBlock Code="@ExtractCode()" Language="@DetectLanguage()" />
        }
        else
        {
            <div class="message-text">@((MarkupString)FormatMessage())</div>
        }
    </div>
    
    @if (Message.Metadata?.Any() == true)
    {
        <div class="message-metadata">
            @foreach (var item in Message.Metadata)
            {
                <span class="metadata-item">@item.Key: @item.Value</span>
            }
        </div>
    }
</div>

@code {
    [Parameter, EditorRequired] public ChatMessage Message { get; set; } = default!;
    
    private string GetMessageClass()
    {
        return Message.Role switch
        {
            MessageRole.User => "message-user",
            MessageRole.Assistant => "message-ivan",
            MessageRole.System => "message-system",
            _ => "message-unknown"
        };
    }
    
    private string GetSenderName()
    {
        return Message.Role switch
        {
            MessageRole.User => "You",
            MessageRole.Assistant => "Ivan",
            MessageRole.System => "System",
            _ => "Unknown"
        };
    }
    
    private string GetFormattedTime()
    {
        var localTime = Message.Timestamp.ToLocalTime();
        return localTime.ToString("HH:mm");
    }
    
    private bool HasCodeContent()
    {
        return Message.Content.Contains("```") || 
               Message.Content.Contains("<code>") ||
               Message.Metadata?.ContainsKey("hasCode") == true;
    }
    
    private string ExtractCode()
    {
        if (Message.Content.Contains("```"))
        {
            var start = Message.Content.IndexOf("```") + 3;
            var end = Message.Content.LastIndexOf("```");
            if (end > start)
            {
                var codeBlock = Message.Content.Substring(start, end - start);
                // Remove language identifier if present
                var newlineIndex = codeBlock.IndexOf('\n');
                return newlineIndex > 0 ? codeBlock.Substring(newlineIndex + 1) : codeBlock;
            }
        }
        
        return Message.Content;
    }
    
    private string DetectLanguage()
    {
        if (Message.Content.StartsWith("```csharp") || Message.Content.StartsWith("```c#"))
            return "csharp";
        if (Message.Content.StartsWith("```json"))
            return "json";
        if (Message.Content.StartsWith("```javascript") || Message.Content.StartsWith("```js"))
            return "javascript";
        if (Message.Content.StartsWith("```typescript") || Message.Content.StartsWith("```ts"))
            return "typescript";
        if (Message.Content.StartsWith("```sql"))
            return "sql";
            
        return "text";
    }
    
    private string FormatMessage()
    {
        var content = Message.Content;
        
        // Remove code blocks for text formatting
        if (HasCodeContent())
        {
            var codeStart = content.IndexOf("```");
            var codeEnd = content.LastIndexOf("```") + 3;
            if (codeEnd > codeStart)
            {
                content = content.Remove(codeStart, codeEnd - codeStart);
            }
        }
        
        // Apply text formatting
        content = content
            .Replace("**", "<strong>", StringComparison.OrdinalIgnoreCase)
            .Replace("**", "</strong>", StringComparison.OrdinalIgnoreCase)
            .Replace("*", "<em>", StringComparison.OrdinalIgnoreCase)
            .Replace("*", "</em>", StringComparison.OrdinalIgnoreCase)
            .Replace("\n", "<br>", StringComparison.OrdinalIgnoreCase);
        
        return content;
    }
}
```

---

## 🔧 BUILD AND DEPLOYMENT CONFIGURATION

### Web App Configuration
```xml
<!-- DigitalIvan.Web.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <ServiceWorkerAssetsManifest>service-worker-assets.js</ServiceWorkerAssetsManifest>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.0" />
    <PackageReference Include="Fluxor.Blazor.Web" Version="5.9.1" />
    <PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.0.0" />
    <PackageReference Include="BlazorPro.BlazorSize" Version="6.2.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\DigitalIvan.UI.Shared\DigitalIvan.UI.Shared.csproj" />
  </ItemGroup>

  <ItemGroup>
    <ServiceWorker Include="wwwroot\service-worker.js" PublishedContent="wwwroot\service-worker.published.js" />
  </ItemGroup>
</Project>
```

### MAUI App Configuration
```xml
<!-- DigitalIvan.MAUI.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFrameworks>net8.0-android;net8.0-ios;net8.0-windows10.0.19041.0</TargetFrameworks>
    <UseMaui>true</UseMaui>
    <MauiVersion>8.0.3</MauiVersion>
    
    <!-- App info -->
    <ApplicationTitle>Digital Ivan</ApplicationTitle>
    <ApplicationId>com.digitalivan.app</ApplicationId>
    <ApplicationVersion>1</ApplicationVersion>
    <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
    
    <!-- Platform-specific -->
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">24.0</SupportedOSPlatformVersion>
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">11.0</SupportedOSPlatformVersion>
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">10.0.17763.0</SupportedOSPlatformVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Maui.Controls" Version="8.0.3" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebView.Maui" Version="8.0.3" />
    <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="8.0.0" />
    <PackageReference Include="CommunityToolkit.Maui" Version="7.0.1" />
    <PackageReference Include="Plugin.LocalNotification" Version="10.1.8" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\DigitalIvan.UI.Shared\DigitalIvan.UI.Shared.csproj" />
  </ItemGroup>
</Project>
```

---

**🏗️ ARCHITECTURE EXCELLENCE**: Comprehensive frontend architecture ensuring 90% code reuse, consistent user experience, and optimal platform-specific features across Web, MAUI, and Telegram.

**🚀 IMPLEMENTATION READY**: Complete technical specification ready for Blazor and MAUI development after Milestone 1 (API Foundation Ready).