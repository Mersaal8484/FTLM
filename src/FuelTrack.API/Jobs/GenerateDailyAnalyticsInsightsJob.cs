using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.API.Jobs;

public class GenerateDailyAnalyticsInsightsJob
{
    private readonly FuelTrackDbContext _db;
    private readonly ILogger<GenerateDailyAnalyticsInsightsJob> _logger;

    public GenerateDailyAnalyticsInsightsJob(FuelTrackDbContext db, ILogger<GenerateDailyAnalyticsInsightsJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[JOB] Starting GenerateDailyAnalyticsInsights");

        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        var generators = await _db.Generators
            .Include(g => g.SubStation)
            .Where(g => g.SubStation != null)
            .ToListAsync(ct);

        var insightsCreated = 0;

        foreach (var generator in generators)
        {
            var readings = await _db.DailyReadings
                .Where(r => r.GeneratorId == generator.Id
                    && r.ReadingDate >= yesterday
                    && r.ReadingDate < today)
                .ToListAsync(ct);

            if (readings.Count == 0) continue;

            var avgEnergyKwh = readings.Average(r => (double)r.CurrentEnergyKWh);
            var avgRunningHours = readings.Average(r => (double)(r.CurrentRunningHours - r.PrevRunningHours));

            if (generator.ActualPowerKW > 0 && avgRunningHours > 0)
            {
                var expectedEnergy = generator.ActualPowerKW * avgRunningHours;
                var efficiency = expectedEnergy > 0 ? avgEnergyKwh / expectedEnergy * 100 : 0;

                if (efficiency < 70)
                {
                    _db.AnalyticsInsights.Add(new AnalyticsInsight
                    {
                        SubStationId = generator.SubStationId,
                        Title = $"Low efficiency detected: {generator.Name}",
                        Description = $"Generator {generator.Name} efficiency dropped to {efficiency:F1}% (expected ~100%). " +
                                      $"Average energy: {avgEnergyKwh:F1} kWh, running hours: {avgRunningHours:F1}h.",
                        InsightType = "EfficiencyAlert",
                        Severity = efficiency < 50 ? "High" : "Medium",
                        Category = "Performance",
                        GeneratedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = 0, // System sentinel (not tied to a user)
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = 0
                    });
                    insightsCreated++;
                }
            }

            var totalEnergy = readings.Sum(r => (double)r.CurrentEnergyKWh);
            if (totalEnergy == 0 && readings.Count >= 2)
            {
                _db.AnalyticsInsights.Add(new AnalyticsInsight
                {
                    SubStationId = generator.SubStationId,
                    Title = $"Zero energy output: {generator.Name}",
                    Description = $"Generator {generator.Name} produced 0 kWh over {readings.Count} readings yesterday.",
                    InsightType = "ZeroOutput",
                    Severity = "High",
                    Category = "Availability",
                    GeneratedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 0, // System sentinel (not tied to a user)
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = 0
                });
                insightsCreated++;
            }
        }

        if (insightsCreated > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("[JOB] Generated {Count} analytics insights", insightsCreated);
    }
}
