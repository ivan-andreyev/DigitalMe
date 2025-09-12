namespace DigitalMe.Services.FileProcessing;

/// <summary>
/// Service for file validation operations following Single Responsibility Principle
/// Part of Clean Architecture compliance - focused business service
/// </summary>
public interface IFileValidationService
{
    /// <summary>
    /// Validate if file exists and is accessible for processing
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>True if file is accessible for processing</returns>
    Task<bool> IsFileAccessibleAsync(string filePath);
}