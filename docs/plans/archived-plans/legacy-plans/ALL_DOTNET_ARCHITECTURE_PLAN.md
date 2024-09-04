# All-.NET персональный агент Ивана

## Обзор архитектуры

**Концепция:** Единая .NET экосистема для всех компонентов персонального агента с Claude Code как LLM оркестратором.

### Основные принципы:
- **All-in .NET** - единый технологический стек
- **Multi-Frontend** - 5 способов взаимодействия  
- **Claude Code** как intelligence layer через MCP
- **Shared Codebase** - максимальное переиспользование кода
- **Cloud-Agnostic** - без привязки к конкретному облачному провайдеру

## Архитектурная диаграмма

```
┌─────────────────────────────────────────────────────────────────┐
│                         Frontend Layer                         │
├─────────────┬─────────────┬─────────────┬─────────────┬─────────┤
│ Blazor Web  │ MAUI Mobile │MAUI Desktop │Telegram Bot │ Voice   │
│ Server/WASM │iOS/Android  │Win/Mac/Linux│   .NET      │ Azure   │
└─────────────┴─────────────┴─────────────┴─────────────┴─────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    .NET 8 Backend API                          │
├─────────────────────────────────────────────────────────────────│
│ • ASP.NET Core Web API  • SignalR Hubs  • MCP Client          │
│ • Authentication        • Orchestration  • Personality Engine  │
│ • Background Services   • Rate Limiting   • Context Manager    │
└─────────────────────────────────────────────────────────────────┘
                              │
                    ┌─────────┴─────────┐
                    ▼                   ▼
        ┌─────────────────────┐  ┌─────────────────┐
        │   Claude Code       │  │ External APIs   │
        │   (MCP Server)      │  │ • Telegram API  │
        │ • LLM Intelligence  │  │ • Google APIs   │
        │ • Code Generation   │  │ • GitHub API    │
        │ • Reasoning         │  │ • Slack API     │
        │ • Tool Execution    │  │ • OpenAI API    │
        └─────────────────────┘  └─────────────────┘
                              │
                              ▼
                ┌─────────────────────────────┐
                │       Data Layer            │
                │ • PostgreSQL (Primary)     │
                │ • Redis (Cache/Sessions)   │
                │ • File Storage (Docs/Media)│
                └─────────────────────────────┘
```

## Технологический стек

### Backend (.NET 8)
```xml
<!-- Core Framework -->
<PackageReference Include="Microsoft.AspNetCore.App" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.10" />

<!-- SignalR for Real-time -->
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="1.1.0" />

<!-- MCP Integration -->
<PackageReference Include="Microsoft.Extensions.AI.MCP" Version="9.0.0" />

<!-- External Integrations -->
<PackageReference Include="Telegram.Bot" Version="22.6.0" />
<PackageReference Include="Google.Apis.Calendar.v3" Version="1.69.0.3746" />
<PackageReference Include="Octokit" Version="13.0.1" />

<!-- Background Processing -->
<PackageReference Include="Hangfire.AspNetCore" Version="1.8.12" />
<PackageReference Include="Hangfire.PostgreSql" Version="1.20.9" />

<!-- Caching -->
<PackageReference Include="StackExchange.Redis" Version="2.8.16" />

<!-- Authentication -->
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.10" />
</xml>
```

### Web Frontend (Blazor)
**Выбор: Blazor Server + Blazor WebAssembly Hybrid**

```xml
<!-- Blazor Components -->
<PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="8.0.10" />
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="8.0.10" />

<!-- UI Framework -->
<PackageReference Include="MudBlazor" Version="7.8.0" />

<!-- SignalR Client -->
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.10" />
```

**Преимущества Blazor:**
- Shared models с backend
- Real-time updates через SignalR
- Server-side рендеринг для SEO
- WebAssembly для offline работы

### Mobile & Desktop (MAUI)
```xml
<!-- MAUI Framework -->
<PackageReference Include="Microsoft.Maui.Controls" Version="8.0.91" />
<PackageReference Include="Microsoft.Maui.Controls.Compatibility" Version="8.0.91" />

<!-- Platform Specific -->
<PackageReference Include="Microsoft.Maui.Essentials" Version="8.0.91" />

<!-- HTTP Client -->
<PackageReference Include="Microsoft.Extensions.Http" Version="8.0.1" />

<!-- SignalR for Real-time -->
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.10" />
```

**MAUI Преимущества:**
- Shared business logic с Web
- Native performance
- Platform-specific UI customization
- Offline capabilities

