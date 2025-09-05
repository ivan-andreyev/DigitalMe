using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using DigitalMe.Models;
using DigitalMe.Services;
using System.Text.Json;
using System.Text;

namespace DigitalMe.Integrations.MCP;

public interface IAnthropicService
{
    Task<string> SendMessageAsync(string message, PersonalityProfile? personality = null);
    Task<bool> IsConnectedAsync();
}

public class AnthropicConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeyEnvironmentVariable { get; set; } = "ANTHROPIC_API_KEY";
    public string Model { get; set; } = "claude-3-5-sonnet-20241022";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public string BaseUrl { get; set; } = "https://api.anthropic.com";
}

public class AnthropicServiceSimple : IAnthropicService
{
    private readonly HttpClient _httpClient;
    private readonly AnthropicConfiguration _config;
    private readonly ILogger<AnthropicServiceSimple> _logger;
    private readonly IIvanPersonalityService _ivanPersonalityService;

    public AnthropicServiceSimple(HttpClient httpClient, IOptions<AnthropicConfiguration> config, ILogger<AnthropicServiceSimple> logger, IIvanPersonalityService ivanPersonalityService)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
        _ivanPersonalityService = ivanPersonalityService;

        // Try to get API key from environment variable if not set in config
        var apiKey = GetApiKey();
        
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        }
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    private string GetApiKey()
    {
        // First try config
        if (!string.IsNullOrEmpty(_config.ApiKey))
            return _config.ApiKey;

        // Then try environment variable
        var envKey = Environment.GetEnvironmentVariable(_config.ApiKeyEnvironmentVariable);
        if (!string.IsNullOrEmpty(envKey))
        {
            _logger.LogInformation("Using Anthropic API key from environment variable: {Variable}", _config.ApiKeyEnvironmentVariable);
            return envKey;
        }

        _logger.LogWarning("Anthropic API key not found in config or environment variable: {Variable}", _config.ApiKeyEnvironmentVariable);
        return string.Empty;
    }

    public async Task<string> SendMessageAsync(string message, PersonalityProfile? personality = null)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Anthropic API key not configured. Using fallback response.");
            return await GenerateFallbackResponseAsync(message, personality);
        }

        try
        {
            _logger.LogInformation("Sending message to Anthropic API: {Message}", message.Substring(0, Math.Min(100, message.Length)));

            var systemPrompt = await GenerateSystemPromptAsync(personality);
            
            var request = new
            {
                model = _config.Model,
                max_tokens = 1000,
                system = systemPrompt,
                messages = new[]
                {
                    new { role = "user", content = message }
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("v1/messages", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseText = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseText);
                
                if (responseData.TryGetProperty("content", out var contentArray) &&
                    contentArray.GetArrayLength() > 0 &&
                    contentArray[0].TryGetProperty("text", out var textElement))
                {
                    var result = textElement.GetString() ?? "Empty response";
                    _logger.LogInformation("Received response from Anthropic API, length: {Length}", result.Length);
                    return result;
                }
            }
            else
            {
                _logger.LogWarning("Anthropic API returned {StatusCode}: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                var errorText = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Error details: {ErrorText}", errorText);
            }

            return await GenerateFallbackResponseAsync(message, personality);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to Anthropic API");
            return await GenerateFallbackResponseAsync(message, personality);
        }
    }

    public async Task<bool> IsConnectedAsync()
    {
        if (string.IsNullOrEmpty(_config.ApiKey))
        {
            return false;
        }

        try
        {
            // Simple connectivity test
            var request = new
            {
                model = _config.Model,
                max_tokens = 10,
                messages = new[]
                {
                    new { role = "user", content = "Hi" }
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("v1/messages", content);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> GenerateSystemPromptAsync(PersonalityProfile? personality)
    {
        // Always use Ivan's real personality data
        var ivanProfile = await _ivanPersonalityService.GetIvanPersonalityAsync();
        var systemPrompt = _ivanPersonalityService.GenerateSystemPrompt(ivanProfile);
        
        _logger.LogInformation("Generated system prompt for Ivan's personality with {TraitCount} traits", ivanProfile.Traits?.Count ?? 0);
        return systemPrompt;
    }

    private async Task<string> GenerateFallbackResponseAsync(string message, PersonalityProfile? personality)
    {
        // Use Ivan's personality even in fallback responses
        var ivanProfile = await _ivanPersonalityService.GetIvanPersonalityAsync();
        
        var responses = new[]
        {
            "Слушай, тут без API ключа от Claude нормально не поработаешь. Настрой конфиг как положено - я же Head of R&D, должен все работать правильно.",
            "API недоступен, но по твоему вопросу могу сказать - нужно разбираться в деталях. У меня 4 года опыта в программировании, так что знаю о чём говорю.",
            "Проблема с подключением к Claude. Проверь настройки и ключ API. Я из военного перешёл в IT, так что с инфраструктурой разбираюсь.",
            "Хочешь нормальный ответ - настрой интеграцию с Anthropic API как положено. Я работаю за троих минимум, но без инструментов ничего не сделать.",
            "Без доступа к Claude могу только сказать: структурируй проблему, определи факторы, взвесь их и решай. Это мой подход к любым решениям."
        };

        var random = new Random();
        var response = responses[random.Next(responses.Length)];
        
        _logger.LogInformation("Generated fallback response in Ivan's personality style");
        return response;
    }
}