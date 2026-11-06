using FuelTrack.Application.Common;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public sealed class IdempotencyService(FuelTrackDbContext db, ITenantRequestContext? tenant = null) : IIdempotencyService
{
    public async Task<Result<IdempotencyStartResult>> StartAsync(Guid clientRequestId, string operation, string requestHash, int userId, CancellationToken cancellationToken = default)
    {
        if (clientRequestId == Guid.Empty || userId <= 0 || string.IsNullOrWhiteSpace(operation) || requestHash.Length != 64)
            return Result<IdempotencyStartResult>.Failure("A request ID, operation, and SHA-256 request hash are required.");

        var tenantId = TenantScopeId();
        var existing = await FindAsync(tenantId, userId, clientRequestId, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.Operation, operation, StringComparison.Ordinal) ||
                !string.Equals(existing.RequestHash, requestHash, StringComparison.OrdinalIgnoreCase))
                return Result<IdempotencyStartResult>.Conflict("Client request ID was already used for a different command.");

            return Result<IdempotencyStartResult>.Success(new(false, existing.ResponseStatusCode, existing.ResponsePayload));
        }

        db.IdempotencyRequests.Add(new IdempotencyRequest
        {
            TenantId = tenantId,
            UserId = userId,
            ClientRequestId = clientRequestId,
            Operation = operation,
            RequestHash = requestHash,
            Status = "Processing",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedBy = userId,
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The filtered unique index is the concurrency authority. A concurrent
            // retry may have stored this exact key after the initial lookup.
            db.ChangeTracker.Clear();
            existing = await FindAsync(tenantId, userId, clientRequestId, cancellationToken);
            if (existing is null) throw;
            if (!string.Equals(existing.Operation, operation, StringComparison.Ordinal) ||
                !string.Equals(existing.RequestHash, requestHash, StringComparison.OrdinalIgnoreCase))
                return Result<IdempotencyStartResult>.Conflict("Client request ID was already used for a different command.");
            return Result<IdempotencyStartResult>.Success(new(false, existing.ResponseStatusCode, existing.ResponsePayload));
        }
        return Result<IdempotencyStartResult>.Success(new(true, null, null));
    }

    public async Task CompleteAsync(Guid clientRequestId, int responseStatusCode, string? responsePayload, int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
        var request = await FindAsync(TenantScopeId(), userId, clientRequestId, cancellationToken)
            ?? throw new InvalidOperationException("Idempotency request was not started.");
        request.Status = "Completed";
        request.ResponseStatusCode = responseStatusCode;
        request.ResponsePayload = responsePayload;
        request.CompletedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        request.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken);
    }

    private Guid TenantScopeId() => tenant?.TenantId ?? Guid.Empty;

    private Task<IdempotencyRequest?> FindAsync(Guid tenantId, int userId, Guid clientRequestId, CancellationToken cancellationToken) =>
        db.IdempotencyRequests.SingleOrDefaultAsync(
            x => x.TenantId == tenantId && x.UserId == userId && x.ClientRequestId == clientRequestId,
            cancellationToken);
}
