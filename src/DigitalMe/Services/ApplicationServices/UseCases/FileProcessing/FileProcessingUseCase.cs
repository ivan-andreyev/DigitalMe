using DigitalMe.Common;
using DigitalMe.Infrastructure;
using DigitalMe.Services.FileProcessing;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.ApplicationServices.UseCases.FileProcessing;

/// <summary>
/// Implementation of file processing use case.
/// Coordinates file operations without direct infrastructure dependencies.
/// </summary>
public class FileProcessingUseCase : IFileProcessingUseCase
{
    private readonly IFileRepository _fileRepository;
    private readonly IFileProcessingService _fileProcessingService;
    private readonly ILogger<FileProcessingUseCase> _logger;

    public FileProcessingUseCase(
        IFileRepository fileRepository,
        IFileProcessingService fileProcessingService,
        ILogger<FileProcessingUseCase> logger)
    {
        _fileRepository = fileRepository;
        _fileProcessingService = fileProcessingService;
        _logger = logger;
    }

    public async Task<Result<FileProcessingResult>> ExecuteAsync(FileProcessingCommand command)
    {
        return await ResultExtensions.TryAsync(async () =>
        {
            _logger.LogInformation("Executing file processing workflow with content: {ContentPreview}...",
                command.content.Substring(0, Math.Min(50, command.content.Length)));

            // Step 1: Create temporary file through repository (infrastructure abstraction)
            var tempFile = await _fileRepository.CreateTemporaryFileAsync(".pdf");

            var parameters = new Dictionary<string, object>
            {
                ["content"] = command.content,
                ["title"] = command.title ?? "Test Document"
            };

            // Step 2: Create PDF using business service
            var pdfResult = await _fileProcessingService.ProcessPdfAsync("create", tempFile.filePath, parameters);
            if (!pdfResult.Success)
            {
                await _fileRepository.DeleteFileAsync(tempFile.fileId);
                return new FileProcessingResult(
                    success: false,
                    pdfCreated: false,
                    textExtracted: false,
                    contentMatch: false,
                    fileId: null,
                    extractedTextPreview: null,
                    errorMessage: $"PDF creation failed: {pdfResult.Message}");
            }

            // Step 3: Extract text back
            var extractedText = await _fileProcessingService.ExtractTextAsync(tempFile.filePath);
            if (string.IsNullOrEmpty(extractedText))
            {
                return new FileProcessingResult(
                    success: false,
                    pdfCreated: true,
                    textExtracted: false,
                    contentMatch: false,
                    fileId: tempFile.fileId,
                    extractedTextPreview: null,
                    errorMessage: "Text extraction failed: No text extracted");
            }

            // Step 4: Verify content matches
            var contentMatch = extractedText.Contains(command.content.Substring(0, Math.Min(20, command.content.Length)));

            return new FileProcessingResult(
                success: true,
                pdfCreated: pdfResult.Success,
                textExtracted: !string.IsNullOrEmpty(extractedText),
                contentMatch: contentMatch,
                fileId: tempFile.fileId,
                extractedTextPreview: extractedText.Substring(0, Math.Min(100, extractedText.Length)));
        }, $"File processing workflow failed for content: {command.content.Substring(0, Math.Min(30, command.content.Length))}");
    }
}