using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Tests.Services;

public sealed class DbTransactionRunnerTests
{
    [Fact]
    public async Task RunAsync_InMemoryProvider_ExecutesWorkAndReturnsItsResult()
    {
        await using var db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        var calls = 0;

        var result = await DbTransactionRunner.RunAsync(db, _ => Task.FromResult(++calls));

        Assert.Equal(1, result);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task FailedAttempt_DetachesAttemptEntities_SoRetryPersistsExactlyOneRecordAndOilTransaction()
    {
        await using var db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

        var attempts = 0;
        await Assert.ThrowsAsync<TimeoutException>(() => DbTransactionRunner.RunAsync(db, async _ =>
        {
            db.ServiceReplacementRecords.Add(new ServiceReplacementRecord { GeneratorId = 1, ServiceItemId = 1, HoursAtService = 10m });
            db.OilTransactions.Add(new OilTransaction { SubStationId = 1, OilLookupId = 1, TransactionTypeLookupId = 1, QuantityLiters = 1d });
            attempts++;
            throw new TimeoutException("simulated transient failure");
        }));

        await DbTransactionRunner.RunAsync(db, async _ =>
        {
            db.ServiceReplacementRecords.Add(new ServiceReplacementRecord { GeneratorId = 1, ServiceItemId = 1, HoursAtService = 10m });
            db.OilTransactions.Add(new OilTransaction { SubStationId = 1, OilLookupId = 1, TransactionTypeLookupId = 1, QuantityLiters = 1d });
            await db.SaveChangesAsync();
        });

        Assert.Equal(1, attempts);
        Assert.Single(db.ServiceReplacementRecords);
        Assert.Single(db.OilTransactions);
    }
}
