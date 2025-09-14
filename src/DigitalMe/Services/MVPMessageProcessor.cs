using DigitalMe.Common.Exceptions;
using DigitalMe.Integrations.MCP;
using DigitalMe.Models;

namespace DigitalMe.Services;

/// <summary>
/// MVP Message Processor with loose coupling and SOLID compliance.
/// Simple pipeline: User input → Ivan personality response
/// </summary>
public class MvpMessageProcessor : IMvpMessageProcessor
{
    private readonly IPersonalityService _personalityService;
    private readonly IClaudeApiService _claudeApiService;
    private readonly ILogger<MvpMessageProcessor> _logger;

    public MvpMessageProcessor(
        IPersonalityService personalityService,
        IClaudeApiService claudeApiService,
        ILogger<MvpMessageProcessor> logger)
    {
        _personalityService = personalityService ?? throw new ArgumentNullException(nameof(personalityService));
        _claudeApiService = claudeApiService ?? throw new ArgumentNullException(nameof(claudeApiService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> ProcessMessageAsync(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            throw new ArgumentException("User message cannot be null or empty", nameof(userMessage));
        }

        try
        {
            _logger.LogInformation("🔄 Processing user message (length: {MessageLength})", userMessage.Length);

            // Step 1: Get Ivan's personality context - используем интерфейсный метод для SOLID compliance
            var systemPrompt = await _personalityService.GenerateIvanSystemPromptAsync();

            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                _logger.LogError("❌ Failed to generate system prompt - personality service returned empty result");
                throw new PersonalityServiceException("Failed to generate Ivan's personality context");
            }

            _logger.LogInformation("✅ System prompt generated (length: {PromptLength})", systemPrompt.Length);

            // Step 2: Call Claude API with personality context
            var response = await _claudeApiService.GenerateResponseAsync(systemPrompt, userMessage);

            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogError("❌ Claude API returned empty response");
                throw new ExternalServiceException("Claude API returned empty response");
            }

            _logger.LogInformation("✅ Ivan's response generated (length: {ResponseLength})", response.Length);

            return response;
        }
        catch (PersonalityServiceException)
        {
            _logger.LogError("💥 Personality service failed while processing message");
            throw; // Re-throw domain-specific exceptions
        }
        catch (ExternalServiceException)
        {
            _logger.LogError("💥 External service (Claude API) failed while processing message");
            throw; // Re-throw external service exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Unexpected error while processing user message");
            throw new MessageProcessingException("Failed to process user message", ex,
                new { userMessageLength = userMessage.Length });
        }
    }
}
