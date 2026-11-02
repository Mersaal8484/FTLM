using System.Security.Cryptography;

namespace FuelTrack.API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items["csp-nonce"] = nonce;

        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["X-XSS-Protection"] = "1; mode=block";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        var csp = $"default-src 'self'; " +
                  $"script-src 'self' 'nonce-{nonce}'; " +
                  $"style-src 'self' 'nonce-{nonce}'; " +
                  $"font-src 'self' https: data:; " +
                  $"img-src 'self' data: https:; " +
                  $"connect-src 'self' wss: ws:; " +
                  $"frame-ancestors 'none'; " +
                  $"base-uri 'self'; " +
                  $"form-action 'self'";

        headers["Content-Security-Policy"] = csp;

        if (context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        }

        await _next(context);
    }
}
