# Chat Hub Diagnosis & Implementation

**Родительский план**: [../05-01-chat-functionality-fix-plan.md](../05-01-chat-functionality-fix-plan.md)

## Current Issue Analysis

### Problem Statement
Agent Behavior Engine не отвечает на сообения пользователей. SignalR соединение работает, сообщения доходят до сервера, но система не генерирует ответы.

### Working Components ✅
- **SignalR Connection**: Establishes successfully
- **Message Sending**: Frontend to server communication functional  
- **ChatRequestDto**: Serialization/deserialization working
- **ChatHub.SendMessage**: Method receives calls correctly
- **Authentication**: JWT authorization functional

### Broken Components 🔴
- **Agent Behavior Engine**: Not generating responses
- **Typing Indicators**: Not visible to users
- **Error Handling**: Missing for failed responses
- **Response Pipeline**: Complete breakdown in message processing

## Chat Hub Implementation Fix

### Current ChatHub Analysis
**File**: `src/DigitalMe.API/Hubs/ChatHub.cs`

**Issues Identified:**
1. **Missing Agent Service Integration**: ChatHub не подключен к AgentBehaviorService
2. **No Response Generation**: Отсутствует логика генерации ответов
3. **Missing Error Handling**: Нет обработки ошибок при failures
4. **No Typing Indicators**: Отсутствуют индикаторы печати

### Fixed ChatHub Implementation
```csharp
[Authorize]
public class ChatHub : Hub
{
    private readonly IAgentBehaviorService _agentService;
    private readonly IPersonalityService _personalityService;
    private readonly IConversationRepository _conversationRepo;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IAgentBehaviorService agentService,
        IPersonalityService personalityService,
        IConversationRepository conversationRepo,
        ILogger<ChatHub> logger)
    {
        _agentService = agentService;
        _personalityService = personalityService;
        _conversationRepo = conversationRepo;
        _logger = logger;
    }

    public async Task SendMessage(ChatRequestDto request)
    {
        try
        {
            _logger.LogInformation("Received message from user {UserId}: {Message}", 
                Context.UserIdentifier, request.Message);

            // Show typing indicator
            await Clients.Caller.SendAsync("TypingStarted", "Ivan");

            // Get or create conversation
            var conversation = await GetOrCreateConversationAsync(request.ConversationId);
            
            // Save user message
            var userMessage = await SaveUserMessageAsync(conversation.Id, request.Message);
            
            // Generate response using Agent Behavior Engine
            var response = await _agentService.GenerateResponseAsync(new AgentRequest
            {
                Message = request.Message,
                ConversationId = conversation.Id,
                UserId = Context.UserIdentifier,
                PersonalityProfileName = "Ivan"
            });

            // Save agent response
            var agentMessage = await SaveAgentMessageAsync(conversation.Id, response.Content);

            // Stop typing indicator
            await Clients.Caller.SendAsync("TypingStopped", "Ivan");

            // Send response to client
            await Clients.Caller.SendAsync("MessageReceived", new ChatResponseDto
            {
                Id = agentMessage.Id,
                Content = response.Content,
                SenderName = "Ivan",
                SentAt = agentMessage.CreatedAt,
                ConversationId = conversation.Id,
                DetectedMood = response.DetectedMood,
                ConfidenceScore = response.ConfidenceScore
            });

            _logger.LogInformation("Successfully sent response to user {UserId}", Context.UserIdentifier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from user {UserId}: {Message}", 
                Context.UserIdentifier, request.Message);

            // Stop typing indicator on error
            await Clients.Caller.SendAsync("TypingStopped", "Ivan");
            
            // Send error message to client
            await Clients.Caller.SendAsync("ErrorOccurred", new
            {
                Error = "Sorry, I'm having trouble processing your message right now. Please try again.",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public async Task JoinConversation(Guid conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
        _logger.LogInformation("User {UserId} joined conversation {ConversationId}", 
            Context.UserIdentifier, conversationId);
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
        _logger.LogInformation("User {UserId} left conversation {ConversationId}", 
            Context.UserIdentifier, conversationId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("User {UserId} connected to chat hub", Context.UserIdentifier);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        _logger.LogInformation("User {UserId} disconnected from chat hub. Exception: {Exception}", 
            Context.UserIdentifier, exception?.Message);
        await base.OnDisconnectedAsync(exception);
    }

    private async Task<Conversation> GetOrCreateConversationAsync(Guid? conversationId)
    {
        if (conversationId.HasValue)
        {
            var existing = await _conversationRepo.GetByIdAsync(conversationId.Value);
            if (existing != null) return existing;
        }

        // Create new conversation
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            PersonalityProfileId = await GetIvanProfileIdAsync(),
            Platform = "Web",
            Title = "Chat Session",
            StartedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _conversationRepo.CreateAsync(conversation);
        return conversation;
    }

    private async Task<Message> SaveUserMessageAsync(Guid conversationId, string content)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Content = content,
            Type = MessageType.User,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        // Save to database (implement repository)
        // await _messageRepo.CreateAsync(message);
        return message;
    }

    private async Task<Message> SaveAgentMessageAsync(Guid conversationId, string content)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Content = content,
            Type = MessageType.Assistant,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        // Save to database (implement repository)
        // await _messageRepo.CreateAsync(message);
        return message;
    }

    private async Task<Guid> GetIvanProfileIdAsync()
    {
        // Get Ivan's personality profile ID
        // This should be implemented in PersonalityService
        return Guid.NewGuid(); // Placeholder
    }
}
```

