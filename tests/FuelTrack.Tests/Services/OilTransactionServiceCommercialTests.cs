using FuelTrack.Application.DTOs.OilTransactions;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FuelTrack.Tests.Services;

public sealed class OilTransactionServiceCommercialTests : IDisposable
{
    private readonly FuelTrackDbContext _db;

    public OilTransactionServiceCommercialTests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"OilTransactionCommercial_{Guid.NewGuid()}").Options);

        var txTypeCat = new LookupCategory { Id = 100, Name = "TransactionType", DisplayName = "Transaction Type", IsActive = true };
        var oilCat = new LookupCategory { Id = 101, Name = "OilType", DisplayName = "Oil Type", IsActive = true };

        _db.AddRange(
            txTypeCat, oilCat,
            new Lookup { Id = 200, CategoryId = 100, Category = txTypeCat, Code = "Purchase", Name = "توريد", IsActive = true },
            new Lookup { Id = 300, CategoryId = 101, Category = oilCat, Code = "15W40", Name = "زيت 15W-40", IsActive = true },
            new Branch { Id = 1, Name = "Technical Branch", Code = "TECH-PS01", IsActive = true },
            new Station { Id = 1, BranchId = 1, Name = "Commercial Station", Code = "PS01", IsActive = true },
            new SubStation { Id = 1, StationId = 1, Name = "Commercial Station", Code = "PS01", IsActive = true },
            new SubStation { Id = 2, StationId = 1, Name = "Other Site", Code = "OTH", IsActive = true },
            new User { Id = 1, UserName = "admin@ft.com", Email = "admin@ft.com", FullName = "Admin User", IsActive = true }
        );
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void EnableCommercialMode()
    {
        var existing = _db.CustomerFeatureSettings.FirstOrDefault(f => f.Key == CommercialStationModeService.FeatureKey);
        if (existing != null)
        {
            existing.IsEnabled = true;
            existing.Value = CommercialStationModeService.CommercialModeValue;
        }
        else
        {
            _db.CustomerFeatureSettings.Add(new CustomerFeatureSetting
            {
                Key = CommercialStationModeService.FeatureKey,
                IsEnabled = true,
                Value = CommercialStationModeService.CommercialModeValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        _db.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_CommercialMode_ResolvesTechnicalScopeAndStoresSubStationId()
    {
        EnableCommercialMode();
        var commercialMode = new CommercialStationModeService(_db);
        var service = new OilTransactionService(_db, new StubUserScopeFilterService(), new StubCurrentUserService(), commercialMode);

        var dto = new CreateOilTransactionDto(
            SubStationId: 0,
            OilLookupId: 300,
            TransactionTypeLookupId: 0, // Should default to Purchase
            QuantityLiters: 250,
            Reference: "DN-101",
            SupplierCompany: "PetroCo");

        var result = await service.CreateAsync(dto, 1);

        Assert.NotNull(result);
        Assert.Equal(1, result.SubStationId);
        Assert.Equal(250, result.QuantityLiters);
        Assert.Equal("DN-101", result.Reference);
    }

    [Fact]
    public async Task CreateAsync_EnterpriseMode_RejectsInvalidSubStationId()
    {
        var commercialMode = new CommercialStationModeService(_db);
        var service = new OilTransactionService(_db, new StubUserScopeFilterService(), new StubCurrentUserService(), commercialMode);

        var dto = new CreateOilTransactionDto(
            SubStationId: 999, // Non-existent site
            OilLookupId: 300,
            TransactionTypeLookupId: 200,
            QuantityLiters: 100);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAsync(dto, 1));
    }

    [Fact]
    public async Task GetAllAsync_CommercialMode_FiltersByTechnicalSubStation()
    {
        EnableCommercialMode();
        var commercialMode = new CommercialStationModeService(_db);
        var service = new OilTransactionService(_db, new StubUserScopeFilterService(), new StubCurrentUserService(), commercialMode);

        // Add commercial transaction (site 1)
        await service.CreateAsync(new CreateOilTransactionDto(
            SubStationId: 0,
            OilLookupId: 300,
            TransactionTypeLookupId: 200,
            QuantityLiters: 100), 1);

        // Add transaction for other site (site 2)
        _db.OilTransactions.Add(new OilTransaction
        {
            SubStationId = 2,
            OilLookupId = 300,
            TransactionTypeLookupId = 200,
            QuantityLiters = 50,
            TransactionDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();

        var list = await service.GetAllAsync();

        Assert.Single(list);
        Assert.Equal(1, list[0].SubStationId);
    }
}
