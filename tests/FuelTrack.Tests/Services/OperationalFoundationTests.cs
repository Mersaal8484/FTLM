using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Data;
using FuelTrack.Infrastructure.Services;
using FuelTrack.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Tests.Services;

public sealed class OperationalFoundationTests
{
    [Fact]
    public async Task AttachmentService_AcceptsValidJpegWithinApprovedLimit()
    {
        await using var db = CreateDatabase();
        var service = new AttachmentService(db, new MemoryAttachmentStorage());

        var result = await service.StoreAsync(new UploadAttachmentDto([0xFF, 0xD8, 0xFF, 0xE0, 0x00], "image/jpeg", "screen.jpg"), 12);

        Assert.True(result.IsSuccess);
        Assert.Equal("image/jpeg", result.Value!.ContentType);
        Assert.Equal(1, await db.Attachments.CountAsync());
    }

    [Fact]
    public async Task AttachmentService_RejectsWrongSignatureAndOversizedImage()
    {
        await using var db = CreateDatabase();
        var service = new AttachmentService(db, new MemoryAttachmentStorage());

        var wrongType = await service.StoreAsync(new UploadAttachmentDto([0x89, 0x50, 0x4E, 0x47], "image/png", "image.png"), 12);
        var tooLarge = await service.StoreAsync(new UploadAttachmentDto(CreateJpeg(65 * 1024 + 1), "image/jpeg", "large.jpg"), 12);

        Assert.False(wrongType.IsSuccess);
        Assert.False(tooLarge.IsSuccess);
        Assert.Empty(db.Attachments);
    }

