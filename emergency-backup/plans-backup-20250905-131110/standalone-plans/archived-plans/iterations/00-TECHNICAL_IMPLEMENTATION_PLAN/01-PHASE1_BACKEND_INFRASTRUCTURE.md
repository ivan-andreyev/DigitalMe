# Phase 1: Core Backend Infrastructure (Weeks 1-4)

**Objective:** Build the central orchestration platform with basic integrations  
**Duration:** 4 weeks  
**Dependencies:** None (Starting phase)  
**Team:** 1x Senior .NET Developer

## Overview

This phase establishes the foundational backend architecture for the DigitalMe platform. The focus is on creating a robust, scalable .NET 8 API with MCP integration and basic external service connections.

## 1.1 Backend API Foundation (Week 1-2)

### Tasks

**Week 1:**
- [ ] Set up .NET 8 Web API project structure
- [ ] Configure dependency injection container with all required services
- [ ] Implement base controller architecture with standard error handling
- [ ] Set up Serilog logging with structured logging
- [ ] Configure CORS policies for frontend access
- [ ] Implement health checks endpoint for monitoring

**Week 2:**
- [ ] Create authentication middleware structure
- [ ] Set up API versioning (v1 baseline)
- [ ] Implement rate limiting middleware
- [ ] Configure Swagger/OpenAPI documentation
- [ ] Set up unit test project structure
- [ ] Create Docker containerization setup

### Technical Specifications

**Project Structure:**
```
DigitalMe.Api/
├── Controllers/
│   ├── AuthController.cs
│   ├── UsersController.cs
│   ├── TasksController.cs
│   └── CalendarController.cs
├── Services/
│   ├── IPersonalAgentService.cs
│   ├── PersonalAgentService.cs
│   └── Interfaces/
├── Models/
│   ├── Requests/
│   ├── Responses/
│   └── DTOs/
├── Infrastructure/
│   ├── Middleware/
│   ├── Extensions/
│   └── Configurations/
└── Program.cs
```

**Core Dependencies:**
```xml
<PackageReference Include="Microsoft.AspNetCore.App" Version="8.0.0" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
<PackageReference Include="AspNetCoreRateLimit" Version="5.0.0" />
```

### Deliverables
- [ ] Running ASP.NET Core API with health endpoints
- [ ] Structured project architecture following Clean Architecture principles
- [ ] Basic authentication framework ready for JWT implementation
- [ ] Dockerized application ready for container deployment
- [ ] Comprehensive API documentation via Swagger

### Acceptance Criteria
- API responds to health check requests with 200 status
- All endpoints return proper HTTP status codes
- Logging captures all requests and responses
- Docker container builds and runs successfully
- Swagger UI accessible and documents all endpoints

## 1.2 Database Layer Implementation (Week 2)

### Tasks

**Database Design:**
- [ ] Design Entity Framework Core models for all domain entities
- [ ] Create database migration scripts with proper indexing
- [ ] Set up Supabase PostgreSQL instance and connection
- [ ] Implement repository pattern with generic base repository
- [ ] Configure connection pooling and retry policies
- [ ] Set up database seeding with initial data

**Data Models:**
- [ ] User entity with authentication properties
- [ ] AgentSession for conversation tracking
- [ ] Task entity with status and priority management
- [ ] UserIntegration for external service connections
- [ ] CalendarEvent for cached calendar data
- [ ] ConversationHistory for chat logging

### Database Schema

