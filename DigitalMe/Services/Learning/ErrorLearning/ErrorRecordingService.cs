using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DigitalMe.Services.Learning.ErrorLearning.Models;
using DigitalMe.Services.Learning.ErrorLearning.Repositories;

namespace DigitalMe.Services.Learning.ErrorLearning;

/// <summary>
/// Service for recording and initial classification of errors
/// Implements SRP by focusing only on error recording operations
/// </summary>
public class ErrorRecordingService : IErrorRecordingService
{
    private readonly ILogger<ErrorRecordingService> _logger;
    private readonly IErrorPatternRepository _errorPatternRepository;
    private readonly ILearningHistoryRepository _learningHistoryRepository;

    public ErrorRecordingService(
        ILogger<ErrorRecordingService> logger,
        IErrorPatternRepository errorPatternRepository,
        ILearningHistoryRepository learningHistoryRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorPatternRepository = errorPatternRepository ?? throw new ArgumentNullException(nameof(errorPatternRepository));
        _learningHistoryRepository = learningHistoryRepository ?? throw new ArgumentNullException(nameof(learningHistoryRepository));
    }

    /// <inheritdoc />
    public async Task<LearningHistoryEntry> RecordErrorAsync(ErrorRecordingRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.Source))
            throw new ArgumentException("Source cannot be null or empty", nameof(request));

        if (string.IsNullOrWhiteSpace(request.ErrorMessage))
            throw new ArgumentException("ErrorMessage cannot be null or empty", nameof(request));

        try
        {
            _logger.LogInformation("Recording error from {Source}: {ErrorMessage}", request.Source, request.ErrorMessage);

            // Create learning history entry
            var learningEntry = new LearningHistoryEntry
            {
                Timestamp = DateTime.UtcNow,
                Source = request.Source,
                ErrorMessage = request.ErrorMessage,
                TestCaseName = request.TestCaseName,
                ApiName = request.ApiName,
                RequestDetails = request.RequestDetails,
                ResponseDetails = request.ResponseDetails,
                StackTrace = request.StackTrace,
                EnvironmentContext = request.EnvironmentContext,
                IsAnalyzed = false,
                ContributedToPattern = false,
                ConfidenceScore = 0.0
            };

            // Try to find existing pattern for immediate classification - using fast-return pattern
            var patternHash = GeneratePatternHash(request.ErrorMessage, request.ApiEndpoint, request.HttpMethod, request.HttpStatusCode);
            var existingPattern = await _errorPatternRepository.GetByPatternHashAsync(patternHash);
            
            if (existingPattern == null)
            {
                // Create new pattern if this looks like a new error type
                var newPattern = CreateNewErrorPattern(
                    patternHash, request.ErrorMessage, request.ApiEndpoint, request.HttpMethod, request.HttpStatusCode);
                
                var createdPattern = await _errorPatternRepository.CreateAsync(newPattern);
                learningEntry.ErrorPatternId = createdPattern.Id;
                learningEntry.ConfidenceScore = newPattern.ConfidenceScore;
            }
            else
            {
                // Update existing pattern
                existingPattern.OccurrenceCount++;
                existingPattern.LastObserved = DateTime.UtcNow;
                existingPattern.ConfidenceScore = CalculateConfidenceScore(existingPattern.OccurrenceCount);
                
                learningEntry.ErrorPatternId = existingPattern.Id;
                learningEntry.ContributedToPattern = true;
                learningEntry.ConfidenceScore = existingPattern.ConfidenceScore;

                await _errorPatternRepository.UpdateAsync(existingPattern);
            }

            var savedEntry = await _learningHistoryRepository.CreateAsync(learningEntry);
            _logger.LogDebug("Error recorded with ID: {LearningEntryId}", savedEntry.Id);

            return savedEntry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record error from {Source}", request.Source);
            throw;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Generates a unique hash for error pattern identification
    /// </summary>
    private string GeneratePatternHash(string errorMessage, string? apiEndpoint, string? httpMethod, int? httpStatusCode)
    {
        var normalizedError = NormalizeErrorMessage(errorMessage);
        var hashInput = $"{normalizedError}|{apiEndpoint}|{httpMethod}|{httpStatusCode}";
        
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
        return Convert.ToBase64String(hashBytes)[..16]; // Use first 16 chars for readability
    }

    /// <summary>
    /// Normalizes error message for pattern matching
    /// </summary>
    private string NormalizeErrorMessage(string errorMessage)
    {
        // Remove timestamps, IDs, and other variable content
        var normalized = errorMessage.ToLowerInvariant();
        
        // Remove common variable patterns
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}", "[timestamp]");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\b[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}\b", "[guid]");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\b\d+\b", "[number]");
        
        return normalized;
    }

    /// <summary>
    /// Creates a new error pattern from the first occurrence
    /// </summary>
    private ErrorPattern CreateNewErrorPattern(
        string patternHash, string errorMessage, string? apiEndpoint, string? httpMethod, int? httpStatusCode)
    {
        var category = ClassifyErrorCategory(errorMessage, httpStatusCode);
        var severity = CalculateSeverityLevel(errorMessage, httpStatusCode);

        return new ErrorPattern
        {
            PatternHash = patternHash,
            Category = category.Category,
            Subcategory = category.Subcategory,
            Description = GeneratePatternDescription(errorMessage, apiEndpoint, httpMethod),
            HttpStatusCode = httpStatusCode,
            ApiEndpoint = apiEndpoint,
            HttpMethod = httpMethod,
            OccurrenceCount = 1,
            FirstObserved = DateTime.UtcNow,
            LastObserved = DateTime.UtcNow,
            SeverityLevel = severity,
            ConfidenceScore = CalculateConfidenceScore(1)
        };
    }

    /// <summary>
    /// Classifies error into category and subcategory
    /// </summary>
    private (string Category, string Subcategory) ClassifyErrorCategory(string errorMessage, int? httpStatusCode)
    {
        var lowerMessage = errorMessage.ToLowerInvariant();

        // HTTP-specific classification
        if (httpStatusCode.HasValue)
        {
            return httpStatusCode.Value switch
            {
                400 => ("HTTP", "BadRequest"),
                401 => ("HTTP", "Unauthorized"),
                403 => ("HTTP", "Forbidden"), 
                404 => ("HTTP", "NotFound"),
                429 => ("HTTP", "RateLimited"),
                500 => ("HTTP", "InternalServerError"),
                502 => ("HTTP", "BadGateway"),
                503 => ("HTTP", "ServiceUnavailable"),
                _ => ("HTTP", "Unknown")
            };
        }

        // Content-based classification
        if (lowerMessage.Contains("timeout"))
        {
            return ("Network", "Timeout");
        }

        if (lowerMessage.Contains("connection") && (lowerMessage.Contains("refused") || lowerMessage.Contains("failed")))
        {
            return ("Network", "ConnectionFailure");
        }

        if (lowerMessage.Contains("authentication") || lowerMessage.Contains("unauthorized"))
        {
            return ("Security", "Authentication");
        }

        if (lowerMessage.Contains("permission") || lowerMessage.Contains("access denied"))
        {
            return ("Security", "Authorization");
        }

        if (lowerMessage.Contains("parse") || lowerMessage.Contains("format"))
        {
            return ("Data", "ParseError");
        }

        if (lowerMessage.Contains("validation") || lowerMessage.Contains("invalid"))
        {
            return ("Data", "ValidationError");
        }

        return ("General", "Unknown");
    }

    /// <summary>
    /// Calculates severity level based on error characteristics
    /// </summary>
    private int CalculateSeverityLevel(string errorMessage, int? httpStatusCode)
    {
        var lowerMessage = errorMessage.ToLowerInvariant();

        // Critical severity (Level 5)
        if (httpStatusCode >= 500 || lowerMessage.Contains("critical") || lowerMessage.Contains("fatal"))
        {
            return 5;
        }

        // High severity (Level 4)
        if (httpStatusCode == 429 || lowerMessage.Contains("timeout") || lowerMessage.Contains("connection"))
        {
            return 4;
        }

        // Medium severity (Level 3)
        if (httpStatusCode == 401 || httpStatusCode == 403 || lowerMessage.Contains("unauthorized") || lowerMessage.Contains("forbidden"))
        {
            return 3;
        }

        // Low severity (Level 2)
        if (httpStatusCode == 400 || httpStatusCode == 404 || lowerMessage.Contains("validation") || lowerMessage.Contains("not found"))
        {
            return 2;
        }

        // Minimal severity (Level 1)
        return 1;
    }

    /// <summary>
    /// Generates pattern description from error details
    /// </summary>
    private string GeneratePatternDescription(string errorMessage, string? apiEndpoint, string? httpMethod)
    {
        var normalizedMessage = NormalizeErrorMessage(errorMessage);
        var truncatedMessage = normalizedMessage.Length > 100 ? normalizedMessage[..97] + "..." : normalizedMessage;

        if (!string.IsNullOrEmpty(apiEndpoint))
        {
            return $"{httpMethod ?? "HTTP"} {apiEndpoint}: {truncatedMessage}";
        }

        return truncatedMessage;
    }

    /// <summary>
    /// Calculates confidence score based on occurrence count
    /// </summary>
    private double CalculateConfidenceScore(int occurrenceCount)
    {
        // Simple confidence calculation - more occurrences = higher confidence
        // Max confidence of 0.95 to leave room for uncertainty
        return Math.Min(0.95, 0.3 + (occurrenceCount * 0.1));
    }

    #endregion
}