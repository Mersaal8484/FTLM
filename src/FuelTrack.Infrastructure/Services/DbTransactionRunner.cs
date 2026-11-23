using System.Data;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

/// <summary>
/// Executes a unit of work inside an EF execution strategy and a database transaction.
/// The in-memory provider deliberately keeps its existing non-transactional test path.
/// </summary>
public static class DbTransactionRunner
{
    public static Task RunAsync(
        DbContext db,
        Func<CancellationToken, Task> work,
        CancellationToken cancellationToken = default) =>
        RunAsync<object?>(
            db,
            async ct =>
            {
                await work(ct);
                return null;
            },
            cancellationToken);

    public static async Task<T> RunAsync<T>(
        DbContext db,
        Func<CancellationToken, Task<T>> work,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(work);

        if (!db.Database.IsRelational())
        {
            var entriesBeforeAttempt = CaptureTrackedEntities(db);
            try
            {
                return await work(cancellationToken);
            }
            catch
            {
                DetachAttemptEntities(db, entriesBeforeAttempt);
                throw;
            }
        }

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // Execution strategies retry the complete delegate. Preserve entities that were
            // already tracked by the caller, but detach every entity materialised or added by
            // a failed attempt so a second attempt starts with a clean unit of work.
            var entriesBeforeAttempt = CaptureTrackedEntities(db);

            try
            {
                await using var transaction = await db.Database.BeginTransactionAsync(
                    IsolationLevel.ReadCommitted, cancellationToken);
                var result = await work(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                DetachAttemptEntities(db, entriesBeforeAttempt);
                throw;
            }
        });
    }

    private static HashSet<object> CaptureTrackedEntities(DbContext db) => db.ChangeTracker.Entries()
        .Select(entry => entry.Entity)
        .ToHashSet(ReferenceEqualityComparer.Instance);

    private static void DetachAttemptEntities(DbContext db, HashSet<object> entriesBeforeAttempt)
    {
        foreach (var entry in db.ChangeTracker.Entries().ToList())
        {
            if (!entriesBeforeAttempt.Contains(entry.Entity))
                entry.State = EntityState.Detached;
        }
    }
}
