namespace FuelTrack.Application.DTOs.Oils;

public record CreateOilDto(
    int OilLookupId,
    decimal CurrentStock,
    decimal MinStock,
    int? StationId,
    int? SubStationId,
    bool IsActive = true);
