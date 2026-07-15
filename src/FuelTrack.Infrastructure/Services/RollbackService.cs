using System.Text.Json;
using FuelTrack.Application.DTOs.Rollback;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class RollbackService : IRollbackService
{
    private readonly FuelTrackDbContext _db;

    public RollbackService(FuelTrackDbContext db)
    {
        _db = db;
    }

    public async Task<List<RollbackListDto>> GetHistoryAsync()
    {
        return await _db.AuditLogs
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.Timestamp)
            .Join(_db.Users,
                  al => al.UserId,
                  u => u.Id,
                  (al, u) => new RollbackListDto(
                      al.Id,
                      al.UserId,
                      u.FullName,
                      al.Action,
                      al.EntityName,
                      al.EntityId.ToString(),
                      al.Timestamp
                  ))
            .ToListAsync();
    }

    public async Task<RollbackCheckResultDto> CheckRollbackAsync(int auditLogId)
    {
        var audit = await _db.AuditLogs
            .FirstOrDefaultAsync(x => x.Id == auditLogId && x.IsActive);

        if (audit is null)
            return new RollbackCheckResultDto(false, "Audit log not found", null, null, null);

        if (audit.Action == "Create")
            return new RollbackCheckResultDto(
                true, null, audit.EntityName, audit.EntityId.ToString(),
                $"Delete {audit.EntityName} {audit.EntityId}");

        if (string.IsNullOrEmpty(audit.EntityName))
            return new RollbackCheckResultDto(false, "Missing entity info for rollback", null, null, null);

        var entityExists = await EntityExistsAsync(audit.EntityName, audit.EntityId.ToString());

        if (!entityExists)
            return new RollbackCheckResultDto(
                false, "Entity no longer exists in database",
                audit.EntityName, audit.EntityId.ToString(), null);

        if (string.IsNullOrEmpty(audit.OldValues))
            return new RollbackCheckResultDto(
                false, "No old values to restore",
                audit.EntityName, audit.EntityId.ToString(), null);

        return new RollbackCheckResultDto(
            true, null, audit.EntityName, audit.EntityId.ToString(),
            $"Restore {audit.EntityName} {audit.EntityId} to previous values");
    }

    public async Task ExecuteRollbackAsync(int auditLogId, int userId)
    {
        var audit = await _db.AuditLogs
            .FirstOrDefaultAsync(x => x.Id == auditLogId && x.IsActive)
            ?? throw new KeyNotFoundException($"Audit log {auditLogId} not found");

        if (audit.Action == "Create")
        {
            await RollbackCreateAsync(audit);
        }
        else if (audit.Action == "Update" || audit.Action == "Edit")
        {
            await RollbackUpdateAsync(audit);
        }
        else if (audit.Action == "Delete" || audit.Action == "Remove")
        {
            throw new InvalidOperationException("Cannot rollback a delete operation automatically");
        }

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "Rollback",
            EntityName = audit.EntityName,
            EntityId = audit.EntityId,
            OldValues = audit.NewValues,
            NewValues = audit.OldValues,
            IpAddress = null,
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    private async Task RollbackCreateAsync(AuditLog audit)
    {
        var entity = await FindEntityAsync(audit.EntityName, audit.EntityId.ToString());
        if (entity is not null)
        {
            if (entity is BaseEntity baseEntity)
                baseEntity.IsActive = false;
            else
                _db.Remove(entity);
        }
    }

    private async Task RollbackUpdateAsync(AuditLog audit)
    {
        var entity = await FindEntityAsync(audit.EntityName, audit.EntityId.ToString())
            ?? throw new KeyNotFoundException($"Entity {audit.EntityName} with id {audit.EntityId} not found");

        if (string.IsNullOrEmpty(audit.OldValues))
            throw new InvalidOperationException("No old values to restore");

        var oldValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(audit.OldValues);
        if (oldValues is null)
            throw new InvalidOperationException("Failed to parse old values");

        foreach (var (key, value) in oldValues)
        {
            var prop = entity.GetType().GetProperty(key);
            if (prop is null || !prop.CanWrite) continue;

            if (value.ValueKind == JsonValueKind.Null)
            {
                prop.SetValue(entity, null);
            }
            else
            {
                var deserialized = JsonSerializer.Deserialize(value.GetRawText(), prop.PropertyType);
                prop.SetValue(entity, deserialized);
            }
        }
    }

    private async Task<bool> EntityExistsAsync(string entityName, string entityId)
    {
        var entity = await FindEntityAsync(entityName, entityId);
        if (entity is null) return false;

        if (entity is BaseEntity be && !be.IsActive) return false;
        return true;
    }

    private async Task<object?> FindEntityAsync(string entityName, string entityId)
    {
        if (!int.TryParse(entityId, out var parsed)) return null;

        return entityName switch
        {
            "Controller" => await _db.Controllers.FirstOrDefaultAsync(x => x.Id == parsed),
            "Generator" => await _db.Generators.FirstOrDefaultAsync(x => x.Id == parsed),
            "Meter" => await _db.Meters.FirstOrDefaultAsync(x => x.Id == parsed),
            "FuelTank" => await _db.FuelTanks.FirstOrDefaultAsync(x => x.Id == parsed),
            "Station" => await _db.Stations.FirstOrDefaultAsync(x => x.Id == parsed),
            "SubStation" => await _db.SubStations.FirstOrDefaultAsync(x => x.Id == parsed),
            "Lookup" => await _db.Lookups.FirstOrDefaultAsync(x => x.Id == parsed),
            _ => null
        };
    }
}
