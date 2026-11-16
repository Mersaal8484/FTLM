using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;

namespace FuelTrack.Application.Interfaces;

public interface ICommercialStationModeService
{
    Task<bool> IsCommercialModeAsync(CancellationToken cancellationToken = default);
    Task<CommercialStationModeDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<CommercialStationActivationPreflightDto> GetActivationPreflightAsync(CancellationToken cancellationToken = default);
    Task<Result<CommercialStationModeDto>> ActivateAsync(ActivateCommercialStationModeDto dto, int userId, CancellationToken cancellationToken = default);
    Task<Result> DeactivateAsync(int userId, CancellationToken cancellationToken = default);
    Task<CommercialStationScope?> GetScopeAsync(CancellationToken cancellationToken = default);
}
