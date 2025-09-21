using DigitalMe.DTOs;
using DigitalMe.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IMvpPersonalityService _personalityService;
    private readonly IMvpMessageProcessor _messageProcessor;
    private readonly IConversationService _conversationService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IMvpPersonalityService personalityService,
        IMvpMessageProcessor messageProcessor,
        IConversationService conversationService,
        ILogger<ChatController> logger)
    {
        _personalityService = personalityService;
        _messageProcessor = messageProcessor;
        _conversationService = conversationService;
        _logger = logger;
    }

    [HttpPost("send")]
    [EnableRateLimiting("chat")]
    public async Task<ActionResult<MessageDto>> SendMessage([FromBody] ChatRequestDto request)
    {
        try
        {
            // 🔥 NUCLEAR LOGGING - print to both ILogger AND Console directly
            var startMsg = "🚀🔥 PRODUCTION DEBUG: ChatController.SendMessage started";
            _logger.LogError(startMsg); // Use ERROR level to guarantee it shows in Cloud Run
            Console.WriteLine(startMsg);
            Console.Out.Flush();

            // Validate input
            if (request == null)
            {
                var errorMsg = "❌🔥 PRODUCTION DEBUG: Request is null";
                _logger.LogError(errorMsg);
                Console.WriteLine(errorMsg);
                Console.Out.Flush();
                return BadRequest("Request cannot be null");
            }

            // Handle empty messages gracefully
            var userMessage = request.Message ?? "";
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                userMessage = "Hello";
                _logger.LogInformation("📝 Received empty message, using default greeting");
            }

            var msgInfo = $"💬🔥 PRODUCTION DEBUG: Processing message: '{userMessage}' from Platform: {request.Platform}, UserId: {request.UserId}";
            _logger.LogError(msgInfo);
            Console.WriteLine(msgInfo);
            Console.Out.Flush();

            // Check if Ivan's personality is available
            var personalityMsg = "👤🔥 PRODUCTION DEBUG: Getting Ivan's personality profile...";
            _logger.LogError(personalityMsg);
            Console.WriteLine(personalityMsg);
            Console.Out.Flush();

            var ivanProfile = await _personalityService.GetIvanProfileAsync();
            if (ivanProfile == null)
            {
                _logger.LogError("❌ Ivan's personality profile not found!");
                return BadRequest("Ivan's personality profile not found. Please create it first.");
            }
            _logger.LogInformation("✅ Ivan's profile loaded with {TraitCount} traits", ivanProfile.Traits?.Count ?? 0);

            // Get or create active conversation for this user+platform
            _logger.LogInformation("💼 Getting/creating conversation...");
            var conversation = await _conversationService.GetActiveConversationAsync(request.Platform, request.UserId);
            if (conversation == null)
            {
                _logger.LogInformation("🆕 Creating new conversation");
                conversation = await _conversationService.StartConversationAsync(request.Platform, request.UserId, "Chat Session");
            }
            _logger.LogInformation("💼 Using conversation ID: {ConversationId}", conversation.Id);

            // Add user message to conversation
            _logger.LogInformation("💾 Saving user message to conversation...");
            await _conversationService.AddMessageAsync(conversation.Id, "user", userMessage);

            // Process message through MVP pipeline
            _logger.LogInformation("🤖 Processing message through MVP pipeline...");
            var response = await _messageProcessor.ProcessMessageAsync(userMessage);
            _logger.LogInformation("✅ Got response: '{Response}'", response);

            // Analyze mood and confidence for conversation pipeline completion
            var mood = AnalyzeMood(userMessage, response);
            var confidence = CalculateConfidence(response);

            // Add assistant response to conversation and return it
            var assistantMessage = await _conversationService.AddMessageAsync(conversation.Id, "assistant", response, new Dictionary<string, object>
            {
                ["platform"] = request.Platform,
                ["userId"] = request.UserId,
                ["processed_via"] = "MVP_Pipeline",
                ["mood"] = mood,
                ["confidence"] = confidence
            });

            // Return enhanced response for conversation pipeline
            return Ok(new MessageDto
            {
                Id = assistantMessage.Id,
                ConversationId = assistantMessage.ConversationId,
                Role = assistantMessage.Role,
                Content = assistantMessage.Content,
                Timestamp = assistantMessage.Timestamp,
                Metadata = new Dictionary<string, object>
                {
                    ["platform"] = request.Platform,
                    ["userId"] = request.UserId,
                    ["processed_via"] = "MVP_Pipeline",
                    ["mood"] = mood,
                    ["confidence"] = confidence
                }
            });
        }
        catch (Exception ex)
        {
            // 🔥 NUCLEAR ERROR LOGGING - capture EVERYTHING
            var errorMsg = $"💥🔥 PRODUCTION CRASH: {ex.GetType().Name}: {ex.Message}";
            var stackMsg = $"🔥 STACK TRACE: {ex.StackTrace}";
            var innerMsg = ex.InnerException != null ? $"🔥 INNER: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}" : "🔥 NO INNER EXCEPTION";

            _logger.LogError(ex, errorMsg);
            _logger.LogError(stackMsg);
            _logger.LogError(innerMsg);

            Console.WriteLine(errorMsg);
            Console.WriteLine(stackMsg);
            Console.WriteLine(innerMsg);
            Console.Out.Flush();

            return StatusCode(500, "An error occurred while processing your message");
        }
    }

    [HttpGet("status")]
    [EnableRateLimiting("api")]
    public async Task<ActionResult<object>> GetStatus()
    {
        var ivanProfile = await _personalityService.GetIvanProfileAsync();
        var personalityLoaded = ivanProfile != null;

        return Ok(new
        {
            McpConnected = true, // MVP: Always true for simplicity
            PersonalityLoaded = personalityLoaded,
            Status = personalityLoaded ? "Ready" : "Not Ready",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Analyze mood from user message and assistant response for conversation pipeline
    /// </summary>
    private string AnalyzeMood(string userMessage, string assistantResponse)
    {
        // Simple mood analysis based on message content
        var userLower = userMessage.ToLowerInvariant();
        var responseLower = assistantResponse.ToLowerInvariant();

        // Check for positive indicators
        if (userLower.Contains("great") || userLower.Contains("awesome") || userLower.Contains("good") ||
            userLower.Contains("thanks") || userLower.Contains("perfect") || userLower.Contains("excellent"))
        {
            return "positive";
        }

        // Check for negative indicators
        if (userLower.Contains("problem") || userLower.Contains("error") || userLower.Contains("issue") ||
            userLower.Contains("wrong") || userLower.Contains("fail") || userLower.Contains("bad"))
        {
            return "negative";
        }

        // Check for technical/analytical indicators (Ivan's style)
        if (userLower.Contains("how") || userLower.Contains("what") || userLower.Contains("why") ||
            userLower.Contains("technical") || userLower.Contains("code") || userLower.Contains("project"))
        {
            return "analytical";
        }

        return "neutral";
    }

    /// <summary>
    /// Calculate confidence level based on response characteristics
    /// </summary>
    private double CalculateConfidence(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            return 0.0;
        }

        double confidence = 0.7; // Base confidence

        // Higher confidence for longer, more detailed responses
        if (response.Length > 200)
        {
            confidence += 0.1;
        }
        if (response.Length > 500)
        {
            confidence += 0.1;
        }

        // Higher confidence for responses with technical terms (Ivan's expertise)
        var techTerms = new[] { "C#", ".NET", "code", "system", "architecture", "solution", "implementation" };
        var techCount = techTerms.Count(term => response.Contains(term, StringComparison.OrdinalIgnoreCase));
        confidence += techCount * 0.05;

        // Higher confidence for structured responses
        if (response.Contains("1.") || response.Contains("2.") || response.Contains("•"))
        {
            confidence += 0.05;
        }

        // Cap confidence at 0.95 to maintain realism
        return Math.Min(confidence, 0.95);
    }
}
