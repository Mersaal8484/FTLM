using FuelTrack.Application.Common;
using FuelTrack.Application.Interfaces;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.API.Jobs;

public class WeeklyReportJob
{
    private readonly FuelTrackDbContext _db;
    private readonly IReportService _reportService;
    private readonly IEmailService _emailService;
    private readonly ILogger<WeeklyReportJob> _logger;

    public WeeklyReportJob(
        FuelTrackDbContext db,
        IReportService reportService,
        IEmailService emailService,
        ILogger<WeeklyReportJob> logger)
    {
        _db = db;
        _reportService = reportService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[JOB] Starting WeeklyReportJob");

        try
        {
            var dateTo = DateTime.UtcNow;
            var dateFrom = dateTo.AddDays(-7);

            var stats = await _reportService.GetDashboardStatsAsync(null, null, null, dateFrom, dateTo);
            var efficiency = await _reportService.GetEfficiencyReportAsync(null, null, null, dateFrom, dateTo, 1, 1000);
            var fuelTx = await _reportService.GetFuelTransactionsReportAsync(null, null, null, dateFrom, dateTo, 1, 1000);

            var totalFuelConsumed = fuelTx.Summary.TotalConsumptionLiters;
            var totalEnergyProduced = efficiency.Items.Any() ? efficiency.Items.Sum(i => i.TotalEnergyProducedKWh) : 0;
            var avgEff = efficiency.Summary.AverageEfficiencyPercent;

            var reportData = new WeeklyReportEmailData
            {
                ReportPeriod = $"{dateFrom:yyyy-MM-dd} — {dateTo:yyyy-MM-dd}",
                TotalBranches = stats.System.TotalBranches,
                TotalStations = stats.System.TotalStations,
                TotalSubStations = stats.System.TotalSubStations,
                TotalGenerators = stats.System.TotalGenerators,
                TotalFuelTanks = stats.System.TotalFuelTanks,
                TotalFuelConsumedLiters = totalFuelConsumed,
                TotalEnergyProducedKWh = totalEnergyProduced,
                OverallEfficiencyPercent = Math.Round(avgEff, 1)
            };

            var recipients = await _db.Users
                .Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email) && u.EmailConfirmed)
                .Select(u => new { u.Email, u.FullName })
                .ToListAsync(ct);

            var sent = 0;
            foreach (var user in recipients)
            {
                var ok = await _emailService.SendWeeklyReportAsync(
                    user.Email!,
                    user.FullName,
                    reportData);
                if (ok) sent++;
            }

            _logger.LogInformation("[JOB] WeeklyReportJob completed — sent {Sent}/{Total} emails", sent, recipients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[JOB] WeeklyReportJob failed");
            throw;
        }
    }
}
