namespace FuelTrack.Application.Interfaces;

/// <summary>Trusted per-request tenant state; never populated directly from client-provided IDs.</summary>
public interface ITenantRequestContext
{
    Guid? TenantId { get; }
    string? DatabaseConnection { get; }
}
