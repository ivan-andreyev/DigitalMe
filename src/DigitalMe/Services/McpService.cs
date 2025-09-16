using DigitalMe.Common;
using DigitalMe.Integrations.MCP.Models;
using DigitalMe.Models;

namespace DigitalMe.Services;

// Legacy wrapper - redirects to new MCP implementation
public class McpService : IMcpService
{
    private readonly DigitalMe.Integrations.MCP.McpService _mcpService;

    public McpService(DigitalMe.Integrations.MCP.McpService mcpService)
    {
        _mcpService = mcpService;
    }

    public async Task<Result<bool>> InitializeAsync()
    {
        return await _mcpService.InitializeAsync();
    }

    public async Task<Result<string>> SendMessageAsync(string message, PersonalityContext context)
    {
        return await _mcpService.SendMessageAsync(message, context);
    }

    public async Task<Result<McpResponse>> CallToolAsync(string toolName, Dictionary<string, object> parameters)
    {
        return await _mcpService.CallToolAsync(toolName, parameters);
    }

    public async Task<Result<bool>> IsConnectedAsync()
    {
        return await _mcpService.IsConnectedAsync();
    }

    public async Task<Result<bool>> DisconnectAsync()
    {
        await _mcpService.DisconnectAsync();
        return Result<bool>.Success(true);
    }
}
