using System.Diagnostics;

namespace GitHubClient.Middleware;

/// <summary>
/// Logs incoming HTTP requests with method, path, status code, and elapsed time.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="RequestLoggingMiddleware"/>.
    /// </summary>
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Logs request details before and after processing.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "********************************************\n" +
            "RequestLoggingMiddleware :: Incoming request\n" +
            "Method: {Method} | Path: {Path}{QueryString}\n" +
            "********************************************",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString);

        await _next(context);

        stopwatch.Stop();

        var statusLabel = context.Response.StatusCode switch
        {
            >= 200 and < 300 => "SUCCESS",
            >= 300 and < 400 => "REDIRECT",
            >= 400 and < 500 => "CLIENT ERROR",
            >= 500 => "SERVER ERROR",
            _ => "UNKNOWN"
        };

        _logger.LogInformation(
            "********************************************\n" +
            "RequestLoggingMiddleware :: Request completed :: {StatusLabel}\n" +
            "Method: {Method} | Path: {Path} | StatusCode: {StatusCode} | Duration: {ElapsedMs}ms\n" +
            "********************************************",
            statusLabel,
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}