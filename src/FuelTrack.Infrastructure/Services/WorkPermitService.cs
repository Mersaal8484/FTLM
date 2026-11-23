using FuelTrack.Application;
using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.DTOs.Warehouse;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FuelTrack.Infrastructure.Services;

public class WorkPermitService : IWorkPermitService
{
    private static readonly SemaphoreSlim _permitSequenceLock = new(1, 1);

    private readonly FuelTrackDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserScopeFilterService _scopeFilter;
    private readonly ICommercialStationModeService _commercialMode;

    public WorkPermitService(
        FuelTrackDbContext db,
        ICurrentUserService currentUser,
        IUserScopeFilterService scopeFilter,
        ICommercialStationModeService commercialMode)
    {
        _db = db;
        _currentUser = currentUser;
        _scopeFilter = scopeFilter;
        _commercialMode = commercialMode;
    }

    private async Task<bool> CanAccessAsync(int subStationId)
    {
        if (await _commercialMode.IsCommercialModeAsync())
        {
            var scope = await _commercialMode.GetScopeAsync();
            return scope is not null && scope.SubStationId == subStationId;
        }
        if (_currentUser.HasAnyAdminRole) return true;
        return _currentUser.UserId is int userId && await _scopeFilter.HasAccessToSubStationAsync(userId, subStationId);
    }

    public async Task<WorkPermitDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        if (await _commercialMode.IsCommercialModeAsync(ct))
        {
            var scope = await _commercialMode.GetScopeAsync(ct);
            if (scope is null)
                return null;

            return await _db.WorkPermits
                .AsNoTracking()
                .Include(wp => wp.SubStation)
                .Include(wp => wp.Generator)
                .Include(wp => wp.MaintenanceTypeLookup)
                .Include(wp => wp.DepartmentLookup)
                .Include(wp => wp.PriorityLookup)
                .Include(wp => wp.StatusLookup)
                .Include(wp => wp.RequestedBy)
                .Include(wp => wp.AssignedTo)
                .Include(wp => wp.ApprovedBy)
                .Include(wp => wp.Tasks)
                .Include(wp => wp.DispatchRequests)
                .Include(wp => wp.InstalledParts)
                    .ThenInclude(ip => ip.SparePart)
                .Include(wp => wp.Logs)
                    .ThenInclude(l => l.LoggedBy)
                .Where(wp => wp.Id == id && wp.DeletedAt == null && wp.SubStationId == scope.SubStationId)
                .Select(wp => MapToDetail(wp))
                .FirstOrDefaultAsync(ct);
        }

        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (!currentUserId.HasValue) return null;
            var accessibleSubStationIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
            if (accessibleSubStationIds.Count == 0)
                return null;

