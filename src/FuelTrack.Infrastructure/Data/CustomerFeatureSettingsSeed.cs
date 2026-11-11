using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Data;

/// <summary>
/// Safe startup seed for customer-scoped capabilities. The default keeps all
/// legacy workflows unchanged until a customer is deliberately moved.
/// </summary>
public static class CustomerFeatureSettingsSeed
{
    private static readonly (string Key, bool IsEnabled, string? Value)[] Defaults =
    [
        ("operational_data", false, CustomerFeatureService.LegacyModeValue),
        ("inventory", true, null),
        ("maintenance", true, null),
        ("solar", false, null),
    ];

    public static async Task SeedAsync(FuelTrackDbContext db, CancellationToken cancellationToken = default)
    {
        var keys = Defaults.Select(x => x.Key).ToList();
        var existing = await db.CustomerFeatureSettings
            .Where(x => keys.Contains(x.Key))
            .Select(x => x.Key)
            .ToListAsync(cancellationToken);

        foreach (var item in Defaults.Where(x => !existing.Contains(x.Key, StringComparer.OrdinalIgnoreCase)))
        {
            db.CustomerFeatureSettings.Add(new CustomerFeatureSetting
            {
                Key = item.Key,
                IsEnabled = item.IsEnabled,
                Value = item.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = 0,
                UpdatedBy = 0,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
