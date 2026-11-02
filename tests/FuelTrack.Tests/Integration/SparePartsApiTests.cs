using System.Net;
using System.Net.Http.Json;
using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FuelTrack.Tests.Integration;

public class SparePartsApiTests : IntegrationTestBase
{
    private async Task<int> SeedCategoryAsync(string name, string code)
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

    private async Task<int> SeedUnitAsync(string name, string code)
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

    private async Task<int> SeedPriorityAsync(string name, string code)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var cat = await context.LookupCategories.FirstOrDefaultAsync(c => c.Name == "ReplacementPriority");
        if (cat == null)
        {
            cat = new LookupCategory { Name = "ReplacementPriority", DisplayName = "أهمية الاستبدال", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
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

    private CreateSparePartDto BuildCreateDto(int categoryId, int unitId)
    {
        return new CreateSparePartDto(
            Name: "Test Part",
            NameEn: "Test Part EN",
            PartNumber: "TP-001",
            ManufacturerPartNumber: "MFG-001",
            Barcode: "BAR-001",
            PartCategoryLookupId: categoryId,
            UnitLookupId: unitId,
            Description: "Test description",
            UnitPrice: 10.5m,
            MinStockQuantity: 5,
            ReplacementIntervalHours: 100,
            IsHoursTracked: true,
            ReplacementPriorityLookupId: null,
            Notes: "Test notes");
    }

    // ─────────────── GET /api/spare-parts ───────────────

    [Fact]
    public async Task GetPaged_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/spare-parts");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPaged_Authenticated_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();
        var categoryId = await SeedCategoryAsync("Mechanical", "Mechanical");
        await SeedUnitAsync("Piece", "Piece");

        var response = await client.GetAsync("/api/spare-parts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPaged_FilterByCategoryId_ReturnsFiltered()
    {
        var client = await CreateAuthenticatedClientAsync();
        var categoryId = await SeedCategoryAsync("Electrical", "Electrical");
        var unitId = await SeedUnitAsync("Piece", "Piece");

        var dto = BuildCreateDto(categoryId, unitId);
        await client.PostAsJsonAsync("/api/spare-parts", dto);

        var response = await client.GetAsync($"/api/spare-parts?categoryId={categoryId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<SparePartListDto>>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
    }

    [Fact]
    public async Task GetPaged_FilterByCategoryName_ReturnsFiltered()
    {
        var client = await CreateAuthenticatedClientAsync();
        var categoryId = await SeedCategoryAsync("Electronic", "Electronic");
        var unitId = await SeedUnitAsync("Piece", "Piece");

        var dto = BuildCreateDto(categoryId, unitId);
        await client.PostAsJsonAsync("/api/spare-parts", dto);

        var response = await client.GetAsync("/api/spare-parts?category=Electronic");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<SparePartListDto>>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
    }

    // ─────────────── GET /api/spare-parts/all ───────────────

    [Fact]
    public async Task GetAll_ReturnsAllParts()
    {
        var client = await CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/spare-parts/all");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<SparePartListDto>>();
        Assert.NotNull(items);
    }

    // ─────────────── POST /api/spare-parts ───────────────

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        var client = await CreateAuthenticatedClientAsync();
        var categoryId = await SeedCategoryAsync("Mechanical", "Mechanical");
        var unitId = await SeedUnitAsync("Piece", "Piece");

        var dto = BuildCreateDto(categoryId, unitId);
        var response = await client.PostAsJsonAsync("/api/spare-parts", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<SparePartDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal("Test Part", detail.Name);
        Assert.Equal("TP-001", detail.PartNumber);
        Assert.Equal(categoryId, detail.PartCategoryLookupId);
        Assert.Equal(unitId, detail.UnitLookupId);
        Assert.Equal(10.5m, detail.UnitPrice);
        Assert.Equal(5, detail.MinStockQuantity);
    }

    [Fact]
    public async Task Create_DuplicatePartNumber_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        var categoryId = await SeedCategoryAsync("Mechanical", "Mechanical");
        var unitId = await SeedUnitAsync("Piece", "Piece");

        var dto = BuildCreateDto(categoryId, unitId);
        await client.PostAsJsonAsync("/api/spare-parts", dto);

        var response = await client.PostAsJsonAsync("/api/spare-parts", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─────────────── GET /api/spare-parts/{id} ───────────────

    [Fact]
    public async Task GetById_ReturnsDetail()
    {
        var client = await CreateAuthenticatedClientAsync();
        var categoryId = await SeedCategoryAsync("Mechanical", "Mechanical");
        var unitId = await SeedUnitAsync("Piece", "Piece");

        var dto = BuildCreateDto(categoryId, unitId);
        var createRes = await client.PostAsJsonAsync("/api/spare-parts", dto);
        var created = await createRes.Content.ReadFromJsonAsync<SparePartDetailDto>();
        Assert.NotNull(created);

        var response = await client.GetAsync($"/api/spare-parts/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<SparePartDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail.Id);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/spare-parts/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─────────────── PUT /api/spare-parts/{id} ───────────────

    [Fact]
    public async Task Update_ValidRequest_UpdatesPart()
    {
        var client = await CreateAuthenticatedClientAsync();
        var categoryId = await SeedCategoryAsync("Mechanical", "Mechanical");
        var unitId = await SeedUnitAsync("Piece", "Piece");

        var dto = BuildCreateDto(categoryId, unitId);
        var createRes = await client.PostAsJsonAsync("/api/spare-parts", dto);
        var created = await createRes.Content.ReadFromJsonAsync<SparePartDetailDto>();
        Assert.NotNull(created);

        var updateDto = new UpdateSparePartDto(
            Id: created.Id,
            Name: "Updated Part",
            NameEn: "Updated Part EN",
            PartNumber: "TP-002",
            ManufacturerPartNumber: "MFG-002",
            Barcode: "BAR-002",
            PartCategoryLookupId: categoryId,
            UnitLookupId: unitId,
            Description: "Updated description",
            UnitPrice: 20m,
            MinStockQuantity: 10,
            ReplacementIntervalHours: 200,
            IsHoursTracked: false,
            ReplacementPriorityLookupId: null,
            Notes: "Updated notes",
            IsActive: true);

        var response = await client.PutAsJsonAsync($"/api/spare-parts/{created.Id}", updateDto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<SparePartDetailDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Part", updated.Name);
        Assert.Equal("TP-002", updated.PartNumber);
    }

    // ─────────────── DELETE /api/spare-parts/{id} ───────────────

    [Fact]
    public async Task Delete_ExistingPart_ReturnsNoContent()
    {
        var client = await CreateAuthenticatedClientAsync();
        var categoryId = await SeedCategoryAsync("Mechanical", "Mechanical");
        var unitId = await SeedUnitAsync("Piece", "Piece");

        var dto = BuildCreateDto(categoryId, unitId);
        var createRes = await client.PostAsJsonAsync("/api/spare-parts", dto);
        var created = await createRes.Content.ReadFromJsonAsync<SparePartDetailDto>();
        Assert.NotNull(created);

        var response = await client.DeleteAsync($"/api/spare-parts/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getRes = await client.GetAsync($"/api/spare-parts/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);
    }

    // ─────────────── GET /api/spare-parts/{id}/stock ───────────────

    [Fact]
    public async Task GetStock_ReturnsStockList()
    {
        var client = await CreateAuthenticatedClientAsync();
        var categoryId = await SeedCategoryAsync("Mechanical", "Mechanical");
        var unitId = await SeedUnitAsync("Piece", "Piece");

        var dto = BuildCreateDto(categoryId, unitId);
        var createRes = await client.PostAsJsonAsync("/api/spare-parts", dto);
        var created = await createRes.Content.ReadFromJsonAsync<SparePartDetailDto>();
        Assert.NotNull(created);

        var response = await client.GetAsync($"/api/spare-parts/{created.Id}/stock");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stock = await response.Content.ReadFromJsonAsync<List<WarehouseStockDto>>();
        Assert.NotNull(stock);
    }

    // ─────────────── GET /api/spare-parts/export ───────────────

    [Fact]
    public async Task Export_ReturnsCsvFile()
    {
        var client = await CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/spare-parts/export");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("PartNumber,Name,Category,Unit,MinStock,UnitPrice,IsActive", content);
    }
}
