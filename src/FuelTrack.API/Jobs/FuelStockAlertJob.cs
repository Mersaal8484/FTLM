using FuelTrack.Application.Common;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.API.Jobs;

public class FuelStockAlertJob
{
    private readonly FuelTrackDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<FuelStockAlertJob> _logger;

    public FuelStockAlertJob(
        FuelTrackDbContext db,
        IEmailService emailService,
        ILogger<FuelStockAlertJob> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[JOB] Starting FuelStockAlertJob");

        var lowTanks = await _db.FuelTanks
            .Include(ft => ft.SubStation)
            .Include(ft => ft.FuelLookup)
            .Where(ft => ft.ActualStockLiters != null
                && ft.MinAlertLiters != null
                && ft.ActualStockLiters <= ft.MinAlertLiters)
            .ToListAsync(ct);

        if (lowTanks.Count == 0)
        {
            _logger.LogInformation("[JOB] No low fuel tanks detected");
            return;
        }

        var recipients = await _db.Users
            .Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email) && u.EmailConfirmed)
            .Select(u => new { u.Email, u.FullName })
            .ToListAsync(ct);

        var sent = 0;
        foreach (var tank in lowTanks)
        {
            var alertData = new FuelStockAlertEmailData
            {
                TankName = tank.Name,
                FuelType = tank.FuelLookup?.Name ?? "Unknown",
                CurrentLevelLiters = tank.ActualStockLiters ?? 0,
                CapacityLiters = tank.CapacityLiters
            };

            foreach (var user in recipients)
            {
                var ok = await _emailService.SendFuelStockAlertAsync(
                    user.Email!,
                    user.FullName,
                    alertData);
                if (ok) sent++;
            }
        }

        _logger.LogInformation("[JOB] FuelStockAlertJob completed — {Tanks} low tanks, {Sent} emails sent",
            lowTanks.Count, sent);
    }
}
