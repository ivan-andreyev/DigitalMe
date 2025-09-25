namespace DigitalMe.Tests.E2E;

/// <summary>
/// Configuration for authentication e2e tests database setup
/// </summary>
public static class AuthTestDatabaseConfig
{
    /// <summary>
    /// Demo user credentials that should be seeded in all environments
    /// </summary>
    public static class DemoCredentials
    {
        public const string Email = "demo@digitalme.ai";
        public const string Password = "Ivan2024!";
        public const string DisplayName = "Demo User";
    }

    /// <summary>
    /// Test user credentials for creating new users during tests
    /// </summary>
    public static class TestCredentials
    {
        public const string Password = "TestPassword123!";
        public const string WeakPassword = "123";
        public const string DomainSuffix = "@digitalme.test";

        /// <summary>
        /// Generate unique test email for registration tests
        /// </summary>
        public static string GenerateTestEmail() =>
            $"test-{Guid.NewGuid():N}{DomainSuffix}";
    }

    /// <summary>
    /// Expected JWT token properties for validation
    /// </summary>
    public static class JwtExpectations
    {
        public const string ExpectedIssuer = "DigitalMe.API";
        public const string ExpectedAudience = "DigitalMe.Client";
        public const int MaxExpiryHours = 24;
        public const int MinExpiryMinutes = 60;

        /// <summary>
        /// Expected roles for demo user
        /// </summary>
        public static readonly string[] DemoUserRoles = { "User", "Admin" };
    }

    /// <summary>
    /// Security test payloads for validation
    /// </summary>
    public static class SecurityTestData
    {
        public static readonly string[] SqlInjectionPayloads =
        {
            "admin'; DROP TABLE Users; --",
            "' OR '1'='1",
            "'; UPDATE Users SET password='hacked' WHERE '1'='1'; --",
            "admin'/**/OR/**/'1'='1",
            "1' UNION SELECT * FROM Users--"
        };

        public static readonly string[] XssPayloads =
        {
            "<script>alert('xss')</script>@example.com",
            "javascript:alert('xss')@example.com",
            "<img src=x onerror=alert('xss')>@example.com",
            "test@<script>alert('xss')</script>.com"
        };

        public static readonly string[] WeakPasswords =
        {
            "",           // Empty
            "123",        // Too short
            "password",   // Common
            "123456",     // Numeric only
            "abcdef",     // Letters only
            "a",          // Single char
            "          "  // Only spaces
        };

        public static readonly string[] InvalidEmails =
        {
            "",
            "not-an-email",
            "@example.com",
            "test@",
            "test..test@example.com",
            "test@example",
            "test@.example.com"
        };
    }

    /// <summary>
    /// Performance test thresholds
    /// </summary>
    public static class PerformanceThresholds
    {
        public static readonly TimeSpan MaxAuthenticationTime = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan MaxTokenValidationTime = TimeSpan.FromSeconds(1);
        public static readonly TimeSpan MaxLargePayloadTime = TimeSpan.FromSeconds(10);
        public const int TokenValidationTestRequests = 5;
    }

    /// <summary>
    /// Database seeding requirements for e2e tests
    /// </summary>
    public static class DatabaseRequirements
    {
        /// <summary>
        /// Verify that required demo users exist in the target environment
        /// </summary>
        public static bool ShouldSeedDemoUsers(string environment) =>
            environment is "local" or "staging";

        /// <summary>
        /// Check if environment supports user creation tests
        /// </summary>
        public static bool SupportsUserCreation(string environment) =>
            environment != "production"; // Don't create test users in production

        /// <summary>
        /// Get expected demo user data for validation
        /// </summary>
        public static (string email, string[] roles) GetExpectedDemoUser() =>
            (DemoCredentials.Email, JwtExpectations.DemoUserRoles);
    }
}