using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DigitalMe.Services.Learning.ErrorLearning;
using DigitalMe.Services.Learning.ErrorLearning.Integration;
using DigitalMe.Services.Learning.ErrorLearning.Models;
using DigitalMe.Services.Learning;

namespace DigitalMe.Controllers;

/// <summary>
/// Controller for Error Learning System operations
/// Provides endpoints for testing and managing the error learning functionality
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ErrorLearningController : ControllerBase
{
    private readonly ILogger<ErrorLearningController> _logger;
    private readonly IErrorLearningService _errorLearningService;
    private readonly TestOrchestratorFactory _testOrchestratorFactory;

    public ErrorLearningController(
        ILogger<ErrorLearningController> logger,
        IErrorLearningService errorLearningService,
        TestOrchestratorFactory testOrchestratorFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorLearningService = errorLearningService ?? throw new ArgumentNullException(nameof(errorLearningService));
        _testOrchestratorFactory = testOrchestratorFactory ?? throw new ArgumentNullException(nameof(testOrchestratorFactory));
    }

    /// <summary>
    /// Tests the integration between SelfTestingFramework and Error Learning System
    /// </summary>
    [HttpPost("test-integration")]
    public async Task<IActionResult> TestIntegration()
    {
        try
        {
            _logger.LogInformation("Testing Error Learning System integration");

            // Create learning-enabled orchestrator
            var orchestrator = _testOrchestratorFactory.CreateLearningEnabledOrchestrator();

            // Simulate a test failure scenario
            var testCase = CreateTestFailureScenario();

            // Execute test (this will capture failure for learning)
            var result = await orchestrator.ExecuteTestCaseAsync(testCase);

            // Get learning statistics
            var stats = await _errorLearningService.GetLearningStatisticsAsync();

            return Ok(new
            {
                Message = "Error Learning System integration test completed",
                TestResult = new
                {
                    TestCaseName = result.TestCaseName,
                    Success = result.Success,
                    ErrorMessage = result.ErrorMessage,
                    ExecutionTime = result.ExecutionTime
                },
                LearningStats = new
                {
                    TotalErrorPatterns = stats.TotalErrorPatterns,
                    TotalLearningEntries = stats.TotalLearningEntries,
                    UnanalyzedEntries = stats.UnanalyzedEntries,
                    AveragePatternConfidence = stats.AveragePatternConfidence
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during integration test");
            return StatusCode(500, new { Error = "Integration test failed", Details = ex.Message });
        }
    }

    /// <summary>
    /// Records a manual error for testing error learning
    /// </summary>
    [HttpPost("record-error")]
    public async Task<IActionResult> RecordTestError([FromBody] TestErrorRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request cannot be null");

            var learningEntry = await _errorLearningService.RecordErrorAsync(
                source: request.Source ?? "Manual Test",
                errorMessage: request.ErrorMessage,
                testCaseName: request.TestCaseName,
                apiName: request.ApiName,
                httpMethod: request.HttpMethod,
                apiEndpoint: request.ApiEndpoint,
                httpStatusCode: request.HttpStatusCode,
                stackTrace: request.StackTrace
            );

            return Ok(new
            {
                Message = "Test error recorded successfully",
                LearningEntryId = learningEntry.Id,
                RecordedAt = learningEntry.Timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record test error");
            return StatusCode(500, new { Error = "Failed to record error", Details = ex.Message });
        }
    }

    /// <summary>
    /// Analyzes error patterns and generates optimization suggestions
    /// </summary>
    [HttpPost("analyze-patterns")]
    public async Task<IActionResult> AnalyzeErrorPatterns()
    {
        try
        {
            var patternsFound = await _errorLearningService.AnalyzeErrorPatternsAsync();
            var patterns = await _errorLearningService.GetErrorPatternsAsync();

            return Ok(new
            {
                Message = "Error pattern analysis completed",
                PatternsAnalyzed = patternsFound,
                TotalPatterns = patterns.Count,
                Patterns = patterns.Select(p => new
                {
                    p.Id,
                    p.Category,
                    p.Subcategory,
                    p.PatternHash,
                    p.OccurrenceCount,
                    p.SeverityLevel,
                    p.ConfidenceScore,
                    p.ApiEndpoint
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pattern analysis");
            return StatusCode(500, new { Error = "Pattern analysis failed", Details = ex.Message });
        }
    }

    /// <summary>
    /// Gets learning statistics and system health
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var stats = await _errorLearningService.GetLearningStatisticsAsync();

            return Ok(new
            {
                Message = "Learning statistics retrieved successfully",
                Statistics = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get learning statistics");
            return StatusCode(500, new { Error = "Failed to get statistics", Details = ex.Message });
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Creates a test case that will intentionally fail for learning purposes
    /// </summary>
    private SelfGeneratedTestCase CreateTestFailureScenario()
    {
        return new SelfGeneratedTestCase
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Error Learning Integration Test",
            Description = "Intentional failure to test error learning capture",
            ApiName = "TestAPI",
            Endpoint = "/api/test/failing-endpoint",
            HttpMethod = "GET",
            Assertions = new List<TestAssertion>
            {
                new TestAssertion
                {
                    Name = "Status Code Check",
                    Type = AssertionType.StatusCode,
                    ExpectedValue = "200",
                    Operator = ComparisonOperator.Equals,
                    IsCritical = true
                }
            },
            Priority = TestPriority.Medium,
            ExpectedExecutionTime = TimeSpan.FromSeconds(2)
        };
    }

    #endregion
}

/// <summary>
/// Request model for manual error recording
/// </summary>
public class TestErrorRequest
{
    public string? Source { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string? TestCaseName { get; set; }
    public string? ApiName { get; set; }
    public string? HttpMethod { get; set; }
    public string? ApiEndpoint { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? StackTrace { get; set; }
}