using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Audio;

namespace DigitalMe.Services.Voice;

/// <summary>
/// Implementation of IVoiceService providing Ivan-Level voice capabilities
/// Using OpenAI API for Text-to-Speech and Speech-to-Text operations
/// Following Clean Architecture patterns with dependency injection
/// Implements segregated interfaces following Interface Segregation Principle
/// </summary>
public class VoiceService : IVoiceService, ISpeechToTextService, ISpeechToTextConfigurationService
{
    private readonly ILogger<VoiceService> _logger;
    private readonly OpenAIClient _openAiClient;
    private readonly VoiceServiceConfig _config;

    public VoiceService(ILogger<VoiceService> logger, IOptions<VoiceServiceConfig> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(config);
        _config = config.Value;
        _openAiClient = new OpenAIClient(_config.OpenAiApiKey);
    }

    /// <summary>
    /// Converts text to speech using OpenAI TTS API
    /// </summary>
    public async Task<VoiceResult> TextToSpeechAsync(string text, TtsOptions? options = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return VoiceResult.ErrorResult("Text cannot be null or empty");
            }

            var ttsOptions = options ?? new TtsOptions();
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("Starting TTS operation for {CharCount} characters using voice {Voice}", 
                text.Length, ttsOptions.Voice);

            var audioClient = _openAiClient.GetAudioClient("tts-1");
            var response = await audioClient.GenerateSpeechAsync(
                text, 
                MapTtsVoice(ttsOptions.Voice));

            if (response?.Value != null)
            {
                var audioData = response.Value.ToArray();
                var processingTime = DateTime.UtcNow - startTime;
                var cost = EstimateTtsCost(text, ttsOptions.Voice);

                _logger.LogInformation("TTS operation completed in {ProcessingTime}ms, generated {AudioSize} bytes", 
                    processingTime.TotalMilliseconds, audioData.Length);

                return VoiceResult.SuccessResult(
                    data: audioData,
                    message: $"Text-to-speech completed successfully. Generated {audioData.Length} bytes of audio.",
                    processingTime: processingTime,
                    cost: cost,
                    format: ttsOptions.Format
                );
            }

