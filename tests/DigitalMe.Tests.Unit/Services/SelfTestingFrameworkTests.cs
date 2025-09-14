using System.Net;
using System.Text;
using DigitalMe.Services.Learning;
using DigitalMe.Services.Learning.Testing;
using DigitalMe.Services.Learning.Testing.ResultsAnalysis;
using DigitalMe.Services.Learning.Testing.TestExecution;
using DigitalMe.Services.Learning.Testing.TestGeneration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace DigitalMe.Tests.Unit.Services;

/// <summary>
/// Unit tests for SelfTestingFramework - Phase 1 Advanced Cognitive Capabilities
/// Tests the ability to generate, execute, and analyze test cases for self-validation
/// Updated for SOLID architecture refactoring with extracted TestCaseGenerator
/// </summary>
public class SelfTestingFrameworkTests : IDisposable
{
    private readonly Mock<ILogger<SelfTestingFramework>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ITestCaseGenerator> _mockTestCaseGenerator;
    private readonly Mock<ITestExecutor> _mockTestExecutor;
    private readonly Mock<IResultsAnalyzer> _mockResultsAnalyzer;
    private readonly Mock<ITestOrchestrator> _mockTestOrchestrator;
    private readonly Mock<ICapabilityValidator> _mockCapabilityValidator;
    private readonly Mock<ITestAnalyzer> _mockTestAnalyzer;
    private readonly SelfTestingFramework _framework;

    public SelfTestingFrameworkTests()
    {
        this._mockLogger = new Mock<ILogger<SelfTestingFramework>>();
        this._mockHttpHandler = new Mock<HttpMessageHandler>();
        this._mockTestCaseGenerator = new Mock<ITestCaseGenerator>();
        this._mockTestExecutor = new Mock<ITestExecutor>();
        this._mockResultsAnalyzer = new Mock<IResultsAnalyzer>();
        this._mockTestOrchestrator = new Mock<ITestOrchestrator>();
        this._mockCapabilityValidator = new Mock<ICapabilityValidator>();
        this._mockTestAnalyzer = new Mock<ITestAnalyzer>();
        this._httpClient = new HttpClient(this._mockHttpHandler.Object)
        {
            BaseAddress = new Uri("https://api.test.com")
        };
        this._framework = new SelfTestingFramework(this._mockLogger.Object, this._mockTestOrchestrator.Object, this._mockCapabilityValidator.Object, this._mockTestAnalyzer.Object);
    }

    #region Test Case Generation Tests

    [Fact]
    public async Task GenerateTestCasesAsync_WithValidApiDocumentation_GeneratesTestCases()
    {
        // Arrange
        var apiDocumentation = this.CreateSampleApiDocumentation();
        var expectedTestCases = this.CreateSampleTestCases();
        this._mockTestOrchestrator
            .Setup(x => x.GenerateTestCasesAsync(It.IsAny<DocumentationParseResult>()))
            .ReturnsAsync(expectedTestCases);

        // Act
        var testCases = await this._framework.GenerateTestCasesAsync(apiDocumentation);

        // Assert
        Assert.NotEmpty(testCases);
        Assert.All(testCases, tc => Assert.NotEmpty(tc.Name));
        Assert.All(testCases, tc => Assert.NotEmpty(tc.Endpoint));
        Assert.All(testCases, tc => Assert.NotEmpty(tc.HttpMethod));
        Assert.Contains(testCases, tc => tc.Priority == TestPriority.High);
        this._mockTestOrchestrator.Verify(x => x.GenerateTestCasesAsync(apiDocumentation), Times.Once);
    }