### Voice Integration
```xml
<!-- Azure Cognitive Services -->
<PackageReference Include="Microsoft.CognitiveServices.Speech" Version="1.40.0" />

<!-- Audio Processing -->
<PackageReference Include="NAudio" Version="2.2.1" />
```

## Компонентная архитектура

### Shared Libraries (.NET Standard 2.1)
```
DigitalMe.Shared/
├── Models/           # Общие модели данных
├── DTOs/            # Data Transfer Objects
├── Interfaces/      # Контракты сервисов
├── Constants/       # Константы и конфигурация
└── Extensions/      # Extension methods
```

### Backend Services
```
DigitalMe.Api/
├── Controllers/     # REST API endpoints
├── Hubs/           # SignalR hubs
├── Services/       # Business logic
├── Middleware/     # Custom middleware
├── Data/           # EF Context & Migrations
└── MCP/            # Claude Code integration
```

### Frontend Projects
```
DigitalMe.Web/          # Blazor Web App
DigitalMe.Mobile/       # MAUI Mobile App  
DigitalMe.Desktop/      # MAUI Desktop App
DigitalMe.TelegramBot/  # Telegram Bot Service
```

## API Контракты

### REST API Endpoints
```csharp
// Conversation Management
[HttpPost("/api/conversation/message")]
public async Task<ConversationResponse> ProcessMessage(MessageRequest request)

[HttpGet("/api/conversation/history")]
public async Task<List<ConversationMessage>> GetHistory(int limit = 50)

// Context Management  
[HttpGet("/api/context/current")]
public async Task<PersonalityContext> GetCurrentContext()

[HttpPost("/api/context/update")]
public async Task UpdateContext(ContextUpdateRequest request)

// Task Management
[HttpPost("/api/tasks")]
public async Task<TaskResponse> CreateTask(CreateTaskRequest request)

[HttpGet("/api/tasks")]
public async Task<List<UserTask>> GetTasks(TaskFilter filter)

// Calendar Integration
[HttpGet("/api/calendar/events")]
public async Task<List<CalendarEvent>> GetEvents(DateRange range)

[HttpPost("/api/calendar/events")]
public async Task<CalendarEvent> CreateEvent(CreateEventRequest request)

// External Integrations
[HttpPost("/api/telegram/send")]
public async Task SendTelegramMessage(TelegramMessageRequest request)

[HttpGet("/api/github/repositories")]
public async Task<List<Repository>> GetRepositories()
```

### SignalR Hubs
```csharp
public class PersonalAgentHub : Hub
{
    // Real-time notifications
    public async Task JoinPersonalRoom(string userId)
    
    // Cross-device synchronization
    public async Task SyncContextUpdate(PersonalityContext context)
    
    // Live conversation updates
    public async Task SendMessage(string message)
    
    // Task updates
    public async Task NotifyTaskUpdate(UserTask task)
}
```

## Claude Code Integration (MCP)

### MCP Resources
```csharp
// Personality Profile Resource
public class PersonalityResource : IMCPResource
{
    public string Name => "ivan-personality-profile";
    public string Description => "Detailed personality profile of Ivan";
    
    public async Task<string> GetContent()
    {
        // Load Ivan's personality data from database
        var profile = await _profileService.GetPersonalityProfile();
        return JsonSerializer.Serialize(profile);
    }
}

// Memory System Resource
public class ConversationMemoryResource : IMCPResource
{
    public string Name => "conversation-memory";
    public string Description => "Recent conversation history and context";
    
    public async Task<string> GetContent()
    {
        var history = await _conversationService.GetRecentHistory(50);
        return JsonSerializer.Serialize(history);
    }
}
```

### MCP Tools
```csharp
// Telegram Integration Tool
public class TelegramTool : IMCPTool
{
    public string Name => "send-telegram-message";
    public string Description => "Send message in Telegram chat";
    
    public async Task<MCPToolResult> Execute(MCPToolRequest request)
    {
        var chatId = request.Parameters["chat_id"];
        var message = request.Parameters["message"];
        
        await _telegramService.SendMessage(chatId, message);
        return MCPToolResult.Success("Message sent successfully");
    }
}

// Calendar Management Tool
public class CalendarTool : IMCPTool
{
    public string Name => "manage-calendar";
    public string Description => "Create, update, or delete calendar events";
    
    public async Task<MCPToolResult> Execute(MCPToolRequest request)
    {
        var action = request.Parameters["action"]; // create, update, delete
        var eventData = request.Parameters["event"];
        
        return action switch
        {
            "create" => await CreateEvent(eventData),
            "update" => await UpdateEvent(eventData),
            "delete" => await DeleteEvent(eventData),
            _ => MCPToolResult.Error("Unknown action")
        };
    }
}
```

