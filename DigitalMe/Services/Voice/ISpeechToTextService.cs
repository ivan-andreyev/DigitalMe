using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Voice;

/// <summary>
/// Core interface for Speech-to-Text transcription operations
/// Handles audio transcription from various sources
/// Following Interface Segregation Principle (≤5 methods)
/// </summary>
public interface ISpeechToTextService
{
    /// <summary>
    /// Converts speech to text using OpenAI STT API
    /// </summary>
    /// <param name="audioData">Audio data to transcribe</param>
    /// <param name="options">STT options including language and format</param>
    /// <returns>Speech recognition result with transcribed text</returns>
    Task<VoiceResult> SpeechToTextAsync(byte[] audioData, SttOptions? options = null);

    /// <summary>
    /// Converts speech to text from audio file
    /// </summary>
    /// <param name="audioFilePath">Path to audio file</param>
    /// <param name="options">STT options including language and format</param>
    /// <returns>Speech recognition result with transcribed text</returns>
    Task<VoiceResult> SpeechToTextFromFileAsync(string audioFilePath, SttOptions? options = null);

    /// <summary>
    /// Converts speech to text from stream
    /// </summary>
    /// <param name="audioStream">Audio stream to transcribe</param>
    /// <param name="fileName">Original filename for format detection</param>
    /// <param name="options">STT options including language and format</param>
    /// <returns>Speech recognition result with transcribed text</returns>
    Task<VoiceResult> SpeechToTextFromStreamAsync(Stream audioStream, string fileName, SttOptions? options = null);
}

/// <summary>
/// Support interface for Speech-to-Text configuration and validation
/// Handles format validation, cost estimation, and configuration
/// Following Interface Segregation Principle (≤5 methods)
/// </summary>
public interface ISpeechToTextConfigurationService
{
    /// <summary>
    /// Gets supported audio formats for STT
    /// </summary>
    /// <returns>List of supported audio formats</returns>
    Task<VoiceResult> GetSupportedAudioFormatsAsync();

    /// <summary>
    /// Validates audio format for STT processing
    /// </summary>
    /// <param name="audioData">Audio data to validate</param>
    /// <param name="fileName">Filename for format detection</param>
    /// <returns>Validation result</returns>
    Task<VoiceResult> ValidateAudioFormatAsync(byte[] audioData, string fileName);

    /// <summary>
    /// Estimates cost for STT operation
    /// </summary>
    /// <param name="audioData">Audio data to estimate cost for</param>
    /// <returns>Cost estimation result</returns>
    Task<VoiceResult> EstimateSttCostAsync(byte[] audioData);
}