**Core Entities:**
```sql
-- Users table
CREATE TABLE Users (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TelegramUserId BIGINT UNIQUE,
    Email VARCHAR(255),
    Username VARCHAR(100),
    TimeZone VARCHAR(50) DEFAULT 'UTC',
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Agent Sessions
CREATE TABLE AgentSessions (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    UserId UUID REFERENCES Users(Id) ON DELETE CASCADE,
    SessionType VARCHAR(50) NOT NULL,
    ContextData JSONB,
    Status VARCHAR(50) DEFAULT 'active',
    StartedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    EndedAt TIMESTAMP WITH TIME ZONE
);

-- Tasks Management
CREATE TABLE Tasks (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    UserId UUID REFERENCES Users(Id) ON DELETE CASCADE,
    Title VARCHAR(500) NOT NULL,
    Description TEXT,
    Status VARCHAR(50) DEFAULT 'pending',
    Priority VARCHAR(20) DEFAULT 'medium',
    DueDate TIMESTAMP WITH TIME ZONE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- External Service Integrations
CREATE TABLE UserIntegrations (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    UserId UUID REFERENCES Users(Id) ON DELETE CASCADE,
    ServiceType VARCHAR(50) NOT NULL,
    AccessToken TEXT NOT NULL,
    RefreshToken TEXT,
    ExpiresAt TIMESTAMP WITH TIME ZONE,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Performance Indexes
CREATE INDEX idx_users_telegram_id ON Users(TelegramUserId);
CREATE INDEX idx_tasks_user_status ON Tasks(UserId, Status);
CREATE INDEX idx_sessions_user_active ON AgentSessions(UserId, Status);
CREATE INDEX idx_integrations_user_service ON UserIntegrations(UserId, ServiceType);
```

### Technical Implementation

**Entity Framework Configuration:**
```csharp
public class DigitalMeDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<AgentSession> AgentSessions { get; set; }
    public DbSet<UserTask> Tasks { get; set; }
    public DbSet<UserIntegration> UserIntegrations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TelegramUserId).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255);
        });
        
        // Additional entity configurations...
    }
}
```

### Deliverables
- [ ] Complete EF Core data models for all entities
- [ ] Database migration scripts ready for deployment
- [ ] Supabase PostgreSQL connection established and tested
- [ ] Repository pattern implementation with unit of work
- [ ] Database seeding functionality for development/testing

### Acceptance Criteria
- All database migrations run successfully
- EF Core models map correctly to database schema
- Repository methods perform CRUD operations correctly
- Connection pooling configured and functioning
- Database indexes improve query performance measurably

## 1.3 MCP Server Implementation (Week 3-4)

### Tasks

**MCP Protocol Setup:**
- [ ] Install and configure ModelContextProtocol NuGet package
- [ ] Implement MCP server class with base functionality
- [ ] Create core tools for calendar and task management
- [ ] Set up resource providers for user context
- [ ] Implement MCP authentication and security
- [ ] Configure MCP server for Claude Code integration

**Core MCP Tools:**
- [ ] `get_user_calendar` - Retrieve calendar events
- [ ] `create_calendar_event` - Create new calendar events
- [ ] `manage_tasks` - Task CRUD operations
- [ ] `send_notification` - Cross-platform notifications
- [ ] `get_user_context` - Current user state

### Technical Implementation

**MCP Server Class:**
```csharp
[MCPServer("digitalmee-agent")]
public class DigitalMeMCPServer : IMCPServer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DigitalMeMCPServer> _logger;

    public DigitalMeMCPServer(
        IServiceProvider serviceProvider,
        ILogger<DigitalMeMCPServer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [MCPTool("get_user_calendar")]
    [Description("Get calendar events for a user within a date range")]
    public async Task<CalendarEvent[]> GetUserCalendarAsync(
        [Description("Start date (YYYY-MM-DD)")] DateTime startDate,
        [Description("End date (YYYY-MM-DD)")] DateTime endDate,
        [Description("User ID")] string? userId = null)
    {
        var calendarService = _serviceProvider.GetRequiredService<ICalendarService>();
        return await calendarService.GetEventsAsync(startDate, endDate, userId);
    }

    [MCPTool("create_calendar_event")]
    [Description("Create a new calendar event")]
    public async Task<CalendarEvent> CreateCalendarEventAsync(
        [Description("Event title")] string title,
        [Description("Start date and time")] DateTime startTime,
        [Description("End date and time")] DateTime endTime,
        [Description("Event description")] string? description = null,
        [Description("Event location")] string? location = null)
    {
        var calendarService = _serviceProvider.GetRequiredService<ICalendarService>();
        var eventData = new CreateCalendarEventRequest
        {
            Title = title,
            StartTime = startTime,
            EndTime = endTime,
            Description = description,
            Location = location
        };
        
        return await calendarService.CreateEventAsync(eventData);
    }

    [MCPTool("manage_tasks")]
    [Description("Manage user tasks (create, update, delete, list)")]
    public async Task<TaskOperationResult> ManageTasksAsync(
        [Description("Operation type: create, update, delete, list")] string operation,
        [Description("Task data as JSON")] string taskData)
    {
        var taskService = _serviceProvider.GetRequiredService<ITaskService>();
        
        return operation.ToLower() switch
        {
            "create" => await taskService.CreateTaskFromJsonAsync(taskData),
            "update" => await taskService.UpdateTaskFromJsonAsync(taskData),
            "delete" => await taskService.DeleteTaskAsync(taskData),
            "list" => await taskService.GetTasksAsync(taskData),
            _ => throw new ArgumentException($"Unknown operation: {operation}")
        };
    }

    [MCPResource("user_context")]
    [Description("Current user context and preferences")]
    public async Task<UserContextResource> GetUserContextAsync(string userId)
    {
        var userService = _serviceProvider.GetRequiredService<IUserService>();
        var user = await userService.GetUserByIdAsync(userId);
        
        return new UserContextResource
        {
            UserId = user.Id,
            TimeZone = user.TimeZone,
            Preferences = await userService.GetUserPreferencesAsync(userId),
            ActiveIntegrations = await userService.GetActiveIntegrationsAsync(userId)
        };
    }
}
```

