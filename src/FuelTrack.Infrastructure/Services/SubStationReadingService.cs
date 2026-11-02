using FuelTrack.Application.DTOs.SubStationReadings;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class SubStationReadingService : ISubStationReadingService
{
    private readonly FuelTrackDbContext _db;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly ICurrentUserService _currentUser;

    public SubStationReadingService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, ICurrentUserService currentUser)
    {
        _db = db;
        _scopeFilter = scopeFilter;
        _currentUser = currentUser;
    }

    private async Task<int?> GetExportEnergyMeterTypeIdAsync()
    {
        return await _db.Lookups
            .Where(l => l.Category != null && l.Category.Name == "MeterType" &&
                        (l.Code == "ExportEnergy" || l.Name == "عداد طاقة"))
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<int> GetLookupIdAsync(string categoryName, string code)
    {
        var categoryId = await _db.LookupCategories
            .Where(c => c.Name == categoryName)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();

        if (categoryId == 0)
            return 0;

        return await _db.Lookups
            .Where(l => l.CategoryId == categoryId && l.Code == code)
            .Select(l => l.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<SubStationReadingDto> CreateAsync(CreateSubStationReadingDto dto)
    {
        if (dto.SubStationId is null or <= 0)
            throw new ArgumentException("Site number (SubStationId) is required.");

        var subStationId = dto.SubStationId.Value;
        var subStation = await _db.SubStations.FindAsync(subStationId)
            ?? throw new KeyNotFoundException($"SubStation {subStationId} not found.");

        int? meterId = dto.MeterId;
        if (meterId is null || meterId == 0)
        {
            var exportEnergyTypeId = await GetExportEnergyMeterTypeIdAsync();
            if (exportEnergyTypeId.HasValue)
            {
                meterId = await _db.Meters
                    .Where(m => m.SubStationId == dto.SubStationId && m.MeterTypeLookupId == exportEnergyTypeId)
                    .Select(m => (int?)m.Id)
                    .FirstOrDefaultAsync();
            }
        }

        if (meterId is null || meterId == 0)
            throw new InvalidOperationException("Cannot create reading: site is not linked to an active meter. Link a meter to the site first.");

        var meter = await _db.Meters
            .FirstOrDefaultAsync(m => m.Id == meterId.Value);
        if (meter is null || meter.SubStationId != subStationId || meter.IsActive != true)
            throw new InvalidOperationException("Cannot create reading: the linked meter does not belong to this site or is inactive.");

        int? statusId = dto.ReadingStatusLookupId;
        if (statusId is null || statusId == 0)
        {
            statusId = await GetLookupIdAsync("ReadingStatus", "Pending");

            if (statusId is null || statusId == 0)
            {
                statusId = await GetLookupIdAsync("ReadingStatus", "Draft");
                if (statusId is null || statusId == 0)
                    statusId = await GetLookupIdAsync("ReadingStatus", "Submitted");
            }
        }

        if (statusId is null || statusId == 0)
            throw new InvalidOperationException("No 'ReadingStatus' lookup found. Seed the ReadingStatus lookup category.");

        var readingDateUtc = dto.ReadingDate.Kind == DateTimeKind.Utc
            ? dto.ReadingDate
            : DateTime.SpecifyKind(dto.ReadingDate, DateTimeKind.Utc);
        var readingDay = readingDateUtc.Date;
        var duplicateExists = await _db.SubStationReadings
            .AnyAsync(r => r.SubStationId == subStationId &&
                           r.ReadingDate.Date == readingDay);
        if (duplicateExists)
            throw new InvalidOperationException(
                "A reading for this site on the selected date already exists. Edit the existing reading instead of creating a new one.");

        var submitterId = _currentUser.UserId ?? 0;

        var reading = new SubStationReading
        {
            SubStationId = subStationId,
            MeterId = meterId,
            ReadingDate = dto.ReadingDate,
            PrevExportedEnergyKWh = dto.PrevExportedEnergyKWh,
            CurrentExportedEnergyKWh = dto.CurrentExportedEnergyKWh,
            InternalConsumption = dto.InternalConsumption,
            ReadingStatusLookupId = statusId.Value,
            RejectionReason = dto.RejectionReason,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = submitterId,
            UpdatedBy = submitterId,
        };

        _db.SubStationReadings.Add(reading);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            if (ex.InnerException?.Message.Contains("IX_SubStationReadings_SubStationId_ReadingDate",
                    StringComparison.OrdinalIgnoreCase) == true)
            {
                throw new InvalidOperationException(
                    "A reading for this site on the selected date already exists. Edit the existing reading instead of creating a new one.");
            }
            throw;
        }

        var meterName = meterId.HasValue
            ? await _db.Meters.Where(m => m.Id == meterId.Value).Select(m => m.Name).FirstOrDefaultAsync()
            : null;
        var statusName = await _db.Lookups.Where(l => l.Id == reading.ReadingStatusLookupId).Select(l => l.Name).FirstOrDefaultAsync();

        return new SubStationReadingDto(
            reading.Id, reading.SubStationId, subStation.Name,
            reading.MeterId, meterName, reading.ReadingDate,
            reading.PrevExportedEnergyKWh, reading.CurrentExportedEnergyKWh, reading.InternalConsumption,
            reading.ReadingStatusLookupId, statusName, reading.RejectionReason, reading.Notes,
            reading.CreatedAt, reading.CreatedBy);
    }

    public async Task<SubStationReadingDto> UpdateAsync(int id, CreateSubStationReadingDto dto)
    {
        var reading = await _db.SubStationReadings.FindAsync(id)
            ?? throw new KeyNotFoundException($"SubStationReading {id} not found.");

        int? meterId = dto.MeterId;
        if (meterId is null || meterId == 0)
        {
            var exportEnergyTypeId = await GetExportEnergyMeterTypeIdAsync();
            if (exportEnergyTypeId.HasValue)
            {
                meterId = await _db.Meters
                    .Where(m => m.SubStationId == reading.SubStationId && m.MeterTypeLookupId == exportEnergyTypeId)
                    .Select(m => (int?)m.Id)
                    .FirstOrDefaultAsync();
            }
        }

        int? statusId = dto.ReadingStatusLookupId;
        if (statusId is null || statusId == 0)
        {
            statusId = await GetLookupIdAsync("ReadingStatus", "Pending");

            if (statusId is null || statusId == 0)
            {
                statusId = await GetLookupIdAsync("ReadingStatus", "Draft");
                if (statusId is null || statusId == 0)
                    statusId = await GetLookupIdAsync("ReadingStatus", "Submitted");
            }
        }

        if (statusId is null || statusId == 0)
            throw new InvalidOperationException("No 'ReadingStatus' lookup found. Seed the ReadingStatus lookup category.");

        reading.MeterId = meterId;
        reading.ReadingDate = dto.ReadingDate;
        reading.PrevExportedEnergyKWh = dto.PrevExportedEnergyKWh;
        reading.CurrentExportedEnergyKWh = dto.CurrentExportedEnergyKWh;
        reading.InternalConsumption = dto.InternalConsumption;
        reading.ReadingStatusLookupId = statusId.Value;
        reading.RejectionReason = dto.RejectionReason;
        reading.Notes = dto.Notes;
        reading.UpdatedAt = DateTime.UtcNow;
        reading.UpdatedBy = _currentUser.UserId ?? 0;

        await _db.SaveChangesAsync();

        var subStation = await _db.SubStations.FindAsync(reading.SubStationId);
        var meterName = reading.MeterId.HasValue
            ? await _db.Meters.Where(m => m.Id == reading.MeterId.Value).Select(m => m.Name).FirstOrDefaultAsync()
            : null;
        var statusName = await _db.Lookups.Where(l => l.Id == reading.ReadingStatusLookupId).Select(l => l.Name).FirstOrDefaultAsync();

        return new SubStationReadingDto(
            reading.Id, reading.SubStationId, subStation?.Name,
            reading.MeterId, meterName, reading.ReadingDate,
            reading.PrevExportedEnergyKWh, reading.CurrentExportedEnergyKWh, reading.InternalConsumption,
            reading.ReadingStatusLookupId, statusName, reading.RejectionReason, reading.Notes,
            reading.CreatedAt, reading.CreatedBy);
    }

    public async Task<SubStationReadingDto?> GetByIdAsync(int id)
    {
        return await _db.SubStationReadings
            .Include(r => r.SubStation)
            .Include(r => r.Meter)
            .Include(r => r.ReadingStatusLookup)
            .Where(r => r.Id == id)
            .Select(r => new SubStationReadingDto(
                r.Id, r.SubStationId, r.SubStation.Name,
                r.MeterId, r.Meter.Name, r.ReadingDate,
                r.PrevExportedEnergyKWh, r.CurrentExportedEnergyKWh, r.InternalConsumption,
                r.ReadingStatusLookupId, r.ReadingStatusLookup.Name, r.RejectionReason, r.Notes,
                r.CreatedAt, r.CreatedBy))
            .FirstOrDefaultAsync();
    }

    public async Task<List<SubStationReadingDto>> GetByStationAsync(int stationId)
    {
        var query = _db.SubStationReadings
            .Include(r => r.SubStation)
            .Include(r => r.Meter)
            .Include(r => r.ReadingStatusLookup)
            .Where(r => r.SubStation != null && r.SubStation.StationId == stationId)
            .AsQueryable();

        if (!_currentUser.HasAnyAdminRole)
        {
            var userId = _currentUser.UserId;
            if (userId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
                if (!accessibleIds.Contains(stationId))
                    return new List<SubStationReadingDto>();
            }
        }

        return await query
            .OrderByDescending(r => r.ReadingDate)
            .Select(r => new SubStationReadingDto(
                r.Id, r.SubStationId, r.SubStation.Name,
                r.MeterId, r.Meter.Name, r.ReadingDate,
                r.PrevExportedEnergyKWh, r.CurrentExportedEnergyKWh, r.InternalConsumption,
                r.ReadingStatusLookupId, r.ReadingStatusLookup.Name, r.RejectionReason, r.Notes,
                r.CreatedAt, r.CreatedBy))
            .ToListAsync();
    }

    public async Task<SubStationReadingDto?> GetLastByStationAsync(int stationId, int? subStation = null, CancellationToken ct = default)
    {
        var query = _db.SubStationReadings
            .Include(r => r.SubStation)
            .Include(r => r.Meter)
            .Include(r => r.ReadingStatusLookup)
            .Where(r => r.SubStation != null && r.SubStation.StationId == stationId)
            .AsQueryable();

        if (subStation.HasValue)
            query = query.Where(r => r.SubStationId == subStation.Value);

        if (!_currentUser.HasAnyAdminRole)
        {
            var userId = _currentUser.UserId;
            if (userId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
                if (accessibleIds.Count == 0)
                    return null;
                query = query.Where(r => accessibleIds.Contains(r.SubStationId));
            }
        }

        var approvedLookupId = await GetLookupIdAsync("ReadingStatus", "Approved");

        var reading = await query
            .Where(r => r.ReadingStatusLookupId == approvedLookupId)
            .OrderByDescending(r => r.ReadingDate)
            .FirstOrDefaultAsync();

        if (reading is null) return null;

        var meterName = reading.MeterId.HasValue
            ? await _db.Meters.Where(m => m.Id == reading.MeterId.Value).Select(m => m.Name).FirstOrDefaultAsync()
            : null;
        var statusName = await _db.Lookups.Where(l => l.Id == reading.ReadingStatusLookupId).Select(l => l.Name).FirstOrDefaultAsync();

        return new SubStationReadingDto(
            reading.Id, reading.SubStationId, reading.SubStation?.Name,
            reading.MeterId, meterName, reading.ReadingDate,
            reading.PrevExportedEnergyKWh, reading.CurrentExportedEnergyKWh, reading.InternalConsumption,
            reading.ReadingStatusLookupId, statusName, reading.RejectionReason, reading.Notes,
            reading.CreatedAt, reading.CreatedBy);
    }

    public async Task<List<PendingSubStationReadingDto>> GetPendingAsync(CancellationToken ct = default)
    {
        var query = _db.SubStationReadings
            .Include(r => r.SubStation)
            .Include(r => r.Meter)
            .Include(r => r.ReadingStatusLookup)
            .AsQueryable();

        if (!_currentUser.HasAnyAdminRole)
        {
            var userId = _currentUser.UserId;
            if (userId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
                if (accessibleIds.Count == 0)
                    return new List<PendingSubStationReadingDto>();
                query = query.Where(r => accessibleIds.Contains(r.SubStationId));
            }
        }

        // Only return Draft readings for the approval page (ReadingStatusLookupId=55)
        var draftLookupId = await GetLookupIdAsync("ReadingStatus", "Draft");

        query = query.Where(r => r.ReadingStatusLookupId == draftLookupId);

        return await query
            .OrderByDescending(r => r.ReadingDate)
            .Select(r => new PendingSubStationReadingDto(
                r.Id, r.SubStationId, r.SubStation.Name,
                r.MeterId, r.Meter.Name, r.ReadingDate,
                r.PrevExportedEnergyKWh, r.CurrentExportedEnergyKWh, r.InternalConsumption,
                r.ReadingStatusLookupId, r.ReadingStatusLookup.Name, r.RejectionReason, r.Notes,
                r.CreatedAt, r.CreatedBy))
            .ToListAsync(ct);
    }

    public async Task ApproveAsync(int id, int userId)
    {
        var reading = await _db.SubStationReadings
            .Include(r => r.ReadingStatusLookup)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"SubStationReading {id} not found.");

        // Only readings that are Pending or Submitted can be approved.
        if (reading.ReadingStatusLookup?.Code == "Approved" || reading.ReadingStatusLookup?.Code == "Rejected")
            throw new InvalidOperationException($"Cannot approve reading {id}: it has already been {reading.ReadingStatusLookup.Code.ToLower()}.");

        var approvedLookupId = await GetLookupIdAsync("ReadingStatus", "Approved");

        if (approvedLookupId == 0)
            throw new InvalidOperationException("ReadingStatus lookup with Code='Approved' not found. Seed the ReadingStatus lookup category.");

        reading.ReadingStatusLookupId = approvedLookupId;
        reading.UpdatedAt = DateTime.UtcNow;
        reading.UpdatedBy = userId;
        await _db.SaveChangesAsync();
    }

    public async Task<ProductionSummaryDto> GetProductionSummaryAsync(int stationId, DateTime date, int? subStationId = null)
    {
        var approvedLookupId = await GetLookupIdAsync("ReadingStatus", "Approved");

        var query = _db.DailyReadings
            .Include(r => r.Generator)
            .Where(r => r.ReadingDate.Date == date.Date
                      && r.Generator.SubStation.StationId == stationId
                      && r.ReadingStatusLookupId == approvedLookupId
                      && r.DeletedAt == null)
            .AsQueryable();

        if (subStationId.HasValue)
            query = query.Where(r => r.Generator.SubStationId == subStationId.Value);

        if (!_currentUser.HasAnyAdminRole)
        {
            var userId = _currentUser.UserId;
            if (userId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
                if (accessibleIds.Count > 0)
                    query = query.Where(r => accessibleIds.Contains(r.Generator.SubStationId));
                else
                    return new ProductionSummaryDto(0, 0, date);
            }
        }

        var result = await query
            .GroupBy(r => 1)
            .Select(g => new ProductionSummaryDto(
                g.Sum(r => r.CurrentEnergyKWh - r.PrevEnergyKWh),
                g.Select(r => r.GeneratorId).Distinct().Count(),
                date))
            .FirstOrDefaultAsync();

        return result ?? new ProductionSummaryDto(0, 0, date);
    }

    public async Task RejectAsync(int id, int userId, string? reason = null)
    {
        var reading = await _db.SubStationReadings
            .Include(r => r.ReadingStatusLookup)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"SubStationReading {id} not found.");

        var rejectedLookupId = await GetLookupIdAsync("ReadingStatus", "Rejected");

        if (rejectedLookupId == 0)
            throw new InvalidOperationException("ReadingStatus lookup with Code='Rejected' not found. Seed the ReadingStatus lookup category.");

        reading.ReadingStatusLookupId = rejectedLookupId;
        reading.RejectionReason = reason;
        reading.UpdatedAt = DateTime.UtcNow;
        reading.UpdatedBy = userId;
        await _db.SaveChangesAsync();
    }
}
