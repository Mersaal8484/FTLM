using FuelTrack.Domain.Enums;

namespace FuelTrack.Application.DTOs.FuelTanks;

public record CreateFuelTankDto(
    int SubStationId,
    int FuelLookupId,
    string Name,
    string Code,
    double CapacityLiters,
    double? MinAlertLiters = null,
    double? DeadStockLiters = null,
    double? ActualStockLiters = null,
    bool HasMeter = false,
    double? MeterReading = null,
    double? MeterReadingActual = null,
    string? Notes = null,
    bool IsActive = true,
    TankRole? TankRole = null,
    int? GeneratorId = null,
    int? ParentTankId = null,
    TankShape? Shape = null,
    decimal? DiameterCm = null,
    decimal? CylinderHeightCm = null,
    decimal? ConeHeightCm = null,
    decimal? MultiplicationFactor = null,
    TankCalibrationMode? CalibrationMode = null,
    decimal? LitresPerCm = null,
    decimal? CapacityHeightCm = null,
    decimal? MinAlertHeightCm = null,
    decimal? DeadStockHeightCm = null,
    decimal? ActualStockHeightCm = null)
{
    public CreateFuelTankDto() : this(0, 0, string.Empty, string.Empty, 0) { }
}
