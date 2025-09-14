namespace DigitalMe.Services.FileProcessing;

/// <summary>
/// Focused interface for file utility operations
/// Handles text extraction, conversion, and validation
/// Following Interface Segregation Principle
/// </summary>
public interface IFileUtilities
{
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