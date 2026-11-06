using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public sealed class StationOperationalSettingsService(
    FuelTrackDbContext db,
    IUserScopeFilterService scopeFilter,
    ICurrentUserService currentUser) : IStationOperationalSettingsService
{
    public async Task<StationOperationalSettingsDto?> GetAsync(int stationId, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessStationAsync(stationId, cancellationToken)) return null;

        var settings = await db.StationOperationalSettings.AsNoTracking()
            .Where(x => x.StationId == stationId)
            .Select(x => ToDto(x))
            .SingleOrDefaultAsync(cancellationToken);

        return settings ?? Defaults(stationId);
    }

    public async Task<Result<StationOperationalSettingsDto>> UpsertAsync(
        int stationId,
        UpdateStationOperationalSettingsDto dto,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (!await CanAccessStationAsync(stationId, cancellationToken))
            return Result<StationOperationalSettingsDto>.NotFound("Station not found or outside your scope.");

        if (!IsTimeZoneValid(dto.TimeZone))
            return Result<StationOperationalSettingsDto>.Failure("Time zone must be a valid IANA or platform time-zone identifier.");
        if (dto.ProducedVsSentWarningThresholdPercent is < 0 or > 100)
            return Result<StationOperationalSettingsDto>.Failure("Warning threshold must be between 0 and 100.");
        if (dto.OilPressureUnit is not ("bar" or "psi" or "kPa"))
            return Result<StationOperationalSettingsDto>.Failure("Oil pressure unit must be bar, psi, or kPa.");
        if (dto.ReadingPhotoMaxBytes != StationOperationalSettings.DefaultReadingPhotoMaxBytes)
            return Result<StationOperationalSettingsDto>.Failure("The approved reading-photo limit is 65 KB.");

        var settings = await db.StationOperationalSettings.SingleOrDefaultAsync(x => x.StationId == stationId, cancellationToken);
        if (settings is null)
        {
            settings = new StationOperationalSettings
            {
                StationId = stationId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
            };
            db.StationOperationalSettings.Add(settings);
        }

        settings.TimeZone = dto.TimeZone.Trim();
        settings.ProducedVsSentWarningThresholdPercent = dto.ProducedVsSentWarningThresholdPercent;
        settings.OilPressureUnit = dto.OilPressureUnit;
        settings.IsReadingPhotoRequired = dto.IsReadingPhotoRequired;
        settings.ReadingPhotoMaxBytes = dto.ReadingPhotoMaxBytes;
        settings.RequiresDifferentReviewer = dto.RequiresDifferentReviewer;
        settings.UpdatedAt = DateTime.UtcNow;
        settings.UpdatedBy = userId;

        await db.SaveChangesAsync(cancellationToken);
        return Result<StationOperationalSettingsDto>.Success(ToDto(settings));
    }

    private async Task<bool> CanAccessStationAsync(int stationId, CancellationToken cancellationToken)
    {
        if (currentUser.HasAnyAdminRole) return await db.Stations.AnyAsync(s => s.Id == stationId, cancellationToken);
        if (currentUser.UserId is not int userId) return false;
        var stationIds = await scopeFilter.GetAccessibleStationIdsAsync(userId);
        return stationIds.Contains(stationId);
    }

    private static bool IsTimeZoneValid(string value)
    {
        try { _ = TimeZoneInfo.FindSystemTimeZoneById(value.Trim()); return true; }
        catch (TimeZoneNotFoundException) { return false; }
        catch (InvalidTimeZoneException) { return false; }
    }

    private static StationOperationalSettingsDto Defaults(int stationId) => new(
        stationId,
        StationOperationalSettings.DefaultTimeZone,
        10m,
        StationOperationalSettings.DefaultOilPressureUnit,
        true,
        StationOperationalSettings.DefaultReadingPhotoMaxBytes,
        true);

    private static StationOperationalSettingsDto ToDto(StationOperationalSettings entity) => new(
        entity.StationId,
        entity.TimeZone,
        entity.ProducedVsSentWarningThresholdPercent,
        entity.OilPressureUnit,
        entity.IsReadingPhotoRequired,
        entity.ReadingPhotoMaxBytes,
        entity.RequiresDifferentReviewer);
}
