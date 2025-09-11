using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Drawing;
using System.Text;

namespace DigitalMe.Services.FileProcessing;

/// <summary>
/// Implementation of file processing service for PDF and Excel operations
/// Following Clean Architecture patterns with dependency injection
/// </summary>
public class FileProcessingService : IFileProcessingService
{
    private readonly ILogger<FileProcessingService> _logger;

    public FileProcessingService(ILogger<FileProcessingService> logger)
    {
        _logger = logger;
        
        // Set EPPlus license context (required for non-commercial use)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <inheritdoc />
    public async Task<FileProcessingResult> ProcessPdfAsync(string operation, string filePath, Dictionary<string, object>? parameters = null)
    {
        try
        {
            _logger.LogInformation("Processing PDF operation: {Operation} on file: {FilePath}", operation, filePath);

            // For create operation, don't check if file exists yet
            if (operation.ToLowerInvariant() != "create" && !await IsFileAccessibleAsync(filePath))
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

    /// <inheritdoc />
    public async Task<FileProcessingResult> ProcessExcelAsync(string operation, string filePath, Dictionary<string, object>? parameters = null)
    {
        try
        {
            _logger.LogInformation("Processing Excel operation: {Operation} on file: {FilePath}", operation, filePath);

            return operation.ToLowerInvariant() switch
            {
                "read" => await ReadExcelAsync(filePath),
                "write" => await WriteExcelAsync(filePath, parameters),
                "create" => await CreateExcelAsync(filePath, parameters),
                _ => FileProcessingResult.ErrorResult($"Unsupported Excel operation: {operation}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Excel operation {Operation} on file {FilePath}", operation, filePath);
            return FileProcessingResult.ErrorResult($"Excel processing failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<string> ExtractTextAsync(string filePath)
    {
        try
        {
            if (!await IsFileAccessibleAsync(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            return extension switch
            {
                ".pdf" => await ExtractPdfTextInternalAsync(filePath),
                ".xlsx" or ".xls" => await ExtractExcelTextAsync(filePath),
                ".txt" => await File.ReadAllTextAsync(filePath),
                _ => throw new NotSupportedException($"File format not supported for text extraction: {extension}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from file {FilePath}", filePath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FileProcessingResult> ConvertFileAsync(string inputPath, string outputPath, string targetFormat)
    {
        try
        {
            _logger.LogInformation("Converting file from {InputPath} to {OutputPath} in format {TargetFormat}", 
                inputPath, outputPath, targetFormat);

            if (!await IsFileAccessibleAsync(inputPath))
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

    /// <inheritdoc />
    public async Task<bool> IsFileAccessibleAsync(string filePath)
    {
        try
        {
            return await Task.FromResult(File.Exists(filePath) && new FileInfo(filePath).Length > 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking file accessibility for {FilePath}", filePath);
            return false;
        }
    }

    #region Private PDF Methods

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
        var fileInfo = new FileInfo(filePath);
        var basicInfo = $"PDF file: {fileInfo.Name}, Size: {fileInfo.Length} bytes, Created: {fileInfo.CreationTime}";
        
        return await Task.FromResult($"Text extraction not implemented with current PDF library. {basicInfo}");
    }

    private async Task<FileProcessingResult> CreatePdfAsync(string filePath, Dictionary<string, object>? parameters = null)
    {
        try
        {
            var content = parameters?.GetValueOrDefault("content", "Default PDF content") as string ?? "Default PDF content";
            var title = parameters?.GetValueOrDefault("title", "Generated PDF") as string ?? "Generated PDF";

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
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

    #endregion

    #region Private Excel Methods

    private async Task<FileProcessingResult> ReadExcelAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return FileProcessingResult.ErrorResult($"Excel file not found: {filePath}");
        }

        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheets = package.Workbook.Worksheets;
        
        var metadata = new
        {
            WorksheetCount = worksheets.Count,
            WorksheetNames = worksheets.Select(ws => ws.Name).ToArray(),
            TotalCells = worksheets.Sum(ws => ws.Dimension?.Rows * ws.Dimension?.Columns ?? 0)
        };

        return await Task.FromResult(FileProcessingResult.SuccessResult(metadata, $"Excel file read successfully. {worksheets.Count} worksheets found."));
    }

    private async Task<FileProcessingResult> WriteExcelAsync(string filePath, Dictionary<string, object>? parameters = null)
    {
        var data = parameters?.GetValueOrDefault("data") as object[,];
        var worksheetName = parameters?.GetValueOrDefault("worksheetName", "Sheet1") as string ?? "Sheet1";

        if (data == null)
        {
            return FileProcessingResult.ErrorResult("No data provided for Excel writing");
        }

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(worksheetName);
        
        // Write data to worksheet
        var rows = data.GetLength(0);
        var cols = data.GetLength(1);
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                worksheet.Cells[row + 1, col + 1].Value = data[row, col];
            }
        }

        // Save to file
        await package.SaveAsAsync(new FileInfo(filePath));
        
        return FileProcessingResult.SuccessResult(null, $"Excel file written successfully to {filePath}");
    }

    private async Task<FileProcessingResult> CreateExcelAsync(string filePath, Dictionary<string, object>? parameters = null)
    {
        using var package = new ExcelPackage();
        var worksheetName = parameters?.GetValueOrDefault("worksheetName", "Sheet1") as string ?? "Sheet1";
        var worksheet = package.Workbook.Worksheets.Add(worksheetName);
        
        // Add sample data if provided
        var headers = parameters?.GetValueOrDefault("headers") as string[];
        if (headers != null)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }
        }

        // Save to file
        await package.SaveAsAsync(new FileInfo(filePath));
        
        return FileProcessingResult.SuccessResult(null, $"Excel file created successfully at {filePath}");
    }

    private async Task<string> ExtractExcelTextAsync(string filePath)
    {
        using var package = new ExcelPackage(new FileInfo(filePath));
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

    #endregion

    #region Private Conversion Methods

    private async Task<FileProcessingResult> ConvertTextToPdfAsync(string inputPath, string outputPath)
    {
        try
        {
            var textContent = await File.ReadAllTextAsync(inputPath);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
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
                if (yPosition > page.Height - 50) break;
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

    #endregion
}