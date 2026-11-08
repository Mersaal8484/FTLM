using FuelTrack.Domain.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Platform;

/// <summary>
/// Looks up a trusted request host in the platform registry. This does not authenticate
/// a user or authorize access: the eventual API must also bind the resulting tenant
/// to the authenticated identity and choose that tenant's restricted DB credential.
/// </summary>
public sealed class TenantAccessResolver(PlatformDbContext db)
{
    public async Task<TenantAccess?> ResolveAuthenticatedAsync(string host, string? tenantClaim, DateTimeOffset utcNow, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(tenantClaim, out var authenticatedTenantId) || authenticatedTenantId == Guid.Empty)
            return null;
        var resolved = await ResolveAsync(host, utcNow, cancellationToken);
        return resolved?.TenantId == authenticatedTenantId ? resolved : null;
    }

    public async Task<TenantAccess?> ResolveAsync(string host, DateTimeOffset utcNow, CancellationToken cancellationToken = default)
    {
        string normalized;
        try { normalized = TenantIdentity.NormalizeHost(host); }
        catch (ArgumentException) { return null; }
        var now = utcNow.ToUniversalTime();
        var tenant = await db.Tenants.AsNoTracking()
            .Where(t => t.Host == normalized && t.Status == PlatformTenantStatus.Active)
            .Select(t => new { t.Id, t.Host })
            .SingleOrDefaultAsync(cancellationToken);
        if (tenant is null) return null;
        var hasSubscription = await db.Subscriptions.AsNoTracking()
            .AnyAsync(s => s.TenantId == tenant.Id && s.StartsAt <= now && now < s.EndsAt, cancellationToken);
        return hasSubscription ? new TenantAccess(tenant.Id, tenant.Host, TenantIdentity.SchemaName(tenant.Id)) : null;
    }
}

public sealed record TenantAccess(Guid TenantId, string Host, string Schema);