## Data Models

### Core Entities
```csharp
public class PersonalityProfile
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string ProfileData { get; set; } // JSON with Big Five, preferences, etc
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ConversationMessage
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Content { get; set; }
    public MessageType Type { get; set; } // User, Assistant, System
    public string Source { get; set; } // Web, Mobile, Telegram, etc
    public DateTime Timestamp { get; set; }
    public string? Context { get; set; } // JSON context data
}

public class UserTask
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Source { get; set; } // Which frontend created the task
}

public class ExternalIntegration
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public IntegrationType Type { get; set; } // Telegram, Google, GitHub, etc
    public string Configuration { get; set; } // JSON with tokens, settings
    public bool IsEnabled { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
}
```

## Security & Authentication

### Identity System
```csharp
public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation properties
    public PersonalityProfile PersonalityProfile { get; set; }
    public List<ExternalIntegration> Integrations { get; set; }
}
```

### JWT Configuration
```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
        
        // SignalR JWT support
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && 
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
```

## Deployment & Infrastructure

### Cloud-Agnostic подход
**Containerized deployment с поддержкой множества провайдеров**

#### Docker Configuration
```dockerfile
# Backend API
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DigitalMe.Api/DigitalMe.Api.csproj", "DigitalMe.Api/"]
COPY ["DigitalMe.Shared/DigitalMe.Shared.csproj", "DigitalMe.Shared/"]
RUN dotnet restore "DigitalMe.Api/DigitalMe.Api.csproj"

COPY . .
RUN dotnet build "DigitalMe.Api/DigitalMe.Api.csproj" -c Release -o /app/build
RUN dotnet publish "DigitalMe.Api/DigitalMe.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DigitalMe.Api.dll"]
```

#### Database Options
```yaml
# Option 1: Supabase PostgreSQL
SUPABASE_CONNECTION_STRING: "Server=db.xxx.supabase.co;Port=5432;Database=postgres;User Id=postgres;Password=xxx;"

# Option 2: Railway PostgreSQL  
RAILWAY_CONNECTION_STRING: "postgresql://postgres:xxx@containers-us-west-xxx.railway.app:5432/railway"

# Option 3: Render PostgreSQL
RENDER_CONNECTION_STRING: "postgresql://user:pass@xxx.render.com/dbname"

# Option 4: Self-hosted
POSTGRES_CONNECTION_STRING: "Server=localhost;Port=5432;Database=digitalme;User Id=postgres;Password=xxx;"
```

#### Hosting Platforms
```yaml
# Container-ready deployment options:

# 1. Railway (Recommended for simplicity)
# - Auto-deploy from GitHub
# - Built-in PostgreSQL 
# - $5-20/month
# - Zero config deployment

# 2. Render
# - Docker support
# - PostgreSQL add-on
# - $7-25/month
# - Easy scaling

# 3. DigitalOcean App Platform
# - Kubernetes-based
# - Database add-ons
# - $12-50/month
# - Good performance

# 4. Azure Container Apps
# - Native .NET integration
# - Scale-to-zero
# - $15-40/month
# - Microsoft ecosystem

# 5. Self-hosted VPS
# - Full control
# - Docker Compose
# - $5-20/month
# - Requires maintenance
```

## Development Plan

### Phase 1: Core Backend + MCP (4 weeks)
**MVP с основной функциональностью**

**Week 1-2: Backend Foundation**
- ✅ .NET 8 Web API project setup
- ✅ Entity Framework Core + PostgreSQL
- ✅ Identity system with JWT
- ✅ Basic SignalR hub
- ✅ MCP client integration

**Week 3-4: Claude Integration**  
- ✅ MCP Resources (Personality Profile, Memory)
- ✅ MCP Tools (basic set)
- ✅ Claude Code communication
- ✅ Simple personality engine
- ✅ Unit tests for core functionality

**Deliverables:**
- Working API with authentication
- Claude Code integration via MCP
- Basic personality responses
- Database with core entities

### Phase 2: Primary Frontends (4 weeks)
**Telegram Bot + Blazor Web App**

**Week 1-2: Telegram Bot**
- ✅ Telegram.Bot integration
- ✅ Message processing pipeline
- ✅ Voice message support
- ✅ Context management
- ✅ Rich formatting and keyboards