**MCP Configuration:**
```csharp
// In Program.cs
builder.Services.AddMCPServer<DigitalMeMCPServer>(options =>
{
    options.ServerName = "DigitalMe Agent";
    options.ServerVersion = "1.0.0";
    options.EnableAuthentication = true;
    options.MaxConcurrentConnections = 100;
});
```

### Deliverables
- [ ] Fully functional MCP server with all core tools
- [ ] MCP server integrated with main API application
- [ ] Claude Code integration tested and working
- [ ] MCP tools authenticated and secured
- [ ] Comprehensive MCP tool documentation

### Acceptance Criteria
- MCP server responds to Claude Code connections
- All MCP tools execute successfully and return expected data
- MCP authentication prevents unauthorized access
- MCP tools integrate properly with backend services
- Performance meets < 1 second response time requirement

## 1.4 Semantic Kernel Integration (Week 3-4)

### Tasks

**Semantic Kernel Setup:**
- [ ] Install Microsoft.SemanticKernel NuGet packages
- [ ] Configure multiple LLM providers (OpenAI, Anthropic)
- [ ] Implement plugin architecture for extensibility
- [ ] Create calendar and task management plugins
- [ ] Set up conversation memory management
- [ ] Configure model fallback strategies

**Plugin Implementation:**
- [ ] CalendarPlugin for calendar operations
- [ ] TaskPlugin for task management
- [ ] NotificationPlugin for sending alerts
- [ ] UserContextPlugin for user information

### Technical Implementation

**Semantic Kernel Configuration:**
```csharp
public static class SemanticKernelExtensions
{
    public static IServiceCollection AddSemanticKernel(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<Kernel>(serviceProvider =>
        {
            var builder = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                    "gpt-4o",
                    configuration["OpenAI:ApiKey"]!)
                .AddAnthropicChatCompletion(
                    "claude-3-5-sonnet-20241022",
                    configuration["Anthropic:ApiKey"]!);

            // Add plugins
            builder.Plugins.AddFromType<CalendarPlugin>();
            builder.Plugins.AddFromType<TaskPlugin>();
            builder.Plugins.AddFromType<NotificationPlugin>();

            return builder.Build();
        });

        services.AddScoped<IPersonalAgentService, PersonalAgentService>();
        return services;
    }
}
```

