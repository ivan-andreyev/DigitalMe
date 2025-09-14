using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Learning.Testing.TestExecution;

/// <summary>
/// Single test execution engine implementation - Breaks circular dependency
/// Focused only on individual test case execution without parallel concerns
/// Extracted from TestExecutor to resolve TestExecutor ↔ ParallelTestRunner cycle
/// </summary>
public class SingleTestExecutor : ISingleTestExecutor
{
    private readonly ILogger<SingleTestExecutor> _logger;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public SingleTestExecutor(
        ILogger<SingleTestExecutor> logger,
        HttpClient httpClient)
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
            using var cts = new CancellationTokenSource(testCase.ExpectedExecutionTime);
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
            result.ErrorMessage = $"Test timeout occurred after {testCase.ExpectedExecutionTime.TotalSeconds} seconds";
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

    #region Private Helper Methods

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
            // Handle relative endpoint - construct full URI from base URI and endpoint
            var baseUri = _httpClient.BaseAddress ?? new Uri("https://localhost");
            var fullUri = new Uri(baseUri, testCase.Endpoint);

            var uriBuilder = new UriBuilder(fullUri);

            // Build query string with proper escaping for each parameter
            var queryParams = testCase.Parameters.Select(p =>
                $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value?.ToString() ?? "")}");

            // Set query without additional processing to preserve encoding
            var queryString = string.Join("&", queryParams);
            uriBuilder.Query = queryString;

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
            AssertionType.ResponseHeader => ExtractResponseHeaderValue(response, assertion.ActualValuePath),
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
            ComparisonOperator.Contains => actualValue.Contains(assertion.ExpectedValue),
            ComparisonOperator.NotContains => !actualValue.Contains(assertion.ExpectedValue),
            ComparisonOperator.StartsWith => actualValue.StartsWith(assertion.ExpectedValue),
            ComparisonOperator.EndsWith => actualValue.EndsWith(assertion.ExpectedValue),
            _ => false
        };
    }

    private string ExtractResponseHeaderValue(HttpResponseMessage response, string headerName)
    {
        try
        {
            // First, try to get from response headers
            if (response.Headers.TryGetValues(headerName, out var responseHeaderValues))
            {
                return responseHeaderValues.FirstOrDefault() ?? "";
            }

            // If not found, try content headers (Content-Type, Content-Length, etc.)
            if (response.Content?.Headers != null)
            {
                if (response.Content.Headers.TryGetValues(headerName, out var contentHeaderValues))
                {
                    return contentHeaderValues.FirstOrDefault() ?? "";
                }

                // Special handling for ContentType which is not directly in TryGetValues
                if (headerName.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    return response.Content.Headers.ContentType?.ToString() ?? "";
                }
            }

            return "";
        }
        catch
        {
            return "";
        }
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

    #endregion
}
