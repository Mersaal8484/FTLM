namespace FuelTrack.API.Middleware;

/// <summary>
/// Rate limiting per-IP for unauthenticated endpoints and request body size limits.
/// </summary>
public class ApiHardeningMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiHardeningMiddleware> _logger;

    private const int MaxRequestBodyBytes = 1_048_576; // 1MB
    private const int MaxQueryStringLength = 2048;

    public ApiHardeningMiddleware(RequestDelegate next, ILogger<ApiHardeningMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Block oversized request bodies
        if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > MaxRequestBodyBytes)
        {
            context.Response.StatusCode = 413;
            await context.Response.WriteAsJsonAsync(new { error = "Request body too large", maxSize = MaxRequestBodyBytes });
            return;
        }

        // Block oversized query strings
        if (context.Request.QueryString.Value?.Length > MaxQueryStringLength)
        {
            context.Response.StatusCode = 414;
            await context.Response.WriteAsJsonAsync(new { error = "Query string too long", maxLength = MaxQueryStringLength });
            return;
        }

        // Block path traversal attempts
        var path = context.Request.Path.Value ?? "";
        if (path.Contains("..") || path.Contains("%2e%2e", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("[SECURITY] Path traversal attempt blocked: {Path}", path);
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid path" });
            return;
        }

        // Block suspicious user agents
        var userAgent = context.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            _logger.LogWarning("[SECURITY] Request with empty User-Agent from {IP}", context.Connection.RemoteIpAddress);
        }

        await _next(context);
    }
}
