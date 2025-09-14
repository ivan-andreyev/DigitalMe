using System.Text.Json;
using DigitalMe.Integrations.MCP.Models;
using DigitalMe.Models;
using DigitalMe.Services;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Integrations.MCP;

public class McpServiceProper : IMcpService
{
    private readonly IMcpClient _mcpClient;
    private readonly IAnthropicService _anthropicService; // Fallback
    private readonly ILogger<McpServiceProper> _logger;
    private readonly IIvanPersonalityService _ivanPersonalityService;

    public McpServiceProper(
        IMcpClient mcpClient,
        IAnthropicService anthropicService,
        ILogger<McpServiceProper> logger,
        IIvanPersonalityService ivanPersonalityService)
    {
        _mcpClient = mcpClient;
        _anthropicService = anthropicService;
        _logger = logger;
        _ivanPersonalityService = ivanPersonalityService;
    }

    public async Task<bool> InitializeAsync()
    {
        _logger.LogInformation("🚀 Initializing MCP Service (proper implementation)");

        try
        {
            // Try MCP first
            var mcpConnected = await _mcpClient.InitializeAsync();
            if (mcpConnected)
            {
                _logger.LogInformation("✅ MCP server connection established");
                return true;
            }

            // Fallback to direct Anthropic
            _logger.LogWarning("⚠️ MCP server unavailable, checking direct Anthropic fallback");
            var anthropicConnected = await _anthropicService.IsConnectedAsync();

            if (anthropicConnected)
            {
                _logger.LogInformation("✅ Using direct Anthropic as fallback");
                return true;
            }

            _logger.LogError("❌ Both MCP and Anthropic are unavailable");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Failed to initialize MCP Service");
            return false;
        }
    }

