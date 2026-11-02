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
    bool IsActive = true);
