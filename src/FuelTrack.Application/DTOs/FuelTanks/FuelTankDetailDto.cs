namespace FuelTrack.Application.DTOs.FuelTanks;

public record FuelTankDetailDto(
    int Id,
    int SubStationId,
    string SubStationName,
    int FuelLookupId,
    string FuelLookupName,
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
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
