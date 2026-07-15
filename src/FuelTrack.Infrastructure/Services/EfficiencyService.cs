using FuelTrack.Application.DTOs.Efficiency;
using FuelTrack.Application.Interfaces;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class EfficiencyService : IEfficiencyService
{
    private readonly FuelTrackDbContext _db;

    public EfficiencyService(FuelTrackDbContext db)
    {
        _db = db;
    }

    public async Task<EfficiencyReportDto> GetEfficiencyReportAsync(int generatorId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        var generator = await _db.Generators
            .Where(g => g.Id == generatorId)
            .Select(g => new { g.Name })
            .FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException($"Generator {generatorId} not found");

        var readings = await _db.DailyReadings
            .AsNoTracking()
            .Where(r => r.GeneratorId == generatorId && r.ReadingDate >= startDate && r.ReadingDate <= endDate)
            .OrderBy(r => r.ReadingDate)
            .ToListAsync(ct);

        if (readings.Count == 0)
            return new EfficiencyReportDto(generatorId, generator.Name, startDate, endDate, 0, 0, 0, 0, 0, 0);

        var totalEnergy = (double)readings.Sum(r => r.CurrentEnergyKWh - r.PrevEnergyKWh);
        var totalFuel = (double)readings.Sum(r =>
            (decimal)r.StandardFuelRate * (decimal)(r.CurrentRunningHours - r.PrevRunningHours));

        var totalRunningHours = (double)readings.Sum(r => r.CurrentRunningHours - r.PrevRunningHours);
        var avgEfficiency = totalFuel > 0 ? totalEnergy / totalFuel * 100 : 0;
        var avgLoad = totalRunningHours > 0 ? totalEnergy / totalRunningHours : 0;

        return new EfficiencyReportDto(
            generatorId, generator.Name, startDate, endDate,
            totalEnergy, totalFuel, totalRunningHours,
            Math.Round(avgEfficiency, 2),
            Math.Round(avgLoad, 2),
            readings.Count);
    }

    public async Task<EfficiencyTrendDto> GetEfficiencyTrendAsync(int generatorId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        var generator = await _db.Generators
            .Where(g => g.Id == generatorId)
            .Select(g => new { g.Name })
            .FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException($"Generator {generatorId} not found");

        var readings = await _db.DailyReadings
            .AsNoTracking()
            .Where(r => r.GeneratorId == generatorId && r.ReadingDate >= startDate && r.ReadingDate <= endDate)
            .OrderBy(r => r.ReadingDate)
            .ToListAsync(ct);

        var points = readings
            .Where(r => r.CurrentEnergyKWh > r.PrevEnergyKWh)
            .Select(r =>
            {
                var energy = (double)(r.CurrentEnergyKWh - r.PrevEnergyKWh);
                var fuel = (double)r.StandardFuelRate * (double)(r.CurrentRunningHours - r.PrevRunningHours);
                var hours = (double)(r.CurrentRunningHours - r.PrevRunningHours);
                var eff = fuel > 0 ? energy / fuel * 100 : 0;
                return new EfficiencyDataPoint(r.ReadingDate, energy, fuel, Math.Round(eff, 2), hours);
            })
            .ToList();

        return new EfficiencyTrendDto(generatorId, generator.Name, points);
    }
}
