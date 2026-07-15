namespace FuelTrack.API.Middleware;

public class InputSanitizationMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly string[] DangerousPatterns =
    [
        "<script",
        "</script",
        "javascript:",
        "vbscript:",
        "data:text/html",
        "onerror=",
        "onload=",
        "onclick=",
        "onmouse",
        "onfocus=",
        "onblur=",
        "onsubmit=",
        "onchange=",
        "onkeydown=",
        "onkeyup=",
        "oninput=",
        "eval(",
        "expression(",
        "document.cookie",
        "document.domain",
        "innerHTML",
        "outerHTML",
        "<iframe",
        "<object",
        "<embed",
        "<svg onload",
        "fromCharCode",
        "String.fromCharCode",
        "base64,"
    ];

    public InputSanitizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        if (method is "POST" or "PUT" or "PATCH")
        {
            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            if (ContainsDangerousPatterns(body))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Potentially malicious input detected"
                });
                return;
            }

            context.Request.Body.Position = 0;
        }

        await _next(context);
    }

    private static bool ContainsDangerousPatterns(string body)
    {
        if (string.IsNullOrEmpty(body))
            return false;

        var lowerBody = body.ToLowerInvariant();
        return DangerousPatterns.Any(pattern => lowerBody.Contains(pattern));
    }
}
