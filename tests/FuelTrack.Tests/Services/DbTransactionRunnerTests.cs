using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
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
}
