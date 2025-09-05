# Phase 2: Core Integrations (Weeks 5-8)

**Objective:** Implement external service integrations and basic frontend interfaces  
**Duration:** 4 weeks  
**Dependencies:** Phase 1 completed and tested  
**Team:** 1x Senior .NET Developer + 1x Frontend Developer (part-time)

## Overview

Phase 2 focuses on building the essential external service integrations and creating the primary user interfaces. This phase establishes the core value proposition by connecting to Google services, implementing the Telegram bot, and providing a web interface for management.

## 2.1 Google Services Integration (Week 5-6)

### Tasks

**Week 5 - Google Calendar Integration:**
- [ ] Set up Google Cloud Console project and enable APIs
- [ ] Implement OAuth2 authentication flow for Google services
- [ ] Create Google Calendar service client with retry logic
- [ ] Implement calendar event CRUD operations
- [ ] Set up calendar synchronization and caching
- [ ] Test calendar integration with multiple time zones

**Week 6 - Extended Google Services:**
- [ ] Implement Google Drive file management
- [ ] Set up Google Contacts synchronization
- [ ] Create service account authentication option
- [ ] Implement rate limiting and quota management
- [ ] Add Google Docs/Sheets basic integration
- [ ] Create Google services health monitoring

### Technical Implementation

**Google Services Configuration:**
```csharp
public static class GoogleServicesExtensions
{
    public static IServiceCollection AddGoogleServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GoogleOptions>(configuration.GetSection("Google"));
        
        services.AddScoped<IGoogleCalendarService, GoogleCalendarService>();
        services.AddScoped<IGoogleDriveService, GoogleDriveService>();
        services.AddScoped<IGoogleContactsService, GoogleContactsService>();
        
        services.AddHttpClient<GoogleCalendarService>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
            
        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}
```

**Google Calendar Service:**
```csharp
public interface IGoogleCalendarService
{
    Task<CalendarEvent[]> GetEventsAsync(DateTime start, DateTime end, string? calendarId = null);
    Task<CalendarEvent> CreateEventAsync(CreateCalendarEventRequest request);
    Task<CalendarEvent> UpdateEventAsync(string eventId, UpdateCalendarEventRequest request);
    Task<bool> DeleteEventAsync(string eventId);
    Task<UserCalendar[]> GetCalendarsAsync();
    Task<bool> SynchronizeCalendarAsync(string userId);
}

public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly CalendarService _calendarService;
    private readonly ILogger<GoogleCalendarService> _logger;
    private readonly IUserIntegrationService _integrationService;

    public GoogleCalendarService(
        CalendarService calendarService,
        ILogger<GoogleCalendarService> logger,
        IUserIntegrationService integrationService)
    {
        _calendarService = calendarService;
        _logger = logger;
        _integrationService = integrationService;
    }

    public async Task<CalendarEvent[]> GetEventsAsync(DateTime start, DateTime end, string? calendarId = null)
    {
        try
        {
            var request = _calendarService.Events.List(calendarId ?? "primary");
            request.TimeMin = start;
            request.TimeMax = end;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var events = await request.ExecuteAsync();
            
            return events.Items?.Select(MapToCalendarEvent).ToArray() ?? Array.Empty<CalendarEvent>();
        }
        catch (GoogleApiException ex)
        {
            _logger.LogError(ex, "Error retrieving calendar events from {Start} to {End}", start, end);
            throw;
        }
    }

    public async Task<CalendarEvent> CreateEventAsync(CreateCalendarEventRequest request)
    {
        try
        {
            var googleEvent = new Event
            {
                Summary = request.Title,
                Description = request.Description,
                Location = request.Location,
                Start = new EventDateTime
                {
                    DateTime = request.StartTime,
                    TimeZone = request.TimeZone ?? "UTC"
                },
                End = new EventDateTime
                {
                    DateTime = request.EndTime,
                    TimeZone = request.TimeZone ?? "UTC"
                }
            };

            if (request.Attendees?.Any() == true)
            {
                googleEvent.Attendees = request.Attendees.Select(email => new EventAttendee { Email = email }).ToList();
            }

            var createdEvent = await _calendarService.Events
                .Insert(googleEvent, request.CalendarId ?? "primary")
                .ExecuteAsync();

            _logger.LogInformation("Created calendar event {EventId}: {Title}", createdEvent.Id, request.Title);
            
            return MapToCalendarEvent(createdEvent);
        }
        catch (GoogleApiException ex)
        {
            _logger.LogError(ex, "Error creating calendar event: {Title}", request.Title);
            throw;
        }
    }

    private static CalendarEvent MapToCalendarEvent(Event googleEvent)
    {
        return new CalendarEvent
        {
            Id = googleEvent.Id,
            Title = googleEvent.Summary ?? "Untitled Event",
            Description = googleEvent.Description,
            Location = googleEvent.Location,
            StartTime = googleEvent.Start.DateTime ?? DateTime.Parse(googleEvent.Start.Date),
            EndTime = googleEvent.End.DateTime ?? DateTime.Parse(googleEvent.End.Date),
            IsAllDay = googleEvent.Start.DateTime == null,
            Attendees = googleEvent.Attendees?.Select(a => a.Email).ToArray() ?? Array.Empty<string>(),
            ExternalId = googleEvent.Id,
            Source = "google"
        };
    }
}
```

