using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace FuelTrack.Infrastructure.Data;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var userId = GetUserId();
        var now = DateTime.UtcNow;

        var addedEntries = context.ChangeTracker.Entries()
            .Where(e => (e.Entity is BaseEntity || e.Entity is User || e.Entity is Role)
                && e.Entity is not AuditLog && e.State == EntityState.Added)
            .ToList();

        foreach (var entry in addedEntries)
        {
            if (entry.Entity is BaseEntity be)
            {
                be.CreatedAt = now;
                be.CreatedBy = userId;
                be.UpdatedAt = now;
                be.UpdatedBy = userId;
            }
            else if (entry.Entity is User u)
            {
                u.CreatedAt = now;
                u.CreatedBy = userId;
                u.UpdatedAt = now;
                u.UpdatedBy = userId;
            }
            else if (entry.Entity is Role r)
            {
                r.CreatedAt = now;
                r.CreatedBy = userId;
                r.UpdatedAt = now;
                r.UpdatedBy = userId;
            }
        }

        var auditEntries = context.ChangeTracker.Entries()
            .Where(e => (e.Entity is BaseEntity || e.Entity is User || e.Entity is Role)
                && e.Entity is not AuditLog
                && e.State is EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (auditEntries.Count == 0)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in auditEntries)
        {
            int entityId = entry.Entity switch
            {
                BaseEntity be => be.Id,
                User u => u.Id,
                Role r => r.Id,
                _ => 0
            };

            switch (entry.State)
            {

                case EntityState.Modified:
                    if (entry.Entity is BaseEntity beMod)
                    {
                        beMod.UpdatedAt = now;
                        beMod.UpdatedBy = userId;
                    }
                    else if (entry.Entity is User uMod)
                    {
                        uMod.UpdatedAt = now;
                        uMod.UpdatedBy = userId;
                    }
                    else if (entry.Entity is Role rMod)
                    {
                        rMod.UpdatedAt = now;
                        rMod.UpdatedBy = userId;
                    }

                    context.Add(new AuditLog
                    {
                        UserId = userId,
                        Action = "Modified",
                        EntityName = entry.Entity.GetType().Name,
                        EntityId = entityId,
                        OldValues = JsonSerializer.Serialize(
                            entry.OriginalValues.Properties.ToDictionary(p => p.Name, p => entry.OriginalValues[p])),
                        NewValues = JsonSerializer.Serialize(
                            entry.CurrentValues.Properties.ToDictionary(p => p.Name, p => entry.CurrentValues[p])),
                        IpAddress = GetIpAddress(),
                        Timestamp = now
                    });
                    break;

                case EntityState.Deleted:
                {
                    entry.State = EntityState.Modified;
                    if (entry.Entity is BaseEntity beDel)
                    {
                        beDel.DeletedAt = now;
                        beDel.UpdatedAt = now;
                        beDel.UpdatedBy = userId;
                    }
                    else if (entry.Entity is User uDel)
                    {
                        uDel.DeletedAt = now;
                        uDel.UpdatedAt = now;
                        uDel.UpdatedBy = userId;
                    }
                    else if (entry.Entity is Role rDel)
                    {
                        rDel.DeletedAt = now;
                        rDel.UpdatedAt = now;
                        rDel.UpdatedBy = userId;
                    }

                    context.Add(new AuditLog
                    {
                        UserId = userId,
                        Action = "Deleted",
                        EntityName = entry.Entity.GetType().Name,
                        EntityId = entityId,
                        OldValues = JsonSerializer.Serialize(
                            entry.OriginalValues.Properties.ToDictionary(p => p.Name, p => entry.OriginalValues[p])),
                        NewValues = null,
                        IpAddress = GetIpAddress(),
                        Timestamp = now
                    });
                    break;
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private int GetUserId()
    {
        var id = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var result) ? result : 0;
    }

    private string? GetIpAddress()
    {
        return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}
