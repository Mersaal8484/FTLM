using FuelTrack.Application.DTOs.Intelligence;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Tests.Services;

public class AnalyticsInsightServiceTests : IDisposable
{
    private readonly AnalyticsInsightService _service;
    private readonly FuelTrackDbContext _db;
    private const int _genId = 1;
    private const int _subStationId = 1;

    public AnalyticsInsightServiceTests()
    {
        var o = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"AnalyticsTestDb_{Guid.NewGuid()}")
            .Options;
        _db = new FuelTrackDbContext(o);

        _service = new AnalyticsInsightService(_db);

        _db.SubStations.Add(new SubStation { Id = _subStationId, Name = "SS1", Code = "SS1", StationId = 1 });
        _db.Generators.Add(new Generator { Id = _genId, Name = "Gen1", SerialNumber = "G1", SubStationId = _subStationId });
        _db.SaveChanges();
    }

    [Fact]
    public async Task GenerateInsightsAsync_NoReadings_NoInsightCreated()
    {
        await _service.GenerateInsightsAsync(null, null);

        var count = await _db.AnalyticsInsights.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GenerateInsightsAsync_AnomalyDetected_CreatesInsight()
    {
        var now = DateTime.UtcNow;

        // historical readings (stable energy output)
        for (int i = 29; i >= 8; i--)
        {
            _db.DailyReadings.Add(new DailyReading
            {
                GeneratorId = _genId, ReadingDate = now.AddDays(-i),
                PrevEnergyKWh = 100, CurrentEnergyKWh = 100
            });
        }
        // recent readings (sharp energy increase)
        for (int i = 6; i >= 0; i--)
        {
            _db.DailyReadings.Add(new DailyReading
            {
                GeneratorId = _genId, ReadingDate = now.AddDays(-i),
                PrevEnergyKWh = 100, CurrentEnergyKWh = 155
            });
        }
        await _db.SaveChangesAsync();

        await _service.GenerateInsightsAsync(_subStationId, null);

        var insights = await _db.AnalyticsInsights.ToListAsync();
        Assert.Contains(insights, i => i.Title.Contains("anomaly"));
    }

    [Fact]
    public async Task GenerateInsightsAsync_MaintenancePredicted_CreatesInsight()
    {
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);

        for (int i = 0; i < 30; i++)
        {
            _db.DailyReadings.Add(new DailyReading
            {
                GeneratorId = _genId, ReadingDate = sixMonthsAgo.AddDays(i * 6),
                PrevRunningHours = 0, CurrentRunningHours = 10
            });
        }
        await _db.SaveChangesAsync();

        await _service.GenerateInsightsAsync(_subStationId, null);

        var insights = await _db.AnalyticsInsights.ToListAsync();
        Assert.Contains(insights, i => i.Category == "Maintenance");
    }

    [Fact]
    public async Task GenerateInsightsAsync_EfficiencyDeclineDetected_CreatesInsight()
    {
        var now = DateTime.UtcNow;

        // 20 readings: first 10 high efficiency, last 10 low efficiency
        for (int i = 0; i < 20; i++)
        {
            var energyDelta = i < 10 ? 20.0 : 10.0;
            _db.DailyReadings.Add(new DailyReading
            {
                GeneratorId = _genId, ReadingDate = now.AddDays(-20 + i),
                PrevEnergyKWh = 0, CurrentEnergyKWh = energyDelta,
                PrevRunningHours = 0, CurrentRunningHours = 2
            });
        }
        await _db.SaveChangesAsync();

        await _service.GenerateInsightsAsync(_subStationId, null);

        var insights = await _db.AnalyticsInsights.ToListAsync();
        Assert.Contains(insights, i => i.Category == "Efficiency");
    }

    [Fact]
    public async Task GenerateInsightsAsync_ScopedBySubStation_FiltersCorrectly()
    {
        const int otherGenId = 2;
        _db.Generators.Add(new Generator
            { Id = otherGenId, Name = "Gen2", SerialNumber = "G2", SubStationId = 2 });
        _db.DailyReadings.Add(new DailyReading
        {
            GeneratorId = otherGenId, ReadingDate = DateTime.UtcNow.AddDays(-5),
            PrevEnergyKWh = 100, CurrentEnergyKWh = 200
        });
        _db.DailyReadings.Add(new DailyReading
        {
            GeneratorId = otherGenId, ReadingDate = DateTime.UtcNow.AddDays(-25),
            PrevEnergyKWh = 100, CurrentEnergyKWh = 100
        });
        await _db.SaveChangesAsync();

        await _service.GenerateInsightsAsync(_subStationId, null);

        var count = await _db.AnalyticsInsights.CountAsync();
        Assert.Equal(0, count);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
