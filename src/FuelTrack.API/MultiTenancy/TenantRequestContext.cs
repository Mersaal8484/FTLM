using FuelTrack.Application.Interfaces;

namespace FuelTrack.API.MultiTenancy;

public sealed class TenantRequestContext : ITenantRequestContext
{
    public Guid? TenantId { get; private set; }
    public string? DatabaseConnection { get; private set; }

    internal void Initialize(Guid tenantId, string connectionString)
    {
        if (TenantId.HasValue) throw new InvalidOperationException("Tenant context is already initialized.");
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Invalid tenant context.");
        TenantId = tenantId;
        DatabaseConnection = connectionString;
    }
}
