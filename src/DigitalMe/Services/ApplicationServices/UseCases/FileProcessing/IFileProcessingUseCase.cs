using DigitalMe.Common;
using DigitalMe.Services.ApplicationServices.Commands;
using DigitalMe.Services.ApplicationServices.Queries;

namespace DigitalMe.Services.ApplicationServices.UseCases.FileProcessing;

/// <summary>
/// Use case for file processing operations.
/// Implements Clean Architecture Application Services layer patterns.
/// Contains only business logic, no infrastructure concerns.
/// </summary>
public interface IFileProcessingUseCase : IApplicationService
{
    /// <summary>
    /// Executes file processing workflow with comprehensive testing.
    /// </summary>
    Task<Result<FileProcessingResult>> ExecuteAsync(FileProcessingCommand command);
}

/// <summary>
/// Command for file processing operations.
/// </summary>
public record FileProcessingCommand(
    string Content,
    string? Title = null);

/// <summary>
/// Result of file processing operations.
/// </summary>
public record FileProcessingResult(
    bool Success,
    bool PdfCreated,
    bool TextExtracted,
    bool ContentMatch,
    string? FileId,
    string? ExtractedTextPreview,
    string? ErrorMessage = null);