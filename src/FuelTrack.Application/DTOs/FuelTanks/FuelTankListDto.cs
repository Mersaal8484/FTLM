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
    double? MeterReadingActual);