**OAuth2 Flow Implementation:**
```csharp
public class GoogleAuthController : ControllerBase
{
    private readonly IGoogleAuthService _authService;

    [HttpGet("google/authorize")]
    public IActionResult AuthorizeGoogle([FromQuery] string userId)
    {
        var authUrl = _authService.GetAuthorizationUrl(userId);
        return Redirect(authUrl);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
    {
        try
        {
            var tokens = await _authService.ExchangeCodeForTokensAsync(code);
            await _authService.SaveUserTokensAsync(state, tokens);
            
            return Ok(new { success = true, message = "Google integration setup successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
```

### Deliverables
- [ ] Google Calendar full CRUD operations working
- [ ] OAuth2 authentication flow for Google services
- [ ] Google Drive file management capabilities
- [ ] Google Contacts synchronization
- [ ] Rate limiting and error handling for all Google APIs
- [ ] Integration health monitoring dashboard

### Acceptance Criteria
- Google Calendar events sync bidirectionally without data loss
- OAuth2 flow completes successfully for new users
- API rate limits are respected with appropriate backoff
- All Google services handle network failures gracefully
- Integration status is monitored and reported accurately

## 2.2 Telegram Bot Implementation (Week 6)

### Tasks

**Telegram Bot Development:**
- [ ] Create Telegram bot via @BotFather and obtain token
- [ ] Implement Telegram Bot API integration with webhook
- [ ] Create message handling pipeline with natural language processing
- [ ] Implement command system (/start, /help, /calendar, /tasks)
- [ ] Set up inline keyboards and quick replies
- [ ] Add voice message transcription capability

### Technical Implementation

