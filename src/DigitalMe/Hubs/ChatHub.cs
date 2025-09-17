using DigitalMe.DTOs;
using DigitalMe.Models;
using DigitalMe.Services;
using DigitalMe.Services.AgentBehavior;
using Microsoft.AspNetCore.SignalR;

namespace DigitalMe.Hubs;

public class ChatHub : Hub
{
    private readonly IMessageProcessor _messageProcessor;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IMessageProcessor messageProcessor,
        ILogger<ChatHub> logger)
    {
        _messageProcessor = messageProcessor;
        _logger = logger;
    }

    public async Task JoinChat(string userId, string platform = "Web")
    {
        var groupName = $"chat_{userId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation("👋 User {UserId} joined chat from {Platform} (Connection: {ConnectionId})",
            userId, platform, Context.ConnectionId);

        await Clients.Caller.SendAsync("JoinedChat", new
        {
            UserId = userId,
            Platform = platform,
            ConnectionId = Context.ConnectionId,
            Status = "Connected"
        });
    }

    // TEST METHOD - Remove after debugging
    public async Task TestMessage(string message)
    {
        _logger.LogInformation("🧪 TEST MESSAGE RECEIVED: '{TestMessage}' from connection {ConnectionId}",
            message, Context.ConnectionId);

        await Clients.Caller.SendAsync("TestResponse", new
        {
            Message = $"Test received: {message}",
            Timestamp = DateTime.UtcNow,
            ConnectionId = Context.ConnectionId
        });
    }

    public async Task SendMessage(ChatRequestDto request)
    {
        try
        {
            _logger.LogInformation("🚀 ChatHub.SendMessage STARTED - UserId: {UserId}, Platform: {Platform}, Message: '{Message}'",
                request.UserId, request.Platform, request.Message);

            // Process user message through MessageProcessor
            var result = await _messageProcessor.ProcessUserMessageAsync(request);

            if (!result.IsSuccess)
            {
                _logger.LogError("❌ Failed to process user message: {Error}", result.Error);
                await Clients.Caller.SendAsync("Error", new
                {
                    code = "PROCESSING_ERROR",
                    message = "Произошла ошибка при обработке сообщения. Попробуйте снова."
                });
                return;
            }

            var processResult = result.Value;

            _logger.LogInformation("📡 STEP 3: Notifying group {GroupName} about user message",
                processResult.GroupName);

            await Clients.Group(processResult.GroupName).SendAsync("MessageReceived", new MessageDto
            {
                Id = processResult.UserMessage.Id,
                ConversationId = processResult.UserMessage.ConversationId,
                Role = processResult.UserMessage.Role,
                Content = processResult.UserMessage.Content,
                Timestamp = processResult.UserMessage.Timestamp,
                Metadata = new Dictionary<string, object>
                {
                    ["isRealTime"] = true,
                    ["connectionId"] = Context.ConnectionId
                }
            });

            // Show typing indicator
            _logger.LogInformation("⏳ STEP 4: Showing typing indicator for group {GroupName}",
                processResult.GroupName);
            await Clients.Group(processResult.GroupName).SendAsync("TypingIndicator", new
            {
                IsTyping = true,
                User = "Ivan",
                Message = "Иван печатает..."
            });

            // Process agent response synchronously for integration tests reliability
            await ProcessAgentResponseAsync(request, processResult.Conversation.Id, processResult.GroupName);

            _logger.LogInformation("🎉 ChatHub.SendMessage COMPLETED (background processing started) for user {UserId}",
                request.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 ChatHub.SendMessage FAILED for user {UserId}: {ErrorMessage}",
                request.UserId, ex.Message);

            await Clients.Caller.SendAsync("Error", new
            {
                code = "PROCESSING_ERROR",
                message = "Произошла ошибка при обработке сообщения. Попробуйте снова."
            });
        }
    }

    private async Task ProcessAgentResponseAsync(ChatRequestDto request, Guid conversationId, string groupName)
    {
        try
        {
            // Process agent response through MessageProcessor
            var result = await _messageProcessor.ProcessAgentResponseAsync(request, conversationId);

            if (!result.IsSuccess)
            {
                _logger.LogError("❌ Failed to process agent response: {Error}", result.Error);
                await Clients.Group(groupName).SendAsync("Error", new
                {
                    code = "PROCESSING_ERROR",
                    message = "Произошла ошибка при обработке сообщения. Попробуйте снова."
                });
                return;
            }

            var agentResult = result.Value;

            // Hide typing indicator
            _logger.LogInformation("⏹️ STEP 7: Hiding typing indicator");
            await Clients.Group(groupName).SendAsync("TypingIndicator", new
            {
                IsTyping = false,
                User = "Ivan"
            });

            // Send agent response to all clients in group
            _logger.LogInformation("📡 STEP 9: Sending agent response to group {GroupName}",
                groupName);
            await Clients.Group(groupName).SendAsync("MessageReceived", new MessageDto
            {
                Id = agentResult.AssistantMessage.Id,
                ConversationId = agentResult.AssistantMessage.ConversationId,
                Role = agentResult.AssistantMessage.Role,
                Content = agentResult.AssistantMessage.Content,
                Timestamp = agentResult.AssistantMessage.Timestamp,
                Metadata = new Dictionary<string, object>
                {
                    ["mood"] = agentResult.AgentResponse.Mood.PrimaryMood,
                    ["moodIntensity"] = agentResult.AgentResponse.Mood.Intensity,
                    ["confidence"] = agentResult.AgentResponse.ConfidenceScore,
                    ["triggeredTools"] = agentResult.AgentResponse.TriggeredTools,
                    ["isRealTime"] = true,
                    ["backgroundProcessed"] = true
                }
            });

            _logger.LogInformation("🎉 Background processing COMPLETED SUCCESSFULLY for user {UserId}",
                request.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Background processing FAILED for user {UserId}: {ErrorMessage}",
                request.UserId, ex.Message);

            // Hide typing indicator on error
            await Clients.Group(groupName).SendAsync("TypingIndicator", new
            {
                IsTyping = false,
                User = "Ivan"
            });

            await Clients.Group(groupName).SendAsync("Error", new
            {
                code = "PROCESSING_ERROR",
                message = "Произошла ошибка при обработке сообщения. Попробуйте снова."
            });
        }
    }

    public async Task LeaveChat(string userId)
    {
        var groupName = $"chat_{userId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation("User {UserId} left chat (Connection: {ConnectionId})",
            userId, Context.ConnectionId);

        await Clients.Group(groupName).SendAsync("UserLeft", new
        {
            UserId = userId,
            ConnectionId = Context.ConnectionId,
            Timestamp = DateTime.UtcNow
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Connection {ConnectionId} disconnected. Exception: {Exception}",
            Context.ConnectionId, exception?.Message);

        await base.OnDisconnectedAsync(exception);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("New connection established: {ConnectionId} from {UserAgent}",
            Context.ConnectionId, Context.GetHttpContext()?.Request.Headers["User-Agent"]);

        await Clients.Caller.SendAsync("Connected", new
        {
            ConnectionId = Context.ConnectionId,
            ServerTime = DateTime.UtcNow,
            Status = "Ready"
        });

        await base.OnConnectedAsync();
    }
}
