using System.Net;
using System.Net.Http.Json;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FuelTrack.Tests.Integration;

public class GeneratorsApiTests : IntegrationTestBase
{
    private async Task SeedGeneratorGraphAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();

        if (!context.Branches.Any())
        {
            context.Branches.Add(new Branch
            {
                Name = "Gen Test Branch",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = 1
            });
            await context.SaveChangesAsync();
        }

        if (!context.Stations.Any())
        {
            var branch = await context.Branches.FirstAsync();
            context.Stations.Add(new Station
            {
                BranchId = branch.Id,
                Name = "Gen Test Station",
                Code = "GTS-001",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = 1
            });
            await context.SaveChangesAsync();
        }

        if (!context.SubStations.Any())
        {
            var station = await context.Stations.FirstAsync();
            context.SubStations.Add(new SubStation
            {
                StationId = station.Id,
                Name = "Gen Test SubStation",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = 1
            });
            await context.SaveChangesAsync();
        }

        if (!context.LookupCategories.Any(c => c.Name == "GeneratorStatus"))
        {
            context.LookupCategories.Add(new LookupCategory
            {
                Name = "GeneratorStatus",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = 1
            });
            await context.SaveChangesAsync();
        }

        if (!context.Generators.Any())
        {
            var subStation = await context.SubStations.FirstAsync();
            var category = await context.LookupCategories.FirstAsync(c => c.Name == "GeneratorStatus");
            var status = new Lookup
            {
                CategoryId = category.Id,
                Name = "شغال", // -> "Running" via GeneratorService mapping
                SortOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = 1
            };
            context.Lookups.Add(status);
            await context.SaveChangesAsync();

            context.Generators.Add(new Generator
            {
                SubStationId = subStation.Id,
                Name = "Gen-Alpha-01",
                SerialNumber = "SN-GEN-001",
                NominalPower = 500,
                ActualPowerKW = 480,
                FuelConsumptionRatePerHour = 12.5m,
                StatusLookupId = status.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 1,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = 1
            });
            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task GetMy_Seeded_ReturnsOkWithPageShape()
    {
        await SeedGeneratorGraphAsync();
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/generators/my");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var generators = await response.Content.ReadFromJsonAsync<List<MyGeneratorListDto>>();
        Assert.NotNull(generators);
        Assert.NotEmpty(generators);

        var gen = generators!.First(g => g.Name == "Gen-Alpha-01");
        // Fields consumed by frontend MyGenerators.jsx
        Assert.Equal("Gen Test SubStation", gen.SubStationName);
        Assert.NotNull(gen.StationId);
        Assert.Equal("Gen Test Station", gen.StationName);
        Assert.Equal("Running", gen.Status); // شغال -> Running mapping
        Assert.Equal(480, gen.ActualPowerKW);
        Assert.Null(gen.LastPowerRecord); // no DailyReading seeded
        Assert.NotNull(gen.AssignedOilTypes); // returns [] (empty list) when no Oils seeded
        Assert.Empty(gen.AssignedOilTypes);
    }

    [Fact]
    public async Task GetMy_OperatorWithoutSystemOwner_IsAllowedByMiddleware()
    {
        await SeedGeneratorGraphAsync();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var user = await context.Users.FirstOrDefaultAsync(u => u.UserName == TestUserName);
        Assert.NotNull(user);

        // Operator token WITHOUT SystemOwner/Admin bypass
        var token = GenerateJwtToken(user!.Id, user.UserName!, "Operator", includeSystemOwner: false);
        var client = CreateClientWithToken(token);

        var response = await client.GetAsync("/api/generators/my");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMy_Unauthenticated_ReturnsUnauthorized()
    {
        await SeedGeneratorGraphAsync();
        var client = Factory.CreateClient(); // no auth header

        var response = await client.GetAsync("/api/generators/my");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveWithControllers_ReturnsOnlyActiveGeneratorsWithControllers()
    {
        await SeedGeneratorGraphAsync();
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/generators/active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var generators = await response.Content.ReadFromJsonAsync<List<ActiveGeneratorDto>>();
        Assert.NotNull(generators);
        // All returned generators must be active and have a linked controller
        foreach (var gen in generators!)
        {
            Assert.True(gen.IsActive);
            Assert.NotNull(gen.ControllerName);
        }
    }

    [Fact]
    public async Task GetActiveWithControllers_OperatorWithoutSystemOwner_IsAllowedByMiddleware()
    {
        await SeedGeneratorGraphAsync();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var user = await context.Users.FirstOrDefaultAsync(u => u.UserName == TestUserName);
        Assert.NotNull(user);

        var token = GenerateJwtToken(user!.Id, user.UserName!, "Operator", includeSystemOwner: false);
        var client = CreateClientWithToken(token);

        var response = await client.GetAsync("/api/generators/active");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveWithControllers_Unauthenticated_ReturnsUnauthorized()
    {
        await SeedGeneratorGraphAsync();
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/generators/active");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Mirrors FuelTrack.Application.DTOs.Generators.ActiveGeneratorDto (camelCase JSON)
    private record ActiveGeneratorDto(
        int Id,
        int SubStationId,
        string SubStationName,
        string Name,
        string SerialNumber,
        string? Model,
        string? Manufacturer,
        double NominalPower,
        int ActualPowerKW,
        string? GenType,
        string? Voltage,
        string? Frequency,
        string? Status,
        bool IsActive,
        string? ControllerName,
        string? ControllerCode,
        string? ControllerType,
        double ControllerEnergyReading,
        double ControllerOperatingHours,
        string? Location);

    // Mirrors FuelTrack.Application.DTOs.Generators.My.MyGeneratorListDto (camelCase JSON)
    private record MyGeneratorListDto(
        int Id,
        int SubStationId,
        string SubStationName,
        int? StationId,
        string? StationName,
        string Name,
        string SerialNumber,
        double NominalPower,
        int ActualPowerKW,
        decimal FuelConsumptionRatePerHour,
        string? GenType,
        string Status,
        double? LastPowerRecord,
        double? LastHoursRecord,
        List<AssignedOilTypeDto>? AssignedOilTypes,
        int? FuelLookupId,
        decimal? StandardFuelRate,
        int? OilLookupId);

    private record AssignedOilTypeDto(int OilLookupId, string Name, decimal CurrentStock, decimal MinStock);
}
