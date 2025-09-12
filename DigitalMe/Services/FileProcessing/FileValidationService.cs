using Microsoft.Extensions.Logging;
using DigitalMe.Infrastructure;

namespace DigitalMe.Services.FileProcessing;

/// <summary>
/// File validation service following Single Responsibility Principle
/// Clean Architecture compliant - no direct infrastructure dependencies
/// </summary>
public class FileValidationService : IFileValidationService
{
    private readonly ILogger<FileValidationService> _logger;
    private readonly IFileRepository _fileRepository;

    public FileValidationService(ILogger<FileValidationService> logger, IFileRepository fileRepository)
    {
        _logger = logger;
        _fileRepository = fileRepository;
    }

    /// <inheritdoc />
    public async Task<bool> IsFileAccessibleAsync(string filePath)
    {
        try
        {
            return await _fileRepository.IsAccessibleAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking file accessibility for {FilePath}", filePath);
            return false;
        }
    }
}