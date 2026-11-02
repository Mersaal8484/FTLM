using System.Net;
using System.Net.Http.Json;
using FuelTrack.Application;
using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FuelTrack.Tests.Integration;

public class WarehouseMaintenanceApiTests : IntegrationTestBase
{
    // ─────────────── Seed Helpers ───────────────

    private async Task<int> SeedSupplyStatusAsync(string code, string name)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();

        var cat = await context.LookupCategories.FirstOrDefaultAsync(c => c.Name == "WarehouseSupplyStatus");
        if (cat == null)
        {
            cat = new LookupCategory { Name = "WarehouseSupplyStatus", DisplayName = "حالة سند التوريد", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.LookupCategories.Add(cat);
            await context.SaveChangesAsync();
        }

        var lookup = await context.Lookups.FirstOrDefaultAsync(l => l.CategoryId == cat.Id && l.Code == code);
        if (lookup == null)
        {
            lookup = new Lookup { CategoryId = cat.Id, Name = name, Code = code, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.Lookups.Add(lookup);
            await context.SaveChangesAsync();
        }
        return lookup.Id;
    }

    private async Task<int> SeedTransactionTypeAsync(string code, string name)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();

        var cat = await context.LookupCategories.FirstOrDefaultAsync(c => c.Name == "WarehouseTransactionType");
        if (cat == null)
        {
            cat = new LookupCategory { Name = "WarehouseTransactionType", DisplayName = "نوع حركة المخزون", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.LookupCategories.Add(cat);
            await context.SaveChangesAsync();
        }

        var lookup = await context.Lookups.FirstOrDefaultAsync(l => l.CategoryId == cat.Id && l.Code == code);
        if (lookup == null)
        {
            lookup = new Lookup { CategoryId = cat.Id, Name = name, Code = code, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.Lookups.Add(lookup);
            await context.SaveChangesAsync();
        }
        return lookup.Id;
    }

    private async Task<int> SeedPartCategoryAsync(string code, string name)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();

        var cat = await context.LookupCategories.FirstOrDefaultAsync(c => c.Name == "PartCategory");
        if (cat == null)
        {
            cat = new LookupCategory { Name = "PartCategory", DisplayName = "فئة القطعة", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.LookupCategories.Add(cat);
            await context.SaveChangesAsync();
        }

        var lookup = await context.Lookups.FirstOrDefaultAsync(l => l.CategoryId == cat.Id && l.Code == code);
        if (lookup == null)
        {
            lookup = new Lookup { CategoryId = cat.Id, Name = name, Code = code, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.Lookups.Add(lookup);
            await context.SaveChangesAsync();
        }
        return lookup.Id;
    }

    private async Task<int> SeedPartUnitAsync(string code, string name)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();

        var cat = await context.LookupCategories.FirstOrDefaultAsync(c => c.Name == "PartUnit");
        if (cat == null)
        {
            cat = new LookupCategory { Name = "PartUnit", DisplayName = "وحدة القطعة", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.LookupCategories.Add(cat);
            await context.SaveChangesAsync();
        }

        var lookup = await context.Lookups.FirstOrDefaultAsync(l => l.CategoryId == cat.Id && l.Code == code);
        if (lookup == null)
        {
            lookup = new Lookup { CategoryId = cat.Id, Name = name, Code = code, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.Lookups.Add(lookup);
            await context.SaveChangesAsync();
        }
        return lookup.Id;
    }

    private async Task<Branch> SeedBranchAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var branch = new Branch
        {
            Name = "Test Branch",
            Code = "TB001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 1,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = 1
        };
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
        return branch;
    }

    private async Task<Station> SeedStationAsync(int branchId)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var station = new Station
        {
            BranchId = branchId,
            Name = "Test Station",
            Code = "TS001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 1,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = 1
        };
        context.Stations.Add(station);
        await context.SaveChangesAsync();
        return station;
    }

    private async Task<SparePart> SeedSparePartAsync(int categoryId, int unitId)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var part = new SparePart
        {
            Name = "Test Spare Part",
            NameEn = "Test Spare Part EN",
            PartNumber = "SP-001",
            PartCategoryLookupId = categoryId,
            UnitLookupId = unitId,
            UnitPrice = 15.5m,
            MinStockQuantity = 5,
            ReplacementIntervalHours = 100,
            IsHoursTracked = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 1,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = 1
        };
        context.SpareParts.Add(part);
        await context.SaveChangesAsync();
        return part;
    }

    private async Task<SubStation> SeedSubStationAsync(int stationId)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var sub = new SubStation
        {
            StationId = stationId,
            Name = "Test SubStation",
            Code = "SUB001",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 1,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = 1
        };
        context.SubStations.Add(sub);
        await context.SaveChangesAsync();
        return sub;
    }

