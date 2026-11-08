using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;

namespace FuelTrack.Application.Interfaces;

public interface IFeederService
{
    Task<Result<FeederDto>> CreateAsync(CreateFeederDto dto, int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeederDto>> GetBySiteAsync(int subStationId, int userId, CancellationToken cancellationToken = default);
}
