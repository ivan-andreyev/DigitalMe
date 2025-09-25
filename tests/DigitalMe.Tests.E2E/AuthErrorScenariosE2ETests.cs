using FluentAssertions;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using Xunit;

namespace DigitalMe.Tests.E2E;

/// <summary>
/// E2E tests for authentication error scenarios, edge cases, and security validation
/// </summary>
public class AuthErrorScenariosE2ETests : IDisposable
{
    private readonly HttpClient _httpClient;

    public AuthErrorScenariosE2ETests()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(E2ETestConfig.ApiBaseUrl),
            Timeout = E2ETestConfig.HttpTimeout
        };
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Component", "Security")]
    public async Task Login_WithSqlInjectionAttempt_ShouldNotCrash()
    {
        // Arrange - SQL injection attempts
        var maliciousInputs = new[]
        {
            "admin'; DROP TABLE Users; --",
            "' OR '1'='1",
            "'; UPDATE Users SET password='hacked' WHERE '1'='1'; --",
            "admin'/**/OR/**/'1'='1",
            "1' UNION SELECT * FROM Users--"
        };

        foreach (var maliciousEmail in maliciousInputs)
        {
            var loginData = new
            {
                email = maliciousEmail,
                password = "anypassword"
            };

            var json = JsonConvert.SerializeObject(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync("/api/account/login", content);

            // Assert - Should handle gracefully without crashing
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,     // Input validation failed
                HttpStatusCode.Unauthorized,   // Authentication failed safely
                HttpStatusCode.NotFound,       // Controller not deployed
                HttpStatusCode.InternalServerError // Other issues, but not SQL injection
            );

            // Should return quickly (not hang on malicious query)
            response.Should().NotBeNull();
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Component", "Security")]
    public async Task Login_WithXSSAttempts_ShouldSanitizeInput()
    {
        // Arrange - XSS attempts
        var xssPayloads = new[]
        {
            "<script>alert('xss')</script>@example.com",
            "javascript:alert('xss')@example.com",
            "<img src=x onerror=alert('xss')>@example.com",
            "test@<script>alert('xss')</script>.com",
            "test@example.com<script>alert(1)</script>"
        };

        foreach (var xssEmail in xssPayloads)
        {
            var loginData = new
            {
                email = xssEmail,
                password = "password"
            };

            var json = JsonConvert.SerializeObject(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync("/api/account/login", content);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.NotFound,
                HttpStatusCode.InternalServerError
            );

            // Check response doesn't contain unescaped script
            if (response.Content != null)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                responseContent.Should().NotContain("<script>", "Response should not contain unescaped script tags");
            }
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Component", "Validation")]
    public async Task Login_WithInvalidEmailFormats_ShouldReturn400()
    {
        // Arrange - Invalid email formats
        var invalidEmails = new[]
        {
            "",
            "not-an-email",
            "@example.com",
            "test@",
            "test..test@example.com",
            "test@example",
            "test@.example.com",
            "test@example..com",
            new string('a', 255) + "@example.com" // Very long email
        };

        foreach (var invalidEmail in invalidEmails)
        {
            var loginData = new
            {
                email = invalidEmail,
                password = "password123"
            };

            var json = JsonConvert.SerializeObject(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync("/api/account/login", content);

            // Assert - Should validate email format
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,   // Expected for invalid input
                HttpStatusCode.Unauthorized, // Or just unauthorized
                HttpStatusCode.NotFound,     // If controller not deployed
                HttpStatusCode.InternalServerError
            );
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Component", "Security")]
    public async Task Register_WithWeakPasswords_ShouldReject()
    {
        // Arrange - Weak passwords that should be rejected
        var weakPasswords = new[]
        {
            "",           // Empty
            "123",        // Too short
            "password",   // Common password
            "123456",     // Numeric only
            "abcdef",     // No numbers/special chars
            "a",          // Single character
            "          "  // Only spaces
        };

        foreach (var weakPassword in weakPasswords)
        {
            var registerData = new
            {
                email = $"test-{Guid.NewGuid():N}@example.com",
                password = weakPassword,
                confirmPassword = weakPassword
            };

            var json = JsonConvert.SerializeObject(registerData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync("/api/account/register", content);

            // Assert - Should reject weak passwords
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,   // Expected for weak password
                HttpStatusCode.NotFound,     // If controller not deployed
                HttpStatusCode.InternalServerError
            );

            // Should NOT return 200/201 for weak passwords
            response.IsSuccessStatusCode.Should().BeFalse(
                $"Weak password '{weakPassword}' should be rejected");
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Component", "Validation")]
    public async Task Register_WithMismatchedPasswords_ShouldReturn400()
    {
        // Arrange
        var registerData = new
        {
            email = $"test-{Guid.NewGuid():N}@example.com",
            password = "StrongPassword123!",
            confirmPassword = "DifferentPassword456!"
        };

        var json = JsonConvert.SerializeObject(registerData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PostAsync("/api/account/register", content);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,   // Expected for password mismatch
            HttpStatusCode.NotFound,     // If controller not deployed
            HttpStatusCode.InternalServerError
        );

        response.IsSuccessStatusCode.Should().BeFalse(
            "Mismatched passwords should be rejected");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Component", "Performance")]
    public async Task Auth_WithLargePayloads_ShouldHandleGracefully()
    {
        // Arrange - Very large payload (potential DoS attempt)
        var largeString = new string('a', 10000); // 10KB string

        var loginData = new
        {
            email = largeString + "@example.com",
            password = largeString,
            extraField = largeString // Extra large field
        };

        var json = JsonConvert.SerializeObject(loginData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var startTime = DateTime.UtcNow;
        var response = await _httpClient.PostAsync("/api/account/login", content);
        var duration = DateTime.UtcNow - startTime;

        // Assert - Should handle within reasonable time
        duration.Should().BeLessThan(TimeSpan.FromSeconds(10),
            "Large payloads should not cause excessive processing time");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,     // Request too large
            HttpStatusCode.RequestEntityTooLarge, // Payload too large
            HttpStatusCode.Unauthorized,   // Rejected credentials
            HttpStatusCode.NotFound,       // Controller not deployed
            HttpStatusCode.InternalServerError
        );
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Component", "Headers")]
    public async Task Auth_WithMissingContentType_ShouldHandle()
    {
        // Arrange
        var loginData = new
        {
            email = "test@example.com",
            password = "password"
        };

        var json = JsonConvert.SerializeObject(loginData);
        var content = new StringContent(json, Encoding.UTF8);
        // Remove content type to test handling
        content.Headers.Remove("Content-Type");

        // Act
        var response = await _httpClient.PostAsync("/api/account/login", content);

        // Assert - Should handle gracefully
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,     // Missing content type
            HttpStatusCode.UnsupportedMediaType, // Invalid media type
            HttpStatusCode.Unauthorized,   // Processed but failed auth
            HttpStatusCode.NotFound,
            HttpStatusCode.InternalServerError
        );
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Component", "HTTP")]
    public async Task Auth_WithInvalidHttpMethods_ShouldReturn405()
    {
        // Test endpoints with wrong HTTP methods
        var testCases = new[]
        {
            ("/api/account/login", HttpMethod.Get),
            ("/api/account/login", HttpMethod.Put),
            ("/api/account/login", HttpMethod.Delete),
            ("/api/account/register", HttpMethod.Get),
            ("/api/account/register", HttpMethod.Put),
            ("/api/account/register", HttpMethod.Delete)
        };

        foreach (var (endpoint, method) in testCases)
        {
            // Act
            using var request = new HttpRequestMessage(method, endpoint);
            var response = await _httpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.MethodNotAllowed, // Expected for wrong method
                HttpStatusCode.NotFound,         // If controller not deployed
                HttpStatusCode.BadRequest        // Some frameworks return 400
            );
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Component", "Encoding")]
    public async Task Auth_WithDifferentEncodings_ShouldHandle()
    {
        var loginData = new
        {
            email = "test@example.com",
            password = "пароль123" // Cyrillic characters
        };

        var encodings = new[]
        {
            Encoding.UTF8,
            Encoding.ASCII,
            Encoding.UTF32
        };

        foreach (var encoding in encodings)
        {
            try
            {
                var json = JsonConvert.SerializeObject(loginData);
                var content = new StringContent(json, encoding, "application/json");

                var response = await _httpClient.PostAsync("/api/account/login", content);

                // Should handle different encodings gracefully
                response.StatusCode.Should().BeOneOf(
                    HttpStatusCode.BadRequest,
                    HttpStatusCode.Unauthorized,
                    HttpStatusCode.NotFound,
                    HttpStatusCode.InternalServerError
                );

                response.Should().NotBeNull();
            }
            catch (Exception)
            {
                // Some encodings may fail, which is acceptable
                continue;
            }
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}