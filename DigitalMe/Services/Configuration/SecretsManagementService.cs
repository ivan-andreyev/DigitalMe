using System.Security.Cryptography;
using System.Text;

namespace DigitalMe.Services.Configuration;

/// <summary>
/// Production-ready secrets management service with environment variable fallbacks
/// </summary>
public class SecretsManagementService : ISecretsManagementService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SecretsManagementService> _logger;

    public SecretsManagementService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<SecretsManagementService> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public string? GetSecret(string secretKey, string? environmentVariableName = null)
    {
        try
        {
            // Priority 1: Configuration (includes User Secrets in development)
            var configValue = _configuration[secretKey];
            if (!string.IsNullOrWhiteSpace(configValue) && !IsPlaceholderValue(configValue))
            {
                return configValue;
            }

            // Priority 2: Environment variable fallback (exact name)
            if (!string.IsNullOrEmpty(environmentVariableName))
            {
                var envValue = GetEnvironmentVariableCrossPlatform(environmentVariableName);
                if (!string.IsNullOrWhiteSpace(envValue))
                {
                    _logger.LogDebug("Secret '{SecretKey}' loaded from environment variable '{EnvVar}'",
                        secretKey, environmentVariableName);
                    return envValue;
                }
            }

            // Priority 3: Auto-detect environment variable from key
            var autoEnvVar = ConvertToEnvironmentVariableName(secretKey);
            var autoEnvValue = GetEnvironmentVariableCrossPlatform(autoEnvVar);
            if (!string.IsNullOrWhiteSpace(autoEnvValue))
            {
                _logger.LogDebug("Secret '{SecretKey}' loaded from auto-detected environment variable '{EnvVar}'",
                    secretKey, autoEnvVar);
                return autoEnvValue;
            }


            _logger.LogWarning("Secret '{SecretKey}' not found in configuration or environment variables", secretKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret '{SecretKey}'", secretKey);
            return null;
        }
    }

    public string GetRequiredSecret(string secretKey, string? environmentVariableName = null)
    {
        var secret = GetSecret(secretKey, environmentVariableName);

        if (string.IsNullOrWhiteSpace(secret))
        {
            var envVarInfo = !string.IsNullOrEmpty(environmentVariableName)
                ? $" or environment variable '{environmentVariableName}'"
                : "";

            throw new InvalidOperationException(
                $"Required secret '{secretKey}' not found in configuration{envVarInfo}. " +
                $"Please configure this value using User Secrets (development) or environment variables (production).");
        }

        return secret;
    }

    public SecretsValidationResult ValidateSecrets()
    {
        var result = new SecretsValidationResult { IsValid = true };

        // Critical secrets that must be present in production
        var criticalSecrets = new Dictionary<string, string>
        {
            { "Anthropic:ApiKey", "ANTHROPIC_API_KEY" },
            { "JWT:Key", "JWT_KEY" }
        };

        // Optional but recommended secrets
        var recommendedSecrets = new Dictionary<string, string>
        {
            { "Integrations:GitHub:PersonalAccessToken", "GITHUB_TOKEN" },
            { "Integrations:Telegram:BotToken", "TELEGRAM_BOT_TOKEN" },
            { "Integrations:Google:ClientSecret", "GOOGLE_CLIENT_SECRET" }
        };

        // Validate critical secrets
        foreach (var (key, envVar) in criticalSecrets)
        {
            var secret = GetSecret(key, envVar);
            if (string.IsNullOrWhiteSpace(secret))
            {
                result.MissingSecrets.Add($"{key} (or {envVar})");
                result.IsValid = false;
            }
            else
            {
                ValidateSecretStrength(key, secret, result);
            }
        }

        // Check recommended secrets
        foreach (var (key, envVar) in recommendedSecrets)
        {
            var secret = GetSecret(key, envVar);
            if (string.IsNullOrWhiteSpace(secret))
            {
                result.Warnings.Add($"Optional secret '{key}' not configured - some integrations may not work");
            }
        }

        // Security recommendations
        if (IsSecureEnvironment())
        {
            result.SecurityRecommendations.Add("Production environment detected - ensure all secrets are configured via environment variables or secure key vault");

            // Check if any secrets are still using placeholder values
            foreach (var (key, _) in criticalSecrets.Concat(recommendedSecrets))
            {
                var configValue = _configuration[key];
                if (!string.IsNullOrEmpty(configValue) && IsPlaceholderValue(configValue))
                {
                    result.WeakSecrets.Add($"{key} is using a placeholder value");
                    result.IsValid = false;
                }
            }
        }
        else
        {
            result.SecurityRecommendations.Add("Development environment - consider using User Secrets for sensitive configuration");
        }

        return result;
    }

    public string GetSecureJwtKey()
    {
        // Try to get configured key first
        var configuredKey = GetSecret("JWT:Key", "JWT_KEY");

        if (!string.IsNullOrWhiteSpace(configuredKey) && !IsPlaceholderValue(configuredKey))
        {
            // Validate key strength
            if (configuredKey.Length < 32)
            {
                _logger.LogWarning("Configured JWT key is shorter than recommended 32 characters ({Length} chars)",
                    configuredKey.Length);
            }

            if (!IsSecureEnvironment() && configuredKey.Contains("YourSuperSecretKey"))
            {
                _logger.LogWarning("JWT key appears to be a default/example value - consider updating for production use");
            }

            return configuredKey;
        }

        // Generate secure key if not configured and we're not in production
        if (!IsSecureEnvironment())
        {
            _logger.LogInformation("Generating secure JWT key for development environment");
            return GenerateSecureKey(64); // 64 characters for extra security
        }

        // In production, require explicit configuration
        throw new InvalidOperationException(
            "JWT key must be explicitly configured in production environment. " +
            "Set JWT:Key in configuration or JWT_KEY environment variable.");
    }

    public bool IsSecureEnvironment()
    {
        return _environment.IsProduction() || _environment.IsStaging();
    }

    public bool IsTestEnvironment()
    {
        return _environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase);
    }

    // Private helper methods

    private static bool IsPlaceholderValue(string value)
    {
        var placeholders = new[]
        {
            "your-api-key-here",
            "YOUR_API_KEY_HERE",
            "your-token-here",
            "YOUR_TOKEN_HERE",
            "YourSuperSecretKey",
            "your-secret-here",
            "YOUR_SECRET_HERE",
            "sk-ant-your-",
            "ghp_your-",
            "",
            " "
        };

        return placeholders.Any(placeholder =>
            value.Contains(placeholder, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(value));
    }

    private static string ConvertToEnvironmentVariableName(string configKey)
    {
        // Convert "Anthropic:ApiKey" to "ANTHROPIC_API_KEY"
        return configKey
            .Replace(":", "_")
            .Replace(".", "_")
            .ToUpperInvariant();
    }

    private void ValidateSecretStrength(string key, string secret, SecretsValidationResult result)
    {
        // JWT key validation
        if (key.Contains("JWT") && key.Contains("Key"))
        {
            if (secret.Length < 32)
            {
                result.WeakSecrets.Add($"{key}: JWT key should be at least 32 characters (current: {secret.Length})");
            }
        }

        // API key validation
        if (key.Contains("ApiKey") || key.Contains("Token"))
        {
            if (secret.Length < 20)
            {
                result.WeakSecrets.Add($"{key}: API key/token appears too short (current: {secret.Length} chars)");
            }
        }

        // Check for common weak patterns
        if (IsPlaceholderValue(secret))
        {
            result.WeakSecrets.Add($"{key}: Using placeholder or default value");
        }
    }

    private string? GetEnvironmentVariableCrossPlatform(string variableName)
    {
        try
        {
            _logger.LogInformation("🔧 BLOCKER #2 FIX: Attempting to get environment variable '{VarName}'", variableName);

            // Try standard .NET environment variable access first
            var envValue = Environment.GetEnvironmentVariable(variableName);
            _logger.LogInformation("🔧 .NET Environment.GetEnvironmentVariable('{VarName}') = '{Value}'",
                variableName, envValue ?? "null");

            if (!string.IsNullOrWhiteSpace(envValue))
            {
                _logger.LogInformation("🔧 BLOCKER #2 FIX: Found via standard .NET - '{VarName}' = {Length} chars",
                    variableName, envValue.Length);
                return envValue;
            }

            // On Windows with Git Bash/WSL, try alternative approaches
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    // Try PowerShell environment variable access
                    var processInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-Command \"$env:{variableName}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using var process = System.Diagnostics.Process.Start(processInfo);
                    if (process != null)
                    {
                        var output = process.StandardOutput.ReadToEnd().Trim();
                        process.WaitForExit();

                        if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                        {
                            _logger.LogDebug("Environment variable '{VarName}' found via PowerShell: {ValueLength} chars",
                                variableName, output.Length);
                            return output;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "PowerShell environment variable access failed for '{VarName}'", variableName);
                }

                try
                {
                    // Try cmd environment variable access
                    var cmdInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c echo %{variableName}%",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using var cmdProcess = System.Diagnostics.Process.Start(cmdInfo);
                    if (cmdProcess != null)
                    {
                        var cmdOutput = cmdProcess.StandardOutput.ReadToEnd().Trim();
                        cmdProcess.WaitForExit();

                        if (cmdProcess.ExitCode == 0 && !string.IsNullOrWhiteSpace(cmdOutput) &&
                            !cmdOutput.Equals($"%{variableName}%", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogDebug("Environment variable '{VarName}' found via cmd: {ValueLength} chars",
                                variableName, cmdOutput.Length);
                            return cmdOutput;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "CMD environment variable access failed for '{VarName}'", variableName);
                }
            }

            _logger.LogDebug("Environment variable '{VarName}' not found in any context", variableName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing environment variable '{VarName}'", variableName);
            return null;
        }
    }

    private static string GenerateSecureKey(int lengthInBytes)
    {
        using var rng = RandomNumberGenerator.Create();
        var keyBytes = new byte[lengthInBytes];
        rng.GetBytes(keyBytes);
        return Convert.ToBase64String(keyBytes);
    }
}
