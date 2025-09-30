namespace DigitalMe.Services.Security;

/// <summary>
/// Интерфейс сервиса для валидации API ключей различных провайдеров.
/// Предоставляет методы валидации для разных AI провайдеров.
/// </summary>
public interface IApiKeyValidator
{
    /// <summary>
    /// Валидирует API ключ Anthropic путем тестового запроса к их API.
    /// </summary>
    /// <param name="apiKey">API ключ Anthropic для валидации.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <returns>Результат валидации с индикацией успеха или неудачи и опциональным сообщением об ошибке.</returns>
    Task<ApiKeyValidationResult> ValidateAnthropicKeyAsync(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Валидирует API ключ OpenAI путем тестового запроса к их API.
    /// </summary>
    /// <param name="apiKey">API ключ OpenAI для валидации.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <returns>Результат валидации с индикацией успеха или неудачи и опциональным сообщением об ошибке.</returns>
    Task<ApiKeyValidationResult> ValidateOpenAIKeyAsync(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Валидирует API ключ Google путем тестового запроса к их API.
    /// </summary>
    /// <param name="apiKey">API ключ Google для валидации.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <returns>Результат валидации с индикацией успеха или неудачи и опциональным сообщением об ошибке.</returns>
    Task<ApiKeyValidationResult> ValidateGoogleKeyAsync(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Валидирует API ключ для любого поддерживаемого провайдера.
    /// </summary>
    /// <param name="provider">Название провайдера (например, "Anthropic", "OpenAI", "Google").</param>
    /// <param name="apiKey">API ключ для валидации.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <returns>Результат валидации с индикацией успеха или неудачи и опциональным сообщением об ошибке.</returns>
    Task<ApiKeyValidationResult> ValidateKeyAsync(string provider, string apiKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// Результат попытки валидации API ключа.
/// </summary>
public record ApiKeyValidationResult
{
    /// <summary>
    /// Указывает, является ли API ключ валидным и рабочим.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Сообщение об ошибке, если валидация провалилась, null если успешна.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// HTTP статус код из запроса валидации, если применимо.
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>
    /// Длительность запроса валидации в миллисекундах.
    /// </summary>
    public long DurationMs { get; init; }

    /// <summary>
    /// Создает успешный результат валидации.
    /// </summary>
    /// <param name="durationMs">Длительность валидации в миллисекундах.</param>
    public static ApiKeyValidationResult Success(long durationMs) =>
        new() { IsValid = true, DurationMs = durationMs };

    /// <summary>
    /// Создает неуспешный результат валидации с сообщением об ошибке.
    /// </summary>
    /// <param name="errorMessage">Сообщение об ошибке.</param>
    /// <param name="statusCode">HTTP статус код, если применимо.</param>
    /// <param name="durationMs">Длительность валидации в миллисекундах.</param>
    public static ApiKeyValidationResult Failure(string errorMessage, int? statusCode = null, long durationMs = 0) =>
        new() { IsValid = false, ErrorMessage = errorMessage, StatusCode = statusCode, DurationMs = durationMs };
}