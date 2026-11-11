using System.Security.Claims;
using FuelTrack.API.Middleware;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
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
        services.AddScoped<ICustomerFeatureService, CustomerFeatureService>();

        _serviceProvider = services.BuildServiceProvider();
        _db = _serviceProvider.GetRequiredService<FuelTrackDbContext>();

        SeedPermissions();
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

    [Theory]
    // 1. Existing operational_data controller (retains exact legacy behavior: system owner always bypasses, normal user needs feature enabled)
    [InlineData("OperationalCaptures", "POST", "/api/operational-captures", "disabled", false, 404)]
    [InlineData("OperationalCaptures", "POST", "/api/operational-captures", "disabled", true, 200)]
    [InlineData("OperationalCaptures", "POST", "/api/operational-captures", "enabled_legacy", false, 200)]
    [InlineData("OperationalCaptures", "POST", "/api/operational-captures", "enabled_legacy", true, 200)]
    [InlineData("OperationalCaptures", "POST", "/api/operational-captures", "enabled_new", false, 200)]
    [InlineData("OperationalCaptures", "POST", "/api/operational-captures", "enabled_new", true, 200)]

    // 2. ServiceItems (new controller: requires operational_data enabled and value != "legacy"; system owner does not bypass feature gate)
    [InlineData("ServiceItems", "GET", "/api/service-items", "disabled", false, 404)]
    [InlineData("ServiceItems", "GET", "/api/service-items", "disabled", true, 404)]
    [InlineData("ServiceItems", "GET", "/api/service-items", "enabled_legacy", false, 404)]
    [InlineData("ServiceItems", "GET", "/api/service-items", "enabled_legacy", true, 404)]
    [InlineData("ServiceItems", "GET", "/api/service-items", "enabled_new", false, 200)]
    [InlineData("ServiceItems", "GET", "/api/service-items", "enabled_new", true, 200)]

    // 3. ServiceIntervalDefinitions (new controller: requires operational_data enabled and value != "legacy")
    [InlineData("ServiceIntervalDefinitions", "GET", "/api/service-interval-definitions", "disabled", false, 404)]
    [InlineData("ServiceIntervalDefinitions", "GET", "/api/service-interval-definitions", "disabled", true, 404)]
    [InlineData("ServiceIntervalDefinitions", "GET", "/api/service-interval-definitions", "enabled_legacy", false, 404)]
    [InlineData("ServiceIntervalDefinitions", "GET", "/api/service-interval-definitions", "enabled_legacy", true, 404)]
    [InlineData("ServiceIntervalDefinitions", "GET", "/api/service-interval-definitions", "enabled_new", false, 200)]
    [InlineData("ServiceIntervalDefinitions", "GET", "/api/service-interval-definitions", "enabled_new", true, 200)]

    // 4. ServiceReplacements (new controller: requires operational_data enabled and value != "legacy")
    [InlineData("ServiceReplacements", "POST", "/api/service-replacements", "disabled", false, 404)]
    [InlineData("ServiceReplacements", "POST", "/api/service-replacements", "disabled", true, 404)]
    [InlineData("ServiceReplacements", "POST", "/api/service-replacements", "enabled_legacy", false, 404)]
    [InlineData("ServiceReplacements", "POST", "/api/service-replacements", "enabled_legacy", true, 404)]
    [InlineData("ServiceReplacements", "POST", "/api/service-replacements", "enabled_new", false, 200)]
    [InlineData("ServiceReplacements", "POST", "/api/service-replacements", "enabled_new", true, 200)]

    // 5. ServiceDue (new controller: requires operational_data enabled and value != "legacy")
    [InlineData("ServiceDue", "POST", "/api/service-due/evaluate", "disabled", false, 404)]
    [InlineData("ServiceDue", "POST", "/api/service-due/evaluate", "disabled", true, 404)]
    [InlineData("ServiceDue", "POST", "/api/service-due/evaluate", "enabled_legacy", false, 404)]
    [InlineData("ServiceDue", "POST", "/api/service-due/evaluate", "enabled_legacy", true, 404)]
    [InlineData("ServiceDue", "POST", "/api/service-due/evaluate", "enabled_new", false, 200)]
    [InlineData("ServiceDue", "POST", "/api/service-due/evaluate", "enabled_new", true, 200)]
    public async Task Matrix_ControllerFeatureOwnerMatrix_EnforcesExpectedBehavior(
        string controller, string method, string path, string featureMode, bool isSystemOwner, int expectedStatus)
    {
        SetFeatureState(featureMode);

        var context = CreateHttpContext(controller, method, path, isSystemOwner);
        var nextExecuted = false;
        var middleware = new PermissionEnforcementMiddleware(_ =>
        {
            nextExecuted = true;
            return Task.CompletedTask;
        }, NullLogger<PermissionEnforcementMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        if (expectedStatus == StatusCodes.Status200OK)
        {
            Assert.True(nextExecuted);
        }
        else
        {
            Assert.False(nextExecuted);
        }
    }

    private void SetFeatureState(string mode)
    {
        var setting = _db.CustomerFeatureSettings.FirstOrDefault(s => s.Key == "operational_data");
        if (setting == null)
        {
            setting = new CustomerFeatureSetting { Key = "operational_data" };
            _db.CustomerFeatureSettings.Add(setting);
        }

        switch (mode)
        {
            case "disabled":
                setting.IsEnabled = false;
                setting.Value = CustomerFeatureService.LegacyModeValue;
                break;
            case "enabled_legacy":
                setting.IsEnabled = true;
                setting.Value = CustomerFeatureService.LegacyModeValue;
                break;
            case "enabled_new":
                setting.IsEnabled = true;
                setting.Value = "new";
                break;
        }

        _db.SaveChanges();
    }

    private void SeedPermissions()
    {
        var opModule = new ModuleDefinition { Id = 1, Key = "operational_data", NameAr = "العمليات", NameEn = "Operations", IsActive = true };
        var maintModule = new ModuleDefinition { Id = 2, Key = "maintenance", NameAr = "الصيانة", NameEn = "Maintenance", IsActive = true };
        _db.ModuleDefinitions.AddRange(opModule, maintModule);

        var pCapture = new PagePermission { Id = 1, ModuleDefinitionId = 1, ModuleDefinition = opModule, ActionKey = "capture", NameAr = "التقاط", NameEn = "Capture", IsActive = true };
        var pView = new PagePermission { Id = 2, ModuleDefinitionId = 2, ModuleDefinition = maintModule, ActionKey = "view", NameAr = "عرض", NameEn = "View", IsActive = true };
        var pRecord = new PagePermission { Id = 3, ModuleDefinitionId = 2, ModuleDefinition = maintModule, ActionKey = "record_service", NameAr = "تسجيل", NameEn = "Record", IsActive = true };
        _db.PagePermissions.AddRange(pCapture, pView, pRecord);

        var role = new Role { Id = 1, Name = "Operator", NormalizedName = "OPERATOR" };
        _db.Roles.Add(role);

        _db.RolePermissions.AddRange(
            new RolePermission { RoleId = 1, Role = role, PagePermissionId = 1, PagePermission = pCapture, IsGranted = true },
            new RolePermission { RoleId = 1, Role = role, PagePermissionId = 2, PagePermission = pView, IsGranted = true },
            new RolePermission { RoleId = 1, Role = role, PagePermissionId = 3, PagePermission = pRecord, IsGranted = true }
        );

        _db.SaveChanges();
    }

    private HttpContext CreateHttpContext(string controller, string method, string path, bool isSystemOwner)
    {
        var context = new DefaultHttpContext();
        context.RequestServices = _serviceProvider;
        context.Request.Path = path;
        context.Request.Method = method;
        // PermissionEnforcementMiddleware reads Request.RouteValues directly.
        // Assign the same route-value collection that the request exposes so
        // this test exercises the real controller/feature-gate path.
        context.Request.RouteValues["controller"] = controller;

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
            claims.Add(new Claim(ClaimTypes.Role, "SystemOwner"));
            claims.Add(new Claim("role", "SystemOwner"));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.Role, "Operator"));
            claims.Add(new Claim("role", "Operator"));
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
