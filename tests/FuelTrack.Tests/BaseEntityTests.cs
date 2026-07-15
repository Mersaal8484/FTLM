using FuelTrack.Domain.Models;

namespace FuelTrack.Tests;

public class BaseEntityTests
{
    [Fact]
    public void BaseEntity_DefaultValues_AreCorrect()
    {
        var entity = new TestEntity();

        Assert.Equal(0, entity.Id);
        Assert.True(entity.IsActive);
        Assert.True(entity.CreatedAt <= DateTime.UtcNow);
        Assert.Equal(0, entity.CreatedBy);
        Assert.Equal(default, entity.UpdatedAt);
        Assert.Equal(0, entity.UpdatedBy);
    }

    [Fact]
    public void SubStation_DefaultCollection_NotNull()
    {
        var ss = new SubStation
        {
            Name = "Test SS",
            Code = "TSS-01",
            StationId = 1
        };

        Assert.NotNull(ss.Generators);
        Assert.NotNull(ss.FuelTanks);
    }

    [Fact]
    public void Generator_DefaultCollection_NotNull()
    {
        var gen = new Generator
        {
            Name = "Gen-1",
            SerialNumber = "SN-001",
            SubStationId = 1
        };

        Assert.NotNull(gen.DailyReadings);
    }

    [Fact]
    public void DailyReading_Defaults()
    {
        var dr = new DailyReading
        {
            GeneratorId = 1,
            ReadingDate = DateTime.UtcNow,
            PrevEnergyKWh = 0,
            CurrentEnergyKWh = 5000,
            PrevRunningHours = 0,
            CurrentRunningHours = 10.5,
            FuelLookupId = 1,
            StandardFuelRate = 10m,
            OilLookupId = 1,
            OilUsedLiters = 0,
            ReadingStatusLookupId = 1
        };

        Assert.True(dr.IsActive);
    }

    [Fact]
    public void User_DefaultCollection_NotNull()
    {
        var user = new User
        {
            UserName = "admin",
            PasswordHash = "hash",
            FullName = "Admin User",
            Email = "admin@fuel-track.com"
        };

        Assert.NotNull(user.Scopes);
        Assert.NotNull(user.Notifications);
    }
}

internal class TestEntity : BaseEntity
{
}
