using System.Text.Json;
using DigitalMe.Common;
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
    private readonly IPersonalityService _personalityService;

    public McpServiceProper(
        IMcpClient mcpClient,
        IAnthropicService anthropicService,
        ILogger<McpServiceProper> logger,
        IPersonalityService personalityService)
    {
        _mcpClient = mcpClient;
        _anthropicService = anthropicService;
        _logger = logger;
        _personalityService = personalityService;
    }

    public async Task<Result<bool>> InitializeAsync()
    {
        return await ResultExtensions.TryAsync(async () =>
        {
            _logger.LogInformation("🚀 Initializing MCP Service (proper implementation)");

            // Try MCP first
            var mcpConnected = await _mcpClient.InitializeAsync();
            if (mcpConnected)
            {
                _logger.LogInformation("✅ MCP server connection established");
                return true;
            }

            // Fallback to direct Anthropic
            _logger.LogWarning("⚠️ MCP server unavailable, checking direct Anthropic fallback");
            var anthropicConnectedResult = await _anthropicService.IsConnectedAsync();

            if (anthropicConnectedResult.IsSuccess && anthropicConnectedResult.Value)
            {
                _logger.LogInformation("✅ Using direct Anthropic as fallback");
                return true;
            }

            _logger.LogError("❌ Both MCP and Anthropic are unavailable");
            return false;
        }, "Failed to initialize MCP Service");
    }

    public async Task<Result<string>> SendMessageAsync(string message, PersonalityContext context)
    {
        return await ResultExtensions.TryAsync(async () =>
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
                var anthropicResult = await _anthropicService.SendMessageAsync(message, context.Profile);
                if (anthropicResult.IsSuccess)
                {
                    return anthropicResult.Value;
                }
                else
                {
                    _logger.LogWarning("Anthropic fallback failed: {Error}", anthropicResult.Error);
                    return await GenerateFallbackResponseAsync(message, context);
                }
            }
        }, $"Failed to send message via MCP: {message.Substring(0, Math.Min(50, message.Length))}");
    }

    private async Task<string> SendMessageViaMcpAsync(string message, PersonalityContext context)
    {
        _logger.LogInformation("🔗 Sending message via MCP protocol");

        try
        {
            // Get Ivan's personality for system prompt
            var ivanPersonalityResult = await _personalityService.GetPersonalityAsync();
            var systemPrompt = ivanPersonalityResult.IsSuccess && ivanPersonalityResult.Value != null
                ? _personalityService.GenerateSystemPrompt(ivanPersonalityResult.Value).Value ?? "System prompt unavailable"
                : "Error loading personality profile";

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
                var anthropicFallbackResult = await _anthropicService.SendMessageAsync(message, context.Profile);
                return anthropicFallbackResult.IsSuccess && anthropicFallbackResult.Value != null
                    ? anthropicFallbackResult.Value
                    : await GenerateFallbackResponseAsync(message, context);
            }

            if (response.Result?.Content != null)
            {
                var resultText = ExtractContentFromMcpResult(response.Result.Content);
                _logger.LogInformation("✅ MCP response received: {Length} characters", resultText.Length);
                return resultText;
            }

            _logger.LogWarning("⚠️ Empty MCP response, using fallback");
            var anthropicEmptyResult = await _anthropicService.SendMessageAsync(message, context.Profile);
            return anthropicEmptyResult.IsSuccess && anthropicEmptyResult.Value != null
                ? anthropicEmptyResult.Value
                : await GenerateFallbackResponseAsync(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 MCP protocol error, falling back to Anthropic");
            var anthropicErrorResult = await _anthropicService.SendMessageAsync(message, context.Profile);
            return anthropicErrorResult.IsSuccess && anthropicErrorResult.Value != null
                ? anthropicErrorResult.Value
                : await GenerateFallbackResponseAsync(message, context);
        }
    }

    public async Task<Result<McpResponse>> CallToolAsync(string toolName, Dictionary<string, object> parameters)
    {
        return await ResultExtensions.TryAsync(async () =>
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
        }, $"Failed to call MCP tool: {toolName}");
    }

    public async Task<Result<bool>> IsConnectedAsync()
    {
        return await ResultExtensions.TryAsync(async () =>
        {
            if (_mcpClient.IsConnected)
            {
                return true;
            }

            // Check fallback Anthropic connection
            var anthropicResult = await _anthropicService.IsConnectedAsync();
            return anthropicResult.IsSuccess && anthropicResult.Value;
        }, "Failed to check MCP service connection status");
    }

    public async Task<Result<bool>> DisconnectAsync()
    {
        return await ResultExtensions.TryAsync(async () =>
        {
            await _mcpClient.DisconnectAsync();
            return true;
        }, "Failed to disconnect MCP service");
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
        var ivanProfileResult = await _personalityService.GetPersonalityAsync();
        var ivanProfile = ivanProfileResult.IsSuccess ? ivanProfileResult.Value : null;

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
