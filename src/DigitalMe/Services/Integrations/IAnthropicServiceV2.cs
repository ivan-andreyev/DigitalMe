using DigitalMe.Models.Integrations;

namespace DigitalMe.Services.Integrations;

/// <summary>
/// Сервис для работы с Anthropic API с поддержкой динамической конфигурации API ключей.
/// Версия 2: Использует IApiConfigurationService для получения ключей и IApiUsageTracker для отслеживания использования.
/// </summary>
public interface IAnthropicServiceV2
{
    /// <summary>
    /// Отправляет сообщение в Anthropic API с динамической конфигурацией.
    /// </summary>
    /// <param name="prompt">Текст запроса к модели.</param>
    /// <param name="userId">Идентификатор пользователя для получения персонального API ключа (null = системный ключ).</param>
    /// <param name="systemPrompt">Системный промпт для установки контекста (опционально).</param>
    /// <returns>Ответ от Anthropic API с информацией об использовании токенов.</returns>
    Task<AnthropicResponse> SendMessageAsync(string prompt, string? userId = null, string? systemPrompt = null);

    /// <summary>
    /// Проверяет подключение к Anthropic API с использованием API ключа пользователя или системного.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя (null = системный ключ).</param>
    /// <returns>True если API доступен и ключ валиден.</returns>
    Task<bool> IsConnectedAsync(string? userId = null);
}