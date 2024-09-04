using Microsoft.AspNetCore.Mvc;
using DigitalMe.Services;
using DigitalMe.Services.AgentBehavior;
using DigitalMe.DTOs;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly IPersonalityService _personalityService;
    private readonly IMcpService _mcpService;
    private readonly IAgentBehaviorEngine _agentBehaviorEngine;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IConversationService conversationService,
        IPersonalityService personalityService,
        IMcpService mcpService,
        IAgentBehaviorEngine agentBehaviorEngine,
        ILogger<ChatController> logger)
    {
        _conversationService = conversationService;
        _personalityService = personalityService;
        _mcpService = mcpService;
        _agentBehaviorEngine = agentBehaviorEngine;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<ActionResult<MessageDto>> SendMessage([FromBody] ChatRequestDto request)
    {
        try
        {
            // Ensure MCP service is connected
            if (!await _mcpService.IsConnectedAsync())
            {
                await _mcpService.InitializeAsync();
            }

            // Get or create conversation
            var conversation = await _conversationService.StartConversationAsync(
                request.Platform, 
                request.UserId, 
                "Chat Session");

            // Add user message
            var userMessage = await _conversationService.AddMessageAsync(
                conversation.Id, 
                "user", 
                request.Message);

            // Get Ivan's personality for system prompt
            var personality = await _personalityService.GetPersonalityAsync("Ivan");
            if (personality == null)
            {
                return BadRequest("Ivan's personality profile not found. Please create it first.");
            }

            // Create personality context
            var recentMessages = await _conversationService.GetConversationHistoryAsync(conversation.Id, 10);
            var personalityContext = new DigitalMe.Models.PersonalityContext
            {
                Profile = personality,
                RecentMessages = recentMessages,
                CurrentState = new Dictionary<string, object>
                {
                    ["platform"] = request.Platform,
                    ["userId"] = request.UserId,
                    ["conversationId"] = conversation.Id.ToString()
                }
            };

            // Process message through Agent Behavior Engine
            var agentResponse = await _agentBehaviorEngine.ProcessMessageAsync(request.Message, personalityContext);

            // Add assistant response with enhanced metadata
            var assistantMessage = await _conversationService.AddMessageAsync(
                conversation.Id, 
                "assistant", 
                agentResponse.Content,
                agentResponse.Metadata);

            return Ok(new MessageDto
            {
                Id = assistantMessage.Id,
                ConversationId = assistantMessage.ConversationId,
                Role = assistantMessage.Role,
                Content = assistantMessage.Content,
                Timestamp = assistantMessage.Timestamp,
                Metadata = new Dictionary<string, object>
                {
                    ["mood"] = agentResponse.Mood.PrimaryMood,
                    ["mood_intensity"] = agentResponse.Mood.Intensity,
                    ["confidence"] = agentResponse.ConfidenceScore,
                    ["triggered_tools"] = agentResponse.TriggeredTools
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, "An error occurred while processing your message");
        }
    }

    [HttpGet("status")]
    public async Task<ActionResult<object>> GetStatus()
    {
        var mcpConnected = await _mcpService.IsConnectedAsync();
        var ivanPersonality = await _personalityService.GetPersonalityAsync("Ivan");
        
        return Ok(new
        {
            McpConnected = mcpConnected,
            PersonalityLoaded = ivanPersonality != null,
            Status = mcpConnected && ivanPersonality != null ? "Ready" : "Not Ready",
            Timestamp = DateTime.UtcNow
        });
    }
}