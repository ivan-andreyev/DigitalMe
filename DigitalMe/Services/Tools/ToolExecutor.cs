using DigitalMe.Models;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Tools;

/// <summary>
/// Исполнитель инструментов на основе Tool Strategy Pattern.
/// Заменяет старый MCP ToolExecutor и использует зарегистрированные стратегии.
/// </summary>
public class ToolExecutor
{
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<ToolExecutor> _logger;

    public ToolExecutor(IToolRegistry toolRegistry, ILogger<ToolExecutor> logger)
    {
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Выполняет инструмент с указанными параметрами, используя соответствующую Strategy.
    /// </summary>
    /// <param name="toolName">Имя инструмента для выполнения</param>
    /// <param name="parameters">Параметры для выполнения</param>
    /// <param name="context">Контекст личности для персонализации</param>
    /// <returns>Результат выполнения инструмента</returns>
    public async Task<object> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters, PersonalityContext context)
    {
        _logger.LogInformation("Executing tool {ToolName} with parameters {Parameters}", 
            toolName, string.Join(", ", parameters.Keys));

        if (string.IsNullOrWhiteSpace(toolName))
        {
            var error = "Tool name cannot be null or empty";
            _logger.LogError(error);
            return new { success = false, error, tool_name = toolName };
        }

        try
        {
            var toolStrategy = _toolRegistry.GetTool(toolName);
            if (toolStrategy == null)
            {
                var error = $"Tool '{toolName}' not found in registry";
                _logger.LogWarning(error);
                
                // Возвращаем информацию о доступных инструментах
                var availableTools = _toolRegistry.GetAllTools().Select(t => t.ToolName).ToList();
                return new 
                { 
                    success = false, 
                    error,
                    tool_name = toolName,
                    available_tools = availableTools
                };
            }

            _logger.LogDebug("Found tool strategy: {ToolName} - {Description}", 
                toolStrategy.ToolName, toolStrategy.Description);

            var startTime = DateTime.UtcNow;
            var result = await toolStrategy.ExecuteAsync(parameters, context);
            var executionTime = DateTime.UtcNow - startTime;

            _logger.LogInformation("Successfully executed tool {ToolName} in {ExecutionTime}ms", 
                toolName, executionTime.TotalMilliseconds);

            // Добавляем метаданные о выполнении, если результат является object
            if (result is Dictionary<string, object> resultDict)
            {
                if (!resultDict.ContainsKey("execution_time_ms"))
                {
                    resultDict["execution_time_ms"] = executionTime.TotalMilliseconds;
                }
                if (!resultDict.ContainsKey("executed_at"))
                {
                    resultDict["executed_at"] = DateTime.UtcNow;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute tool {ToolName}", toolName);
            return new 
            { 
                success = false, 
                error = ex.Message,
                tool_name = toolName,
                error_type = ex.GetType().Name
            };
        }
    }

    /// <summary>
    /// Получает схему параметров для указанного инструмента.
    /// Only returns schema if tool implements IParameterizedTool (ISP compliance).
    /// </summary>
    /// <param name="toolName">Имя инструмента</param>
    /// <returns>JSON Schema для параметров инструмента</returns>
    public object? GetToolParameterSchema(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return null;

        var toolStrategy = _toolRegistry.GetTool(toolName);
        return toolStrategy is IParameterizedTool parameterizedTool 
            ? parameterizedTool.GetParameterSchema() 
            : null;
    }

    /// <summary>
    /// Получает информацию обо всех доступных инструментах.
    /// Uses ISP-compliant approach - only includes parameter schema for tools that implement IParameterizedTool.
    /// </summary>
    /// <returns>Список доступных инструментов с их описаниями</returns>
    public List<object> GetAvailableTools()
    {
        try
        {
            var tools = _toolRegistry.GetAllTools().Select(tool => new
            {
                name = tool.ToolName,
                description = tool.Description,
                priority = tool.Priority,
                parameter_schema = tool is IParameterizedTool parameterizedTool 
                    ? parameterizedTool.GetParameterSchema() 
                    : null,
                has_parameters = tool is IParameterizedTool
            }).ToList();

            _logger.LogDebug("Retrieved {Count} available tools", tools.Count);
            return tools.Cast<object>().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve available tools");
            return new List<object>();
        }
    }

    /// <summary>
    /// Проверяет, поддерживается ли указанный инструмент.
    /// </summary>
    /// <param name="toolName">Имя инструмента</param>
    /// <returns>True если инструмент поддерживается</returns>
    public bool IsToolSupported(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return false;

        return _toolRegistry.GetTool(toolName) != null;
    }
}