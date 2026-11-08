using FuelTrack.Application.Common;

namespace FuelTrack.Application.Interfaces;

public sealed record IdempotencyStartResult(bool IsNew, int? ResponseStatusCode, string? ResponsePayload);

public interface IIdempotencyService
{
    Task<Result<IdempotencyStartResult>> StartAsync(Guid clientRequestId, string operation, string requestHash, int userId, CancellationToken cancellationToken = default);
    Task CompleteAsync(Guid clientRequestId, int responseStatusCode, string? responsePayload, int userId, CancellationToken cancellationToken = default);
}
