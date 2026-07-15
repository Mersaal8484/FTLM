using FuelTrack.Application.DTOs.Intelligence;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class AnalyticsInsightService : IAnalyticsInsightService
{
    private readonly FuelTrackDbContext _db;

    public AnalyticsInsightService(FuelTrackDbContext db)
    {
        _db = db;
    }

    public async Task<List<AnalyticsInsightDto>> GetInsightsAsync(int? subStationId, int? stationId)
    {
        var query = _db.AnalyticsInsights.AsQueryable();

        if (subStationId.HasValue)
            query = query.Where(i => i.SubStationId == subStationId);
        if (stationId.HasValue)
            query = query.Where(i => i.StationId == stationId);

        return await query
            .OrderByDescending(i => i.GeneratedAt)
            .Select(i => new AnalyticsInsightDto(
                i.Id, i.SubStationId, i.StationId,
                i.Title, i.Description, i.InsightType,
                i.Severity, i.Category, i.DataPayload,
                i.GeneratedAt))
            .ToListAsync();
    }

    public async Task GenerateInsightsAsync(int? subStationId, int? stationId)
    {
        var insights = new List<AnalyticsInsight>();

        var generators = await GetFilteredGeneratorsAsync(subStationId, stationId);
        if (generators.Count == 0) return;

        var recentInsights = await _db.AnalyticsInsights
            .Where(i => i.GeneratedAt >= DateTime.UtcNow.AddDays(-1))
            .Select(i => new { i.SubStationId, i.InsightType })
            .ToListAsync();
        var recentInsightSet = recentInsights
            .Select(i => (i.SubStationId, i.InsightType))
            .ToHashSet();

        var generatorIds = generators.Keys.ToList();
        var generatorDict = generators;

        // 1. Anomaly detection: compare recent fuel level changes vs historical average
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        var recentReadings = await _db.DailyReadings
            .Where(r => generatorIds.Contains(r.GeneratorId) && r.ReadingDate >= sevenDaysAgo)
            .GroupBy(r => r.GeneratorId)
            .Select(g => new { GeneratorId = g.Key, AvgFuelPct = g.Average(r => r.CurrentEnergyKWh - r.PrevEnergyKWh) })
            .ToListAsync();

        var historicalReadings = await _db.DailyReadings
            .Where(r => generatorIds.Contains(r.GeneratorId) && r.ReadingDate >= thirtyDaysAgo && r.ReadingDate < sevenDaysAgo)
            .GroupBy(r => r.GeneratorId)
            .Select(g => new { GeneratorId = g.Key, AvgFuelPct = g.Average(r => r.CurrentEnergyKWh - r.PrevEnergyKWh) })
            .ToListAsync();

        foreach (var recent in recentReadings)
        {
            var hist = historicalReadings.FirstOrDefault(h => h.GeneratorId == recent.GeneratorId);
            if (hist == null) continue;

            var diffPct = Math.Abs(recent.AvgFuelPct - hist.AvgFuelPct);
            if (diffPct > 15)
            {
                if (!generatorDict.TryGetValue(recent.GeneratorId, out var gen)) continue;

                var direction = recent.AvgFuelPct > hist.AvgFuelPct ? "increase" : "decrease";
                if (recentInsightSet.Contains((gen.SubStationId, "Anomaly"))) continue;
                insights.Add(new AnalyticsInsight
                {
                    SubStationId = gen.SubStationId,
                    Title = $"Energy output anomaly detected on {gen.Name}",
                    Description = $"Average energy output {direction} by {diffPct:F1} compared to historical baseline. Generator: {gen.Name} ({gen.SerialNumber}).",
                    InsightType = "Anomaly",
                    Severity = diffPct > 30 ? "High" : "Medium",
                    Category = "EnergyOutput",
                    DataPayload = $"{{\"generatorId\":\"{gen.Id}\",\"recentAvg\":{recent.AvgFuelPct:F1},\"historicalAvg\":{hist.AvgFuelPct:F1},\"diffPct\":{diffPct:F1}}}",
                    GeneratedAt = DateTime.UtcNow
                });
            }
        }

        // 2. Maintenance prediction: generators approaching service interval
        var now = DateTime.UtcNow;
        var sixMonthsAgo = now.AddMonths(-6);

        var generatorRunHours = await _db.DailyReadings
            .Where(r => generatorIds.Contains(r.GeneratorId) && r.ReadingDate >= sixMonthsAgo)
            .GroupBy(r => r.GeneratorId)
            .Select(g => new { GeneratorId = g.Key, TotalHours = g.Sum(r => r.CurrentRunningHours - r.PrevRunningHours) })
            .ToListAsync();

        foreach (var genHours in generatorRunHours)
        {
            if (genHours.TotalHours > 250)
            {
                if (!generatorDict.TryGetValue(genHours.GeneratorId, out var gen)) continue;

                if (recentInsightSet.Contains((gen.SubStationId, "Prediction"))) continue;
                insights.Add(new AnalyticsInsight
                {
                    SubStationId = gen.SubStationId,
                    Title = $"Maintenance recommended for {gen.Name}",
                    Description = $"Generator {gen.Name} ({gen.SerialNumber}) has accumulated {genHours.TotalHours:F0} running hours in the last 6 months. Service interval approaching.",
                    InsightType = "Prediction",
                    Severity = genHours.TotalHours > 500 ? "High" : "Medium",
                    Category = "Maintenance",
                    DataPayload = $"{{\"generatorId\":\"{gen.Id}\",\"totalHours\":{genHours.TotalHours:F0}}}",
                    GeneratedAt = DateTime.UtcNow
                });
            }
        }

        // 3. Efficiency decline: energy per running hour trend
        var allReadingsWithEnergy = await _db.DailyReadings
            .Where(r => generatorIds.Contains(r.GeneratorId) && (r.CurrentRunningHours - r.PrevRunningHours) > 0)
            .OrderBy(r => r.GeneratorId)
            .ThenBy(r => r.ReadingDate)
            .Select(r => new { r.GeneratorId, r.ReadingDate, Efficiency = (r.CurrentEnergyKWh - r.PrevEnergyKWh) / (r.CurrentRunningHours - r.PrevRunningHours) })
            .ToListAsync();

        var readingsByGen = allReadingsWithEnergy
            .GroupBy(r => r.GeneratorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var kvp in readingsByGen)
        {
            var genId = kvp.Key;
            var readings = kvp.Value;
            if (readings.Count < 10) continue;

            var firstHalf = readings.Take(readings.Count / 2).Average(r => r.Efficiency);
            var secondHalf = readings.Skip(readings.Count / 2).Average(r => r.Efficiency);
            var declinePct = firstHalf > 0 ? ((firstHalf - secondHalf) / firstHalf) * 100 : 0;

            if (declinePct > 10)
            {
                if (!generatorDict.TryGetValue(genId, out var gen)) continue;

                if (recentInsightSet.Contains((gen.SubStationId, "Anomaly"))) continue;
                insights.Add(new AnalyticsInsight
                {
                    SubStationId = gen.SubStationId,
                    Title = $"Efficiency decline detected on {gen.Name}",
                    Description = $"Generator {gen.Name} ({gen.SerialNumber}) shows {declinePct:F1}% efficiency decline over recent readings. Energy output per running hour decreasing.",
                    InsightType = "Anomaly",
                    Severity = declinePct > 20 ? "High" : "Medium",
                    Category = "Efficiency",
                    DataPayload = $"{{\"generatorId\":\"{gen.Id}\",\"declinePct\":{declinePct:F1},\"firstHalfAvg\":{firstHalf:F1},\"secondHalfAvg\":{secondHalf:F1}}}",
                    GeneratedAt = DateTime.UtcNow
                });
            }
        }

        if (insights.Count > 0)
        {
            _db.AnalyticsInsights.AddRange(insights);
            await _db.SaveChangesAsync();
        }
    }

    public async Task AddFeedbackAsync(CreateInsightFeedbackDto dto, int userId)
    {
        var insight = await _db.AnalyticsInsights.FindAsync(dto.AnalyticsInsightId)
            ?? throw new KeyNotFoundException($"AnalyticsInsight {dto.AnalyticsInsightId} not found.");

        var feedback = new InsightFeedback
        {
            AnalyticsInsightId = dto.AnalyticsInsightId,
            IsHelpful = dto.IsHelpful,
            Comment = dto.Comment,
            UserId = userId,
            FeedbackAt = DateTime.UtcNow,
        };

        _db.InsightFeedbacks.Add(feedback);
        await _db.SaveChangesAsync();
    }

    private async Task<Dictionary<int, Generator>> GetFilteredGeneratorsAsync(int? subStationId, int? stationId)
    {
        var query = _db.Generators.AsQueryable();

        if (subStationId.HasValue)
            query = query.Where(g => g.SubStationId == subStationId.Value);
        if (stationId.HasValue)
            query = query.Where(g => g.SubStation.StationId == stationId.Value);

        return await query.ToDictionaryAsync(g => g.Id);
    }
}
