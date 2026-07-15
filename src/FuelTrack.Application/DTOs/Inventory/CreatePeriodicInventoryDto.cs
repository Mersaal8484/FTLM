namespace FuelTrack.Application.DTOs.Inventory;

public record CreatePeriodicInventoryDto(
    int SubStationId,
    int Year,
    int Month,
    int PeriodTypeLookupId,
    DateTime InventoryDate,
    string? Notes,
    List<FuelReadingDto>? FuelReadings,
    List<OilReadingDto>? OilReadings);

public record FuelReadingDto(
    int? FuelTankId,
    int FuelLookupId,
    double ExpectedQtyLiters,
    double ActualQtyLiters);

public record OilReadingDto(
    int? OilId,
    int OilLookupId,
    double ExpectedQtyLiters,
    double ActualQtyLiters);
