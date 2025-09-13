using Microsoft.Extensions.Logging;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using DigitalMe.Infrastructure;

namespace DigitalMe.Services.FileProcessing;

/// <summary>
/// File conversion service following Single Responsibility Principle
/// Clean Architecture compliant - no direct infrastructure dependencies
/// </summary>
public class FileConversionService : IFileConversionService
{
    private readonly ILogger<FileConversionService> _logger;
    private readonly IFileRepository _fileRepository;

    public FileConversionService(ILogger<FileConversionService> logger, IFileRepository fileRepository)
    {
        _logger = logger;
        _fileRepository = fileRepository;
    }

    /// <inheritdoc />
    public async Task<FileProcessingResult> ConvertFileAsync(string inputPath, string outputPath, string targetFormat)
    {
        try
        {
            _logger.LogInformation("Converting file from {InputPath} to {OutputPath} in format {TargetFormat}", 
                inputPath, outputPath, targetFormat);

            if (!await _fileRepository.IsAccessibleAsync(inputPath))
            {
                return FileProcessingResult.ErrorResult($"Input file not accessible: {inputPath}");
            }

            // Basic conversion logic - can be extended based on needs
            var inputExtension = Path.GetExtension(inputPath).ToLowerInvariant();
            var targetExtension = targetFormat.ToLowerInvariant().StartsWith('.') ? targetFormat : $".{targetFormat}";

            if (inputExtension == ".txt" && targetExtension == ".pdf")
            {
                return await ConvertTextToPdfAsync(inputPath, outputPath);
            }

            return FileProcessingResult.ErrorResult($"Conversion from {inputExtension} to {targetExtension} not supported yet");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting file from {InputPath} to {OutputPath}", inputPath, outputPath);
            return FileProcessingResult.ErrorResult($"File conversion failed: {ex.Message}", ex.ToString());
        }
    }

    private async Task<FileProcessingResult> ConvertTextToPdfAsync(string inputPath, string outputPath)
    {
        try
        {
            var textContent = await _fileRepository.ReadAllTextAsync(inputPath);
            
            // Ensure directory exists using repository
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !await _fileRepository.EnsureDirectoryExistsAsync(directory))
            {
                return FileProcessingResult.ErrorResult($"Failed to create directory: {directory}");
            }
            
            using var document = new PdfDocument();
            document.Info.Title = $"Converted from {Path.GetFileName(inputPath)}";
            document.Info.Creator = "DigitalMe Ivan-Level Agent";

            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 12);

            var lines = textContent.Split('\n');
            var yPosition = 50;
            
            foreach (var line in lines)
            {
                if (yPosition > page.Height - 50)
                {
                    break;
                }
                gfx.DrawString(line, font, XBrushes.Black, new XRect(50, yPosition, page.Width - 100, 20), XStringFormats.TopLeft);
                yPosition += 20;
            }

            gfx.Dispose();
            document.Save(outputPath);

            return FileProcessingResult.SuccessResult(null, $"Successfully converted {inputPath} to {outputPath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting {InputPath} to {OutputPath}", inputPath, outputPath);
            return FileProcessingResult.ErrorResult($"File conversion failed: {ex.Message}", ex.ToString());
        }
    }
}