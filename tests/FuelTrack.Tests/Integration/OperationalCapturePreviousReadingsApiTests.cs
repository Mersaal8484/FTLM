using System.Net;
using System.Net.Http.Json;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FuelTrack.Tests.Integration;

public sealed class OperationalCapturePreviousReadingsApiTests : IntegrationTestBase
{
    [Fact]
    public async Task GetPreviousReadings_ReturnsControllerAndMeterInitialBaselinesOverHttp()
    {
        int siteId;
        int genId;
        int feederId;

        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
            var branch = new Branch { Name = "B-PR", Code = "BPR", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            db.Branches.Add(branch);
            await db.SaveChangesAsync();

            var station = new Station { BranchId = branch.Id, Name = "S-PR", Code = "SPR", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            db.Stations.Add(station);
            await db.SaveChangesAsync();

            var site = new SubStation { StationId = station.Id, Name = "Site-PR", Code = "SITEPR", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            db.SubStations.Add(site);
            await db.SaveChangesAsync();
            siteId = site.Id;

            var gen = new Generator
            {
                SubStationId = site.Id,
                Name = "PERKINS-01",
                SerialNumber = "PK-01",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Generators.Add(gen);
            await db.SaveChangesAsync();
            genId = gen.Id;

            db.Controllers.Add(new Domain.Models.Controller
            {
                GeneratorId = gen.Id,
                Name = "DSE-01",
                Code = "DSE-01",
                ControllerType = "DSE8610",
                EnergyReading = 5000,
                OperatingHours = 500,
                EnergyUnit = "kWh",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow,
            });

            var feederMeter = new Meter
            {
                SubStationId = site.Id,
                Name = "Feeder 1 Meter",
                EnergyReading = 400,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow,
            };
            db.Meters.Add(feederMeter);
            await db.SaveChangesAsync();

            var feeder = new Feeder
            {
                SubStationId = site.Id,
                Name = "Feeder 1",
                Code = "F1",
                MeterId = feederMeter.Id,
                MeterMultiplier = 2m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow,
            };
            db.Feeders.Add(feeder);
            await db.SaveChangesAsync();
            feederId = feeder.Id;
        }

        var client = await CreateAuthenticatedClientAsync();
        var response = await client.GetAsync($"/api/operational-captures/previous-readings?subStationId={siteId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PreviousMeterReadingsDto>();
        Assert.NotNull(payload);

        var genEnergy = Assert.Single(payload!.Readings, r => r.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced && r.SubjectId == genId);
        var genHours = Assert.Single(payload.Readings, r => r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours && r.SubjectId == genId);
        var feederEnergy = Assert.Single(payload.Readings, r => r.SubjectType == MeterReadingSubjectType.FeederEnergySent && r.SubjectId == feederId);

        Assert.Equal(5000m, genEnergy.LatestAny!.RawValue);
        Assert.Equal("Controller", genEnergy.LatestAny.Source);
        Assert.Equal(5000m, genEnergy.LatestOfficialDailyClosing!.RawValue);

        Assert.Equal(500m, genHours.LatestAny!.RawValue);
        Assert.Equal("Controller", genHours.LatestAny.Source);

        Assert.Equal(400m, feederEnergy.LatestAny!.RawValue);
        Assert.Equal(2m, feederEnergy.LatestAny.MultiplierSnapshot);
        Assert.Equal("MeterInitial", feederEnergy.LatestAny.Source);
    }
}
