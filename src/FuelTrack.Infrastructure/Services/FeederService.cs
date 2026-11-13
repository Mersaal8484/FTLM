using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public sealed class FeederService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, ICurrentUserService currentUser) : IFeederService
{
    public async Task<Result<FeederDto>> CreateAsync(CreateFeederDto dto, int userId, CancellationToken cancellationToken = default)
    {
        if (dto.SubStationId <= 0 || string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name) || dto.MeterMultiplier is null or <= 0)
            return Result<FeederDto>.Failure("Site, code, name, and a positive meter multiplier are required.");
        if (!currentUser.HasAnyAdminRole && !await scopeFilter.HasAccessToSubStationAsync(userId, dto.SubStationId)) return Result<FeederDto>.NotFound("Site not found or outside your scope.");
        if (!await db.SubStations.AnyAsync(x => x.Id == dto.SubStationId, cancellationToken)) return Result<FeederDto>.NotFound("Site was not found.");
        if (dto.MeterId.HasValue && !await db.Meters.AnyAsync(x => x.Id == dto.MeterId && x.SubStationId == dto.SubStationId, cancellationToken)) return Result<FeederDto>.Failure("The selected meter does not belong to the Site.");
        if (await db.Feeders.AnyAsync(x => x.SubStationId == dto.SubStationId && x.Code == dto.Code.Trim(), cancellationToken)) return Result<FeederDto>.Conflict("Feeder code already exists at this Site.");
        var feeder = new Feeder { SubStationId = dto.SubStationId, Code = dto.Code.Trim(), Name = dto.Name.Trim(), MeterMultiplier = dto.MeterMultiplier ?? 1m, MeterId = dto.MeterId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, CreatedBy = userId, UpdatedBy = userId };
        db.Feeders.Add(feeder); await db.SaveChangesAsync(cancellationToken);
        return Result<FeederDto>.Success(ToDto(feeder), 201);
    }

    public async Task<Result<FeederDto>> UpdateAsync(int feederId, UpdateFeederDto dto, int userId, CancellationToken cancellationToken = default)
    {
        var feeder = await db.Feeders.FirstOrDefaultAsync(x => x.Id == feederId, cancellationToken);
        if (feeder is null) return Result<FeederDto>.NotFound("Feeder not found.");
        if (!currentUser.HasAnyAdminRole && !await scopeFilter.HasAccessToSubStationAsync(userId, feeder.SubStationId))
            return Result<FeederDto>.NotFound("Feeder not found or outside your scope.");
        if (!dto.IsActive)
        {
            var deactivation = await DeactivateAsync(feederId, userId, cancellationToken);
            return deactivation.IsSuccess
                ? Result<FeederDto>.Success(ToDto(feeder))
                : Result<FeederDto>.Failure(deactivation.Error ?? "Unable to deactivate feeder.", deactivation.StatusCode);
        }
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.MeterMultiplier is null or <= 0)
            return Result<FeederDto>.Failure("Name and a positive meter multiplier are required.");
        if (dto.MeterId.HasValue && !await db.Meters.AnyAsync(x => x.Id == dto.MeterId && x.SubStationId == feeder.SubStationId, cancellationToken))
            return Result<FeederDto>.Failure("The selected meter does not belong to the Site.");

        feeder.Name = dto.Name.Trim();
        feeder.MeterMultiplier = dto.MeterMultiplier.Value;
        feeder.MeterId = dto.MeterId;
        feeder.UpdatedAt = DateTime.UtcNow;
        feeder.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken);
        return Result<FeederDto>.Success(ToDto(feeder));
    }

    public async Task<Result> DeactivateAsync(int feederId, int userId, CancellationToken cancellationToken = default)
    {
        var feeder = await db.Feeders.FirstOrDefaultAsync(x => x.Id == feederId, cancellationToken);
        if (feeder is null) return Result.NotFound("Feeder not found.");
        if (!currentUser.HasAnyAdminRole && !await scopeFilter.HasAccessToSubStationAsync(userId, feeder.SubStationId))
            return Result.NotFound("Feeder not found or outside your scope.");

        feeder.DeletedAt = DateTime.UtcNow;
        feeder.UpdatedAt = DateTime.UtcNow;
        feeder.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<int>> CreateDefaultsAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (!currentUser.HasAnyAdminRole) return Result<int>.Forbidden("Only an administrator can create default feeders.");
        var siteIds = await db.SubStations.AsNoTracking().Select(x => x.Id).ToListAsync(cancellationToken);
        var configuredSiteIds = await db.Feeders.AsNoTracking().Select(x => x.SubStationId).Distinct().ToListAsync(cancellationToken);
        var missing = siteIds.Except(configuredSiteIds).ToList();
        if (missing.Count == 0) return Result<int>.Success(0);

        var now = DateTime.UtcNow;
        db.Feeders.AddRange(missing.Select(siteId => new Feeder
        {
            SubStationId = siteId,
            Code = "F1",
            Name = "\u0627\u0644\u0641\u064a\u062f\u0631 \u0627\u0644\u0631\u0626\u064a\u0633\u064a",
            MeterMultiplier = 1m,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = userId,
            UpdatedBy = userId,
        }));
        await db.SaveChangesAsync(cancellationToken);
        return Result<int>.Success(missing.Count, 201);
    }

    public async Task<IReadOnlyList<FeederDto>> GetBySiteAsync(int subStationId, int userId, CancellationToken cancellationToken = default)
    {
        if (!currentUser.HasAnyAdminRole && !await scopeFilter.HasAccessToSubStationAsync(userId, subStationId)) return [];
        return await db.Feeders.AsNoTracking().Where(x => x.SubStationId == subStationId).OrderBy(x => x.Name).Select(x => new FeederDto(x.Id, x.SubStationId, x.Code, x.Name, x.MeterMultiplier, x.MeterId)).ToListAsync(cancellationToken);
    }

    private static FeederDto ToDto(Feeder feeder) => new(feeder.Id, feeder.SubStationId, feeder.Code, feeder.Name, feeder.MeterMultiplier, feeder.MeterId);
}
