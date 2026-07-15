using System.Net;
using System.Net.Http.Json;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FuelTrack.Tests.Integration;

public class StationApiTests : IntegrationTestBase
{
    private async Task SeedStationDataAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();

        // Seed a Branch (Station has FK to Branch)
        if (!context.Branches.Any())
        {
            context.Branches.Add(new Branch
            {
                Name = "Test Branch",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = 1
            });
            await context.SaveChangesAsync();
        }

        // Seed a Station
        if (!context.Stations.Any())
        {
            var branch = await context.Branches.FirstAsync();
            context.Stations.Add(new Station
            {
                BranchId = branch.Id,
                Name = "Test Station Alpha",
                Code = "TSA-001",
                Notes = "Integration test station",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = 1
            });
            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithData()
    {
        await SeedStationDataAsync();
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/stations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stations = await response.Content.ReadFromJsonAsync<List<StationListDto>>();
        Assert.NotNull(stations);
        Assert.NotEmpty(stations);
        Assert.Contains(stations, s => s.Name == "Test Station Alpha");
    }

    [Fact]
    public async Task GetById_ExistingStation_ReturnsOkWithDetail()
    {
        await SeedStationDataAsync();
        var client = await CreateAuthenticatedClientAsync();

        // Get the station ID from the database
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var stationId = (await context.Stations.FirstAsync()).Id;

        var response = await client.GetAsync($"/api/stations/{stationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<StationDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal("Test Station Alpha", detail.Name);
        Assert.Equal("TSA-001", detail.Code);
    }

    [Fact]
    public async Task GetById_NonExistentId_ReturnsNotFound()
    {
        await SeedStationDataAsync();
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/stations/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_Unauthorized_Returns401()
    {
        await SeedStationDataAsync();
        var client = Factory.CreateClient(); // no auth header

        var response = await client.GetAsync("/api/stations");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // DTOs matching StationListDto / StationDetailDto from the API project
    private record StationListDto(int Id, int BranchId, string BranchName, string Name, string Code, bool IsActive);
    private record StationDetailDto(int Id, int BranchId, string BranchName, string Name, string Code, string? Notes, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);
}
