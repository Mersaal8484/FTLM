using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FuelTrack.Tests.Services;

/// <summary>Phase 3 — Energy balance math tests.</summary>
public sealed class EnergyBalanceServiceTests
{
    private static FuelTrackDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static async Task<(int StationId, int SubStationId, int GeneratorId, int FeederId, int SubmissionId)>
        SeedBaseAsync(FuelTrackDbContext db, int? internalMeterId = null)
    {
        var station = new Station { Id = 1, BranchId = 1, Name = "S", Code = "S", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var settings = new StationOperationalSettings
        {
            StationId = 1, ProducedVsSentWarningThresholdPercent = 10m,
            InternalConsumptionMeterId = internalMeterId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var site = new SubStation { Id = 10, StationId = 1, Name = "Site", Code = "SITE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var generator = new Generator { Id = 5, SubStationId = 10, Name = "G", SerialNumber = "G", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var feeder = new Feeder { Id = 3, SubStationId = 10, Name = "F", Code = "F", MeterMultiplier = 1m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var submission = new OperationalSubmission
        {
            SubStationId = 10, ClientRequestId = Guid.NewGuid(), ReceivedAtUtc = DateTime.UtcNow,
            Status = OperationalReadingStatus.Approved,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedBy = 1
        };
        db.AddRange(station, settings, site, generator, feeder, submission);
        await db.SaveChangesAsync();
        return (1, 10, generator.Id, feeder.Id, submission.Id);
    }

    private static MeterReading Closing(int subId, MeterReadingSubjectType type, int subjectId, DateOnly date, DateTime capturedAt, decimal value, bool approved = true) =>
        new()
        {
            OperationalSubmissionId = subId, ClientLineId = Guid.NewGuid(),
            SubjectType = type, SubjectId = subjectId,
            Kind = MeterReadingKind.DailyClosing,
            CapturedAtUtc = capturedAt, ReceivedAtUtc = capturedAt,
            BusinessDate = date,
            Status = approved ? OperationalReadingStatus.Approved : OperationalReadingStatus.Pending,
            RawValue = value, MultiplierSnapshot = 1m,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedBy = 1
        };

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStationBalance_CorrectMath_NoInternal()
    {
        // gen=100 kWh produced, feeder=90 kWh sent
        // unexplained = 100 - 90 - 0 = 10; not measured → threshold suppressed
        await using var db = CreateDb();
        var (stationId, _, genId, feederId, subId) = await SeedBaseAsync(db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        db.MeterReadings.AddRange(
            Closing(subId, MeterReadingSubjectType.GeneratorEnergyProduced, genId, yesterday, DateTime.UtcNow.AddDays(-1), 0m),
            Closing(subId, MeterReadingSubjectType.GeneratorEnergyProduced, genId, today, DateTime.UtcNow.AddMinutes(-5), 100m),
            Closing(subId, MeterReadingSubjectType.FeederEnergySent, feederId, yesterday, DateTime.UtcNow.AddDays(-1), 0m),
            Closing(subId, MeterReadingSubjectType.FeederEnergySent, feederId, today, DateTime.UtcNow.AddMinutes(-10), 90m));
        await db.SaveChangesAsync();

        var svc = new EnergyBalanceService(db, new MeterDeltaService(db));
        var result = await svc.GetStationBalanceAsync(stationId, today);

        Assert.Equal(100m, result.ImportKwh);
        Assert.Equal(90m, result.ExportKwh);
        Assert.Equal(10m, result.UnexplainedDiffKwh);
        Assert.False(result.IsInternalMeasured);
        Assert.False(result.ThresholdExceeded);    // threshold suppressed when internal unknown
        Assert.Equal("غير مفسَّر (يشمل الاستهلاك الداخلي)", result.UnexplainedLabel);
    }

    [Fact]
    public async Task GetStationBalance_WithInternal_ShowsMeasuredDiff()
    {
        // gen=100, feeder=90, internal=5 → unexplained=5 (5% of 100)
        // threshold=10% → NOT exceeded
        await using var db = CreateDb();
        var meter = new Meter { Id = 99, SubStationId = 10, Name = "Internal", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        // We need a station with InternalConsumptionMeterId set
        // Re-seed with internal meter id
        var station = new Station { Id = 2, BranchId = 1, Name = "S2", Code = "S2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var settings = new StationOperationalSettings
        {
            StationId = 2, ProducedVsSentWarningThresholdPercent = 10m,
            InternalConsumptionMeterId = 99,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var site = new SubStation { Id = 20, StationId = 2, Name = "Site2", Code = "S2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var generator = new Generator { Id = 6, SubStationId = 20, Name = "G2", SerialNumber = "G2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var feeder = new Feeder { Id = 4, SubStationId = 20, Name = "F2", Code = "F2", MeterMultiplier = 1m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var submission = new OperationalSubmission
        {
            SubStationId = 20, ClientRequestId = Guid.NewGuid(), ReceivedAtUtc = DateTime.UtcNow,
            Status = OperationalReadingStatus.Approved,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedBy = 1
        };

        await using var db2 = CreateDb();
        db2.AddRange(station, settings, site, generator, feeder, meter, submission);
        await db2.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        var subId = submission.Id;

        db2.MeterReadings.AddRange(
            Closing(subId, MeterReadingSubjectType.GeneratorEnergyProduced, 6, yesterday, DateTime.UtcNow.AddDays(-1), 0m),
            Closing(subId, MeterReadingSubjectType.GeneratorEnergyProduced, 6, today, DateTime.UtcNow.AddMinutes(-5), 100m),
            Closing(subId, MeterReadingSubjectType.FeederEnergySent, 4, yesterday, DateTime.UtcNow.AddDays(-1), 0m),
            Closing(subId, MeterReadingSubjectType.FeederEnergySent, 4, today, DateTime.UtcNow.AddMinutes(-10), 90m),
            // Internal: MeterId=99, SubjectType=GeneratorRunningHours used as stand-in internal meter subject
            new MeterReading
            {
                OperationalSubmissionId = subId, ClientLineId = Guid.NewGuid(),
                SubjectType = MeterReadingSubjectType.GeneratorRunningHours, SubjectId = 6,
                MeterId = 99,
                Kind = MeterReadingKind.DailyClosing,
                CapturedAtUtc = DateTime.UtcNow.AddMinutes(-8), ReceivedAtUtc = DateTime.UtcNow,
                BusinessDate = today,
                Status = OperationalReadingStatus.Approved,
                RawValue = 5m, MultiplierSnapshot = 1m,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedBy = 1
            },
            new MeterReading
            {
                OperationalSubmissionId = subId, ClientLineId = Guid.NewGuid(),
                SubjectType = MeterReadingSubjectType.GeneratorRunningHours, SubjectId = 6,
                MeterId = 99,
                Kind = MeterReadingKind.DailyClosing,
                CapturedAtUtc = DateTime.UtcNow.AddDays(-1).AddMinutes(-8), ReceivedAtUtc = DateTime.UtcNow,
                BusinessDate = yesterday,
                Status = OperationalReadingStatus.Approved,
                RawValue = 0m, MultiplierSnapshot = 1m,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedBy = 1
            });
        await db2.SaveChangesAsync();

        var svc = new EnergyBalanceService(db2, new MeterDeltaService(db2));
        var result = await svc.GetStationBalanceAsync(2, today);

        Assert.Equal(100m, result.ImportKwh);
        Assert.Equal(90m, result.ExportKwh);
        Assert.Equal(5m, result.InternalConsumptionKwh);
        Assert.Equal(5m, result.UnexplainedDiffKwh);
        Assert.True(result.IsInternalMeasured);
        Assert.False(result.ThresholdExceeded); // 5% < 10%
    }

    [Fact]
    public async Task GetStationBalance_ThresholdOnlyWhenInternalMeasured()
    {
        // Even if unexplained is 50%, threshold must NOT fire when internal is not measured
        await using var db = CreateDb();
        var (stationId, _, genId, feederId, subId) = await SeedBaseAsync(db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        db.MeterReadings.AddRange(
            Closing(subId, MeterReadingSubjectType.GeneratorEnergyProduced, genId, yesterday, DateTime.UtcNow.AddDays(-1), 0m),
            Closing(subId, MeterReadingSubjectType.GeneratorEnergyProduced, genId, today, DateTime.UtcNow.AddMinutes(-5), 100m),
            Closing(subId, MeterReadingSubjectType.FeederEnergySent, feederId, yesterday, DateTime.UtcNow.AddDays(-1), 0m),
            Closing(subId, MeterReadingSubjectType.FeederEnergySent, feederId, today, DateTime.UtcNow.AddMinutes(-10), 50m));
        await db.SaveChangesAsync();

        var svc = new EnergyBalanceService(db, new MeterDeltaService(db));
        var result = await svc.GetStationBalanceAsync(stationId, today);

        Assert.Equal(50m, result.UnexplainedDiffKwh);
        Assert.False(result.IsInternalMeasured);
        Assert.False(result.ThresholdExceeded); // must NOT fire
    }
}