**Telegram Bot Service:**
```csharp
public interface ITelegramBotService
{
    Task StartAsync();
    Task StopAsync();
    Task ProcessUpdateAsync(Update update);
    Task SendMessageAsync(long chatId, string message, InlineKeyboardMarkup? replyMarkup = null);
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
            AllowedUpdates = new[]
            {
                UpdateType.Message,
                UpdateType.CallbackQuery,
                UpdateType.InlineQuery
            }
        };

        _botClient.StartReceiving(
            updateHandler: ProcessUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: _cancellationTokenSource.Token);

        var me = await _botClient.GetMeAsync();
        _logger.LogInformation("Telegram bot {BotName} started successfully", me.Username);
    }

    public async Task ProcessUpdateAsync(Update update)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    await HandleMessageAsync(update.Message!);
                    break;
                case UpdateType.CallbackQuery:
                    await HandleCallbackQueryAsync(update.CallbackQuery!);
                    break;
                case UpdateType.InlineQuery:
                    await HandleInlineQueryAsync(update.InlineQuery!);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Telegram update");
        }
    }

    private async Task HandleMessageAsync(Message message)
    {
        if (message.Text is not { } messageText)
            return;

        var userId = message.From!.Id;
        var chatId = message.Chat.Id;

        // Handle commands
        if (messageText.StartsWith('/'))
        {
            await HandleCommandAsync(messageText, chatId, userId);
            return;
        }

        // Handle natural language
        await HandleNaturalLanguageAsync(messageText, chatId, userId);
    }

    private async Task HandleCommandAsync(string command, long chatId, long userId)
    {
        var response = command.Split(' ')[0].ToLower() switch
        {
            "/start" => await HandleStartCommandAsync(userId),
            "/help" => GetHelpMessage(),
            "/calendar" => await HandleCalendarCommandAsync(userId),
            "/tasks" => await HandleTasksCommandAsync(userId),
            "/settings" => await HandleSettingsCommandAsync(userId),
            _ => "Unknown command. Type /help to see available commands."
        };

        await _botClient.SendTextMessageAsync(chatId, response);
    }

    private async Task HandleNaturalLanguageAsync(string message, long chatId, long userId)
    {
        try
        {
            var session = await _agentService.StartSessionAsync(userId, "telegram");
            var context = new AgentContext
            {
                SessionId = session.Id,
                UserId = userId.ToString(),
                ChatId = chatId,
                Platform = "telegram"
            };

            var response = await _agentService.ProcessRequestAsync(message, context);
            
            // Handle response formatting for Telegram
            var formattedResponse = FormatResponseForTelegram(response);
            
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: formattedResponse,
                parseMode: ParseMode.Markdown,
                replyMarkup: GenerateInlineKeyboard(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing natural language message from user {UserId}", userId);
            await _botClient.SendTextMessageAsync(chatId, "Sorry, I encountered an error processing your request. Please try again.");
        }
    }

    private async Task<string> HandleStartCommandAsync(long userId)
    {
        var user = await _agentService.GetOrCreateUserAsync(userId);
        
        return $@"👋 Welcome to DigitalMe, {user.FirstName ?? "there"}!

I'm your personal agent, ready to help you with:
📅 Calendar management
✅ Task tracking  
📱 Smart notifications
🔗 Service integrations

To get started, try:
• ""Show my calendar for today""
• ""Create a task to buy groceries""
• ""What's my schedule tomorrow?""

Type /help for more commands or just talk to me naturally!";
    }

    private InlineKeyboardMarkup? GenerateInlineKeyboard(AgentResponse response)
    {
        if (response.SuggestedActions?.Any() != true)
            return null;

        var keyboard = response.SuggestedActions
            .Select(action => new[]
            {
                InlineKeyboardButton.WithCallbackData(action.Title, $"action:{action.Id}")
            })
            .ToArray();

        return new InlineKeyboardMarkup(keyboard);
    }
}
```

**Command Handlers:**
```csharp
public class TelegramCommandHandlers
{
    private readonly ICalendarService _calendarService;
    private readonly ITaskService _taskService;

    public async Task<string> HandleCalendarCommandAsync(string userId, string[] args)
    {
        var date = args.Length > 0 && DateTime.TryParse(args[0], out var parsedDate)
            ? parsedDate
            : DateTime.Today;

        var events = await _calendarService.GetEventsAsync(date, date.AddDays(1), userId);

        if (!events.Any())
            return $"📅 No events scheduled for {date:MMM dd, yyyy}";

        var response = $"📅 Your schedule for {date:MMM dd, yyyy}:\n\n";
        foreach (var evt in events.OrderBy(e => e.StartTime))
        {
            var time = evt.IsAllDay ? "All day" : evt.StartTime.ToString("HH:mm");
            response += $"• {time} - {evt.Title}\n";
            if (!string.IsNullOrEmpty(evt.Location))
                response += $"  📍 {evt.Location}\n";
        }

        return response;
    }

    public async Task<string> HandleTasksCommandAsync(string userId, string[] args)
    {
        var tasks = await _taskService.GetActiveTasksAsync(userId);

        if (!tasks.Any())
            return "✅ You're all caught up! No pending tasks.";

        var response = "📋 Your active tasks:\n\n";
        foreach (var task in tasks.OrderBy(t => t.Priority).ThenBy(t => t.DueDate))
        {
            var priority = task.Priority switch
            {
                TaskPriority.High => "🔴",
                TaskPriority.Medium => "🟡",
                _ => "🟢"
            };
            
            var dueDate = task.DueDate?.ToString("MMM dd") ?? "No due date";
            response += $"{priority} {task.Title} ({dueDate})\n";
        }

        return response;
    }
}
```

