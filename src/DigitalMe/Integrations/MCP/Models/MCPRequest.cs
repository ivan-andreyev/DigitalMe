using System.Text.Json;
using System.Text.Json.Serialization;

namespace DigitalMe.Integrations.MCP.Models;

public class MCPRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

public class MCPResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public MCPResult? Result { get; set; }

    [JsonPropertyName("error")]
    public MCPError? Error { get; set; }
}

public class MCPResult
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("toolCalls")]
    public IEnumerable<MCPToolCall> ToolCalls { get; set; } = new List<MCPToolCall>();

    // For tools/list response
    [JsonPropertyName("tools")]
    public List<MCPTool> Tools { get; set; } = new List<MCPTool>();

    // For llm/complete response metadata
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    // For structured responses, use JsonExtensionData to capture any additional properties
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public class MCPError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

public class MCPToolCall
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();
}

public class MCPTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public object Parameters { get; set; } = new();
}
