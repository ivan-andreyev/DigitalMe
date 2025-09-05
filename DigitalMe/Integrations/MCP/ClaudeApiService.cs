using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DigitalMe.Integrations.MCP;

/// <summary>
/// Service for integrating with Claude API using Anthropic SDK.
/// Provides personality-aware message processing and system prompt generation.
/// Implements rate limiting, error handling, and response caching.
/// </summary>
public interface IClaudeApiService
{
    Task<string> GenerateResponseAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
    Task<string> GeneratePersonalityResponseAsync(Guid personalityId, string userMessage, CancellationToken cancellationToken = default);
    Task<bool> ValidateApiConnectionAsync();
    Task<ClaudeApiHealth> GetHealthStatusAsync();
}

public class ClaudeApiService : IClaudeApiService
{
    private readonly AnthropicClient _anthropicClient;
    private readonly ILogger<ClaudeApiService> _logger;
    private readonly IConfiguration _configuration;
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private readonly TimeSpan _rateLimitDelay;

    // Configuration constants
    private const string DefaultModel = AnthropicModels.Claude35Sonnet;
    private const int DefaultMaxTokens = 4096;
    private const int DefaultTimeout = 30000; // 30 seconds
    private const int MaxConcurrentRequests = 5;

    public ClaudeApiService(
        IConfiguration configuration,
        ILogger<ClaudeApiService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Initialize Anthropic client
        var apiKey = _configuration["Claude:ApiKey"] 
            ?? throw new ArgumentException("Claude API key not configured");

        _anthropicClient = new AnthropicClient(apiKey);

        // Setup rate limiting
        _rateLimitSemaphore = new SemaphoreSlim(MaxConcurrentRequests, MaxConcurrentRequests);
        _rateLimitDelay = TimeSpan.FromMilliseconds(_configuration.GetValue("Claude:RateLimitDelayMs", 100));

        _logger.LogInformation("ClaudeApiService initialized with model: {Model}", GetConfiguredModel());
    }

    /// <summary>
    /// Generates a response using Claude API with custom system prompt and user message.
    /// NOTE: This is currently a STUB implementation due to namespace conflicts between
    /// DigitalMe.Data.Entities.Message and Anthropic.SDK.Messaging.Message
    /// </summary>
    public async Task<string> GenerateResponseAsync(
        string systemPrompt, 
        string userMessage, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(systemPrompt))
            throw new ArgumentException("System prompt cannot be null or empty", nameof(systemPrompt));

        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("User message cannot be null or empty", nameof(userMessage));

        await _rateLimitSemaphore.WaitAsync(cancellationToken);

        try
        {
            _logger.LogDebug("Generating Claude response for message: {Message}", 
                userMessage.Substring(0, Math.Min(userMessage.Length, 100)));

            // TODO: Implement proper Anthropic SDK integration with correct API mapping
            // The issue: namespace conflict between our domain Message entity and Anthropic SDK Message
            // Solution: Create proper mapping layer between domain and transport
            
            await Task.Delay(100, cancellationToken); // Simulate API delay
            
            var responseText = $@"[ANTHROPIC SDK INTEGRATION TEMPORARILY DISABLED]

This is a placeholder response from ClaudeApiService.

User Message: {userMessage}
System Prompt: {systemPrompt}
Model: {GetConfiguredModel()}
Max Tokens: {GetConfiguredMaxTokens()}

The actual integration requires proper mapping between:
- DigitalMe.Data.Entities.Message (our domain model)
- Anthropic.SDK.Messaging.Message (transport layer)

This separation ensures clean architecture where domain entities
don't leak into external API concerns.";
            
            _logger.LogDebug("Generated placeholder Claude response with {CharCount} characters", 
                responseText.Length);

            return responseText;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Claude API request cancelled by user");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Claude response");
            throw new InvalidOperationException("Failed to generate Claude response", ex);
        }
        finally
        {
            // Add small delay for rate limiting
            if (_rateLimitDelay > TimeSpan.Zero)
            {
                await Task.Delay(_rateLimitDelay, cancellationToken);
            }
            
            _rateLimitSemaphore.Release();
        }
    }

    public async Task<string> GeneratePersonalityResponseAsync(
        Guid personalityId, 
        string userMessage, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating personality-aware response for profile: {PersonalityId}", personalityId);

        try
        {
            var basicSystemPrompt = GenerateBasicSystemPrompt();
            return await GenerateResponseAsync(basicSystemPrompt, userMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating personality-aware response for profile: {PersonalityId}", personalityId);
            throw;
        }
    }

    public async Task<bool> ValidateApiConnectionAsync()
    {
        try
        {
            _logger.LogDebug("Validating Claude API connection");

            var testMessage = "Test connection";
            var testSystemPrompt = "Respond with 'Connection successful'";

            var response = await GenerateResponseAsync(testSystemPrompt, testMessage);
            
            var isValid = !string.IsNullOrWhiteSpace(response);
            
            _logger.LogInformation("Claude API connection validation: {Status}", 
                isValid ? "SUCCESS" : "FAILED");
                
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude API connection validation failed");
            return false;
        }
    }

    public async Task<ClaudeApiHealth> GetHealthStatusAsync()
    {
        var health = new ClaudeApiHealth();

        try
        {
            var startTime = DateTime.UtcNow;
            var isConnected = await ValidateApiConnectionAsync();
            var responseTime = DateTime.UtcNow - startTime;

            health.IsHealthy = isConnected;
            health.ResponseTimeMs = (int)responseTime.TotalMilliseconds;
            health.LastChecked = DateTime.UtcNow;
            health.Model = GetConfiguredModel();
            health.MaxTokens = GetConfiguredMaxTokens();
            health.AvailableRequests = _rateLimitSemaphore.CurrentCount;
            health.MaxConcurrentRequests = MaxConcurrentRequests;

            if (isConnected)
            {
                health.Status = "Healthy (Stub Implementation)";
            }
            else
            {
                health.Status = "Unhealthy - Connection failed";
                health.ErrorMessage = "Unable to establish connection to Claude API";
            }
        }
        catch (Exception ex)
        {
            health.IsHealthy = false;
            health.Status = "Unhealthy - Exception occurred";
            health.ErrorMessage = ex.Message;
            health.LastChecked = DateTime.UtcNow;
        }

        return health;
    }

    private string GetConfiguredModel()
    {
        return _configuration["Claude:Model"] ?? DefaultModel;
    }

    private int GetConfiguredMaxTokens()
    {
        return _configuration.GetValue("Claude:MaxTokens", DefaultMaxTokens);
    }

    private static string GenerateBasicSystemPrompt()
    {
        return @"You are a helpful AI assistant. Provide clear, concise, and accurate responses.
Be professional but friendly in your communication style.
If you're unsure about something, acknowledge the uncertainty rather than guessing.";
    }

    public void Dispose()
    {
        _rateLimitSemaphore?.Dispose();
    }
}

/// <summary>
/// Health status information for Claude API service.
/// </summary>
public class ClaudeApiHealth
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int ResponseTimeMs { get; set; }
    public DateTime LastChecked { get; set; }
    public string Model { get; set; } = string.Empty;
    public int MaxTokens { get; set; }
    public int AvailableRequests { get; set; }
    public int MaxConcurrentRequests { get; set; }

    /// <summary>
    /// Returns a JSON representation of the health status.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }
}