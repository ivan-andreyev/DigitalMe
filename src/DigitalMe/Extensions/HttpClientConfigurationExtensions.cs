using DigitalMe.Common;
using DigitalMe.Services.Resilience;
using Polly;
using Polly.Extensions.Http;

namespace DigitalMe.Extensions;

/// <summary>
/// Расширения для унификации конфигурации HTTP клиентов
/// Устраняет дублирование в ServiceCollectionExtensions - 5+ идентичных блоков конфигурации
/// Заменяет повторяющиеся паттерны Timeout, User-Agent, MaxConnectionsPerServer, AddPolicyHandler
/// </summary>
public static class HttpClientConfigurationExtensions
{
    /// <summary>
    /// Применяет стандартную конфигурацию HTTP клиента с resilience политиками
    /// </summary>
    /// <param name="builder">HTTP client builder</param>
    /// <param name="maxConnectionsPerServer">Максимальное количество соединений на сервер</param>
    /// <param name="serviceName">Имя сервиса для resilience политик</param>
    /// <param name="timeout">Таймаут (по умолчанию 30 секунд)</param>
    /// <returns>Сконфигурированный HTTP client builder</returns>
    public static IHttpClientBuilder ConfigureStandardHttpClient(
        this IHttpClientBuilder builder,
        int maxConnectionsPerServer = HttpConstants.ConnectionPools.Standard,
        string? serviceName = null,
        TimeSpan? timeout = null)
    {
        return builder
            .ConfigureHttpClient(client =>
            {
                client.Timeout = timeout ?? HttpConstants.DefaultTimeout;
                client.DefaultRequestHeaders.Add("User-Agent", HttpConstants.UserAgent);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                MaxConnectionsPerServer = maxConnectionsPerServer
            })
            .AddStandardResiliencePolicy(serviceName);
    }

    /// <summary>
    /// Применяет стандартную конфигурацию для долгих операций (увеличенный таймаут)
    /// </summary>
    /// <param name="builder">HTTP client builder</param>
    /// <param name="maxConnectionsPerServer">Максимальное количество соединений на сервер</param>
    /// <param name="serviceName">Имя сервиса для resilience политик</param>
    /// <returns>Сконфигурированный HTTP client builder</returns>
    public static IHttpClientBuilder ConfigureExtendedTimeoutHttpClient(
        this IHttpClientBuilder builder,
        int maxConnectionsPerServer = HttpConstants.ConnectionPools.Conservative,
        string? serviceName = null)
    {
        return builder.ConfigureStandardHttpClient(
            maxConnectionsPerServer,
            serviceName,
            HttpConstants.ExtendedTimeout);
    }

    /// <summary>
    /// Добавляет стандартную resilience политику с retry и circuit breaker
    /// </summary>
    /// <param name="builder">HTTP client builder</param>
    /// <param name="serviceName">Имя сервиса для получения политик</param>
    /// <returns>HTTP client builder с resilience политиками</returns>
    public static IHttpClientBuilder AddStandardResiliencePolicy(
        this IHttpClientBuilder builder,
        string? serviceName = null)
    {
        return builder.AddPolicyHandler((serviceProvider, request) =>
        {
            var resilienceService = serviceProvider.GetService<IResiliencePolicyService>();

            if (resilienceService != null && !string.IsNullOrEmpty(serviceName))
            {
                return resilienceService.GetCombinedPolicy(serviceName);
            }

            // Fallback к стандартной политике если сервис недоступен
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    HttpConstants.StandardRetryCount,
                    _ => HttpConstants.StandardRetryDelay);
        });
    }

    /// <summary>
    /// Добавляет расширенную resilience политику для чувствительных API
    /// </summary>
    /// <param name="builder">HTTP client builder</param>
    /// <param name="serviceName">Имя сервиса для получения политик</param>
    /// <param name="retryDelay">Задержка между попытками (по умолчанию увеличенная)</param>
    /// <returns>HTTP client builder с расширенными resilience политиками</returns>
    public static IHttpClientBuilder AddExtendedResiliencePolicy(
        this IHttpClientBuilder builder,
        string? serviceName = null,
        TimeSpan? retryDelay = null)
    {
        return builder.AddPolicyHandler((serviceProvider, request) =>
        {
            var resilienceService = serviceProvider.GetService<IResiliencePolicyService>();

            if (resilienceService != null && !string.IsNullOrEmpty(serviceName))
            {
                return resilienceService.GetCombinedPolicy(serviceName);
            }

            // Fallback к расширенной политике для чувствительных API
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    HttpConstants.StandardRetryCount,
                    _ => retryDelay ?? HttpConstants.ExtendedRetryDelay);
        });
    }

    /// <summary>
    /// Конфигурация для API с консервативными лимитами (Slack, CAPTCHA сервисы)
    /// </summary>
    /// <param name="builder">HTTP client builder</param>
    /// <param name="serviceName">Имя сервиса</param>
    /// <returns>HTTP client builder для консервативных API</returns>
    public static IHttpClientBuilder ConfigureConservativeHttpClient(
        this IHttpClientBuilder builder,
        string serviceName)
    {
        return builder.ConfigureStandardHttpClient(
            HttpConstants.ConnectionPools.Conservative,
            serviceName)
            .AddExtendedResiliencePolicy(serviceName);
    }

    /// <summary>
    /// Конфигурация для высокопроизводительных API (GitHub)
    /// </summary>
    /// <param name="builder">HTTP client builder</param>
    /// <param name="serviceName">Имя сервиса</param>
    /// <returns>HTTP client builder для высокопроизводительных API</returns>
    public static IHttpClientBuilder ConfigureHighThroughputHttpClient(
        this IHttpClientBuilder builder,
        string serviceName)
    {
        return builder.ConfigureStandardHttpClient(
            HttpConstants.ConnectionPools.HighThroughput,
            serviceName);
    }

    /// <summary>
    /// Конфигурация для сбалансированных API (ClickUp)
    /// </summary>
    /// <param name="builder">HTTP client builder</param>
    /// <param name="serviceName">Имя сервиса</param>
    /// <returns>HTTP client builder для сбалансированных API</returns>
    public static IHttpClientBuilder ConfigureBalancedHttpClient(
        this IHttpClientBuilder builder,
        string serviceName)
    {
        return builder.ConfigureStandardHttpClient(
            HttpConstants.ConnectionPools.Balanced,
            serviceName);
    }
}