using FuelTrack.Application.Interfaces;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.API.MultiTenancy;

/// <summary>Rewraps a tenant's secrets after its platform key changes. Retain old keys until this succeeds.</summary>
public sealed class TenantTwoFactorKeyRotator(FuelTrackDbContext database, ITenantRequestContext tenant, ITwoFactorSecretProtector protector)
{
    public async Task<int> RewrapAsync(CancellationToken cancellationToken = default)
    {
        if (!database.Database.IsNpgsql() || tenant.TenantId is not Guid id || id == Guid.Empty)
            throw new InvalidOperationException("A trusted PostgreSQL tenant is required.");
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var users = await database.Users.FromSqlRaw("SELECT * FROM \"Users\" WHERE \"TwoFactorSecret\" IS NOT NULL FOR UPDATE")
            .ToListAsync(cancellationToken);
        foreach (var user in users)
        {
            var secret = await protector.UnprotectAsync(user.Id, user.TwoFactorSecret!, cancellationToken);
            user.TwoFactorSecret = await protector.ProtectAsync(user.Id, secret, cancellationToken);
            user.ConcurrencyStamp = Guid.NewGuid().ToString();
        }
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return users.Count;
    }
}
