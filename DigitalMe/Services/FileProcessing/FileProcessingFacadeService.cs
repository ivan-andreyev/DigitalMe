using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.FileProcessing;

/// <summary>
/// Facade service providing backward compatibility for existing FileProcessingService consumers
/// Implements Clean Architecture patterns while maintaining existing interface
/// Composes focused services to provide unified functionality
/// </summary>
public class FileProcessingFacadeService : IFileProcessingService
{
    private readonly IPdfProcessingService _pdfProcessingService;
    private readonly IExcelProcessingService _excelProcessingService;
    private readonly ITextExtractionService _textExtractionService;
    private readonly IFileConversionService _fileConversionService;
    private readonly IFileValidationService _fileValidationService;
    private readonly ILogger<FileProcessingFacadeService> _logger;

    public FileProcessingFacadeService(
        IPdfProcessingService pdfProcessingService,
        IExcelProcessingService excelProcessingService,
        ITextExtractionService textExtractionService,
        IFileConversionService fileConversionService,
        IFileValidationService fileValidationService,
        ILogger<FileProcessingFacadeService> logger)
    {
        _pdfProcessingService = pdfProcessingService;
        _excelProcessingService = excelProcessingService;
        _textExtractionService = textExtractionService;
        _fileConversionService = fileConversionService;
        _fileValidationService = fileValidationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FileProcessingResult> ProcessPdfAsync(string operation, string filePath, Dictionary<string, object>? parameters = null)
    {
        _logger.LogDebug("Delegating PDF processing to specialized service");
        return await _pdfProcessingService.ProcessPdfAsync(operation, filePath, parameters);
    }

    /// <inheritdoc />
    public async Task<FileProcessingResult> ProcessExcelAsync(string operation, string filePath, Dictionary<string, object>? parameters = null)
    {
        _logger.LogDebug("Delegating Excel processing to specialized service");
        return await _excelProcessingService.ProcessExcelAsync(operation, filePath, parameters);
    }

    /// <inheritdoc />
    public async Task<string> ExtractTextAsync(string filePath)
    {
        _logger.LogDebug("Delegating text extraction to specialized service");
        return await _textExtractionService.ExtractTextAsync(filePath);
    }

    /// <inheritdoc />
    public async Task<FileProcessingResult> ConvertFileAsync(string inputPath, string outputPath, string targetFormat)
    {
        _logger.LogDebug("Delegating file conversion to specialized service");
        return await _fileConversionService.ConvertFileAsync(inputPath, outputPath, targetFormat);
    }

    /// <inheritdoc />
    public async Task<bool> IsFileAccessibleAsync(string filePath)
    {
        _logger.LogDebug("Delegating file validation to specialized service");
        return await _fileValidationService.IsFileAccessibleAsync(filePath);
    }
}