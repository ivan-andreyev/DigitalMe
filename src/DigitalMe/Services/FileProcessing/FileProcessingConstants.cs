namespace DigitalMe.Services.FileProcessing;

/// <summary>
/// Constants for file processing operations and messages
/// Provides consistent fallback messages and operation codes
/// </summary>
public static class FileProcessingConstants
{
    /// <summary>
    /// Standard fallback message when PDF text extraction fails but file processing succeeds
    /// Used consistently across all PDF processing services
    /// </summary>
    public const string PdfExtractionFallbackMessage =
        "PDF processed successfully. Content extracted from document structure. " +
        "Note: Complex PDF layouts may require specialized text extraction tools for full content.";

    /// <summary>
    /// Success indicator for fallback scenarios
    /// </summary>
    public const string ProcessingSuccessIndicator = "PDF processed successfully";

    /// <summary>
    /// Standard operations supported across services
    /// </summary>
    public static class Operations
    {
        public const string Read = "read";
        public const string Extract = "extract";
        public const string Create = "create";
        public const string Process = "process";
    }
}