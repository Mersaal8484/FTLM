using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FuelTrack.Tests.Services;

public sealed class PreviousReadingBaselineTests
{
    private static async Task<(SqliteConnection Connection, FuelTrackDbContext Db, int BranchId, int StationId, int SiteId, int OtherSiteId)> CreateSqliteDbAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<FuelTrackDbContext>().UseSqlite(connection).Options;
        var db = new FuelTrackDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var branch = new Branch { Name = "B1", Code = "B1", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var station = new Station { BranchId = branch.Id, Name = "S1", Code = "S1", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Stations.Add(station);
        await db.SaveChangesAsync();

        var site1 = new SubStation { StationId = station.Id, Name = "Site 1", Code = "ST-1", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var site2 = new SubStation { StationId = station.Id, Name = "Site 2", Code = "ST-2", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.SubStations.AddRange(site1, site2);
        await db.SaveChangesAsync();

        return (connection, db, branch.Id, station.Id, site1.Id, site2.Id);
    }

    private static OperationalCaptureService CreateService(
        FuelTrackDbContext db,
        int allowedSiteId,
        bool isCommercial = true,
        int branchId = 1,
        int stationId = 1)
    {
        var scope = new Mock<IUserScopeFilterService>();
        scope.Setup(s => s.HasAccessToSubStationAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((int _, int sid) => sid == allowedSiteId);

        var user = new Mock<ICurrentUserService>();
        user.SetupGet(u => u.UserId).Returns(1);
        user.SetupGet(u => u.HasAnyAdminRole).Returns(false);

        var commercial = new Mock<ICommercialStationModeService>();
        commercial.Setup(c => c.IsCommercialModeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(isCommercial);
        commercial.Setup(c => c.GetScopeAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(isCommercial ? new CommercialStationScope(branchId, stationId, allowedSiteId) : null);

        return new OperationalCaptureService(
            db,
            scope.Object,
            user.Object,
            new IdempotencyService(db),
            Mock.Of<IFuelLoadSafetyService>(),
            commercial.Object);
    }

    private static async Task<int> SeedSubmissionAsync(FuelTrackDbContext db, int siteId)
    {
        var user = new User
        {
            UserName = $"op_{Guid.NewGuid():N}",
            NormalizedUserName = $"OP_{Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@test.local",
            NormalizedEmail = $"{Guid.NewGuid():N}@TEST.LOCAL",
            FullName = "Op",
            SecurityStamp = Guid.NewGuid().ToString(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var sub = new OperationalSubmission
        {
            SubStationId = siteId,
            ClientRequestId = Guid.NewGuid(),
            SubmittedByUserId = user.Id,
            ReceivedAtUtc = DateTime.UtcNow,
            Status = OperationalReadingStatus.Recorded,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.OperationalSubmissions.Add(sub);
        await db.SaveChangesAsync();
        return sub.Id;
    }

    [Fact]
    public async Task GetPreviousReadings_ControllerBaseline_UsedForEnergyAndHours_WhenNoSubmittedReading()
    {
        var (conn, db, bId, sId, siteId, _) = await CreateSqliteDbAsync();
        await using (conn)
        await using (db)
        {
            var gen = new Generator { SubStationId = siteId, Name = "PERKINS-01", SerialNumber = "P1", IsActive = true };
            db.Generators.Add(gen);
            await db.SaveChangesAsync();

            var created = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            var updated = new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc);
            db.Controllers.Add(new Domain.Models.Controller
            {
                GeneratorId = gen.Id,
                Name = "DSE-1",
                Code = "C-1",
                ControllerType = "DSE8610",
                EnergyReading = 5000d,
                OperatingHours = 500d,
                EnergyUnit = "kWh",
                CreatedAt = created,
                UpdatedAt = updated,
            });
            await db.SaveChangesAsync();

            var res = await CreateService(db, siteId, true, bId, sId).GetPreviousReadingsAsync(0, 1);

            Assert.True(res.IsSuccess);
            var energy = Assert.Single(res.Value!.Readings, r => r.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced && r.SubjectId == gen.Id);
            var hours = Assert.Single(res.Value.Readings, r => r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours && r.SubjectId == gen.Id);

            Assert.NotNull(energy.LatestAny);
            Assert.NotNull(energy.LatestOfficialDailyClosing);
            Assert.Equal(5000m, energy.LatestAny!.RawValue);
            Assert.Equal(5000m, energy.LatestOfficialDailyClosing!.RawValue);
            Assert.Equal("Controller", energy.LatestAny.Source);
            Assert.Equal("Controller", energy.LatestOfficialDailyClosing.Source);
            Assert.Null(energy.RecentAverageDelta);

            Assert.Equal(500m, hours.LatestAny!.RawValue);
            Assert.Equal(500m, hours.LatestOfficialDailyClosing!.RawValue);
            Assert.Equal("Controller", hours.LatestAny.Source);
        }
    }

    [Fact]
    public async Task GetPreviousReadings_MeterInitial_UsedWhenControllerAbsent_ForGeneratorFeederAndSolar()
    {
        var (conn, db, bId, sId, siteId, _) = await CreateSqliteDbAsync();
        await using (conn)
        await using (db)
        {
            var solarCat = new LookupCategory { Name = "MeterType", DisplayName = "MeterType", IsActive = true };
            db.LookupCategories.Add(solarCat);
            await db.SaveChangesAsync();
            var solarType = new Lookup { CategoryId = solarCat.Id, Name = "Solar", Code = "SolarMeter", IsActive = true };
            db.Lookups.Add(solarType);
            await db.SaveChangesAsync();

            var gen = new Generator { SubStationId = siteId, Name = "G-M", SerialNumber = "GM1", IsActive = true };
            db.Generators.Add(gen);
            await db.SaveChangesAsync();

            var genMeter = new Meter
            {
                Name = "Gen Meter",
                SubStationId = siteId,
                GeneratorId = gen.Id,
                EnergyReading = 1200d,
                HoursReading = 150d,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            var feederMeter = new Meter
            {
                Name = "Feeder Meter",
                SubStationId = siteId,
                EnergyReading = 400d,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            var solarMeter = new Meter
            {
                Name = "Solar Meter",
                SubStationId = siteId,
                MeterTypeLookupId = solarType.Id,
                EnergyReading = 350d,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Meters.AddRange(genMeter, feederMeter, solarMeter);
            await db.SaveChangesAsync();

            var feeder = new Feeder
            {
                SubStationId = siteId,
                Code = "F-1",
                Name = "Feeder 1",
                MeterId = feederMeter.Id,
                MeterMultiplier = 2.5m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Feeders.Add(feeder);
            await db.SaveChangesAsync();

            var res = await CreateService(db, siteId, true, bId, sId).GetPreviousReadingsAsync(siteId, 1);

            Assert.True(res.IsSuccess);
            var gEnergy = Assert.Single(res.Value!.Readings, r => r.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced);
            var gHours = Assert.Single(res.Value.Readings, r => r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours);
            var fEnergy = Assert.Single(res.Value.Readings, r => r.SubjectType == MeterReadingSubjectType.FeederEnergySent);
            var sEnergy = Assert.Single(res.Value.Readings, r => r.SubjectType == MeterReadingSubjectType.SolarEnergyProduced);

            Assert.Equal(1200m, gEnergy.LatestAny!.RawValue);
            Assert.Equal("MeterInitial", gEnergy.LatestAny.Source);
            Assert.Equal(150m, gHours.LatestAny!.RawValue);
            Assert.Equal("MeterInitial", gHours.LatestAny.Source);

            Assert.Equal(400m, fEnergy.LatestAny!.RawValue);
            Assert.Equal(2.5m, fEnergy.LatestAny.MultiplierSnapshot);
            Assert.Equal("MeterInitial", fEnergy.LatestAny.Source);
            Assert.Equal(400m, fEnergy.LatestOfficialDailyClosing!.RawValue);

            Assert.Equal(350m, sEnergy.LatestAny!.RawValue);
            Assert.Equal("MeterInitial", sEnergy.LatestAny.Source);
        }
    }

    [Fact]
    public async Task GetPreviousReadings_SubmittedReadingWins_AndVoidedOrRejectedIgnored()
    {
        var (conn, db, bId, sId, siteId, _) = await CreateSqliteDbAsync();
        await using (conn)
        await using (db)
        {
            var g1 = new Generator { SubStationId = siteId, Name = "G1", SerialNumber = "G1", IsActive = true };
            var g2 = new Generator { SubStationId = siteId, Name = "G2", SerialNumber = "G2", IsActive = true };
            db.Generators.AddRange(g1, g2);
            await db.SaveChangesAsync();

            db.Controllers.AddRange(
                new Domain.Models.Controller { GeneratorId = g1.Id, Name = "C1", Code = "C1", ControllerType = "DSE", EnergyReading = 5000, OperatingHours = 500, EnergyUnit = "kWh", CreatedAt = DateTime.UtcNow.AddDays(-1), UpdatedAt = DateTime.UtcNow },
                new Domain.Models.Controller { GeneratorId = g2.Id, Name = "C2", Code = "C2", ControllerType = "DSE", EnergyReading = 7000, OperatingHours = 700, EnergyUnit = "kWh", CreatedAt = DateTime.UtcNow.AddDays(-1), UpdatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var subId = await SeedSubmissionAsync(db, siteId);
            db.MeterReadings.AddRange(
                new MeterReading
                {
                    OperationalSubmissionId = subId, ClientLineId = Guid.NewGuid(),
                    SubjectType = MeterReadingSubjectType.GeneratorEnergyProduced, SubjectId = g1.Id,
                    Kind = MeterReadingKind.DailyClosing, Status = OperationalReadingStatus.Approved,
                    RawValue = 5400m, MultiplierSnapshot = 1m,
                    CapturedAtUtc = DateTime.UtcNow.AddMinutes(-10), ReceivedAtUtc = DateTime.UtcNow, BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
                },
                new MeterReading
                {
                    OperationalSubmissionId = subId, ClientLineId = Guid.NewGuid(),
                    SubjectType = MeterReadingSubjectType.GeneratorEnergyProduced, SubjectId = g2.Id,
                    Kind = MeterReadingKind.Periodic, Status = OperationalReadingStatus.Voided,
                    RawValue = 9999m, MultiplierSnapshot = 1m,
                    CapturedAtUtc = DateTime.UtcNow.AddMinutes(-5), ReceivedAtUtc = DateTime.UtcNow, BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
                },
                new MeterReading
                {
                    OperationalSubmissionId = subId, ClientLineId = Guid.NewGuid(),
                    SubjectType = MeterReadingSubjectType.GeneratorRunningHours, SubjectId = g2.Id,
                    Kind = MeterReadingKind.Periodic, Status = OperationalReadingStatus.Rejected,
                    RawValue = 888m, MultiplierSnapshot = 1m,
                    CapturedAtUtc = DateTime.UtcNow.AddMinutes(-5), ReceivedAtUtc = DateTime.UtcNow, BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow),
                });
            await db.SaveChangesAsync();

            var res = await CreateService(db, siteId, true, bId, sId).GetPreviousReadingsAsync(siteId, 1);

            var g1Energy = Assert.Single(res.Value!.Readings, r => r.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced && r.SubjectId == g1.Id);
            Assert.Equal(5400m, g1Energy.LatestAny!.RawValue);
            Assert.Equal("Submitted", g1Energy.LatestAny.Source);

            var g2Energy = Assert.Single(res.Value.Readings, r => r.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced && r.SubjectId == g2.Id);
            var g2Hours = Assert.Single(res.Value.Readings, r => r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours && r.SubjectId == g2.Id);
            Assert.Equal(7000m, g2Energy.LatestAny!.RawValue);
            Assert.Equal("Controller", g2Energy.LatestAny.Source);
            Assert.Equal(700m, g2Hours.LatestAny!.RawValue);
            Assert.Equal("Controller", g2Hours.LatestAny.Source);
        }
    }

    [Fact]
    public async Task GetPreviousReadings_SkipsUninitializedZeroNonKwhSoftDeletedAndOtherSite()
    {
        var (conn, db, bId, sId, siteId, otherSiteId) = await CreateSqliteDbAsync();
        await using (conn)
        await using (db)
        {
            var gZeroUnupdated = new Generator { SubStationId = siteId, Name = "G-Zero", SerialNumber = "GZ", IsActive = true };
            var gNonKwh = new Generator { SubStationId = siteId, Name = "G-VAR", SerialNumber = "GV", IsActive = true };
            var gSoftDeleted = new Generator { SubStationId = siteId, Name = "G-Del", SerialNumber = "GD", IsActive = true };
            var gOtherSite = new Generator { SubStationId = otherSiteId, Name = "G-Other", SerialNumber = "GO", IsActive = true };
            db.Generators.AddRange(gZeroUnupdated, gNonKwh, gSoftDeleted, gOtherSite);
            await db.SaveChangesAsync();

            var sameTime = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc);
            db.Controllers.AddRange(
                // Uninitialized 0 with UpdatedAt == CreatedAt -> skipped
                new Domain.Models.Controller { GeneratorId = gZeroUnupdated.Id, Name = "CZ", Code = "CZ", ControllerType = "DSE", EnergyReading = 0, OperatingHours = 0, EnergyUnit = "kWh", CreatedAt = sameTime, UpdatedAt = sameTime },
                // Non-kWh unit -> energy skipped (do not guess), hours kept
                new Domain.Models.Controller { GeneratorId = gNonKwh.Id, Name = "CV", Code = "CV", ControllerType = "DSE", EnergyReading = 300, OperatingHours = 40, EnergyUnit = "VAR", CreatedAt = sameTime, UpdatedAt = sameTime.AddHours(1) },
                // Soft-deleted controller & meter -> ignored
                new Domain.Models.Controller { GeneratorId = gSoftDeleted.Id, Name = "CD", Code = "CD", ControllerType = "DSE", EnergyReading = 900, OperatingHours = 90, EnergyUnit = "kWh", CreatedAt = sameTime, UpdatedAt = sameTime.AddHours(1), DeletedAt = DateTime.UtcNow },
                // Other site's controller -> must not appear on site 1
                new Domain.Models.Controller { GeneratorId = gOtherSite.Id, Name = "CO", Code = "CO", ControllerType = "DSE", EnergyReading = 9999, OperatingHours = 999, EnergyUnit = "kWh", CreatedAt = sameTime, UpdatedAt = sameTime.AddHours(1) });
            db.Meters.Add(new Meter
            {
                Name = "Deleted Meter",
                SubStationId = siteId,
                GeneratorId = gSoftDeleted.Id,
                EnergyReading = 250,
                HoursReading = 25,
                IsActive = true,
                CreatedAt = sameTime,
                UpdatedAt = sameTime,
                DeletedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            // Verify both commercial mode and enterprise mode return the exact same baseline rules
            foreach (var commercialFlag in new[] { true, false })
            {
                var res = await CreateService(db, siteId, commercialFlag, bId, sId).GetPreviousReadingsAsync(siteId, 1);
                Assert.True(res.IsSuccess);

                var zeroE = Assert.Single(res.Value!.Readings, r => r.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced && r.SubjectId == gZeroUnupdated.Id);
                Assert.Null(zeroE.LatestAny);
                Assert.Null(zeroE.LatestOfficialDailyClosing);

                var varE = Assert.Single(res.Value.Readings, r => r.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced && r.SubjectId == gNonKwh.Id);
                var varH = Assert.Single(res.Value.Readings, r => r.SubjectType == MeterReadingSubjectType.GeneratorRunningHours && r.SubjectId == gNonKwh.Id);
                Assert.Null(varE.LatestAny);
                Assert.Equal(40m, varH.LatestAny!.RawValue);
                Assert.Equal("Controller", varH.LatestAny.Source);

                var delE = Assert.Single(res.Value.Readings, r => r.SubjectType == MeterReadingSubjectType.GeneratorEnergyProduced && r.SubjectId == gSoftDeleted.Id);
                Assert.Null(delE.LatestAny);

                Assert.DoesNotContain(res.Value.Readings, r => r.SubjectId == gOtherSite.Id);
            }
        }
    }
}
