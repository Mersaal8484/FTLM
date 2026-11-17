using FuelTrack.Domain.Enums;

namespace FuelTrack.Application.DTOs.FuelTanks;

public record CreateFuelTankDto(
    int SubStationId,
    int FuelLookupId,
    string Name,
    string Code,
    double CapacityLiters,
    double? MinAlertLiters,
    double? DeadStockLiters,
    double? ActualStockLiters,
    bool HasMeter,
    double? MeterReading,
    double? MeterReadingActual,
    string? Notes,
    bool IsActive = true,
    TankRole? TankRole = null,
    int? GeneratorId = null,
    int? ParentTankId = null,
    TankShape? Shape = null,
    decimal? DiameterCm = null,
    decimal? CylinderHeightCm = null,
    decimal? ConeHeightCm = null,
    decimal? MultiplicationFactor = null);
