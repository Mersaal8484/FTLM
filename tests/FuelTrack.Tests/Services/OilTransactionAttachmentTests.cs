using FuelTrack.Application.Common;
using FuelTrack.Application.Constants;
using FuelTrack.Application.DTOs.OilTransactions;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FuelTrack.Tests.Services;

public sealed class OilTransactionAttachmentTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly Mock<IAttachmentService> _attachments = new();

    public OilTransactionAttachmentTests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"OilTransactionAttachment_{Guid.NewGuid()}").Options);

        var transactionCategory = new LookupCategory { Id = 1, Name = "TransactionType", DisplayName = "Transaction type", IsActive = true };
        var oilCategory = new LookupCategory { Id = 2, Name = "OilType", DisplayName = "Oil type", IsActive = true };
        _db.AddRange(
            transactionCategory,
            oilCategory,
            new Lookup { Id = 10, Category = transactionCategory, CategoryId = 1, Name = "Purchase", Code = "Purchase", IsActive = true },
            new Lookup { Id = 20, Category = oilCategory, CategoryId = 2, Name = "Engine oil", Code = "EngineOil", IsActive = true },
            new Branch { Id = 1, Name = "Branch", Code = "BR", IsActive = true },
            new Station { Id = 1, BranchId = 1, Name = "Station", Code = "ST", IsActive = true },
            new SubStation { Id = 1, StationId = 1, Name = "Site", Code = "SS", IsActive = true },
            new Attachment { Id = 100, CreatedBy = 1, StorageKey = "oil-100", OriginalFileName = "oil.jpg", ContentType = "image/jpeg", ByteSize = 100, Sha256 = "100" },
            new Attachment { Id = 101, CreatedBy = 2, StorageKey = "oil-101", OriginalFileName = "foreign.jpg", ContentType = "image/jpeg", ByteSize = 100, Sha256 = "101" },
            new Attachment { Id = 102, CreatedBy = 1, StorageKey = "oil-102", OriginalFileName = "linked.jpg", ContentType = "image/jpeg", ByteSize = 100, Sha256 = "102" });
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Fact]
    public async Task CreateAsync_AllowsOilSupplyWithAndWithoutAttachments()
    {
        _attachments.Setup(x => x.LinkAsync(100, AttachmentEntityTypes.OilTransaction, It.IsAny<int>(), 0, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var service = CreateService();

        var withoutAttachment = await service.CreateAsync(CreateDto(), 1);
        var withAttachment = await service.CreateAsync(CreateDto([100]), 1);

        Assert.Empty(withoutAttachment.AttachmentIds!);
        Assert.Equal([100], withAttachment.AttachmentIds);
        _attachments.Verify(x => x.LinkAsync(100, AttachmentEntityTypes.OilTransaction, withAttachment.Id, 0, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_RejectsForeignUserAttachment()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => CreateService().CreateAsync(CreateDto([101]), 1));
        Assert.Empty(_db.OilTransactions);
    }

    [Fact]
    public async Task CreateAsync_RejectsAlreadyLinkedAttachment()
    {
        _db.AttachmentLinks.Add(new AttachmentLink { AttachmentId = 102, EntityType = "Elsewhere", EntityId = 7, SortOrder = 0 });
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().CreateAsync(CreateDto([102]), 1));
        Assert.Empty(_db.OilTransactions);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateOrMoreThanThreeAttachmentIds()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().CreateAsync(CreateDto([100, 100]), 1));
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().CreateAsync(CreateDto([100, 101, 102, 103]), 1));

        Assert.Empty(_db.OilTransactions);
    }

    [Fact]
    public async Task CreateAsync_RollsBackOilTransactionWhenAttachmentLinkFails()
    {
        _attachments.Setup(x => x.LinkAsync(100, AttachmentEntityTypes.OilTransaction, It.IsAny<int>(), 0, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Link failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().CreateAsync(CreateDto([100]), 1));

        Assert.Empty(_db.OilTransactions);
        Assert.Empty(_db.AttachmentLinks);
    }

    [Fact]
    public async Task CreateAsync_EnterpriseModeKeepsRequestedSubStation()
    {
        var result = await CreateService().CreateAsync(CreateDto(), 1);

        Assert.Equal(1, result.SubStationId);
    }

    private OilTransactionService CreateService() => new(
        _db,
        new StubUserScopeFilterService(),
        new StubCurrentUserService(),
        new CommercialStationModeService(_db),
        _attachments.Object);

    private static CreateOilTransactionDto CreateDto(IReadOnlyList<int>? attachmentIds = null) => new(
        SubStationId: 1,
        OilLookupId: 20,
        TransactionTypeLookupId: 10,
        QuantityLiters: 25,
        AttachmentIds: attachmentIds);
}