### Deliverables
- [ ] Fully functional Telegram bot with all core commands
- [ ] Natural language processing integration
- [ ] Inline keyboards and quick replies working
- [ ] Voice message transcription capability
- [ ] Webhook setup for production deployment
- [ ] Comprehensive error handling and logging

### Acceptance Criteria
- Bot responds to all implemented commands correctly
- Natural language requests are processed and return appropriate responses
- Inline keyboards generate the correct actions
- Bot handles high message volume without errors
- Webhook processes messages reliably in production

## 2.3 GitHub API Integration (Week 7)

### Tasks

**GitHub Integration Development:**
- [ ] Set up GitHub API authentication with personal access tokens
- [ ] Implement repository management operations
- [ ] Create issue tracking integration
- [ ] Set up GitHub Actions monitoring
- [ ] Implement webhook handling for repository events
- [ ] Add pull request management capabilities

### Technical Implementation

**GitHub Service Interface:**
```csharp
public interface IGitHubService
{
    Task<Repository[]> GetRepositoriesAsync(string username);
    Task<Repository> GetRepositoryAsync(string owner, string repo);
    Task<Issue[]> GetIssuesAsync(string owner, string repo, IssueFilter? filter = null);
    Task<Issue> CreateIssueAsync(string owner, string repo, CreateIssueRequest request);
    Task<Issue> UpdateIssueAsync(string owner, string repo, int number, UpdateIssueRequest request);
    Task<PullRequest[]> GetPullRequestsAsync(string owner, string repo, PullRequestFilter? filter = null);
    Task<WorkflowRun[]> GetWorkflowRunsAsync(string owner, string repo);
}

public class GitHubService : IGitHubService
{
    private readonly GitHubClient _githubClient;
    private readonly ILogger<GitHubService> _logger;

    public GitHubService(GitHubClient githubClient, ILogger<GitHubService> logger)
    {
        _githubClient = githubClient;
        _logger = logger;
    }

    public async Task<Repository[]> GetRepositoriesAsync(string username)
    {
        try
        {
            var repos = await _githubClient.Repository.GetAllForUser(username);
            return repos.Select(MapToRepository).ToArray();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Error retrieving repositories for user {Username}", username);
            throw;
        }
    }

    public async Task<Issue> CreateIssueAsync(string owner, string repo, CreateIssueRequest request)
    {
        try
        {
            var newIssue = new NewIssue(request.Title)
            {
                Body = request.Description
            };

            if (request.Labels?.Any() == true)
            {
                foreach (var label in request.Labels)
                    newIssue.Labels.Add(label);
            }

            if (!string.IsNullOrEmpty(request.Assignee))
                newIssue.Assignees.Add(request.Assignee);

            var issue = await _githubClient.Issue.Create(owner, repo, newIssue);
            
            _logger.LogInformation("Created GitHub issue #{Number} in {Owner}/{Repo}: {Title}", 
                issue.Number, owner, repo, request.Title);
            
            return MapToIssue(issue);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "Error creating GitHub issue in {Owner}/{Repo}: {Title}", 
                owner, repo, request.Title);
            throw;
        }
    }

    private static Repository MapToRepository(Octokit.Repository githubRepo)
    {
        return new Repository
        {
            Id = githubRepo.Id,
            Name = githubRepo.Name,
            FullName = githubRepo.FullName,
            Description = githubRepo.Description,
            IsPrivate = githubRepo.Private,
            Url = githubRepo.HtmlUrl,
            Language = githubRepo.Language,
            StarCount = githubRepo.StargazersCount,
            ForkCount = githubRepo.ForksCount,
            LastUpdated = githubRepo.UpdatedAt.DateTime
        };
    }
}
```

