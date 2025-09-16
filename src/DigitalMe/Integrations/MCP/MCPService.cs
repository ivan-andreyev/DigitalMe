using DigitalMe.Common;
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

    public async Task<Result<string>> SendMessageAsync(string message, PersonalityContext context)
    {
        return await ResultExtensions.TryAsync(async () =>
        {
            _logger.LogInformation("Отправляем сообщение через MCP (Anthropic): {MessageLength} символов", message.Length);

            // Use the real Anthropic service
            var responseResult = await _anthropicService.SendMessageAsync(message, context.Profile);
            if (responseResult.IsSuccess)
            {
                _logger.LogInformation("Получен ответ от Anthropic: {ResponseLength} символов", responseResult.Value!.Length);
                return responseResult.Value!;
            }
            else
            {
                _logger.LogWarning("Ошибка от Anthropic: {Error}", responseResult.Error);
                return GenerateFallbackResponse(message, context);
            }
        }, $"Failed to send message via MCP/Anthropic: {message.Substring(0, Math.Min(50, message.Length))}");
    }

    public async Task<Result<bool>> InitializeAsync()
    {
        return await _anthropicService.IsConnectedAsync();
    }

    public async Task<Result<bool>> IsConnectedAsync()
    {
        return await _anthropicService.IsConnectedAsync();
    }

    public async Task<Result<McpResponse>> CallToolAsync(string toolName, Dictionary<string, object> parameters)
    {
        return await ResultExtensions.TryAsync(async () =>
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
        }, $"Failed to execute tool: {toolName}");
    }

    public async Task<Result<bool>> DisconnectAsync()
    {
        return await ResultExtensions.TryAsync(async () =>
        {
            // Nothing to disconnect for Anthropic service
            await Task.CompletedTask;
            return true;
        }, "Failed to disconnect MCP service");
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
