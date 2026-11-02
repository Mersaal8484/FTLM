using FuelTrack.Platform;
using Microsoft.AspNetCore.Authentication;
using Npgsql;

namespace FuelTrack.API.MultiTenancy;

/// <summary>Resolves the registered host, checks token binding and selects a restricted tenant connection.</summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantAccessResolver resolver, TenantCredentialReader credentials,
        TenantRequestContext tenant, IConfiguration configuration)
    {
        var access = await resolver.ResolveAsync(context.Request.Host.Host, DateTimeOffset.UtcNow, context.RequestAborted);
        if (access is null) { context.Response.StatusCode = StatusCodes.Status404NotFound; return; }

        // SignalR uses a different JWT scheme and query-string bearer token.
        if (context.Request.Path.StartsWithSegments("/hubs"))
        {
            var signalR = await context.AuthenticateAsync("SignalR");
            if (!signalR.Succeeded || signalR.Principal is null) { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return; }
            context.User = signalR.Principal;
        }
        var tenantClaims = context.User.FindAll("tenant_id").ToArray();
        if (context.User.Identity?.IsAuthenticated == true &&
            (tenantClaims.Length != 1 || !Guid.TryParse(tenantClaims[0].Value, out var tokenTenantId) || tokenTenantId != access.TenantId))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var template = configuration.GetConnectionString("TenantDatabase")
            ?? throw new InvalidOperationException("Tenant database connection template is missing.");
        var password = await credentials.GetPasswordAsync(access.TenantId, context.RequestAborted);
        var builder = new NpgsqlConnectionStringBuilder(template)
        {
            Username = access.Schema,
            Password = password,
            SearchPath = access.Schema
        };
        tenant.Initialize(access.TenantId, builder.ConnectionString);
        await next(context);
    }
}
