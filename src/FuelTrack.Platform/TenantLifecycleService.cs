using FuelTrack.Domain.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Platform;

/// <summary>Serializes tenant lifecycle changes and fails closed before customer access is ready.</summary>
public sealed class TenantLifecycleService(PlatformDbContext db)
{
    public async Task SuspendAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var tenant = await LockTenantAsync(id, cancellationToken);
        tenant.Suspend();
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ResumeAsync(Guid id, bool accessReady, CancellationToken cancellationToken = default)
    {
        if (!accessReady) throw new InvalidOperationException("Customer access is not ready.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var tenant = await LockTenantAsync(id, cancellationToken);
        if (tenant.Status != PlatformTenantStatus.Suspended) throw new InvalidOperationException("Only suspended tenants can be resumed.");
        var now = DateTimeOffset.UtcNow;
        if (!await db.TenantCredentials.AnyAsync(c => c.TenantId == id, cancellationToken) ||
            !await db.Subscriptions.AnyAsync(s => s.TenantId == id && s.StartsAt <= now && now < s.EndsAt, cancellationToken))
            throw new InvalidOperationException("An active subscription and provisioned credential are required.");
        tenant.Activate();
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ActivateLocalDemoAsync(Guid id, bool localDemoEnabled, CancellationToken cancellationToken = default)
    {
        if (!localDemoEnabled) throw new InvalidOperationException("Local demo is not enabled.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var tenant = await LockTenantAsync(id, cancellationToken);
        var label = tenant.Host.EndsWith(".localhost", StringComparison.Ordinal) ? tenant.Host[..^".localhost".Length] : "";
        if (tenant.Status != PlatformTenantStatus.Provisioning || label.Length is < 1 or > 63 ||
            label[0] == '-' || label[^1] == '-' || label.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-'))
            throw new InvalidOperationException("Only a provisioned local tenant can be activated.");
        var now = DateTimeOffset.UtcNow;
        if (!await db.TenantCredentials.AnyAsync(c => c.TenantId == id, cancellationToken) ||
            !await db.Subscriptions.AnyAsync(s => s.TenantId == id && s.StartsAt <= now && now < s.EndsAt, cancellationToken))
            throw new InvalidOperationException("An active subscription and provisioned credential are required.");
        tenant.Activate();
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<PlatformTenant> LockTenantAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(id));
        return await db.Tenants.FromSqlInterpolated($"SELECT * FROM platform.\"Tenants\" WHERE \"Id\" = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Tenant not found.");
    }
}
