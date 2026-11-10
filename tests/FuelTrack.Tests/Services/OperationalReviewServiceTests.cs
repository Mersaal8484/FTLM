using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Domain.Services;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FuelTrack.Tests.Services;

/// <summary>Phase 3 — Review queue, approve/reject state machine tests.</summary>
public sealed class OperationalReviewServiceTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static FuelTrackDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static OperationalReviewService CreateService(
        FuelTrackDbContext db, bool sameUserAllowed = false)
    {
        var scope = new SiteScope();
        var notif = new StubNotificationService();
        var delta = new MeterDeltaService(db);
        var safety = new FuelLoadSafetyService(db, scope, notif);
        return new OperationalReviewService(db, scope, notif, delta, safety);
    }

    private static async Task<(int SubmissionId, int SubmitterId, int ReviewerId, int SubStationId, int MeterReadingId)>
        SeedPendingSubmissionAsync(FuelTrackDbContext db, bool sameUser = false)
    {
        const int stationId = 1;
        const int subStationId = 10;
        const int submitterId = 100;
        int reviewerId = sameUser ? submitterId : 200;

        var station = new Station { Id = stationId, BranchId = 1, Name = "S", Code = "S", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var site = new SubStation { Id = subStationId, StationId = stationId, Name = "Site", Code = "SITE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var settings = new StationOperationalSettings
        {
            StationId = stationId, RequiresDifferentReviewer = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.AddRange(station, site, settings);

        var submission = new OperationalSubmission
        {
            SubStationId = subStationId, ClientRequestId = Guid.NewGuid(),
            ReceivedAtUtc = DateTime.UtcNow, Status = OperationalReadingStatus.Pending,
            SubmittedByUserId = submitterId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = submitterId, UpdatedBy = submitterId
        };
        db.OperationalSubmissions.Add(submission);
        await db.SaveChangesAsync();

        var generator = new Generator { Id = 1, SubStationId = subStationId, Name = "G1", SerialNumber = "G1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Generators.Add(generator);

        var genReading = new OperationalGeneratorReading
        {
            OperationalSubmissionId = submission.Id, GeneratorId = generator.Id,
            ClientLineId = Guid.NewGuid(), CapturedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            ReceivedAtUtc = DateTime.UtcNow, BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = OperationalReadingStatus.Pending, IsRunning = true, ActiveLoadKw = 100m,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = submitterId, UpdatedBy = submitterId
        };

        // Seed a DailyClosing meter reading for monotonicity tests
        var meter = new Meter { Id = 1, SubStationId = subStationId, Name = "M1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Meters.Add(meter);

        var meterReading = new MeterReading
        {
            OperationalSubmissionId = submission.Id, ClientLineId = Guid.NewGuid(),
            SubjectType = MeterReadingSubjectType.GeneratorEnergyProduced, SubjectId = generator.Id,
            Kind = MeterReadingKind.DailyClosing, CapturedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ReceivedAtUtc = DateTime.UtcNow, BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = OperationalReadingStatus.Pending, RawValue = 500m, MultiplierSnapshot = 1m,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = submitterId, UpdatedBy = submitterId
        };

        db.AddRange(genReading, meterReading);
        await db.SaveChangesAsync();

        return (submission.Id, submitterId, reviewerId, subStationId, meterReading.Id);
    }

    // ── Approve ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveSubmission_SetsAllLinesApproved()
    {
        await using var db = CreateDb();
        var (submissionId, _, reviewerId, _, _) = await SeedPendingSubmissionAsync(db);
        var service = CreateService(db);

        var result = await service.ApproveSubmissionAsync(submissionId, reviewerId);

        Assert.True(result.IsSuccess, result.Error);
        var genLine = await db.OperationalGeneratorReadings.FirstAsync();
        Assert.Equal(OperationalReadingStatus.Approved, genLine.Status);
        Assert.NotNull(genLine.ApprovedAt);
        Assert.Equal(reviewerId, genLine.ApprovedByUserId);
        var submission = await db.OperationalSubmissions.FindAsync(submissionId);
        Assert.Equal(OperationalReadingStatus.Approved, submission!.Status);
    }

    [Fact]
    public async Task ApproveSubmission_EvaluatesDailyTankDeadStockAfterCommit()
    {
        await using var db = CreateDb();
        var (submissionId, _, reviewerId, subStationId, _) = await SeedPendingSubmissionAsync(db);
        var generator = await db.Generators.SingleAsync();
        var now = DateTime.UtcNow;
        var tank = new FuelTank
        {
            SubStationId = subStationId,
            GeneratorId = generator.Id,
            Name = "Daily tank",
            Code = "DT-1",
            DeadStockLiters = 100,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.FuelTanks.Add(tank);
        await db.SaveChangesAsync();
        db.TankLevelReadings.Add(new TankLevelReading
        {
            OperationalSubmissionId = submissionId,
            FuelTankId = tank.Id,
            ClientLineId = Guid.NewGuid(),
            CapturedAtUtc = now,
            ReceivedAtUtc = now,
            BusinessDate = DateOnly.FromDateTime(now),
            Status = OperationalReadingStatus.Pending,
            EventType = TankEventType.Level,
            ComputedLitres = 90m,
            DeadStockLitresSnapshot = 100m,
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).ApproveSubmissionAsync(submissionId, reviewerId);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Single(await db.TankDeadStockEpisodes.ToListAsync());
    }

    [Fact]
    public async Task ApproveSubmission_RejectsWhenApproverIsSubmitter()
    {
        await using var db = CreateDb();
        var (submissionId, submitterId, _, _, _) = await SeedPendingSubmissionAsync(db, sameUser: false);
        var service = CreateService(db);

        // Try to approve as the same user who submitted
        var result = await service.ApproveSubmissionAsync(submissionId, submitterId);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task ApproveSubmission_IsIdempotent_WhenAlreadyApproved()
    {
        await using var db = CreateDb();
        var (submissionId, _, reviewerId, _, _) = await SeedPendingSubmissionAsync(db);
        var service = CreateService(db);

        var first = await service.ApproveSubmissionAsync(submissionId, reviewerId);
        var second = await service.ApproveSubmissionAsync(submissionId, reviewerId);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess); // idempotent
    }

    // ── Reject ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RejectLine_SetsRejectedWithReason()
    {
        await using var db = CreateDb();
        var (submissionId, _, reviewerId, _, _) = await SeedPendingSubmissionAsync(db);
        var service = CreateService(db);
        var genLine = await db.OperationalGeneratorReadings.FirstAsync();

        var result = await service.RejectLineAsync("generator", genLine.Id,
            new RejectLineDto("Incorrect values"), reviewerId);

        Assert.True(result.IsSuccess, result.Error);
        await db.Entry(genLine).ReloadAsync();
        Assert.Equal(OperationalReadingStatus.Rejected, genLine.Status);
        Assert.Equal("Incorrect values", genLine.RejectionReason);
        Assert.Equal(reviewerId, genLine.RejectedByUserId);
    }

    [Fact]
    public async Task RejectLine_FailsOnEmptyReason()
    {
        await using var db = CreateDb();
        var (_, _, reviewerId, _, _) = await SeedPendingSubmissionAsync(db);
        var service = CreateService(db);
        var genLine = await db.OperationalGeneratorReadings.FirstAsync();

        var result = await service.RejectLineAsync("generator", genLine.Id,
            new RejectLineDto(""), reviewerId);

        Assert.False(result.IsSuccess);
        Assert.Equal(422, result.StatusCode);
    }

    [Fact]
    public async Task RejectLine_SubmissionStatusBecomesRejectedWhenAllLinesRejected()
    {
        await using var db = CreateDb();
        var (submissionId, _, reviewerId, _, meterReadingId) = await SeedPendingSubmissionAsync(db);
        var service = CreateService(db);

        var genLine = await db.OperationalGeneratorReadings.FirstAsync();
        await service.RejectLineAsync("generator", genLine.Id, new RejectLineDto("Bad gen"), reviewerId);
        await service.RejectLineAsync("meter", meterReadingId, new RejectLineDto("Bad meter"), reviewerId);

        var sub = await db.OperationalSubmissions.FindAsync(submissionId);
        Assert.Equal(OperationalReadingStatus.Rejected, sub!.Status);
    }

    [Fact]
    public async Task GetPendingSubmissions_FiltersToScope()
    {
        await using var db = CreateDb();
        var (submissionId, _, _, subStationId, _) = await SeedPendingSubmissionAsync(db);
        var service = CreateService(db);

        // SiteScope always returns subStationId=10
        var list = await service.GetPendingSubmissionsAsync(999, null);
        Assert.Contains(list, s => s.Id == submissionId);
    }

    // ── Stubs ─────────────────────────────────────────────────────────────────

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

    private sealed class StubNotificationService : INotificationService
    {
        public Task<FuelTrack.Application.DTOs.Notifications.NotificationsResponseDto> GetUserNotificationsAsync(int userId)
            => Task.FromResult(new FuelTrack.Application.DTOs.Notifications.NotificationsResponseDto([], 0));
        public Task<FuelTrack.Application.DTOs.Notifications.NotificationDto> CreateAsync(FuelTrack.Application.DTOs.Notifications.CreateNotificationDto dto, int? sentById)
            => Task.FromResult(new FuelTrack.Application.DTOs.Notifications.NotificationDto(0, null, "", "", "", null, false, null, null, null, null, DateTime.UtcNow));
        public Task<FuelTrack.Application.DTOs.Notifications.NotificationDto?> PublishScopedAsync(FuelTrack.Application.DTOs.Notifications.CreateNotificationDto dto, int? sentById, IReadOnlyCollection<string> moduleKeys, CancellationToken ct = default)
            => Task.FromResult<FuelTrack.Application.DTOs.Notifications.NotificationDto?>(null);
        public Task MarkAsReadAsync(int notificationId, int userId) => Task.CompletedTask;
        public Task MarkAllAsReadAsync(int userId) => Task.CompletedTask;
        public Task<int> GetUnreadCountAsync(int userId) => Task.FromResult(0);
        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    [Fact]
    public async Task Approval_Concurrency_SerializedExecution_PreventsRaceConditions()
    {
        await using var db = CreateDb();
        var (submissionId, submitterId, reviewerId, _, _) = await SeedPendingSubmissionAsync(db);
        var service1 = CreateService(db);
        var service2 = CreateService(db);

        var task1 = service1.ApproveSubmissionAsync(submissionId, reviewerId);
        var task2 = service2.ApproveSubmissionAsync(submissionId, reviewerId);

        var results = await Task.WhenAll(task1, task2);

        Assert.All(results, r => Assert.True(r.IsSuccess));

        var sub = await db.OperationalSubmissions.FindAsync(submissionId);
        Assert.Equal(OperationalReadingStatus.Approved, sub!.Status);
    }
}
