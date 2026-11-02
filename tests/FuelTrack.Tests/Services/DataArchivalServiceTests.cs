using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FuelTrack.Tests.Services;

public class DataArchivalServiceTests
{
    private FuelTrackDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FuelTrackDbContext(options);
    }

    [Fact]
    public async Task ArchiveAuditLogsAsync_Archives_Old_Records_And_Removes_From_Active()
    {
        using var db = CreateInMemoryDbContext();
        var service = new DataArchivalService(db, NullLogger<DataArchivalService>.Instance);

        // Add 2 old audit logs (> 90 days) and 1 recent audit log
        var oldDate = DateTime.UtcNow.AddDays(-120);
        var recentDate = DateTime.UtcNow.AddDays(-10);

        db.AuditLogs.AddRange(
            new AuditLog { UserId = 1, Action = "Create", EntityName = "Generator", EntityId = 1, Timestamp = oldDate },
            new AuditLog { UserId = 2, Action = "Update", EntityName = "FuelTank", EntityId = 2, Timestamp = oldDate },
            new AuditLog { UserId = 3, Action = "Delete", EntityName = "Reading", EntityId = 3, Timestamp = recentDate }
        );
        await db.SaveChangesAsync();

        var count = await service.ArchiveAuditLogsAsync(90);

        Assert.Equal(2, count);
        Assert.Equal(1, await db.AuditLogs.CountAsync());
        Assert.Equal(2, await db.AuditLogArchives.CountAsync());
    }

    [Fact]
    public async Task ArchiveDailyReadingsAsync_Archives_Old_Approved_Readings()
    {
        using var db = CreateInMemoryDbContext();
        var service = new DataArchivalService(db, NullLogger<DataArchivalService>.Instance);

        var oldDate = DateTime.UtcNow.AddDays(-400);
        var recentDate = DateTime.UtcNow.AddDays(-30);

        db.DailyReadings.AddRange(
            new DailyReading { GeneratorId = 1, ReadingDate = oldDate, ReadingStatusLookupId = 2, CurrentEnergyKWh = 100, FuelLookupId = 1, OilLookupId = 1 },
            new DailyReading { GeneratorId = 1, ReadingDate = oldDate, ReadingStatusLookupId = 2, CurrentEnergyKWh = 200, FuelLookupId = 1, OilLookupId = 1 },
            new DailyReading { GeneratorId = 1, ReadingDate = recentDate, ReadingStatusLookupId = 2, CurrentEnergyKWh = 300, FuelLookupId = 1, OilLookupId = 1 }
        );
        await db.SaveChangesAsync();

        var count = await service.ArchiveDailyReadingsAsync(365);

        Assert.Equal(2, count);
        Assert.Equal(1, await db.DailyReadings.CountAsync());
        Assert.Equal(2, await db.DailyReadingArchives.CountAsync());
    }

    [Fact]
    public async Task ArchiveAuditLogsAsync_Returns_Zero_When_No_Matching_Records()
    {
        using var db = CreateInMemoryDbContext();
        var service = new DataArchivalService(db, NullLogger<DataArchivalService>.Instance);

        var recentDate = DateTime.UtcNow.AddDays(-5);
        db.AuditLogs.Add(new AuditLog { UserId = 1, Action = "Login", EntityName = "User", EntityId = 1, Timestamp = recentDate });
        await db.SaveChangesAsync();

        var count = await service.ArchiveAuditLogsAsync(90);

        Assert.Equal(0, count);
        Assert.Equal(1, await db.AuditLogs.CountAsync());
        Assert.Equal(0, await db.AuditLogArchives.CountAsync());
    }
}
