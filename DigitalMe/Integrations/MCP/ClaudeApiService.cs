using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
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
    private const string DefaultModel = AnthropicModels.Claude3Sonnet;
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
    /// </summary>
    /// <param name="systemPrompt">System prompt for context and personality</param>
    /// <param name="userMessage">User's input message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Claude's response text</returns>
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

            var messages = new List<Message>
            {
                new(RoleType.User, userMessage)
            };

            var request = new MessageRequest
            {
                Model = GetConfiguredModel(),
                MaxTokens = GetConfiguredMaxTokens(),
                System = systemPrompt,
                Messages = messages
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(DefaultTimeout);

            var response = await _anthropicClient.Messages.CreateAsync(request, cts.Token);

            if (response?.Content?.FirstOrDefault()?.Text == null)
            {
                _logger.LogWarning("Received empty response from Claude API");
                throw new InvalidOperationException("Claude API returned empty response");
            }

            var responseText = response.Content.First().Text;
            
            _logger.LogDebug("Successfully generated Claude response with {CharCount} characters", 
                responseText.Length);

            return responseText;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Claude API request cancelled by user");
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Claude API request timed out after {Timeout}ms", DefaultTimeout);
            throw new TimeoutException($"Claude API request timed out after {DefaultTimeout}ms");
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

    /// <summary>
    /// Generates a personality-aware response using the specified personality profile.
    /// </summary>
    /// <param name="personalityId">ID of the personality profile to use</param>
    /// <param name="userMessage">User's input message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Personality-aware response from Claude</returns>
    public async Task<string> GeneratePersonalityResponseAsync(
        Guid personalityId, 
        string userMessage, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating personality-aware response for profile: {PersonalityId}", personalityId);

        try
        {
            // Note: This method will need to be implemented once PersonalityService 
            // is updated to work with the new PersonalityProfile entity structure
            
            // For now, return a basic response with placeholder system prompt
            var basicSystemPrompt = GenerateBasicSystemPrompt();
            return await GenerateResponseAsync(basicSystemPrompt, userMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating personality-aware response for profile: {PersonalityId}", personalityId);
            throw;
        }
    }

    /// <summary>
    /// Validates the connection to Claude API.
    /// </summary>
    /// <returns>True if connection is successful</returns>
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

    /// <summary>
    /// Gets the current health status of the Claude API service.
    /// </summary>
    /// <returns>Health status information</returns>
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
                health.Status = "Healthy";
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

    /// <summary>
    /// Gets the configured Claude model from configuration.
    /// </summary>
    private string GetConfiguredModel()
    {
        return _configuration["Claude:Model"] ?? DefaultModel;
    }

    /// <summary>
    /// Gets the configured maximum tokens from configuration.
    /// </summary>
    private int GetConfiguredMaxTokens()
    {
        return _configuration.GetValue("Claude:MaxTokens", DefaultMaxTokens);
    }

    /// <summary>
    /// Generates a basic system prompt for fallback scenarios.
    /// </summary>
    private static string GenerateBasicSystemPrompt()
    {
        return @"You are a helpful AI assistant. Provide clear, concise, and accurate responses.
Be professional but friendly in your communication style.
If you're unsure about something, acknowledge the uncertainty rather than guessing.";
    }

    /// <summary>
    /// Disposes resources used by the service.
    /// </summary>
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