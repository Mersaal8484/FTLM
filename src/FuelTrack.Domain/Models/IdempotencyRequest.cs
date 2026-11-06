namespace FuelTrack.Domain.Models;

/// <summary>
/// Stores the result of a client command so retries cannot create duplicate writes.
/// </summary>
public sealed class IdempotencyRequest : BaseEntity
{
    public Guid ClientRequestId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string Status { get; set; } = "Processing";
    public int? ResponseStatusCode { get; set; }
    public string? ResponsePayload { get; set; }
    public DateTime? CompletedAt { get; set; }
}
