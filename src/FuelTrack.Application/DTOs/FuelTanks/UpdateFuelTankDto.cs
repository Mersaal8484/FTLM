using FuelTrack.Domain.Enums;

namespace FuelTrack.Application.DTOs.FuelTanks;

public record UpdateFuelTankDto(
    int? SubStationId,
    int? FuelLookupId,
    string? Name,
    string? Code,
    double? CapacityLiters,
    double? MinAlertLiters,
    double? DeadStockLiters,
    double? ActualStockLiters,
    bool? HasMeter,
    double? MeterReading,
    double? MeterReadingActual,
    string? Notes,
    bool? IsActive,
    TankRole? TankRole = null,
    int? GeneratorId = null,
    int? ParentTankId = null,
    TankShape? Shape = null);