**Week 3-4: Blazor Web App**
- ✅ Blazor Server application
- ✅ MudBlazor UI framework
- ✅ SignalR real-time updates
- ✅ Responsive design
- ✅ Authentication flow

**Deliverables:**  
- Fully functional Telegram bot
- Web interface for conversations
- Cross-platform context sync
- Voice interaction support

### Phase 3: Mobile & Advanced Features (4 weeks)
**MAUI Apps + Enhanced Integrations**

**Week 1-2: MAUI Mobile App**
- ✅ iOS/Android MAUI project
- ✅ Shared UI components
- ✅ Push notifications
- ✅ Offline capability
- ✅ Voice integration

**Week 3-4: Advanced Integrations**
- ✅ Google Calendar full integration
- ✅ GitHub repository access
- ✅ Enhanced MCP tools
- ✅ Background processing
- ✅ Analytics and monitoring

**Deliverables:**
- Native mobile applications
- Full external service integrations
- Background task processing
- Performance monitoring

### Phase 4: Production & Polish (4 weeks)
**Desktop App + Production Readiness**

**Week 1-2: MAUI Desktop**
- ✅ Windows/macOS/Linux apps  
- ✅ System tray integration
- ✅ Desktop notifications
- ✅ File system integration
- ✅ Keyboard shortcuts

**Week 3-4: Production Setup**
- ✅ CI/CD pipelines
- ✅ Monitoring & logging
- ✅ Security hardening
- ✅ Performance optimization
- ✅ Documentation

**Deliverables:**
- Cross-platform desktop apps
- Production deployment
- Comprehensive monitoring
- User documentation

## Cost Estimation

### Development Costs (16 weeks)
- **Solo developer:** $0 (your time)
- **1 Additional senior dev:** $64,000-96,000
- **Total team cost:** $64,000-96,000

### Monthly Operational Costs
```
Database (PostgreSQL):
  - Supabase Pro: $25/month
  - Railway: $20/month  
  - Render: $25/month

Hosting (Container):
  - Railway: $10-30/month
  - Render: $15-40/month
  - DigitalOcean: $20-50/month

External APIs:
  - OpenAI: $50-200/month
  - Google APIs: $10-50/month
  - Telegram Bot: Free
  - GitHub API: Free (personal use)

Storage & CDN:
  - File storage: $5-20/month
  - CDN: $10-30/month

Total: $135-445/month
```

## Risk Assessment & Mitigation

### Technical Risks
1. **MCP Integration Complexity**
   - *Risk:* Claude Code MCP API changes
   - *Mitigation:* Abstraction layer, regular updates

2. **Cross-Platform Compatibility**  
   - *Risk:* MAUI platform-specific issues
   - *Mitigation:* Platform-specific testing, gradual rollout

3. **Real-time Performance**
   - *Risk:* SignalR scalability issues
   - *Mitigation:* Redis backplane, connection limits

### Operational Risks  
1. **API Rate Limits**
   - *Risk:* External service throttling
   - *Mitigation:* Caching, rate limiting, fallbacks

2. **Cost Overruns**
   - *Risk:* LLM API usage spikes
   - *Mitigation:* Usage monitoring, budget alerts

## Success Metrics

### Technical KPIs
- **Response Time:** <2 seconds for simple queries
- **Uptime:** 99.5% availability  
- **Cross-platform:** All 5 interfaces functional
- **Real-time Sync:** <500ms latency between devices

### User Experience KPIs  
- **Context Retention:** Personality consistency across interfaces
- **Voice Recognition:** >95% accuracy for voice commands
- **Integration Success:** All external services working
- **User Satisfaction:** Subjective evaluation by Ivan

## Next Steps

1. **Immediate (Week 1):**
   - Setup solution structure
   - Initialize .NET 8 projects  
   - Configure PostgreSQL database
   - Setup basic MCP integration

2. **Short-term (Month 1):**
   - Complete Phase 1 (Backend + MCP)
   - Begin Telegram bot development
   - Setup CI/CD pipeline
   - Choose hosting provider

3. **Medium-term (Month 2-3):**
   - Complete primary frontends
   - Add mobile applications  
   - Enhance integrations
   - Performance optimization

4. **Long-term (Month 4):**
   - Desktop applications
   - Production deployment
   - Monitoring & analytics
   - Feature enhancements

---
*Plan created: 2025-08-27*  
*Technology: All-.NET ecosystem*  
*Target: 16-week development cycle*