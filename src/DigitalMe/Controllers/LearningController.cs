using Microsoft.AspNetCore.Mvc;
using DigitalMe.Services.Learning;

namespace DigitalMe.Controllers;

/// <summary>
/// Controller for Phase 1 Advanced Cognitive Capabilities - Learning and Self-Testing
/// Demonstrates agent's ability to learn new APIs and validate capabilities automatically
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LearningController : ControllerBase
{
    private readonly IAutoDocumentationParser _documentationParser;
    private readonly ISelfTestingFramework _testingFramework;
    private readonly ILogger<LearningController> _logger;

    public LearningController(
        IAutoDocumentationParser documentationParser,
        ISelfTestingFramework testingFramework,
        ILogger<LearningController> logger)
    {
        _documentationParser = documentationParser ?? throw new ArgumentNullException(nameof(documentationParser));
        _testingFramework = testingFramework ?? throw new ArgumentNullException(nameof(testingFramework));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Demonstrate auto-learning capability by parsing API documentation
    /// Phase 1 Feature: Agent can learn new APIs by reading documentation
    /// </summary>
    /// <param name="request">Documentation parsing request</param>
    /// <returns>Parsed API information with generated test cases</returns>
    [HttpPost("learn-api")]
    public async Task<ActionResult<LearnApiResponse>> LearnApiAsync([FromBody] LearnApiRequest request)
    {
        try
        {
            _logger.LogInformation("Learning new API: {ApiName} from {DocumentationUrl}", 
                request.ApiName, request.DocumentationUrl);

            // Parse API documentation
            var parseResult = await _documentationParser.ParseApiDocumentationAsync(
                request.DocumentationUrl, request.ApiName);

            if (!parseResult.Success)
            {
                return BadRequest(new { error = parseResult.ErrorMessage });
            }

            // Generate test cases based on learned patterns
            var testCases = await _testingFramework.GenerateTestCasesAsync(parseResult);

            // Execute a subset of tests to validate learning (if requested)
            TestSuiteResult? validationResult = null;
            if (request.ValidateLearning && testCases.Any())
            {
                var priorityTests = testCases
                    .Where(tc => tc.Priority >= TestPriority.High)
                    .Take(5) // Limit to 5 high-priority tests for demo
                    .ToList();

                if (priorityTests.Any())
                {
                    validationResult = await _testingFramework.ExecuteTestSuiteAsync(priorityTests);
                }
            }

            var response = new LearnApiResponse
            {
                ApiName = parseResult.ApiName,
                LearningSuccess = true,
                ParsedEndpoints = parseResult.Endpoints.Count,
                ExtractedExamples = parseResult.Examples.Count,
                GeneratedTestCases = testCases.Count,
                Authentication = parseResult.Authentication.ToString(),
                RequiredHeaders = parseResult.RequiredHeaders,
                ValidationResult = validationResult,
                LearnedAt = DateTime.UtcNow,
                Capabilities = ExtractCapabilities(parseResult),
                Confidence = CalculateLearningConfidence(parseResult, validationResult)
            };

            _logger.LogInformation("API learning completed: {ApiName} - {EndpointCount} endpoints, {TestCaseCount} test cases generated",
                request.ApiName, parseResult.Endpoints.Count, testCases.Count);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error learning API: {ApiName}", request.ApiName);
            return StatusCode(500, new { error = "Failed to learn API", details = ex.Message });
        }
    }

    /// <summary>
    /// Demonstrate self-testing capability by validating learned skills
    /// Phase 1 Feature: Agent can test itself and validate capabilities
    /// </summary>
    /// <param name="request">Self-validation request</param>
    /// <returns>Capability validation results with performance metrics</returns>
    [HttpPost("validate-capability")]
    public async Task<ActionResult<ValidateCapabilityResponse>> ValidateCapabilityAsync([FromBody] ValidateCapabilityRequest request)
    {
        try
        {
            _logger.LogInformation("Validating capability: {CapabilityName}", request.CapabilityName);

            var capability = new LearnedCapability
            {
                Name = request.CapabilityName,
                Description = request.Description,
                ValidationTests = request.TestCases?.Select(MapToSelfGeneratedTestCase).ToList() ?? new List<SelfGeneratedTestCase>()
            };

            // Validate the capability
            var validationResult = await _testingFramework.ValidateLearnedCapabilityAsync(
                request.CapabilityName, capability);

            // Generate performance benchmark
            var benchmark = await _testingFramework.BenchmarkNewSkillAsync(
                request.CapabilityName, validationResult.ValidationResults);

            // Analyze failures if any
            TestAnalysisResult? analysisResult = null;
            var failedTests = validationResult.ValidationResults.Where(r => !r.Success).ToList();
            if (failedTests.Any())
            {
                analysisResult = await _testingFramework.AnalyzeTestFailuresAsync(failedTests);
            }

            var response = new ValidateCapabilityResponse
            {
                CapabilityName = request.CapabilityName,
                ValidationSuccess = validationResult.IsValid,
                ConfidenceScore = validationResult.ConfidenceScore,
                Status = validationResult.NewStatus.ToString(),
                TestResults = validationResult.ValidationResults,
                PerformanceBenchmark = benchmark,
                Strengths = validationResult.Strengths,
                Weaknesses = validationResult.Weaknesses,
                ImprovementSuggestions = validationResult.ImprovementSuggestions,
                FailureAnalysis = analysisResult,
                ValidatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Capability validation completed: {CapabilityName} - Valid: {IsValid}, Confidence: {ConfidenceScore:F2}",
                request.CapabilityName, validationResult.IsValid, validationResult.ConfidenceScore);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating capability: {CapabilityName}", request.CapabilityName);
            return StatusCode(500, new { error = "Failed to validate capability", details = ex.Message });
        }
    }

    /// <summary>
    /// Demonstrate comprehensive learning workflow
    /// Phase 1 Feature: End-to-end learning from documentation to validation
    /// </summary>
    /// <param name="request">Complete learning workflow request</param>
    /// <returns>Full learning and validation results</returns>
    [HttpPost("complete-learning-workflow")]
    public async Task<ActionResult<CompleteLearningResponse>> CompleteLearningWorkflowAsync([FromBody] CompleteLearningRequest request)
    {
        try
        {
            _logger.LogInformation("Starting complete learning workflow for API: {ApiName}", request.ApiName);

            var workflow = new List<string>();
            var startTime = DateTime.UtcNow;

            // Step 1: Learn API from documentation
            workflow.Add("Parsing API documentation");
            var parseResult = await _documentationParser.ParseApiDocumentationAsync(
                request.DocumentationUrl, request.ApiName);

            if (!parseResult.Success)
            {
                return BadRequest(new { error = "Failed to parse API documentation", details = parseResult.ErrorMessage });
            }

            // Step 2: Generate comprehensive test cases
            workflow.Add("Generating test cases based on learned patterns");
            var testCases = await _testingFramework.GenerateTestCasesAsync(parseResult);

            // Step 3: Execute validation tests
            workflow.Add("Executing validation test suite");
            var validationResult = await _testingFramework.ExecuteTestSuiteAsync(testCases);

            // Step 4: Benchmark performance
            workflow.Add("Benchmarking performance");
            var benchmark = await _testingFramework.BenchmarkNewSkillAsync(
                request.ApiName, validationResult.TestResults);

            // Step 5: Analyze any failures
            TestAnalysisResult? analysisResult = null;
            if (validationResult.FailedTests > 0)
            {
                workflow.Add("Analyzing test failures");
                var failedTests = validationResult.TestResults.Where(r => !r.Success).ToList();
                analysisResult = await _testingFramework.AnalyzeTestFailuresAsync(failedTests);
            }

            var totalTime = DateTime.UtcNow - startTime;

            var response = new CompleteLearningResponse
            {
                ApiName = request.ApiName,
                WorkflowSuccess = validationResult.SuccessRate > 50, // At least 50% tests should pass for successful learning
                TotalExecutionTime = totalTime,
                WorkflowSteps = workflow,
                ParsedApiInfo = new ApiSummary
                {
                    EndpointCount = parseResult.Endpoints.Count,
                    ExampleCount = parseResult.Examples.Count,
                    AuthenticationMethod = parseResult.Authentication.ToString(),
                    RequiredHeaders = parseResult.RequiredHeaders
                },
                TestingSummary = new TestingSummary
                {
                    TotalTests = validationResult.TotalTests,
                    PassedTests = validationResult.PassedTests,
                    FailedTests = validationResult.FailedTests,
                    SuccessRate = validationResult.SuccessRate,
                    ExecutionTime = validationResult.TotalExecutionTime
                },
                PerformanceGrade = benchmark.Grade.ToString(),
                LearningConfidence = CalculateLearningConfidence(parseResult, validationResult),
                FailureAnalysis = analysisResult,
                CompletedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Complete learning workflow finished for {ApiName}: Success Rate {SuccessRate:F1}% in {TotalTime}",
                request.ApiName, validationResult.SuccessRate, totalTime);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in complete learning workflow for API: {ApiName}", request.ApiName);
            return StatusCode(500, new { error = "Learning workflow failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Get current learning capabilities status
    /// Shows what the agent has learned and can do
    /// </summary>
    /// <returns>Current learning status and capabilities</returns>
    [HttpGet("status")]
    public ActionResult<LearningStatusResponse> GetLearningStatus()
    {
        var response = new LearningStatusResponse
        {
            Phase = "Phase 1: Advanced Cognitive Capabilities",
            CurrentCapabilities = new List<string>
            {
                "Auto-Documentation Parsing - Learn APIs from documentation",
                "Self-Testing Framework - Generate and execute validation tests",
                "Pattern Recognition - Identify usage patterns in code examples",
                "Performance Benchmarking - Measure and grade skill performance",
                "Failure Analysis - Analyze test failures and suggest improvements",
                "Confidence Assessment - Calculate learning confidence scores"
            },
            SystemStatus = "Learning Infrastructure Ready",
            AvailableEndpoints = new List<string>
            {
                "/api/learning/learn-api - Learn new API from documentation",
                "/api/learning/validate-capability - Validate learned capabilities",
                "/api/learning/complete-learning-workflow - Full learning demonstration",
                "/api/learning/status - Get current status"
            },
            StatusCheckedAt = DateTime.UtcNow
        };

        return Ok(response);
    }

    #region Helper Methods

    private List<string> ExtractCapabilities(DocumentationParseResult parseResult)
    {
        var capabilities = new List<string>();

        if (parseResult.Endpoints.Any(e => e.Method == "GET"))
            capabilities.Add("Data Retrieval");
        
        if (parseResult.Endpoints.Any(e => e.Method == "POST"))
            capabilities.Add("Data Creation");
        
        if (parseResult.Endpoints.Any(e => e.Method == "PUT" || e.Method == "PATCH"))
            capabilities.Add("Data Modification");
        
        if (parseResult.Endpoints.Any(e => e.Method == "DELETE"))
            capabilities.Add("Data Deletion");

        if (parseResult.Authentication != AuthenticationMethod.None)
            capabilities.Add("Authentication Handling");

        if (parseResult.Examples.Any())
            capabilities.Add("Pattern-Based Usage");

        return capabilities;
    }

    private double CalculateLearningConfidence(DocumentationParseResult parseResult, TestSuiteResult? validationResult = null)
    {
        var baseConfidence = 0.5; // Start with 50% base confidence

        // Increase confidence based on parsed information
        if (parseResult.Endpoints.Any())
        {
            baseConfidence += 0.2;
        }
        if (parseResult.Examples.Any())
        {
            baseConfidence += 0.1;
        }
        if (parseResult.Configuration.Any())
        {
            baseConfidence += 0.1;
        }

        // Adjust based on validation results if available
        if (validationResult != null)
        {
            var validationFactor = validationResult.SuccessRate / 100.0 * 0.3;
            baseConfidence += validationFactor;
        }

        return Math.Min(1.0, baseConfidence);
    }

    private SelfGeneratedTestCase MapToSelfGeneratedTestCase(TestCaseDto dto)
    {
        return new SelfGeneratedTestCase
        {
            Name = dto.Name,
            Description = dto.Description,
            Endpoint = dto.Endpoint,
            HttpMethod = dto.HttpMethod,
            Headers = dto.Headers ?? new Dictionary<string, string>(),
            Parameters = dto.Parameters ?? new Dictionary<string, object>(),
            Priority = Enum.TryParse<TestPriority>(dto.Priority, out var priority) ? priority : TestPriority.Medium
        };
    }

    #endregion
}

#region Request/Response Models

public class LearnApiRequest
{
    public string ApiName { get; set; } = string.Empty;
    public string DocumentationUrl { get; set; } = string.Empty;
    public bool ValidateLearning { get; set; } = true;
}

public class LearnApiResponse
{
    public string ApiName { get; set; } = string.Empty;
    public bool LearningSuccess { get; set; }
    public int ParsedEndpoints { get; set; }
    public int ExtractedExamples { get; set; }
    public int GeneratedTestCases { get; set; }
    public string Authentication { get; set; } = string.Empty;
    public List<string> RequiredHeaders { get; set; } = new();
    public TestSuiteResult? ValidationResult { get; set; }
    public DateTime LearnedAt { get; set; }
    public List<string> Capabilities { get; set; } = new();
    public double Confidence { get; set; }
}

public class ValidateCapabilityRequest
{
    public string CapabilityName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<TestCaseDto>? TestCases { get; set; }
}

public class ValidateCapabilityResponse
{
    public string CapabilityName { get; set; } = string.Empty;
    public bool ValidationSuccess { get; set; }
    public double ConfidenceScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<TestExecutionResult> TestResults { get; set; } = new();
    public PerformanceBenchmarkResult PerformanceBenchmark { get; set; } = null!;
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> ImprovementSuggestions { get; set; } = new();
    public TestAnalysisResult? FailureAnalysis { get; set; }
    public DateTime ValidatedAt { get; set; }
}

public class CompleteLearningRequest
{
    public string ApiName { get; set; } = string.Empty;
    public string DocumentationUrl { get; set; } = string.Empty;
}

public class CompleteLearningResponse
{
    public string ApiName { get; set; } = string.Empty;
    public bool WorkflowSuccess { get; set; }
    public TimeSpan TotalExecutionTime { get; set; }
    public List<string> WorkflowSteps { get; set; } = new();
    public ApiSummary ParsedApiInfo { get; set; } = null!;
    public TestingSummary TestingSummary { get; set; } = null!;
    public string PerformanceGrade { get; set; } = string.Empty;
    public double LearningConfidence { get; set; }
    public TestAnalysisResult? FailureAnalysis { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class LearningStatusResponse
{
    public string Phase { get; set; } = string.Empty;
    public List<string> CurrentCapabilities { get; set; } = new();
    public string SystemStatus { get; set; } = string.Empty;
    public List<string> AvailableEndpoints { get; set; } = new();
    public DateTime StatusCheckedAt { get; set; }
}

public class TestCaseDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "GET";
    public Dictionary<string, string>? Headers { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
    public string Priority { get; set; } = "Medium";
}

public class ApiSummary
{
    public int EndpointCount { get; set; }
    public int ExampleCount { get; set; }
    public string AuthenticationMethod { get; set; } = string.Empty;
    public List<string> RequiredHeaders { get; set; } = new();
}

public class TestingSummary
{
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan ExecutionTime { get; set; }
}

#endregion