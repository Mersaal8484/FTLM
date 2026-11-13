using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FuelTrack.Tests.Services;

/// <summary>Phase 3 — Meter delta computation and monotonicity enforcement.</summary>
public sealed class MeterDeltaServiceTests
{
    private static FuelTrackDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static async Task<(int SubStationId, int GeneratorId, int SubmissionId)> SeedBaseAsync(FuelTrackDbContext db)
    {
        var station = new Station { Id = 1, BranchId = 1, Name = "S", Code = "S", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var site = new SubStation { Id = 10, StationId = 1, Name = "Site", Code = "SITE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var generator = new Generator { Id = 5, SubStationId = 10, Name = "G", SerialNumber = "G", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var submission = new OperationalSubmission
        {
            SubStationId = 10, ClientRequestId = Guid.NewGuid(), ReceivedAtUtc = DateTime.UtcNow,
            Status = OperationalReadingStatus.Approved,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedBy = 1
        };
        db.AddRange(station, site, generator, submission);
        await db.SaveChangesAsync();
        return (10, generator.Id, submission.Id);
    }

    private static MeterReading BuildClosing(int submissionId, int generatorId, DateOnly date, DateTime capturedAt, decimal value, bool isApproved = true, bool reset = false) =>
        new()
        {
            OperationalSubmissionId = submissionId,
            ClientLineId = Guid.NewGuid(),
            SubjectType = MeterReadingSubjectType.GeneratorEnergyProduced,
            SubjectId = generatorId,
            Kind = MeterReadingKind.DailyClosing,
            CapturedAtUtc = capturedAt,
            ReceivedAtUtc = capturedAt,
            BusinessDate = date,
            Status = isApproved ? OperationalReadingStatus.Approved : OperationalReadingStatus.Pending,
            RawValue = value,
            MultiplierSnapshot = 1m,
            IsMeterReset = reset,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedBy = 1
        };

    // ── Delta tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDailyDelta_ReturnsCorrectDelta()
    {
        await using var db = CreateDb();
        var (_, genId, subId) = await SeedBaseAsync(db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        db.MeterReadings.AddRange(
            BuildClosing(subId, genId, yesterday, DateTime.UtcNow.AddDays(-1), 1000m),
            BuildClosing(subId, genId, today,     DateTime.UtcNow.AddMinutes(-5), 1250m));
        await db.SaveChangesAsync();

        var svc = new MeterDeltaService(db);
        var result = await svc.GetDailyDeltaAsync(MeterReadingSubjectType.GeneratorEnergyProduced, genId, today);

        Assert.NotNull(result);
        Assert.Equal(250m, result!.Delta);       // (1250-1000)*1
        Assert.Equal(1000m, result.PreviousValue);
        Assert.Equal(1250m, result.CurrentValue);
        Assert.Null(result.SpanDays);            // consecutive days
        Assert.False(result.IsMeterReset);
    }

    [Fact]
    public async Task GetDailyDelta_MultiDaySpan_SetsSpanDays()
    {
        await using var db = CreateDb();
        var (_, genId, subId) = await SeedBaseAsync(db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var threeDaysAgo = today.AddDays(-3);

        db.MeterReadings.AddRange(
            BuildClosing(subId, genId, threeDaysAgo, DateTime.UtcNow.AddDays(-3), 500m),
            BuildClosing(subId, genId, today,        DateTime.UtcNow.AddMinutes(-5), 800m));
        await db.SaveChangesAsync();

        var svc = new MeterDeltaService(db);
        var result = await svc.GetDailyDeltaAsync(MeterReadingSubjectType.GeneratorEnergyProduced, genId, today);

        Assert.NotNull(result);
        Assert.Equal(300m, result!.Delta);
        Assert.Equal(3, result.SpanDays);
    }

    [Fact]
    public async Task GetDailyDelta_MeterReset_NullDelta()
    {
        await using var db = CreateDb();
        var (_, genId, subId) = await SeedBaseAsync(db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        db.MeterReadings.AddRange(
            BuildClosing(subId, genId, yesterday, DateTime.UtcNow.AddDays(-1), 9000m),
            BuildClosing(subId, genId, today, DateTime.UtcNow.AddMinutes(-5), 100m, reset: true));
        await db.SaveChangesAsync();

        var svc = new MeterDeltaService(db);
        var result = await svc.GetDailyDeltaAsync(MeterReadingSubjectType.GeneratorEnergyProduced, genId, today);

        Assert.NotNull(result);
        Assert.Null(result!.Delta);
        Assert.True(result.IsMeterReset);
    }

    [Fact]
    public async Task GetIntervalDelta_UsesPreviousUsableReadingAndCurrentMultiplier()
    {
        await using var db = CreateDb();
        var (_, genId, subId) = await SeedBaseAsync(db);
        var first = BuildClosing(subId, genId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), DateTime.UtcNow.AddHours(-2), 100m);
        first.Kind = MeterReadingKind.Periodic;
        var current = BuildClosing(subId, genId, DateOnly.FromDateTime(DateTime.UtcNow), DateTime.UtcNow.AddHours(-1), 130m);
        current.Kind = MeterReadingKind.Periodic;
        current.MultiplierSnapshot = 2m;
        db.MeterReadings.AddRange(first, current);
        await db.SaveChangesAsync();

        var result = await new MeterDeltaService(db).GetIntervalDeltaAsync(current.Id);

        Assert.NotNull(result);
        Assert.Equal(60m, result!.Delta);
        Assert.Equal(100m, result.PreviousValue);
        Assert.Equal(130m, result.CurrentValue);
    }

    [Fact]
    public async Task GetIntervalDelta_ReturnsNoDeltaForResetReading()
    {
        await using var db = CreateDb();
        var (_, genId, subId) = await SeedBaseAsync(db);
        var reset = BuildClosing(subId, genId, DateOnly.FromDateTime(DateTime.UtcNow), DateTime.UtcNow, 10m, reset: true);
        reset.Kind = MeterReadingKind.Periodic;
        db.MeterReadings.Add(reset);
        await db.SaveChangesAsync();

        var result = await new MeterDeltaService(db).GetIntervalDeltaAsync(reset.Id);

        Assert.NotNull(result);
        Assert.Null(result!.Delta);
        Assert.True(result.IsMeterReset);
    }

    // ── Monotonicity tests ────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateMonotonicity_RejectsOutOfOrder()
    {
        await using var db = CreateDb();
        var (_, genId, subId) = await SeedBaseAsync(db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        // Previous approved at 1000; new Pending at 900 (less than prev)
        var prev = BuildClosing(subId, genId, yesterday, DateTime.UtcNow.AddDays(-1), 1000m);
        var curr = BuildClosing(subId, genId, today, DateTime.UtcNow.AddMinutes(-5), 900m, isApproved: false);
        db.MeterReadings.AddRange(prev, curr);
        await db.SaveChangesAsync();

        var svc = new MeterDeltaService(db);
        var result = await svc.ValidateMonotonicityAsync(curr.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(422, result.StatusCode);
    }

    [Fact]
    public async Task ValidateMonotonicity_AllowsReset()
    {
        await using var db = CreateDb();
        var (_, genId, subId) = await SeedBaseAsync(db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        var prev = BuildClosing(subId, genId, yesterday, DateTime.UtcNow.AddDays(-1), 1000m);
        var curr = BuildClosing(subId, genId, today, DateTime.UtcNow.AddMinutes(-5), 50m, isApproved: false, reset: true);
        db.MeterReadings.AddRange(prev, curr);
        await db.SaveChangesAsync();

        var svc = new MeterDeltaService(db);
        var result = await svc.ValidateMonotonicityAsync(curr.Id);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidateMonotonicity_AllowsIncreasingOrder()
    {
        await using var db = CreateDb();
        var (_, genId, subId) = await SeedBaseAsync(db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        var prev = BuildClosing(subId, genId, yesterday, DateTime.UtcNow.AddDays(-1), 1000m);
        var curr = BuildClosing(subId, genId, today, DateTime.UtcNow.AddMinutes(-5), 1200m, isApproved: false);
        db.MeterReadings.AddRange(prev, curr);
        await db.SaveChangesAsync();

        var svc = new MeterDeltaService(db);
        var result = await svc.ValidateMonotonicityAsync(curr.Id);

        Assert.True(result.IsSuccess);
    }
}
