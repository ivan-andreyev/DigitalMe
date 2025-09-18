namespace DigitalMe.Infrastructure;

/// <summary>
/// Repository interface for file system operations
/// Part of Clean Architecture - Infrastructure layer abstraction
/// Removes infrastructure dependencies from business services
/// Supports both basic file operations and temporary file management
/// </summary>
public interface IFileRepository
{
    /// <summary>
    /// Check if file exists in the file system
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>True if file exists</returns>
    Task<bool> ExistsAsync(string filePath);

    /// <summary>
    /// Get file information including size
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>File information or null if file doesn't exist</returns>
    Task<FileInfo?> GetFileInfoAsync(string filePath);

    /// <summary>
    /// Ensure directory exists, create if necessary
    /// </summary>
    /// <param name="directoryPath">Path to the directory</param>
    /// <returns>True if directory exists or was created successfully</returns>
    Task<bool> EnsureDirectoryExistsAsync(string directoryPath);

    /// <summary>
    /// Read all text from a file
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>File content as string</returns>
    Task<string> ReadAllTextAsync(string filePath);

    /// <summary>
    /// Read all bytes from a file
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>File content as byte array</returns>
    Task<byte[]> ReadAllBytesAsync(string filePath);

    /// <summary>
    /// Check if file is accessible for processing (exists and has content)
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>True if file is accessible</returns>
    Task<bool> IsAccessibleAsync(string filePath);

    /// <summary>
    /// Creates a temporary file with the specified extension
    /// </summary>
    /// <param name="extension">File extension (e.g., ".pdf", ".txt")</param>
    /// <returns>Temporary file information</returns>
    Task<TemporaryFileInfo> CreateTemporaryFileAsync(string extension);

    /// <summary>
    /// Deletes a file by its identifier
    /// </summary>
    /// <param name="fileId">Unique file identifier</param>
    Task DeleteFileAsync(string fileId);
}

/// <summary>
/// Information about a temporary file.
/// </summary>
public record TemporaryFileInfo(
    string fileId,
    string filePath,
    string extension,
    DateTime createdAt);