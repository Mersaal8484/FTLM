using FuelTrack.Application.Common;
using FuelTrack.Application.Interfaces;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.API.Jobs;

public class FuelStockAlertJob
{
    private readonly FuelTrackDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly IAlertGeneratorService? _alertGenerator;
    private readonly ILogger<FuelStockAlertJob> _logger;

    public FuelStockAlertJob(
        FuelTrackDbContext db,
        IEmailService emailService,
        IUserScopeFilterService scopeFilter,
        ILogger<FuelStockAlertJob> logger,
        IAlertGeneratorService? alertGenerator = null)
    {
        _db = db;
        _emailService = emailService;
        _scopeFilter = scopeFilter;
        _logger = logger;
        _alertGenerator = alertGenerator;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[JOB] Starting FuelStockAlertJob");
        var inAppAlerts = _alertGenerator == null ? 0 : await _alertGenerator.CheckFuelStockLevelsAsync(ct);

        var lowTanks = await _db.FuelTanks
            .Include(ft => ft.SubStation)
            .Include(ft => ft.FuelLookup)
            .Where(ft => ft.ActualStockLiters != null
                && ft.MinAlertLiters != null
                && (Math.Max(0.0, ft.ActualStockLiters.Value - (ft.DeadStockLiters ?? 0.0)) <= ft.MinAlertLiters.Value))
            .ToListAsync(ct);

        if (lowTanks.Count == 0)
        {
            _logger.LogInformation("[JOB] No low fuel tanks detected");
            return;
        }

        var allRecipients = await _db.Users
            .Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email) && u.EmailConfirmed)
            .Select(u => new { u.Id, u.Email, u.FullName })
            .ToListAsync(ct);

        var sent = 0;
        foreach (var tank in lowTanks)
        {
            var authorizedUserIds = await _scopeFilter
                .GetUserIdsWithAccessToSubStationAsync(tank.SubStationId);

            var scopedRecipients = allRecipients
                .Where(u => authorizedUserIds.Contains(u.Id))
                .ToList();

            if (scopedRecipients.Count == 0)
            {
                _logger.LogInformation("[JOB] No authorized recipients for low tank {TankName} at {SubStation}",
                    tank.Name, tank.SubStation?.Name);
                continue;
            }

            var alertData = new FuelStockAlertEmailData
            {
                TankName = tank.Name,
                FuelType = tank.FuelLookup?.Name ?? "Unknown",
                CurrentLevelLiters = tank.ActualStockLiters ?? 0,
                CapacityLiters = tank.CapacityLiters
            };

            foreach (var user in scopedRecipients)
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
        _logger.LogInformation("[JOB] FuelStockAlertJob created {AlertCount} in-app alerts", inAppAlerts);
    }
}
