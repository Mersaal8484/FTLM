using FuelTrack.Application.DTOs.Meters;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace FuelTrack.Tests.Services;

/// <summary>
/// Phase C4.1: Solar Metering and Energy Balance integration tests.
/// </summary>
public sealed class CommercialSolarMeteringTests
{
    private static FuelTrackDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static MeterReading Closing(int subId, MeterReadingSubjectType type, int subjectId, DateOnly date, DateTime capturedAt, decimal value, bool approved = true) =>
        new()
        {
            OperationalSubmissionId = subId,
            ClientLineId = Guid.NewGuid(),
            SubjectType = type,
            SubjectId = subjectId,
            Kind = MeterReadingKind.DailyClosing,
            CapturedAtUtc = capturedAt,
            ReceivedAtUtc = capturedAt,
            BusinessDate = date,
            Status = approved ? OperationalReadingStatus.Approved : OperationalReadingStatus.Pending,
            RawValue = value,
            MultiplierSnapshot = 1m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = 1,
            UpdatedBy = 1
        };

    [Fact]
    public void MeterReadingSubjectType_SolarEnergyProduced_HasValueThree()
    {
        // Owner decision & Finding:
        // Replaces string-reflection "SolarImport" with strongly typed MeterReadingSubjectType.SolarEnergyProduced = 3
        Assert.Equal(3, (int)MeterReadingSubjectType.SolarEnergyProduced);
    }