**Calendar Plugin Example:**
```csharp
public class CalendarPlugin
{
    private readonly ICalendarService _calendarService;

    public CalendarPlugin(ICalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    [KernelFunction]
    [Description("Get calendar events for a date range")]
    public async Task<string> GetCalendarEventsAsync(
        [Description("Start date (YYYY-MM-DD)")] string startDate,
        [Description("End date (YYYY-MM-DD)")] string endDate)
    {
        var start = DateTime.Parse(startDate);
        var end = DateTime.Parse(endDate);
        
        var events = await _calendarService.GetEventsAsync(start, end);
        
        if (!events.Any())
            return $"No events found between {startDate} and {endDate}.";
        
        var result = $"Events from {startDate} to {endDate}:\n";
        foreach (var evt in events.OrderBy(e => e.StartTime))
        {
            result += $"• {evt.StartTime:MM/dd HH:mm} - {evt.Title}\n";
            if (!string.IsNullOrEmpty(evt.Location))
                result += $"  📍 {evt.Location}\n";
        }
        
        return result;
    }

    [KernelFunction]
    [Description("Create a new calendar event")]
    public async Task<string> CreateCalendarEventAsync(
        [Description("Event title")] string title,
        [Description("Start date and time (YYYY-MM-DD HH:mm)")] string startDateTime,
        [Description("Duration in minutes")] int durationMinutes,
        [Description("Event description")] string? description = null,
        [Description("Event location")] string? location = null)
    {
        try
        {
            var start = DateTime.Parse(startDateTime);
            var end = start.AddMinutes(durationMinutes);

            var eventRequest = new CreateCalendarEventRequest
            {
                Title = title,
                StartTime = start,
                EndTime = end,
                Description = description,
                Location = location
            };

            var createdEvent = await _calendarService.CreateEventAsync(eventRequest);
            return $"✅ Event '{title}' created for {start:MM/dd/yyyy HH:mm}";
        }
        catch (Exception ex)
        {
            return $"❌ Error creating event: {ex.Message}";
        }
    }
}
```

### Deliverables
- [ ] Semantic Kernel configured with multiple LLM providers
- [ ] Plugin architecture implemented and extensible
- [ ] Core plugins (Calendar, Tasks, Notifications) functional
- [ ] Conversation memory system working
- [ ] Model fallback mechanism tested

### Acceptance Criteria
- Semantic Kernel successfully processes natural language requests
- Plugins execute correctly and return appropriate responses
- Multiple LLM providers work with automatic fallback
- Conversation context maintains throughout sessions
- Plugin system allows easy addition of new functionality

## Risk Assessment & Mitigation

### Technical Risks

**Risk: MCP SDK Stability (Preview Version)**
- **Probability:** Medium
- **Impact:** High
- **Mitigation:** Use stable version, create abstraction layer
- **Contingency:** Direct HTTP MCP implementation

**Risk: Entity Framework Migration Issues**
- **Probability:** Low
- **Impact:** Medium
- **Mitigation:** Thorough testing in development environment
- **Contingency:** Manual SQL script fallback

**Risk: Semantic Kernel Performance**
- **Probability:** Medium
- **Impact:** Medium
- **Mitigation:** Implement caching, optimize prompts
- **Contingency:** Reduce plugin complexity

### Timeline Risks

**Risk: Development Complexity Underestimation**
- **Probability:** Medium
- **Impact:** High
- **Mitigation:** Daily standups, weekly milestone reviews
- **Contingency:** Scope reduction, phase extension

## Phase 1 Success Criteria

### Technical Validation
- [ ] Backend API responds to all core endpoints
- [ ] Database operations (CRUD) work for all entities
- [ ] MCP server accepts connections from Claude Code
- [ ] Semantic Kernel processes requests with plugins
- [ ] All unit tests pass (>90% coverage)

### Integration Validation
- [ ] MCP tools return expected data formats
- [ ] Semantic Kernel plugins integrate with backend services
- [ ] Database connections are stable under load
- [ ] API authentication works correctly
- [ ] Logging captures all important events

### Performance Validation
- [ ] API response times < 500ms (95th percentile)
- [ ] Database queries < 100ms average
- [ ] MCP tool responses < 1 second
- [ ] Memory usage stays within acceptable limits
- [ ] No memory leaks detected in long-running tests

## Next Phase Preparation

### Phase 2 Prerequisites
- [ ] All Phase 1 deliverables completed and tested
- [ ] Google Cloud Console project set up with required APIs
- [ ] Telegram Bot created and token obtained
- [ ] GitHub personal access token configured
- [ ] Development environment ready for frontend work

### Handoff Documentation
- [ ] Complete API documentation in Swagger
- [ ] Database schema documentation
- [ ] MCP tools usage guide
- [ ] Deployment instructions for development environment
- [ ] Known issues and troubleshooting guide