    public async Task<string> SendMessageAsync(string message, PersonalityContext context)
    {
        try
        {
            _logger.LogInformation("🔍 SendMessageAsync called with message: '{Message}', MCP IsConnected: {IsConnected}", message, _mcpClient.IsConnected);

            // Try to initialize if not connected
            if (!_mcpClient.IsConnected)
            {
                _logger.LogInformation("🔗 MCP not connected, attempting initialization...");
                var initialized = await _mcpClient.InitializeAsync();
                _logger.LogInformation("🔗 MCP initialization result: {Result}", initialized);
            }

            if (_mcpClient.IsConnected)
            {
                return await SendMessageViaMcpAsync(message, context);
            }
            else
            {
                _logger.LogInformation("📞 Using Anthropic fallback (MCP unavailable)");
                return await _anthropicService.SendMessageAsync(message, context.Profile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Failed to send message via MCP");
            return await GenerateFallbackResponseAsync(message, context);
        }
    }

    private async Task<string> SendMessageViaMcpAsync(string message, PersonalityContext context)
    {
        _logger.LogInformation("🔗 Sending message via MCP protocol");

        try
        {
            // Get Ivan's personality for system prompt
            var ivanPersonality = await _ivanPersonalityService.GetIvanPersonalityAsync();
            var systemPrompt = _ivanPersonalityService.GenerateSystemPrompt(ivanPersonality);

            // Prepare MCP request for conversation
            var request = new McpRequest
            {
                Method = "llm/complete",
                Params = new
                {
                    model = "claude-3-5-sonnet-20241022",
                    systemPrompt = systemPrompt,
                    messages = new[]
                    {
                        new { role = "user", content = message }
                    },
                    maxTokens = 1000,
                    temperature = 0.7,
                    metadata = new
                    {
                        userId = context.CurrentState.GetValueOrDefault("userId", "unknown").ToString(),
                        platform = context.CurrentState.GetValueOrDefault("platform", "unknown").ToString(),
                        conversationId = context.CurrentState.GetValueOrDefault("conversationId", "").ToString(),
                        isRealTime = context.CurrentState.GetValueOrDefault("isRealTime", false)
                    }
                }
            };

            var response = await _mcpClient.SendRequestAsync(request);

            if (response.Error != null)
            {
                _logger.LogError("❌ MCP request failed: {ErrorCode} - {ErrorMessage}",
                    response.Error.Code, response.Error.Message);

                // Fallback to direct Anthropic
                return await _anthropicService.SendMessageAsync(message, context.Profile);
            }

            if (response.Result?.Content != null)
            {
                var resultText = ExtractContentFromMcpResult(response.Result.Content);
                _logger.LogInformation("✅ MCP response received: {Length} characters", resultText.Length);
                return resultText;
            }

            _logger.LogWarning("⚠️ Empty MCP response, using fallback");
            return await _anthropicService.SendMessageAsync(message, context.Profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 MCP protocol error, falling back to Anthropic");
            return await _anthropicService.SendMessageAsync(message, context.Profile);
        }
    }

    public async Task<McpResponse> CallToolAsync(string toolName, Dictionary<string, object> parameters)
    {
        if (_mcpClient.IsConnected)
        {
            _logger.LogInformation("🔧 Calling MCP tool: {ToolName}", toolName);
            return await _mcpClient.CallToolAsync(toolName, parameters);
        }
        else
        {
            _logger.LogWarning("⚠️ MCP not connected, cannot call tool: {ToolName}", toolName);
            return new McpResponse
            {
                Error = new McpError
                {
                    Code = -32001,
                    Message = "MCP server not connected"
                }
            };
        }
    }

    public async Task<bool> IsConnectedAsync()
    {
        if (_mcpClient.IsConnected)
        {
            return true;
        }

        // Check fallback Anthropic connection
        return await _anthropicService.IsConnectedAsync();
    }

    public async Task DisconnectAsync()
    {
        await _mcpClient.DisconnectAsync();
    }

    private string ExtractContentFromMcpResult(string resultContent)
    {
        try
        {
            // Try to parse as JSON first
            var jsonResult = JsonSerializer.Deserialize<JsonElement>(resultContent);

            // Look for common response patterns
            if (jsonResult.TryGetProperty("content", out var contentElement))
            {
                return contentElement.GetString() ?? resultContent;
            }

            if (jsonResult.TryGetProperty("text", out var textElement))
            {
                return textElement.GetString() ?? resultContent;
            }

            if (jsonResult.TryGetProperty("message", out var messageElement))
            {
                return messageElement.GetString() ?? resultContent;
            }

            // If it's an array, get first element
            if (jsonResult.ValueKind == JsonValueKind.Array && jsonResult.GetArrayLength() > 0)
            {
                var firstElement = jsonResult[0];
                if (firstElement.TryGetProperty("text", out var arrayTextElement))
                {
                    return arrayTextElement.GetString() ?? resultContent;
                }
            }

            return resultContent;
        }
        catch
        {
            // If parsing fails, return as-is
            return resultContent;
        }
    }

    private async Task<string> GenerateFallbackResponseAsync(string message, PersonalityContext context)
    {
        var ivanProfile = await _ivanPersonalityService.GetIvanPersonalityAsync();

        var responses = new[]
        {
            "Слушай, MCP протокол сейчас недоступен. Нужно поднять MCP сервер или настроить подключение как положено.",
            "Без MCP сервера могу только сказать - нужно разбираться в архитектуре. У меня как у Head of R&D опыта достаточно.",
            "MCP интеграция упала. Проверь конфигурацию серверов и протоколы. Я работаю за троих, но без инструментов сложно.",
            "Проблема с MCP подключением. Структурированно: определи факторы → проверь конфиг → исправь → протестируй.",
            "MCP недоступен, но по твоему вопросу могу сказать базово. Для полноценной работы нужна настроенная инфраструктура."
        };

        var random = new Random();
        var response = responses[random.Next(responses.Length)];

        _logger.LogInformation("Generated Ivan-style MCP fallback response");
        return response;
    }
}
