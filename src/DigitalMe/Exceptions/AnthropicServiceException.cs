namespace DigitalMe.Exceptions;

/// <summary>
/// Исключение, возникающее при ошибках работы с Anthropic API.
/// </summary>
public class AnthropicServiceException : Exception
{
    /// <summary>
    /// HTTP status code ответа, если доступен.
    /// </summary>
    public int? StatusCode { get; }

    public AnthropicServiceException(string message) : base(message)
    {
    }

    public AnthropicServiceException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public AnthropicServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}