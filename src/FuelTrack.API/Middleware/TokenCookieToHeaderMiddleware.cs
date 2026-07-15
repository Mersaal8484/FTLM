namespace FuelTrack.API.Middleware;

public class TokenCookieToHeaderMiddleware
{
    private readonly RequestDelegate _next;

    public TokenCookieToHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            var token = context.Request.Cookies["access_token"];
            if (!string.IsNullOrWhiteSpace(token))
            {
                context.Request.Headers.Authorization = $"Bearer {token}";
            }
        }

        await _next(context);
    }
}
