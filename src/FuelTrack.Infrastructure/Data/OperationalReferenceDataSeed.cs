using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Data;

/// <summary>
/// Keeps the reference records used by the generic operational, inventory and
/// maintenance modules present without depending on fixed database identities.
/// Arabic is the display value; the stable English code is the secondary label.
/// </summary>
public static class OperationalReferenceDataSeed
{
    private sealed record Item(string ArabicName, string EnglishCode, int SortOrder);

    private static readonly IReadOnlyDictionary<string, Item[]> Items = new Dictionary<string, Item[]>
    {
        ["PartCategory"] =
        [
            new("ميكانيكي", "Mechanical", 1), new("كهربائي", "Electrical", 2),
            new("إلكتروني", "Electronic", 3), new("أجهزة قياس", "Instrumentation", 4),
            new("ورشة", "Workshop", 5), new("أخرى", "Other", 6),
        ],
        ["MaintenanceDepartment"] =
        [
            new("الكل", "All", 1), new("ميكانيكي", "Mechanical", 2),
            new("كهربائي", "Electrical", 3), new("أجهزة قياس", "Instrumentation", 4),
            new("ورشة", "Workshop", 5),
        ],
        ["ServiceItem"] =
        [
            new("طقم تغيير الزيت", "OilChangeSet", 1), new("فلتر الزيت", "OilFilter", 2),
            new("فلتر الهواء", "AirFilter", 3), new("البخاخات", "Nozzles", 4),
        ],
    };

    public static async Task SeedAsync(FuelTrackDbContext db, CancellationToken cancellationToken = default)
    {
        foreach (var (categoryName, values) in Items)
        {
            var category = await db.LookupCategories.SingleOrDefaultAsync(x => x.Name == categoryName, cancellationToken);
            if (category is null)
            {
                category = new LookupCategory
                {
                    Name = categoryName,
                    DisplayName = categoryName == "ServiceItem" ? "بنود الخدمة" : categoryName,
                    Description = categoryName == "ServiceItem" ? "Bilingual generator service items" : null,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = 0,
                    UpdatedBy = 0,
                };
                db.LookupCategories.Add(category);
                await db.SaveChangesAsync(cancellationToken);
            }

            var existingCodes = await db.Lookups
                .Where(x => x.CategoryId == category.Id)
                .Select(x => x.Code)
                .ToListAsync(cancellationToken);

            foreach (var item in values.Where(item => !existingCodes.Contains(item.EnglishCode, StringComparer.OrdinalIgnoreCase)))
            {
                db.Lookups.Add(new Lookup
                {
                    CategoryId = category.Id,
                    Name = item.ArabicName,
                    Code = item.EnglishCode,
                    SortOrder = item.SortOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = 0,
                    UpdatedBy = 0,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
