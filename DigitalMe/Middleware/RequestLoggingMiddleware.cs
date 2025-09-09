using System.Diagnostics;
using System.Text;

namespace DigitalMe.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        // Log request
        _logger.LogInformation("HTTP {Method} {Path} started. TraceId: {TraceId}, UserAgent: {UserAgent}",
            context.Request.Method,
            context.Request.Path.Value,
            traceId,
            context.Request.Headers["User-Agent"].FirstOrDefault());

        // Log request body for POST/PUT requests (for debugging)
        if (context.Request.Method == "POST" || context.Request.Method == "PUT")
        {
            context.Request.EnableBuffering();
            var bodyContent = await ReadRequestBodyAsync(context.Request);
            if (!string.IsNullOrWhiteSpace(bodyContent) && bodyContent.Length < 1000) // Limit size
            {
                _logger.LogDebug("Request body: {RequestBody}", bodyContent);
            }
            context.Request.Body.Position = 0; // Reset position
        }

        var originalResponseBody = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Copy response body back
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBody);

            // Log response
            _logger.LogInformation("HTTP {Method} {Path} completed in {Duration}ms. StatusCode: {StatusCode}, TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path.Value,
                stopwatch.ElapsedMilliseconds,
                context.Response.StatusCode,
                traceId);

            // Log response body for errors (for debugging)
            if (context.Response.StatusCode >= 400 && responseBodyStream.Length < 1000)
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                _logger.LogWarning("Error response body: {ResponseBody}", responseBody);
            }
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
