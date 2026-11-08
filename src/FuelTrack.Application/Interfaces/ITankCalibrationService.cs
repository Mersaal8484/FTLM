using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;

namespace FuelTrack.Application.Interfaces;

public interface ITankCalibrationService
{
    Task<Result<TankCalibrationDto>> UpsertAsync(int fuelTankId, UpdateTankCalibrationDto dto, int userId, CancellationToken cancellationToken = default);
    TankCalibrationPreviewDto Preview(UpdateTankCalibrationDto dto);
}
