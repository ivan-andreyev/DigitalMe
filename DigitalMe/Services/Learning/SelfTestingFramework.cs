using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DigitalMe.Services.Learning;

/// <summary>
/// Implementation of self-testing framework for Phase 1 Advanced Cognitive Capabilities
/// Enables agent to automatically test and validate learned capabilities
/// Following Clean Architecture patterns with dependency injection
/// </summary>
public class SelfTestingFramework : ISelfTestingFramework
{
    private readonly ILogger<SelfTestingFramework> _logger;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public SelfTestingFramework(ILogger<SelfTestingFramework> logger, HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    /// <inheritdoc />
    public async Task<List<SelfGeneratedTestCase>> GenerateTestCasesAsync(DocumentationParseResult apiDocumentation)
    {
        try
        {
            _logger.LogInformation("Generating test cases for API: {ApiName}", apiDocumentation.ApiName);

            var testCases = new List<SelfGeneratedTestCase>();

            // Generate test cases for each endpoint
            foreach (var endpoint in apiDocumentation.Endpoints)
            {
                testCases.AddRange(await GenerateEndpointTestCasesAsync(apiDocumentation, endpoint));
            }

            // Generate integration test cases based on examples
            testCases.AddRange(await GenerateExampleBasedTestCasesAsync(apiDocumentation));

            // Generate error handling test cases
            testCases.AddRange(GenerateErrorHandlingTestCases(apiDocumentation));

            // Generate authentication test cases
            if (apiDocumentation.Authentication != AuthenticationMethod.None)
            {
                testCases.AddRange(GenerateAuthenticationTestCases(apiDocumentation));
            }

            _logger.LogInformation("Generated {TestCaseCount} test cases for API: {ApiName}", 
                testCases.Count, apiDocumentation.ApiName);

            return testCases;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating test cases for API: {ApiName}", apiDocumentation.ApiName);
            return new List<SelfGeneratedTestCase>();
        }
    }

    /// <inheritdoc />
    public async Task<TestExecutionResult> ExecuteTestCaseAsync(SelfGeneratedTestCase testCase)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new TestExecutionResult
        {
            TestCaseId = testCase.Id,
            TestCaseName = testCase.Name
        };

        try
        {
            _logger.LogDebug("Executing test case: {TestCaseName}", testCase.Name);

            // Prepare HTTP request
            var request = await CreateHttpRequestAsync(testCase);
            
            // Execute request with timeout
            using var cts = new CancellationTokenSource(testCase.ExpectedExecutionTime.Add(TimeSpan.FromSeconds(10)));
            var response = await _httpClient.SendAsync(request, cts.Token);
            
            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;
            
            // Parse response
            var responseBody = await response.Content.ReadAsStringAsync();
            result.Response = responseBody;

            // Execute assertions
            result.AssertionResults = await ExecuteAssertionsAsync(testCase.Assertions, response, responseBody);
            
            // Determine overall success
            result.Success = result.AssertionResults.All(a => a.Passed || !a.IsCritical);

            // Collect metrics
            result.Metrics = CollectExecutionMetrics(response, result.ExecutionTime);

            _logger.LogDebug("Test case {TestCaseName} executed in {ExecutionTime}ms: {Status}", 
                testCase.Name, result.ExecutionTime.TotalMilliseconds, result.Success ? "PASSED" : "FAILED");
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;
            result.Success = false;
            result.ErrorMessage = $"Test timed out after {testCase.ExpectedExecutionTime.TotalSeconds} seconds";
            _logger.LogWarning("Test case {TestCaseName} timed out", testCase.Name);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Exception = ex;
            _logger.LogError(ex, "Test case {TestCaseName} failed with exception", testCase.Name);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<TestSuiteResult> ExecuteTestSuiteAsync(List<SelfGeneratedTestCase> testCases)
    {
        var suiteStopwatch = Stopwatch.StartNew();
        var result = new TestSuiteResult
        {
            SuiteName = $"Self-Generated Test Suite ({testCases.Count} tests)",
            Status = TestSuiteStatus.Running
        };

        try
        {
            _logger.LogInformation("Executing test suite with {TestCount} test cases", testCases.Count);

            // Execute tests in parallel with limited concurrency
            var semaphore = new SemaphoreSlim(5); // Max 5 concurrent tests
            var executionTasks = testCases.Select(async testCase =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await ExecuteTestCaseAsync(testCase);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            result.TestResults = (await Task.WhenAll(executionTasks)).ToList();
            
            suiteStopwatch.Stop();
            result.TotalExecutionTime = suiteStopwatch.Elapsed;
            result.Status = TestSuiteStatus.Completed;

            // Generate recommendations based on results
            result.Recommendations = GenerateTestSuiteRecommendations(result);

            _logger.LogInformation("Test suite completed: {PassedTests}/{TotalTests} tests passed ({SuccessRate:F1}%) in {ExecutionTime}ms",
                result.PassedTests, result.TotalTests, result.SuccessRate, result.TotalExecutionTime.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            suiteStopwatch.Stop();
            result.TotalExecutionTime = suiteStopwatch.Elapsed;
            result.Status = TestSuiteStatus.Failed;
            _logger.LogError(ex, "Test suite execution failed");
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<CapabilityValidationResult> ValidateLearnedCapabilityAsync(string apiName, LearnedCapability capability)
    {
        try
        {
            _logger.LogInformation("Validating learned capability: {CapabilityName}", capability.Name);

            var result = new CapabilityValidationResult
            {
                CapabilityName = capability.Name
            };

            // Execute validation tests
            var suiteResult = await ExecuteTestSuiteAsync(capability.ValidationTests);
            result.ValidationResults = suiteResult.TestResults;

            // Calculate confidence score based on test results
            result.ConfidenceScore = CalculateConfidenceScore(suiteResult);
            result.IsValid = result.ConfidenceScore >= 0.8; // 80% threshold

            // Analyze strengths and weaknesses
            AnalyzeCapabilityResults(result, suiteResult);

            // Determine new status
            result.NewStatus = DetermineCapabilityStatus(result);

            _logger.LogInformation("Capability validation completed for {CapabilityName}: Valid={IsValid}, Confidence={ConfidenceScore:F2}",
                capability.Name, result.IsValid, result.ConfidenceScore);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating capability: {CapabilityName}", capability.Name);
            return new CapabilityValidationResult
            {
                CapabilityName = capability.Name,
                IsValid = false,
                ConfidenceScore = 0,
                NewStatus = CapabilityStatus.Failed
            };
        }
    }

    /// <inheritdoc />
    public async Task<PerformanceBenchmarkResult> BenchmarkNewSkillAsync(string skillName, List<TestExecutionResult> testResults)
    {
        try
        {
            _logger.LogInformation("Benchmarking skill performance: {SkillName}", skillName);

            if (!testResults.Any())
            {
                return new PerformanceBenchmarkResult
                {
                    SkillName = skillName,
                    Grade = PerformanceGrade.F
                };
            }

            var successfulTests = testResults.Where(r => r.Success).ToList();
            var executionTimes = testResults.Select(r => r.ExecutionTime).ToList();

            var benchmark = new PerformanceBenchmarkResult
            {
                SkillName = skillName,
                AverageExecutionTime = TimeSpan.FromMilliseconds(executionTimes.Average(t => t.TotalMilliseconds)),
                MinExecutionTime = executionTimes.Min(),
                MaxExecutionTime = executionTimes.Max(),
                SuccessRate = testResults.Count > 0 ? (double)successfulTests.Count / testResults.Count : 0,
                TotalOperations = testResults.Count
            };

            // Collect additional performance metrics
            benchmark.PerformanceMetrics = CollectDetailedMetrics(testResults);

            // Assign performance grade
            benchmark.Grade = AssignPerformanceGrade(benchmark.SuccessRate);

            // Generate performance recommendations
            benchmark.PerformanceRecommendations = GeneratePerformanceRecommendations(benchmark);

            _logger.LogInformation("Performance benchmark completed for {SkillName}: Grade={Grade}, Success Rate={SuccessRate:F2}%, Avg Time={AvgTime}ms",
                skillName, benchmark.Grade, benchmark.SuccessRate * 100, benchmark.AverageExecutionTime.TotalMilliseconds);

            return benchmark;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error benchmarking skill: {SkillName}", skillName);
            return new PerformanceBenchmarkResult
            {
                SkillName = skillName,
                Grade = PerformanceGrade.F
            };
        }
    }

    /// <inheritdoc />
    public async Task<TestAnalysisResult> AnalyzeTestFailuresAsync(List<TestExecutionResult> failedTests)
    {
        try
        {
            _logger.LogInformation("Analyzing {FailedTestCount} failed tests", failedTests.Count);

            var analysis = new TestAnalysisResult
            {
                TotalFailedTests = failedTests.Count
            };

            // Categorize failures
            analysis.FailureCategories = CategorizeFailures(failedTests);

            // Identify common patterns
            analysis.CommonPatterns = await IdentifyCommonFailurePatternsAsync(failedTests);

            // Generate improvement suggestions
            analysis.Suggestions = GenerateImprovementSuggestions(analysis);

            // Calculate overall health score
            analysis.OverallHealthScore = CalculateHealthScore(analysis);

            // Identify critical issues
            analysis.CriticalIssues = IdentifyCriticalIssues(analysis);

            _logger.LogInformation("Test failure analysis completed: {PatternCount} patterns identified, Health Score: {HealthScore:F2}",
                analysis.CommonPatterns.Count, analysis.OverallHealthScore);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing test failures");
            return new TestAnalysisResult
            {
                TotalFailedTests = failedTests.Count,
                OverallHealthScore = 0
            };
        }
    }

    #region Private Helper Methods

    private async Task<List<SelfGeneratedTestCase>> GenerateEndpointTestCasesAsync(
        DocumentationParseResult apiDoc, ApiEndpoint endpoint)
    {
        var testCases = new List<SelfGeneratedTestCase>();

        // Happy path test
        var happyPathTest = new SelfGeneratedTestCase
        {
            Name = $"{endpoint.Method}_{endpoint.Path.Replace("/", "_")}_HappyPath",
            Description = $"Test successful {endpoint.Method} request to {endpoint.Path}",
            ApiName = apiDoc.ApiName,
            Endpoint = endpoint.Path,
            HttpMethod = endpoint.Method,
            Priority = TestPriority.High,
            Headers = CreateStandardHeaders(apiDoc)
        };

        // Add required parameters
        foreach (var param in endpoint.Parameters.Where(p => p.Required))
        {
            happyPathTest.Parameters[param.Name] = GenerateTestValue(param);
        }

        // Add standard assertions
        happyPathTest.Assertions.Add(new TestAssertion
        {
            Name = "Status Code Success",
            Type = AssertionType.StatusCode,
            ExpectedValue = "200,201,202",
            Operator = ComparisonOperator.Contains
        });

        testCases.Add(happyPathTest);

        // Parameter validation tests
        foreach (var param in endpoint.Parameters.Where(p => p.Required))
        {
            var paramTest = new SelfGeneratedTestCase
            {
                Name = $"{endpoint.Method}_{endpoint.Path.Replace("/", "_")}_Missing_{param.Name}",
                Description = $"Test {endpoint.Method} request with missing required parameter {param.Name}",
                ApiName = apiDoc.ApiName,
                Endpoint = endpoint.Path,
                HttpMethod = endpoint.Method,
                Priority = TestPriority.Medium,
                Headers = CreateStandardHeaders(apiDoc)
            };

            // Add other required parameters but skip this one
            foreach (var otherParam in endpoint.Parameters.Where(p => p.Required && p.Name != param.Name))
            {
                paramTest.Parameters[otherParam.Name] = GenerateTestValue(otherParam);
            }

            paramTest.Assertions.Add(new TestAssertion
            {
                Name = "Bad Request Status",
                Type = AssertionType.StatusCode,
                ExpectedValue = "400",
                Operator = ComparisonOperator.Equals
            });

            testCases.Add(paramTest);
        }

        return testCases;
    }

    private async Task<List<SelfGeneratedTestCase>> GenerateExampleBasedTestCasesAsync(DocumentationParseResult apiDoc)
    {
        var testCases = new List<SelfGeneratedTestCase>();

        foreach (var example in apiDoc.Examples.Take(5)) // Limit to 5 examples
        {
            var testCase = new SelfGeneratedTestCase
            {
                Name = $"Example_Based_Test_{testCases.Count + 1}",
                Description = $"Test based on documentation example: {example.Description}",
                ApiName = apiDoc.ApiName,
                Endpoint = example.Endpoint,
                Priority = TestPriority.High,
                Headers = CreateStandardHeaders(apiDoc)
            };

            // Extract method from example code
            testCase.HttpMethod = ExtractHttpMethod(example.Code);

            // Use extracted values as parameters
            testCase.Parameters = example.ExtractedValues.ToDictionary(
                kv => kv.Key, 
                kv => kv.Value);

            // Add response validation
            testCase.Assertions.Add(new TestAssertion
            {
                Name = "Response Received",
                Type = AssertionType.StatusCode,
                ExpectedValue = "200,201,202",
                Operator = ComparisonOperator.Contains
            });

            testCases.Add(testCase);
        }

        return testCases;
    }

    private List<SelfGeneratedTestCase> GenerateErrorHandlingTestCases(DocumentationParseResult apiDoc)
    {
        var testCases = new List<SelfGeneratedTestCase>();

        // Unauthorized test
        if (apiDoc.Authentication != AuthenticationMethod.None)
        {
            testCases.Add(new SelfGeneratedTestCase
            {
                Name = "Unauthorized_Access_Test",
                Description = "Test API response when authentication is missing",
                ApiName = apiDoc.ApiName,
                Endpoint = apiDoc.Endpoints.FirstOrDefault()?.Path ?? "/",
                HttpMethod = "GET",
                Priority = TestPriority.High,
                // Intentionally no auth headers
                Assertions = new List<TestAssertion>
                {
                    new TestAssertion
                    {
                        Name = "Unauthorized Status",
                        Type = AssertionType.StatusCode,
                        ExpectedValue = "401",
                        Operator = ComparisonOperator.Equals
                    }
                }
            });
        }

        // Not Found test
        testCases.Add(new SelfGeneratedTestCase
        {
            Name = "Not_Found_Test",
            Description = "Test API response for non-existent endpoint",
            ApiName = apiDoc.ApiName,
            Endpoint = "/non-existent-endpoint",
            HttpMethod = "GET",
            Priority = TestPriority.Medium,
            Headers = CreateStandardHeaders(apiDoc),
            Assertions = new List<TestAssertion>
            {
                new TestAssertion
                {
                    Name = "Not Found Status",
                    Type = AssertionType.StatusCode,
                    ExpectedValue = "404",
                    Operator = ComparisonOperator.Equals
                }
            }
        });

        return testCases;
    }

    private List<SelfGeneratedTestCase> GenerateAuthenticationTestCases(DocumentationParseResult apiDoc)
    {
        var testCases = new List<SelfGeneratedTestCase>();

        // Valid authentication test
        testCases.Add(new SelfGeneratedTestCase
        {
            Name = "Valid_Authentication_Test",
            Description = "Test API with valid authentication",
            ApiName = apiDoc.ApiName,
            Endpoint = apiDoc.Endpoints.FirstOrDefault()?.Path ?? "/",
            HttpMethod = "GET",
            Priority = TestPriority.Critical,
            Headers = CreateStandardHeaders(apiDoc),
            Assertions = new List<TestAssertion>
            {
                new TestAssertion
                {
                    Name = "Authentication Success",
                    Type = AssertionType.StatusCode,
                    ExpectedValue = "401",
                    Operator = ComparisonOperator.NotEquals
                }
            }
        });

        return testCases;
    }

    private async Task<HttpRequestMessage> CreateHttpRequestAsync(SelfGeneratedTestCase testCase)
    {
        var request = new HttpRequestMessage(new HttpMethod(testCase.HttpMethod), testCase.Endpoint);

        // Add headers
        foreach (var header in testCase.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Add body for POST/PUT requests
        if (testCase.HttpMethod.ToUpperInvariant() is "POST" or "PUT" or "PATCH")
        {
            if (testCase.RequestBody != null)
            {
                var json = JsonSerializer.Serialize(testCase.RequestBody, _jsonOptions);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            }
            else if (testCase.Parameters.Any())
            {
                var json = JsonSerializer.Serialize(testCase.Parameters, _jsonOptions);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            }
        }
        // Add as query parameters for GET requests
        else if (testCase.Parameters.Any())
        {
            var queryString = string.Join("&", testCase.Parameters.Select(p => 
                $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value?.ToString() ?? "")}"));
            
            var uriBuilder = new UriBuilder(testCase.Endpoint);
            uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query) 
                ? queryString 
                : uriBuilder.Query + "&" + queryString;
            
            request.RequestUri = uriBuilder.Uri;
        }

        return request;
    }

    private async Task<List<AssertionResult>> ExecuteAssertionsAsync(
        List<TestAssertion> assertions, HttpResponseMessage response, string responseBody)
    {
        var results = new List<AssertionResult>();

        foreach (var assertion in assertions)
        {
            var result = new AssertionResult
            {
                AssertionName = assertion.Name,
                IsCritical = assertion.IsCritical
            };

            try
            {
                var actualValue = await ExtractActualValueAsync(assertion, response, responseBody);
                result.ActualValue = actualValue;
                result.ExpectedValue = assertion.ExpectedValue;

                result.Passed = EvaluateAssertion(assertion, actualValue);
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.ErrorMessage = ex.Message;
            }

            results.Add(result);
        }

        return results;
    }

    private async Task<string> ExtractActualValueAsync(TestAssertion assertion, 
        HttpResponseMessage response, string responseBody)
    {
        return assertion.Type switch
        {
            AssertionType.StatusCode => ((int)response.StatusCode).ToString(),
            AssertionType.ResponseTime => "0", // Would need to be passed from calling method
            AssertionType.ResponseBody => responseBody,
            AssertionType.ResponseHeader => response.Headers.GetValues(assertion.ActualValuePath).FirstOrDefault() ?? "",
            AssertionType.JsonPath => ExtractJsonPathValue(responseBody, assertion.ActualValuePath),
            _ => ""
        };
    }

    private bool EvaluateAssertion(TestAssertion assertion, string actualValue)
    {
        return assertion.Operator switch
        {
            ComparisonOperator.Equals => actualValue == assertion.ExpectedValue,
            ComparisonOperator.NotEquals => actualValue != assertion.ExpectedValue,
            ComparisonOperator.Contains => assertion.ExpectedValue.Split(',').Contains(actualValue),
            ComparisonOperator.NotContains => !actualValue.Contains(assertion.ExpectedValue),
            ComparisonOperator.StartsWith => actualValue.StartsWith(assertion.ExpectedValue),
            ComparisonOperator.EndsWith => actualValue.EndsWith(assertion.ExpectedValue),
            _ => false
        };
    }

    private string ExtractJsonPathValue(string json, string jsonPath)
    {
        try
        {
            var jsonNode = JsonNode.Parse(json);
            // Simplified JSON path extraction - in production would use JSONPath library
            var pathParts = jsonPath.Split('.');
            JsonNode? current = jsonNode;
            
            foreach (var part in pathParts)
            {
                current = current?[part];
            }
            
            return current?.ToString() ?? "";
        }
        catch
        {
            return "";
        }
    }

    private Dictionary<string, object> CollectExecutionMetrics(HttpResponseMessage response, TimeSpan executionTime)
    {
        return new Dictionary<string, object>
        {
            ["StatusCode"] = (int)response.StatusCode,
            ["ExecutionTimeMs"] = executionTime.TotalMilliseconds,
            ["ResponseLength"] = response.Content.Headers.ContentLength ?? 0,
            ["ContentType"] = response.Content.Headers.ContentType?.ToString() ?? "unknown"
        };
    }

    private Dictionary<string, string> CreateStandardHeaders(DocumentationParseResult apiDoc)
    {
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json",
            ["Accept"] = "application/json"
        };

        // Add authentication header based on detected method
        switch (apiDoc.Authentication)
        {
            case AuthenticationMethod.ApiKey:
                headers["X-API-Key"] = "test-api-key";
                break;
            case AuthenticationMethod.Bearer:
                headers["Authorization"] = "Bearer test-token";
                break;
            case AuthenticationMethod.Basic:
                headers["Authorization"] = "Basic dGVzdDp0ZXN0"; // test:test
                break;
        }

        return headers;
    }

    private object GenerateTestValue(ApiParameter parameter)
    {
        // Generate appropriate test values based on parameter type and constraints
        return parameter.Type.ToLowerInvariant() switch
        {
            "string" => parameter.AllowedValues.Any() ? parameter.AllowedValues.First() : "test_value",
            "integer" or "int" => 42,
            "boolean" or "bool" => true,
            "number" or "float" or "double" => 3.14,
            "array" => new[] { "test1", "test2" },
            _ => parameter.DefaultValue ?? "test_value"
        };
    }

    private string ExtractHttpMethod(string code)
    {
        var upperCode = code.ToUpperInvariant();
        if (upperCode.Contains("POST")) return "POST";
        if (upperCode.Contains("PUT")) return "PUT";
        if (upperCode.Contains("DELETE")) return "DELETE";
        if (upperCode.Contains("PATCH")) return "PATCH";
        return "GET";
    }

    private List<string> GenerateTestSuiteRecommendations(TestSuiteResult result)
    {
        var recommendations = new List<string>();

        if (result.SuccessRate < 50)
        {
            recommendations.Add("Overall test success rate is low. Review API configuration and test data.");
        }
        
        if (result.FailedTests > result.PassedTests)
        {
            recommendations.Add("More tests are failing than passing. Consider reviewing test expectations.");
        }

        var avgExecutionTime = result.TestResults.Average(r => r.ExecutionTime.TotalMilliseconds);
        if (avgExecutionTime > 5000)
        {
            recommendations.Add("Average test execution time is high. Consider optimizing API performance.");
        }

        return recommendations;
    }

    private double CalculateConfidenceScore(TestSuiteResult suiteResult)
    {
        if (suiteResult.TotalTests == 0) return 0;

        var baseScore = suiteResult.SuccessRate / 100.0;
        
        // Adjust based on number of tests (more tests = higher confidence)
        var testCountFactor = Math.Min(1.0, suiteResult.TotalTests / 10.0);
        
        // Adjust based on execution consistency
        var executionTimes = suiteResult.TestResults.Select(r => r.ExecutionTime.TotalMilliseconds).ToList();
        var avgTime = executionTimes.Average();
        var consistencyFactor = executionTimes.All(t => Math.Abs(t - avgTime) < avgTime * 0.5) ? 1.0 : 0.9;
        
        return baseScore * testCountFactor * consistencyFactor;
    }

    private void AnalyzeCapabilityResults(CapabilityValidationResult result, TestSuiteResult suiteResult)
    {
        var successfulTests = suiteResult.TestResults.Where(r => r.Success).ToList();
        var failedTests = suiteResult.TestResults.Where(r => !r.Success).ToList();

        // Identify strengths
        if (successfulTests.Any())
        {
            result.Strengths.Add($"{successfulTests.Count} test cases passed successfully");
            
            var avgSuccessTime = successfulTests.Average(t => t.ExecutionTime.TotalMilliseconds);
            if (avgSuccessTime < 1000)
            {
                result.Strengths.Add("Fast execution time for successful operations");
            }
        }

        // Identify weaknesses
        if (failedTests.Any())
        {
            result.Weaknesses.Add($"{failedTests.Count} test cases failed");
            
            var commonErrors = failedTests
                .Where(t => !string.IsNullOrEmpty(t.ErrorMessage))
                .GroupBy(t => t.ErrorMessage)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            
            if (commonErrors != null)
            {
                result.Weaknesses.Add($"Common error pattern: {commonErrors.Key}");
            }
        }

        // Generate improvement suggestions
        if (result.ConfidenceScore < 0.8)
        {
            result.ImprovementSuggestions.Add("Increase test coverage to improve confidence");
        }
        
        if (failedTests.Any(t => t.ErrorMessage?.Contains("timeout") == true))
        {
            result.ImprovementSuggestions.Add("Consider increasing timeout values for slow operations");
        }
    }

    private CapabilityStatus DetermineCapabilityStatus(CapabilityValidationResult result)
    {
        if (result.ConfidenceScore >= 0.9) return CapabilityStatus.Validated;
        if (result.ConfidenceScore >= 0.7) return CapabilityStatus.Learned;
        if (result.ConfidenceScore >= 0.5) return CapabilityStatus.Learning;
        return CapabilityStatus.Failed;
    }

    private Dictionary<string, double> CollectDetailedMetrics(List<TestExecutionResult> testResults)
    {
        var metrics = new Dictionary<string, double>();
        
        if (testResults.Any())
        {
            metrics["AverageExecutionTimeMs"] = testResults.Average(r => r.ExecutionTime.TotalMilliseconds);
            metrics["MedianExecutionTimeMs"] = CalculateMedian(testResults.Select(r => r.ExecutionTime.TotalMilliseconds));
            metrics["StandardDeviationMs"] = CalculateStandardDeviation(testResults.Select(r => r.ExecutionTime.TotalMilliseconds));
            metrics["SuccessfulTestsPercentage"] = (double)testResults.Count(r => r.Success) / testResults.Count * 100;
        }

        return metrics;
    }

    private double CalculateMedian(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var count = sorted.Count;
        
        if (count == 0) return 0;
        if (count % 2 == 0)
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
        else
            return sorted[count / 2];
    }

    private double CalculateStandardDeviation(IEnumerable<double> values)
    {
        var valueList = values.ToList();
        if (!valueList.Any()) return 0;

        var average = valueList.Average();
        var sumOfSquaresOfDifferences = valueList.Sum(val => (val - average) * (val - average));
        return Math.Sqrt(sumOfSquaresOfDifferences / valueList.Count);
    }

    private PerformanceGrade AssignPerformanceGrade(double successRate)
    {
        var percentage = successRate * 100;
        
        return percentage switch
        {
            >= 90 => PerformanceGrade.A,
            >= 80 => PerformanceGrade.B,
            >= 70 => PerformanceGrade.C,
            >= 60 => PerformanceGrade.D,
            _ => PerformanceGrade.F
        };
    }

    private List<string> GeneratePerformanceRecommendations(PerformanceBenchmarkResult benchmark)
    {
        var recommendations = new List<string>();

        if (benchmark.SuccessRate < 0.9)
        {
            recommendations.Add("Improve error handling to increase success rate");
        }

        if (benchmark.AverageExecutionTime.TotalSeconds > 5)
        {
            recommendations.Add("Consider optimizing for faster execution times");
        }

        if (benchmark.MaxExecutionTime.TotalMilliseconds > benchmark.AverageExecutionTime.TotalMilliseconds * 3)
        {
            recommendations.Add("High execution time variance detected - investigate performance inconsistencies");
        }

        return recommendations;
    }

    private Dictionary<string, int> CategorizeFailures(List<TestExecutionResult> failedTests)
    {
        var categories = new Dictionary<string, int>();

        foreach (var test in failedTests)
        {
            var category = CategorizeFailure(test);
            categories[category] = categories.GetValueOrDefault(category, 0) + 1;
        }

        return categories;
    }

    private string CategorizeFailure(TestExecutionResult test)
    {
        if (test.ErrorMessage?.Contains("timeout") == true)
            return "Timeout";
        if (test.ErrorMessage?.Contains("401") == true)
            return "Authentication";
        if (test.ErrorMessage?.Contains("404") == true)
            return "Not Found";
        if (test.ErrorMessage?.Contains("500") == true)
            return "Server Error";
        if (test.AssertionResults.Any(a => !a.Passed))
            return "Assertion Failure";
        
        return "Unknown";
    }

    private async Task<List<CommonFailurePattern>> IdentifyCommonFailurePatternsAsync(List<TestExecutionResult> failedTests)
    {
        var patterns = new List<CommonFailurePattern>();

        // Group by error message
        var errorGroups = failedTests
            .Where(t => !string.IsNullOrEmpty(t.ErrorMessage))
            .GroupBy(t => t.ErrorMessage)
            .Where(g => g.Count() > 1);

        foreach (var group in errorGroups)
        {
            patterns.Add(new CommonFailurePattern
            {
                Pattern = "Error Message",
                Description = group.Key!,
                Frequency = group.Count(),
                AffectedTests = group.Select(t => t.TestCaseName).ToList(),
                Severity = DetermineFailureSeverity(group.Count(), failedTests.Count)
            });
        }

        // Group by assertion failures
        var assertionGroups = failedTests
            .SelectMany(t => t.AssertionResults.Where(a => !a.Passed))
            .GroupBy(a => a.AssertionName)
            .Where(g => g.Count() > 1);

        foreach (var group in assertionGroups)
        {
            patterns.Add(new CommonFailurePattern
            {
                Pattern = "Assertion Failure",
                Description = $"'{group.Key}' assertion consistently failing",
                Frequency = group.Count(),
                Severity = DetermineFailureSeverity(group.Count(), failedTests.Count)
            });
        }

        return patterns;
    }

    private FailureSeverity DetermineFailureSeverity(int frequency, int totalFailures)
    {
        var percentage = (double)frequency / totalFailures;
        
        return percentage switch
        {
            >= 0.8 => FailureSeverity.Critical,
            >= 0.5 => FailureSeverity.High,
            >= 0.3 => FailureSeverity.Medium,
            _ => FailureSeverity.Low
        };
    }

    private List<ImprovementSuggestion> GenerateImprovementSuggestions(TestAnalysisResult analysis)
    {
        var suggestions = new List<ImprovementSuggestion>();

        // Suggestions based on failure categories
        foreach (var category in analysis.FailureCategories.Where(kv => kv.Value > 2))
        {
            suggestions.Add(new ImprovementSuggestion
            {
                Title = $"Address {category.Key} Issues",
                Description = $"Multiple tests ({category.Value}) are failing due to {category.Key.ToLowerInvariant()} issues",
                Priority = SuggestionPriority.High,
                ActionSteps = new List<string>
                {
                    $"Review and fix {category.Key.ToLowerInvariant()} related problems",
                    "Update test expectations if necessary",
                    "Add more robust error handling"
                }
            });
        }

        // Suggestions based on common patterns
        foreach (var pattern in analysis.CommonPatterns.Where(p => p.Severity >= FailureSeverity.High))
        {
            suggestions.Add(new ImprovementSuggestion
            {
                Title = $"Fix {pattern.Pattern} Pattern",
                Description = pattern.Description,
                Priority = pattern.Severity == FailureSeverity.Critical ? SuggestionPriority.Urgent : SuggestionPriority.High,
                ActionSteps = new List<string>
                {
                    "Investigate root cause of pattern",
                    "Implement systematic fix",
                    "Re-run affected tests to validate fix"
                }
            });
        }

        return suggestions;
    }

    private double CalculateHealthScore(TestAnalysisResult analysis)
    {
        if (analysis.TotalFailedTests == 0) return 100;

        var baseScore = 100.0;
        
        // Reduce score based on failure severity
        foreach (var pattern in analysis.CommonPatterns)
        {
            var reduction = pattern.Severity switch
            {
                FailureSeverity.Critical => 30,
                FailureSeverity.High => 20,
                FailureSeverity.Medium => 10,
                FailureSeverity.Low => 5,
                _ => 0
            };
            
            baseScore -= reduction * (pattern.Frequency / (double)analysis.TotalFailedTests);
        }

        return Math.Max(0, baseScore);
    }

    private List<string> IdentifyCriticalIssues(TestAnalysisResult analysis)
    {
        var issues = new List<string>();

        var criticalPatterns = analysis.CommonPatterns.Where(p => p.Severity == FailureSeverity.Critical);
        foreach (var pattern in criticalPatterns)
        {
            issues.Add($"Critical {pattern.Pattern}: {pattern.Description} (affects {pattern.Frequency} tests)");
        }

        if (analysis.OverallHealthScore < 50)
        {
            issues.Add($"Overall system health is critically low: {analysis.OverallHealthScore:F1}%");
        }

        return issues;
    }

    #endregion
}