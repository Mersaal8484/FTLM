using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FuelTrack.Application.DTOs.Notifications;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace FuelTrack.Tests.Services;

public sealed class FuelLoadSafetyServiceTests
{
    private static FuelTrackDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private sealed class SiteScope : IUserScopeFilterService
    {
        public Task<List<int>> GetAccessibleSubStationIdsAsync(int userId) => Task.FromResult(new List<int> { 10 });
        public Task<List<int>> GetAccessibleStationIdsAsync(int userId) => Task.FromResult(new List<int> { 1 });
        public Task<List<int>> GetAccessibleBranchIdsAsync(int userId) => Task.FromResult(new List<int> { 1 });
        public Task<List<int>> GetAccessibleMeterIdsAsync(int userId) => Task.FromResult(new List<int>());
        public Task<List<int>> GetUserIdsWithAccessToSubStationAsync(int subStationId) => Task.FromResult(new List<int>());
        public Task<bool> HasAccessToSubStationAsync(int userId, int subStationId) => Task.FromResult(subStationId == 10);
        public Task<bool> HasFullAccessToStationAsync(int userId, int stationId) => Task.FromResult(stationId == 1);
    }

    private sealed class TrackingNotificationService : INotificationService
    {
        public int PublishCallCount { get; private set; }
        public List<CreateNotificationDto> PublishedNotifications { get; } = new();