    [Fact]
    public async Task GenerateTestCasesAsync_WithMultipleEndpoints_GeneratesAppropriateCoverage()
    {
        // Arrange
        var apiDocumentation = new DocumentationParseResult
        {
            Success = true,
            ApiName = "MultiEndpointAPI",
            Endpoints = new List<ApiEndpoint>
            {
                new ApiEndpoint { Method = "GET", Path = "/users", Description = "Get users" },
                new ApiEndpoint { Method = "POST", Path = "/users", Description = "Create user" },
                new ApiEndpoint { Method = "GET", Path = "/health", Description = "Health check" }
            },
            Examples = new List<CodeExample>(),
            Authentication = AuthenticationMethod.Bearer,
            RequiredHeaders = new List<string> { "Authorization" }
        };

        var expectedTestCases = new List<SelfGeneratedTestCase>();
        var getUsersTest = this.CreateSampleTestCase("GET_users", "/users");
        getUsersTest.HttpMethod = "GET";
        expectedTestCases.Add(getUsersTest);

        var postUsersTest = this.CreateSampleTestCase("POST_users", "/users");
        postUsersTest.HttpMethod = "POST";
        expectedTestCases.Add(postUsersTest);

        var getHealthTest = this.CreateSampleTestCase("GET_health", "/health");
        getHealthTest.HttpMethod = "GET";
        expectedTestCases.Add(getHealthTest);

        this._mockTestOrchestrator
            .Setup(x => x.GenerateTestCasesAsync(It.IsAny<DocumentationParseResult>()))
            .ReturnsAsync(expectedTestCases);

        // Act
        var testCases = await this._framework.GenerateTestCasesAsync(apiDocumentation);

        // Assert
        Assert.True(testCases.Count >= 3, "Should generate test cases for each endpoint");
        Assert.Contains(testCases, tc => tc.Endpoint.Contains("/users") && tc.HttpMethod == "GET");
        Assert.Contains(testCases, tc => tc.Endpoint.Contains("/users") && tc.HttpMethod == "POST");
        Assert.Contains(testCases, tc => tc.Endpoint.Contains("/health"));
        this._mockTestOrchestrator.Verify(x => x.GenerateTestCasesAsync(apiDocumentation), Times.Once);
    }

