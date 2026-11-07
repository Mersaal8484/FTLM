using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Tests.Services;

public sealed class OperationalCaptureServiceTests
{
    [Fact]
    public async Task SubmitAsync_CreatesRecordedTierOneCaptureAndReplaysIdempotentRequest()
    {
        await using var db = CreateDatabase();
        var fixture = await SeedAsync(db);
        var service = CreateService(db);
        var dto = CreateGeneratorCapture(fixture, DateTime.UtcNow.AddMinutes(-1));

        var first = await service.SubmitAsync(dto, fixture.UserId);
        var replay = await service.SubmitAsync(dto, fixture.UserId);

        Assert.True(first.IsSuccess);
        Assert.True(replay.IsSuccess);
        Assert.Equal(first.Value!.Id, replay.Value!.Id);
        var saved = await db.OperationalGeneratorReadings.SingleAsync();
        Assert.Equal(FuelTrack.Domain.Enums.OperationalReadingStatus.Recorded, saved.Status);
        Assert.Equal(1, await db.AttachmentLinks.CountAsync(x => x.EntityType == nameof(OperationalGeneratorReading)));
    }

    [Fact]
    public async Task SubmitAsync_RejectsFutureCaptureAndMissingPhoto()
    {
        await using var db = CreateDatabase();
        var fixture = await SeedAsync(db);
        var service = CreateService(db);
        var future = CreateGeneratorCapture(fixture, DateTime.UtcNow.AddMinutes(6));
        var noPhoto = CreateGeneratorCapture(fixture, DateTime.UtcNow.AddMinutes(-2)) with
        {
            GeneratorReadings = [CreateGeneratorCapture(fixture, DateTime.UtcNow.AddMinutes(-2)).GeneratorReadings[0] with { PhotoAttachmentIds = [] }]
        };

        var futureResult = await service.SubmitAsync(future, fixture.UserId);
        var photoResult = await service.SubmitAsync(noPhoto, fixture.UserId);

        Assert.False(futureResult.IsSuccess);
        Assert.Contains("future", futureResult.Error!, StringComparison.OrdinalIgnoreCase);
        Assert.False(photoResult.IsSuccess);
        Assert.Contains("photo", photoResult.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitAsync_ReturnsDuplicateTimeForSameGeneratorAndFreeCaptureMinute()
    {
        await using var db = CreateDatabase();
        var fixture = await SeedAsync(db);
        var service = CreateService(db);
        var capturedAt = DateTime.UtcNow.AddMinutes(-3);
        var first = CreateGeneratorCapture(fixture, capturedAt);
        var duplicate = CreateGeneratorCapture(fixture, capturedAt);

        Assert.True((await service.SubmitAsync(first, fixture.UserId)).IsSuccess);
        var result = await service.SubmitAsync(duplicate, fixture.UserId);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!.Lines!, x => x.Outcome == "duplicate_time");
    }

    [Fact]
    public async Task SubmitAsync_RequiresDistinctDeviceLineIds()
    {
        await using var db = CreateDatabase();
        var fixture = await SeedAsync(db);
        var service = CreateService(db);
        var dto = CreateGeneratorCapture(fixture, DateTime.UtcNow.AddMinutes(-1)) with
        {
            GeneratorReadings = [CreateGeneratorCapture(fixture, DateTime.UtcNow.AddMinutes(-1)).GeneratorReadings[0] with { ClientLineId = null }]
        };

        var result = await service.SubmitAsync(dto, fixture.UserId);

        Assert.False(result.IsSuccess);
        Assert.Contains("client line ID", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SubmitAsync_AcceptsValidLinesWhenAnotherLineIsRejected()
    {
        await using var db = CreateDatabase();
        var fixture = await SeedAsync(db);
        var service = CreateService(db);
        var valid = CreateGeneratorCapture(fixture, DateTime.UtcNow.AddMinutes(-1));
        var invalid = CreateGeneratorCapture(fixture, DateTime.UtcNow.AddMinutes(6));
        var dto = valid with { GeneratorReadings = [valid.GeneratorReadings[0], invalid.GeneratorReadings[0]] };

        var result = await service.SubmitAsync(dto, fixture.UserId);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.GeneratorReadingCount);
        Assert.Contains(result.Value.Lines!, x => x.Outcome == "accepted");
        Assert.Contains(result.Value.Lines!, x => x.Outcome == "rejected");
    }

    [Fact]
    public async Task SubmitAsync_HidesSiteOutsideCallerScope()
    {
        await using var db = CreateDatabase();
        var fixture = await SeedAsync(db);
        var service = new OperationalCaptureService(db, new SiteScope(false), new CurrentUser(), new IdempotencyService(db));

        var result = await service.SubmitAsync(CreateGeneratorCapture(fixture, DateTime.UtcNow.AddMinutes(-1)), fixture.UserId);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.StatusCode);
    }

    private static OperationalCaptureService CreateService(FuelTrackDbContext db) =>
        new(db, new SiteScope(), new CurrentUser(), new IdempotencyService(db));

    private static async Task<(int UserId, int SiteId, int GeneratorId, int AttachmentId)> SeedAsync(FuelTrackDbContext db)
    {
        const int userId = 41;
        var station = new Station { Id = 10, BranchId = 1, Name = "Station", Code = "ST", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var site = new SubStation { Id = 20, StationId = station.Id, Name = "Site", Code = "SITE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var generator = new Generator { Id = 30, SubStationId = site.Id, Name = "Generator", SerialNumber = "G-1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var attachment = new Attachment { StorageKey = "test.jpg", OriginalFileName = "test.jpg", ContentType = "image/jpeg", ByteSize = 5, Sha256 = "a", CreatedBy = userId, UpdatedBy = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.AddRange(station, site, generator, attachment);
        await db.SaveChangesAsync();
        return (userId, site.Id, generator.Id, attachment.Id);
    }

    private static SubmitOperationalCaptureDto CreateGeneratorCapture((int UserId, int SiteId, int GeneratorId, int AttachmentId) fixture, DateTime capturedAt) => new(
        Guid.NewGuid(), fixture.SiteId,
        [new GeneratorCaptureLineDto(fixture.GeneratorId, capturedAt, [fixture.AttachmentId], true, 400m, 1m, 80m, 2m, 90m, 100m, null, null, null, null, null, Guid.NewGuid())], [], [], []);

    private static FuelTrackDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<FuelTrackDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        var db = new FuelTrackDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private sealed class SiteScope(bool allowsSite = true) : IUserScopeFilterService
    {
        public Task<List<int>> GetAccessibleSubStationIdsAsync(int userId) => Task.FromResult(new List<int> { 20 });
        public Task<List<int>> GetAccessibleStationIdsAsync(int userId) => Task.FromResult(new List<int> { 10 });
        public Task<List<int>> GetAccessibleBranchIdsAsync(int userId) => Task.FromResult(new List<int> { 1 });
        public Task<List<int>> GetAccessibleMeterIdsAsync(int userId) => Task.FromResult(new List<int>());
        public Task<List<int>> GetUserIdsWithAccessToSubStationAsync(int subStationId) => Task.FromResult(new List<int>());
        public Task<bool> HasAccessToSubStationAsync(int userId, int subStationId) => Task.FromResult(allowsSite && subStationId == 20);
        public Task<bool> HasFullAccessToStationAsync(int userId, int stationId) => Task.FromResult(stationId == 10);
    }

    private sealed class CurrentUser : ICurrentUserService
    {
        public int? UserId => 41; public string? UserName => "operator"; public bool IsAuthenticated => true;
        public bool IsAdmin => false; public bool IsSystemOwner => false; public bool HasAnyAdminRole => false;
    }
}
