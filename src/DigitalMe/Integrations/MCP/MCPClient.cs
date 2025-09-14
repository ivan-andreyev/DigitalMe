using Microsoft.Extensions.Logging;
using DigitalMe.Integrations.MCP.Models;
using System.Text.Json;
using System.Text;

namespace DigitalMe.Integrations.MCP;

public interface IMCPClient
{
    Task<bool> InitializeAsync();
    Task<MCPResponse> SendRequestAsync(MCPRequest request);
    Task<List<MCPTool>> ListToolsAsync();
    Task<MCPResponse> CallToolAsync(string toolName, Dictionary<string, object> parameters);
    Task DisconnectAsync();
    bool IsConnected { get; }
}

public class MCPClient : IMCPClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MCPClient> _logger;
    private readonly string _serverUrl;
    private bool _isConnected;

    public bool IsConnected => _isConnected;

    public MCPClient(HttpClient httpClient, ILogger<MCPClient> logger, string serverUrl = "http://localhost:3000")
    {
        _httpClient = httpClient;
        _logger = logger;
        _serverUrl = serverUrl;
        _httpClient.BaseAddress = new Uri(_serverUrl);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<bool> InitializeAsync()
    {
        try
        {
            _logger.LogInformation("🔗 Initializing MCP connection to {ServerUrl}", _serverUrl);

            // Send MCP initialize request
            var initRequest = new MCPRequest
            {
                Method = "initialize",
                Params = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new
                    {
                        tools = new { },
                        resources = new { },
                        prompts = new { }
                    },
                    clientInfo = new
                    {
                        name = "DigitalMe",
                        version = "1.0.0"
                    }
                }
            };

            var response = await SendRequestAsync(initRequest);

            if (response.Error == null)
            {
                _isConnected = true;
                _logger.LogInformation("✅ MCP connection initialized successfully");

                // Send initialized notification (no response expected)
                var initializedNotification = new MCPRequest
                {
                    Method = "notifications/initialized",
                    Params = new { }
                };

                await SendNotificationAsync(initializedNotification);

                return true;
            }
            else
            {
                _logger.LogError("❌ MCP initialization failed: {ErrorCode} - {ErrorMessage}",
                    response.Error.Code, response.Error.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to initialize MCP connection");
            return false;
        }
    }

    public async Task<MCPResponse> SendRequestAsync(MCPRequest request)
    {
        try
        {
            _logger.LogDebug("📤 Sending MCP request: {Method} (ID: {RequestId})", request.Method, request.Id);

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpResponse = await _httpClient.PostAsync("/mcp", content);

            if (httpResponse.IsSuccessStatusCode)
            {
                var responseText = await httpResponse.Content.ReadAsStringAsync();
                var mcpResponse = JsonSerializer.Deserialize<MCPResponse>(responseText);

                _logger.LogDebug("📥 Received MCP response for ID: {RequestId}", request.Id);

                return mcpResponse ?? new MCPResponse
                {
                    Error = new MCPError { Code = -32700, Message = "Parse error: Invalid response format" }
                };
            }
            else
            {
                _logger.LogWarning("⚠️ HTTP error from MCP server: {StatusCode} - {ReasonPhrase}",
                    httpResponse.StatusCode, httpResponse.ReasonPhrase);

                return new MCPResponse
                {
                    Error = new MCPError
                    {
                        Code = (int)httpResponse.StatusCode,
                        Message = $"HTTP {httpResponse.StatusCode}: {httpResponse.ReasonPhrase}"
                    }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Failed to send MCP request: {Method}", request.Method);

            return new MCPResponse
            {
                Error = new MCPError
                {
                    Code = -32603,
                    Message = $"Internal error: {ex.Message}"
                }
            };
        }
    }

    public async Task<List<MCPTool>> ListToolsAsync()
    {
        var request = new MCPRequest
        {
            Method = "tools/list",
            Params = new { }
        };

        var response = await SendRequestAsync(request);

        if (response.Error != null)
        {
            _logger.LogError("Failed to list MCP tools: {ErrorCode} - {ErrorMessage}",
                response.Error.Code, response.Error.Message);
            return new List<MCPTool>();
        }

        // Return tools directly from the result
        return response.Result?.Tools ?? new List<MCPTool>();
    }

    public async Task<MCPResponse> CallToolAsync(string toolName, Dictionary<string, object> parameters)
    {
        var request = new MCPRequest
        {
            Method = "tools/call",
            Params = new
            {
                name = toolName,
                arguments = parameters
            }
        };

        _logger.LogInformation("🔧 Calling MCP tool: {ToolName} with {ParameterCount} parameters",
            toolName, parameters.Count);

        var response = await SendRequestAsync(request);

        if (response.Error != null)
        {
            _logger.LogError("Tool call failed: {ToolName} - {ErrorMessage}", toolName, response.Error.Message);
        }
        else
        {
            _logger.LogInformation("✅ Tool call successful: {ToolName}", toolName);
        }

        return response;
    }

    private async Task SendNotificationAsync(MCPRequest notification)
    {
        try
        {
            var json = JsonSerializer.Serialize(notification);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Notifications don't expect responses, so we don't wait for success
            await _httpClient.PostAsync("/mcp/notify", content);

            _logger.LogDebug("📢 Sent MCP notification: {Method}", notification.Method);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send MCP notification: {Method}", notification.Method);
        }
    }

    public Task DisconnectAsync()
    {
        if (_isConnected)
        {
            _logger.LogInformation("🔌 Disconnecting from MCP server");

            // Send disconnect notification if needed
            _isConnected = false;
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
        _httpClient?.Dispose();
    }
}
