using System.Net;
using System.Net.Http.Json;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FuelTrack.Tests.Integration;

public class SubStationReadingApiTests : IntegrationTestBase
{
    private async Task<(int stationId, int subStationId, int meterId, int otherSubStationId, int otherMeterId)> SeedAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();

        if (!context.Branches.Any())
        {
            context.Branches.Add(new Branch { Name = "Test Branch", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 });
            await context.SaveChangesAsync();
        }

        var branch = await context.Branches.FirstAsync();

        if (!context.Stations.Any())
        {
            context.Stations.Add(new Station { BranchId = branch.Id, Name = "Test Station", Code = "TS-1", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 });
            await context.SaveChangesAsync();
        }
        var stationId = (await context.Stations.FirstAsync()).Id;

        int subStationId;
        if (!await context.SubStations.AnyAsync(s => s.Name == "Site A"))
        {
            var ss = new SubStation { StationId = stationId, Name = "Site A", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.SubStations.Add(ss);
            await context.SaveChangesAsync();
        }
        subStationId = (await context.SubStations.FirstAsync(s => s.Name == "Site A")).Id;

        int otherSubStationId;
        if (!await context.SubStations.AnyAsync(s => s.Name == "Site B"))
        {
            var ss = new SubStation { StationId = stationId, Name = "Site B", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.SubStations.Add(ss);
            await context.SaveChangesAsync();
        }
        otherSubStationId = (await context.SubStations.FirstAsync(s => s.Name == "Site B")).Id;

        // Reading status lookup (required FK on SubStationReading).
        var statusCat = await context.LookupCategories.FirstOrDefaultAsync(c => c.Name == "ReadingStatus");
        if (statusCat == null)
        {
            statusCat = new LookupCategory { Name = "ReadingStatus", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
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

        // Grid (exported-energy) meter for Site A with an initial energy reading.
        int meterId;
        var meter = await context.Meters.FirstOrDefaultAsync(m => m.SubStationId == subStationId && m.MeterTypeLookupId == 17);
        if (meter == null)
        {
            meter = new Meter { SubStationId = subStationId, Name = "Grid Meter A", MeterTypeLookupId = 17, EnergyReading = 500, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.Meters.Add(meter);
            await context.SaveChangesAsync();
        }
        meterId = meter.Id;

        // Grid meter for Site B (no reading recorded yet -> previous should fall back to meter).
        int otherMeterId;
        var otherMeter = await context.Meters.FirstOrDefaultAsync(m => m.SubStationId == otherSubStationId && m.MeterTypeLookupId == 17);
        if (otherMeter == null)
        {
            otherMeter = new Meter { SubStationId = otherSubStationId, Name = "Grid Meter B", MeterTypeLookupId = 17, EnergyReading = 777, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = 1, UpdatedAt = DateTime.UtcNow, UpdatedBy = 1 };
            context.Meters.Add(otherMeter);
            await context.SaveChangesAsync();
        }
        otherMeterId = otherMeter.Id;

        // A PREVIOUS reading recorded for Site A (the value the form must display as "previous").
        if (!await context.SubStationReadings.AnyAsync(r => r.SubStationId == subStationId))
        {
            context.SubStationReadings.Add(new SubStationReading
            {
                SubStationId = subStationId,
                MeterId = meterId,
                ReadingDate = DateTime.UtcNow.AddDays(-1),
                PrevExportedEnergyKWh = 0,
                CurrentExportedEnergyKWh = 200,
                InternalConsumption = 10,
                ReadingStatusLookupId = approved.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                CreatedBy = 1,
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedBy = 1
            });
            await context.SaveChangesAsync();
        }

        return (stationId, subStationId, meterId, otherSubStationId, otherMeterId);
    }

    private record SubStationReadingDto(
        int Id, int SubStationId, string? SubStationName, int? MeterId, string? MeterName,
        DateTime ReadingDate, double PrevExportedEnergyKWh, double CurrentExportedEnergyKWh,
        double? InternalConsumption, int ReadingStatusLookupId, string? ReadingStatusLookupName,
        string? RejectionReason, string? Notes, DateTime CreatedAt, int CreatedBy);

    [Fact]
    public async Task GetLast_ReturnsPreviousSiteReading_CurrentExportedEnergyKWh()
    {
        var (stationId, subStationId, _, _, _) = await SeedAsync();
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/substation-readings/last/{stationId}?subStation={subStationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reading = await response.Content.ReadFromJsonAsync<SubStationReadingDto>();
        Assert.NotNull(reading);
        Assert.Equal(subStationId, reading!.SubStationId);
        // This is exactly the value the form shows as the "Previous Sent Energy Reading".
        Assert.Equal(200, reading.CurrentExportedEnergyKWh);
        Assert.Equal(10, reading.InternalConsumption);
    }

    [Fact]
    public async Task Create_ThenGetLast_ReturnsNewReadingAsPrevious()
    {
        var (stationId, subStationId, meterId, _, _) = await SeedAsync();
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/substation-readings", new
        {
            subStationId,
            meterId,
            readingDate = DateTime.UtcNow.ToString("o"),
            prevExportedEnergyKWh = 200,
            currentExportedEnergyKWh = 350,
            internalConsumption = 12.5,
            readingStatusLookupId = (int?)null,
            notes = "test"
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<SubStationReadingDto>();
        Assert.NotNull(created);

        var approveResponse = await client.PostAsync($"/api/substation-readings/{created!.Id}/approve", null);
        Assert.Equal(HttpStatusCode.NoContent, approveResponse.StatusCode);

        var last = await client.GetAsync($"/api/substation-readings/last/{stationId}?subStation={subStationId}");
        var reading = await last.Content.ReadFromJsonAsync<SubStationReadingDto>();
        Assert.NotNull(reading);
        Assert.Equal(350, reading!.CurrentExportedEnergyKWh);
        Assert.Equal(12.5, reading.InternalConsumption);
    }

    [Fact]
    public async Task GetLast_NoReadingYet_ReturnsNull_SoFormFallsBackToMeterEnergyReading()
    {
        var (stationId, _, _, otherSubStationId, otherMeterId) = await SeedAsync();
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/substation-readings/last/{stationId}?subStation={otherSubStationId}");

        // No previous reading recorded -> API returns 204 No Content (empty body).
        // The form then falls back to the exported-energy meter's initial reading.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(string.IsNullOrWhiteSpace(content));

        // The form's previous value then comes from the exported-energy meter (METERS table).
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var meterEnergy = (await context.Meters.FindAsync(otherMeterId))!.EnergyReading;
        Assert.Equal(777, meterEnergy);
    }
}
