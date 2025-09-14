using DigitalMe.Integrations.MCP.Models;
using DigitalMe.Integrations.MCP.Tools;
using DigitalMe.Models;
using DigitalMe.Services;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Integrations.MCP;

public class McpService : IMcpService
{
    private readonly IAnthropicService _anthropicService;
    private readonly ILogger<McpService> _logger;
    private readonly ToolExecutor _toolExecutor;

    public McpService(
        IAnthropicService anthropicService,
        ILogger<McpService> logger,
        ToolExecutor toolExecutor)
    {
        _anthropicService = anthropicService;
        _logger = logger;
        _toolExecutor = toolExecutor;
    }

    public async Task<string> SendMessageAsync(string message, PersonalityContext context)
    {
        try
        {
            _logger.LogInformation("Отправляем сообщение через MCP (Anthropic): {MessageLength} символов", message.Length);

            // Use the real Anthropic service
            var response = await _anthropicService.SendMessageAsync(message, context.Profile);

            _logger.LogInformation("Получен ответ от Anthropic: {ResponseLength} символов", response.Length);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки через MCP/Anthropic");
            return GenerateFallbackResponse(message, context);
        }
    }

    public async Task<bool> InitializeAsync()
    {
        return await _anthropicService.IsConnectedAsync();
    }

    public async Task<bool> IsConnectedAsync()
    {
        return await _anthropicService.IsConnectedAsync();
    }

    public async Task<McpResponse> CallToolAsync(string toolName, Dictionary<string, object> parameters)
    {
        try
        {
            var result = await _toolExecutor.ExecuteToolAsync(toolName, parameters);
            return new McpResponse
            {
                Result = new McpResult
                {
                    Content = result?.ToString() ?? "No result",
                    ToolCalls = new List<McpToolCall>()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
            return new McpResponse
            {
                Error = new McpError
                {
                    Code = -1,
                    Message = $"Tool execution failed: {ex.Message}"
                }
            };
        }
    }

    public async Task DisconnectAsync()
    {
        // Nothing to disconnect for Anthropic service
        await Task.CompletedTask;
    }

    private string GenerateFallbackResponse(string message, PersonalityContext context)
    {
        var responses = new[]
        {
            "Не смог сгенерировать ответ через MCP, но вопрос понял. Разберись с настройками API.",
            "MCP соединение недоступно. Проверь конфигурацию Anthropic API.",
            "Всем похуй на мои проблемы с API, но твой вопрос требует настроенного подключения.",
            "Сила в правде: без API ключа от Claude нормально не отвечу. Исправь это.",
        };

        var random = new Random();
        return responses[random.Next(responses.Length)];
    }
}
