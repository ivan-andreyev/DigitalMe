using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Voice;

/// <summary>
/// Focused interface for Text-to-Speech operations
/// Handles text synthesis and voice management
/// Following Interface Segregation Principle
/// </summary>
public interface ITextToSpeechService
{
    /// <summary>
    /// Converts text to speech using OpenAI TTS API
    /// </summary>
    /// <param name="text">Text to convert to speech</param>
    /// <param name="options">TTS options including voice, format, and speed</param>
    /// <returns>Voice synthesis result with audio data</returns>
    Task<VoiceResult> TextToSpeechAsync(string text, TtsOptions? options = null);

    /// <summary>
    /// Gets available TTS voices from OpenAI
    /// </summary>
    /// <returns>List of available voices</returns>
    Task<VoiceResult> GetAvailableVoicesAsync();

    /// <summary>
    /// Estimates cost for TTS operation
    /// </summary>
    /// <param name="text">Text to estimate cost for</param>
    /// <param name="voice">Voice to use</param>
    /// <returns>Cost estimation result</returns>
    Task<VoiceResult> EstimateTtsCostAsync(string text, TtsVoice voice = TtsVoice.Alloy);
}