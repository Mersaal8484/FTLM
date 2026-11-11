using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public sealed class CustomerFeatureService(FuelTrackDbContext db) : ICustomerFeatureService
{
    public const string LegacyModeValue = "legacy";
    public const string NewModeValue = "new";

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

    public async Task<bool> IsOperationalDataNewModeAsync(CancellationToken cancellationToken = default)
    {
        var setting = await db.CustomerFeatureSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == "operational_data", cancellationToken);

        // New operational behavior is opt-in. Any missing, null, or unrecognised
        // value is deliberately treated as legacy so a feature cannot become live
        // merely because an old setting was incomplete.
        return setting is { IsEnabled: true } &&
               string.Equals(setting.Value, NewModeValue, StringComparison.OrdinalIgnoreCase);
    }
}
