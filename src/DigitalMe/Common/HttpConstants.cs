namespace DigitalMe.Common;

/// <summary>
/// Константы для HTTP клиентов - устраняет дублирование magic strings и чисел в ServiceCollectionExtensions
/// Заменяет 17+ дублирований TimeSpan.FromSeconds(30) и "DigitalMe/1.0"
/// </summary>
public static class HttpConstants
{
    /// <summary>
    /// Стандартный таймаут для HTTP запросов (30 секунд)
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Увеличенный таймаут для долгих операций (120 секунд)
    /// </summary>
    public static readonly TimeSpan ExtendedTimeout = TimeSpan.FromSeconds(120);

    /// <summary>
    /// User-Agent для всех HTTP клиентов DigitalMe
    /// </summary>
    public const string UserAgent = "DigitalMe/1.0";

    /// <summary>
    /// Стандартная задержка для retry политик
    /// </summary>
    public static readonly TimeSpan StandardRetryDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Увеличенная задержка для чувствительных API (CAPTCHA, etc.)
    /// </summary>
    public static readonly TimeSpan ExtendedRetryDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Стандартное количество попыток повтора
    /// </summary>
    public const int StandardRetryCount = 3;

    /// <summary>
    /// Конфигурация пулов соединений для различных сервисов
    /// </summary>
    public static class ConnectionPools
    {
        /// <summary>
        /// Консервативный пул для API с жёсткими лимитами (Slack, 2captcha)
        /// </summary>
        public const int Conservative = 5;

        /// <summary>
        /// Стандартный пул для большинства API
        /// </summary>
        public const int Standard = 10;

        /// <summary>
        /// Сбалансированный пул для средних нагрузок
        /// </summary>
        public const int Balanced = 15;

        /// <summary>
        /// Высокопроизводительный пул для API с хорошими лимитами (GitHub)
        /// </summary>
        public const int HighThroughput = 20;
    }
}