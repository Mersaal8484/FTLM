using System.Security.Claims;
using FuelTrack.API.Middleware;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FuelTrack.Tests.Middleware;

public sealed class PermissionEnforcementMiddlewareTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly ServiceProvider _serviceProvider;

    public PermissionEnforcementMiddlewareTests()
    {
        var services = new ServiceCollection();
        var dbName = $"PermMiddlewareTests_{Guid.NewGuid()}";
        services.AddDbContext<FuelTrackDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        _serviceProvider = services.BuildServiceProvider();
        _db = _serviceProvider.GetRequiredService<FuelTrackDbContext>();
    }

    [Theory]
    [InlineData("ServiceItems", "GET", "/api/service-items", "maintenance.view")]
    [InlineData("ServiceItems", "POST", "/api/service-items", "maintenance.configure_service")]
    [InlineData("ServiceIntervalDefinitions", "PUT", "/api/service-interval-definitions/1", "maintenance.configure_service")]
    [InlineData("ServiceReplacements", "POST", "/api/service-replacements", "maintenance.record_service")]
    [InlineData("ServiceDue", "POST", "/api/service-due/evaluate", "maintenance.record_service")]
    [InlineData("OperationalCaptures", "POST", "/api/operational-captures", "operational_data.capture")]
    [InlineData("StationOperationalSettings", "POST", "/api/station-operational-settings", "operational_data.configure")]
    public void PermissionMapping_ResolvesExpectedKey(string controller, string method, string path, string expected)
    {
        var resolved = PermissionMapping.ResolvePermissionKey(controller, method, path);
        Assert.Equal(expected, resolved);
    }

    [Fact]
    public async Task NewServiceController_Returns404_WhenOperationalDataDisabled_EvenForSystemOwner()
    {
        // Feature disabled
        _db.CustomerFeatureSettings.Add(new CustomerFeatureSetting
        {
            Key = "operational_data",
            IsEnabled = false,
            Value = "new"
        });
        await _db.SaveChangesAsync();

        var context = CreateHttpContext("ServiceItems", "GET", "/api/service-items", isSystemOwner: true);
        var middleware = new PermissionEnforcementMiddleware(_ => Task.CompletedTask, NullLogger<PermissionEnforcementMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task NewServiceController_Returns404_WhenOperationalDataEnabled_WithLegacyValue()
    {
        // Feature enabled, but Value is "legacy"
        _db.CustomerFeatureSettings.Add(new CustomerFeatureSetting
        {
            Key = "operational_data",
            IsEnabled = true,
            Value = "legacy"
        });
        await _db.SaveChangesAsync();

        var context = CreateHttpContext("ServiceItems", "GET", "/api/service-items", isSystemOwner: true);
        var middleware = new PermissionEnforcementMiddleware(_ => Task.CompletedTask, NullLogger<PermissionEnforcementMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task NewServiceController_AllowsSystemOwner_WhenOperationalDataEnabled_WithNewValue()
    {
        _db.CustomerFeatureSettings.Add(new CustomerFeatureSetting
        {
            Key = "operational_data",
            IsEnabled = true,
            Value = "new"
        });
        await _db.SaveChangesAsync();

        var context = CreateHttpContext("ServiceItems", "GET", "/api/service-items", isSystemOwner: true);
        var nextExecuted = false;
        var middleware = new PermissionEnforcementMiddleware(_ =>
        {
            nextExecuted = true;
            return Task.CompletedTask;
        }, NullLogger<PermissionEnforcementMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.True(nextExecuted);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task ExistingOperationalController_AllowsSystemOwner_EvenInLegacyMode()
    {
        _db.CustomerFeatureSettings.Add(new CustomerFeatureSetting
        {
            Key = "operational_data",
            IsEnabled = true,
            Value = "legacy"
        });
        await _db.SaveChangesAsync();

        // Existing Phase 1-3b operational controller
        var context = CreateHttpContext("OperationalCaptures", "GET", "/api/operational-captures", isSystemOwner: true);
        var nextExecuted = false;
        var middleware = new PermissionEnforcementMiddleware(_ =>
        {
            nextExecuted = true;
            return Task.CompletedTask;
        }, NullLogger<PermissionEnforcementMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.True(nextExecuted);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    private HttpContext CreateHttpContext(string controller, string method, string path, bool isSystemOwner)
    {
        var context = new DefaultHttpContext();
        context.RequestServices = _serviceProvider;
        context.Request.Path = path;
        context.Request.Method = method;

        var routeData = new RouteData();
        routeData.Values["controller"] = controller;
        context.Features.Set<IRoutingFeature>(new RoutingFeatureWrapper(routeData));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Name, "testuser"),
        };
        if (isSystemOwner)
        {
            claims.Add(new Claim("is_system_owner", "true"));
        }

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        return context;
    }

    private sealed class RoutingFeatureWrapper(RouteData routeData) : IRoutingFeature
    {
        public RouteData RouteData { get; set; } = routeData;
    }

    public void Dispose()
    {
        _db.Dispose();
        _serviceProvider.Dispose();
    }
}