## Required Service Dependencies

### IAgentBehaviorService Interface
**File**: `src/DigitalMe.Core/Interfaces/IAgentBehaviorService.cs`

```csharp
public interface IAgentBehaviorService
{
    Task<AgentResponse> GenerateResponseAsync(AgentRequest request);
    Task<PersonalityMood> AnalyzeMoodAsync(string message);
    Task UpdatePersonalityStateAsync(string profileName, ConversationContext context);
}

public class AgentRequest
{
    public string Message { get; set; }
    public Guid ConversationId { get; set; }
    public string UserId { get; set; }
    public string PersonalityProfileName { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

public class AgentResponse
{
    public string Content { get; set; }
    public PersonalityMood DetectedMood { get; set; }
    public double ConfidenceScore { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

### DTOs for Chat Communication
**File**: `src/DigitalMe.Shared/DTOs/ChatDTOs.cs`

```csharp
public class ChatRequestDto
{
    public string Message { get; set; }
    public Guid? ConversationId { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

public class ChatResponseDto
{
    public Guid Id { get; set; }
    public string Content { get; set; }
    public string SenderName { get; set; }
    public DateTime SentAt { get; set; }
    public Guid ConversationId { get; set; }
    public PersonalityMood DetectedMood { get; set; }
    public double ConfidenceScore { get; set; }
}
```

## Integration Requirements

### Service Registration
**File**: `src/DigitalMe.API/Program.cs`

```csharp
// Add SignalR
services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Add required services
services.AddScoped<IAgentBehaviorService, AgentBehaviorService>();
services.AddScoped<IPersonalityService, PersonalityService>();
services.AddScoped<IConversationRepository, ConversationRepository>();

// Configure hub
app.MapHub<ChatHub>("/chatHub");
```

## Testing Implementation

### ChatHub Unit Tests
**File**: `tests/DigitalMe.Tests.Unit/Hubs/ChatHubTests.cs`

```csharp
public class ChatHubTests
{
    private readonly Mock<IAgentBehaviorService> _mockAgentService;
    private readonly Mock<IPersonalityService> _mockPersonalityService;
    private readonly Mock<IConversationRepository> _mockConversationRepo;
    private readonly Mock<ILogger<ChatHub>> _mockLogger;
    private readonly ChatHub _chatHub;

    public ChatHubTests()
    {
        _mockAgentService = new Mock<IAgentBehaviorService>();
        _mockPersonalityService = new Mock<IPersonalityService>();
        _mockConversationRepo = new Mock<IConversationRepository>();
        _mockLogger = new Mock<ILogger<ChatHub>>();

        _chatHub = new ChatHub(
            _mockAgentService.Object,
            _mockPersonalityService.Object,
            _mockConversationRepo.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SendMessage_Should_Generate_And_Send_Response()
    {
        // Arrange
        var request = new ChatRequestDto { Message = "Hello Ivan" };
        var expectedResponse = new AgentResponse 
        { 
            Content = "Hello! How can I help you?",
            DetectedMood = PersonalityMood.Calm,
            ConfidenceScore = 0.95
        };

        _mockAgentService.Setup(x => x.GenerateResponseAsync(It.IsAny<AgentRequest>()))
                       .ReturnsAsync(expectedResponse);

        // Act & Assert  
        await _chatHub.SendMessage(request);
        
        // Verify agent service was called
        _mockAgentService.Verify(x => x.GenerateResponseAsync(It.IsAny<AgentRequest>()), Times.Once);
    }

    [Fact]
    public async Task SendMessage_Should_Handle_Errors_Gracefully()
    {
        // Arrange
        var request = new ChatRequestDto { Message = "Test message" };
        
        _mockAgentService.Setup(x => x.GenerateResponseAsync(It.IsAny<AgentRequest>()))
                       .ThrowsAsync(new Exception("Service error"));

        // Act & Assert
        await _chatHub.SendMessage(request);
        
        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
```

## Success Criteria
- [ ] **ChatHub Integration**: Agent service properly integrated
- [ ] **Response Generation**: Messages generate Ivan responses  
- [ ] **Typing Indicators**: Visual feedback during processing
- [ ] **Error Handling**: Graceful failure handling and user notification
- [ ] **Message Persistence**: All messages saved to database
- [ ] **Unit Tests**: Comprehensive test coverage >90%
- [ ] **Integration Tests**: End-to-end chat flow tested
- [ ] **Performance**: Response time <2s for typical messages

## Next Steps
- [Agent Behavior Engine Implementation](./05-01-02-agent-behavior-engine.md)
- [Message Processing Pipeline](./05-01-03-message-pipeline.md)
- [End-to-End Testing Strategy](./05-01-04-testing-strategy.md)

## Navigation
- **Parent**: [Chat Functionality Fix Plan](../05-01-chat-functionality-fix-plan.md)
- **Next**: [Agent Behavior Engine](./05-01-02-agent-behavior-engine.md)