namespace DigitalMe.Services.FileProcessing;

/// <summary>
/// Service for Excel-specific operations following Single Responsibility Principle
/// Part of Clean Architecture compliance - focused business service
/// </summary>
public interface IExcelProcessingService
{
    /// <summary>
    /// Process Excel files - read, modify, or create spreadsheets
    /// </summary>
    /// <param name="operation">Excel operation type (read, write, create)</param>
    /// <param name="filePath">Path to the Excel file</param>
    /// <param name="parameters">Additional parameters for processing</param>
    /// <returns>Result of Excel processing operation</returns>
    Task<FileProcessingResult> ProcessExcelAsync(string operation, string filePath, Dictionary<string, object>? parameters = null);
}