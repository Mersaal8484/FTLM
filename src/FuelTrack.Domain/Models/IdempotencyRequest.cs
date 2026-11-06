namespace FuelTrack.Domain.Models;

/// <summary>
/// Stores the result of a client command so retries cannot create duplicate writes.
/// </summary>
public sealed class IdempotencyRequest : BaseEntity
{
    /// <summary>
    /// Trusted tenant scope. Guid.Empty represents the single-tenant deployment.
    /// This is never taken from a client request.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The authenticated caller that owns this request key. Kept explicitly rather
    /// than inferred from audit metadata so the uniqueness boundary is unambiguous.
    /// </summary>
    public int UserId { get; set; }

    public Guid ClientRequestId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string Status { get; set; } = "Processing";
    public int? ResponseStatusCode { get; set; }
    public string? ResponsePayload { get; set; }
    public DateTime? CompletedAt { get; set; }
}
