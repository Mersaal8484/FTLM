using FuelTrack.Application.Common;
using FuelTrack.Application.Constants;
using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FuelTrack.Tests.Services;

public sealed class CommercialC6_2OilReplacementTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly Mock<ICommercialStationModeService> _commercialMode = new();
    private readonly Mock<IServiceDueEvaluationService> _dueEvaluation = new();
    private readonly StubUserScopeFilterService _scopeFilter = new();
    private readonly StubCurrentUserService _currentUser = new() { IsAdmin = true, UserId = 1 };
    private readonly OilLedgerService _oilLedger;
    private readonly ServiceReplacementService _service;

    public CommercialC6_2OilReplacementTests()
    {
        var options = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"CommercialC6_2_{Guid.NewGuid()}")
            .Options;
        _db = new FuelTrackDbContext(options);

        _commercialMode.Setup(x => x.IsCommercialModeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _oilLedger = new OilLedgerService(
            _db,
            _scopeFilter,
            _currentUser,
            _commercialMode.Object);

        _service = new ServiceReplacementService(
            _db,
            _dueEvaluation.Object,
            _scopeFilter,
            _currentUser,
            new IdempotencyService(_db),
            NullLogger<ServiceReplacementService>.Instance,
            _commercialMode.Object,
            _oilLedger);
    }

    private async Task<(SubStation Site, Generator Gen, ServiceItem Item, Lookup OilLookup, Oil OilRecord, Lookup ConsumptionType)> SeedBasicSetupAsync(
        decimal initialStock = 100m, decimal configuredLiters = 15m)
    {
        var branch = new Branch { Id = 1, Name = "B", Code = "B" };
        var station = new Station { Id = 1, BranchId = branch.Id, Name = "S", Code = "S" };
        var site = new SubStation { Id = 1, StationId = station.Id, Name = "Commercial Station", Code = "CS" };
        var gen = new Generator { Id = 10, SubStationId = site.Id, SubStation = site, Name = "Gen-1", Model = "CAT-C15" };
        var item = new ServiceItem { Id = 20, Code = "OilFilter", NameAr = "فلتر زيت", NameEn = "Oil Filter", IsActive = true };

        var oilCat = new LookupCategory { Id = 1, Name = "OilType", DisplayName = "Oil Type" };
        var txCat = new LookupCategory { Id = 100, Name = "OilTransactionType", DisplayName = "Oil Transaction Type" };
        var oilLookup = new Lookup { Id = 30, CategoryId = oilCat.Id, Code = "15W40", Name = "15W-40", IsActive = true };
        var consumptionType = new Lookup { Id = 206, CategoryId = txCat.Id, Code = "Consumption", Name = "استهلاك", IsActive = true };

        var oil = new Oil { Id = 40, SubStationId = site.Id, OilLookupId = oilLookup.Id, CurrentStock = initialStock, MinStock = 10 };

        _db.Branches.Add(branch);
        _db.Stations.Add(station);
        _db.SubStations.Add(site);
        _db.Generators.Add(gen);
        _db.ServiceItems.Add(item);
        _db.LookupCategories.AddRange(oilCat, txCat);
        _db.Lookups.AddRange(oilLookup, consumptionType);
        _db.Oils.Add(oil);

        if (configuredLiters > 0)
        {
            _db.GeneratorMaintenanceOilConfigurations.Add(new GeneratorMaintenanceOilConfiguration
            {
                GeneratorId = gen.Id,
                ServiceItemId = item.Id,
                OilId = oil.Id,
                QuantityLiters = configuredLiters
            });
        }

        await _db.SaveChangesAsync();

        _commercialMode.Setup(x => x.GetScopeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommercialStationScope(branch.Id, station.Id, site.Id));

        return (site, gen, item, oilLookup, oil, consumptionType);
    }

    [Fact]
    public async Task RecordReplacement_ConfiguredItem_CreatesApprovedConsumptionTransactionAndLinksOilTransactionId()
    {
        var (site, gen, item, oilLookup, _, consumptionType) = await SeedBasicSetupAsync(initialStock: 100m, configuredLiters: 15m);

        var request = new RecordReplacementRequestDto(
            GeneratorId: gen.Id,
            ServiceItemId: item.Id,
            HoursAtService: 500m,
            OccurredAtUtc: DateTime.UtcNow,
            ClientRequestId: Guid.NewGuid());

        var result = await _service.RecordReplacementAsync(request, 1);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.OilTransactionId);
        Assert.Null(result.Value.Warnings);

        var oilTx = Assert.Single(_db.OilTransactions);
        Assert.Equal(site.Id, oilTx.SubStationId);
        Assert.Equal(oilLookup.Id, oilTx.OilLookupId);
        Assert.Equal(consumptionType.Id, oilTx.TransactionTypeLookupId);
        Assert.Equal(15.0, oilTx.QuantityLiters);
        Assert.Equal($"SRV-{result.Value.Id}", oilTx.Reference);
        Assert.Contains(item.NameAr, oilTx.Notes);
        Assert.True(oilTx.IsApproved);
        Assert.Null(oilTx.CorrectedFromId);
        Assert.Equal(oilTx.Id, result.Value.OilTransactionId);

        var balance = await _oilLedger.GetCurrentBalanceAsync(site.Id, oilLookup.Id);
        Assert.Equal(85m, balance);
    }

    [Fact]
    public async Task RecordReplacement_UnconfiguredItem_CreatesRecordWithoutOilTransaction_ReturnsOilNotConfiguredWarning()
    {
        var (site, gen, item, oilLookup, _, _) = await SeedBasicSetupAsync(initialStock: 100m, configuredLiters: 0);

        var request = new RecordReplacementRequestDto(
            GeneratorId: gen.Id,
            ServiceItemId: item.Id,
            HoursAtService: 500m,
            OccurredAtUtc: DateTime.UtcNow,
            ClientRequestId: Guid.NewGuid());

        var result = await _service.RecordReplacementAsync(request, 1);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.OilTransactionId);
        Assert.NotNull(result.Value.Warnings);
        Assert.Contains(ServiceReplacementWarningCodes.OilNotConfigured, result.Value.Warnings);

        Assert.Empty(_db.OilTransactions);
        Assert.Single(_db.ServiceReplacementRecords);

        var balance = await _oilLedger.GetCurrentBalanceAsync(site.Id, oilLookup.Id);
        Assert.Equal(100m, balance);
    }

    [Fact]
    public async Task RecordBulkBaseline_NeverConsumesOil()
    {
        var (site, gen, item, oilLookup, _, _) = await SeedBasicSetupAsync(initialStock: 100m, configuredLiters: 20m);

        var request = new BulkBaselineRequestDto(
            GeneratorId: gen.Id,
            Items: [new BulkBaselineRowDto(item.Id, 250m, DateTime.UtcNow)],
            ClientRequestId: Guid.NewGuid());

        var result = await _service.RecordBulkBaselineAsync(request, 1);

        Assert.True(result.IsSuccess);
        var rec = Assert.Single(result.Value!);
        Assert.Equal(ServiceRecordKind.Baseline, rec.Kind);
        Assert.Null(rec.OilTransactionId);

        Assert.Empty(_db.OilTransactions);

        var balance = await _oilLedger.GetCurrentBalanceAsync(site.Id, oilLookup.Id);
        Assert.Equal(100m, balance);
    }

    [Fact]
    public async Task RecordMultiItemReplacement_MixedConfiguration_ProcessesEachCorrectly()
    {
        var (site, gen, item1, oilLookup, oil, consumptionType) = await SeedBasicSetupAsync(initialStock: 100m, configuredLiters: 12m);

        var item2 = new ServiceItem { Id = 21, Code = "AirFilter", NameAr = "فلتر هواء", NameEn = "Air Filter", IsActive = true };
        _db.ServiceItems.Add(item2);
        await _db.SaveChangesAsync();

        var request = new MultiItemReplacementRequestDto(
            GeneratorId: gen.Id,
            HoursAtService: 600m,
            OccurredAtUtc: DateTime.UtcNow,
            WorkPermitId: null,
            Items:
            [
                new MultiItemReplacementRowDto(item1.Id),
                new MultiItemReplacementRowDto(item2.Id)
            ],
            ClientRequestId: Guid.NewGuid());

        var result = await _service.RecordMultiItemReplacementAsync(request, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);

        var rec1 = result.Value.Single(x => x.ServiceItemId == item1.Id);
        var rec2 = result.Value.Single(x => x.ServiceItemId == item2.Id);

        Assert.NotNull(rec1.OilTransactionId);
        Assert.Null(rec1.Warnings);

        Assert.Null(rec2.OilTransactionId);
        Assert.NotNull(rec2.Warnings);
        Assert.Contains(ServiceReplacementWarningCodes.OilNotConfigured, rec2.Warnings);

        var oilTx = Assert.Single(_db.OilTransactions);
        Assert.Equal(rec1.OilTransactionId, oilTx.Id);
        Assert.Equal(12.0, oilTx.QuantityLiters);

        var balance = await _oilLedger.GetCurrentBalanceAsync(site.Id, oilLookup.Id);
        Assert.Equal(88m, balance);
    }

    [Fact]
    public async Task RecordReplacement_IdempotentReplay_DoesNotCreateDuplicateOilTransaction()
    {
        var (_, gen, item, _, _, _) = await SeedBasicSetupAsync(initialStock: 100m, configuredLiters: 15m);
        var clientRequestId = Guid.NewGuid();

        var request = new RecordReplacementRequestDto(
            GeneratorId: gen.Id,
            ServiceItemId: item.Id,
            HoursAtService: 500m,
            OccurredAtUtc: DateTime.UtcNow,
            ClientRequestId: clientRequestId);

        var first = await _service.RecordReplacementAsync(request, 1);
        Assert.True(first.IsSuccess);
        Assert.NotNull(first.Value!.OilTransactionId);

        var second = await _service.RecordReplacementAsync(request, 1);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.Id, second.Value!.Id);
        Assert.Equal(first.Value.OilTransactionId, second.Value.OilTransactionId);

        Assert.Single(_db.ServiceReplacementRecords);
        Assert.Single(_db.OilTransactions);
    }

    [Fact]
    public async Task RecordReplacement_InsufficientStock_DoesNotBlock_CreatesTransactionAndAttachesWarning()
    {
        var (site, gen, item, oilLookup, _, _) = await SeedBasicSetupAsync(initialStock: 5m, configuredLiters: 15m);

        var request = new RecordReplacementRequestDto(
            GeneratorId: gen.Id,
            ServiceItemId: item.Id,
            HoursAtService: 500m,
            OccurredAtUtc: DateTime.UtcNow,
            ClientRequestId: Guid.NewGuid());

        var result = await _service.RecordReplacementAsync(request, 1);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.OilTransactionId);
        Assert.NotNull(result.Value.Warnings);
        Assert.Contains(ServiceReplacementWarningCodes.OilStockInsufficient, result.Value.Warnings);

        Assert.Single(_db.OilTransactions);

        var balance = await _oilLedger.GetCurrentBalanceAsync(site.Id, oilLookup.Id);
        Assert.Equal(-10m, balance); // Went negative without blocking
    }

    [Fact]
    public async Task VoidAsync_ReversesOilTransactionAtomicallyWithCompensatingTransaction_ZeroNetLedgerEffect()
    {
        var (site, gen, item, oilLookup, _, consumptionType) = await SeedBasicSetupAsync(initialStock: 100m, configuredLiters: 15m);

        var request = new RecordReplacementRequestDto(
            GeneratorId: gen.Id,
            ServiceItemId: item.Id,
            HoursAtService: 500m,
            OccurredAtUtc: DateTime.UtcNow,
            ClientRequestId: Guid.NewGuid());

        var recResult = await _service.RecordReplacementAsync(request, 1);
        Assert.True(recResult.IsSuccess);
        var recordId = recResult.Value!.Id;

        var balanceAfterService = await _oilLedger.GetCurrentBalanceAsync(site.Id, oilLookup.Id);
        Assert.Equal(85m, balanceAfterService);

        var voidResult = await _service.VoidAsync(recordId, new VoidReplacementRecordDto("Mistaken entry"), 1);
        Assert.True(voidResult.IsSuccess);

        Assert.Equal(2, _db.OilTransactions.Count());

        var original = await _db.OilTransactions.FirstAsync(x => x.Id == recResult.Value.OilTransactionId);
        Assert.True(original.IsApproved);
        Assert.Equal(15.0, original.QuantityLiters);

        var compensating = await _db.OilTransactions.FirstAsync(x => x.Reference == $"VOID-SRV-{recordId}");
        Assert.True(compensating.IsApproved);
        Assert.Equal(-15.0, compensating.QuantityLiters);
        Assert.Equal(consumptionType.Id, compensating.TransactionTypeLookupId);
        Assert.Contains("Mistaken entry", compensating.Notes);

        var balanceRestored = await _oilLedger.GetCurrentBalanceAsync(site.Id, oilLookup.Id);
        Assert.Equal(100m, balanceRestored); // Net ledger effect is exactly zero

        var secondVoid = await _service.VoidAsync(recordId, new VoidReplacementRecordDto("Second try"), 1);
        Assert.False(secondVoid.IsSuccess);
        Assert.Equal(400, secondVoid.StatusCode);
        Assert.Equal("This service replacement record has already been voided.", secondVoid.Error);
    }

    [Fact]
    public async Task RecordReplacement_InvalidOilConfiguration_ReturnsFailureInsteadOfThrowing()
    {
        var (_, gen, item, oilLookup, _, _) = await SeedBasicSetupAsync(initialStock: 100m, configuredLiters: 0m);
        var oil = await _db.Oils.SingleAsync(x => x.OilLookupId == oilLookup.Id);
        _db.GeneratorMaintenanceOilConfigurations.Add(new GeneratorMaintenanceOilConfiguration
        {
            GeneratorId = gen.Id,
            ServiceItemId = item.Id,
            OilId = oil.Id,
            QuantityLiters = 0m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 1,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = 1
        });
        await _db.SaveChangesAsync();

        var result = await _service.RecordReplacementAsync(new RecordReplacementRequestDto(
            gen.Id, item.Id, 500m, DateTime.UtcNow, ClientRequestId: Guid.NewGuid()), 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task RecordReplacement_EnterpriseMode_NeverCreatesOilTransaction()
    {
        var (site, gen, item, oilLookup, _, _) = await SeedBasicSetupAsync(initialStock: 100m, configuredLiters: 15m);

        // Switch off commercial mode (Enterprise mode)
        _commercialMode.Setup(x => x.IsCommercialModeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var request = new RecordReplacementRequestDto(
            GeneratorId: gen.Id,
            ServiceItemId: item.Id,
            HoursAtService: 500m,
            OccurredAtUtc: DateTime.UtcNow,
            ClientRequestId: Guid.NewGuid());

        var result = await _service.RecordReplacementAsync(request, 1);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.OilTransactionId);
        Assert.Null(result.Value.Warnings);

        Assert.Empty(_db.OilTransactions);
        Assert.Single(_db.ServiceReplacementRecords);

        var balance = await _oilLedger.GetCurrentBalanceAsync(site.Id, oilLookup.Id);
        Assert.Equal(100m, balance);
    }

    [Fact]
    public async Task RecordReplacement_UserWithoutAccessToSubStation_ReturnsForbidden()
    {
        var (_, gen, item, _, _, _) = await SeedBasicSetupAsync(initialStock: 100m, configuredLiters: 15m);

        var user = new StubCurrentUserService { IsAdmin = false, UserId = 99 };
        var deniedScope = new Mock<IUserScopeFilterService>();
        deniedScope.Setup(x => x.HasAccessToSubStationAsync(99, It.IsAny<int>())).ReturnsAsync(false);

        var service = new ServiceReplacementService(
            _db,
            _dueEvaluation.Object,
            deniedScope.Object,
            user,
            new IdempotencyService(_db),
            NullLogger<ServiceReplacementService>.Instance,
            _commercialMode.Object,
            _oilLedger);

        var request = new RecordReplacementRequestDto(
            GeneratorId: gen.Id,
            ServiceItemId: item.Id,
            HoursAtService: 500m,
            OccurredAtUtc: DateTime.UtcNow,
            ClientRequestId: Guid.NewGuid());

        var result = await service.RecordReplacementAsync(request, 99);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
        Assert.Empty(_db.ServiceReplacementRecords);
        Assert.Empty(_db.OilTransactions);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
