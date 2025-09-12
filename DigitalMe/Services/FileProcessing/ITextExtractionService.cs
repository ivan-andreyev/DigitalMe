namespace DigitalMe.Services.FileProcessing;

/// <summary>
/// Service for text extraction operations following Single Responsibility Principle
/// Part of Clean Architecture compliance - focused business service
/// </summary>
public interface ITextExtractionService
{
    /// <summary>
    /// Extract text from various document formats (PDF, Word, Excel, etc.)
    /// </summary>
    /// <param name="filePath">Path to the document file</param>
    /// <returns>Extracted text content</returns>
    Task<string> ExtractTextAsync(string filePath);
}