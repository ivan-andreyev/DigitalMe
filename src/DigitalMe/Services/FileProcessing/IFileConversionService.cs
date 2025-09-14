namespace DigitalMe.Services.FileProcessing;

/// <summary>
/// Service for file format conversion operations following Single Responsibility Principle
/// Part of Clean Architecture compliance - focused business service
/// </summary>
public interface IFileConversionService
{
    /// <summary>
    /// Convert files between different formats
    /// </summary>
    /// <param name="inputPath">Source file path</param>
    /// <param name="outputPath">Destination file path</param>
    /// <param name="targetFormat">Target format for conversion</param>
    /// <returns>Result of conversion operation</returns>
    Task<FileProcessingResult> ConvertFileAsync(string inputPath, string outputPath, string targetFormat);
}