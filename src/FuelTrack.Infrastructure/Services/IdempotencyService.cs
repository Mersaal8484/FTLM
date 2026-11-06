using FuelTrack.Application.Common;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public sealed class IdempotencyService(FuelTrackDbContext db) : IIdempotencyService
{
    public async Task<Result<IdempotencyStartResult>> StartAsync(Guid clientRequestId, string operation, string requestHash, int userId, CancellationToken cancellationToken = default)
    {
        if (clientRequestId == Guid.Empty || string.IsNullOrWhiteSpace(operation) || requestHash.Length != 64)
            return Result<IdempotencyStartResult>.Failure("A request ID, operation, and SHA-256 request hash are required.");

        var existing = await db.IdempotencyRequests.SingleOrDefaultAsync(x => x.ClientRequestId == clientRequestId, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.Operation, operation, StringComparison.Ordinal) ||
                !string.Equals(existing.RequestHash, requestHash, StringComparison.OrdinalIgnoreCase))
                return Result<IdempotencyStartResult>.Conflict("Client request ID was already used for a different command.");

            return Result<IdempotencyStartResult>.Success(new(false, existing.ResponseStatusCode, existing.ResponsePayload));
        }

        db.IdempotencyRequests.Add(new IdempotencyRequest
        {
            ClientRequestId = clientRequestId,
            Operation = operation,
            RequestHash = requestHash,
            Status = "Processing",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedBy = userId,
        });
        await db.SaveChangesAsync(cancellationToken);
        return Result<IdempotencyStartResult>.Success(new(true, null, null));
    }

    public async Task CompleteAsync(Guid clientRequestId, int responseStatusCode, string? responsePayload, int userId, CancellationToken cancellationToken = default)
    {
        var request = await db.IdempotencyRequests.SingleOrDefaultAsync(x => x.ClientRequestId == clientRequestId, cancellationToken)
            ?? throw new InvalidOperationException("Idempotency request was not started.");
        request.Status = "Completed";
        request.ResponseStatusCode = responseStatusCode;
        request.ResponsePayload = responsePayload;
        request.CompletedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken);
    }
}