            return await _db.WorkPermits
                .AsNoTracking()
                .Include(wp => wp.SubStation)
                .Include(wp => wp.Generator)
                .Include(wp => wp.MaintenanceTypeLookup)
                .Include(wp => wp.DepartmentLookup)
                .Include(wp => wp.PriorityLookup)
                .Include(wp => wp.StatusLookup)
                .Include(wp => wp.RequestedBy)
                .Include(wp => wp.AssignedTo)
                .Include(wp => wp.ApprovedBy)
                .Include(wp => wp.Tasks)
                .Include(wp => wp.DispatchRequests)
                .Include(wp => wp.InstalledParts)
                    .ThenInclude(ip => ip.SparePart)
                .Include(wp => wp.Logs)
                    .ThenInclude(l => l.LoggedBy)
                .Where(wp => wp.Id == id && wp.DeletedAt == null && accessibleSubStationIds.Contains(wp.SubStationId))
                .Select(wp => MapToDetail(wp))
                .FirstOrDefaultAsync(ct);
        }

        return await _db.WorkPermits
            .AsNoTracking()
            .Include(wp => wp.SubStation)
            .Include(wp => wp.Generator)
            .Include(wp => wp.MaintenanceTypeLookup)
            .Include(wp => wp.DepartmentLookup)
            .Include(wp => wp.PriorityLookup)
            .Include(wp => wp.StatusLookup)
            .Include(wp => wp.RequestedBy)
            .Include(wp => wp.AssignedTo)
            .Include(wp => wp.ApprovedBy)
            .Include(wp => wp.Tasks)
            .Include(wp => wp.DispatchRequests)
            .Include(wp => wp.InstalledParts)
                .ThenInclude(ip => ip.SparePart)
            .Include(wp => wp.Logs)
                .ThenInclude(l => l.LoggedBy)
            .Where(wp => wp.Id == id && wp.DeletedAt == null)
            .Select(wp => MapToDetail(wp))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<WorkPermitSheetDto?> GetSheetAsync(int id, CancellationToken ct = default)
    {
        var permit = await _db.WorkPermits
            .AsNoTracking()
            .Include(wp => wp.SubStation)
                .ThenInclude(ss => ss.Station)
            .Include(wp => wp.Generator)
            .Include(wp => wp.MaintenanceTypeLookup)
            .Include(wp => wp.DepartmentLookup)
            .Include(wp => wp.PriorityLookup)
            .Include(wp => wp.StatusLookup)
            .Include(wp => wp.RequestedBy)
            .Include(wp => wp.AssignedTo)
            .Include(wp => wp.ApprovedBy)
            .Include(wp => wp.Tasks)
            .Include(wp => wp.Logs)
                .ThenInclude(l => l.LoggedBy)
            .FirstOrDefaultAsync(wp => wp.Id == id && wp.DeletedAt == null, ct);

        if (permit == null)
            return null;

        if (await _commercialMode.IsCommercialModeAsync(ct))
        {
            var scope = await _commercialMode.GetScopeAsync(ct);
            if (scope == null || permit.SubStationId != scope.SubStationId)
                return null;
        }
        else if (!await CanAccessAsync(permit.SubStationId))
        {
            return null;
        }

        // Commercial terminology: station only, no branch/substation/site words.
        var stationName = permit.SubStation.Station != null
            ? permit.SubStation.Station.Name
            : permit.SubStation.Name;

        // Dispatched parts from DispatchRequests
        var dispatchRequests = await _db.DispatchRequests
            .AsNoTracking()
            .Include(dr => dr.Items)
                .ThenInclude(i => i.SparePart)
                    .ThenInclude(sp => sp.UnitLookup)
            .Where(dr => dr.WorkPermitId == id && dr.DeletedAt == null)
            .ToListAsync(ct);

        var dispatchedParts = dispatchRequests
            .SelectMany(dr => dr.Items)
            .Select(i => new WorkPermitSheetDispatchedPartDto(
                i.SparePart.Name,
                i.SparePart.PartNumber,
                i.RequestedQuantity,
                i.ApprovedQuantity,
                i.SparePart.UnitLookup != null ? i.SparePart.UnitLookup.Name : null))
            .ToList();

        // Replacements recorded and oil consumed (from C6.2)
        var replacements = await _db.ServiceReplacementRecords
            .AsNoTracking()
            .Include(r => r.ServiceItem)
            .Include(r => r.OilTransaction)
                .ThenInclude(ot => ot!.OilLookup)
            .Where(r => r.WorkPermitId == id && r.VoidedAtUtc == null && r.DeletedAt == null)
            .OrderBy(r => r.OccurredAtUtc)
            .ToListAsync(ct);

        decimal totalOilConsumed = 0m;
        var replacementDtos = new List<WorkPermitSheetReplacementDto>();
        foreach (var r in replacements)
        {
            decimal oilLitres = r.OilTransaction != null ? (decimal)r.OilTransaction.QuantityLiters : 0m;
            totalOilConsumed += oilLitres;
            var oilType = r.OilTransaction?.OilLookup?.Name;
            var serviceItemName = !string.IsNullOrWhiteSpace(r.ServiceItem.NameAr)
                ? r.ServiceItem.NameAr
                : r.ServiceItem.NameEn;

            replacementDtos.Add(new WorkPermitSheetReplacementDto(
                r.Id,
                serviceItemName,
                r.HoursAtService,
                r.Kind.ToString(),
                r.OccurredAtUtc,
                r.IsEarly,
                r.EarlyReplacementReason,
                r.IntervalHoursSnapshot,
                r.OilTransactionId,
                oilLitres,
                oilType,
                r.Notes));
        }

        var taskDtos = permit.Tasks
            .OrderBy(t => t.SortOrder)
            .Select(t => new WorkPermitSheetTaskDto(
                t.Id,
                t.TaskDescription,
                t.IsCompleted,
                t.CompletedAt,
                null))
            .ToList();

        var logDtos = permit.Logs
            .OrderByDescending(l => l.LogDate)
            .Select(l => new WorkPermitSheetLogDto(
                l.Id,
                l.Action,
                l.Notes,
                l.LoggedBy?.FullName ?? "",
                l.LogDate))
            .ToList();

        // Approved log timestamp if any
        DateTime? approvedAt = permit.Logs
            .Where(l => l.Action.Contains("Approved", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(l => l.LogDate)
            .Select(l => (DateTime?)l.LogDate)
            .FirstOrDefault();

        return new WorkPermitSheetDto(
            permit.Id,
            permit.PermitNumber,
            stationName,
            permit.GeneratorId,
            permit.Generator?.Name,
            permit.Generator?.SerialNumber,
            permit.MaintenanceTypeLookup?.Name ?? "",
            permit.DepartmentLookup?.Name ?? "",
            permit.PriorityLookup?.Name ?? "",
            permit.StatusLookup?.Name ?? "",
            permit.StatusLookup?.Code ?? "",
            permit.PermitDate,
            permit.ProblemDescription,
            permit.ProblemCause,
            permit.PlannedStartDate,
            permit.PlannedEndDate,
            permit.ActualStartDate,
            permit.ActualEndDate,
            permit.RunningHoursAtCreation,
            permit.RequestedBy?.FullName ?? "",
            permit.CreatedAt,
            permit.ApprovedBy?.FullName,
            approvedAt,
            permit.AssignedTo?.FullName,
            permit.CompletionNotes,
            permit.RejectionReason,
            permit.Notes,
            taskDtos,
            dispatchedParts,
            replacementDtos,
            totalOilConsumed,
            logDtos);
    }

    public async Task<PagedResult<WorkPermitListDto>> GetBySubStationAsync(int subStationId, int page, int pageSize, int? status = null, CancellationToken ct = default)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (!currentUserId.HasValue)
                return new PagedResult<WorkPermitListDto> { Items = Array.Empty<WorkPermitListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
            var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
            if (!accessibleIds.Contains(subStationId))
                return new PagedResult<WorkPermitListDto> { Items = Array.Empty<WorkPermitListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }

        return await BuildPagedQuery(wp => wp.SubStationId == subStationId && wp.DeletedAt == null, page, pageSize, status, ct);
    }

    public async Task<PagedResult<WorkPermitListDto>> GetByGeneratorAsync(int generatorId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.WorkPermits
            .AsNoTracking()
            .Include(wp => wp.SubStation)
            .Include(wp => wp.Generator)
            .Include(wp => wp.MaintenanceTypeLookup)
            .Include(wp => wp.DepartmentLookup)
            .Include(wp => wp.PriorityLookup)
            .Include(wp => wp.StatusLookup)
            .Include(wp => wp.RequestedBy)
            .Include(wp => wp.Tasks)
            .Where(wp => wp.GeneratorId == generatorId && wp.DeletedAt == null)
            .AsQueryable();

        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (!currentUserId.HasValue)
                return new PagedResult<WorkPermitListDto> { Items = Array.Empty<WorkPermitListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
            var accessibleSubStationIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
            if (accessibleSubStationIds.Count == 0)
                return new PagedResult<WorkPermitListDto> { Items = Array.Empty<WorkPermitListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
            query = query.Where(wp => accessibleSubStationIds.Contains(wp.SubStationId));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(wp => wp.PermitDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(wp => new WorkPermitListDto(
                wp.Id,
                wp.PermitNumber,
                wp.SubStation.Name,
                wp.Generator != null ? wp.Generator.Name : null,
                wp.MaintenanceTypeLookup.Name,
                wp.DepartmentLookup.Name,
                wp.PriorityLookup.Name,
                wp.StatusLookup.Name,
                wp.PermitDate,
                wp.RequestedBy.FullName,
                wp.AssignedTo != null ? wp.AssignedTo.FullName : null,
                wp.PlannedStartDate,
                wp.ActualStartDate,
                wp.Tasks.Count,
                wp.Tasks.Count(t => t.IsCompleted),
                wp.StatusLookup.Code, wp.PriorityLookup.Code))
            .ToListAsync(ct);

        return new PagedResult<WorkPermitListDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<WorkPermitListDto>> GetAllAsync(int page, int pageSize, int? status = null, int? priority = null, int? subStationId = null, CancellationToken ct = default)
    {
        var query = _db.WorkPermits
            .AsNoTracking()
            .Include(wp => wp.SubStation)
            .Include(wp => wp.Generator)
            .Include(wp => wp.MaintenanceTypeLookup)
            .Include(wp => wp.DepartmentLookup)
            .Include(wp => wp.PriorityLookup)
            .Include(wp => wp.StatusLookup)
            .Include(wp => wp.RequestedBy)
            .Include(wp => wp.Tasks)
            .Where(wp => wp.DeletedAt == null)
            .AsQueryable();

        if (await _commercialMode.IsCommercialModeAsync(ct))
        {
            var scope = await _commercialMode.GetScopeAsync(ct);
            if (scope is null)
                return new PagedResult<WorkPermitListDto> { Items = Array.Empty<WorkPermitListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };

            query = query.Where(wp => wp.SubStationId == scope.SubStationId);
        }
        else
        {
            if (!_currentUser.HasAnyAdminRole)
            {
                var currentUserId = _currentUser.UserId;
                if (!currentUserId.HasValue)
                    return new PagedResult<WorkPermitListDto> { Items = Array.Empty<WorkPermitListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
                var accessibleSubStationIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleSubStationIds.Count == 0)
                    return new PagedResult<WorkPermitListDto> { Items = Array.Empty<WorkPermitListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
                query = query.Where(wp => accessibleSubStationIds.Contains(wp.SubStationId));
            }

            if (subStationId.HasValue)
                query = query.Where(wp => wp.SubStationId == subStationId.Value);
        }

        if (status.HasValue)
            query = query.Where(wp => wp.StatusLookupId == status.Value);

        if (priority.HasValue)
            query = query.Where(wp => wp.PriorityLookupId == priority.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(wp => wp.PermitDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(wp => new WorkPermitListDto(
                wp.Id,
                wp.PermitNumber,
                wp.SubStation.Name,
                wp.Generator != null ? wp.Generator.Name : null,
                wp.MaintenanceTypeLookup.Name,
                wp.DepartmentLookup.Name,
                wp.PriorityLookup.Name,
                wp.StatusLookup.Name,
                wp.PermitDate,
                wp.RequestedBy.FullName,
                wp.AssignedTo != null ? wp.AssignedTo.FullName : null,
                wp.PlannedStartDate,
                wp.ActualStartDate,
                wp.Tasks.Count,
                wp.Tasks.Count(t => t.IsCompleted),
                wp.StatusLookup.Code, wp.PriorityLookup.Code))
            .ToListAsync(ct);

        return new PagedResult<WorkPermitListDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Result<WorkPermitDetailDto>> CreateAsync(CreateWorkPermitDto dto, int userId, CancellationToken ct = default)
    {
        int targetSubStationId;

        if (await _commercialMode.IsCommercialModeAsync(ct))
        {
            var scope = await _commercialMode.GetScopeAsync(ct);
            if (scope is null)
                return Result<WorkPermitDetailDto>.Failure("Commercial mode has no active technical station scope.");

            var station = await _db.Stations.AsNoTracking().FirstOrDefaultAsync(s => s.Id == scope.StationId && s.DeletedAt == null, ct);
            if (station == null || string.IsNullOrWhiteSpace(station.Code))
                return Result<WorkPermitDetailDto>.Failure("Commercial station code is missing or blank.");

            targetSubStationId = scope.SubStationId;
        }
        else
        {
            if (dto.SubStationId <= 0)
                return Result<WorkPermitDetailDto>.Failure("SubStationId is required outside commercial mode.");

            var subStation = await _db.SubStations.FirstOrDefaultAsync(ss => ss.Id == dto.SubStationId && ss.DeletedAt == null, ct);
            if (subStation is null)
                return Result<WorkPermitDetailDto>.NotFound("SubStation not found.");

            targetSubStationId = dto.SubStationId;
        }

        if (!await CanAccessAsync(targetSubStationId))
            return Result<WorkPermitDetailDto>.Forbidden();

        if (dto.GeneratorId.HasValue)
        {
            var generator = await _db.Generators.FirstOrDefaultAsync(g => g.Id == dto.GeneratorId.Value && g.DeletedAt == null, ct);
            if (generator is null)
                return Result<WorkPermitDetailDto>.NotFound("Generator not found.");

            if (generator.SubStationId != targetSubStationId)
                return Result<WorkPermitDetailDto>.Failure("Generator does not belong to the permit sub-station.");
        }

        var maintenanceType = await _db.Lookups.FirstOrDefaultAsync(l => l.Id == dto.MaintenanceTypeLookupId && l.DeletedAt == null, ct);
        if (maintenanceType == null)
            return Result<WorkPermitDetailDto>.NotFound("Maintenance type not found.");

        var department = await _db.Lookups.FirstOrDefaultAsync(l => l.Id == dto.DepartmentLookupId && l.DeletedAt == null, ct);
        if (department == null)
            return Result<WorkPermitDetailDto>.NotFound("Department not found.");

        var priority = await _db.Lookups.FirstOrDefaultAsync(l => l.Id == dto.PriorityLookupId && l.DeletedAt == null, ct);
        if (priority == null)
            return Result<WorkPermitDetailDto>.NotFound("Priority not found.");

        var draftStatus = await _db.Lookups
            .FirstOrDefaultAsync(l => l.Category.Name == "MaintenanceStatus" && l.Code == "Draft", ct);

        if (draftStatus is null)
            return Result<WorkPermitDetailDto>.Failure("Draft status is not configured.");

        WorkPermit permit = null!;

        await _permitSequenceLock.WaitAsync(ct);
        try
        {
            const int maxRetries = 5;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var permitNumber = await GeneratePermitNumberAsync(targetSubStationId, ct);

                permit = new WorkPermit
                {
                    PermitNumber = permitNumber,
                    SubStationId = targetSubStationId,
                    GeneratorId = dto.GeneratorId,
                    MaintenanceTypeLookupId = dto.MaintenanceTypeLookupId,
                    DepartmentLookupId = dto.DepartmentLookupId,
                    PriorityLookupId = dto.PriorityLookupId,
                    StatusLookupId = draftStatus.Id,
                    PermitDate = dto.PermitDate,
                    ProblemDescription = dto.ProblemDescription,
                    ProblemCause = dto.ProblemCause,
                    PlannedStartDate = dto.PlannedStartDate,
                    PlannedEndDate = dto.PlannedEndDate,
                    RequestedById = userId,
                    AssignedToId = dto.AssignedToId,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = userId
                };

                try
                {
                    _db.WorkPermits.Add(permit);
                    await _db.SaveChangesAsync(ct);
                    break;
                }
                catch (DbUpdateException) when (attempt < maxRetries)
                {
                    _db.Entry(permit).State = EntityState.Detached;
                }
            }
        }
        finally
        {
            _permitSequenceLock.Release();
        }

        if (dto.Tasks != null && dto.Tasks.Count > 0)
        {
            var tasks = new List<WorkTask>();
            foreach (var taskDto in dto.Tasks)
            {
                tasks.Add(new WorkTask
                {
                    WorkPermitId = permit.Id,
                    TaskDescription = taskDto.TaskDescription,
                    SortOrder = taskDto.SortOrder,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = userId
                });
            }
            _db.WorkTasks.AddRange(tasks);
            await _db.SaveChangesAsync(ct);
        }

        var detail = await GetByIdAsync(permit.Id, ct);
        return Result<WorkPermitDetailDto>.Success(detail!);
    }

    public async Task<Result<WorkPermitDetailDto>> UpdateAsync(int id, UpdateWorkPermitDto dto, int userId, CancellationToken ct = default)
    {
        var permit = await _db.WorkPermits
            .Include(wp => wp.Tasks)
            .Include(wp => wp.StatusLookup)
            .FirstOrDefaultAsync(wp => wp.Id == id && wp.DeletedAt == null, ct);

        if (permit is null)
            return Result<WorkPermitDetailDto>.NotFound($"Work permit {id} not found.");

        if (await _commercialMode.IsCommercialModeAsync(ct))
        {
            var scope = await _commercialMode.GetScopeAsync(ct);
            if (scope is null || permit.SubStationId != scope.SubStationId)
                return Result<WorkPermitDetailDto>.NotFound($"Work permit {id} not found.");
        }

        if (!await CanAccessAsync(permit.SubStationId))
            return Result<WorkPermitDetailDto>.Forbidden();

        if (permit.StatusLookup != null && (permit.StatusLookup.Code == "Completed" || permit.StatusLookup.Code == "Cancelled"))
            return Result<WorkPermitDetailDto>.Failure("Cannot update a completed or cancelled work permit.");

        if (dto.GeneratorId.HasValue)
        {
            var generator = await _db.Generators.FirstOrDefaultAsync(g => g.Id == dto.GeneratorId.Value && g.DeletedAt == null, ct);
            if (generator is null)
                return Result<WorkPermitDetailDto>.NotFound("Generator not found.");
            if (generator.SubStationId != permit.SubStationId)
                return Result<WorkPermitDetailDto>.Failure("Generator does not belong to the permit sub-station.");
            permit.GeneratorId = dto.GeneratorId.Value;
        }

        if (dto.MaintenanceTypeLookupId.HasValue)
        {
            var maintenanceType = await _db.Lookups.FirstOrDefaultAsync(l => l.Id == dto.MaintenanceTypeLookupId.Value && l.DeletedAt == null, ct);
            if (maintenanceType == null)
                return Result<WorkPermitDetailDto>.NotFound("Maintenance type not found.");
            permit.MaintenanceTypeLookupId = dto.MaintenanceTypeLookupId.Value;
        }

        if (dto.DepartmentLookupId.HasValue)
        {
            var department = await _db.Lookups.FirstOrDefaultAsync(l => l.Id == dto.DepartmentLookupId.Value && l.DeletedAt == null, ct);
            if (department == null)
                return Result<WorkPermitDetailDto>.NotFound("Department not found.");
            permit.DepartmentLookupId = dto.DepartmentLookupId.Value;
        }

        if (dto.PriorityLookupId.HasValue)
        {
            var priority = await _db.Lookups.FirstOrDefaultAsync(l => l.Id == dto.PriorityLookupId.Value && l.DeletedAt == null, ct);
            if (priority == null)
                return Result<WorkPermitDetailDto>.NotFound("Priority not found.");
            permit.PriorityLookupId = dto.PriorityLookupId.Value;
        }

        if (dto.PermitDate.HasValue) permit.PermitDate = dto.PermitDate.Value;
        if (dto.ProblemDescription is not null) permit.ProblemDescription = dto.ProblemDescription;
        if (dto.ProblemCause is not null) permit.ProblemCause = dto.ProblemCause;
        if (dto.PlannedStartDate.HasValue) permit.PlannedStartDate = dto.PlannedStartDate;
        if (dto.PlannedEndDate.HasValue) permit.PlannedEndDate = dto.PlannedEndDate;
        if (dto.AssignedToId.HasValue) permit.AssignedToId = dto.AssignedToId;
        if (dto.CompletionNotes is not null) permit.CompletionNotes = dto.CompletionNotes;
        if (dto.RejectionReason is not null) permit.RejectionReason = dto.RejectionReason;
        if (dto.Notes is not null) permit.Notes = dto.Notes;

        permit.UpdatedAt = DateTime.UtcNow;
        permit.UpdatedBy = userId;

        if (dto.Tasks != null)
        {
            var newTaskDescriptions = dto.Tasks.Select(t => t.TaskDescription).ToList();

            foreach (var existingTask in permit.Tasks.ToList())
            {
                if (!newTaskDescriptions.Contains(existingTask.TaskDescription))
                {
                    _db.WorkTasks.Remove(existingTask);
                }
            }

            foreach (var taskDto in dto.Tasks)
            {
                var existingTask = permit.Tasks.FirstOrDefault(t => t.TaskDescription == taskDto.TaskDescription);
                if (existingTask == null)
                {
                    _db.WorkTasks.Add(new WorkTask
                    {
                        WorkPermitId = permit.Id,
                        TaskDescription = taskDto.TaskDescription,
                        SortOrder = taskDto.SortOrder,
                        IsCompleted = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = userId
                    });
                }
            }
        }

        await _db.SaveChangesAsync(ct);

        var detail = await GetByIdAsync(permit.Id, ct);
        return Result<WorkPermitDetailDto>.Success(detail!);
    }

    public async Task<Result<WorkPermitDetailDto>> UpdateStatusAsync(int id, UpdateWorkPermitStatusDto dto, int userId, CancellationToken ct = default)
    {
        var permit = await _db.WorkPermits
            .Include(wp => wp.StatusLookup)
            .FirstOrDefaultAsync(wp => wp.Id == id && wp.DeletedAt == null, ct);

        if (permit is null)
            return Result<WorkPermitDetailDto>.NotFound($"Work permit {id} not found.");

        var isCommercial = await _commercialMode.IsCommercialModeAsync(ct);
        if (isCommercial)
        {
            var scope = await _commercialMode.GetScopeAsync(ct);
            if (scope is null || permit.SubStationId != scope.SubStationId)
                return Result<WorkPermitDetailDto>.NotFound($"Work permit {id} not found.");
        }

        if (!await CanAccessAsync(permit.SubStationId))
            return Result<WorkPermitDetailDto>.Forbidden();

        var newStatus = await _db.Lookups.FirstOrDefaultAsync(l => l.Id == dto.StatusLookupId && l.Category.Name == "MaintenanceStatus" && l.DeletedAt == null, ct);
        if (newStatus == null)
            return Result<WorkPermitDetailDto>.NotFound("Status not found.");

        var currentStatus = permit.StatusLookup;
        if (currentStatus == null)
        {
            currentStatus = await _db.Lookups.FirstOrDefaultAsync(l => l.Id == permit.StatusLookupId && l.Category.Name == "MaintenanceStatus", ct);
        }

        var currentStatusCode = currentStatus?.Code ?? "Draft";
        var newStatusCode = newStatus.Code ?? "";
        if (string.IsNullOrWhiteSpace(newStatusCode))
            return Result<WorkPermitDetailDto>.Failure("Status code is not defined.");

        if (isCommercial)
        {
            if (WorkPermitTransitionMatrix.IsTerminal(currentStatusCode))
            {
                return Result<WorkPermitDetailDto>.Conflict($"Status '{currentStatusCode}' is terminal and cannot transition to any other status.");
            }

            if (!WorkPermitTransitionMatrix.IsTransitionAllowed(currentStatusCode, newStatusCode, out var requiredAction, out var transitionError))
            {
                return Result<WorkPermitDetailDto>.Conflict(transitionError ?? $"Transition from '{currentStatusCode}' to '{newStatusCode}' is invalid.");
            }

            if (!await UserHasActionPermissionAsync(userId, requiredAction!, ct))
            {
                return Result<WorkPermitDetailDto>.Forbidden($"User lacks permission '{requiredAction}' required for status transition.");
            }
        }

        permit.StatusLookupId = dto.StatusLookupId;
        permit.Notes = string.IsNullOrEmpty(dto.Notes) ? permit.Notes : $"{permit.Notes}\n{dto.Notes}".Trim();
        if (!string.IsNullOrWhiteSpace(dto.RejectionReason))
            permit.RejectionReason = dto.RejectionReason;

        if (newStatusCode == "Approved")
        {
            permit.ApprovedById = userId;
        }

        if (dto.ActualStartDate.HasValue) permit.ActualStartDate = dto.ActualStartDate.Value;
        if (dto.ActualEndDate.HasValue) permit.ActualEndDate = dto.ActualEndDate.Value;
        permit.UpdatedAt = DateTime.UtcNow;
        permit.UpdatedBy = userId;

        // Write to permit log (MaintenanceLog)
        var log = new MaintenanceLog
        {
            WorkPermitId = permit.Id,
            GeneratorId = permit.GeneratorId,
            LogDate = DateTime.UtcNow,
            Action = $"StatusChanged:{currentStatusCode}->{newStatusCode}",
            Notes = !string.IsNullOrWhiteSpace(dto.Notes)
                ? dto.Notes
                : (!string.IsNullOrWhiteSpace(dto.RejectionReason) ? dto.RejectionReason : $"Status updated to {newStatus.Name}"),
            LoggedById = userId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId
        };
        _db.MaintenanceLogs.Add(log);

        await _db.SaveChangesAsync(ct);

        var detail = await GetByIdAsync(permit.Id, ct);
        return Result<WorkPermitDetailDto>.Success(detail!);
    }

    public async Task<Result<WorkPermitDetailDto>> ApproveAsync(int id, string? notes, int userId, CancellationToken ct = default)
    {
        var approvedLookup = await _db.Lookups.FirstOrDefaultAsync(l => l.Category.Name == "MaintenanceStatus" && l.Code == "Approved" && l.DeletedAt == null, ct);
        if (approvedLookup == null)
            return Result<WorkPermitDetailDto>.Failure("Approved status is not configured.");

        return await UpdateStatusAsync(id, new UpdateWorkPermitStatusDto(approvedLookup.Id, notes, null, null, null), userId, ct);
    }

    public async Task<Result<WorkPermitDetailDto>> RejectAsync(int id, string reason, int userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result<WorkPermitDetailDto>.Failure("Rejection reason is required.");

        var rejectedLookup = await _db.Lookups.FirstOrDefaultAsync(l => l.Category.Name == "MaintenanceStatus" && l.Code == "Rejected" && l.DeletedAt == null, ct);
        if (rejectedLookup == null)
            return Result<WorkPermitDetailDto>.Failure("Rejected status is not configured.");

        return await UpdateStatusAsync(id, new UpdateWorkPermitStatusDto(rejectedLookup.Id, null, reason, null, null), userId, ct);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var permit = await _db.WorkPermits.FindAsync(new object[] { id }, ct);
        if (permit is null)
            return Result.NotFound($"Work permit {id} not found.");

        if (await _commercialMode.IsCommercialModeAsync(ct))
        {
            var scope = await _commercialMode.GetScopeAsync(ct);
            if (scope is null || permit.SubStationId != scope.SubStationId)
                return Result.NotFound($"Work permit {id} not found.");
        }
        if (!await CanAccessAsync(permit.SubStationId)) return Result.Failure("Forbidden", 403);

        permit.IsActive = false;
        permit.DeletedAt = DateTime.UtcNow;
        permit.UpdatedAt = DateTime.UtcNow;
        permit.UpdatedBy = 0;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<WorkTaskDto> CompleteTaskAsync(int permitId, int taskId, int userId, CancellationToken ct = default)
    {
        var taskPermit = await _db.WorkPermits.AsNoTracking().FirstOrDefaultAsync(wp => wp.Id == permitId && wp.DeletedAt == null, ct);
        if (taskPermit is null || !await CanAccessAsync(taskPermit.SubStationId)) throw new KeyNotFoundException("Work permit not found.");

        var task = await _db.WorkTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.WorkPermitId == permitId && t.DeletedAt == null, ct);

        if (task is null)
            throw new KeyNotFoundException($"Task {taskId} not found in permit {permitId}.");

        task.IsCompleted = true;
        task.CompletedAt = DateTime.UtcNow;
        task.CompletedById = userId;
        task.UpdatedAt = DateTime.UtcNow;
        task.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);

        return new WorkTaskDto(
            task.Id,
            task.TaskDescription,
            task.SortOrder,
            task.IsCompleted,
            task.CompletedAt,
            task.CompletedById,
            "");
    }

    public async Task<MaintenanceLogDto> AddLogAsync(int permitId, string action, string? notes, int userId, CancellationToken ct = default)
    {
        var permit = await _db.WorkPermits.FirstOrDefaultAsync(wp => wp.Id == permitId && wp.DeletedAt == null, ct);
        if (permit is null || !await CanAccessAsync(permit.SubStationId))
            throw new KeyNotFoundException($"Work permit {permitId} not found.");

        var generatorId = permit.GeneratorId;

        var log = new MaintenanceLog
        {
            WorkPermitId = permitId,
            GeneratorId = generatorId,
            LogDate = DateTime.UtcNow,
            Action = action,
            Notes = notes,
            LoggedById = userId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = userId
        };

        _db.MaintenanceLogs.Add(log);
        await _db.SaveChangesAsync(ct);

        var loggedBy = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, ct);
        var generator = permit.Generator;

        return new MaintenanceLogDto(
            log.Id,
            permit.PermitNumber,
            generator != null ? generator.Name : null,
            log.LogDate,
            null,
            log.Action,
            log.Notes,
            loggedBy != null ? loggedBy.FullName : "",
            log.CreatedAt);
    }

    public async Task<string> GeneratePermitNumberAsync(int subStationId, CancellationToken ct = default)
    {
        if (await _commercialMode.IsCommercialModeAsync(ct))
        {
            var scope = await _commercialMode.GetScopeAsync(ct);
            if (scope == null)
                throw new InvalidOperationException("Commercial mode has no active technical station scope.");

            var station = await _db.Stations.AsNoTracking().FirstOrDefaultAsync(s => s.Id == scope.StationId && s.DeletedAt == null, ct);
            if (station == null || string.IsNullOrWhiteSpace(station.Code))
                throw new InvalidOperationException("Commercial station code is missing or blank.");

            var stationCode = station.Code.Trim();
            var prefix = $"{stationCode}-WP-";

            var existingPermits = await _db.WorkPermits
                .AsNoTracking()
                .Where(wp => wp.PermitNumber.StartsWith(prefix) && wp.DeletedAt == null)
                .Select(wp => wp.PermitNumber)
                .ToListAsync(ct);

            int maxSeq = 0;
            foreach (var pNum in existingPermits)
            {
                var suffix = pNum[prefix.Length..];
                if (int.TryParse(suffix, out var parsed) && parsed > maxSeq)
                    maxSeq = parsed;
            }

            return $"{prefix}{(maxSeq + 1):D5}";
        }
        else
        {
            var subStation = await _db.SubStations.AsNoTracking().FirstOrDefaultAsync(ss => ss.Id == subStationId && ss.DeletedAt == null, ct);
            if (subStation == null)
                throw new InvalidOperationException("SubStation not found.");

            if (string.IsNullOrWhiteSpace(subStation.Code))
                throw new InvalidOperationException("SubStation code is missing or blank.");

            var year = DateTime.UtcNow.Year;
            var prefix = $"WP-{year}-{subStation.Code.Trim()}-";

            var existingPermits = await _db.WorkPermits
                .AsNoTracking()
                .Where(wp => wp.PermitNumber.StartsWith(prefix) && wp.DeletedAt == null)
                .Select(wp => wp.PermitNumber)
                .ToListAsync(ct);

            int maxSeq = 0;
            foreach (var pNum in existingPermits)
            {
                var suffix = pNum[prefix.Length..];
                if (int.TryParse(suffix, out var parsed) && parsed > maxSeq)
                    maxSeq = parsed;
            }

            return $"{prefix}{(maxSeq + 1):D4}";
        }
    }

    private async Task<bool> UserHasActionPermissionAsync(int userId, string actionKey, CancellationToken ct)
    {
        if (_currentUser.HasAnyAdminRole)
            return true;

        var userRoleIds = await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);

        if (userRoleIds.Count == 0)
            return false;

        var roleNames = await _db.Roles
            .Where(r => userRoleIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToListAsync(ct);

        if (roleNames.Any(r => r is "Admin" or "SystemOwner"))
            return true;

        if (actionKey == "approve")
        {
            if (roleNames.Any(r => r is "Manager" or "OperationsManager" or "StationManager"))
                return true;

            return await _db.RolePermissions.AnyAsync(rp =>
                userRoleIds.Contains(rp.RoleId) &&
                rp.IsGranted &&
                rp.PagePermission.ModuleDefinition.Key == "maintenance" &&
                rp.PagePermission.ActionKey == "approve" &&
                rp.PagePermission.IsActive, ct);
        }

        if (actionKey == "update")
        {
            if (roleNames.Any(r => r is "Manager" or "OperationsManager" or "Maintenance" or "Technician"))
                return true;

            return await _db.RolePermissions.AnyAsync(rp =>
                userRoleIds.Contains(rp.RoleId) &&
                rp.IsGranted &&
                rp.PagePermission.ModuleDefinition.Key == "maintenance" &&
                (rp.PagePermission.ActionKey == "update" || rp.PagePermission.ActionKey == "manage" || rp.PagePermission.ActionKey == "close_permit") &&
                rp.PagePermission.IsActive, ct);
        }

        if (actionKey == "create")
        {
            if (roleNames.Any(r => r is "Operator" or "Manager" or "Maintenance"))
                return true;

            return await _db.RolePermissions.AnyAsync(rp =>
                userRoleIds.Contains(rp.RoleId) &&
                rp.IsGranted &&
                rp.PagePermission.ModuleDefinition.Key == "maintenance" &&
                rp.PagePermission.ActionKey == "create" &&
                rp.PagePermission.IsActive, ct);
        }

        return false;
    }

    private async Task<PagedResult<WorkPermitListDto>> BuildPagedQuery(Expression<Func<WorkPermit, bool>> predicate, int page, int pageSize, int? status, CancellationToken ct)
    {
        var query = _db.WorkPermits
            .AsNoTracking()
            .Include(wp => wp.SubStation)
            .Include(wp => wp.Generator)
            .Include(wp => wp.MaintenanceTypeLookup)
            .Include(wp => wp.DepartmentLookup)
            .Include(wp => wp.PriorityLookup)
            .Include(wp => wp.StatusLookup)
            .Include(wp => wp.RequestedBy)
            .Include(wp => wp.Tasks)
            .Where(predicate)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(wp => wp.StatusLookupId == status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(wp => wp.PermitDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(wp => new WorkPermitListDto(
                wp.Id,
                wp.PermitNumber,
                wp.SubStation.Name,
                wp.Generator != null ? wp.Generator.Name : null,
                wp.MaintenanceTypeLookup.Name,
                wp.DepartmentLookup.Name,
                wp.PriorityLookup.Name,
                wp.StatusLookup.Name,
                wp.PermitDate,
                wp.RequestedBy.FullName,
                wp.AssignedTo != null ? wp.AssignedTo.FullName : null,
                wp.PlannedStartDate,
                wp.ActualStartDate,
                wp.Tasks.Count,
                wp.Tasks.Count(t => t.IsCompleted),
                wp.StatusLookup.Code, wp.PriorityLookup.Code))
            .ToListAsync(ct);

        return new PagedResult<WorkPermitListDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static WorkPermitDetailDto MapToDetail(WorkPermit wp)
    {
        return new WorkPermitDetailDto(
            wp.Id,
            wp.PermitNumber,
            wp.SubStationId,
            wp.SubStation.Name,
            wp.GeneratorId,
            wp.Generator != null ? wp.Generator.Name : null,
            wp.MaintenanceTypeLookupId,
            wp.MaintenanceTypeLookup.Name,
            wp.DepartmentLookupId,
            wp.DepartmentLookup.Name,
            wp.PriorityLookupId,
            wp.PriorityLookup.Name,
            wp.StatusLookupId,
            wp.StatusLookup.Name,
            wp.PermitDate,
            wp.ProblemDescription,
            wp.ProblemCause,
            wp.PlannedStartDate,
            wp.PlannedEndDate,
            wp.ActualStartDate,
            wp.ActualEndDate,
            wp.RunningHoursAtCreation,
            wp.RequestedById,
            wp.RequestedBy.FullName,
            wp.AssignedToId,
            wp.AssignedTo != null ? wp.AssignedTo.FullName : null,
            wp.ApprovedById,
            wp.ApprovedBy != null ? wp.ApprovedBy.FullName : null,
            wp.CompletionNotes,
            wp.RejectionReason,
            wp.Notes,
            wp.CreatedAt,
            wp.Tasks.Select(t => new WorkTaskDto(
                t.Id,
                t.TaskDescription,
                t.SortOrder,
                t.IsCompleted,
                t.CompletedAt,
                t.CompletedById,
                "")).ToList(),
            wp.DispatchRequests.Select(dr => new DispatchRequestListDto(
                dr.Id,
                dr.RequestNumber,
                dr.SubStation.Name,
                dr.Station.Name,
                dr.RequestDate,
                dr.StatusLookup.Name,
                dr.RequestedBy.FullName,
                dr.RequestedById,
                dr.Items.Count,
                null,
                dr.WorkPermitId,
                dr.Purpose,
                dr.CreatedAt)).ToList(),
            wp.InstalledParts.Select(ip => new InstalledPartDto(
                ip.Id,
                ip.GeneratorId,
                ip.Generator.Name,
                ip.SparePartId,
                ip.SparePart.Name,
                ip.SparePart.PartNumber,
                null,
                ip.InstallationDate,
                ip.InstallationHours,
                ip.ReplacementIntervalHours,
                ip.EstimatedReplacementAtHours,
                null,
                null,
                false,
                ip.IsActive,
                ip.RemovedAt,
                ip.RemovedAtHours,
                ip.EarlyReplacementReason,
                ip.Notes)).ToList(),
            wp.Logs.Select(l => new MaintenanceLogDto(
                l.Id,
                wp.PermitNumber,
                l.Generator != null ? l.Generator.Name : null,
                l.LogDate,
                l.RunningHoursAtLog,
                l.Action,
                l.Notes,
                l.LoggedBy.FullName,
                l.CreatedAt)).ToList());
    }
}