**GitHub Webhooks Handler:**
```csharp
[ApiController]
[Route("api/webhooks/github")]
public class GitHubWebhooksController : ControllerBase
{
    private readonly IGitHubWebhookService _webhookService;
    private readonly ILogger<GitHubWebhooksController> _logger;

    [HttpPost]
    public async Task<IActionResult> HandleWebhook([FromHeader(Name = "X-GitHub-Event")] string eventType)
    {
        try
        {
            var payload = await ReadPayloadAsync();
            
            await _webhookService.ProcessWebhookAsync(eventType, payload);
            
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GitHub webhook: {EventType}", eventType);
            return StatusCode(500);
        }
    }

    private async Task<string> ReadPayloadAsync()
    {
        using var reader = new StreamReader(Request.Body);
        return await reader.ReadToEndAsync();
    }
}

public class GitHubWebhookService : IGitHubWebhookService
{
    public async Task ProcessWebhookAsync(string eventType, string payload)
    {
        switch (eventType)
        {
            case "issues":
                await HandleIssuesEventAsync(payload);
                break;
            case "pull_request":
                await HandlePullRequestEventAsync(payload);
                break;
            case "workflow_run":
                await HandleWorkflowRunEventAsync(payload);
                break;
            default:
                _logger.LogInformation("Unhandled webhook event type: {EventType}", eventType);
                break;
        }
    }
}
```

### Deliverables
- [ ] GitHub repository management functionality
- [ ] Issue creation and tracking system
- [ ] Pull request monitoring
- [ ] GitHub Actions workflow monitoring
- [ ] Webhook processing for real-time updates
- [ ] GitHub integration with Semantic Kernel plugins

### Acceptance Criteria
- Can successfully retrieve and display repository information
- Issue creation works with proper formatting and labels
- Webhooks process GitHub events without errors
- Integration respects GitHub API rate limits
- All GitHub operations are logged and monitored

## 2.4 Basic Web Frontend (Blazor) (Week 7-8)

### Tasks

**Week 7 - Blazor Application Setup:**
- [ ] Create Blazor Server application project
- [ ] Set up authentication and authorization pages
- [ ] Implement responsive layout with navigation
- [ ] Create user dashboard with overview widgets
- [ ] Set up SignalR client for real-time updates

**Week 8 - Core Features Implementation:**
- [ ] Build calendar management interface
- [ ] Create task management with CRUD operations
- [ ] Implement conversation history browser
- [ ] Add integration management settings
- [ ] Set up notification system

### Technical Implementation

**Blazor Project Structure:**
```
DigitalMe.Web/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor
│   │   ├── NavMenu.razor
│   │   └── TopBar.razor
│   ├── Pages/
│   │   ├── Dashboard.razor
│   │   ├── Calendar.razor
│   │   ├── Tasks.razor
│   │   └── Settings.razor
│   └── Shared/
│       ├── LoadingSpinner.razor
│       ├── ConfirmDialog.razor
│       └── NotificationComponent.razor
├── Services/
│   ├── ApiService.cs
│   ├── SignalRService.cs
│   └── AuthenticationService.cs
└── wwwroot/
    ├── css/
    ├── js/
    └── images/
```

