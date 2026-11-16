using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

/// <summary>
/// Owns the tenant-level commercial-mode transition. The Branch/Station/Site
/// records it creates are technical implementation details, never commercial
/// navigation data. No user scope rows are created or removed here.
/// </summary>
public sealed class CommercialStationModeService(FuelTrackDbContext db) : ICommercialStationModeService
{
    public const string FeatureKey = "commercial_station_mode";
    public const string CommercialModeValue = "commercial";
    public const string EnterpriseModeValue = "enterprise";

    public async Task<bool> IsCommercialModeAsync(CancellationToken cancellationToken = default)
    {
        var setting = await db.CustomerFeatureSettings.AsNoTracking()
            .Where(x => x.Key == FeatureKey)
            .Select(x => new { x.IsEnabled, x.Value })
            .SingleOrDefaultAsync(cancellationToken);

        return setting is { IsEnabled: true } &&
               string.Equals(setting.Value, CommercialModeValue, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<CommercialStationModeDto> GetStatusAsync(CancellationToken cancellationToken = default) =>
        new(await IsCommercialModeAsync(cancellationToken));

    public async Task<CommercialStationActivationPreflightDto> GetActivationPreflightAsync(CancellationToken cancellationToken = default)
    {
        var counts = await GetActiveCountsAsync(cancellationToken);
        if (counts.Branches == 0 && counts.Stations == 0 && counts.SubStations == 0)
            return new(true, null, counts.Branches, counts.Stations, counts.SubStations);

        if (counts.Branches != 1 || counts.Stations != 1 || counts.SubStations != 1)
        {
            return new(false,
                "Commercial mode requires either an empty hierarchy or exactly one active Branch, Station, and Site. Review the tenant hierarchy before activation.",
                counts.Branches, counts.Stations, counts.SubStations);
        }

        var scope = await GetActiveScopeAsync(cancellationToken);
        return scope is null
            ? new(false, "The active technical hierarchy is not linked as a single Branch → Station → Site chain.", counts.Branches, counts.Stations, counts.SubStations)
            : new(true, null, counts.Branches, counts.Stations, counts.SubStations);
    }

    public async Task<Result<CommercialStationModeDto>> ActivateAsync(ActivateCommercialStationModeDto dto, int userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.StationName) || dto.StationName.Trim().Length > 200)
            return Result<CommercialStationModeDto>.Failure("A station name of up to 200 characters is required.");
        if (string.IsNullOrWhiteSpace(dto.StationCode) || dto.StationCode.Trim().Length > 100)
            return Result<CommercialStationModeDto>.Failure("A station code of up to 100 characters is required.");

        if (await IsCommercialModeAsync(cancellationToken))
            return Result<CommercialStationModeDto>.Success(new(true));

        var preflight = await GetActivationPreflightAsync(cancellationToken);
        if (!preflight.CanActivate)
            return Result<CommercialStationModeDto>.Conflict(preflight.BlockingReason!);

        var now = DateTime.UtcNow;
        var scope = await GetActiveScopeAsync(cancellationToken);
        if (scope is null)
        {
            var stationName = dto.StationName.Trim();
            var stationCode = dto.StationCode.Trim();
            var branchCode = $"TECH-{stationCode}";

            var duplicateCode = await db.Branches.AnyAsync(x => x.Code == branchCode && x.DeletedAt == null, cancellationToken) ||
                                await db.Stations.AnyAsync(x => x.Code == stationCode && x.DeletedAt == null, cancellationToken) ||
                                await db.SubStations.AnyAsync(x => x.Code == stationCode && x.DeletedAt == null, cancellationToken);
            if (duplicateCode)
                return Result<CommercialStationModeDto>.Conflict("The proposed technical station code is already in use.");

            var branch = new Branch
            {
                Name = $"Technical branch for {stationName}",
                Code = branchCode,
                IsGeneration = true,
                Notes = "Hidden commercial-mode technical hierarchy.",
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = userId,
                UpdatedBy = userId,
            };
            db.Branches.Add(branch);

            var station = new Station
            {
                Branch = branch,
                Name = stationName,
                Code = stationCode,
                Notes = "Hidden commercial-mode technical station.",
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = userId,
                UpdatedBy = userId,
            };
            db.Stations.Add(station);

            db.SubStations.Add(new SubStation
            {
                Station = station,
                Name = stationName,
                Code = stationCode,
                Location = null,
                CapacityKW = null,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = userId,
                UpdatedBy = userId,
            });
        }

        var feature = await db.CustomerFeatureSettings.SingleOrDefaultAsync(x => x.Key == FeatureKey, cancellationToken);
        if (feature is null)
        {
            feature = new CustomerFeatureSetting
            {
                Key = FeatureKey,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = userId,
                UpdatedBy = userId,
            };
            db.CustomerFeatureSettings.Add(feature);
        }

        feature.IsEnabled = true;
        feature.Value = CommercialModeValue;
        feature.UpdatedAt = now;
        feature.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken);
        return Result<CommercialStationModeDto>.Success(new(true));
    }

    public async Task<Result> DeactivateAsync(int userId, CancellationToken cancellationToken = default)
    {
        var feature = await db.CustomerFeatureSettings.SingleOrDefaultAsync(x => x.Key == FeatureKey, cancellationToken);
        if (feature is null)
            return Result.Success();

        feature.IsEnabled = false;
        feature.Value = EnterpriseModeValue;
        feature.UpdatedAt = DateTime.UtcNow;
        feature.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<CommercialStationScope?> GetScopeAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsCommercialModeAsync(cancellationToken))
            return null;
        return await GetActiveScopeAsync(cancellationToken);
    }

    private async Task<(int Branches, int Stations, int SubStations)> GetActiveCountsAsync(CancellationToken cancellationToken) =>
        (await db.Branches.CountAsync(x => x.DeletedAt == null && x.IsActive, cancellationToken),
         await db.Stations.CountAsync(x => x.DeletedAt == null && x.IsActive, cancellationToken),
         await db.SubStations.CountAsync(x => x.DeletedAt == null && x.IsActive, cancellationToken));

    private async Task<CommercialStationScope?> GetActiveScopeAsync(CancellationToken cancellationToken)
    {
        var scopes = await db.SubStations.AsNoTracking()
            .Where(site => site.DeletedAt == null && site.IsActive &&
                           site.Station.DeletedAt == null && site.Station.IsActive &&
                           site.Station.Branch.DeletedAt == null && site.Station.Branch.IsActive)
            .Select(site => new CommercialStationScope(site.Station.BranchId, site.StationId, site.Id))
            .Take(2)
            .ToListAsync(cancellationToken);

        return scopes.Count == 1 ? scopes[0] : null;
    }
}