    [Fact]
    public async Task GetStationBalance_WithActiveSolarMeter_CombinesProduction_AndCalculatesSolarShare()
    {
        // Given a station with generator + solar production meter:
        // Generator produced = 10,000 kWh
        // Solar produced = 4,000 kWh
        // Total produced (import) = 14,000 kWh
        // Feeder exported = 13,000 kWh
        // Loss & auxiliary = 1,000 kWh (7.14%)
        // Solar share = 4,000 / 14,000 * 100 = 28.57%
        await using var db = CreateDb();

        var station = new Station { Id = 1, BranchId = 1, Name = "Commercial Station", Code = "CS1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var settings = new StationOperationalSettings
        {
            StationId = 1,
            ProducedVsSentWarningThresholdPercent = 10m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var site = new SubStation { Id = 10, StationId = 1, Name = "Site", Code = "SITE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var generator = new Generator { Id = 5, SubStationId = 10, Name = "G1", SerialNumber = "G1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var feeder = new Feeder { Id = 3, SubStationId = 10, Name = "F1", Code = "F1", MeterMultiplier = 1m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        var meterTypeCategory = new LookupCategory
        {
            Id = 1,
            Name = "MeterType",
            DisplayName = "Meter types",
            IsActive = true
        };

        var solarLookup = new Lookup
        {
            Id = 99,
            CategoryId = meterTypeCategory.Id,
            Category = meterTypeCategory,
            Code = "SolarProductionMeter",
            Name = "عداد إنتاج الطاقة الشمسية",
            IsActive = true
        };

        var solarMeter = new Meter
        {
            Id = 20,
            SubStationId = 10,
            Name = "Solar Inverter Master Meter",
            SerialNumber = "SOLAR-INV-001",
            MeterTypeLookupId = solarLookup.Id,
            MeterTypeLookup = solarLookup,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var submission = new OperationalSubmission
        {
            Id = 100,
            SubStationId = 10,
            ClientRequestId = Guid.NewGuid(),
            ReceivedAtUtc = DateTime.UtcNow,
            Status = OperationalReadingStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = 1,
            UpdatedBy = 1
        };

        db.AddRange(station, settings, site, generator, feeder, meterTypeCategory, solarLookup, solarMeter, submission);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        // Seed readings
        db.MeterReadings.AddRange(
            // Generator reading: 0 -> 10,000
            Closing(submission.Id, MeterReadingSubjectType.GeneratorEnergyProduced, generator.Id, yesterday, DateTime.UtcNow.AddDays(-1), 0m),
            Closing(submission.Id, MeterReadingSubjectType.GeneratorEnergyProduced, generator.Id, today, DateTime.UtcNow.AddMinutes(-10), 10000m),

            // Solar meter reading: 0 -> 4,000
            Closing(submission.Id, MeterReadingSubjectType.SolarEnergyProduced, solarMeter.Id, yesterday, DateTime.UtcNow.AddDays(-1), 0m),
            Closing(submission.Id, MeterReadingSubjectType.SolarEnergyProduced, solarMeter.Id, today, DateTime.UtcNow.AddMinutes(-5), 4000m),

            // Feeder reading: 0 -> 13,000
            Closing(submission.Id, MeterReadingSubjectType.FeederEnergySent, feeder.Id, yesterday, DateTime.UtcNow.AddDays(-1), 0m),
            Closing(submission.Id, MeterReadingSubjectType.FeederEnergySent, feeder.Id, today, DateTime.UtcNow.AddMinutes(-2), 13000m)
        );
        await db.SaveChangesAsync();

        var svc = new EnergyBalanceService(db, new MeterDeltaService(db));
        var result = await svc.GetStationBalanceAsync(station.Id, today);

        Assert.True(result.HasSolarMeter);
        Assert.Equal(14000m, result.ImportKwh);
        Assert.Equal(10000m, result.GeneratorImportKwh);
        Assert.Equal(4000m, result.SolarImportKwh);
        Assert.Equal(13000m, result.ExportKwh);
        Assert.Equal(1000m, result.UnexplainedDiffKwh);
        Assert.Equal(7.14m, result.UnexplainedDiffPct, 2);
        Assert.NotNull(result.SolarSharePct);
        Assert.Equal(28.57m, result.SolarSharePct.Value, 2);
        Assert.Contains("الفاقد والاستهلاك المساعد للمحطة", result.UnexplainedLabel);
    }

    [Fact]
    public async Task GetStationBalance_WithoutSolarMeter_ExcludesSolarAndHidesShare()
    {
        // When no active solar production meter exists for the station:
        // HasSolarMeter must be false, SolarSharePct must be null
        await using var db = CreateDb();

        var station = new Station { Id = 2, BranchId = 1, Name = "Non Solar Station", Code = "NSS", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var settings = new StationOperationalSettings
        {
            StationId = 2,
            ProducedVsSentWarningThresholdPercent = 10m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var site = new SubStation { Id = 20, StationId = 2, Name = "Site2", Code = "SITE2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var generator = new Generator { Id = 6, SubStationId = 20, Name = "G2", SerialNumber = "G2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var feeder = new Feeder { Id = 4, SubStationId = 20, Name = "F2", Code = "F2", MeterMultiplier = 1m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        var submission = new OperationalSubmission
        {
            Id = 200,
            SubStationId = 20,
            ClientRequestId = Guid.NewGuid(),
            ReceivedAtUtc = DateTime.UtcNow,
            Status = OperationalReadingStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = 1,
            UpdatedBy = 1
        };

        db.AddRange(station, settings, site, generator, feeder, submission);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        db.MeterReadings.AddRange(
            Closing(submission.Id, MeterReadingSubjectType.GeneratorEnergyProduced, generator.Id, yesterday, DateTime.UtcNow.AddDays(-1), 0m),
            Closing(submission.Id, MeterReadingSubjectType.GeneratorEnergyProduced, generator.Id, today, DateTime.UtcNow.AddMinutes(-10), 5000m),
            Closing(submission.Id, MeterReadingSubjectType.FeederEnergySent, feeder.Id, yesterday, DateTime.UtcNow.AddDays(-1), 0m),
            Closing(submission.Id, MeterReadingSubjectType.FeederEnergySent, feeder.Id, today, DateTime.UtcNow.AddMinutes(-2), 4800m)
        );
        await db.SaveChangesAsync();

        var svc = new EnergyBalanceService(db, new MeterDeltaService(db));
        var result = await svc.GetStationBalanceAsync(station.Id, today);

        Assert.False(result.HasSolarMeter);
        Assert.Equal(5000m, result.ImportKwh);
        Assert.Equal(5000m, result.GeneratorImportKwh);
        Assert.Equal(0m, result.SolarImportKwh);
        Assert.Null(result.SolarSharePct);
        Assert.Equal(4800m, result.ExportKwh);
        Assert.Equal(200m, result.UnexplainedDiffKwh);
    }

    [Fact]
    public async Task MeterDeltaService_CalculatesDelta_ForSolarEnergyProduced()
    {
        await using var db = CreateDb();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        const int solarMeterId = 42;

        var sub = new OperationalSubmission
        {
            Id = 300,
            SubStationId = 1,
            ClientRequestId = Guid.NewGuid(),
            ReceivedAtUtc = DateTime.UtcNow,
            Status = OperationalReadingStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = 1,
            UpdatedBy = 1
        };
        db.OperationalSubmissions.Add(sub);

        db.MeterReadings.AddRange(
            Closing(sub.Id, MeterReadingSubjectType.SolarEnergyProduced, solarMeterId, yesterday, DateTime.UtcNow.AddDays(-1), 12500m),
            Closing(sub.Id, MeterReadingSubjectType.SolarEnergyProduced, solarMeterId, today, DateTime.UtcNow, 15000m)
        );
        await db.SaveChangesAsync();

        var deltaSvc = new MeterDeltaService(db);
        var delta = await deltaSvc.GetDailyDeltaAsync(MeterReadingSubjectType.SolarEnergyProduced, solarMeterId, today);

        Assert.NotNull(delta);
        Assert.Equal(2500m, delta.Delta);
        Assert.Equal(12500m, delta.PreviousValue);
        Assert.Equal(15000m, delta.CurrentValue);
        Assert.Null(delta.SpanDays);
    }

    [Fact]
    public void ReadingApprovalPolicy_SolarDailyClosing_RequiresTier2Approval()
    {
        // Verifies that DailyClosing readings for any meter kind require Tier-2 supervisor approval
        var requiresApproval = ReadingApprovalPolicy.RequiresApproval(OperationalSubjectType.Meter, MeterReadingKind.DailyClosing);
        Assert.True(requiresApproval, "Daily closing solar meter readings must require approval (Tier-2 gate).");
    }
}
