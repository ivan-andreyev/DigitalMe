using DigitalMe.Integrations.MCP.Models;
using DigitalMe.Models;

namespace DigitalMe.Services;

public interface IMcpService
{
    Task<bool> InitializeAsync();
    Task<string> SendMessageAsync(string message, PersonalityContext context);
    Task<MCPResponse> CallToolAsync(string toolName, Dictionary<string, object> parameters);
    Task<bool> IsConnectedAsync();
    Task DisconnectAsync();
}
