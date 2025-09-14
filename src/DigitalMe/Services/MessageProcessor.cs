using DigitalMe.Common.Exceptions;
using DigitalMe.DTOs;
using DigitalMe.Models;
using DigitalMe.Services.AgentBehavior;

namespace DigitalMe.Services;

public class MessageProcessor : IMessageProcessor
{
    private readonly IConversationService _conversationService;
    private readonly IPersonalityService _personalityService;
    private readonly IAgentBehaviorEngine _agentBehaviorEngine;
    private readonly ILogger<MessageProcessor> _logger;

    public MessageProcessor(
        IConversationService conversationService,
        IPersonalityService personalityService,
        IAgentBehaviorEngine agentBehaviorEngine,
        ILogger<MessageProcessor> logger)
    {
        _conversationService = conversationService;
        _personalityService = personalityService;
        _agentBehaviorEngine = agentBehaviorEngine;
        _logger = logger;
    }

    public async Task<ProcessMessageResult> ProcessUserMessageAsync(ChatRequestDto request)
    {
        try
        {
            _logger.LogInformation("📝 Processing user message for UserId: {UserId}, Platform: {Platform}",
                request.UserId, request.Platform);

            // Get or create conversation
            var conversation = await _conversationService.StartConversationAsync(
                request.Platform,
                request.UserId,
                "Real-time Chat");

            _logger.LogInformation("✅ Conversation ID {ConversationId} created/found", conversation.Id);

            // Add user message
            var userMessage = await _conversationService.AddMessageAsync(
                conversation.Id,
                "user",
                request.Message);

            _logger.LogInformation("✅ User message {MessageId} added", userMessage.Id);

            var groupName = $"chat_{request.UserId}";

            return new ProcessMessageResult(conversation, userMessage, groupName);
        }
        catch (Exception ex) when (!(ex is DigitalMeException))
        {
            _logger.LogError(ex, "💥 Failed to process user message for UserId: {UserId}", request.UserId);
            throw new MessageProcessingException("Failed to process user message", ex,
                new { userId = request.UserId, platform = request.Platform, messageLength = request.Message.Length });
        }
    }

    public async Task<ProcessAgentResponseResult> ProcessAgentResponseAsync(ChatRequestDto request, Guid conversationId)
    {
        try
        {
            _logger.LogInformation("🧠 Processing agent response for ConversationId: {ConversationId}", conversationId);

            // Get Ivan's personality
            var personality = await _personalityService.GetPersonalityAsync("Ivan");
            if (personality == null)
            {
                _logger.LogError("❌ Ivan's personality profile not found!");
                throw new PersonalityServiceException("Ivan's personality profile not found. Please create it first.",
                    new { userId = request.UserId, platform = request.Platform });
            }

            _logger.LogInformation("✅ Personality profile loaded - {ProfileName}", personality.Name);

            // Create personality context
            var recentMessages = await _conversationService.GetConversationHistoryAsync(conversationId, 10);
            var personalityContext = new PersonalityContext
            {
                Profile = personality,
                RecentMessages = recentMessages,
                CurrentState = new Dictionary<string, object>
                {
                    ["platform"] = request.Platform,
                    ["userId"] = request.UserId,
                    ["conversationId"] = conversationId.ToString(),
                    ["isRealTime"] = true,
                    ["backgroundProcessing"] = true
                }
            };

            // Process through Agent Behavior Engine
            _logger.LogInformation("🤖 Processing message through Agent Behavior Engine");
            var agentResponse = await _agentBehaviorEngine.ProcessMessageAsync(request.Message, personalityContext);

            _logger.LogInformation("✅ Agent response generated - Length: {ContentLength}, Mood: {Mood}",
                agentResponse.Content.Length, agentResponse.Mood.PrimaryMood);

            // Save assistant response
            var assistantMessage = await _conversationService.AddMessageAsync(
                conversationId,
                "assistant",
                agentResponse.Content,
                agentResponse.Metadata);

            _logger.LogInformation("✅ Assistant message {MessageId} saved", assistantMessage.Id);

            return new ProcessAgentResponseResult(assistantMessage, agentResponse);
        }
        catch (PersonalityServiceException)
        {
            throw; // Re-throw domain-specific exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Failed to process agent response for ConversationId: {ConversationId}", conversationId);
            throw new AgentBehaviorException("Failed to process agent response", ex,
                new { conversationId, userId = request.UserId, platform = request.Platform });
        }
    }
}
