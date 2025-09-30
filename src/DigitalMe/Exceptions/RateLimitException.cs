namespace DigitalMe.Exceptions;

/// <summary>
/// Исключение, возникающее при превышении лимита запросов к API.
/// </summary>
public class RateLimitException : Exception
{
    /// <summary>
    /// Время в секундах до следующей попытки.
    /// </summary>
    public int? RetryAfterSeconds { get; }

    public RateLimitException(string message) : base(message)
    {
    }

    public RateLimitException(string message, int retryAfterSeconds) : base(message)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }

    public RateLimitException(string message, Exception innerException) : base(message, innerException)
    {
    }
}