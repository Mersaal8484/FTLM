using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;

namespace FuelTrack.Application.Interfaces;

public interface ICustomerFeatureService
{
    Task<IReadOnlyList<CustomerFeatureSettingDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<CustomerFeatureSettingDto>> UpdateAsync(string key, UpdateCustomerFeatureSettingDto dto, int userId, CancellationToken cancellationToken = default);
}
