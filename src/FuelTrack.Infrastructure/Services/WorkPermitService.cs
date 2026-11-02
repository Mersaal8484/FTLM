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
    private readonly FuelTrackDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserScopeFilterService _scopeFilter;

    public WorkPermitService(FuelTrackDbContext db, ICurrentUserService currentUser, IUserScopeFilterService scopeFilter)
    {
        _db = db;
        _currentUser = currentUser;
        _scopeFilter = scopeFilter;
    }

    public async Task<WorkPermitDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
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

    public async Task<PagedResult<WorkPermitListDto>> GetBySubStationAsync(int subStationId, int page, int pageSize, int? status = null, CancellationToken ct = default)
    {
        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (!accessibleIds.Contains(subStationId))
                    return new PagedResult<WorkPermitListDto> { Items = Array.Empty<WorkPermitListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
            }
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
            if (currentUserId.HasValue)
            {
                var accessibleSubStationIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleSubStationIds.Count == 0)
                    return new PagedResult<WorkPermitListDto> { Items = Array.Empty<WorkPermitListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
                query = query.Where(wp => accessibleSubStationIds.Contains(wp.SubStationId));
            }
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
                wp.Tasks.Count(t => t.IsCompleted)))
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

        if (!_currentUser.HasAnyAdminRole)
        {
            var currentUserId = _currentUser.UserId;
            if (currentUserId.HasValue)
            {
                var accessibleSubStationIds = await _scopeFilter.GetAccessibleSubStationIdsAsync(currentUserId.Value);
                if (accessibleSubStationIds.Count == 0)
                    return new PagedResult<WorkPermitListDto> { Items = Array.Empty<WorkPermitListDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
                query = query.Where(wp => accessibleSubStationIds.Contains(wp.SubStationId));
            }
        }

        if (status.HasValue)
            query = query.Where(wp => wp.StatusLookupId == status.Value);

        if (priority.HasValue)
            query = query.Where(wp => wp.PriorityLookupId == priority.Value);

        if (subStationId.HasValue)
            query = query.Where(wp => wp.SubStationId == subStationId.Value);

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
                wp.Tasks.Count(t => t.IsCompleted)))
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
        var subStation = await _db.SubStations.FirstOrDefaultAsync(ss => ss.Id == dto.SubStationId && ss.DeletedAt == null, ct);
        if (subStation is null)
            return Result<WorkPermitDetailDto>.NotFound("SubStation not found.");

        if (dto.GeneratorId.HasValue)
        {
            var generator = await _db.Generators.FirstOrDefaultAsync(g => g.Id == dto.GeneratorId.Value && g.DeletedAt == null, ct);
            if (generator is null)
                return Result<WorkPermitDetailDto>.NotFound("Generator not found.");
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

        var permitNumber = await GeneratePermitNumberAsync(dto.SubStationId, ct);
        var draftStatus = await _db.Lookups
            .FirstOrDefaultAsync(l => l.Category.Name == "MaintenanceStatus" && l.Code == "Draft", ct);

        var permit = new WorkPermit
        {
            PermitNumber = permitNumber,
            SubStationId = dto.SubStationId,
            GeneratorId = dto.GeneratorId,
            MaintenanceTypeLookupId = dto.MaintenanceTypeLookupId,
            DepartmentLookupId = dto.DepartmentLookupId,
            PriorityLookupId = dto.PriorityLookupId,
            StatusLookupId = draftStatus != null ? draftStatus.Id : 0,
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

        _db.WorkPermits.Add(permit);
        await _db.SaveChangesAsync(ct);

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
            .FirstOrDefaultAsync(wp => wp.Id == id && wp.DeletedAt == null, ct);

        if (permit is null)
            return Result<WorkPermitDetailDto>.NotFound($"Work permit {id} not found.");

        if (permit.StatusLookup.Code == "Completed" || permit.StatusLookup.Code == "Cancelled")
            return Result<WorkPermitDetailDto>.Failure("Cannot update a completed or cancelled work permit.");

        if (dto.GeneratorId.HasValue)
        {
            var generator = await _db.Generators.FirstOrDefaultAsync(g => g.Id == dto.GeneratorId.Value && g.DeletedAt == null, ct);
            if (generator is null)
                return Result<WorkPermitDetailDto>.NotFound("Generator not found.");
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
            var existingTaskIds = permit.Tasks.Select(t => t.Id).ToList();
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
            .FirstOrDefaultAsync(wp => wp.Id == id && wp.DeletedAt == null, ct);

        if (permit is null)
            return Result<WorkPermitDetailDto>.NotFound($"Work permit {id} not found.");

        var newStatus = await _db.Lookups.FirstOrDefaultAsync(l => l.Id == dto.StatusLookupId && l.DeletedAt == null, ct);
        if (newStatus == null)
            return Result<WorkPermitDetailDto>.NotFound("Status not found.");

        permit.StatusLookupId = dto.StatusLookupId;
        permit.Notes = string.IsNullOrEmpty(dto.Notes) ? permit.Notes : $"{permit.Notes}\n{dto.Notes}".Trim();
        permit.RejectionReason = dto.RejectionReason;
        permit.ActualStartDate = dto.ActualStartDate ?? permit.ActualStartDate;
        permit.ActualEndDate = dto.ActualEndDate ?? permit.ActualEndDate;
        permit.UpdatedAt = DateTime.UtcNow;
        permit.UpdatedBy = userId;

        await _db.SaveChangesAsync(ct);

        var detail = await GetByIdAsync(permit.Id, ct);
        return Result<WorkPermitDetailDto>.Success(detail!);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var permit = await _db.WorkPermits.FindAsync(new object[] { id }, ct);
        if (permit is null)
            return Result.NotFound($"Work permit {id} not found.");

        permit.IsActive = false;
        permit.DeletedAt = DateTime.UtcNow;
        permit.UpdatedAt = DateTime.UtcNow;
        permit.UpdatedBy = 0;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<WorkTaskDto> CompleteTaskAsync(int permitId, int taskId, int userId, CancellationToken ct = default)
    {
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
        if (permit is null)
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
        var subStation = await _db.SubStations.FirstOrDefaultAsync(ss => ss.Id == subStationId && ss.DeletedAt == null, ct);
        if (subStation == null)
            throw new InvalidOperationException("SubStation not found.");

        var year = DateTime.UtcNow.Year;
        var prefix = $"WP-{year}-{subStation.Code}";

        var lastPermit = await _db.WorkPermits
            .Where(wp => wp.PermitNumber.StartsWith(prefix) && wp.DeletedAt == null)
            .OrderByDescending(wp => wp.PermitNumber)
            .FirstOrDefaultAsync(ct);

        int seq = 1;
        if (lastPermit != null)
        {
            var parts = lastPermit.PermitNumber.Split('-');
            if (parts.Length == 4 && int.TryParse(parts[3], out var lastSeq))
                seq = lastSeq + 1;
        }

        return $"{prefix}-{seq:D4}";
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
                wp.Tasks.Count(t => t.IsCompleted)))
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
