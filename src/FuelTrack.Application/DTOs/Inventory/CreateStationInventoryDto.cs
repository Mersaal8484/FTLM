namespace FuelTrack.Application.DTOs.Inventory;

public record StationTankReadingDto(int TankId, double ActualQtyLiters);
public record StationOilReadingDto(int OilId, double ActualQtyLiters);
public record CreateStationInventoryDto(
    int StationId, int Year, int Month, int PeriodTypeLookupId, DateTime InventoryDate,
    string? Notes, List<StationTankReadingDto> FuelReadings, List<StationOilReadingDto> OilReadings);
