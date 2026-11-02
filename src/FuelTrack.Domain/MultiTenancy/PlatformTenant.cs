namespace FuelTrack.Domain.MultiTenancy;

public enum PlatformTenantStatus
{
    Provisioning,
    Active,
    Suspended,
    Archived
}

/// <summary>A platform tenant. Its database schema is derived from its immutable ID.</summary>
public sealed class PlatformTenant
{
    private PlatformTenant() { }

    public PlatformTenant(Guid id, string name, string host)
    {
        if (id == Guid.Empty) throw new ArgumentException("A tenant ID is required.", nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Id = id;
        Name = name.Trim();
        Host = TenantIdentity.NormalizeHost(host);
        Status = PlatformTenantStatus.Provisioning;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Host { get; private set; } = "";
    public PlatformTenantStatus Status { get; private set; }
    public string Schema => TenantIdentity.SchemaName(Id);

    public void Activate()
    {
        if (Status is not (PlatformTenantStatus.Provisioning or PlatformTenantStatus.Suspended))
            throw new InvalidOperationException("This tenant cannot be activated from its current status.");
        Status = PlatformTenantStatus.Active;
    }

    public void Suspend()
    {
        if (Status != PlatformTenantStatus.Active)
            throw new InvalidOperationException("Only an active tenant can be suspended.");
        Status = PlatformTenantStatus.Suspended;
    }

    public void Archive()
    {
        if (Status != PlatformTenantStatus.Suspended)
            throw new InvalidOperationException("Only a suspended tenant can be archived.");
        Status = PlatformTenantStatus.Archived;
    }
}
