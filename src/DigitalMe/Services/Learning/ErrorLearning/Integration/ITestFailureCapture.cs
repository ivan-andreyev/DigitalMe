using System.Threading.Tasks;
using DigitalMe.Services.Learning;
using DigitalMe.Services.Learning.ErrorLearning.Models;

namespace DigitalMe.Services.Learning.ErrorLearning.Integration;

/// <summary>
/// Interface for capturing test failures from SelfTestingFramework
/// and feeding them into the Error Learning System
/// Following ISP with focused responsibility for failure capture
/// </summary>
public interface ITestFailureCapture
{
    /// <summary>
    /// Captures test failure and records it for error learning analysis
    /// </summary>
    /// <param name="testExecutionResult">Failed test execution result from SelfTestingFramework</param>
    /// <returns>Learning history entry created from the failure</returns>
    Task<LearningHistoryEntry> CaptureTestFailureAsync(TestExecutionResult testExecutionResult);

    /// <summary>
    /// Captures multiple test failures in batch for efficient processing
    /// </summary>
    /// <param name="failedTestResults">Collection of failed test results</param>
    /// <returns>Collection of learning history entries created</returns>
    Task<List<LearningHistoryEntry>> CaptureTestFailuresBatchAsync(IEnumerable<TestExecutionResult> failedTestResults);
}