# Core Personality Service

**Родительский план**: [../02-05-interfaces.md](../02-05-interfaces.md)

## IPersonalityService Interface
**Файл**: `src/DigitalMe.Core/Interfaces/IPersonalityService.cs:1-25`

```csharp
public interface IPersonalityService
{
    /// <summary>
    /// Получить профиль личности по имени
    /// </summary>
    /// <param name="name">Имя профиля (например "Ivan")</param>
    /// <returns>Профиль личности с загруженными traits</returns>
    Task<PersonalityProfile?> GetPersonalityAsync(string name);
    
    /// <summary>
    /// Обработать сообщение с учетом контекста личности
    /// </summary>
    /// <param name="message">Входящее сообщение пользователя</param>
    /// <param name="context">Контекст диалога и платформы</param>
    /// <returns>Обработанный ответ в стиле личности</returns>
    Task<PersonalityResponse> ProcessMessageAsync(string message, PersonalityContext context);
    
    /// <summary>
    /// Обновить черту личности
    /// </summary>
    /// <param name="profileId">ID профиля</param>
    /// <param name="traitName">Название черты (например "communication_style")</param>
    /// <param name="value">Новое значение черты</param>
    Task UpdatePersonalityTraitAsync(Guid profileId, string traitName, object value);
    
    /// <summary>
    /// Получить системный промпт для LLM на основе профиля
    /// </summary>
    /// <param name="profileName">Имя профиля</param>
    /// <param name="conversationHistory">История последних 10 сообщений</param>
    /// <returns>Системный промпт с контекстом личности</returns>
    Task<string> GenerateSystemPromptAsync(string profileName, IEnumerable<Message> conversationHistory);
    
    /// <summary>
    /// Проанализировать эмоциональное состояние на основе сообщения
    /// </summary>
    /// <param name="message">Сообщение для анализа</param>
    /// <returns>Эмоциональное состояние (Focused, Tired, Irritated, Calm)</returns>
    Task<PersonalityMood> AnalyzeMoodFromMessageAsync(string message);
}
```

## PersonalityService Implementation
**Файл**: `src/DigitalMe.Core/Services/PersonalityService.cs:1-150`

```csharp
public class PersonalityService : IPersonalityService
{
    private readonly IPersonalityRepository _repository;
    private readonly ILogger<PersonalityService> _logger;
    private readonly IMemoryCache _cache;
    
    public PersonalityService(
        IPersonalityRepository repository,
        ILogger<PersonalityService> logger,
        IMemoryCache cache)
    {
        _repository = repository;
        _logger = logger;
        _cache = cache;
    }
    
    public async Task<PersonalityProfile?> GetPersonalityAsync(string name)
    {
        // Cache check - line 25-30
        var cacheKey = $"personality_{name}";
        if (_cache.TryGetValue(cacheKey, out PersonalityProfile? cached))
        {
            _logger.LogDebug("Personality profile {Name} loaded from cache", name);
            return cached;
        }
        
        // Database lookup - line 31-40
        try
        {
            var profile = await _repository.GetByNameAsync(name);
            if (profile == null)
            {
                _logger.LogWarning("Personality profile {Name} not found", name);
                return null;
            }
            
            // Cache for 1 hour - line 41-45
            _cache.Set(cacheKey, profile, TimeSpan.FromHours(1));
            _logger.LogInformation("Personality profile {Name} loaded and cached", name);
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load personality profile {Name}", name);
            throw new PersonalityServiceException($"Failed to load profile: {name}", ex);
        }
    }
    
    public async Task<PersonalityResponse> ProcessMessageAsync(string message, PersonalityContext context)
    {
        // Input validation - line 55-65
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty", nameof(message));
        
        if (context?.Profile == null)
            throw new ArgumentNullException(nameof(context), "PersonalityContext.Profile is required");
            
        // Analyze message sentiment - line 66-75  
        var mood = await AnalyzeMoodFromMessageAsync(message);
        context.CurrentMood = mood;
        
        // Generate contextual response - line 76-90
        try 
        {
            var systemPrompt = await GenerateSystemPromptAsync(context.Profile.Name, context.RecentMessages);
            
            var response = new PersonalityResponse
            {
                Content = await GenerateResponseContentAsync(message, systemPrompt, context),
                Mood = mood,
                Confidence = CalculateConfidence(message, context),
                ProcessedAt = DateTime.UtcNow,
                TokensUsed = await EstimateTokenUsageAsync(message, systemPrompt)
            };
            
            _logger.LogInformation("Processed message for {Profile} with confidence {Confidence:F2}", 
                context.Profile.Name, response.Confidence);
                
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message for profile {Profile}", context.Profile.Name);
            throw new PersonalityServiceException("Message processing failed", ex);
        }
    }

    // Additional helper methods would be implemented here
    private async Task<string> GenerateResponseContentAsync(string message, string systemPrompt, PersonalityContext context)
    {
        // TODO: Implementation for response generation
        throw new NotImplementedException("Architecture stub - implement in development phase");
    }
    
    private double CalculateConfidence(string message, PersonalityContext context)
    {
        // TODO: Implementation for confidence calculation
        throw new NotImplementedException("Architecture stub - implement in development phase");
    }
    
    private async Task<int> EstimateTokenUsageAsync(string message, string systemPrompt)
    {
        // TODO: Implementation for token usage estimation
        throw new NotImplementedException("Architecture stub - implement in development phase");
    }
}
```

## PersonalityResponse Model
**Файл**: `src/DigitalMe.Core/Models/PersonalityResponse.cs:1-20`

```csharp
namespace DigitalMe.Core.Models;

public class PersonalityResponse
{
    public string Content { get; set; } = default!;
    public PersonalityMood Mood { get; set; }
    public double Confidence { get; set; } // 0.0 - 1.0
    public DateTime ProcessedAt { get; set; }
    public int TokensUsed { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class PersonalityContext
{
    public PersonalityProfile Profile { get; set; } = default!;
    public IEnumerable<Message> RecentMessages { get; set; } = Enumerable.Empty<Message>();
    public PersonalityMood? CurrentMood { get; set; }
    public string Platform { get; set; } = "Web"; // Web, Telegram, Mobile
    public Dictionary<string, object> SessionData { get; set; } = new();
}
```