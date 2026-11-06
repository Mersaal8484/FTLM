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

    public async Task<IReadOnlyList<FeederDto>> GetBySiteAsync(int subStationId, int userId, CancellationToken cancellationToken = default)
    {
        if (!currentUser.HasAnyAdminRole && !await scopeFilter.HasAccessToSubStationAsync(userId, subStationId)) return [];
        return await db.Feeders.AsNoTracking().Where(x => x.SubStationId == subStationId).OrderBy(x => x.Name).Select(x => new FeederDto(x.Id, x.SubStationId, x.Code, x.Name, x.MeterMultiplier, x.MeterId)).ToListAsync(cancellationToken);
    }

    private static FeederDto ToDto(Feeder feeder) => new(feeder.Id, feeder.SubStationId, feeder.Code, feeder.Name, feeder.MeterMultiplier, feeder.MeterId);
}