    [Fact]
    public async Task GenerateTestCasesAsync_WithAuthenticationRequired_IncludesAuthHeaders()
    {
        // Arrange
        var apiDocumentation = this.CreateSampleApiDocumentation();
        apiDocumentation.Authentication = AuthenticationMethod.Bearer;
        apiDocumentation.RequiredHeaders = new List<string> { "Authorization", "Content-Type" };

        var expectedTestCases = new List<SelfGeneratedTestCase>();
        var postWithAuth = this.CreateSampleTestCase("POST_with_auth", "/api/test");
        postWithAuth.HttpMethod = "POST";
        postWithAuth.Headers = new Dictionary<string, string> { { "Authorization", "Bearer token" } };
        expectedTestCases.Add(postWithAuth);

        var getWithContentType = this.CreateSampleTestCase("GET_with_content_type", "/api/test");
        getWithContentType.HttpMethod = "GET";
        getWithContentType.Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } };
        expectedTestCases.Add(getWithContentType);

        this._mockTestOrchestrator
            .Setup(x => x.GenerateTestCasesAsync(It.IsAny<DocumentationParseResult>()))
            .ReturnsAsync(expectedTestCases);

        // Act
        var testCases = await this._framework.GenerateTestCasesAsync(apiDocumentation);

        // Assert
        Assert.All(testCases, tc =>
        {
            if (tc.HttpMethod != "GET")
            {
                Assert.True(
                    tc.Headers.ContainsKey("Authorization") || tc.Headers.ContainsKey("Content-Type"),
                    "Test cases should include required authentication headers");
            }
        });
        this._mockTestOrchestrator.Verify(x => x.GenerateTestCasesAsync(apiDocumentation), Times.Once);
    }

    #endregion

    #region Single Test Execution Tests

    [Fact]
    public async Task ExecuteTestCaseAsync_WithSuccessfulResponse_ReturnsSuccessResult()
    {
        // Arrange
        var testCase = this.CreateSampleTestCase();
        var expectedResult = new TestExecutionResult
        {
            TestCaseId = testCase.Id,
            TestCaseName = testCase.Name,
            Success = true,
            Response = "Success response",
            ExecutionTime = TimeSpan.FromMilliseconds(100)
        };
        this._mockTestOrchestrator
            .Setup(x => x.ExecuteTestCaseAsync(testCase))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._framework.ExecuteTestCaseAsync(testCase);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Response);
        Assert.True(result.ExecutionTime > TimeSpan.Zero);
        this._mockTestOrchestrator.Verify(x => x.ExecuteTestCaseAsync(testCase), Times.Once);
    }

    [Fact]
    public async Task ExecuteTestCaseAsync_WithHttpError_ReturnsFailureResult()
    {
        // Arrange
        var testCase = this.CreateSampleTestCase();
        var expectedResult = new TestExecutionResult
        {
            TestCaseId = testCase.Id,
            TestCaseName = testCase.Name,
            Success = false,
            Response = "Error response",
            ExecutionTime = TimeSpan.FromMilliseconds(150),
            AssertionResults = new List<AssertionResult>
            {
                new AssertionResult { AssertionName = "Status Check", Passed = false }
            }
        };
        this._mockTestOrchestrator
            .Setup(x => x.ExecuteTestCaseAsync(testCase))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._framework.ExecuteTestCaseAsync(testCase);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.AssertionResults);
        Assert.True(result.AssertionResults.Any(a => !a.Passed));
        Assert.True(result.ExecutionTime > TimeSpan.Zero);
        this._mockTestOrchestrator.Verify(x => x.ExecuteTestCaseAsync(testCase), Times.Once);
    }

    [Fact]
    public async Task ExecuteTestCaseAsync_WithTimeout_HandlesGracefully()
    {
        // Arrange
        var testCase = this.CreateSampleTestCase();
        var expectedResult = new TestExecutionResult
        {
            TestCaseId = testCase.Id,
            TestCaseName = testCase.Name,
            Success = false,
            ErrorMessage = "Test timed out after 5 seconds",
            ExecutionTime = TimeSpan.FromSeconds(5)
        };
        this._mockTestOrchestrator
            .Setup(x => x.ExecuteTestCaseAsync(testCase))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._framework.ExecuteTestCaseAsync(testCase);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("timed out", result.ErrorMessage.ToLowerInvariant());
        this._mockTestOrchestrator.Verify(x => x.ExecuteTestCaseAsync(testCase), Times.Once);
    }

    #endregion

    #region Test Suite Execution Tests

    [Fact]
    public async Task ExecuteTestSuiteAsync_WithMultipleTestCases_ExecutesAll()
    {
        // Arrange
        var testCases = new List<SelfGeneratedTestCase>
        {
            this.CreateSampleTestCase("Test1", "/api/test1"),
            this.CreateSampleTestCase("Test2", "/api/test2"),
            this.CreateSampleTestCase("Test3", "/api/test3")
        };
        var expectedResult = new TestSuiteResult
        {
            SuiteName = "Test Suite",
            Status = TestSuiteStatus.Completed,
            TotalExecutionTime = TimeSpan.FromMilliseconds(300),
            TestResults = testCases.Select(tc => new TestExecutionResult
            {
                TestCaseId = tc.Id,
                TestCaseName = tc.Name,
                Success = true,
                ExecutionTime = TimeSpan.FromMilliseconds(100)
            }).ToList()
        };
        this._mockTestOrchestrator
            .Setup(x => x.ExecuteTestSuiteAsync(testCases))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._framework.ExecuteTestSuiteAsync(testCases);

        // Assert
        Assert.Equal(3, result.TotalTests);
        Assert.Equal(3, result.TestResults.Count);
        Assert.All(result.TestResults, tr => Assert.True(tr.Success));
        Assert.Equal(100.0, result.SuccessRate);
        Assert.True(result.TotalExecutionTime > TimeSpan.Zero);
        this._mockTestOrchestrator.Verify(x => x.ExecuteTestSuiteAsync(testCases), Times.Once);
    }

    [Fact]
    public async Task ExecuteTestSuiteAsync_WithMixedResults_CalculatesCorrectMetrics()
    {
        // Arrange
        var testCases = new List<SelfGeneratedTestCase>
        {
            this.CreateSampleTestCase("SuccessTest", "/api/success"),
            this.CreateSampleTestCase("FailTest", "/api/fail")
        };
        var expectedResult = new TestSuiteResult
        {
            SuiteName = "Mixed Results Suite",
            Status = TestSuiteStatus.Completed,
            TotalExecutionTime = TimeSpan.FromMilliseconds(200),
            TestResults = new List<TestExecutionResult>
            {
                new TestExecutionResult { TestCaseId = testCases[0].Id, Success = true },
                new TestExecutionResult { TestCaseId = testCases[1].Id, Success = false }
            }
        };
        this._mockTestOrchestrator
            .Setup(x => x.ExecuteTestSuiteAsync(testCases))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._framework.ExecuteTestSuiteAsync(testCases);

        // Assert
        Assert.Equal(2, result.TotalTests);
        Assert.Equal(1, result.PassedTests);
        Assert.Equal(1, result.FailedTests);
        Assert.Equal(50.0, result.SuccessRate);
        this._mockTestOrchestrator.Verify(x => x.ExecuteTestSuiteAsync(testCases), Times.Once);
    }

    [Fact]
    public async Task ExecuteTestSuiteAsync_WithEmptyTestSuite_ReturnsEmptyResult()
    {
        // Arrange
        var testCases = new List<SelfGeneratedTestCase>();
        var expectedResult = new TestSuiteResult
        {
            SuiteName = "Empty Suite",
            Status = TestSuiteStatus.Completed,
            TestResults = new List<TestExecutionResult>()
        };
        this._mockTestOrchestrator
            .Setup(x => x.ExecuteTestSuiteAsync(testCases))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._framework.ExecuteTestSuiteAsync(testCases);

        // Assert
        Assert.Equal(0, result.TotalTests);
        Assert.Equal(0, result.PassedTests);
        Assert.Equal(0, result.FailedTests);
        Assert.Equal(0.0, result.SuccessRate);
        this._mockTestOrchestrator.Verify(x => x.ExecuteTestSuiteAsync(testCases), Times.Once);
        Assert.Empty(result.TestResults);
    }

    #endregion

    #region Capability Validation Tests

    [Fact]
    public async Task ValidateLearnedCapabilityAsync_WithValidCapability_ReturnsValidationResult()
    {
        // Arrange
        var capability = this.CreateSampleLearnedCapability();
        var expectedResult = new CapabilityValidationResult
        {
            ConfidenceScore = 0.85,
            ValidationResults = new List<TestExecutionResult>
            {
                new TestExecutionResult { Success = true, TestCaseName = "ValidationTest1" }
            },
            NewStatus = CapabilityStatus.Validated,
            Strengths = new List<string> { "High success rate" },
            Weaknesses = new List<string>(),
            ImprovementSuggestions = new List<string> { "Continue monitoring" }
        };

        this._mockCapabilityValidator
            .Setup(x => x.ValidateLearnedCapabilityAsync("TestAPI", capability))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._framework.ValidateLearnedCapabilityAsync("TestAPI", capability);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ConfidenceScore >= 0.0 && result.ConfidenceScore <= 1.0);
        Assert.NotEmpty(result.ValidationResults);
        Assert.NotNull(result.NewStatus);
        this._mockCapabilityValidator.Verify(x => x.ValidateLearnedCapabilityAsync("TestAPI", capability), Times.Once);
    }

    [Fact]
    public async Task ValidateLearnedCapabilityAsync_WithFailingTests_LowersConfidenceScore()
    {
        // Arrange
        var capability = this.CreateSampleLearnedCapability();
        var expectedResult = new CapabilityValidationResult
        {
            ConfidenceScore = 0.4,
            ValidationResults = new List<TestExecutionResult>
            {
                new TestExecutionResult { Success = false, TestCaseName = "ValidationTest1", ErrorMessage = "Server error" }
            },
            NewStatus = CapabilityStatus.Failed,
            Strengths = new List<string>(),
            Weaknesses = new List<string> { "High failure rate", "Server connectivity issues" },
            ImprovementSuggestions = new List<string> { "Review server configuration", "Add retry logic" }
        };

        this._mockCapabilityValidator
            .Setup(x => x.ValidateLearnedCapabilityAsync("TestAPI", capability))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._framework.ValidateLearnedCapabilityAsync("TestAPI", capability);

        // Assert
        Assert.True(result.ConfidenceScore < 0.7, "Confidence score should be lowered due to failing tests");
        Assert.Contains(result.Weaknesses, w => !string.IsNullOrEmpty(w));
        Assert.NotEmpty(result.ImprovementSuggestions);
        this._mockCapabilityValidator.Verify(x => x.ValidateLearnedCapabilityAsync("TestAPI", capability), Times.Once);
    }

    [Fact]
    public async Task ValidateLearnedCapabilityAsync_WithHighSuccessRate_ProducesSuggestions()
    {
        // Arrange
        var capability = this.CreateSampleLearnedCapability();
        var expectedResult = new CapabilityValidationResult
        {
            ConfidenceScore = 0.95,
            ValidationResults = new List<TestExecutionResult>
            {
                new TestExecutionResult { Success = true, TestCaseName = "ValidationTest1" },
                new TestExecutionResult { Success = true, TestCaseName = "ValidationTest2" }
            },
            NewStatus = CapabilityStatus.Validated,
            Strengths = new List<string> { "High success rate", "Consistent working performance" },
            Weaknesses = new List<string>(),
            ImprovementSuggestions = new List<string> { "Monitor for edge cases", "Consider performance optimizations" }
        };

        this._mockCapabilityValidator
            .Setup(x => x.ValidateLearnedCapabilityAsync("TestAPI", capability))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._framework.ValidateLearnedCapabilityAsync("TestAPI", capability);

        // Assert
        Assert.NotEmpty(result.Strengths);
        Assert.Contains(result.Strengths, s => s.Contains("success") || s.Contains("working"));
        this._mockCapabilityValidator.Verify(x => x.ValidateLearnedCapabilityAsync("TestAPI", capability), Times.Once);
    }

    #endregion

    #region Performance Benchmarking Tests

    [Fact]
    public async Task BenchmarkNewSkillAsync_WithTestResults_CalculatesMetrics()
    {
        // Arrange
        var skillName = "API Testing Skill";
        var testResults = this.CreateSampleTestResults(5, 3);
        var expectedResult = new PerformanceBenchmarkResult
        {
            SkillName = skillName,
            SuccessRate = 0.6, // 3 out of 5
            AverageExecutionTime = TimeSpan.FromMilliseconds(120),
            Grade = PerformanceGrade.C,
            PerformanceMetrics = new Dictionary<string, double>
            {
                { "TotalTests", 5.0 },
                { "PassedTests", 3.0 },
                { "FailedTests", 2.0 }
            }
        };

        this._mockCapabilityValidator
            .Setup(x => x.BenchmarkNewSkillAsync(skillName, testResults))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._framework.BenchmarkNewSkillAsync(skillName, testResults);

        // Assert
        Assert.Equal(skillName, result.SkillName);
        Assert.True(result.SuccessRate > 0);
        Assert.True(result.AverageExecutionTime > TimeSpan.Zero);
        Assert.NotNull(result.Grade);
        Assert.True(result.Grade >= PerformanceGrade.F && result.Grade <= PerformanceGrade.A);
        Assert.NotEmpty(result.PerformanceMetrics);
        this._mockCapabilityValidator.Verify(x => x.BenchmarkNewSkillAsync(skillName, testResults), Times.Once);
    }

    [Fact]
    public async Task BenchmarkNewSkillAsync_WithAllSuccessfulTests_ReturnsHighGrade()
    {
        // Arrange
        var skillName = "Perfect API Skill";
        var testResults = this.CreateSampleTestResults(5, 5); // All successful
        var expectedResult = new PerformanceBenchmarkResult
        {
            SkillName = skillName,
            SuccessRate = 1.0, // 100% success
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            Grade = PerformanceGrade.A,
            PerformanceMetrics = new Dictionary<string, double>
            {
                { "TotalTests", 5.0 },
                { "PassedTests", 5.0 },
                { "FailedTests", 0.0 }
            }
        };

        this._mockCapabilityValidator
            .Setup(x => x.BenchmarkNewSkillAsync(skillName, testResults))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._framework.BenchmarkNewSkillAsync(skillName, testResults);

        // Assert
        Assert.True(result.Grade >= PerformanceGrade.B, "All successful tests should result in high grade");
        Assert.Equal(1.0, result.SuccessRate); // 1.0 = 100% success rate
        this._mockCapabilityValidator.Verify(x => x.BenchmarkNewSkillAsync(skillName, testResults), Times.Once);
    }

    [Fact]
    public async Task BenchmarkNewSkillAsync_WithAllFailedTests_ReturnsLowGrade()
    {
        // Arrange
        var skillName = "Failing API Skill";
        var testResults = this.CreateSampleTestResults(5, 0); // All failed
        var expectedResult = new PerformanceBenchmarkResult
        {
            SkillName = skillName,
            SuccessRate = 0.0, // 0% success
            AverageExecutionTime = TimeSpan.FromMilliseconds(250),
            Grade = PerformanceGrade.F,
            PerformanceMetrics = new Dictionary<string, double>
            {
                { "TotalTests", 5.0 },
                { "PassedTests", 0.0 },
                { "FailedTests", 5.0 }
            }
        };

        this._mockCapabilityValidator
            .Setup(x => x.BenchmarkNewSkillAsync(skillName, testResults))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._framework.BenchmarkNewSkillAsync(skillName, testResults);

        // Assert
        Assert.True(result.Grade <= PerformanceGrade.D, "All failed tests should result in low grade");
        Assert.Equal(0.0, result.SuccessRate);
        this._mockCapabilityValidator.Verify(x => x.BenchmarkNewSkillAsync(skillName, testResults), Times.Once);
    }

    #endregion

    #region Failure Analysis Tests

    [Fact]
    public async Task AnalyzeTestFailuresAsync_WithMultipleFailures_CategorizesProperly()
    {
        // Arrange
        var failedTests = new List<TestExecutionResult>
        {
            this.CreateFailedTestResult("Test1", 404, "Not Found"),
            this.CreateFailedTestResult("Test2", 500, "Internal Server Error"),
            this.CreateFailedTestResult("Test3", 401, "Unauthorized"),
            this.CreateFailedTestResult("Test4", 404, "Not Found")
        };

        var expectedResult = new TestAnalysisResult
        {
            TotalFailedTests = 4,
            FailureCategories = new Dictionary<string, int>
            {
                { "Not Found", 2 },
                { "Server Error", 1 },
                { "Authentication", 1 }
            },
            CommonPatterns = new List<CommonFailurePattern>(),
            Suggestions = new List<ImprovementSuggestion>
            {
                new ImprovementSuggestion { Title = "404 Errors", Description = "Review endpoint URLs for 404 errors" },
                new ImprovementSuggestion { Title = "Server Errors", Description = "Check server configuration for 500 errors" },
                new ImprovementSuggestion { Title = "Auth Errors", Description = "Verify authentication tokens for 401 errors" }
            }
        };

        this._mockTestAnalyzer
            .Setup(x => x.AnalyzeTestFailuresAsync(failedTests))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._framework.AnalyzeTestFailuresAsync(failedTests);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalFailedTests == 4);
        Assert.NotEmpty(result.FailureCategories);
        Assert.Contains(result.FailureCategories, fc => fc.Key == "Not Found");
        Assert.Contains(result.FailureCategories, fc => fc.Key == "Server Error");
        Assert.Contains(result.FailureCategories, fc => fc.Key == "Authentication");
        Assert.NotEmpty(result.Suggestions);
        this._mockTestAnalyzer.Verify(x => x.AnalyzeTestFailuresAsync(failedTests), Times.Once);
    }

    [Fact]
    public async Task AnalyzeTestFailuresAsync_WithCommonErrorPattern_IdentifiesPattern()
    {
        // Arrange
        var failedTests = new List<TestExecutionResult>
        {
            this.CreateFailedTestResult("Test1", 401, "Unauthorized"),
            this.CreateFailedTestResult("Test2", 401, "Unauthorized"),
            this.CreateFailedTestResult("Test3", 401, "Unauthorized")
        };

        var expectedResult = new TestAnalysisResult
        {
            TotalFailedTests = 3,
            FailureCategories = new Dictionary<string, int> { { "Authentication", 3 } },
            CommonPatterns = new List<CommonFailurePattern>
            {
                new CommonFailurePattern
                {
                    Pattern = "Error Message",
                    Description = "401: Unauthorized",
                    Frequency = 3
                }
            },
            Suggestions = new List<ImprovementSuggestion>
            {
                new ImprovementSuggestion { Title = "Token Validity", Description = "Check authentication token validity" },
                new ImprovementSuggestion { Title = "API Permissions", Description = "Verify API key permissions" }
            }
        };

        this._mockTestAnalyzer
            .Setup(x => x.AnalyzeTestFailuresAsync(failedTests))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._framework.AnalyzeTestFailuresAsync(failedTests);

        // Assert
        Assert.NotEmpty(result.CommonPatterns);
        var pattern = result.CommonPatterns.First();
        Assert.Equal("Error Message", pattern.Pattern);
        Assert.Equal("401: Unauthorized", pattern.Description);
        Assert.Equal(3, pattern.Frequency);
        Assert.NotEmpty(result.Suggestions);
        this._mockTestAnalyzer.Verify(x => x.AnalyzeTestFailuresAsync(failedTests), Times.Once);
    }

    [Fact]
    public async Task AnalyzeTestFailuresAsync_WithEmptyList_ReturnsEmptyAnalysis()
    {
        // Arrange
        var failedTests = new List<TestExecutionResult>();
        var expectedResult = new TestAnalysisResult
        {
            TotalFailedTests = 0,
            FailureCategories = new Dictionary<string, int>(),
            CommonPatterns = new List<CommonFailurePattern>(),
            Suggestions = new List<ImprovementSuggestion>()
        };

        this._mockTestAnalyzer
            .Setup(x => x.AnalyzeTestFailuresAsync(failedTests))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._framework.AnalyzeTestFailuresAsync(failedTests);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalFailedTests);
        Assert.Empty(result.FailureCategories);
        Assert.Empty(result.CommonPatterns);
        this._mockTestAnalyzer.Verify(x => x.AnalyzeTestFailuresAsync(failedTests), Times.Once);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SelfTestingFramework(null!, this._mockTestOrchestrator.Object, this._mockCapabilityValidator.Object, this._mockTestAnalyzer.Object));
    }

    [Fact]
    public void Constructor_WithNullTestOrchestrator_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SelfTestingFramework(this._mockLogger.Object, null!, this._mockCapabilityValidator.Object, this._mockTestAnalyzer.Object));
    }

    [Fact]
    public void Constructor_WithNullCapabilityValidator_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SelfTestingFramework(this._mockLogger.Object, this._mockTestOrchestrator.Object, null!, this._mockTestAnalyzer.Object));
    }

    [Fact]
    public void Constructor_WithNullTestAnalyzer_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SelfTestingFramework(this._mockLogger.Object, this._mockTestOrchestrator.Object, this._mockCapabilityValidator.Object, null!));
    }

    #endregion

    #region Helper Methods

    private DocumentationParseResult CreateSampleApiDocumentation()
    {
        return new DocumentationParseResult
        {
            Success = true,
            ApiName = "TestAPI",
            Endpoints = new List<ApiEndpoint>
            {
                new ApiEndpoint { Method = "GET", Path = "/api/users", Description = "Get all users" },
                new ApiEndpoint { Method = "POST", Path = "/api/users", Description = "Create user" }
            },
            Examples = new List<CodeExample>
            {
                new CodeExample
                {
                    Language = "javascript",
                    Code = "fetch('/api/users')",
                    Endpoint = "/api/users"
                }
            },
            Authentication = AuthenticationMethod.None,
            RequiredHeaders = new List<string>()
        };
    }

    private SelfGeneratedTestCase CreateSampleTestCase(string name = "TestCase", string endpoint = "/api/test")
    {
        return new SelfGeneratedTestCase
        {
            Name = name,
            Description = $"Test case for {endpoint}",
            Endpoint = endpoint,
            HttpMethod = "GET",
            Headers = new Dictionary<string, string>(),
            Parameters = new Dictionary<string, object>(),
            Priority = TestPriority.Medium,
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
            }
        };
    }

    private List<SelfGeneratedTestCase> CreateSampleTestCases()
    {
        var testCases = new List<SelfGeneratedTestCase>
        {
            this.CreateSampleTestCase("GET_Users_HappyPath", "/api/users"),
            this.CreateSampleTestCase("POST_Users_HappyPath", "/api/users"),
            this.CreateSampleTestCase("Unauthorized_Access_Test", "/api/users"),
            this.CreateSampleTestCase("Not_Found_Test", "/non-existent-endpoint")
        };

        // Set some test cases to High priority
        testCases[0].Priority = TestPriority.High;
        testCases[1].Priority = TestPriority.High;

        return testCases;
    }

    private LearnedCapability CreateSampleLearnedCapability()
    {
        return new LearnedCapability
        {
            Name = "Sample API Capability",
            Description = "Ability to interact with sample API",
            ValidationTests = new List<SelfGeneratedTestCase>
            {
                this.CreateSampleTestCase("ValidationTest1", "/api/validate1"),
                this.CreateSampleTestCase("ValidationTest2", "/api/validate2")
            }
        };
    }

    private List<TestExecutionResult> CreateSampleTestResults(int totalTests, int successfulTests)
    {
        var results = new List<TestExecutionResult>();

        for (int i = 0; i < successfulTests; i++)
        {
            results.Add(new TestExecutionResult
            {
                TestCaseName = $"SuccessTest{i + 1}",
                Success = true,
                Response = "Success response",
                ExecutionTime = TimeSpan.FromMilliseconds(100 + i * 10)
            });
        }

        for (int i = successfulTests; i < totalTests; i++)
        {
            results.Add(new TestExecutionResult
            {
                TestCaseName = $"FailTest{i + 1}",
                Success = false,
                ErrorMessage = "Server error",
                ExecutionTime = TimeSpan.FromMilliseconds(200 + i * 10)
            });
        }

        return results;
    }

    private TestExecutionResult CreateFailedTestResult(string testName, int statusCode, string errorMessage)
    {
        return new TestExecutionResult
        {
            TestCaseName = testName,
            Success = false,
            ErrorMessage = $"{statusCode}: {errorMessage}",
            ExecutionTime = TimeSpan.FromMilliseconds(150)
        };
    }

    private void SetupSuccessfulHttpResponse()
    {
        this._mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\": true}", Encoding.UTF8, "application/json")
            });
    }

    private void SetupFailureHttpResponse(HttpStatusCode statusCode)
    {
        this._mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("{\"error\": \"Request failed\"}", Encoding.UTF8, "application/json")
            });
    }

    private void SetupTimeoutHttpResponse()
    {
        this._mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));
    }

    #endregion

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._httpClient?.Dispose();
        }
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }
}