    [Fact]
    public async Task IdempotencyService_ReplaysSameRequestAndRejectsChangedCommand()
    {
        await using var db = CreateDatabase();
        var service = new IdempotencyService(db);
        var requestId = Guid.NewGuid();
        const string hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        var first = await service.StartAsync(requestId, "operational-submission", hash, 12);
        await service.CompleteAsync(requestId, 201, "{\"id\":17}", 12);
        var replay = await service.StartAsync(requestId, "operational-submission", hash, 12);
        var conflict = await service.StartAsync(requestId, "operational-submission", new string('f', 64), 12);

        Assert.True(first.IsSuccess && first.Value!.IsNew);
        Assert.True(replay.IsSuccess && !replay.Value!.IsNew);
        Assert.Equal(201, replay.Value.ResponseStatusCode);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task IdempotencyService_AllowsTheSameKeyForDifferentTenantOrUserScopes()
    {
        await using var db = CreateDatabase();
        var requestId = Guid.NewGuid();
        const string hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        var firstTenant = new IdempotencyService(db, new StubTenantContext(Guid.NewGuid()));
        var secondTenant = new IdempotencyService(db, new StubTenantContext(Guid.NewGuid()));

        var first = await firstTenant.StartAsync(requestId, "operational-submission", hash, 12);
        var otherTenant = await secondTenant.StartAsync(requestId, "operational-submission", hash, 12);
        var otherUser = await firstTenant.StartAsync(requestId, "operational-submission", hash, 13);

        Assert.True(first.Value!.IsNew);
        Assert.True(otherTenant.Value!.IsNew);
        Assert.True(otherUser.Value!.IsNew);
        Assert.Equal(3, await db.IdempotencyRequests.CountAsync());
    }

    [Fact]
    public async Task OperationalReferenceSeed_IsIdempotentAndProvidesBilingualCodes()
    {
        await using var db = CreateDatabase();

        await OperationalReferenceDataSeed.SeedAsync(db);
        await OperationalReferenceDataSeed.SeedAsync(db);

        var serviceItems = await db.Lookups
            .Where(x => x.Category.Name == "ServiceItem")
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        Assert.Equal(["OilChangeSet", "OilFilter", "AirFilter", "Nozzles"], serviceItems.Select(x => x.Code));
        Assert.All(serviceItems, item => Assert.False(string.IsNullOrWhiteSpace(item.Name)));
    }

    [Fact]
    public async Task OperationalReferenceSeed_IsScopedToItsOwnLookupCategories()
    {
        await using var db = CreateDatabase();
        var legacyCategory = new LookupCategory
        {
            Name = "LegacyOnly",
            DisplayName = "Legacy only",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.LookupCategories.Add(legacyCategory);
        await db.SaveChangesAsync();
        db.Lookups.Add(new Lookup
        {
            CategoryId = legacyCategory.Id,
            Name = "Unrelated",
            Code = "Legacy",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await OperationalReferenceDataSeed.SeedAsync(db);

        var legacyItems = await db.Lookups.Where(x => x.CategoryId == legacyCategory.Id).ToListAsync();
        var operationalCategories = await db.LookupCategories
            .Where(x => x.Name == "PartCategory" || x.Name == "MaintenanceDepartment" || x.Name == "ServiceItem")
            .Select(x => x.Name)
            .ToListAsync();

        Assert.Single(legacyItems);
        Assert.Equal("Legacy", legacyItems[0].Code);
        Assert.Equal(3, operationalCategories.Count);
    }

    [Fact]
    public async Task StationOperationalSettings_OnlyReturnsStationsInUserScope()
    {
        await using var db = CreateDatabase();
        db.Stations.AddRange(
            new Station { Id = 1, BranchId = 1, Name = "Allowed", Code = "A" },
            new Station { Id = 2, BranchId = 1, Name = "Blocked", Code = "B" });
        await db.SaveChangesAsync();

        var scope = new AllowedStationScope();
        var user = new StubCurrentUserService { IsAdmin = false, UserId = 88 };
        var service = new StationOperationalSettingsService(db, scope, user);

        var allowed = await service.GetAsync(1);
        var blocked = await service.GetAsync(2);

        Assert.NotNull(allowed);
        Assert.Null(blocked);
        Assert.Equal("Asia/Aden", allowed!.TimeZone);
    }

    [Fact]
    public async Task CustomerFeatureSetting_ChangesOperationalCapabilityWithoutTouchingLegacyData()
    {
        await using var db = CreateDatabase();
        await CustomerFeatureSettingsSeed.SeedAsync(db);
        var service = new CustomerFeatureService(db);

        var updated = await service.UpdateAsync("operational_data", new UpdateCustomerFeatureSettingDto(true, "approved"), 7);

        Assert.True(updated.IsSuccess);
        Assert.True(updated.Value!.IsEnabled);
        Assert.Equal("approved", updated.Value.Value);
        Assert.Empty(db.DailyReadings);
    }

    private static FuelTrackDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new FuelTrackDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static byte[] CreateJpeg(int length)
    {
        var image = new byte[length];
        image[0] = 0xFF;
        image[1] = 0xD8;
        image[2] = 0xFF;
        return image;
    }

    private sealed class MemoryAttachmentStorage : IAttachmentStorage
    {
        private readonly Dictionary<string, byte[]> _objects = [];

        public Task WriteAsync(string storageKey, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
        {
            _objects[storageKey] = content.ToArray();
            return Task.CompletedTask;
        }

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream?>(_objects.TryGetValue(storageKey, out var content) ? new MemoryStream(content) : null);
    }

    private sealed class AllowedStationScope : IUserScopeFilterService
    {
        public Task<List<int>> GetAccessibleSubStationIdsAsync(int userId) => Task.FromResult(new List<int>());
        public Task<List<int>> GetAccessibleStationIdsAsync(int userId) => Task.FromResult(new List<int> { 1 });
        public Task<List<int>> GetAccessibleBranchIdsAsync(int userId) => Task.FromResult(new List<int>());
        public Task<List<int>> GetAccessibleMeterIdsAsync(int userId) => Task.FromResult(new List<int>());
        public Task<List<int>> GetUserIdsWithAccessToSubStationAsync(int subStationId) => Task.FromResult(new List<int>());
        public Task<bool> HasAccessToSubStationAsync(int userId, int subStationId) => Task.FromResult(false);
        public Task<bool> HasFullAccessToStationAsync(int userId, int stationId) => Task.FromResult(stationId == 1);
    }

    private sealed record StubTenantContext(Guid Id) : ITenantRequestContext
    {
        public Guid? TenantId => Id;
        public string? DatabaseConnection => null;
    }
}
