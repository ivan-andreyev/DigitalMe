using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Voice;

/// <summary>
/// Service for voice operations including Text-to-Speech and Speech-to-Text
/// Provides Ivan-Level voice capabilities using OpenAI API
/// Following Clean Architecture patterns with dependency injection
/// </summary>
public interface IVoiceService
{
    /// <summary>
    /// Converts text to speech using OpenAI TTS API
    /// </summary>
    /// <param name="text">Text to convert to speech</param>
    /// <param name="options">TTS options including voice, format, and speed</param>
    /// <returns>Voice synthesis result with audio data</returns>
    Task<VoiceResult> TextToSpeechAsync(string text, TtsOptions? options = null);

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

    /// <summary>
    /// Gets available TTS voices from OpenAI
    /// </summary>
    /// <returns>List of available voices</returns>
    Task<VoiceResult> GetAvailableVoicesAsync();

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
    /// Estimates cost for TTS operation
    /// </summary>
    /// <param name="text">Text to estimate cost for</param>
    /// <param name="voice">Voice to use</param>
    /// <returns>Cost estimation result</returns>
    Task<VoiceResult> EstimateTtsCostAsync(string text, TtsVoice voice = TtsVoice.Alloy);

    /// <summary>
    /// Estimates cost for STT operation
    /// </summary>
    /// <param name="audioData">Audio data to estimate cost for</param>
    /// <returns>Cost estimation result</returns>
    Task<VoiceResult> EstimateSttCostAsync(byte[] audioData);

    /// <summary>
    /// Checks if voice service is available and properly configured
    /// </summary>
    /// <returns>True if service is ready, false otherwise</returns>
    Task<bool> IsServiceAvailableAsync();

    /// <summary>
    /// Gets service usage statistics
    /// </summary>
    /// <returns>Service usage statistics</returns>
    Task<VoiceResult> GetServiceStatsAsync();
}

/// <summary>
/// Result object for voice operations
/// Contains operation success status, audio/text data, and metadata
/// </summary>
public class VoiceResult
{
    public bool Success { get; init; }
    public object? Data { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ErrorDetails { get; init; }
    public TimeSpan? ProcessingTime { get; init; }
    public decimal? Cost { get; init; }
    public string? Language { get; init; }
    public AudioFormat? Format { get; init; }

    /// <summary>
    /// Creates a successful result with data and metadata
    /// </summary>
    /// <param name="data">Operation result data</param>
    /// <param name="message">Success message</param>
    /// <param name="processingTime">Time taken to process</param>
    /// <param name="cost">Cost of the operation</param>
    /// <param name="language">Language used</param>
    /// <param name="format">Audio format</param>
    /// <returns>Successful VoiceResult</returns>
    public static VoiceResult SuccessResult(object? data = null, string message = "Operation completed successfully",
        TimeSpan? processingTime = null, decimal? cost = null, string? language = null, AudioFormat? format = null)
        => new() 
        { 
            Success = true, 
            Data = data, 
            Message = message, 
            ProcessingTime = processingTime, 
            Cost = cost, 
            Language = language, 
            Format = format 
        };

    /// <summary>
    /// Creates an error result with message and details
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="details">Detailed error information</param>
    /// <returns>Error VoiceResult</returns>
    public static VoiceResult ErrorResult(string message, string? details = null)
        => new() { Success = false, Message = message, ErrorDetails = details };
}

/// <summary>
/// Options for Text-to-Speech operations
/// </summary>
public class TtsOptions
{
    /// <summary>
    /// Voice to use for synthesis
    /// </summary>
    public TtsVoice Voice { get; set; } = TtsVoice.Alloy;

    /// <summary>
    /// Audio format for output
    /// </summary>
    public AudioFormat Format { get; set; } = AudioFormat.Mp3;

    /// <summary>
    /// Speech speed (0.25 to 4.0)
    /// </summary>
    public double Speed { get; set; } = 1.0;

    /// <summary>
    /// Target language for pronunciation (optional)
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Whether to optimize for real-time playback
    /// </summary>
    public bool OptimizeStreaming { get; set; } = false;

    /// <summary>
    /// Custom voice settings (advanced)
    /// </summary>
    public Dictionary<string, object>? CustomSettings { get; set; }
}

/// <summary>
/// Options for Speech-to-Text operations
/// </summary>
public class SttOptions
{
    /// <summary>
    /// Language of the audio (ISO 639-1 code)
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Prompt to guide the transcription
    /// </summary>
    public string? Prompt { get; set; }

    /// <summary>
    /// Response format for transcription
    /// </summary>
    public SttResponseFormat ResponseFormat { get; set; } = SttResponseFormat.Json;

    /// <summary>
    /// Temperature for randomness (0 to 1)
    /// </summary>
    public double Temperature { get; set; } = 0.0;

    /// <summary>
    /// Whether to include word-level timestamps
    /// </summary>
    public bool IncludeTimestamps { get; set; } = false;

    /// <summary>
    /// Whether to enable speaker detection
    /// </summary>
    public bool EnableSpeakerDetection { get; set; } = false;

    /// <summary>
    /// Custom model to use for transcription
    /// </summary>
    public string Model { get; set; } = "whisper-1";
}

/// <summary>
/// Available TTS voices from OpenAI
/// </summary>
public enum TtsVoice
{
    Alloy,
    Echo,
    Fable,
    Onyx,
    Nova,
    Shimmer
}

/// <summary>
/// Supported audio formats
/// </summary>
public enum AudioFormat
{
    Mp3,
    Opus,
    Aac,
    Flac,
    Wav,
    Pcm,
    M4a,
    Mp4,
    Mpeg,
    Mpga,
    Webm
}

/// <summary>
/// STT response format options
/// </summary>
public enum SttResponseFormat
{
    Json,
    Text,
    Srt,
    VerboseJson,
    Vtt
}

/// <summary>
/// Voice service usage statistics
/// </summary>
public class VoiceServiceStats
{
    public int TotalTtsRequests { get; set; }
    public int TotalSttRequests { get; set; }
    public TimeSpan TotalAudioGenerated { get; set; }
    public TimeSpan TotalAudioTranscribed { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime LastActivity { get; set; }
    public Dictionary<TtsVoice, int> VoiceUsage { get; set; } = new();
    public Dictionary<string, int> LanguageUsage { get; set; } = new();
}

/// <summary>
/// Available voice information
/// </summary>
public class VoiceInfo
{
    public TtsVoice Voice { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] SuitableFor { get; set; } = Array.Empty<string>();
    public string Gender { get; set; } = string.Empty;
    public string Accent { get; set; } = string.Empty;
}

/// <summary>
/// Audio format validation result
/// </summary>
public class AudioFormatValidation
{
    public bool IsValid { get; set; }
    public AudioFormat DetectedFormat { get; set; }
    public int SampleRate { get; set; }
    public int Channels { get; set; }
    public TimeSpan Duration { get; set; }
    public long FileSize { get; set; }
    public string[] Issues { get; set; } = Array.Empty<string>();
    public string[] Recommendations { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Cost estimation result
/// </summary>
public class CostEstimation
{
    public decimal EstimatedCost { get; set; }
    public int CharacterCount { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
    public string Currency { get; set; } = "USD";
    public string PricingModel { get; set; } = string.Empty;
    public DateTime EstimatedAt { get; set; } = DateTime.UtcNow;
}