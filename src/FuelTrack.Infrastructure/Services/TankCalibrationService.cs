using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public sealed class TankCalibrationService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, ICurrentUserService currentUser) : ITankCalibrationService
{
    public async Task<Result<TankCalibrationDto>> UpsertAsync(int fuelTankId, UpdateTankCalibrationDto dto, int userId, CancellationToken cancellationToken = default)
    {
        if (dto.DiameterCm <= 0 || dto.CylinderHeightCm < 0 || dto.ConeHeightCm <= 0 || dto.MultiplicationFactor <= 0)
            return Result<TankCalibrationDto>.Failure("Tank calibration values are invalid.");
        var tank = await db.FuelTanks.SingleOrDefaultAsync(x => x.Id == fuelTankId, cancellationToken);
        if (tank is null || (!currentUser.HasAnyAdminRole && !await scopeFilter.HasAccessToSubStationAsync(userId, tank.SubStationId)))
            return Result<TankCalibrationDto>.NotFound("Tank not found or outside your scope.");

        tank.Shape = TankShape.VerticalCylinderWithConicalBottom;
        tank.DiameterCm = dto.DiameterCm;
        tank.CylinderHeightCm = dto.CylinderHeightCm;
        tank.ConeHeightCm = dto.ConeHeightCm;
        tank.MultiplicationFactor = dto.MultiplicationFactor;
        tank.ComputedCapacityLitres = TankGeometry.CalculateCapacityLitres(dto.DiameterCm, dto.CylinderHeightCm, dto.ConeHeightCm, dto.MultiplicationFactor);
        tank.UpdatedAt = DateTime.UtcNow;
        tank.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken);
        return Result<TankCalibrationDto>.Success(new(tank.Id, dto.DiameterCm, dto.CylinderHeightCm, dto.ConeHeightCm, dto.MultiplicationFactor, tank.ComputedCapacityLitres.Value));
    }
}
