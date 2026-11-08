using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;

namespace FuelTrack.Application.Interfaces;

public interface IStationOperationalSettingsService
{
    Task<StationOperationalSettingsDto?> GetAsync(int stationId, CancellationToken cancellationToken = default);
    Task<Result<StationOperationalSettingsDto>> UpsertAsync(
        int stationId,
        UpdateStationOperationalSettingsDto dto,
        int userId,
        CancellationToken cancellationToken = default);
}
