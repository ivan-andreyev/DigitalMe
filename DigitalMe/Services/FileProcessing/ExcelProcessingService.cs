using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using DigitalMe.Infrastructure;

namespace DigitalMe.Services.FileProcessing;

/// <summary>
/// Excel processing service following Single Responsibility Principle
/// Clean Architecture compliant - no direct infrastructure dependencies
/// </summary>
public class ExcelProcessingService : IExcelProcessingService
{
    private readonly ILogger<ExcelProcessingService> _logger;
    private readonly IFileRepository _fileRepository;

    public ExcelProcessingService(ILogger<ExcelProcessingService> logger, IFileRepository fileRepository)
    {
        _logger = logger;
        _fileRepository = fileRepository;
        
        // Set EPPlus license context (required for non-commercial use)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
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

    private async Task<FileProcessingResult> ReadExcelAsync(string filePath)
    {
        if (!await _fileRepository.ExistsAsync(filePath))
        {
            return FileProcessingResult.ErrorResult($"Excel file not found: {filePath}");
        }

        var fileInfo = await _fileRepository.GetFileInfoAsync(filePath);
        if (fileInfo == null)
        {
            return FileProcessingResult.ErrorResult($"Cannot access Excel file: {filePath}");
        }

        using var package = new ExcelPackage(fileInfo);
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

        // Ensure directory exists
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !await _fileRepository.EnsureDirectoryExistsAsync(directory))
        {
            return FileProcessingResult.ErrorResult($"Failed to create directory: {directory}");
        }

        // Save to file
        await package.SaveAsAsync(new FileInfo(filePath));
        
        return FileProcessingResult.SuccessResult(null, $"Excel file written successfully to {filePath}");
    }

    private async Task<FileProcessingResult> CreateExcelAsync(string filePath, Dictionary<string, object>? parameters = null)
    {
        // Ensure directory exists
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !await _fileRepository.EnsureDirectoryExistsAsync(directory))
        {
            return FileProcessingResult.ErrorResult($"Failed to create directory: {directory}");
        }

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
}