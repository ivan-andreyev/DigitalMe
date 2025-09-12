using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System.Text;
using DigitalMe.Infrastructure;

namespace DigitalMe.Services.FileProcessing;

/// <summary>
/// Text extraction service following Single Responsibility Principle
/// Clean Architecture compliant - no direct infrastructure dependencies
/// </summary>
public class TextExtractionService : ITextExtractionService
{
    private readonly ILogger<TextExtractionService> _logger;
    private readonly IFileRepository _fileRepository;

    public TextExtractionService(ILogger<TextExtractionService> logger, IFileRepository fileRepository)
    {
        _logger = logger;
        _fileRepository = fileRepository;
        
        // Set EPPlus license context (required for non-commercial use)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <inheritdoc />
    public async Task<string> ExtractTextAsync(string filePath)
    {
        try
        {
            if (!await _fileRepository.IsAccessibleAsync(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            return extension switch
            {
                ".pdf" => await ExtractPdfTextAsync(filePath),
                ".xlsx" or ".xls" => await ExtractExcelTextAsync(filePath),
                ".txt" => await _fileRepository.ReadAllTextAsync(filePath),
                _ => throw new NotSupportedException($"File format not supported for text extraction: {extension}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from file {FilePath}", filePath);
            throw;
        }
    }

    private async Task<string> ExtractPdfTextAsync(string filePath)
    {
        // PdfSharpCore doesn't have built-in text extraction capabilities
        // For now, return a placeholder. In real implementation, we'd use a library like iText7 (with proper licensing)
        // or PDFPig for text extraction
        var fileInfo = await _fileRepository.GetFileInfoAsync(filePath);
        var basicInfo = $"PDF file: {fileInfo?.Name}, Size: {fileInfo?.Length} bytes, Created: {fileInfo?.CreationTime}";
        
        return await Task.FromResult($"Text extraction not implemented with current PDF library. {basicInfo}");
    }

    private async Task<string> ExtractExcelTextAsync(string filePath)
    {
        var fileInfo = await _fileRepository.GetFileInfoAsync(filePath);
        if (fileInfo == null)
        {
            throw new FileNotFoundException($"Excel file not found: {filePath}");
        }

        using var package = new ExcelPackage(fileInfo);
        var text = new StringBuilder();
        
        foreach (var worksheet in package.Workbook.Worksheets)
        {
            text.AppendLine($"Worksheet: {worksheet.Name}");
            
            if (worksheet.Dimension != null)
            {
                for (int row = worksheet.Dimension.Start.Row; row <= worksheet.Dimension.End.Row; row++)
                {
                    var rowText = new List<string>();
                    for (int col = worksheet.Dimension.Start.Column; col <= worksheet.Dimension.End.Column; col++)
                    {
                        var cellValue = worksheet.Cells[row, col].Value?.ToString() ?? "";
                        rowText.Add(cellValue);
                    }
                    text.AppendLine(string.Join("\t", rowText));
                }
            }
            text.AppendLine();
        }

        return await Task.FromResult(text.ToString());
    }
}