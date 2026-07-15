namespace FuelTrack.Application.DTOs.Oils;

public record UpdateOilDto(
    int? OilLookupId,
    decimal? CurrentStock,
    decimal? MinStock,
    int? StationId,
    int? SubStationId);
