namespace FuelTrack.Application.DTOs.Operations;

public sealed record UpdateTankCalibrationDto(decimal DiameterCm, decimal CylinderHeightCm, decimal ConeHeightCm, decimal MultiplicationFactor);
public sealed record TankCalibrationDto(int FuelTankId, decimal DiameterCm, decimal CylinderHeightCm, decimal ConeHeightCm, decimal MultiplicationFactor, decimal ComputedCapacityLitres);
public sealed record TankCalibrationPreviewDto(decimal ComputedCapacityLitres, IReadOnlyList<TankCalibrationPreviewPointDto> Points);
public sealed record TankCalibrationPreviewPointDto(decimal PercentOfHeight, decimal LiquidLevelCm, decimal Litres);
