using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Operations;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public sealed class TankCalibrationService(FuelTrackDbContext db, IUserScopeFilterService scopeFilter, ICurrentUserService currentUser) : ITankCalibrationService
{
    public TankCalibrationPreviewDto Preview(UpdateTankCalibrationDto dto)
    {
        if (!IsValid(dto))
            throw new ArgumentOutOfRangeException(nameof(dto), "Tank calibration values are invalid.");
        var fullHeight = dto.ConeHeightCm + dto.CylinderHeightCm;
        var points = new[] { 0m, 25m, 50m, 75m, 100m }
            .Select(percent => new TankCalibrationPreviewPointDto(percent, fullHeight * percent / 100m,
                TankGeometry.CalculateLitres(dto.DiameterCm, dto.CylinderHeightCm, dto.ConeHeightCm, dto.MultiplicationFactor, fullHeight * percent / 100m)))
            .ToList();
        var deadStockLitres = ResolveDeadStockLitres(dto);
        return new TankCalibrationPreviewDto(
            TankGeometry.CalculateCapacityLitres(dto.DiameterCm, dto.CylinderHeightCm, dto.ConeHeightCm, dto.MultiplicationFactor),
            points,
            deadStockLitres,
            deadStockLitres.HasValue ? TankGeometry.CalculateLiquidLevelCm(dto.DiameterCm, dto.CylinderHeightCm, dto.ConeHeightCm, dto.MultiplicationFactor, deadStockLitres.Value) : null);
    }

    public async Task<Result<TankCalibrationDto>> UpsertAsync(int fuelTankId, UpdateTankCalibrationDto dto, int userId, CancellationToken cancellationToken = default)
    {
        if (!IsValid(dto))
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
        var deadStockLitres = ResolveDeadStockLitres(dto);
        if (deadStockLitres.HasValue)
            tank.DeadStockLiters = (double)deadStockLitres.Value;
        tank.UpdatedAt = DateTime.UtcNow;
        tank.UpdatedBy = userId;
        await db.SaveChangesAsync(cancellationToken);
        return Result<TankCalibrationDto>.Success(new(
            tank.Id, dto.DiameterCm, dto.CylinderHeightCm, dto.ConeHeightCm, dto.MultiplicationFactor,
            tank.ComputedCapacityLitres.Value, deadStockLitres,
            deadStockLitres.HasValue ? TankGeometry.CalculateLiquidLevelCm(dto.DiameterCm, dto.CylinderHeightCm, dto.ConeHeightCm, dto.MultiplicationFactor, deadStockLitres.Value) : null));
    }

    private static bool IsValid(UpdateTankCalibrationDto dto) =>
        dto.DiameterCm > 0 && dto.CylinderHeightCm >= 0 && dto.ConeHeightCm > 0 && dto.MultiplicationFactor > 0 &&
        !(dto.DeadStockLitres.HasValue && dto.DeadStockLevelCm.HasValue) &&
        (!dto.DeadStockLitres.HasValue || dto.DeadStockLitres.Value >= 0) &&
        (!dto.DeadStockLevelCm.HasValue || dto.DeadStockLevelCm.Value >= 0 && dto.DeadStockLevelCm.Value <= dto.CylinderHeightCm + dto.ConeHeightCm);

    private static decimal? ResolveDeadStockLitres(UpdateTankCalibrationDto dto) =>
        dto.DeadStockLitres ?? (dto.DeadStockLevelCm.HasValue
            ? TankGeometry.CalculateLitres(dto.DiameterCm, dto.CylinderHeightCm, dto.ConeHeightCm, dto.MultiplicationFactor, dto.DeadStockLevelCm.Value)
            : null);
}