        public Task<NotificationsResponseDto> GetUserNotificationsAsync(int userId)
            => Task.FromResult(new NotificationsResponseDto([], 0));
        public Task<NotificationDto> CreateAsync(CreateNotificationDto dto, int? sentById)
            => Task.FromResult(new NotificationDto(0, null, "", "", "", null, false, null, null, null, null, DateTime.UtcNow));
        public Task<NotificationDto?> PublishScopedAsync(CreateNotificationDto dto, int? sentById, IReadOnlyCollection<string> moduleKeys, CancellationToken ct = default)
        {
            PublishCallCount++;
            PublishedNotifications.Add(dto);
            return Task.FromResult<NotificationDto?>(new NotificationDto(0, null, "", "", "", null, false, null, null, null, null, DateTime.UtcNow));
        }
        public Task MarkAsReadAsync(int notificationId, int userId) => Task.CompletedTask;
        public Task MarkAllAsReadAsync(int userId) => Task.CompletedTask;
        public Task<int> GetUnreadCountAsync(int userId) => Task.FromResult(0);
        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private static async Task<(int StationId, int SubStationId, int GeneratorId, int TankId, int SubmissionId)>
        SeedBaseAsync(FuelTrackDbContext db, decimal ratedKw = 500m, decimal deadStock = 0m)
    {
        var station = new Station { Id = 1, BranchId = 1, Name = "Station", Code = "ST", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var settings = new StationOperationalSettings
        {
            StationId = 1,
            OverloadThresholdPercent = 100m,
            OverloadHysteresisReadings = 3,
            RequiresDifferentReviewer = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var site = new SubStation { Id = 10, StationId = 1, Name = "Site", Code = "SITE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var generator = new Generator
        {
            Id = 5,
            SubStationId = 10,
            Name = "G1",
            SerialNumber = "G1-SN",
            ActualPowerKW = (int)ratedKw,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var tank = new FuelTank
        {
            Id = 20,
            SubStationId = 10,
            GeneratorId = 5,
            Name = "DailyTank1",
            DeadStockLiters = (double)deadStock,
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
        db.AddRange(station, settings, site, generator, tank, submission);
        await db.SaveChangesAsync();
        return (1, 10, generator.Id, tank.Id, submission.Id);
    }

    [Fact]
    public async Task FuelConsumption_300_180_400_250_Fixture_Calculates270Consumed_And220Added()
    {
        await using var db = CreateDb();
        var (_, _, generatorId, tankId, submissionId) = await SeedBaseAsync(db);
        var notif = new TrackingNotificationService();
        var service = new FuelLoadSafetyService(db, new SiteScope(), notif);

        var t0 = new DateTime(2026, 11, 8, 8, 0, 0, DateTimeKind.Utc);
        var t1 = new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2026, 11, 8, 18, 0, 0, DateTimeKind.Utc);

        db.TankLevelReadings.AddRange(
            new TankLevelReading
            {
                OperationalSubmissionId = submissionId,
                FuelTankId = tankId,
                EventType = TankEventType.Level,
                Status = OperationalReadingStatus.Approved,
                CapturedAtUtc = t0,
                ReceivedAtUtc = t0,
                ComputedLitres = 300m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TankLevelReading
            {
                OperationalSubmissionId = submissionId,
                FuelTankId = tankId,
                EventType = TankEventType.TransferIn,
                Status = OperationalReadingStatus.Approved,
                CapturedAtUtc = t1,
                ReceivedAtUtc = t1,
                BeforeComputedLitres = 180m,
                ComputedLitres = 400m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TankLevelReading
            {
                OperationalSubmissionId = submissionId,
                FuelTankId = tankId,
                EventType = TankEventType.Level,
                Status = OperationalReadingStatus.Approved,
                CapturedAtUtc = t2,
                ReceivedAtUtc = t2,
                ComputedLitres = 250m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
        await db.SaveChangesAsync();

        var result = await service.GetConsumptionAsync(generatorId, t0, t2, 1);
        Assert.True(result.IsSuccess);
        Assert.Equal(270m, result.Value!.ConsumptionLitres);
        Assert.Equal(220m, result.Value!.RefillLitres);
    }

    [Fact]
    public async Task FuelConsumption_TwoRefillsInOneDay_CalculatesTotalRefillsAndConsumptionCorrectly()
    {
        await using var db = CreateDb();
        var (_, _, generatorId, tankId, submissionId) = await SeedBaseAsync(db);
        var service = new FuelLoadSafetyService(db, new SiteScope(), new TrackingNotificationService());

        var t0 = new DateTime(2026, 11, 8, 6, 0, 0, DateTimeKind.Utc);
        var t1 = new DateTime(2026, 11, 8, 10, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2026, 11, 8, 14, 0, 0, DateTimeKind.Utc);
        var t3 = new DateTime(2026, 11, 8, 18, 0, 0, DateTimeKind.Utc);
        var t4 = new DateTime(2026, 11, 8, 22, 0, 0, DateTimeKind.Utc);

        db.TankLevelReadings.AddRange(
            new TankLevelReading { OperationalSubmissionId = submissionId, FuelTankId = tankId, EventType = TankEventType.Level, Status = OperationalReadingStatus.Approved, CapturedAtUtc = t0, ComputedLitres = 500m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new TankLevelReading { OperationalSubmissionId = submissionId, FuelTankId = tankId, EventType = TankEventType.TransferIn, Status = OperationalReadingStatus.Approved, CapturedAtUtc = t1, BeforeComputedLitres = 400m, ComputedLitres = 800m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new TankLevelReading { OperationalSubmissionId = submissionId, FuelTankId = tankId, EventType = TankEventType.Level, Status = OperationalReadingStatus.Approved, CapturedAtUtc = t2, ComputedLitres = 650m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new TankLevelReading { OperationalSubmissionId = submissionId, FuelTankId = tankId, EventType = TankEventType.TransferIn, Status = OperationalReadingStatus.Approved, CapturedAtUtc = t3, BeforeComputedLitres = 600m, ComputedLitres = 900m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new TankLevelReading { OperationalSubmissionId = submissionId, FuelTankId = tankId, EventType = TankEventType.Level, Status = OperationalReadingStatus.Approved, CapturedAtUtc = t4, ComputedLitres = 750m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var result = await service.GetConsumptionAsync(generatorId, t0, t4, 1);
        Assert.True(result.IsSuccess);
        Assert.Equal(450m, result.Value!.ConsumptionLitres);
        Assert.Equal(700m, result.Value!.RefillLitres);
    }

    [Fact]
    public async Task FuelConsumption_UnregisteredRefill_EmitsWarning()
    {
        await using var db = CreateDb();
        var (_, _, generatorId, tankId, submissionId) = await SeedBaseAsync(db);
        var service = new FuelLoadSafetyService(db, new SiteScope(), new TrackingNotificationService());

        var t0 = new DateTime(2026, 11, 8, 8, 0, 0, DateTimeKind.Utc);
        var t1 = new DateTime(2026, 11, 8, 14, 0, 0, DateTimeKind.Utc);

        db.TankLevelReadings.AddRange(
            new TankLevelReading { OperationalSubmissionId = submissionId, FuelTankId = tankId, EventType = TankEventType.Level, Status = OperationalReadingStatus.Approved, CapturedAtUtc = t0, ComputedLitres = 200m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new TankLevelReading { OperationalSubmissionId = submissionId, FuelTankId = tankId, EventType = TankEventType.Level, Status = OperationalReadingStatus.Approved, CapturedAtUtc = t1, ComputedLitres = 350m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var result = await service.GetConsumptionAsync(generatorId, t0, t1, 1);
        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!.Warnings, w => w.Contains("Unregistered refill"));
    }

    [Fact]
    public async Task FuelConsumption_PendingRefill_FlagsAwaitingApproval_ExcludesUntilApproved()
    {
        await using var db = CreateDb();
        var (_, _, generatorId, tankId, submissionId) = await SeedBaseAsync(db);
        var service = new FuelLoadSafetyService(db, new SiteScope(), new TrackingNotificationService());

        var t0 = new DateTime(2026, 11, 8, 8, 0, 0, DateTimeKind.Utc);
        var t1 = new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2026, 11, 8, 16, 0, 0, DateTimeKind.Utc);

        db.TankLevelReadings.AddRange(
            new TankLevelReading { OperationalSubmissionId = submissionId, FuelTankId = tankId, EventType = TankEventType.Level, Status = OperationalReadingStatus.Approved, CapturedAtUtc = t0, ComputedLitres = 200m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new TankLevelReading { OperationalSubmissionId = submissionId, FuelTankId = tankId, EventType = TankEventType.TransferIn, Status = OperationalReadingStatus.Pending, CapturedAtUtc = t1, BeforeComputedLitres = 180m, ComputedLitres = 500m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new TankLevelReading { OperationalSubmissionId = submissionId, FuelTankId = tankId, EventType = TankEventType.Level, Status = OperationalReadingStatus.Approved, CapturedAtUtc = t2, ComputedLitres = 450m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var result = await service.GetConsumptionAsync(generatorId, t0, t2, 1);
        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!.Warnings, w => w.Contains("awaiting approval"));
        Assert.DoesNotContain(result.Value!.Warnings, w => w.Contains("Unregistered refill"));
        Assert.Equal(0m, result.Value!.RefillLitres); // excluded from official figures until approved
    }

    [Fact]
    public void DeadStock_650Measured_100DeadStock_Calculates550Usable_AndConsumptionUnchanged()
    {
        var usable = TankVolumeService.UsableLitres(650m, 100m);
        Assert.Equal(550m, usable);

        Assert.True(TankVolumeService.IsAtOrBelowDeadStock(100m, 100m));
        Assert.True(TankVolumeService.IsAtOrBelowDeadStock(80m, 100m));
        Assert.False(TankVolumeService.IsAtOrBelowDeadStock(650m, 100m));
    }

    [Fact]
    public async Task Overload_Exactly100Percent_IsNotOverload()
    {
        await using var db = CreateDb();
        var (_, _, generatorId, _, submissionId) = await SeedBaseAsync(db, ratedKw: 500m);
        var notif = new TrackingNotificationService();
        var service = new FuelLoadSafetyService(db, new SiteScope(), notif);

        var reading = new OperationalGeneratorReading
        {
            OperationalSubmissionId = submissionId,
            GeneratorId = generatorId,
            Status = OperationalReadingStatus.Recorded,
            CapturedAtUtc = DateTime.UtcNow,
            ReceivedAtUtc = DateTime.UtcNow,
            ActiveLoadKw = 500m, // Exactly 100%
            IsRunning = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.OperationalGeneratorReadings.Add(reading);
        await db.SaveChangesAsync();

        await service.EvaluateOnReceiptAsync(reading.Id);

        var overloads = await db.OverloadEvents.ToListAsync();
        Assert.Empty(overloads);
        Assert.Equal(0, notif.PublishCallCount);
    }

    [Fact]
    public async Task Overload_Above100Percent_CreatesOverloadEvent_AndOneNotificationPerEpisode()
    {
        await using var db = CreateDb();
        var (_, _, generatorId, _, submissionId) = await SeedBaseAsync(db, ratedKw: 500m);
        var notif = new TrackingNotificationService();
        var service = new FuelLoadSafetyService(db, new SiteScope(), notif);

        var t0 = DateTime.UtcNow;
        var r1 = new OperationalGeneratorReading
        {
            OperationalSubmissionId = submissionId,
            GeneratorId = generatorId,
            Status = OperationalReadingStatus.Recorded,
            CapturedAtUtc = t0,
            ReceivedAtUtc = t0,
            ActiveLoadKw = 520m, // 104%
            IsRunning = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.OperationalGeneratorReadings.Add(r1);
        await db.SaveChangesAsync();

        await service.EvaluateOnReceiptAsync(r1.Id);

        var overloads = await db.OverloadEvents.ToListAsync();
        Assert.Single(overloads);
        Assert.Equal(1, notif.PublishCallCount);

        // Second reading in same episode with higher peak
        var t1 = t0.AddHours(1);
        var r2 = new OperationalGeneratorReading
        {
            OperationalSubmissionId = submissionId,
            GeneratorId = generatorId,
            Status = OperationalReadingStatus.Recorded,
            CapturedAtUtc = t1,
            ReceivedAtUtc = t1,
            ActiveLoadKw = 550m, // 110%
            IsRunning = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.OperationalGeneratorReadings.Add(r2);
        await db.SaveChangesAsync();

        await service.EvaluateOnReceiptAsync(r2.Id);

        overloads = await db.OverloadEvents.ToListAsync();
        Assert.Single(overloads);
        Assert.Equal(110m, overloads[0].PeakPercent);
        Assert.Equal(1, notif.PublishCallCount); // Still exactly one notification per episode
    }

    [Fact]
    public async Task Overload_Hysteresis_ResolvesAfterConfiguredNormalReadings_AndStampsReadingTime()
    {
        await using var db = CreateDb();
        var (_, _, generatorId, _, submissionId) = await SeedBaseAsync(db, ratedKw: 500m);
        var notif = new TrackingNotificationService();
        var service = new FuelLoadSafetyService(db, new SiteScope(), notif);

        var t0 = DateTime.UtcNow;
        var r1 = new OperationalGeneratorReading
        {
            OperationalSubmissionId = submissionId,
            GeneratorId = generatorId,
            Status = OperationalReadingStatus.Recorded,
            CapturedAtUtc = t0,
            ReceivedAtUtc = t0,
            ActiveLoadKw = 550m,
            IsRunning = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.OperationalGeneratorReadings.Add(r1);
        await db.SaveChangesAsync();
        await service.EvaluateOnReceiptAsync(r1.Id);

        var episode = await db.OverloadEvents.SingleAsync();
        Assert.Null(episode.ResolvedAtUtc);

        // Normal reading 1
        var t1 = t0.AddHours(1);
        var r2 = new OperationalGeneratorReading { OperationalSubmissionId = submissionId, GeneratorId = generatorId, Status = OperationalReadingStatus.Recorded, CapturedAtUtc = t1, ReceivedAtUtc = t1, ActiveLoadKw = 400m, IsRunning = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.OperationalGeneratorReadings.Add(r2);
        await db.SaveChangesAsync();
        await service.EvaluateOnReceiptAsync(r2.Id);
        episode = await db.OverloadEvents.SingleAsync();
        Assert.Null(episode.ResolvedAtUtc);
        Assert.Equal(1, episode.ConsecutiveNormalReadings);

        // Normal reading 2
        var t2 = t0.AddHours(2);
        var r3 = new OperationalGeneratorReading { OperationalSubmissionId = submissionId, GeneratorId = generatorId, Status = OperationalReadingStatus.Recorded, CapturedAtUtc = t2, ReceivedAtUtc = t2, ActiveLoadKw = 400m, IsRunning = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.OperationalGeneratorReadings.Add(r3);
        await db.SaveChangesAsync();
        await service.EvaluateOnReceiptAsync(r3.Id);
        episode = await db.OverloadEvents.SingleAsync();
        Assert.Null(episode.ResolvedAtUtc);
        Assert.Equal(2, episode.ConsecutiveNormalReadings);

        // Normal reading 3 (Hysteresis = 3 reached -> resolve!)
        var t3 = t0.AddHours(3);
        var r4 = new OperationalGeneratorReading { OperationalSubmissionId = submissionId, GeneratorId = generatorId, Status = OperationalReadingStatus.Recorded, CapturedAtUtc = t3, ReceivedAtUtc = t3, ActiveLoadKw = 400m, IsRunning = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.OperationalGeneratorReadings.Add(r4);
        await db.SaveChangesAsync();
        await service.EvaluateOnReceiptAsync(r4.Id);
        episode = await db.OverloadEvents.SingleAsync();

        Assert.NotNull(episode.ResolvedAtUtc);
        Assert.Equal(t3, episode.ResolvedAtUtc.Value); // Stamped with reading capture time, not UtcNow
    }

    [Fact]
    public void Architecture_CalculationServices_DoNotContainInlineStatusChecks_UseReadingApprovalPolicy()
    {
        var baseDir = AppContext.BaseDirectory;
        // Search upwards to find solution root
        var dir = new DirectoryInfo(baseDir);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FuelTrack.sln")))
        {
            dir = dir.Parent;
        }

        if (dir == null) return; // In CI environments where source is not bundled

        var servicesDir = Path.Combine(dir.FullName, "src", "FuelTrack.Infrastructure", "Services");
        if (!Directory.Exists(servicesDir)) return;

        var calculationServices = new[] { "FuelLoadSafetyService.cs", "MeterDeltaService.cs", "EnergyBalanceService.cs" };
        foreach (var svcName in calculationServices)
        {
            var filePath = Path.Combine(servicesDir, svcName);
            if (!File.Exists(filePath)) continue;

            var code = File.ReadAllText(filePath);
            // Asserts that no inline Recorded || Approved or custom Official check is used
            Assert.DoesNotContain("OperationalReadingStatus.Recorded ||", code);
            Assert.DoesNotContain("OperationalReadingStatus.Approved ||", code);
            Assert.DoesNotContain("private static bool Official", code);

            // Asserts ReadingApprovalPolicy is used
            Assert.Contains("ReadingApprovalPolicy", code);
        }
    }

    [Fact]
    public async Task Tier1Readings_AreExcludedFromReviewQueue()
    {
        await using var db = CreateDb();
        var (_, _, generatorId, _, _) = await SeedBaseAsync(db);

        // Tier 1 submission with Status = Recorded
        var subRecorded = new OperationalSubmission
        {
            Id = 201,
            SubStationId = 10,
            ClientRequestId = Guid.NewGuid(),
            ReceivedAtUtc = DateTime.UtcNow,
            Status = OperationalReadingStatus.Recorded,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Tier 2 submission with Status = Pending
        var subPending = new OperationalSubmission
        {
            Id = 202,
            SubStationId = 10,
            ClientRequestId = Guid.NewGuid(),
            ReceivedAtUtc = DateTime.UtcNow,
            Status = OperationalReadingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.OperationalSubmissions.AddRange(subRecorded, subPending);
        await db.SaveChangesAsync();

        var reviewService = new OperationalReviewService(db, new SiteScope(), new TrackingNotificationService(), new MeterDeltaService(db));
        var queue = await reviewService.GetPendingSubmissionsAsync(1, 10);

        Assert.Single(queue);
        Assert.Equal(202, queue[0].Id);
        Assert.DoesNotContain(queue, s => s.Id == 201);
    }

    [Fact]
    public async Task OperationalCompleteness_UsesStationInterval_AndFlagsAverageLoadMismatch()
    {
        await using var db = CreateDb();
        var (_, _, generatorId, _, submissionId) = await SeedBaseAsync(db);
        var date = new DateOnly(2026, 11, 8);
        var start = new DateTime(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc);
        var settings = await db.StationOperationalSettings.SingleAsync();
        settings.TimeZone = "UTC";
        settings.MissingDataIntervalMinutes = 60;
        settings.AverageLoadWarningTolerancePercent = 15m;

        db.OperationalGeneratorReadings.AddRange(
            new OperationalGeneratorReading { OperationalSubmissionId = submissionId, GeneratorId = generatorId, ClientLineId = Guid.NewGuid(), CapturedAtUtc = start, ReceivedAtUtc = start, BusinessDate = date, Status = OperationalReadingStatus.Recorded, ActiveLoadKw = 100m, CreatedAt = start, UpdatedAt = start },
            new OperationalGeneratorReading { OperationalSubmissionId = submissionId, GeneratorId = generatorId, ClientLineId = Guid.NewGuid(), CapturedAtUtc = start.AddHours(1), ReceivedAtUtc = start.AddHours(1), BusinessDate = date, Status = OperationalReadingStatus.Recorded, ActiveLoadKw = 100m, CreatedAt = start, UpdatedAt = start });
        db.MeterReadings.AddRange(
            new MeterReading { OperationalSubmissionId = submissionId, ClientLineId = Guid.NewGuid(), SubjectType = MeterReadingSubjectType.GeneratorEnergyProduced, SubjectId = generatorId, Kind = MeterReadingKind.DailyClosing, CapturedAtUtc = start.AddDays(-1), ReceivedAtUtc = start, BusinessDate = date.AddDays(-1), Status = OperationalReadingStatus.Recorded, RawValue = 100m, MultiplierSnapshot = 1m, CreatedAt = start, UpdatedAt = start },
            new MeterReading { OperationalSubmissionId = submissionId, ClientLineId = Guid.NewGuid(), SubjectType = MeterReadingSubjectType.GeneratorEnergyProduced, SubjectId = generatorId, Kind = MeterReadingKind.DailyClosing, CapturedAtUtc = start.AddHours(23), ReceivedAtUtc = start, BusinessDate = date, Status = OperationalReadingStatus.Recorded, RawValue = 200m, MultiplierSnapshot = 1m, CreatedAt = start, UpdatedAt = start },
            new MeterReading { OperationalSubmissionId = submissionId, ClientLineId = Guid.NewGuid(), SubjectType = MeterReadingSubjectType.GeneratorRunningHours, SubjectId = generatorId, Kind = MeterReadingKind.DailyClosing, CapturedAtUtc = start.AddDays(-1), ReceivedAtUtc = start, BusinessDate = date.AddDays(-1), Status = OperationalReadingStatus.Recorded, RawValue = 10m, MultiplierSnapshot = 1m, CreatedAt = start, UpdatedAt = start },
            new MeterReading { OperationalSubmissionId = submissionId, ClientLineId = Guid.NewGuid(), SubjectType = MeterReadingSubjectType.GeneratorRunningHours, SubjectId = generatorId, Kind = MeterReadingKind.DailyClosing, CapturedAtUtc = start.AddHours(23), ReceivedAtUtc = start, BusinessDate = date, Status = OperationalReadingStatus.Recorded, RawValue = 12m, MultiplierSnapshot = 1m, CreatedAt = start, UpdatedAt = start });
        await db.SaveChangesAsync();

        var result = await new OperationalCompletenessService(db, new SiteScope()).GetAsync(generatorId, date, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(50m, result.Value!.AverageLoadKw);
        Assert.Equal(100m, result.Value.MeanActiveLoadKw);
        Assert.True(result.Value.AverageLoadWarning);
        Assert.DoesNotContain("00:00", result.Value.MissingSlots);
        Assert.Contains("02:00", result.Value.MissingSlots);
        Assert.False(result.Value.MissingDailyClosing);
    }

    [Fact]
    public async Task FeederCompleteness_UsesScope_AndReportsMissingSlots()
    {
        await using var db = CreateDb();
        var (_, subStationId, _, _, submissionId) = await SeedBaseAsync(db);
        var date = new DateOnly(2026, 11, 8);
        var capturedAt = new DateTime(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc);
        var settings = await db.StationOperationalSettings.SingleAsync();
        settings.TimeZone = "UTC";
        var feeder = new Feeder { Id = 40, SubStationId = subStationId, Code = "F1", Name = "Feeder 1", CreatedAt = capturedAt, UpdatedAt = capturedAt };
        db.Feeders.Add(feeder);
        db.FeederLoadReadings.Add(new FeederLoadReading { OperationalSubmissionId = submissionId, FeederId = feeder.Id, ClientLineId = Guid.NewGuid(), CapturedAtUtc = capturedAt, ReceivedAtUtc = capturedAt, BusinessDate = date, Status = OperationalReadingStatus.Recorded, CreatedAt = capturedAt, UpdatedAt = capturedAt });
        db.MeterReadings.Add(new MeterReading { OperationalSubmissionId = submissionId, ClientLineId = Guid.NewGuid(), SubjectType = MeterReadingSubjectType.FeederEnergySent, SubjectId = feeder.Id, Kind = MeterReadingKind.DailyClosing, CapturedAtUtc = capturedAt.AddHours(23), ReceivedAtUtc = capturedAt, BusinessDate = date, Status = OperationalReadingStatus.Recorded, RawValue = 50m, MultiplierSnapshot = 1m, CreatedAt = capturedAt, UpdatedAt = capturedAt });
        await db.SaveChangesAsync();

        var result = await new OperationalCompletenessService(db, new SiteScope()).GetFeederAsync(feeder.Id, date, 1);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("00:00", result.Value!.MissingSlots);
        Assert.Contains("01:00", result.Value.MissingSlots);
        Assert.False(result.Value.MissingDailyClosing);
    }

    [Fact]
    public async Task DailyLog_HourlyProduction_LabelsActiveLoadFallbackAsEstimated()
    {
        await using var db = CreateDb();
        var (_, _, generatorId, _, submissionId) = await SeedBaseAsync(db);
        var date = new DateOnly(2026, 11, 8);
        var capturedAt = new DateTime(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc);
        db.OperationalGeneratorReadings.Add(new OperationalGeneratorReading
        {
            OperationalSubmissionId = submissionId,
            GeneratorId = generatorId,
            ClientLineId = Guid.NewGuid(),
            CapturedAtUtc = capturedAt,
            ReceivedAtUtc = capturedAt,
            BusinessDate = date,
            Status = OperationalReadingStatus.Recorded,
            ActiveLoadKw = 33m,
            CreatedAt = capturedAt,
            UpdatedAt = capturedAt
        });
        await db.SaveChangesAsync();

        var result = await new FuelLoadSafetyService(db, new SiteScope(), new TrackingNotificationService())
            .GetDailyLogAsync(generatorId, date, 1);

        Assert.True(result.IsSuccess);
        var firstHour = Assert.Single(result.Value!.HourlyProduction.Where(point => point.SlotStartUtc == capturedAt));
        Assert.Equal(33m, firstHour.Kwh);
        Assert.True(firstHour.IsEstimated);
    }
}
