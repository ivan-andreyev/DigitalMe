using DigitalMe.Integrations.MCP.Models;
using DigitalMe.Models;

namespace DigitalMe.Services;

// Legacy wrapper - redirects to new MCP implementation
public class McpService : IMcpService
{
    private readonly DigitalMe.Integrations.MCP.MCPService _mcpService;

    public McpService(DigitalMe.Integrations.MCP.MCPService mcpService)
    {
        _mcpService = mcpService;
    }

    public async Task<bool> InitializeAsync()
    {
        return await _mcpService.InitializeAsync();
    }

    public async Task<string> SendMessageAsync(string message, PersonalityContext context)
    {
        return await _mcpService.SendMessageAsync(message, context);
    }

    public async Task<MCPResponse> CallToolAsync(string toolName, Dictionary<string, object> parameters)
    {
        return await _mcpService.CallToolAsync(toolName, parameters);
    }

    public async Task<bool> IsConnectedAsync()
    {
        return await _mcpService.IsConnectedAsync();
    }

    public async Task DisconnectAsync()
    {
        await _mcpService.DisconnectAsync();
    }
}
