# Agent Controller Architecture 🤖

> **Parent Plan**: [03-02-01-controllers-implementation.md](../03-02-01-controllers-implementation.md) | **Plan Type**: CONTROLLER ARCHITECTURE | **LLM Ready**: ✅ YES  
> **Prerequisites**: IConversationService, IMcpService interfaces | **Execution Time**: 2-3 days

📍 **Architecture** → **Implementation** → **Controllers** → **Agent**

## AgentController Architecture Overview

### Core Responsibilities
- **Chat Management**: Handle chat requests with digital clone
- **Conversation Orchestration**: Coordinate personality, conversation, and MCP services
- **Response Generation**: Integrate MCP service for AI responses
- **History Management**: Retrieve conversation history
- **Context Management**: Maintain conversation context and personality traits

### Class Structure Design

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AgentController : ControllerBase
{
    private readonly IPersonalityService _personalityService;
    private readonly IConversationService _conversationService;
    private readonly IMcpService _mcpService;
    private readonly ILogger<AgentController> _logger;
    
    // Constructor with DI
    // Chat endpoint - HTTP POST
    // GetConversationHistory endpoint - HTTP GET
    // Private helper methods
}
```

### Endpoint Architecture

#### 1. POST /api/agent/chat
**Purpose**: Process chat message and generate AI response
**Architecture Balance**: 85% orchestration design, 15% implementation stub

```csharp
[HttpPost("chat")]
[ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
public async Task<IActionResult> Chat([FromBody] ChatRequest chatRequest)
{
    _logger.LogInformation("Processing chat request for profile {ProfileName}, conversation {ConversationId}", 
        chatRequest.ProfileName, chatRequest.ConversationId);

    // TODO: Validate request model
    // TODO: Get personality profile from _personalityService
    // TODO: Get/create conversation via _conversationService  
    // TODO: Add user message to conversation
    // TODO: Retrieve conversation history for context
    // TODO: Generate system prompt with personality traits
    // TODO: Call MCP service for AI response generation
    // TODO: Add bot response to conversation
    // TODO: Trigger async mood analysis
    // TODO: Return ChatResponse with all metadata

    throw new NotImplementedException("Chat endpoint orchestration implementation pending");
}
```

#### 2. GET /api/agent/conversations/{conversationId}/messages
**Purpose**: Retrieve conversation message history
**Architecture Balance**: 85% design patterns, 15% implementation stub

```csharp
[HttpGet("conversations/{conversationId}/messages")]
[ProducesResponseType(typeof(ConversationHistoryResponse), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetConversationHistory(
    [Required] Guid conversationId,
    [FromQuery] int limit = 20)
{
    _logger.LogInformation("Fetching conversation history for {ConversationId}, limit: {Limit}", 
        conversationId, limit);

    // TODO: Validate conversation exists
    // TODO: Get conversation messages with pagination
    // TODO: Map messages to DTOs
    // TODO: Return ConversationHistoryResponse

    throw new NotImplementedException("GetConversationHistory endpoint implementation pending");
}
```

### Service Orchestration Architecture

The AgentController acts as an orchestrator coordinating multiple services:

```csharp
// Service orchestration flow for Chat endpoint:
public async Task<IActionResult> Chat([FromBody] ChatRequest request)
{
    // 1. Personality Service - Get profile and traits
    var profile = await _personalityService.GetProfileAsync(request.ProfileName);
    
    // 2. Conversation Service - Manage conversation state  
    var conversation = await _conversationService.GetOrCreateConversationAsync(/*params*/);
    var userMessage = await _conversationService.AddMessageAsync(/*params*/);
    var history = await _conversationService.GetRecentMessagesAsync(/*params*/);
    
    // 3. Personality Service - Generate system prompt with traits
    var systemPrompt = await _personalityService.GenerateSystemPromptAsync(/*params*/);
    
    // 4. MCP Service - Generate AI response
    var mcpResponse = await _mcpService.GenerateResponseAsync(new McpChatRequest
    {
        SystemPrompt = systemPrompt,
        Message = request.Message,
        ConversationHistory = history,
        PersonalityTraits = profile.CoreTraits
    });
    
    // 5. Conversation Service - Store bot response
    var botMessage = await _conversationService.AddMessageAsync(/*params*/);
    
    // 6. Background mood analysis (fire-and-forget)
    _ = Task.Run(async () => {
        // TODO: Implement async mood analysis
    });
    
    // TODO: Map to ChatResponse and return
    throw new NotImplementedException();
}
```

### Error Handling Architecture

```csharp
// Specific exception handling for AgentController:
catch (PersonalityNotFoundException ex)
{
    return NotFound(new ErrorResponse { 
        Message = $"Profile '{chatRequest.ProfileName}' not found", 
        ErrorCode = "PROFILE_NOT_FOUND" 
    });
}
catch (McpServiceException ex)  
{
    _logger.LogError(ex, "MCP service error: {Error}", ex.Message);
    return StatusCode(502, new ErrorResponse { 
        Message = "AI service temporarily unavailable", 
        ErrorCode = "MCP_SERVICE_ERROR" 
    });
}
catch (ConversationNotFoundException ex)
{
    return NotFound(new ErrorResponse {
        Message = "Conversation not found",
        ErrorCode = "CONVERSATION_NOT_FOUND"  
    });
}
```

### Dependency Injection Configuration

```csharp
// Required services in DI container:
services.AddScoped<IPersonalityService, PersonalityService>();
services.AddScoped<IConversationService, ConversationService>();
services.AddScoped<IMcpService, McpService>();
services.AddLogging();

// HTTP client for MCP service
services.AddHttpClient<IMcpService>();
```

### Request/Response Flow Architecture

```
ChatRequest → Validation → PersonalityService → ConversationService → McpService → ChatResponse
     ↓              ↓              ↓                  ↓              ↓         ↓
  ModelState → ProfileExists → GetConversation → GeneratePrompt → CallAI → StoreResponse
```

### Background Processing Architecture

```csharp
// Async mood analysis pattern:
_ = Task.Run(async () =>
{
    try
    {
        var mood = await _personalityService.AnalyzeMoodFromMessageAsync(request.Message);
        _logger.LogInformation("Detected mood {Mood} for conversation {ConversationId}", 
            mood, conversation.Id);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to analyze mood for conversation {ConversationId}", 
            conversation.Id);
    }
});
```

### Success Criteria

✅ **Service Orchestration**: Multiple services coordinated properly
✅ **Async Processing**: Background tasks for mood analysis
✅ **Error Handling**: Service-specific exception handling
✅ **Request Flow**: Clear request/response pipeline
✅ **Context Management**: Conversation history and personality integration
✅ **Performance**: Fire-and-forget operations for non-critical tasks

### Implementation Guidance

1. **Implement Constructor**: Set up all service dependencies
2. **Start with Chat Endpoint**: Focus on core orchestration logic
3. **Add Error Handling**: Implement comprehensive exception handling  
4. **Test Service Integration**: Verify each service call works correctly
5. **Add Background Processing**: Implement async mood analysis
6. **History Endpoint**: Add conversation history retrieval

---

## 🔗 NAVIGATION & DEPENDENCIES

### Prerequisites
- **IPersonalityService**: Must provide GetProfileAsync, GenerateSystemPromptAsync
- **IConversationService**: Must provide conversation and message management
- **IMcpService**: Must provide AI response generation
- **DTOs**: ChatRequest, ChatResponse, ConversationHistoryResponse models

### Next Steps
- **Implement**: Fill in NotImplementedException stubs
- **Service Integration**: Connect to actual service implementations
- **Performance Testing**: Test under load with concurrent requests

### Related Plans  
- **Parent**: [03-02-01-controllers-implementation.md](../03-02-01-controllers-implementation.md)
- **Sibling**: [03-02-01-01-personality-controller.md](03-02-01-01-personality-controller.md)
- **DTOs**: [03-02-01-03-dto-models.md](03-02-01-03-dto-models.md)

---

## 📊 PLAN METADATA

- **Type**: CONTROLLER ARCHITECTURE PLAN
- **LLM Ready**: ✅ YES  
- **Implementation Depth**: 85% architecture / 15% implementation stubs
- **Execution Time**: 2-3 days
- **Code Coverage**: ~350 lines architectural guidance
- **Balance Compliance**: ✅ ARCHITECTURAL FOCUS maintained

### 🎯 ORCHESTRATION FOCUS INDICATORS
- **✅ Service Coordination**: Multi-service orchestration patterns
- **✅ Async Processing**: Background task architecture
- **✅ Error Handling**: Service-specific exception management
- **✅ Implementation Stubs**: NotImplementedException placeholders  
- **✅ Context Management**: Conversation and personality integration
- **✅ Performance Design**: Fire-and-forget operations
- **✅ Request Pipeline**: Clear request/response flow architecture