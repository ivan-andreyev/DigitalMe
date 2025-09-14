using Microsoft.Extensions.Logging;
using DigitalMe.Infrastructure;
using DigitalMe.Services.FileProcessing;

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

    public async Task<FileProcessingResult> ExecuteAsync(FileProcessingCommand command)
    {
        try
        {
            _logger.LogInformation("Executing file processing workflow with content: {ContentPreview}...", 
                command.Content.Substring(0, Math.Min(50, command.Content.Length)));

            // Step 1: Create temporary file through repository (infrastructure abstraction)
            var tempFile = await _fileRepository.CreateTemporaryFileAsync(".pdf");
            
            var parameters = new Dictionary<string, object> 
            { 
                ["content"] = command.Content, 
                ["title"] = command.Title ?? "Test Document" 
            };
            
            // Step 2: Create PDF using business service
            var pdfResult = await _fileProcessingService.ProcessPdfAsync("create", tempFile.FilePath, parameters);
            if (!pdfResult.Success)
            {
                await _fileRepository.DeleteFileAsync(tempFile.FileId);
                return new FileProcessingResult(
                    Success: false,
                    PdfCreated: false,
                    TextExtracted: false,
                    ContentMatch: false,
                    FileId: null,
                    ExtractedTextPreview: null,
                    ErrorMessage: $"PDF creation failed: {pdfResult.Message}");
            }

            // Step 3: Extract text back
            var extractedText = await _fileProcessingService.ExtractTextAsync(tempFile.FilePath);
            if (string.IsNullOrEmpty(extractedText))
            {
                return new FileProcessingResult(
                    Success: false,
                    PdfCreated: true,
                    TextExtracted: false,
                    ContentMatch: false,
                    FileId: tempFile.FileId,
                    ExtractedTextPreview: null,
                    ErrorMessage: "Text extraction failed: No text extracted");
            }

            // Step 4: Verify content matches
            var contentMatch = extractedText.Contains(command.Content.Substring(0, Math.Min(20, command.Content.Length)));

            return new FileProcessingResult(
                Success: true,
                PdfCreated: pdfResult.Success,
                TextExtracted: !string.IsNullOrEmpty(extractedText),
                ContentMatch: contentMatch,
                FileId: tempFile.FileId,
                ExtractedTextPreview: extractedText.Substring(0, Math.Min(100, extractedText.Length)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File processing workflow failed");
            return new FileProcessingResult(
                Success: false,
                PdfCreated: false,
                TextExtracted: false,
                ContentMatch: false,
                FileId: null,
                ExtractedTextPreview: null,
                ErrorMessage: $"Workflow failed: {ex.Message}");
        }
    }
}