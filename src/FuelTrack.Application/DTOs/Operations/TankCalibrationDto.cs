namespace FuelTrack.Application.DTOs.Operations;

public sealed record UpdateTankCalibrationDto(decimal DiameterCm, decimal CylinderHeightCm, decimal ConeHeightCm, decimal MultiplicationFactor);
public sealed record TankCalibrationDto(int FuelTankId, decimal DiameterCm, decimal CylinderHeightCm, decimal ConeHeightCm, decimal MultiplicationFactor, decimal ComputedCapacityLitres);
