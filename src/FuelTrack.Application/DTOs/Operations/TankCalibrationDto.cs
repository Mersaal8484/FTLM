namespace FuelTrack.Application.DTOs.Operations;

public sealed record UpdateTankCalibrationDto(
    decimal DiameterCm,
    decimal CylinderHeightCm,
    decimal ConeHeightCm,
    decimal MultiplicationFactor,
    decimal? DeadStockLitres = null,
    decimal? DeadStockLevelCm = null);
public sealed record TankCalibrationDto(
    int FuelTankId,
    decimal DiameterCm,
    decimal CylinderHeightCm,
    decimal ConeHeightCm,
    decimal MultiplicationFactor,
    decimal ComputedCapacityLitres,
    decimal? DeadStockLitres,
    decimal? DeadStockLevelCm,
    string DeadStockInputUnit);
public sealed record TankCalibrationPreviewDto(
    decimal ComputedCapacityLitres,
    IReadOnlyList<TankCalibrationPreviewPointDto> Points,
    decimal? DeadStockLitres,
    decimal? DeadStockLevelCm);
public sealed record TankCalibrationPreviewPointDto(decimal PercentOfHeight, decimal LiquidLevelCm, decimal Litres);
