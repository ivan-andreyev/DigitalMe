using DigitalMe.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Infrastructure.Repositories;

/// <summary>
/// File system implementation of IFileRepository.
/// Handles infrastructure concerns for file operations.
/// Clean Architecture compliant - implements Infrastructure layer interface.
/// </summary>
public class FileSystemFileRepository : IFileRepository
{
    private readonly ILogger<FileSystemFileRepository> _logger;
    private readonly Dictionary<string, TemporaryFileInfo> _fileRegistry = new();

    public FileSystemFileRepository(ILogger<FileSystemFileRepository> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string filePath)
    {
        try
        {
            return await Task.FromResult(File.Exists(filePath));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking if file exists: {FilePath}", filePath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<FileInfo?> GetFileInfoAsync(string filePath)
    {
        try
        {
            if (!await ExistsAsync(filePath))
                return null;

            return await Task.FromResult(new FileInfo(filePath));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting file info: {FilePath}", filePath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> EnsureDirectoryExistsAsync(string directoryPath)
    {
        try
        {
            if (string.IsNullOrEmpty(directoryPath))
                return false;

            if (Directory.Exists(directoryPath))
                return true;

            Directory.CreateDirectory(directoryPath);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring directory exists: {DirectoryPath}", directoryPath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string> ReadAllTextAsync(string filePath)
    {
        try
        {
            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsAccessibleAsync(string filePath)
    {
        try
        {
            var fileInfo = await GetFileInfoAsync(filePath);
            return fileInfo != null && fileInfo.Length > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking file accessibility: {FilePath}", filePath);
            return false;
        }
    }

    /// <inheritdoc />
    public Task<TemporaryFileInfo> CreateTemporaryFileAsync(string extension)
    {
        var fileId = Guid.NewGuid().ToString();
        var tempFilePath = Path.GetTempFileName();
        
        // Rename with proper extension
        if (!string.IsNullOrEmpty(extension) && !extension.StartsWith('.'))
        {
            extension = "." + extension;
        }
        
        var finalPath = tempFilePath + extension;
        File.Move(tempFilePath, finalPath);
        
        var fileInfo = new TemporaryFileInfo(
            FileId: fileId,
            FilePath: finalPath,
            Extension: extension,
            CreatedAt: DateTime.UtcNow);
            
        _fileRegistry[fileId] = fileInfo;
        
        return Task.FromResult(fileInfo);
    }

    /// <inheritdoc />
    public Task DeleteFileAsync(string fileId)
    {
        if (_fileRegistry.TryGetValue(fileId, out var fileInfo))
        {
            try
            {
                if (File.Exists(fileInfo.FilePath))
                {
                    File.Delete(fileInfo.FilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deleting temporary file: {FilePath}", fileInfo.FilePath);
                // Don't throw - temporary file cleanup shouldn't fail the operation
            }
            finally
            {
                _fileRegistry.Remove(fileId);
            }
        }
        
        return Task.CompletedTask;
    }
}