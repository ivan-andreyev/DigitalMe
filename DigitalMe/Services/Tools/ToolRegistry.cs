using DigitalMe.Models;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Tools;

/// <summary>
/// Реализация реестра инструментов с поддержкой Strategy Pattern.
/// Управляет коллекцией Tool Strategy и обеспечивает их обнаружение и выполнение.
/// </summary>
public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, IToolStrategy> _tools = new();
    private readonly ILogger<ToolRegistry> _logger;

    public ToolRegistry(ILogger<ToolRegistry> logger)
    {
        _logger = logger;
    }

    public void RegisterTool(IToolStrategy toolStrategy)
    {
        if (toolStrategy == null)
            throw new ArgumentNullException(nameof(toolStrategy));
            
        if (string.IsNullOrWhiteSpace(toolStrategy.ToolName))
            throw new ArgumentException("Tool name cannot be null or empty", nameof(toolStrategy));

        if (_tools.ContainsKey(toolStrategy.ToolName))
        {
            _logger.LogWarning("Tool {ToolName} is already registered, replacing with new implementation", 
                toolStrategy.ToolName);
        }

        _tools[toolStrategy.ToolName] = toolStrategy;
        _logger.LogInformation("Registered tool: {ToolName} - {Description}", 
            toolStrategy.ToolName, toolStrategy.Description);
    }

    public IEnumerable<IToolStrategy> GetAllTools()
    {
        return _tools.Values.OrderByDescending(t => t.Priority).ToList();
    }

    public IToolStrategy? GetTool(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return null;
            
        return _tools.TryGetValue(toolName, out var tool) ? tool : null;
    }

    public async Task<List<IToolStrategy>> GetTriggeredToolsAsync(string message, PersonalityContext context)
    {
        if (string.IsNullOrWhiteSpace(message))
            return new List<IToolStrategy>();

        var triggeredTools = new List<IToolStrategy>();

        foreach (var tool in _tools.Values)
        {
            try
            {
                if (await tool.ShouldTriggerAsync(message, context))
                {
                    triggeredTools.Add(tool);
                    _logger.LogDebug("Tool {ToolName} triggered for message: {MessagePreview}", 
                        tool.ToolName, message.Length > 50 ? message[..50] + "..." : message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking trigger for tool {ToolName}", tool.ToolName);
            }
        }

        // Сортируем по приоритету (выше = важнее)
        return triggeredTools.OrderByDescending(t => t.Priority).ToList();
    }
}