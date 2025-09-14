namespace DigitalMe.Services;

/// <summary>
/// Simple MVP message processor interface
/// User input → Ivan personality response pipeline
/// </summary>
public interface IMvpMessageProcessor
{
    /// <summary>
    /// Process user message and return Ivan's personality-aware response
    /// </summary>
    Task<string> ProcessMessageAsync(string userMessage);
}
