namespace DigitalMe.Services.FileProcessing;

/// <summary>
/// Comprehensive file processing service interface.
/// Composes focused interfaces following Interface Segregation Principle.
/// Provides backward compatibility for existing consumers.
/// </summary>
public interface IFileProcessingService : 
    IDocumentProcessor,
    IFileUtilities
{
    // All methods inherited from focused interfaces
    // This maintains backward compatibility while allowing clients
    // to depend only on the specific capabilities they need
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