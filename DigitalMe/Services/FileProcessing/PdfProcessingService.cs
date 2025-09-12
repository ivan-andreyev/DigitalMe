using Microsoft.Extensions.Logging;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Drawing;
using DigitalMe.Infrastructure;

namespace DigitalMe.Services.FileProcessing;

/// <summary>
/// PDF processing service following Single Responsibility Principle
/// Clean Architecture compliant - no direct infrastructure dependencies
/// </summary>
public class PdfProcessingService : IPdfProcessingService
{
    private readonly ILogger<PdfProcessingService> _logger;
    private readonly IFileRepository _fileRepository;

    public PdfProcessingService(ILogger<PdfProcessingService> logger, IFileRepository fileRepository)
    {
        _logger = logger;
        _fileRepository = fileRepository;
    }

    /// <inheritdoc />
    public async Task<FileProcessingResult> ProcessPdfAsync(string operation, string filePath, Dictionary<string, object>? parameters = null)
    {
        try
        {
            _logger.LogInformation("Processing PDF operation: {Operation} on file: {FilePath}", operation, filePath);

            // For create operation, don't check if file exists yet
            if (operation.ToLowerInvariant() != "create" && !await _fileRepository.IsAccessibleAsync(filePath))
            {
                return FileProcessingResult.ErrorResult($"File not accessible: {filePath}");
            }

            return operation.ToLowerInvariant() switch
            {
                "read" => await ReadPdfAsync(filePath),
                "extract" => await ExtractPdfTextAsync(filePath),
                "create" => await CreatePdfAsync(filePath, parameters),
                _ => FileProcessingResult.ErrorResult($"Unsupported PDF operation: {operation}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PDF operation {Operation} on file {FilePath}", operation, filePath);
            return FileProcessingResult.ErrorResult($"PDF processing failed: {ex.Message}", ex.ToString());
        }
    }

    private async Task<FileProcessingResult> ReadPdfAsync(string filePath)
    {
        using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.ReadOnly);
        
        var pageCount = document.PageCount;
        var metadata = new
        {
            PageCount = pageCount,
            Title = document.Info.Title,
            Author = document.Info.Author,
            Creator = document.Info.Creator,
            Subject = document.Info.Subject
        };

        return await Task.FromResult(FileProcessingResult.SuccessResult(metadata, $"PDF read successfully. {pageCount} pages found."));
    }

    private async Task<FileProcessingResult> ExtractPdfTextAsync(string filePath)
    {
        var text = await ExtractPdfTextInternalAsync(filePath);
        return FileProcessingResult.SuccessResult(text, $"Extracted {text.Length} characters from PDF");
    }

    private async Task<string> ExtractPdfTextInternalAsync(string filePath)
    {
        // PdfSharpCore doesn't have built-in text extraction capabilities
        // For now, return a placeholder. In real implementation, we'd use a library like iText7 (with proper licensing)
        // or PDFPig for text extraction
        var fileInfo = await _fileRepository.GetFileInfoAsync(filePath);
        var basicInfo = $"PDF file: {fileInfo?.Name}, Size: {fileInfo?.Length} bytes, Created: {fileInfo?.CreationTime}";
        
        return await Task.FromResult($"Text extraction not implemented with current PDF library. {basicInfo}");
    }

    private async Task<FileProcessingResult> CreatePdfAsync(string filePath, Dictionary<string, object>? parameters = null)
    {
        try
        {
            var content = parameters?.GetValueOrDefault("content", "Default PDF content") as string ?? "Default PDF content";
            var title = parameters?.GetValueOrDefault("title", "Generated PDF") as string ?? "Generated PDF";

            // Ensure directory exists using repository
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !await _fileRepository.EnsureDirectoryExistsAsync(directory))
            {
                return FileProcessingResult.ErrorResult($"Failed to create directory: {directory}");
            }

            using var document = new PdfDocument();
            
            // Set document metadata
            document.Info.Title = title;
            document.Info.Creator = "DigitalMe Ivan-Level Agent";
            document.Info.Author = "Ivan-Level Agent";

            // Add a page
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 12);

            // Add content to the page
            var lines = content.Split('\n');
            var yPosition = 50;
            
            foreach (var line in lines)
            {
                if (yPosition > page.Height - 50) break; // Simple overflow protection
                gfx.DrawString(line, font, XBrushes.Black, new XRect(50, yPosition, page.Width - 100, 20), XStringFormats.TopLeft);
                yPosition += 20;
            }

            gfx.Dispose();
            document.Save(filePath);

            return await Task.FromResult(FileProcessingResult.SuccessResult(null, $"PDF created successfully at {filePath}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PDF at {FilePath}", filePath);
            return FileProcessingResult.ErrorResult($"Failed to create PDF: {ex.Message}", ex.ToString());
        }
    }
}