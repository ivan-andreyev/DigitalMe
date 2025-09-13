using System;
using System.Threading.Tasks;
using DigitalMe.Services.Learning.ErrorLearning.Models;

namespace DigitalMe.Services.Learning.ErrorLearning;

/// <summary>
/// Service for learning statistics and metrics reporting
/// Follows SRP - focused only on statistics aggregation and reporting
/// </summary>
public interface ILearningStatisticsService
{
    /// <summary>
    /// Gets learning statistics and metrics
    /// </summary>
    /// <param name="fromDate">Start date for statistics (optional)</param>
    /// <param name="toDate">End date for statistics (optional)</param>
    /// <returns>Learning statistics object</returns>
    Task<LearningStatistics> GetLearningStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);
}