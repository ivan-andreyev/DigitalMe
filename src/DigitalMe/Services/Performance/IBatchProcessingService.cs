namespace DigitalMe.Services.Performance;

/// <summary>
/// Service for processing operations in batches to optimize performance
/// </summary>
public interface IBatchProcessingService
{
    /// <summary>
    /// Batch multiple operations into single request
    /// </summary>
    Task<IEnumerable<TResult>> BatchOperationsAsync<TInput, TResult>(
        IEnumerable<TInput> inputs,
        Func<IEnumerable<TInput>, Task<IEnumerable<TResult>>> batchProcessor,
        int batchSize = 50);
}