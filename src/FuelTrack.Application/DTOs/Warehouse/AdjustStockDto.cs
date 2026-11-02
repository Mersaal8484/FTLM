namespace FuelTrack.Application.DTOs.Warehouse;

public record AdjustStockDto(
    int StationId,
    decimal Delta,
    string Reason);
