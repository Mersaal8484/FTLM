using System.Diagnostics;

namespace FuelTrack.API.Middleware;

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
        var sw = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;
        var query = context.Request.QueryString.ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        try
        {
            await _next(context);
            sw.Stop();

            var statusCode = context.Response.StatusCode;
            var level = statusCode >= 500 ? LogLevel.Error
                      : statusCode >= 400 ? LogLevel.Warning
                      : LogLevel.Information;

            _logger.Log(level,
                "[HTTP] {Method} {Path}{Query} responded {StatusCode} in {ElapsedMs}ms | IP={IP} UA={UserAgent}",
                method, path, query, statusCode, sw.ElapsedMilliseconds, ip, userAgent);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[HTTP] {Method} {Path}{Query} failed after {ElapsedMs}ms | IP={IP}",
                method, path, query, sw.ElapsedMilliseconds, ip);
            throw;
        }
    }
}
