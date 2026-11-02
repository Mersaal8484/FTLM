namespace FuelTrack.Application.DTOs.Inventory;

public record CreatePeriodicInventoryDto(
    int SubStationId,
    int Year,
    int Month,
    int PeriodTypeLookupId,
    DateTime InventoryDate,
    string? Notes,
    List<FuelReadingDto>? FuelReadings,
    List<OilReadingDto>? OilReadings,
    bool? IsApproved = null);

public record FuelReadingDto(
    int FuelLookupId,
    double ActualQtyLiters);

public record OilReadingDto(
    int? OilId,
    int OilLookupId,
    double ActualQtyLiters);