**Main Dashboard Component:**
```razor
@page "/"
@using DigitalMe.Web.Services
@inject ApiService ApiService
@inject SignalRService SignalRService
@implements IAsyncDisposable

<PageTitle>DigitalMe - Dashboard</PageTitle>

<div class="dashboard-container">
    <div class="row">
        <div class="col-md-8">
            <div class="card">
                <div class="card-header">
                    <h5>📅 Today's Schedule</h5>
                </div>
                <div class="card-body">
                    @if (todayEvents?.Any() == true)
                    {
                        @foreach (var evt in todayEvents)
                        {
                            <div class="event-item">
                                <div class="event-time">@evt.StartTime.ToString("HH:mm")</div>
                                <div class="event-details">
                                    <div class="event-title">@evt.Title</div>
                                    @if (!string.IsNullOrEmpty(evt.Location))
                                    {
                                        <div class="event-location">📍 @evt.Location</div>
                                    }
                                </div>
                            </div>
                        }
                    }
                    else
                    {
                        <p class="text-muted">No events scheduled for today</p>
                    }
                </div>
            </div>
        </div>
        
        <div class="col-md-4">
            <div class="card">
                <div class="card-header">
                    <h5>✅ Active Tasks</h5>
                </div>
                <div class="card-body">
                    @if (activeTasks?.Any() == true)
                    {
                        @foreach (var task in activeTasks.Take(5))
                        {
                            <div class="task-item">
                                <input type="checkbox" @onchange="@(() => ToggleTask(task.Id))" />
                                <span class="@(task.Priority == TaskPriority.High ? "high-priority" : "")">
                                    @task.Title
                                </span>
                            </div>
                        }
                    }
                    else
                    {
                        <p class="text-muted">All caught up!</p>
                    }
                </div>
            </div>
        </div>
    </div>

    <div class="row mt-4">
        <div class="col-12">
            <div class="card">
                <div class="card-header">
                    <h5>💬 Quick Chat</h5>
                </div>
                <div class="card-body">
                    <ChatComponent @ref="chatComponent" />
                </div>
            </div>
        </div>
    </div>
</div>

@code {
    private CalendarEvent[]? todayEvents;
    private UserTask[]? activeTasks;
    private ChatComponent? chatComponent;

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardData();
        await SignalRService.StartAsync();
        
        SignalRService.OnTaskUpdated += HandleTaskUpdated;
        SignalRService.OnCalendarEventUpdated += HandleCalendarEventUpdated;
    }

    private async Task LoadDashboardData()
    {
        var today = DateTime.Today;
        todayEvents = await ApiService.GetCalendarEventsAsync(today, today.AddDays(1));
        activeTasks = await ApiService.GetActiveTasksAsync();
    }

    private async Task ToggleTask(Guid taskId)
    {
        await ApiService.ToggleTaskAsync(taskId);
        await LoadDashboardData(); // Refresh tasks
        StateHasChanged();
    }

    private void HandleTaskUpdated(UserTask task)
    {
        InvokeAsync(() =>
        {
            LoadDashboardData();
            StateHasChanged();
        });
    }

    private void HandleCalendarEventUpdated(CalendarEvent evt)
    {
        InvokeAsync(() =>
        {
            LoadDashboardData();
            StateHasChanged();
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (SignalRService != null)
        {
            SignalRService.OnTaskUpdated -= HandleTaskUpdated;
            SignalRService.OnCalendarEventUpdated -= HandleCalendarEventUpdated;
            await SignalRService.DisposeAsync();
        }
    }
}
```

**Calendar Management Component:**
```razor
@page "/calendar"
@using DigitalMe.Web.Components
@inject ApiService ApiService

<PageTitle>Calendar - DigitalMe</PageTitle>

<div class="calendar-container">
    <div class="d-flex justify-content-between align-items-center mb-3">
        <h2>📅 Calendar</h2>
        <button class="btn btn-primary" @onclick="ShowCreateEventModal">
            + New Event
        </button>
    </div>

    <div class="row">
        <div class="col-md-3">
            <div class="card">
                <div class="card-body">
                    <h6>Quick Actions</h6>
                    <div class="d-grid gap-2">
                        <button class="btn btn-outline-primary btn-sm" @onclick="@(() => NavigateToDate(DateTime.Today))">
                            Today
                        </button>
                        <button class="btn btn-outline-primary btn-sm" @onclick="@(() => NavigateToDate(DateTime.Today.AddDays(1)))">
                            Tomorrow
                        </button>
                        <button class="btn btn-outline-primary btn-sm" @onclick="@(() => NavigateToDate(GetStartOfWeek(DateTime.Today)))">
                            This Week
                        </button>
                    </div>
                </div>
            </div>
        </div>
        
        <div class="col-md-9">
            <CalendarViewComponent 
                @ref="calendarView"
                Events="events"
                CurrentDate="currentDate"
                OnDateChanged="HandleDateChanged"
                OnEventSelected="HandleEventSelected" />
        </div>
    </div>
</div>

<CreateEventModal @ref="createEventModal" 
                  OnEventCreated="HandleEventCreated" />

@code {
    private CalendarEvent[] events = Array.Empty<CalendarEvent>();
    private DateTime currentDate = DateTime.Today;
    private CalendarViewComponent? calendarView;
    private CreateEventModal? createEventModal;

    protected override async Task OnInitializedAsync()
    {
        await LoadCalendarEvents();
    }

    private async Task LoadCalendarEvents()
    {
        var startDate = currentDate.AddDays(-30);
        var endDate = currentDate.AddDays(30);
        events = await ApiService.GetCalendarEventsAsync(startDate, endDate);
        StateHasChanged();
    }

    private async Task HandleDateChanged(DateTime newDate)
    {
        currentDate = newDate;
        await LoadCalendarEvents();
    }

    private void HandleEventSelected(CalendarEvent evt)
    {
        // Handle event selection (show details, edit, etc.)
    }

    private async Task ShowCreateEventModal()
    {
        if (createEventModal != null)
            await createEventModal.ShowAsync();
    }

    private async Task HandleEventCreated(CalendarEvent newEvent)
    {
        await LoadCalendarEvents(); // Refresh events
    }
}
```

