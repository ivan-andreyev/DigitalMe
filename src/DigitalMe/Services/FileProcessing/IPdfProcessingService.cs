namespace DigitalMe.Services.FileProcessing;

/// <summary>
/// Service for PDF-specific operations following Single Responsibility Principle
/// Part of Clean Architecture compliance - focused business service
/// </summary>
public interface IPdfProcessingService
{
    /// <summary>
    /// Process PDF files - read, extract text, or create new PDFs
    /// </summary>
    /// <param name="operation">PDF operation type (read, extract, create)</param>
    /// <param name="filePath">Path to the PDF file</param>
    /// <param name="parameters">Additional parameters for processing</param>
    /// <returns>Result of PDF processing operation</returns>
    Task<FileProcessingResult> ProcessPdfAsync(string operation, string filePath, Dictionary<string, object>? parameters = null);
}