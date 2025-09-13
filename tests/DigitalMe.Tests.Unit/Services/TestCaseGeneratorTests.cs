using Microsoft.Extensions.Logging;
using Moq;
using DigitalMe.Services.Learning;
using DigitalMe.Services.Learning.Testing.TestGeneration;

namespace DigitalMe.Tests.Unit.Services;

/// <summary>
/// Unit tests for TestCaseGenerator - SOLID architecture refactoring
/// Tests the extracted test case generation functionality following SRP
/// </summary>
public class TestCaseGeneratorTests : IDisposable
{
    private readonly Mock<ILogger<TestCaseGenerator>> _mockLogger;
    private readonly TestCaseGenerator _generator;

    public TestCaseGeneratorTests()
    {
        _mockLogger = new Mock<ILogger<TestCaseGenerator>>();
        _generator = new TestCaseGenerator(_mockLogger.Object);
    }

    #region Test Case Generation Tests

    [Fact]
    public async Task GenerateTestCasesAsync_WithValidApiDocumentation_GeneratesTestCases()
    {
        // Arrange
        var apiDocumentation = CreateSampleApiDocumentation();

        // Act
        var testCases = await _generator.GenerateTestCasesAsync(apiDocumentation);

        // Assert
        Assert.NotEmpty(testCases);
        Assert.All(testCases, tc => Assert.NotEmpty(tc.Name));
        Assert.All(testCases, tc => Assert.NotEmpty(tc.Endpoint));
        Assert.All(testCases, tc => Assert.NotEmpty(tc.HttpMethod));
        Assert.Contains(testCases, tc => tc.Priority == TestPriority.High);
    }

    [Fact]
    public async Task GenerateEndpointTestCasesAsync_WithRequiredParameters_GeneratesParameterValidationTests()
    {
        // Arrange
        var apiDoc = CreateSampleApiDocumentation();
        var endpoint = new ApiEndpoint 
        { 
            Method = "POST", 
            Path = "/api/users",
            Parameters = new List<ApiParameter>
            {
                new ApiParameter { Name = "username", Required = true, Type = "string" },
                new ApiParameter { Name = "email", Required = true, Type = "string" }
            }
        };

        // Act
        var testCases = await _generator.GenerateEndpointTestCasesAsync(apiDoc, endpoint);

        // Assert
        Assert.NotEmpty(testCases);
        // Should generate happy path + missing parameter tests
        Assert.True(testCases.Count >= 3); // 1 happy path + 2 missing parameter tests
        Assert.Contains(testCases, tc => tc.Name.Contains("HappyPath"));
        Assert.Contains(testCases, tc => tc.Name.Contains("Missing_username"));
        Assert.Contains(testCases, tc => tc.Name.Contains("Missing_email"));
    }

    [Fact]
    public void GenerateErrorHandlingTestCases_WithAuthentication_GeneratesUnauthorizedTest()
    {
        // Arrange
        var apiDoc = CreateSampleApiDocumentation();
        apiDoc.Authentication = AuthenticationMethod.Bearer;

        // Act
        var testCases = _generator.GenerateErrorHandlingTestCases(apiDoc);

        // Assert
        Assert.NotEmpty(testCases);
        Assert.Contains(testCases, tc => tc.Name == "Unauthorized_Access_Test");
        Assert.Contains(testCases, tc => tc.Name == "Not_Found_Test");
    }

    [Fact]
    public void GenerateAuthenticationTestCases_WithValidAuthentication_GeneratesAuthTest()
    {
        // Arrange
        var apiDoc = CreateSampleApiDocumentation();
        apiDoc.Authentication = AuthenticationMethod.Bearer;

        // Act
        var testCases = _generator.GenerateAuthenticationTestCases(apiDoc);

        // Assert
        Assert.NotEmpty(testCases);
        Assert.Contains(testCases, tc => tc.Name == "Valid_Authentication_Test");
        Assert.All(testCases, tc => Assert.Equal(TestPriority.Critical, tc.Priority));
    }

    [Fact]
    public async Task GenerateExampleBasedTestCasesAsync_WithCodeExamples_GeneratesExampleTests()
    {
        // Arrange
        var apiDoc = CreateSampleApiDocumentation();
        apiDoc.Examples = new List<CodeExample>
        {
            new CodeExample 
            { 
                Language = "javascript", 
                Code = "fetch('/api/users', { method: 'POST' })",
                Endpoint = "/api/users",
                Description = "Create user example"
            }
        };

        // Act
        var testCases = await _generator.GenerateExampleBasedTestCasesAsync(apiDoc);

        // Assert
        Assert.NotEmpty(testCases);
        Assert.Contains(testCases, tc => tc.Name.Contains("Example_Based_Test"));
        Assert.Contains(testCases, tc => tc.HttpMethod == "POST");
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
                    Endpoint = "/api/users",
                    Description = "Get users example"
                }
            },
            Authentication = AuthenticationMethod.None,
            RequiredHeaders = new List<string>()
        };
    }

    #endregion

    public void Dispose()
    {
        // Clean up resources if needed
    }
}