    private async Task<Generator> SeedGeneratorAsync(int subStationId)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var gen = new Generator
        {
            SubStationId = subStationId,
            Name = "Test Generator",
            SerialNumber = "GEN-001",
            NominalPower = 500,
            FuelConsumptionRatePerHour = 0.3m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 1,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = 1
        };
        context.Generators.Add(gen);
        await context.SaveChangesAsync();
        return gen;
    }

    // ─────────────── GET /api/warehouse/supply ───────────────

    [Fact]
    public async Task GetSupply_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/warehouse/supply");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSupply_Authenticated_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/warehouse/supply");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<WarehouseSupplyListDto>>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    // ─────────────── POST /api/warehouse/supply ───────────────

    [Fact]
    public async Task CreateSupply_ValidRequest_ReturnsSupplyWithId()
    {
        var client = await CreateAuthenticatedClientAsync();
        var branch = await SeedBranchAsync();
        var station = await SeedStationAsync(branch.Id);
        await SeedSupplyStatusAsync("Draft", "مسودة");
        var catId = await SeedPartCategoryAsync("Mechanical", "ميكانيكية");
        var unitId = await SeedPartUnitAsync("Piece", "قطعة");
        var part = await SeedSparePartAsync(catId, unitId);

        var dto = new CreateWarehouseSupplyDto(
            StationId: station.Id,
            SupplyDate: DateTime.UtcNow,
            SupplierName: "Test Supplier",
            InvoiceNumber: "INV-001",
            InvoiceDate: DateTime.UtcNow,
            TotalAmount: 100m,
            Notes: "Test note",
            Items: new List<CreateSupplyItemDto>
            {
                new CreateSupplyItemDto(part.Id, 2m, 50m, "SN-001", "First item")
            });

        var response = await client.PostAsJsonAsync("/api/warehouse/supply", dto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<WarehouseSupplyDetailDto>();
        Assert.NotNull(detail);
        Assert.True(detail.Id > 0);
        Assert.Equal(station.Id, detail.StationId);
        Assert.Equal("Test Supplier", detail.SupplierName);
        Assert.Single(detail.Items);
    }

    [Fact]
    public async Task CreateSupply_InvalidStation_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync();
        var dto = new CreateWarehouseSupplyDto(
            StationId: 99999,
            SupplyDate: DateTime.UtcNow,
            SupplierName: "Test Supplier",
            InvoiceNumber: "INV-002",
            InvoiceDate: null,
            TotalAmount: 50m,
            Notes: null,
            Items: new List<CreateSupplyItemDto>
            {
                new CreateSupplyItemDto(1, 2m, 25m, null, null)
            });

        var response = await client.PostAsJsonAsync("/api/warehouse/supply", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateSupply_EmptyItems_ReturnsFailure()
    {
        var client = await CreateAuthenticatedClientAsync();
        var branch = await SeedBranchAsync();
        var station = await SeedStationAsync(branch.Id);
        await SeedSupplyStatusAsync("Draft", "مسودة");

        var dto = new CreateWarehouseSupplyDto(
            StationId: station.Id,
            SupplyDate: DateTime.UtcNow,
            SupplierName: "Test Supplier",
            InvoiceNumber: "INV-003",
            InvoiceDate: null,
            TotalAmount: 0m,
            Notes: null,
            Items: new List<CreateSupplyItemDto>());

        var response = await client.PostAsJsonAsync("/api/warehouse/supply", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─────────────── PUT /api/warehouse/supply/{id} ───────────────

    [Fact]
    public async Task UpdateSupply_ChangesItemsAndHeader_ReturnsUpdatedDetail()
    {
        var client = await CreateAuthenticatedClientAsync();
        var branch = await SeedBranchAsync();
        var station = await SeedStationAsync(branch.Id);
        await SeedSupplyStatusAsync("Draft", "مسودة");
        await SeedTransactionTypeAsync("Supply", "توريد");
        var catId = await SeedPartCategoryAsync("Electrical", "كهربائية");
        var unitId = await SeedPartUnitAsync("Piece", "قطعة");
        var part = await SeedSparePartAsync(catId, unitId);

        var createDto = new CreateWarehouseSupplyDto(
            StationId: station.Id,
            SupplyDate: DateTime.UtcNow,
            SupplierName: "Original Supplier",
            InvoiceNumber: "INV-001",
            InvoiceDate: DateTime.UtcNow,
            TotalAmount: 100m,
            Notes: "Original note",
            Items: new List<CreateSupplyItemDto>
            {
                new CreateSupplyItemDto(part.Id, 2m, 50m, "SN-001", "Original item")
            });

        var createRes = await client.PostAsJsonAsync("/api/warehouse/supply", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<WarehouseSupplyDetailDto>();
        Assert.NotNull(created);

        var updateDto = new UpdateWarehouseSupplyDto(
            Id: created.Id,
            StationId: station.Id,
            SupplyDate: DateTime.UtcNow,
            SupplierName: "Updated Supplier",
            InvoiceNumber: "INV-001-UPD",
            InvoiceDate: DateTime.UtcNow,
            TotalAmount: 200m,
            Notes: "Updated note",
            Items: new List<CreateSupplyItemDto>
            {
                new CreateSupplyItemDto(part.Id, 4m, 50m, "SN-001-UPD", "Updated item")
            });

        var response = await client.PutAsJsonAsync($"/api/warehouse/supply/{created.Id}", updateDto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<WarehouseSupplyDetailDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Supplier", updated.SupplierName);
        Assert.Equal(200m, updated.TotalAmount);
        Assert.Single(updated.Items);
        Assert.Equal(4m, updated.Items[0].Quantity);
    }

    // ─────────────── DELETE /api/warehouse/supply/{id} ───────────────

    [Fact]
    public async Task DeleteSupply_ExistingDraft_ReturnsNoContent()
    {
        var client = await CreateAuthenticatedClientAsync();
        var branch = await SeedBranchAsync();
        var station = await SeedStationAsync(branch.Id);
        await SeedSupplyStatusAsync("Draft", "مسودة");
        var catId = await SeedPartCategoryAsync("Mechanical", "ميكانيكية");
        var unitId = await SeedPartUnitAsync("Piece", "قطعة");
        var part = await SeedSparePartAsync(catId, unitId);

        var dto = new CreateWarehouseSupplyDto(
            StationId: station.Id,
            SupplyDate: DateTime.UtcNow,
            SupplierName: "Test Supplier",
            InvoiceNumber: "INV-DEL",
            InvoiceDate: null,
            TotalAmount: 100m,
            Notes: null,
            Items: new List<CreateSupplyItemDto>
            {
                new CreateSupplyItemDto(part.Id, 1m, 100m, null, null)
            });

        var createRes = await client.PostAsJsonAsync("/api/warehouse/supply", dto);
        var created = await createRes.Content.ReadFromJsonAsync<WarehouseSupplyDetailDto>();
        Assert.NotNull(created);

        var delRes = await client.DeleteAsync($"/api/warehouse/supply/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delRes.StatusCode);

        var getRes = await client.GetAsync($"/api/warehouse/supply/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);
    }

    // ─────────────── POST /api/warehouse/supply/{id}/confirm ───────────────

    [Fact]
    public async Task ConfirmSupply_DraftSupply_ReturnsOkAndCreatesStock()
    {
        var client = await CreateAuthenticatedClientAsync();
        var branch = await SeedBranchAsync();
        var station = await SeedStationAsync(branch.Id);
        await SeedSupplyStatusAsync("Draft", "مسودة");
        await SeedSupplyStatusAsync("Completed", "مكتمل");
        await SeedTransactionTypeAsync("Supply", "توريد");
        var catId = await SeedPartCategoryAsync("Mechanical", "ميكانيكية");
        var unitId = await SeedPartUnitAsync("Piece", "قطعة");
        var part = await SeedSparePartAsync(catId, unitId);

        var dto = new CreateWarehouseSupplyDto(
            StationId: station.Id,
            SupplyDate: DateTime.UtcNow,
            SupplierName: "Test Supplier",
            InvoiceNumber: "INV-CONF",
            InvoiceDate: null,
            TotalAmount: 100m,
            Notes: null,
            Items: new List<CreateSupplyItemDto>
            {
                new CreateSupplyItemDto(part.Id, 10m, 100m, null, null)
            });

        var createRes = await client.PostAsJsonAsync("/api/warehouse/supply", dto);
        var created = await createRes.Content.ReadFromJsonAsync<WarehouseSupplyDetailDto>();
        Assert.NotNull(created);

        var response = await client.PostAsync($"/api/warehouse/supply/{created.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var stock = await context.WarehouseStocks.FirstOrDefaultAsync(ws => ws.SparePartId == part.Id && ws.StationId == station.Id);
        Assert.NotNull(stock);
        Assert.Equal(10m, stock.CurrentQuantity);
    }

    // ─────────────── GET /api/warehouse/stock ───────────────

    [Fact]
    public async Task GetWarehouseStock_Authenticated_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();
        var branch = await SeedBranchAsync();
        var station = await SeedStationAsync(branch.Id);
        var catId = await SeedPartCategoryAsync("Mechanical", "ميكانيكية");
        var unitId = await SeedPartUnitAsync("Piece", "قطعة");
        var part = await SeedSparePartAsync(catId, unitId);

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        context.WarehouseStocks.Add(new WarehouseStock
        {
            SparePartId = part.Id,
            StationId = station.Id,
            CurrentQuantity = 100m,
            ReservedQuantity = 10m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 1,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = 1
        });
        await context.SaveChangesAsync();

        var response = await client.GetAsync("/api/warehouse/stock");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stock = await response.Content.ReadFromJsonAsync<List<WarehouseStockDto>>();
        Assert.NotNull(stock);
        Assert.True(stock.Any(s => s.SparePartId == part.Id));
    }

    [Fact]
    public async Task GetWarehouseStock_FilterByStation_ReturnsFiltered()
    {
        var client = await CreateAuthenticatedClientAsync();
        var branch = await SeedBranchAsync();
        var station1 = await SeedStationAsync(branch.Id);
        var station2 = await SeedStationAsync(branch.Id);
        var catId = await SeedPartCategoryAsync("Mechanical", "ميكانيكية");
        var unitId = await SeedPartUnitAsync("Piece", "قطعة");
        var part = await SeedSparePartAsync(catId, unitId);

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        context.WarehouseStocks.Add(new WarehouseStock
        {
            SparePartId = part.Id,
            StationId = station1.Id,
            CurrentQuantity = 100m,
            ReservedQuantity = 0m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 1,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = 1
        });
        context.WarehouseStocks.Add(new WarehouseStock
        {
            SparePartId = part.Id,
            StationId = station2.Id,
            CurrentQuantity = 50m,
            ReservedQuantity = 0m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 1,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = 1
        });
        await context.SaveChangesAsync();

        var response = await client.GetAsync($"/api/warehouse/stock?stationId={station1.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stock = await response.Content.ReadFromJsonAsync<List<WarehouseStockDto>>();
        Assert.NotNull(stock);
        Assert.Single(stock);
        Assert.Equal(station1.Id, stock[0].StationId);
        Assert.Equal("Test Spare Part", stock[0].SparePartName);
        Assert.Equal("ميكانيكية", stock[0].Category);
        Assert.Equal("قطعة", stock[0].UnitName);
    }

    // ─────────────── GET /api/installed-parts ───────────────

    [Fact]
    public async Task GetInstalledParts_Authenticated_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/installed-parts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<InstalledPartDto>>();
        Assert.NotNull(items);
    }

    // ─────────────── POST /api/installed-parts/install ───────────────

    [Fact]
    public async Task InstallPart_ValidRequest_ReturnsInstalledPart()
    {
        var client = await CreateAuthenticatedClientAsync();
        var branch = await SeedBranchAsync();
        var station = await SeedStationAsync(branch.Id);
        var sub = await SeedSubStationAsync(station.Id);
        var gen = await SeedGeneratorAsync(sub.Id);
        var catId = await SeedPartCategoryAsync("Mechanical", "ميكانيكية");
        var unitId = await SeedPartUnitAsync("Piece", "قطعة");
        var part = await SeedSparePartAsync(catId, unitId);

        var dto = new CreateInstalledPartDto(
            GeneratorId: gen.Id,
            SparePartId: part.Id,
            WorkPermitId: null,
            DispatchRequestItemId: null,
            InstallationDate: DateTime.UtcNow,
            InstallationHours: 100,
            ReplacementIntervalHours: 500,
            Notes: "Test installation");

        var response = await client.PostAsJsonAsync("/api/installed-parts/install", dto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<InstalledPartDto>();
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(gen.Id, result.GeneratorId);
        Assert.Equal(part.Id, result.SparePartId);
        Assert.Equal("Test Spare Part", result.SparePartName);
        Assert.Equal("SP-001", result.PartNumber);
    }

    [Fact]
    public async Task InstallPart_InvalidGenerator_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync();
        var catId = await SeedPartCategoryAsync("Mechanical", "ميكانيكية");
        var unitId = await SeedPartUnitAsync("Piece", "قطعة");
        var part = await SeedSparePartAsync(catId, unitId);

        var dto = new CreateInstalledPartDto(
            GeneratorId: 99999,
            SparePartId: part.Id,
            WorkPermitId: null,
            DispatchRequestItemId: null,
            InstallationDate: DateTime.UtcNow,
            InstallationHours: 100,
            ReplacementIntervalHours: null,
            Notes: null);

        var response = await client.PostAsJsonAsync("/api/installed-parts/install", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─────────────── POST /api/installed-parts/{id}/remove ───────────────

    [Fact]
    public async Task RemoveInstalledPart_ExistingPart_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();
        var branch = await SeedBranchAsync();
        var station = await SeedStationAsync(branch.Id);
        var sub = await SeedSubStationAsync(station.Id);
        var gen = await SeedGeneratorAsync(sub.Id);
        var catId = await SeedPartCategoryAsync("Mechanical", "ميكانيكية");
        var unitId = await SeedPartUnitAsync("Piece", "قطعة");
        var part = await SeedSparePartAsync(catId, unitId);

        var installDto = new CreateInstalledPartDto(
            GeneratorId: gen.Id,
            SparePartId: part.Id,
            WorkPermitId: null,
            DispatchRequestItemId: null,
            InstallationDate: DateTime.UtcNow,
            InstallationHours: 100,
            ReplacementIntervalHours: 500,
            Notes: null);

        var installRes = await client.PostAsJsonAsync("/api/installed-parts/install", installDto);
        var installed = await installRes.Content.ReadFromJsonAsync<InstalledPartDto>();
        Assert.NotNull(installed);

        var removeDto = new RemoveInstalledPartDto(
            RemovedAtHours: 550,
            EarlyReplacementReason: "Early replacement test",
            Notes: "Removed during testing");

        var response = await client.PostAsJsonAsync($"/api/installed-parts/{installed.Id}/remove", removeDto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<InstalledPartDto>();
        Assert.NotNull(result);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task RemoveInstalledPart_PartAlreadyRemoved_ReturnsFailure()
    {
        var client = await CreateAuthenticatedClientAsync();
        var branch = await SeedBranchAsync();
        var station = await SeedStationAsync(branch.Id);
        var sub = await SeedSubStationAsync(station.Id);
        var gen = await SeedGeneratorAsync(sub.Id);
        var catId = await SeedPartCategoryAsync("Mechanical", "ميكانيكية");
        var unitId = await SeedPartUnitAsync("Piece", "قطعة");
        var part = await SeedSparePartAsync(catId, unitId);

        var installDto = new CreateInstalledPartDto(
            GeneratorId: gen.Id,
            SparePartId: part.Id,
            WorkPermitId: null,
            DispatchRequestItemId: null,
            InstallationDate: DateTime.UtcNow,
            InstallationHours: 100,
            ReplacementIntervalHours: null,
            Notes: null);

        var installRes = await client.PostAsJsonAsync("/api/installed-parts/install", installDto);
        var installed = await installRes.Content.ReadFromJsonAsync<InstalledPartDto>();
        Assert.NotNull(installed);

        var removeDto = new RemoveInstalledPartDto(
            RemovedAtHours: 550,
            EarlyReplacementReason: "First removal",
            Notes: null);

        await client.PostAsJsonAsync($"/api/installed-parts/{installed.Id}/remove", removeDto);

        var removeDto2 = new RemoveInstalledPartDto(
            RemovedAtHours: 600,
            EarlyReplacementReason: "Second removal attempt",
            Notes: null);

        var response = await client.PostAsJsonAsync($"/api/installed-parts/{installed.Id}/remove", removeDto2);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─────────────── GET /api/warehouse/supply/{id} ───────────────

    [Fact]
    public async Task GetSupplyById_Existing_ReturnsDetail()
    {
        var client = await CreateAuthenticatedClientAsync();
        var branch = await SeedBranchAsync();
        var station = await SeedStationAsync(branch.Id);
        await SeedSupplyStatusAsync("Draft", "مسودة");
        var catId = await SeedPartCategoryAsync("Mechanical", "ميكانيكية");
        var unitId = await SeedPartUnitAsync("Piece", "قطعة");
        var part = await SeedSparePartAsync(catId, unitId);

        var dto = new CreateWarehouseSupplyDto(
            StationId: station.Id,
            SupplyDate: DateTime.UtcNow,
            SupplierName: "Test Supplier",
            InvoiceNumber: "INV-GET",
            InvoiceDate: null,
            TotalAmount: 100m,
            Notes: null,
            Items: new List<CreateSupplyItemDto>
            {
                new CreateSupplyItemDto(part.Id, 2m, 50m, null, null)
            });

        var createRes = await client.PostAsJsonAsync("/api/warehouse/supply", dto);
        var created = await createRes.Content.ReadFromJsonAsync<WarehouseSupplyDetailDto>();
        Assert.NotNull(created);

        var response = await client.GetAsync($"/api/warehouse/supply/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<WarehouseSupplyDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail.Id);
        Assert.Equal("Test Supplier", detail.SupplierName);
        Assert.Single(detail.Items);
    }

    [Fact]
    public async Task GetSupplyById_NotFound_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/warehouse/supply/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
