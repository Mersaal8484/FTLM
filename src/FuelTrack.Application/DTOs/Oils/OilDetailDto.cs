namespace FuelTrack.Application.DTOs.Oils;

public record OilDetailDto(
    int Id,
    int OilLookupId,
    string OilLookupName,
    decimal CurrentStock,
    decimal MinStock,
    int? StationId,
    string? StationName,
    int? SubStationId,
    string? SubStationName,
    DateTime CreatedAt,
    DateTime UpdatedAt);
