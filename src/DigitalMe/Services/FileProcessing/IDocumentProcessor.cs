namespace DigitalMe.Services.FileProcessing;

/// <summary>
/// Focused interface for document processing operations
/// Handles PDF and Excel file processing
/// Following Interface Segregation Principle
/// </summary>
public interface IDocumentProcessor
{
    /// <summary>
    /// Process PDF files - read, extract text, or create new PDFs
    /// </summary>
    /// <param name="operation">PDF operation type (read, extract, create)</param>
    /// <param name="filePath">Path to the PDF file</param>
    /// <param name="parameters">Additional parameters for processing</param>
    /// <returns>Result of PDF processing operation</returns>
    Task<FileProcessingResult> ProcessPdfAsync(string operation, string filePath, Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Process Excel files - read, modify, or create spreadsheets
    /// </summary>
    /// <param name="operation">Excel operation type (read, write, create)</param>
    /// <param name="filePath">Path to the Excel file</param>
    /// <param name="parameters">Additional parameters for processing</param>
    /// <returns>Result of Excel processing operation</returns>
    Task<FileProcessingResult> ProcessExcelAsync(string operation, string filePath, Dictionary<string, object>? parameters = null);
}