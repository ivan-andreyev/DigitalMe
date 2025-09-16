using DigitalMe.Common;
using DigitalMe.Integrations.MCP.Models;
using DigitalMe.Models;

namespace DigitalMe.Services;

public interface IMcpService
{
    Task<Result<bool>> InitializeAsync();
    Task<Result<string>> SendMessageAsync(string message, PersonalityContext context);
    Task<Result<McpResponse>> CallToolAsync(string toolName, Dictionary<string, object> parameters);
    Task<Result<bool>> IsConnectedAsync();
    Task<Result<bool>> DisconnectAsync();
}
