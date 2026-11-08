namespace FuelTrack.Domain.MultiTenancy;

public sealed class PlatformSubscription
{
    private PlatformSubscription() { }
    public PlatformSubscription(Guid id, Guid tenantId, Guid planId, DateTimeOffset start, DateTimeOffset end)
    {
        if (id == Guid.Empty || tenantId == Guid.Empty || planId == Guid.Empty)
            throw new ArgumentException("Subscription, tenant and plan IDs are required.");
        if (end <= start) throw new ArgumentException("Subscription end must be after its start.");
        Id = id;
        TenantId = tenantId;
        PlanId = planId;
        StartsAt = start.ToUniversalTime();
        EndsAt = end.ToUniversalTime();
    }
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid PlanId { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
}
