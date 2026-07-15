using System.Security.Claims;
using FuelTrack.Application.DTOs.SubStationReadings;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class SubStationReadingService : ISubStationReadingService
{
    private readonly FuelTrackDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserScopeFilterService _scopeFilter;

    public SubStationReadingService(FuelTrackDbContext db, IHttpContextAccessor httpContextAccessor, IUserScopeFilterService scopeFilter)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _scopeFilter = scopeFilter;
    }

    private int? GetCurrentUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    private bool IsAdmin()
    {
        return _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
    }

    private int GetSubmitterId()
    {
        return GetCurrentUserId() ?? 0;
    }

    public async Task<SubStationReadingDto> CreateAsync(CreateSubStationReadingDto dto)
    {
        if (dto.SubStationId <= 0)
            throw new ArgumentException("Site number (SubStationId) is required.");

        var subStation = await _db.SubStations.FindAsync(dto.SubStationId)
            ?? throw new KeyNotFoundException($"SubStation {dto.SubStationId} not found.");

        // Resolve the grid meter if the caller did not supply one and the site owns an
        // exported-energy meter.
        int? meterId = dto.MeterId;
        if (meterId is null || meterId == 0)
        {
            meterId = await _db.Meters
                .Where(m => m.SubStationId == dto.SubStationId && m.MeterType == "عداد الطاقة المرسلة")
                .Select(m => (int?)m.Id)
                .FirstOrDefaultAsync();
        }

        // Reject readings when no meter is linked to the site.
        if (meterId is null || meterId == 0)
            throw new InvalidOperationException("Cannot create reading: site is not linked to an active meter. Link a meter to the site first.");

        // Default the reading status to "Draft" when not supplied (a newly submitted
        // reading is pending review). Falls back to Pending/Submitted if seeded that way.
        int? statusId = dto.ReadingStatusLookupId;
        if (statusId is null || statusId == 0)
        {
            statusId = await _db.Lookups
                .Where(l => l.Category != null && l.Category.Name == "ReadingStatus" &&
                            (l.Code == "Draft" || l.Code == "DRAFT" || l.Code == "Pending" || l.Code == "Submitted"))
                .Select(l => (int?)l.Id)
                .FirstOrDefaultAsync();
        }

        if (statusId is null || statusId == 0)
            throw new InvalidOperationException("No 'ReadingStatus' lookup found. Seed the ReadingStatus lookup category.");

        var submitterId = GetSubmitterId();

        var reading = new SubStationReading
        {
            SubStationId = dto.SubStationId,
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
        await _db.SaveChangesAsync();

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
            meterId = await _db.Meters
                .Where(m => m.SubStationId == reading.SubStationId && m.MeterType == "عداد الطاقة المرسلة")
                .Select(m => (int?)m.Id)
                .FirstOrDefaultAsync();
        }

        // Default the reading status to "Draft" when not supplied (a newly submitted
        // reading is pending review). Falls back to Pending/Submitted if seeded that way.
        int? statusId = dto.ReadingStatusLookupId;
        if (statusId is null || statusId == 0)
        {
            statusId = await _db.Lookups
                .Where(l => l.Category != null && l.Category.Name == "ReadingStatus" &&
                            (l.Code == "Draft" || l.Code == "DRAFT" || l.Code == "Pending" || l.Code == "Submitted"))
                .Select(l => (int?)l.Id)
                .FirstOrDefaultAsync();
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
        reading.UpdatedBy = GetSubmitterId();

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
            .Where(r => r.SubStationId == stationId)
            .AsQueryable();

        if (!IsAdmin())
        {
            var userId = GetCurrentUserId();
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

    public async Task<SubStationReadingDto?> GetLastByStationAsync(int stationId, int? subStation = null)
    {
        var query = _db.SubStationReadings
            .Include(r => r.SubStation)
            .Include(r => r.Meter)
            .Include(r => r.ReadingStatusLookup)
            .Where(r => r.SubStation != null && r.SubStation.StationId == stationId)
            .AsQueryable();

        if (subStation.HasValue)
            query = query.Where(r => r.SubStationId == subStation.Value);

        if (!IsAdmin())
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
                if (accessibleIds.Count == 0)
                    return null;
                query = query.Where(r => accessibleIds.Contains(r.SubStationId));
            }
        }

        var reading = await query
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

    public async Task<List<PendingSubStationReadingDto>> GetPendingAsync()
    {
        var query = _db.SubStationReadings
            .Include(r => r.SubStation)
            .Include(r => r.Meter)
            .Include(r => r.ReadingStatusLookup)
            .AsQueryable();

        if (!IsAdmin())
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(userId.Value);
                if (accessibleIds.Count == 0)
                    return new List<PendingSubStationReadingDto>();
                query = query.Where(r => accessibleIds.Contains(r.SubStationId));
            }
        }

        return await query
            .OrderByDescending(r => r.ReadingDate)
            .Select(r => new PendingSubStationReadingDto(
                r.Id, r.SubStationId, r.SubStation.Name,
                r.MeterId, r.Meter.Name, r.ReadingDate,
                r.PrevExportedEnergyKWh, r.CurrentExportedEnergyKWh, r.InternalConsumption,
                r.ReadingStatusLookupId, r.ReadingStatusLookup.Name, r.RejectionReason, r.Notes,
                r.CreatedAt, r.CreatedBy))
            .ToListAsync();
    }

    public async Task ApproveAsync(int id, int userId)
    {
        var reading = await _db.SubStationReadings.FindAsync(id)
            ?? throw new KeyNotFoundException($"SubStationReading {id} not found.");

        var approvedLookupId = await _db.Lookups
            .Where(l => l.Category != null && l.Category.Name == "ReadingStatus" && l.Code == "Approved")
            .Select(l => l.Id)
            .FirstOrDefaultAsync();

        reading.ReadingStatusLookupId = approvedLookupId;
        reading.UpdatedAt = DateTime.UtcNow;
        reading.UpdatedBy = userId;
        await _db.SaveChangesAsync();
    }

    public async Task RejectAsync(int id, int userId)
    {
        var reading = await _db.SubStationReadings.FindAsync(id)
            ?? throw new KeyNotFoundException($"SubStationReading {id} not found.");

        var rejectedLookupId = await _db.Lookups
            .Where(l => l.Category != null && l.Category.Name == "ReadingStatus" && l.Code == "Rejected")
            .Select(l => l.Id)
            .FirstOrDefaultAsync();

        reading.ReadingStatusLookupId = rejectedLookupId;
        reading.UpdatedAt = DateTime.UtcNow;
        reading.UpdatedBy = userId;
        await _db.SaveChangesAsync();
    }
}
