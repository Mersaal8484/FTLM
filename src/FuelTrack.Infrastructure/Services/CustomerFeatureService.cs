using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public sealed class CustomerFeatureService(FuelTrackDbContext db) : ICustomerFeatureService
{
    public async Task<IReadOnlyList<CustomerFeatureSettingDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.CustomerFeatureSettings.AsNoTracking()
            .OrderBy(x => x.Key)
            .Select(x => new CustomerFeatureSettingDto(x.Key, x.IsEnabled, x.Value))
            .ToListAsync(cancellationToken);

    public async Task<Result<CustomerFeatureSettingDto>> UpdateAsync(string key, UpdateCustomerFeatureSettingDto dto, int userId, CancellationToken cancellationToken = default)
    {
        var feature = await db.CustomerFeatureSettings.SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (feature is null) return Result<CustomerFeatureSettingDto>.NotFound("Feature setting was not found.");

        feature.IsEnabled = dto.IsEnabled;
        feature.Value = dto.Value?.Trim();
        feature.UpdatedAt = DateTime.UtcNow;
        feature.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken);
        return Result<CustomerFeatureSettingDto>.Success(new(feature.Key, feature.IsEnabled, feature.Value));
    }
}
