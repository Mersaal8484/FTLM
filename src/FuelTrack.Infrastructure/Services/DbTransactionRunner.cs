using System.Data;
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
            return await work(cancellationToken);

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            var result = await work(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}
