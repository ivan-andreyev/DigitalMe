using DigitalMe.Models;
using DigitalMe.Services.AgentBehavior;
using DigitalMe.Services.Tools;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DigitalMe.Services.AgentBehavior;

public class AgentBehaviorEngine : IAgentBehaviorEngine
{
    private readonly IPersonalityService _personalityService;
    private readonly IMcpService _mcpService;
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<AgentBehaviorEngine> _logger;

    public AgentBehaviorEngine(
        IPersonalityService personalityService,
        IMcpService mcpService,
        IToolRegistry toolRegistry,
        ILogger<AgentBehaviorEngine> logger)
    {
        _personalityService = personalityService;
        _mcpService = mcpService;
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    public async Task<AgentResponse> ProcessMessageAsync(string message, PersonalityContext context)
    {
        _logger.LogInformation("Processing message through Agent Behavior Engine");

        var response = new AgentResponse();

        try
        {
            // Analyze mood from message
            response.Mood = await AnalyzeMoodAsync(message, context.Profile);

            // Determine triggered tools based on message content
            response.TriggeredTools = await DetermineTriggeredToolsAsync(message, context);

            // Generate contextual metadata
            response.Metadata = await GenerateContextMetadataAsync(context);

            // Get MCP response with enhanced context
            var mcpResponse = await _mcpService.SendMessageAsync(message, context);
            response.Content = mcpResponse;

            // Calculate confidence based on various factors
            response.ConfidenceScore = CalculateConfidenceScore(message, context, response.Mood);

            _logger.LogInformation("Agent response generated with confidence {Confidence}%", 
                response.ConfidenceScore * 100);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message through Agent Behavior Engine");
            
            // Return fallback response
            response.Content = "Извини, возникла техническая проблема. Попробуй переформулировать вопрос.";
            response.ConfidenceScore = 0.1;
            return response;
        }
    }

    public async Task<MoodAnalysis> AnalyzeMoodAsync(string message, PersonalityProfile personality)
    {
        _logger.LogDebug("Analyzing mood for message: {MessagePreview}", 
            message.Length > 50 ? message.Substring(0, 50) + "..." : message);

        var moodAnalysis = new MoodAnalysis
        {
            Context = $"Analyzing as {personality.Name}"
        };

        // Simple mood detection based on keywords and patterns
        var moodScores = new Dictionary<string, double>
        {
            ["positive"] = 0.0,
            ["negative"] = 0.0,
            ["neutral"] = 0.0,
            ["technical"] = 0.0,
            ["frustrated"] = 0.0,
            ["confident"] = 0.0
        };

        var messageLower = message.ToLower();

        // Positive indicators
        if (ContainsWords(messageLower, "спасибо", "отлично", "хорошо", "круто", "супер", "класс"))
            moodScores["positive"] += 0.7;

        // Negative indicators
        if (ContainsWords(messageLower, "плохо", "ошибка", "проблема", "не работает", "сломалось"))
            moodScores["negative"] += 0.6;

        // Technical indicators
        if (ContainsWords(messageLower, "код", "программа", "api", "база данных", "архитектура", "c#", ".net"))
            moodScores["technical"] += 0.8;

        // Frustration indicators  
        if (ContainsWords(messageLower, "почему", "как так", "не понимаю", "что за"))
            moodScores["frustrated"] += 0.5;

        // Confidence indicators
        if (ContainsWords(messageLower, "знаю", "уверен", "очевидно", "точно"))
            moodScores["confident"] += 0.6;

        // Default to neutral if no strong indicators
        if (moodScores.Values.All(score => score < 0.3))
            moodScores["neutral"] = 0.5;

        // Find primary mood
        var primaryMood = moodScores.OrderByDescending(kvp => kvp.Value).First();
        moodAnalysis.PrimaryMood = primaryMood.Key;
        moodAnalysis.Intensity = primaryMood.Value;
        moodAnalysis.MoodScores = moodScores;

        await Task.Delay(10); // Simulate processing time

        return moodAnalysis;
    }

    public async Task<bool> ShouldTriggerToolAsync(string toolName, string message, PersonalityContext context)
    {
        _logger.LogDebug("Checking if tool '{ToolName}' should trigger for message", toolName);

        try
        {
            var toolStrategy = _toolRegistry.GetTool(toolName);
            if (toolStrategy == null)
            {
                _logger.LogWarning("Tool strategy not found for tool: {ToolName}", toolName);
                return false;
            }

            var shouldTrigger = await toolStrategy.ShouldTriggerAsync(message, context);
            _logger.LogDebug("Tool '{ToolName}' trigger check result: {ShouldTrigger}", toolName, shouldTrigger);
            
            return shouldTrigger;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check tool trigger for '{ToolName}'", toolName);
            return false;
        }
    }

    public async Task<Dictionary<string, object>> GenerateContextMetadataAsync(PersonalityContext context)
    {
        var metadata = new Dictionary<string, object>
        {
            ["timestamp"] = DateTime.UtcNow,
            ["personality_name"] = context.Profile.Name,
            ["message_count"] = context.RecentMessages.Count(),
            ["conversation_active"] = context.CurrentState.ContainsKey("conversationId")
        };

        // Analyze conversation patterns
        if (context.RecentMessages.Any())
        {
            var lastMessage = context.RecentMessages.Last();
            metadata["last_message_age_minutes"] = (DateTime.UtcNow - lastMessage.Timestamp).TotalMinutes;
            metadata["conversation_length"] = context.RecentMessages.Count();
            
            var userMessages = context.RecentMessages.Where(m => m.Role == "user").Count();
            var assistantMessages = context.RecentMessages.Where(m => m.Role == "assistant").Count();
            metadata["user_assistant_ratio"] = userMessages > 0 ? (double)assistantMessages / userMessages : 0.0;
        }

        // Add platform-specific metadata
        if (context.CurrentState.ContainsKey("platform"))
        {
            metadata["platform"] = context.CurrentState["platform"];
        }

        await Task.Delay(10); // Simulate processing time
        return metadata;
    }

    private async Task<List<string>> DetermineTriggeredToolsAsync(string message, PersonalityContext context)
    {
        _logger.LogDebug("Determining triggered tools for message: {MessagePreview}", 
            message.Length > 50 ? message.Substring(0, 50) + "..." : message);

        try
        {
            var triggeredToolStrategies = await _toolRegistry.GetTriggeredToolsAsync(message, context);
            var triggeredTools = triggeredToolStrategies.Select(t => t.ToolName).ToList();

            _logger.LogInformation("Found {Count} triggered tools: {Tools}", 
                triggeredTools.Count, string.Join(", ", triggeredTools));

            return triggeredTools;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to determine triggered tools");
            return new List<string>();
        }
    }

    private double CalculateConfidenceScore(string message, PersonalityContext context, MoodAnalysis mood)
    {
        var confidence = 0.5; // Base confidence

        // Increase confidence for technical topics (Ivan's expertise)
        if (mood.MoodScores.GetValueOrDefault("technical", 0) > 0.5)
            confidence += 0.3;

        // Increase confidence if we have conversation history
        if (context.RecentMessages.Count() > 2)
            confidence += 0.1;

        // Decrease confidence for very short messages
        if (message.Length < 10)
            confidence -= 0.2;

        // Increase confidence for clear questions
        if (message.Contains("?") || message.ToLower().StartsWith("как") || message.ToLower().StartsWith("что"))
            confidence += 0.1;

        return Math.Max(0.1, Math.Min(1.0, confidence));
    }

    private bool ContainsWords(string text, params string[] words)
    {
        return words.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}