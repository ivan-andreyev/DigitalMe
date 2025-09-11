namespace DigitalMe.Services.FileProcessing;

/// <summary>
/// Service for handling file processing operations including PDF, Excel, and document manipulation
/// Following Clean Architecture patterns for file operations
/// </summary>
public interface IFileProcessingService
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

    /// <summary>
    /// Extract text from various document formats (PDF, Word, Excel, etc.)
    /// </summary>
    /// <param name="filePath">Path to the document file</param>
    /// <returns>Extracted text content</returns>
    Task<string> ExtractTextAsync(string filePath);

    /// <summary>
    /// Convert files between different formats
    /// </summary>
    /// <param name="inputPath">Source file path</param>
    /// <param name="outputPath">Destination file path</param>
    /// <param name="targetFormat">Target format for conversion</param>
    /// <returns>Result of conversion operation</returns>
    Task<FileProcessingResult> ConvertFileAsync(string inputPath, string outputPath, string targetFormat);

    /// <summary>
    /// Validate if file exists and is accessible for processing
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>True if file is accessible for processing</returns>
    Task<bool> IsFileAccessibleAsync(string filePath);
}

/// <summary>
/// Result object for file processing operations
/// </summary>
public class FileProcessingResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
    public string? ErrorDetails { get; set; }
    
    public static FileProcessingResult SuccessResult(object? data = null, string? message = null)
    {
        return new FileProcessingResult
        {
            Success = true,
            Data = data,
            Message = message
        };
    }
    
    public static FileProcessingResult ErrorResult(string message, string? errorDetails = null)
    {
        return new FileProcessingResult
        {
            Success = false,
            Message = message,
            ErrorDetails = errorDetails
        };
    }
}