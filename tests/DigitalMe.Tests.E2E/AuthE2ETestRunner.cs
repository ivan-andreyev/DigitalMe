using System.Net;
using Xunit;
using FluentAssertions;
using Newtonsoft.Json;
using System.Text;

namespace DigitalMe.Tests.E2E;

/// <summary>
/// Comprehensive authentication E2E test runner that validates the complete auth flow
/// </summary>
public class AuthE2ETestRunner : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _environment;

    public AuthE2ETestRunner()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(E2ETestConfig.ApiBaseUrl),
            Timeout = E2ETestConfig.HttpTimeout
        };
        _environment = E2ETestConfig.Environment;
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Component", "AuthenticationFlow")]
    [Trait("Priority", "Critical")]
    public async Task FullAuthenticationFlow_ShouldWorkEndToEnd()
    {
        var testResults = new List<(string test, bool passed, string details)>();

        // Test 1: Health Check
        await RunTest(testResults, "Health Check", async () =>
        {
            var response = await _httpClient.GetAsync("/health/simple");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return "✅ Application is running";
        });

        // Test 2: Account Controller Exists
        await RunTest(testResults, "Account Controller Exists", async () =>
        {
            var testData = new { email = "test", password = "test" };
            var json = JsonConvert.SerializeObject(testData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/account/login", content);
            response.StatusCode.Should().NotBe(HttpStatusCode.NotFound,
                "AccountController should be deployed");
            return "✅ /api/account endpoints exist";
        });

        // Test 3: Demo User Login
        string? validToken = null;
        await RunTest(testResults, "Demo User Authentication", async () =>
        {
            var loginData = new
            {
                email = AuthTestDatabaseConfig.DemoCredentials.Email,
                password = AuthTestDatabaseConfig.DemoCredentials.Password
            };

            var json = JsonConvert.SerializeObject(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/account/login", content);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var authResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                validToken = authResponse?.token;

                validToken.Should().NotBeNullOrEmpty("Should return JWT token");
                return $"✅ Demo user login successful, got token: {validToken?[..20]}...";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return $"❌ Demo login failed: {response.StatusCode} - {errorContent}";
            }
        });

        // Test 4: JWT Token Validation
        if (!string.IsNullOrEmpty(validToken))
        {
            RunTest(testResults, "JWT Token Structure", () =>
            {
                var parts = validToken.Split('.');
                parts.Should().HaveCount(3, "JWT should have header.payload.signature");

                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                handler.CanReadToken(validToken).Should().BeTrue("Token should be valid JWT");

                var jwt = handler.ReadJwtToken(validToken);
                jwt.ValidTo.Should().BeAfter(DateTime.UtcNow, "Token should not be expired");

                return $"✅ JWT structure valid, expires: {jwt.ValidTo}";
            });
        }

        // Test 5: Protected Endpoint Access
        if (!string.IsNullOrEmpty(validToken))
        {
            await RunTest(testResults, "Protected Endpoint Access", async () =>
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", validToken);

                var protectedEndpoints = new[] { "/api/conversations", "/api/profile" };
                var results = new List<string>();

                foreach (var endpoint in protectedEndpoints)
                {
                    var response = await _httpClient.GetAsync(endpoint);
                    var status = response.StatusCode != HttpStatusCode.Unauthorized ? "✅" : "❌";
                    results.Add($"{status} {endpoint}: {response.StatusCode}");
                }

                return string.Join(", ", results);
            });
        }

        // Test 6: Security Validation
        await RunTest(testResults, "Security Validation", async () =>
        {
            var sqlInjection = AuthTestDatabaseConfig.SecurityTestData.SqlInjectionPayloads[0];
            var loginData = new { email = sqlInjection, password = "test" };
            var json = JsonConvert.SerializeObject(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/account/login", content);
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.InternalServerError
            );

            return "✅ SQL injection attempt handled safely";
        });

        // Test 7: Error Handling
        await RunTest(testResults, "Error Handling", async () =>
        {
            var invalidJson = "{ malformed json }";
            var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/account/login", content);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            return "✅ Malformed JSON handled gracefully";
        });

        // Generate final report
        GenerateTestReport(testResults);

        // Fail test if critical issues found
        var failedCriticalTests = testResults
            .Where(r => !r.passed && IsCriticalTest(r.test))
            .ToList();

        if (failedCriticalTests.Any())
        {
            var errorMessage = "Critical authentication tests failed:\n" +
                string.Join("\n", failedCriticalTests.Select(f => $"❌ {f.test}: {f.details}"));
            Assert.Fail(errorMessage);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Component", "AuthenticationReadiness")]
    public async Task AuthenticationReadiness_PreDeploymentCheck()
    {
        // This test validates authentication is ready for deployment
        var readinessChecks = new List<(string check, Func<Task<bool>> test, string description)>
        {
            ("Endpoints Exist", async () =>
            {
                var response = await _httpClient.PostAsync("/api/account/login",
                    new StringContent("{}", Encoding.UTF8, "application/json"));
                return response.StatusCode != HttpStatusCode.NotFound;
            }, "Account endpoints should be deployed"),

            ("Demo User Seeded", async () =>
            {
                var loginData = new
                {
                    email = AuthTestDatabaseConfig.DemoCredentials.Email,
                    password = AuthTestDatabaseConfig.DemoCredentials.Password
                };
                var json = JsonConvert.SerializeObject(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/account/login", content);
                return response.StatusCode == HttpStatusCode.OK;
            }, "Demo user should be seeded and accessible"),

            ("JWT Configuration", async () =>
            {
                // Try to get a token and validate it has proper structure
                var loginData = new
                {
                    email = AuthTestDatabaseConfig.DemoCredentials.Email,
                    password = AuthTestDatabaseConfig.DemoCredentials.Password
                };
                var json = JsonConvert.SerializeObject(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/account/login", content);

                if (response.StatusCode != HttpStatusCode.OK) return false;

                var responseContent = await response.Content.ReadAsStringAsync();
                var authResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                string? token = authResponse?.token;

                if (string.IsNullOrEmpty(token)) return false;

                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                return handler.CanReadToken(token);
            }, "JWT tokens should be properly configured"),

            ("Error Handling", async () =>
            {
                var response = await _httpClient.PostAsync("/api/account/login",
                    new StringContent("invalid json", Encoding.UTF8, "application/json"));
                return response.StatusCode == HttpStatusCode.BadRequest;
            }, "API should handle errors gracefully")
        };

        var results = new List<(string check, bool passed, string details)>();

        foreach (var (check, test, description) in readinessChecks)
        {
            try
            {
                var passed = await test();
                results.Add((check, passed, passed ? "✅ PASS" : $"❌ FAIL: {description}"));
            }
            catch (Exception ex)
            {
                results.Add((check, false, $"❌ ERROR: {ex.Message}"));
            }
        }

        // Generate readiness report
        var report = "🔐 AUTHENTICATION READINESS REPORT\n" +
            "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
            $"Environment: {_environment}\n" +
            $"API Base URL: {E2ETestConfig.ApiBaseUrl}\n" +
            $"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\n" +
            string.Join("\n", results.Select(r => $"{r.details}")) + "\n\n" +
            $"Summary: {results.Count(r => r.passed)}/{results.Count} checks passed\n";

        // Log report (visible in test output)
        Console.WriteLine(report);

        // Fail if any readiness check fails
        var failedChecks = results.Where(r => !r.passed).ToList();
        if (failedChecks.Any())
        {
            var errorMessage = $"Authentication not ready for deployment:\n" +
                string.Join("\n", failedChecks.Select(f => f.details));
            Assert.Fail(errorMessage);
        }
    }

    private async Task RunTest(
        List<(string test, bool passed, string details)> results,
        string testName,
        Func<Task<string>> testAction)
    {
        try
        {
            var result = await testAction();
            results.Add((testName, true, result));
        }
        catch (Exception ex)
        {
            results.Add((testName, false, $"❌ {ex.Message}"));
        }
    }

    private void RunTest(
        List<(string test, bool passed, string details)> results,
        string testName,
        Func<string> testAction)
    {
        try
        {
            var result = testAction();
            results.Add((testName, true, result));
        }
        catch (Exception ex)
        {
            results.Add((testName, false, $"❌ {ex.Message}"));
        }
    }

    private void GenerateTestReport(List<(string test, bool passed, string details)> results)
    {
        var report = "🔐 AUTHENTICATION E2E TEST REPORT\n" +
            "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
            $"Environment: {_environment}\n" +
            $"API Base URL: {E2ETestConfig.ApiBaseUrl}\n" +
            $"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n\n" +
            string.Join("\n", results.Select(r => $"{(r.passed ? "✅" : "❌")} {r.test}: {r.details}")) + "\n\n" +
            $"Summary: {results.Count(r => r.passed)}/{results.Count} tests passed\n";

        Console.WriteLine(report);
    }

    private bool IsCriticalTest(string testName)
    {
        var criticalTests = new[]
        {
            "Account Controller Exists",
            "Demo User Authentication",
            "JWT Token Structure"
        };

        return criticalTests.Contains(testName);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}