**SignalR Service:**
```csharp
public class SignalRService : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;

    public event Action<UserTask>? OnTaskUpdated;
    public event Action<CalendarEvent>? OnCalendarEventUpdated;
    public event Action<string>? OnNotificationReceived;

    public SignalRService(NavigationManager navigationManager)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri("/agenthub"))
            .Build();

        _hubConnection.On<UserTask>("TaskUpdated", task => OnTaskUpdated?.Invoke(task));
        _hubConnection.On<CalendarEvent>("CalendarEventUpdated", evt => OnCalendarEventUpdated?.Invoke(evt));
        _hubConnection.On<string>("NotificationReceived", message => OnNotificationReceived?.Invoke(message));
    }

    public async Task StartAsync()
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            await _hubConnection.StartAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
```

### Deliverables
- [ ] Fully functional Blazor web application
- [ ] User authentication and authorization system
- [ ] Dashboard with calendar and task overview
- [ ] Calendar management interface with CRUD operations
- [ ] Task management with real-time updates
- [ ] Integration settings page
- [ ] Real-time SignalR communication working

### Acceptance Criteria
- Web application loads quickly and is responsive
- Authentication works with proper session management
- Calendar displays events correctly with timezone handling
- Task management allows CRUD operations
- Real-time updates appear without page refresh
- Application works on desktop and mobile browsers

## Phase 2 Success Criteria

### Integration Validation
- [ ] Google Calendar sync works bidirectionally
- [ ] Telegram bot responds appropriately to all message types
- [ ] GitHub integration creates and tracks issues successfully
- [ ] Web frontend displays data from all integrated services
- [ ] Real-time updates work across all interfaces

### User Experience Validation
- [ ] Users can complete end-to-end workflows (create task → view in calendar → get notified via Telegram)
- [ ] Authentication flow is smooth across all interfaces
- [ ] Error messages are user-friendly and actionable
- [ ] Loading states provide appropriate feedback
- [ ] Mobile experience is usable and responsive

### Technical Validation
- [ ] API endpoints handle expected load without errors
- [ ] External service integrations respect rate limits
- [ ] Real-time communication maintains connections reliably
- [ ] Database operations perform within acceptable time limits
- [ ] Security measures prevent unauthorized access

### Performance Metrics
- [ ] Web application loads in < 3 seconds
- [ ] API responses average < 500ms
- [ ] Telegram bot responds in < 2 seconds
- [ ] SignalR updates appear in < 1 second
- [ ] Google API calls complete successfully 99%+ of the time

## Risk Mitigation

### External Dependency Risks
**Google API Changes/Limits:** Implement robust error handling and fallback mechanisms  
**Telegram API Instability:** Use webhook with polling fallback  
**GitHub API Rate Limiting:** Implement intelligent caching and request batching  

### Technical Risks
**SignalR Connection Issues:** Implement automatic reconnection logic  
**Browser Compatibility:** Test on all major browsers and provide fallbacks  
**Mobile Responsiveness:** Extensive testing on various device sizes  

## Next Phase Preparation

### Phase 3 Prerequisites
- [ ] All Phase 2 integrations tested and stable
- [ ] User acceptance testing completed
- [ ] Performance benchmarks established
- [ ] Mobile development environment set up
- [ ] MAUI project template created and configured