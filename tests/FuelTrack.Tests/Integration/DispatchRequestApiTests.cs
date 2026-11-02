using System.Net;
using System.Net.Http.Json;
using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FuelTrack.Tests.Integration;

public class DispatchRequestApiTests : IntegrationTestBase
{
    private async Task<(int stationId, int subStationId, int sparePartId, int otherSubStationId)> SeedAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();

        if (!context.Branches.Any())
        {
            context.Branches.Add(new Branch { Name = "Dispatch Test Branch", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 });
            await context.SaveChangesAsync();
        }
        var branch = await context.Branches.FirstAsync();

        if (!context.Stations.Any())
        {
            context.Stations.Add(new Station { BranchId = branch.Id, Name = "Dispatch Test Station", Code = "DTS-1", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 });
            await context.SaveChangesAsync();
        }
        var stationId = (await context.Stations.FirstAsync()).Id;

        int subStationId;
        if (!await context.SubStations.AnyAsync(s => s.Name == "Dispatch Sub A"))
        {
            var ss = new SubStation { StationId = stationId, Name = "Dispatch Sub A", Code = "DSA", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.SubStations.Add(ss);
            await context.SaveChangesAsync();
        }
        subStationId = (await context.SubStations.FirstAsync(s => s.Name == "Dispatch Sub A")).Id;

        int otherSubStationId;
        if (!await context.SubStations.AnyAsync(s => s.Name == "Dispatch Sub B"))
        {
            var ss = new SubStation { StationId = stationId, Name = "Dispatch Sub B", Code = "DSB", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.SubStations.Add(ss);
            await context.SaveChangesAsync();
        }
        otherSubStationId = (await context.SubStations.FirstAsync(s => s.Name == "Dispatch Sub B")).Id;

        // DispatchStatus lookups
        var statusCat = await context.LookupCategories.FirstOrDefaultAsync(c => c.Name == "DispatchStatus");
        if (statusCat == null)
        {
            statusCat = new LookupCategory { Name = "DispatchStatus", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.LookupCategories.Add(statusCat);
            await context.SaveChangesAsync();
        }
        var pending = await context.Lookups.FirstOrDefaultAsync(l => l.CategoryId == statusCat.Id && l.Code == "Pending");
        if (pending == null)
        {
            pending = new Lookup { CategoryId = statusCat.Id, Category = statusCat, Name = "Pending", Code = "Pending", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.Lookups.Add(pending);
            await context.SaveChangesAsync();
        }
        var approved = await context.Lookups.FirstOrDefaultAsync(l => l.CategoryId == statusCat.Id && l.Code == "Approved");
        if (approved == null)
        {
            approved = new Lookup { CategoryId = statusCat.Id, Category = statusCat, Name = "Approved", Code = "Approved", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.Lookups.Add(approved);
            await context.SaveChangesAsync();
        }
        var partialApproved = await context.Lookups.FirstOrDefaultAsync(l => l.CategoryId == statusCat.Id && l.Code == "PartialApproved");
        if (partialApproved == null)
        {
            partialApproved = new Lookup { CategoryId = statusCat.Id, Category = statusCat, Name = "Partial Approved", Code = "PartialApproved", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.Lookups.Add(partialApproved);
            await context.SaveChangesAsync();
        }
        var rejected = await context.Lookups.FirstOrDefaultAsync(l => l.CategoryId == statusCat.Id && l.Code == "Rejected");
        if (rejected == null)
        {
            rejected = new Lookup { CategoryId = statusCat.Id, Category = statusCat, Name = "Rejected", Code = "Rejected", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.Lookups.Add(rejected);
            await context.SaveChangesAsync();
        }
        var issued = await context.Lookups.FirstOrDefaultAsync(l => l.CategoryId == statusCat.Id && l.Code == "Issued");
        if (issued == null)
        {
            issued = new Lookup { CategoryId = statusCat.Id, Category = statusCat, Name = "Issued", Code = "Issued", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.Lookups.Add(issued);
            await context.SaveChangesAsync();
        }

        // WarehouseTransactionType lookup
        var txCat = await context.LookupCategories.FirstOrDefaultAsync(c => c.Name == "WarehouseTransactionType");
        if (txCat == null)
        {
            txCat = new LookupCategory { Name = "WarehouseTransactionType", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.LookupCategories.Add(txCat);
            await context.SaveChangesAsync();
        }
        var dispatchTx = await context.Lookups.FirstOrDefaultAsync(l => l.CategoryId == txCat.Id && l.Code == "Dispatch");
        if (dispatchTx == null)
        {
            dispatchTx = new Lookup { CategoryId = txCat.Id, Category = txCat, Name = "Dispatch", Code = "Dispatch", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.Lookups.Add(dispatchTx);
            await context.SaveChangesAsync();
        }

        // Spare part
        int sparePartId;
        if (!await context.SpareParts.AnyAsync())
        {
            var unitCat = await context.LookupCategories.FirstOrDefaultAsync(c => c.Name == "Unit");
            if (unitCat == null)
            {
                unitCat = new LookupCategory { Name = "Unit", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
                context.LookupCategories.Add(unitCat);
                await context.SaveChangesAsync();
            }
            var unitLookup = await context.Lookups.FirstOrDefaultAsync(l => l.CategoryId == unitCat.Id && l.Code == "pcs");
            if (unitLookup == null)
            {
                unitLookup = new Lookup { CategoryId = unitCat.Id, Category = unitCat, Name = "Pieces", Code = "pcs", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
                context.Lookups.Add(unitLookup);
                await context.SaveChangesAsync();
            }

            var partCat = await context.LookupCategories.FirstOrDefaultAsync(c => c.Name == "PartCategory");
            if (partCat == null)
            {
                partCat = new LookupCategory { Name = "PartCategory", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
                context.LookupCategories.Add(partCat);
                await context.SaveChangesAsync();
            }
            var partCatLookup = await context.Lookups.FirstOrDefaultAsync(l => l.CategoryId == partCat.Id && l.Code == "general");
            if (partCatLookup == null)
            {
                partCatLookup = new Lookup { CategoryId = partCat.Id, Category = partCat, Name = "General", Code = "general", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
                context.Lookups.Add(partCatLookup);
                await context.SaveChangesAsync();
            }

            var part = new SparePart
            {
                Name = "Test Part",
                PartNumber = "TP-001",
                UnitLookupId = unitLookup.Id,
                PartCategoryLookupId = partCatLookup.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = 1
            };
            context.SpareParts.Add(part);
            await context.SaveChangesAsync();
        }
        sparePartId = (await context.SpareParts.FirstAsync()).Id;

        // Warehouse stock for the station
        if (!await context.WarehouseStocks.AnyAsync(ws => ws.StationId == stationId && ws.SparePartId == sparePartId))
        {
            context.WarehouseStocks.Add(new WarehouseStock
            {
                StationId = stationId,
                SparePartId = sparePartId,
                CurrentQuantity = 100,
                ReservedQuantity = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = 1
            });
            await context.SaveChangesAsync();
        }

        return (stationId, subStationId, sparePartId, otherSubStationId);
    }

    private CreateDispatchRequestDto BuildCreateDto(int stationId, int subStationId, int sparePartId)
    {
        return new CreateDispatchRequestDto(
            SubStationId: subStationId,
            StationId: stationId,
            WorkPermitId: null,
            Purpose: "Test dispatch",
            Notes: "Integration test notes",
            Items: new List<CreateDispatchItemDto>
            {
                new CreateDispatchItemDto(SparePartId: sparePartId, RequestedQuantity: 5, Notes: "First item")
            });
    }

    // ─────────────── GET /api/warehouse/dispatch ───────────────

    [Fact]
    public async Task GetAll_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/warehouse/dispatch");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_Authenticated_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();
        await SeedAsync();

        var response = await client.GetAsync("/api/warehouse/dispatch");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─────────────── POST /api/warehouse/dispatch ───────────────

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedWithDetail()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (stationId, subStationId, sparePartId, _) = await SeedAsync();

        var dto = BuildCreateDto(stationId, subStationId, sparePartId);
        var response = await client.PostAsJsonAsync("/api/warehouse/dispatch", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<DispatchRequestDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal(subStationId, detail.SubStationId);
        Assert.Equal(stationId, detail.StationId);
        Assert.Equal("Test dispatch", detail.Purpose);
        Assert.Single(detail.Items);
        Assert.Equal(sparePartId, detail.Items[0].SparePartId);
        Assert.Equal(5, detail.Items[0].RequestedQuantity);
    }

    [Fact]
    public async Task Create_InvalidSubStation_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (stationId, _, sparePartId, _) = await SeedAsync();

        var dto = BuildCreateDto(stationId, 99999, sparePartId);
        var response = await client.PostAsJsonAsync("/api/warehouse/dispatch", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_NoItems_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (stationId, subStationId, _, _) = await SeedAsync();

        var dto = new CreateDispatchRequestDto(
            SubStationId: subStationId,
            StationId: stationId,
            WorkPermitId: null,
            Purpose: null,
            Notes: null,
            Items: new List<CreateDispatchItemDto>());

        var response = await client.PostAsJsonAsync("/api/warehouse/dispatch", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─────────────── GET /api/warehouse/dispatch/{id} ───────────────

    [Fact]
    public async Task GetById_ReturnsDetail()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (stationId, subStationId, sparePartId, _) = await SeedAsync();

        var createDto = BuildCreateDto(stationId, subStationId, sparePartId);
        var createRes = await client.PostAsJsonAsync("/api/warehouse/dispatch", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<DispatchRequestDetailDto>();
        Assert.NotNull(created);

        var response = await client.GetAsync($"/api/warehouse/dispatch/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<DispatchRequestDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail.Id);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/warehouse/dispatch/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─────────────── GET /api/warehouse/dispatch/{id}/approve ───────────────

    [Fact]
    public async Task GetApprovalData_ReturnsItemsWithStock()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (stationId, subStationId, sparePartId, _) = await SeedAsync();

        var createDto = BuildCreateDto(stationId, subStationId, sparePartId);
        var createRes = await client.PostAsJsonAsync("/api/warehouse/dispatch", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<DispatchRequestDetailDto>();
        Assert.NotNull(created);

        var response = await client.GetAsync($"/api/warehouse/dispatch/{created.Id}/approve");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<DispatchApprovalItemDto>>();
        Assert.NotNull(items);
        Assert.Single(items);
        Assert.Equal(sparePartId, items[0].Id);
        Assert.Equal(5, items[0].RequestedQuantity);
        Assert.Equal(95, items[0].AvailableQty);
    }

    // ─────────────── POST /api/warehouse/dispatch/{id}/approve ───────────────

    [Fact]
    public async Task Approve_AllItems_UpdatesStatusToApproved()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (stationId, subStationId, sparePartId, _) = await SeedAsync();

        var createDto = BuildCreateDto(stationId, subStationId, sparePartId);
        var createRes = await client.PostAsJsonAsync("/api/warehouse/dispatch", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<DispatchRequestDetailDto>();
        Assert.NotNull(created);

        var approveDto = new ApproveDispatchRequestDto(
            Items: new List<ApproveDispatchItemDto>
            {
                new ApproveDispatchItemDto(created.Items[0].Id, 5, true)
            },
            Notes: null);

        var response = await client.PostAsJsonAsync($"/api/warehouse/dispatch/{created.Id}/approve", approveDto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<DispatchRequestDetailDto>();
        Assert.NotNull(updated);
        Assert.Equal("Approved", updated.StatusName);
        Assert.Equal(5, updated.Items[0].ApprovedQuantity);
    }

    // ─────────────── POST /api/warehouse/dispatch/{id}/reject ───────────────

    [Fact]
    public async Task Reject_PendingRequest_UpdatesStatusToRejected()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (stationId, subStationId, sparePartId, _) = await SeedAsync();

        var createDto = BuildCreateDto(stationId, subStationId, sparePartId);
        var createRes = await client.PostAsJsonAsync("/api/warehouse/dispatch", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<DispatchRequestDetailDto>();
        Assert.NotNull(created);

        var response = await client.PostAsJsonAsync($"/api/warehouse/dispatch/{created.Id}/reject", new { reason = "Not needed" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<DispatchRequestDetailDto>();
        Assert.NotNull(updated);
        Assert.Equal("Rejected", updated.StatusName);
    }

    // ─────────────── POST /api/warehouse/dispatch/{id}/issue ───────────────

    [Fact]
    public async Task Issue_ApprovedRequest_UpdatesStockAndStatus()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (stationId, subStationId, sparePartId, _) = await SeedAsync();

        // Create + approve first
        var createDto = BuildCreateDto(stationId, subStationId, sparePartId);
        var createRes = await client.PostAsJsonAsync("/api/warehouse/dispatch", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<DispatchRequestDetailDto>();
        Assert.NotNull(created);

        var approveDto = new ApproveDispatchRequestDto(
            Items: new List<ApproveDispatchItemDto>
            {
                new ApproveDispatchItemDto(created.Items[0].Id, 5, true)
            },
            Notes: null);
        await client.PostAsJsonAsync($"/api/warehouse/dispatch/{created.Id}/approve", approveDto);

        // Now issue
        var issueRes = await client.PostAsync($"/api/warehouse/dispatch/{created.Id}/issue", null);
        Assert.Equal(HttpStatusCode.OK, issueRes.StatusCode);
        var updated = await issueRes.Content.ReadFromJsonAsync<DispatchRequestDetailDto>();
        Assert.NotNull(updated);
        Assert.Equal("Issued", updated.StatusName);
        Assert.Equal(5, updated.Items[0].IssuedQuantity);
    }

    // ─────────────── PUT /api/warehouse/dispatch/{id} ───────────────

    [Fact]
    public async Task Update_PendingRequest_UpdatesNotes()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (stationId, subStationId, sparePartId, _) = await SeedAsync();

        var createDto = BuildCreateDto(stationId, subStationId, sparePartId);
        var createRes = await client.PostAsJsonAsync("/api/warehouse/dispatch", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<DispatchRequestDetailDto>();
        Assert.NotNull(created);

        var updateDto = new UpdateDispatchRequestDto(
            Id: created.Id,
            WorkPermitId: null,
            Purpose: "Updated purpose",
            Notes: "Updated notes");

        var response = await client.PutAsJsonAsync($"/api/warehouse/dispatch/{created.Id}", updateDto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<DispatchRequestDetailDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated purpose", updated.Purpose);
        Assert.Equal("Updated notes", updated.Notes);
    }

    [Fact]
    public async Task Update_IssuedRequest_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (stationId, subStationId, sparePartId, _) = await SeedAsync();

        var createDto = BuildCreateDto(stationId, subStationId, sparePartId);
        var createRes = await client.PostAsJsonAsync("/api/warehouse/dispatch", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<DispatchRequestDetailDto>();
        Assert.NotNull(created);

        // Approve then issue
        var approveDto = new ApproveDispatchRequestDto(
            Items: new List<ApproveDispatchItemDto>
            {
                new ApproveDispatchItemDto(created.Items[0].Id, 5, true)
            },
            Notes: null);
        await client.PostAsJsonAsync($"/api/warehouse/dispatch/{created.Id}/approve", approveDto);
        await client.PostAsync($"/api/warehouse/dispatch/{created.Id}/issue", null);

        var updateDto = new UpdateDispatchRequestDto(created.Id, null, "New purpose", "New notes");
        var response = await client.PutAsJsonAsync($"/api/warehouse/dispatch/{created.Id}", updateDto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─────────────── DELETE /api/warehouse/dispatch/{id} ───────────────

    [Fact]
    public async Task Delete_PendingRequest_ReturnsNoContent()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (stationId, subStationId, sparePartId, _) = await SeedAsync();

        var createDto = BuildCreateDto(stationId, subStationId, sparePartId);
        var createRes = await client.PostAsJsonAsync("/api/warehouse/dispatch", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<DispatchRequestDetailDto>();
        Assert.NotNull(created);

        var response = await client.DeleteAsync($"/api/warehouse/dispatch/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getRes = await client.GetAsync($"/api/warehouse/dispatch/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);
    }

    [Fact]
    public async Task Delete_IssuedRequest_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (stationId, subStationId, sparePartId, _) = await SeedAsync();

        var createDto = BuildCreateDto(stationId, subStationId, sparePartId);
        var createRes = await client.PostAsJsonAsync("/api/warehouse/dispatch", createDto);
        var created = await createRes.Content.ReadFromJsonAsync<DispatchRequestDetailDto>();
        Assert.NotNull(created);

        var approveDto = new ApproveDispatchRequestDto(
            Items: new List<ApproveDispatchItemDto>
            {
                new ApproveDispatchItemDto(created.Items[0].Id, 5, true)
            },
            Notes: null);
        await client.PostAsJsonAsync($"/api/warehouse/dispatch/{created.Id}/approve", approveDto);
        await client.PostAsync($"/api/warehouse/dispatch/{created.Id}/issue", null);

        var response = await client.DeleteAsync($"/api/warehouse/dispatch/{created.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─────────────── GET /api/warehouse/dispatch/by-substation/{id} ───────────────

    [Fact]
    public async Task GetBySubStation_ReturnsPagedResults()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (stationId, subStationId, sparePartId, _) = await SeedAsync();

        var createDto = BuildCreateDto(stationId, subStationId, sparePartId);
        await client.PostAsJsonAsync("/api/warehouse/dispatch", createDto);

        var response = await client.GetAsync($"/api/warehouse/dispatch/by-substation/{subStationId}?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<DispatchRequestListDto>>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
    }

    // ─────────────── GET /api/warehouse/dispatch/by-station/{id} ───────────────

    [Fact]
    public async Task GetByStation_ReturnsPagedResults()
    {
        var client = await CreateAuthenticatedClientAsync();
        var (stationId, subStationId, sparePartId, _) = await SeedAsync();

        var createDto = BuildCreateDto(stationId, subStationId, sparePartId);
        await client.PostAsJsonAsync("/api/warehouse/dispatch", createDto);

        var response = await client.GetAsync($"/api/warehouse/dispatch/by-station/{stationId}?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<DispatchRequestListDto>>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
    }
}
