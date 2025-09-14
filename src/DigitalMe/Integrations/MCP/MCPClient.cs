using System.Text;
using System.Text.Json;
using DigitalMe.Integrations.MCP.Models;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Integrations.MCP;

public interface IMcpClient
{
    Task<bool> InitializeAsync();
    Task<McpResponse> SendRequestAsync(McpRequest request);
    Task<List<McpTool>> ListToolsAsync();
    Task<McpResponse> CallToolAsync(string toolName, Dictionary<string, object> parameters);
    Task DisconnectAsync();
    bool IsConnected { get; }
}

public class McpClient : IMcpClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpClient> _logger;
    private readonly string _serverUrl;
    private bool _isConnected;

    public bool IsConnected => _isConnected;

    public McpClient(HttpClient httpClient, ILogger<McpClient> logger, string serverUrl = "http://localhost:3000")
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
            var initRequest = new McpRequest
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
                var initializedNotification = new McpRequest
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

    public async Task<McpResponse> SendRequestAsync(McpRequest request)
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
                var mcpResponse = JsonSerializer.Deserialize<McpResponse>(responseText);

                _logger.LogDebug("📥 Received MCP response for ID: {RequestId}", request.Id);

                return mcpResponse ?? new McpResponse
                {
                    Error = new McpError { Code = -32700, Message = "Parse error: Invalid response format" }
                };
            }
            else
            {
                _logger.LogWarning("⚠️ HTTP error from MCP server: {StatusCode} - {ReasonPhrase}",
                    httpResponse.StatusCode, httpResponse.ReasonPhrase);

                return new McpResponse
                {
                    Error = new McpError
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

            return new McpResponse
            {
                Error = new McpError
                {
                    Code = -32603,
                    Message = $"Internal error: {ex.Message}"
                }
            };
        }
    }

    public async Task<List<McpTool>> ListToolsAsync()
    {
        var request = new McpRequest
        {
            Method = "tools/list",
            Params = new { }
        };

        var response = await SendRequestAsync(request);

        if (response.Error != null)
        {
            _logger.LogError("Failed to list MCP tools: {ErrorCode} - {ErrorMessage}",
                response.Error.Code, response.Error.Message);
            return new List<McpTool>();
        }

        // Return tools directly from the result
        return response.Result?.Tools ?? new List<McpTool>();
    }

    public async Task<McpResponse> CallToolAsync(string toolName, Dictionary<string, object> parameters)
    {
        var request = new McpRequest
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

    private async Task SendNotificationAsync(McpRequest notification)
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
