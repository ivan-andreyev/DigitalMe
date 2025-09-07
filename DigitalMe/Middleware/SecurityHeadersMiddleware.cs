using Microsoft.Extensions.Options;

namespace DigitalMe.Middleware;

/// <summary>
/// Middleware for adding production security headers to HTTP responses
/// Implements security headers as required by MVP Phase 6 Security Hardening
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Call the next delegate/middleware in the pipeline
            await _next(context);

            // Add security headers after the request is processed
            AddSecurityHeaders(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in security headers middleware");
            throw;
        }
    }

    private static void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;
        
        // Only add headers if they don't already exist (avoid duplicates)
        
        // X-Frame-Options: Prevent the page from being displayed in a frame
        if (!response.Headers.ContainsKey("X-Frame-Options"))
        {
            response.Headers["X-Frame-Options"] = "DENY";
        }
        
        // X-Content-Type-Options: Prevent MIME type sniffing
        if (!response.Headers.ContainsKey("X-Content-Type-Options"))
        {
            response.Headers["X-Content-Type-Options"] = "nosniff";
        }
        
        // X-XSS-Protection: Enable XSS filtering
        if (!response.Headers.ContainsKey("X-XSS-Protection"))
        {
            response.Headers["X-XSS-Protection"] = "1; mode=block";
        }
        
        // Referrer-Policy: Control referrer information
        if (!response.Headers.ContainsKey("Referrer-Policy"))
        {
            response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        }
        
        // Content-Security-Policy: Define valid sources for content
        if (!response.Headers.ContainsKey("Content-Security-Policy"))
        {
            var cspPolicy = "default-src 'self'; " +
                          "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                          "style-src 'self' 'unsafe-inline'; " +
                          "img-src 'self' data: https:; " +
                          "font-src 'self'; " +
                          "connect-src 'self'; " +
                          "media-src 'self'; " +
                          "object-src 'none'; " +
                          "frame-src 'none'";
            
            response.Headers["Content-Security-Policy"] = cspPolicy;
        }
        
        // Permissions-Policy: Control browser features
        if (!response.Headers.ContainsKey("Permissions-Policy"))
        {
            var permissionsPolicy = "camera=(), " +
                                  "microphone=(), " +
                                  "geolocation=(), " +
                                  "payment=(), " +
                                  "usb=(), " +
                                  "magnetometer=(), " +
                                  "gyroscope=(), " +
                                  "accelerometer=()";
            
            response.Headers["Permissions-Policy"] = permissionsPolicy;
        }
        
        // Remove server information headers for security
        response.Headers.Remove("Server");
        response.Headers.Remove("X-Powered-By");
        response.Headers.Remove("X-AspNet-Version");
        response.Headers.Remove("X-AspNetMvc-Version");
    }
}