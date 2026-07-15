using FuelTrack.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;

namespace FuelTrack.Tests.Services;

public class ApiHardeningMiddlewareTests
{
    private static readonly RequestDelegate _nextOk = _ => Task.CompletedTask;

    private static ApiHardeningMiddleware CreateMiddleware()
        => new(_nextOk, NullLogger<ApiHardeningMiddleware>.Instance);

    [Fact]
    public async Task NormalRequest_PassesThrough()
    {
        var sut = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.ContentLength = 100;
        context.Request.QueryString = new QueryString("?page=1");
        context.Request.Headers["User-Agent"] = "TestAgent";

        await sut.InvokeAsync(context);

        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task LargeBody_Returns413()
    {
        var sut = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.ContentLength = 2 * 1024 * 1024;
        context.Request.Headers["User-Agent"] = "TestAgent";

        await sut.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.RequestEntityTooLarge, context.Response.StatusCode);
    }

    [Fact]
    public async Task LongQueryString_Returns414()
    {
        var sut = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?" + new string('x', 3000));
        context.Request.Headers["User-Agent"] = "TestAgent";

        await sut.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.RequestUriTooLong, context.Response.StatusCode);
    }

    [Fact]
    public async Task PathTraversal_Returns400()
    {
        var sut = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/../../etc/passwd";
        context.Request.Headers["User-Agent"] = "TestAgent";

        await sut.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task EmptyUserAgent_PassesThrough()
    {
        var sut = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.ContentLength = 100;
        context.Request.QueryString = new QueryString("?page=1");

        await sut.InvokeAsync(context);

        Assert.Equal(200, context.Response.StatusCode);
    }
}

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task AddsSecurityHeaders()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.True(context.Response.Headers.ContainsKey("X-Content-Type-Options"));
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
        Assert.True(context.Response.Headers.ContainsKey("X-Frame-Options"));
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"].ToString());
    }
}

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task AddsCorrelationId_WhenNotProvided()
    {
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.True(context.Response.Headers.ContainsKey("X-Correlation-Id"));
        Assert.False(string.IsNullOrEmpty(context.Response.Headers["X-Correlation-Id"].ToString()));
    }

    [Fact]
    public async Task UsesProvidedCorrelationId()
    {
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = "test-123";

        await middleware.InvokeAsync(context);

        Assert.Equal("test-123", context.Response.Headers["X-Correlation-Id"].ToString());
    }
}

public class ExceptionHandlingMiddlewareTests
{
    private static ExceptionHandlingMiddleware CreateMiddleware()
        => new(_ => Task.CompletedTask, NullLogger<ExceptionHandlingMiddleware>.Instance);

    [Fact]
    public async Task CatchesInvalidOperationException_Returns400()
    {
        var sut = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("boom"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await sut.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("problem+json", context.Response.ContentType);
        Assert.Contains("boom", body);
    }

    [Fact]
    public async Task CatchesGenericException_Returns500()
    {
        var sut = new ExceptionHandlingMiddleware(
            _ => throw new ArgumentException("bad arg"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await sut.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
    }
}