            return VoiceResult.ErrorResult("Failed to generate speech from OpenAI API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during TTS operation: {ErrorMessage}", ex.Message);
            return VoiceResult.ErrorResult("TTS operation failed", ex.Message);
        }
    }

    /// <summary>
    /// Converts speech to text using OpenAI STT API
    /// </summary>
    public async Task<VoiceResult> SpeechToTextAsync(byte[] audioData, SttOptions? options = null)
    {
        try
        {
            if (audioData == null || audioData.Length == 0)
            {
                return VoiceResult.ErrorResult("Audio data cannot be null or empty");
            }

            var sttOptions = options ?? new SttOptions();
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("Starting STT operation for {AudioSize} bytes of audio data", audioData.Length);

            using var audioStream = new MemoryStream(audioData);
            var result = await ProcessSpeechToTextAsync(audioStream, "audio.wav", sttOptions);

            if (result.Success)
            {
                var processingTime = DateTime.UtcNow - startTime;
                result = VoiceResult.SuccessResult(
                    data: result.Data,
                    message: result.Message,
                    processingTime: processingTime,
                    cost: EstimateSttCost(audioData),
                    language: sttOptions.Language
                );

                _logger.LogInformation("STT operation completed in {ProcessingTime}ms", processingTime.TotalMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during STT operation: {ErrorMessage}", ex.Message);
            return VoiceResult.ErrorResult("STT operation failed", ex.Message);
        }
    }

    /// <summary>
    /// Converts speech to text from audio file
    /// </summary>
    public async Task<VoiceResult> SpeechToTextFromFileAsync(string audioFilePath, SttOptions? options = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(audioFilePath))
            {
                return VoiceResult.ErrorResult("Audio file path cannot be null or empty");
            }

            if (!File.Exists(audioFilePath))
            {
                return VoiceResult.ErrorResult($"Audio file not found: {audioFilePath}");
            }

            var sttOptions = options ?? new SttOptions();
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("Starting STT operation for file: {FilePath}", audioFilePath);

            using var audioStream = File.OpenRead(audioFilePath);
            var fileName = Path.GetFileName(audioFilePath);
            var result = await ProcessSpeechToTextAsync(audioStream, fileName, sttOptions);

            if (result.Success)
            {
                var processingTime = DateTime.UtcNow - startTime;
                result = VoiceResult.SuccessResult(
                    data: result.Data,
                    message: result.Message,
                    processingTime: processingTime,
                    cost: EstimateSttCostFromFile(audioFilePath),
                    language: sttOptions.Language
                );

                _logger.LogInformation("STT operation completed in {ProcessingTime}ms for file {FileName}", 
                    processingTime.TotalMilliseconds, fileName);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during STT file operation: {ErrorMessage}", ex.Message);
            return VoiceResult.ErrorResult("STT file operation failed", ex.Message);
        }
    }

    /// <summary>
    /// Converts speech to text from stream
    /// </summary>
    public async Task<VoiceResult> SpeechToTextFromStreamAsync(Stream audioStream, string fileName, SttOptions? options = null)
    {
        try
        {
            if (audioStream == null)
            {
                return VoiceResult.ErrorResult("Audio stream cannot be null");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return VoiceResult.ErrorResult("File name cannot be null or empty");
            }

            var sttOptions = options ?? new SttOptions();
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("Starting STT operation for stream with filename: {FileName}", fileName);

            var result = await ProcessSpeechToTextAsync(audioStream, fileName, sttOptions);

            if (result.Success)
            {
                var processingTime = DateTime.UtcNow - startTime;
                result = VoiceResult.SuccessResult(
                    data: result.Data,
                    message: result.Message,
                    processingTime: processingTime,
                    language: sttOptions.Language
                );

                _logger.LogInformation("STT stream operation completed in {ProcessingTime}ms", processingTime.TotalMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during STT stream operation: {ErrorMessage}", ex.Message);
            return VoiceResult.ErrorResult("STT stream operation failed", ex.Message);
        }
    }

    /// <summary>
    /// Gets available TTS voices from OpenAI
    /// </summary>
    public async Task<VoiceResult> GetAvailableVoicesAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving available TTS voices");

            var voices = new List<VoiceInfo>
            {
                new() { Voice = TtsVoice.Alloy, Name = "Alloy", Description = "Balanced and neutral voice", SuitableFor = new[] { "General content", "Professional narration" }, Gender = "Neutral", Accent = "American" },
                new() { Voice = TtsVoice.Echo, Name = "Echo", Description = "Clear and articulate voice", SuitableFor = new[] { "Educational content", "Instructions" }, Gender = "Male", Accent = "American" },
                new() { Voice = TtsVoice.Fable, Name = "Fable", Description = "Warm and engaging voice", SuitableFor = new[] { "Storytelling", "Creative content" }, Gender = "Male", Accent = "British" },
                new() { Voice = TtsVoice.Onyx, Name = "Onyx", Description = "Deep and authoritative voice", SuitableFor = new[] { "News", "Formal presentations" }, Gender = "Male", Accent = "American" },
                new() { Voice = TtsVoice.Nova, Name = "Nova", Description = "Energetic and youthful voice", SuitableFor = new[] { "Marketing", "Entertainment" }, Gender = "Female", Accent = "American" },
                new() { Voice = TtsVoice.Shimmer, Name = "Shimmer", Description = "Soft and pleasant voice", SuitableFor = new[] { "Meditation", "Relaxation content" }, Gender = "Female", Accent = "American" }
            };

            return VoiceResult.SuccessResult(voices, "Available voices retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available voices: {ErrorMessage}", ex.Message);
            return VoiceResult.ErrorResult("Failed to retrieve available voices", ex.Message);
        }
    }

    /// <summary>
    /// Gets supported audio formats for STT
    /// </summary>
    public async Task<VoiceResult> GetSupportedAudioFormatsAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving supported audio formats");

            var formats = new[]
            {
                AudioFormat.Mp3,
                AudioFormat.Mp4,
                AudioFormat.Mpeg,
                AudioFormat.Mpga,
                AudioFormat.M4a,
                AudioFormat.Wav,
                AudioFormat.Webm
            };

            return await Task.FromResult(VoiceResult.SuccessResult(formats, "Supported audio formats retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving supported formats: {ErrorMessage}", ex.Message);
            return VoiceResult.ErrorResult("Failed to retrieve supported formats", ex.Message);
        }
    }

    /// <summary>
    /// Validates audio format for STT processing
    /// </summary>
    public async Task<VoiceResult> ValidateAudioFormatAsync(byte[] audioData, string fileName)
    {
        try
        {
            if (audioData == null || audioData.Length == 0)
            {
                return VoiceResult.ErrorResult("Audio data cannot be null or empty");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return VoiceResult.ErrorResult("File name cannot be null or empty");
            }

            _logger.LogInformation("Validating audio format for file: {FileName}", fileName);

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var supportedFormats = new[] { AudioFormat.Mp3, AudioFormat.Mp4, AudioFormat.Mpeg, AudioFormat.Mpga, AudioFormat.M4a, AudioFormat.Wav, AudioFormat.Webm };
            var supportedExtensions = new[] { ".mp3", ".mp4", ".mpeg", ".mpga", ".m4a", ".wav", ".webm" };
            
            var isSupported = supportedExtensions.Contains(extension);
            var detectedFormat = extension switch
            {
                ".mp3" => AudioFormat.Mp3,
                ".mp4" => AudioFormat.Mp4,
                ".mpeg" => AudioFormat.Mpeg,
                ".mpga" => AudioFormat.Mpga,
                ".m4a" => AudioFormat.M4a,
                ".wav" => AudioFormat.Wav,
                ".webm" => AudioFormat.Webm,
                _ => AudioFormat.Mp3  // Default fallback for display purposes only
            };

            var validation = new AudioFormatValidation
            {
                IsValid = isSupported,
                DetectedFormat = detectedFormat,
                FileSize = audioData.Length,
                Issues = isSupported ? Array.Empty<string>() : new[] { $"Unsupported audio format: {extension}" },
                Recommendations = isSupported ? Array.Empty<string>() : new[] { "Convert to MP3, WAV, or M4A format" }
            };

            return await Task.FromResult(VoiceResult.SuccessResult(validation, 
                isSupported ? "Audio format validation successful" : "Audio format validation failed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating audio format: {ErrorMessage}", ex.Message);
            return VoiceResult.ErrorResult("Audio format validation failed", ex.Message);
        }
    }

    /// <summary>
    /// Estimates cost for TTS operation
    /// </summary>
    public async Task<VoiceResult> EstimateTtsCostAsync(string text, TtsVoice voice = TtsVoice.Alloy)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return VoiceResult.ErrorResult("Text cannot be null or empty");
            }

            _logger.LogInformation("Estimating TTS cost for {CharCount} characters", text.Length);

            var cost = EstimateTtsCost(text, voice);
            var estimatedDuration = TimeSpan.FromSeconds(text.Length * 0.1); // Rough estimate

            var estimation = new CostEstimation
            {
                EstimatedCost = cost,
                CharacterCount = text.Length,
                EstimatedDuration = estimatedDuration,
                Currency = "USD",
                PricingModel = "OpenAI TTS-1",
                EstimatedAt = DateTime.UtcNow
            };

            return await Task.FromResult(VoiceResult.SuccessResult(estimation, "TTS cost estimated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error estimating TTS cost: {ErrorMessage}", ex.Message);
            return VoiceResult.ErrorResult("TTS cost estimation failed", ex.Message);
        }
    }

    /// <summary>
    /// Estimates cost for STT operation
    /// </summary>
    public async Task<VoiceResult> EstimateSttCostAsync(byte[] audioData)
    {
        try
        {
            if (audioData == null || audioData.Length == 0)
            {
                return VoiceResult.ErrorResult("Audio data cannot be null or empty");
            }

            _logger.LogInformation("Estimating STT cost for {AudioSize} bytes", audioData.Length);

            var cost = EstimateSttCost(audioData);
            var estimatedDuration = TimeSpan.FromSeconds(audioData.Length / 16000); // Rough estimate based on sample rate

            var estimation = new CostEstimation
            {
                EstimatedCost = cost,
                CharacterCount = 0,
                EstimatedDuration = estimatedDuration,
                Currency = "USD",
                PricingModel = "OpenAI Whisper",
                EstimatedAt = DateTime.UtcNow
            };

            return await Task.FromResult(VoiceResult.SuccessResult(estimation, "STT cost estimated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error estimating STT cost: {ErrorMessage}", ex.Message);
            return VoiceResult.ErrorResult("STT cost estimation failed", ex.Message);
        }
    }

    /// <summary>
    /// Checks if voice service is available and properly configured
    /// </summary>
    public async Task<bool> IsServiceAvailableAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_config.OpenAiApiKey))
            {
                _logger.LogWarning("OpenAI API key is not configured");
                return false;
            }

            // Test with a simple TTS request
            var testResult = await TextToSpeechAsync("test", new TtsOptions { Voice = TtsVoice.Alloy });
            return testResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voice service availability check failed: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Gets service usage statistics
    /// </summary>
    public async Task<VoiceResult> GetServiceStatsAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving voice service statistics");

            // In a real implementation, this would come from a database or monitoring system
            var stats = new VoiceServiceStats
            {
                TotalTtsRequests = 0,
                TotalSttRequests = 0,
                TotalAudioGenerated = TimeSpan.Zero,
                TotalAudioTranscribed = TimeSpan.Zero,
                TotalCost = 0,
                LastActivity = DateTime.UtcNow,
                VoiceUsage = new Dictionary<TtsVoice, int>(),
                LanguageUsage = new Dictionary<string, int>()
            };

            return await Task.FromResult(VoiceResult.SuccessResult(stats, "Service statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving service statistics: {ErrorMessage}", ex.Message);
            return VoiceResult.ErrorResult("Failed to retrieve service statistics", ex.Message);
        }
    }

    #region Private Helper Methods

    private async Task<VoiceResult> ProcessSpeechToTextAsync(Stream audioStream, string fileName, SttOptions options)
    {
        try
        {
            var request = new AudioTranscriptionOptions
            {
                ResponseFormat = MapSttResponseFormat(options.ResponseFormat),
                Language = options.Language,
                Prompt = options.Prompt,
                Temperature = (float)options.Temperature
            };

            var audioClient = _openAiClient.GetAudioClient(options.Model);
            var response = await audioClient.TranscribeAudioAsync(audioStream, fileName, request);

            if (response?.Value != null)
            {
                var transcription = response.Value;
                string resultText;

                switch (options.ResponseFormat)
                {
                    case SttResponseFormat.Json:
                    case SttResponseFormat.VerboseJson:
                        resultText = transcription.Text ?? "";
                        break;
                    case SttResponseFormat.Text:
                        resultText = transcription.Text ?? "";
                        break;
                    case SttResponseFormat.Srt:
                    case SttResponseFormat.Vtt:
                        resultText = transcription.Text ?? "";
                        break;
                    default:
                        resultText = transcription.Text ?? "";
                        break;
                }

                return VoiceResult.SuccessResult(
                    data: resultText,
                    message: $"Speech-to-text completed successfully. Transcribed text length: {resultText.Length} characters."
                );
            }

            return VoiceResult.ErrorResult("Failed to transcribe audio from OpenAI API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing STT: {ErrorMessage}", ex.Message);
            return VoiceResult.ErrorResult("STT operation failed", ex.Message);
        }
    }

    private static GeneratedSpeechVoice MapTtsVoice(TtsVoice voice) => voice switch
    {
        TtsVoice.Alloy => GeneratedSpeechVoice.Alloy,
        TtsVoice.Echo => GeneratedSpeechVoice.Echo,
        TtsVoice.Fable => GeneratedSpeechVoice.Fable,
        TtsVoice.Onyx => GeneratedSpeechVoice.Onyx,
        TtsVoice.Nova => GeneratedSpeechVoice.Nova,
        TtsVoice.Shimmer => GeneratedSpeechVoice.Shimmer,
        _ => GeneratedSpeechVoice.Alloy
    };

    private static GeneratedSpeechFormat MapAudioFormat(AudioFormat format) => format switch
    {
        AudioFormat.Mp3 => GeneratedSpeechFormat.Mp3,
        AudioFormat.Opus => GeneratedSpeechFormat.Opus,
        AudioFormat.Aac => GeneratedSpeechFormat.Aac,
        AudioFormat.Flac => GeneratedSpeechFormat.Flac,
        AudioFormat.Pcm => GeneratedSpeechFormat.Pcm,
        _ => GeneratedSpeechFormat.Mp3
    };

    private static AudioTranscriptionFormat MapSttResponseFormat(SttResponseFormat format) => format switch
    {
        SttResponseFormat.Json => AudioTranscriptionFormat.Simple,
        SttResponseFormat.Text => AudioTranscriptionFormat.Simple,
        SttResponseFormat.Srt => AudioTranscriptionFormat.Srt,
        SttResponseFormat.VerboseJson => AudioTranscriptionFormat.Verbose,
        SttResponseFormat.Vtt => AudioTranscriptionFormat.Vtt,
        _ => AudioTranscriptionFormat.Simple
    };

    private static decimal EstimateTtsCost(string text, TtsVoice voice)
    {
        // OpenAI TTS pricing: $15.00 / 1M characters
        const decimal pricePerCharacter = 15.00m / 1_000_000;
        return text.Length * pricePerCharacter;
    }

    private static decimal EstimateSttCost(byte[] audioData)
    {
        // OpenAI STT pricing: $6.00 / hour
        // Rough estimate: 1MB ≈ 1 minute of audio
        const decimal pricePerHour = 6.00m;
        var estimatedMinutes = audioData.Length / (1024 * 1024); // MB
        return (estimatedMinutes / 60m) * pricePerHour;
    }

    private static decimal EstimateSttCostFromFile(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        return EstimateSttCost(new byte[fileInfo.Length]);
    }

    #endregion
}

/// <summary>
/// Configuration class for VoiceService
/// </summary>
public class VoiceServiceConfig
{
    public string OpenAiApiKey { get; set; } = string.Empty;
    public int DefaultTimeout { get; set; } = 30000;
    public bool EnableDetailedLogging { get; set; } = true;
}