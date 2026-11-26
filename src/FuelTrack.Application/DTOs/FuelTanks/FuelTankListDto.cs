using FuelTrack.Domain.Enums;

namespace FuelTrack.Application.DTOs.FuelTanks;

public record FuelTankListDto(
    int Id,
    int SubStationId,
    string SubStationName,
    int? FuelLookupId,
    string? FuelName,
    string Name,
    string Code,
    double CapacityLiters,
    double? MinAlertLiters,
    double? DeadStockLiters,
    double? ActualStockLiters,
    bool HasMeter,
    double? MeterReading,
    double? MeterReadingActual,
    bool IsActive,
    decimal? DeadStockLevelCm = null,
    string? DeadStockInputUnit = null,
    decimal? DiameterCm = null,
    decimal? CylinderHeightCm = null,
    decimal? ConeHeightCm = null,
    decimal? MultiplicationFactor = null,
    TankRole? TankRole = null,
    int? GeneratorId = null,
    string? GeneratorName = null,
    int? ParentTankId = null,
    TankShape? Shape = null,
    TankCalibrationMode CalibrationMode = TankCalibrationMode.Geometry,
    decimal? LitresPerCm = null,
    decimal? CapacityHeightCm = null,
    decimal? MinAlertHeightCm = null,
    decimal? ActualStockHeightCm = null,
    string? CapacityInputUnit = null,
    string? MinAlertInputUnit = null,
    string? ActualStockInputUnit = null,
    string? ParentTankName = null,
    decimal? MaxLevelCm = null);

public sealed record TankLevelConversionDto(
    int TankId,
    decimal LiquidLevelCm,
    decimal Litres,
    decimal? MaxLevelCm,
    TankCalibrationMode CalibrationMode,
    decimal? LitresPerCm);
