using System.Text;
using Microsoft.Extensions.Options;
using DigitalMe.Services.Security;

namespace DigitalMe.Middleware;

/// <summary>
/// Middleware for automatic security validation of requests
/// </summary>
public class SecurityValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityValidationMiddleware> _logger;
    private readonly SecuritySettings _securitySettings;

    public SecurityValidationMiddleware(
        RequestDelegate next,
        ILogger<SecurityValidationMiddleware> logger,
        IOptions<SecuritySettings> securitySettings)
    {
        _next = next;
        _logger = logger;
        _securitySettings = securitySettings.Value;
    }

    public async Task InvokeAsync(HttpContext context, ISecurityValidationService securityService)
    {
        try
        {
            // Skip validation for health check and static files
            if (IsSkippablePath(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // 1. Validate request size
            if (context.Request.ContentLength > _securitySettings.MaxPayloadSizeBytes)
            {
                _logger.LogWarning("Request too large: {Size} bytes from {RemoteIp}",
                    context.Request.ContentLength, context.Connection.RemoteIpAddress);
                context.Response.StatusCode = 413; // Payload Too Large
                await context.Response.WriteAsync("Request payload too large");
                return;
            }

            // 2. Rate limiting check
            var clientId = GetClientIdentifier(context);
            var endpoint = $"{context.Request.Method}:{context.Request.Path}";

            if (await securityService.IsRateLimitExceededAsync(clientId, endpoint))
            {
                _logger.LogWarning("Rate limit exceeded for {ClientId} on {Endpoint}", clientId, endpoint);
                context.Response.StatusCode = 429; // Too Many Requests
                await context.Response.WriteAsync("Rate limit exceeded");
                return;
            }

            // 3. Validate webhook payloads for webhook endpoints
            if (IsWebhookEndpoint(context.Request.Path))
            {
                var payload = await ReadRequestBodyAsync(context.Request);

                if (!await securityService.ValidateWebhookPayloadAsync(payload, _securitySettings.MaxPayloadSizeBytes))
                {
                    _logger.LogWarning("Invalid webhook payload from {RemoteIp}", context.Connection.RemoteIpAddress);
                    context.Response.StatusCode = 400; // Bad Request
                    await context.Response.WriteAsync("Invalid payload");
                    return;
                }

                // Reset stream position for next middleware
                context.Request.Body.Position = 0;
            }

            // 4. JWT token validation for API endpoints (except auth endpoints)
            if (IsProtectedApiEndpoint(context.Request.Path))
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrEmpty(authHeader))
                {
                    context.Response.StatusCode = 401; // Unauthorized
                    await context.Response.WriteAsync("Missing authorization header");
                    return;
                }

                var tokenValidation = await securityService.ValidateJwtTokenAsync(authHeader);
                if (!tokenValidation.IsValid)
                {
                    context.Response.StatusCode = 401; // Unauthorized
                    await context.Response.WriteAsync("Invalid token");
                    return;
                }

                // Add claims to context
                foreach (var claim in tokenValidation.Claims)
                {
                    context.Items[$"claim:{claim.Key}"] = claim.Value;
                }
            }

            // 5. Log security events
            LogSecurityEvent(context, clientId);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in security validation middleware");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Internal server error");
        }
    }

    private static bool IsSkippablePath(string path)
    {
        var skippablePaths = new[]
        {
            "/health",
            "/metrics",
            "/swagger",
            "/favicon.ico",
            "/_framework",
            "/css",
            "/js",
            "/images"
        };

        return skippablePaths.Any(sp => path.StartsWith(sp, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsWebhookEndpoint(string path)
    {
        return path.Contains("/webhook", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsProtectedApiEndpoint(string path)
    {
        // API endpoints that require authentication (exclude auth endpoints)
        return path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) &&
               !path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase) &&
               !IsWebhookEndpoint(path);
    }

    private static string GetClientIdentifier(HttpContext context)
    {
        // Try to get client identifier from various sources
        var clientId = context.Request.Headers["X-Client-Id"].FirstOrDefault() ??
                      context.Request.Headers["User-Agent"].FirstOrDefault() ??
                      context.Connection.RemoteIpAddress?.ToString() ??
                      "unknown";

        return clientId;
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        // Enable buffering to allow multiple reads
        request.EnableBuffering();

        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        // Reset position for next middleware
        request.Body.Position = 0;

        return body;
    }

    private void LogSecurityEvent(HttpContext context, string clientId)
    {
        // Log security-relevant events for monitoring
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Security validation passed for {ClientId} {Method} {Path}",
                clientId,
                context.Request.Method,
                context.Request.Path);
        }
    }
}
