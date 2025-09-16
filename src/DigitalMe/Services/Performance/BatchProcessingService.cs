using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Performance;

/// <summary>
/// Service for processing operations in batches to optimize performance
/// </summary>
public class BatchProcessingService : IBatchProcessingService
{
    private readonly ILogger<BatchProcessingService> _logger;

    public BatchProcessingService(ILogger<BatchProcessingService> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<TResult>> BatchOperationsAsync<TInput, TResult>(
        IEnumerable<TInput> inputs,
        Func<IEnumerable<TInput>, Task<IEnumerable<TResult>>> batchProcessor,
        int batchSize = 50)
    {
        var results = new List<TResult>();
        var inputList = inputs.ToList();

        _logger.LogInformation("Processing {TotalItems} items in batches of {BatchSize}",
            inputList.Count, batchSize);

        for (int i = 0; i < inputList.Count; i += batchSize)
        {
            var batch = inputList.Skip(i).Take(batchSize);
            var batchNumber = (i / batchSize) + 1;
            var totalBatches = (int)Math.Ceiling((double)inputList.Count / batchSize);

            _logger.LogDebug("Processing batch {BatchNumber}/{TotalBatches}", batchNumber, totalBatches);

            try
            {
                var batchResults = await batchProcessor(batch);
                results.AddRange(batchResults);

                // Small delay between batches to be respectful to APIs
                if (i + batchSize < inputList.Count)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch {BatchNumber}/{TotalBatches}",
                    batchNumber, totalBatches);
                throw;
            }
        }

        _logger.LogInformation("Completed processing {TotalItems} items, got {ResultCount} results",
            inputList.Count, results.Count);

        return results;
    }
}